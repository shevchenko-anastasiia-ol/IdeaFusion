using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// MongoDB telemetry extension for OpenTelemetry instrumentation.
/// Automatically creates spans for MongoDB operations showing commands and collections.
/// </summary>
public static class MongoDbTelemetryExtensions
{
    public static IServiceCollection AddMongoDbTelemetry(this IServiceCollection services, string connectionString, string? serviceName = null)
    {
        var mongoSettings = MongoClientSettings.FromConnectionString(connectionString);

        mongoSettings.ClusterConfigurator = cb =>
        {
            // Subscribe to command started events to create spans
            cb.Subscribe<CommandStartedEvent>(e =>
            {
                var activity = Activity.Current;
                if (activity != null)
                {
                    // Standard database attributes for OpenTelemetry
                    activity.SetTag("db.system", "mongodb");
                    activity.SetTag("db.operation", e.CommandName);
                    activity.SetTag("db.statement", e.Command.ToString());
                    activity.SetTag("db.database", e.DatabaseNamespace.DatabaseName);
                    activity.SetTag("db.name", e.DatabaseNamespace.DatabaseName);
                    
                    // Extract collection name from command if available
                    if (e.Command.Contains("collection"))
                    {
                        var collection = e.Command["collection"]?.AsString;
                        if (!string.IsNullOrEmpty(collection))
                        {
                            activity.SetTag("db.mongodb.collection", collection);
                        }
                    }

                    // Add service name if provided
                    if (!string.IsNullOrEmpty(serviceName))
                    {
                        activity.SetTag("service.name", serviceName);
                    }
                }
            });

            // Subscribe to command succeeded events to record duration
            cb.Subscribe<CommandSucceededEvent>(e =>
            {
                var activity = Activity.Current;
                if (activity != null)
                {
                    activity.SetTag("db.response.size", e.Reply?.ToString()?.Length ?? 0);
                }
            });

            // Subscribe to command failed events to record errors
            cb.Subscribe<CommandFailedEvent>(e =>
            {
                var activity = Activity.Current;
                if (activity != null)
                {
                    activity.SetStatus(ActivityStatusCode.Error, e.Failure.Message);
                    activity.SetTag("error", true);
                    activity.SetTag("error.message", e.Failure.Message);
                }
            });
        };

        services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoSettings));

        return services;
    }
}