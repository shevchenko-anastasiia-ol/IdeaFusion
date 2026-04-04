using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ServiceDiscovery;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using System.Diagnostics;

namespace Microsoft.Extensions.Hosting;

// Adds common Aspire services: service discovery, resilience, health checks, and OpenTelemetry.
// This project should be referenced by each service project in your solution.
// To learn more about using this project, see https://aka.ms/dotnet/aspire/service-defaults
public static class Extensions
{
    private const string HealthEndpointPath = "/health";
    private const string AlivenessEndpointPath = "/alive";

    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        // Configure Serilog for structured logging (must be called before other logging configuration)
        builder.ConfigureSerilog();

        // Configure OpenTelemetry for distributed tracing
        builder.ConfigureOpenTelemetry();

        // Add CorrelationId support
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddTransient<CorrelationIdDelegatingHandler>();

        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            // Add CorrelationId handler to propagate CorrelationId in outgoing HTTP requests
            http.AddHttpMessageHandler<CorrelationIdDelegatingHandler>();

            // Turn on resilience by default
            http.AddStandardResilienceHandler();

            // Turn on service discovery by default
            http.AddServiceDiscovery();
        });

        // Uncomment the following to restrict the allowed schemes for service discovery.
        // builder.Services.Configure<ServiceDiscoveryOptions>(options =>
        // {
        //     options.AllowedSchemes = ["https"];
        // });

        return builder;
    }

    /// <summary>
    /// Configures Serilog for structured logging with automatic context enrichment.
    /// Structured logging stores logs as JSON objects with separate fields for each property.
    /// </summary>
    public static TBuilder ConfigureSerilog<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        // Determine minimum log level based on environment
        var minimumLevel = builder.Environment.IsDevelopment()
            ? LogEventLevel.Debug  // Detailed diagnostics in Development
            : LogEventLevel.Information;  // Reduced noise in Production

        // Configure Serilog logger
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(minimumLevel)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)  // Filter noisy infrastructure logs
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)  // Filter EF Core verbose logs
            .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)  // Filter HTTP client details
            .MinimumLevel.Override("Microsoft.Extensions.Http", LogEventLevel.Warning)
            // Enrichers for automatic log enrichment
            .Enrich.FromLogContext()  // Automatically includes CorrelationId and other context properties
            .Enrich.WithProperty("ServiceName", builder.Environment.ApplicationName)  // Service name for identification
            .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)  // Development/Production
            .Enrich.WithMachineName()  // Computer or container name
            .Enrich.WithThreadId()  // Thread ID for asynchronous debugging
            // Output formatters
            .WriteTo.Console(new CompactJsonFormatter())  // JSON format for Aspire Dashboard compatibility
            // Optional: File sink for persistent storage (uncomment if needed)
            // .WriteTo.File(
            //     new CompactJsonFormatter(),
            //     path: "logs/log-.txt",
            //     rollingInterval: RollingInterval.Day,
            //     retainedFileCountLimit: 7)
            .CreateLogger();

        // Replace default logging with Serilog
        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog(Log.Logger, dispose: true);

        return builder;
    }

    /// <summary>
    /// Configures OpenTelemetry for distributed tracing with automatic instrumentation.
    /// OpenTelemetry automatically records the path of requests through the system.
    /// </summary>
    public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        // Configure logging to OpenTelemetry (complements Serilog)
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        // Determine sampling rate based on environment
        // 100% in Development (keep all traces for debugging), configured percentage in Production
        var samplingRatio = builder.Environment.IsDevelopment() ? 1.0 : 0.1;  // 10% sampling in Production

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();
            })
            .WithTracing(tracing =>
            {
                // Add custom source for application-specific spans
                tracing.AddSource(builder.Environment.ApplicationName);

                // ASP.NET Core instrumentation - automatic span creation for incoming HTTP requests
                tracing.AddAspNetCoreInstrumentation(options =>
                {
                    // Exclude health check requests from tracing
                    options.Filter = context =>
                        !context.Request.Path.StartsWithSegments(HealthEndpointPath)
                        && !context.Request.Path.StartsWithSegments(AlivenessEndpointPath);

                    // Add custom attributes to spans
                    options.EnrichWithHttpRequest = (activity, request) =>
                    {
                        activity.SetTag("service.name", builder.Environment.ApplicationName);
                        activity.SetTag("environment", builder.Environment.EnvironmentName);
                        
                        // Extract CorrelationId from headers if present
                        if (request.Headers.TryGetValue("X-Correlation-Id", out var correlationId))
                        {
                            activity.SetTag("correlation.id", correlationId.ToString());
                        }
                    };

                    options.EnrichWithHttpResponse = (activity, response) =>
                    {
                        activity.SetTag("http.status_code", response.StatusCode);
                    };
                })

                // HttpClient instrumentation - automatic span creation for outgoing HTTP calls
                .AddHttpClientInstrumentation(options =>
                {
                    options.EnrichWithHttpRequestMessage = (activity, request) =>
                    {
                        activity.SetTag("service.name", builder.Environment.ApplicationName);
                        
                        // Extract CorrelationId from headers if present
                        if (request.Headers.TryGetValues("X-Correlation-Id", out var correlationIds))
                        {
                            activity.SetTag("correlation.id", string.Join(",", correlationIds));
                        }
                    };
                })

                // Entity Framework Core instrumentation - SQL traces show specific SQL queries with parameters
                .AddEntityFrameworkCoreInstrumentation(options =>
                {
                    options.EnrichWithIDbCommand = (activity, command) =>
                    {
                        activity.SetTag("db.system", "sqlserver");
                        activity.SetTag("service.name", builder.Environment.ApplicationName);
                    };
                })

                // SQL Client instrumentation - additional SQL tracing
                .AddSqlClientInstrumentation(options =>
                {
                    options.SetDbStatementForText = true;
                    options.EnableConnectionLevelAttributes = true;
                });

                // Configure sampling
                tracing.SetSampler(new TraceIdRatioBasedSampler(samplingRatio));

               
            });

        builder.AddOpenTelemetryExporters();

        return builder;
    }

    private static TBuilder AddOpenTelemetryExporters<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        // Uncomment the following lines to enable the Azure Monitor exporter (requires the Azure.Monitor.OpenTelemetry.AspNetCore package)
        //if (!string.IsNullOrEmpty(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
        //{
        //    builder.Services.AddOpenTelemetry()
        //       .UseAzureMonitor();
        //}

        return builder;
    }

    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddHealthChecks()
            // Add a default liveness check to ensure app is responsive
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        // Add CorrelationId middleware to the pipeline
        // This must be called early in the pipeline to ensure CorrelationId is available for all subsequent middleware
        app.UseCorrelationId();

        // Adding health checks endpoints to applications in non-development environments has security implications.
        // See https://aka.ms/dotnet/aspire/healthchecks for details before enabling these endpoints in non-development environments.
        if (app.Environment.IsDevelopment())
        {
            // All health checks must pass for app to be considered ready to accept traffic after starting
            app.MapHealthChecks(HealthEndpointPath);

            // Only health checks tagged with the "live" tag must pass for app to be considered alive
            app.MapHealthChecks(AlivenessEndpointPath, new HealthCheckOptions
            {
                Predicate = r => r.Tags.Contains("live")
            });
        }

        return app;
    }
}