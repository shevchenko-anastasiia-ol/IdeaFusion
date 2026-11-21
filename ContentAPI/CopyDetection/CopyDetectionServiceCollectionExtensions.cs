using ContentBLL.Services.Interfaces;
using IdeaFusion.Grpc.Teams;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace ContentAPI.CopyDetection;

public static class CopyDetectionServiceCollectionExtensions
{
    /// <summary>
    /// Wires the image copy detection service into ContentService. Must run after
    /// <c>AddBusinessLayer</c>, because it decorates the registered <see cref="IPostService"/>.
    /// </summary>
    public static IServiceCollection AddCopyDetection(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(CopyDetectionOptions.SectionName);
        services.Configure<CopyDetectionOptions>(section);
        var options = section.Get<CopyDetectionOptions>() ?? new CopyDetectionOptions();

        services.AddHttpClient<IImageCopyClient, ImageCopyClient>((sp, http) =>
            {
                var o = sp.GetRequiredService<IOptions<CopyDetectionOptions>>().Value;
                http.BaseAddress = new Uri(o.BaseUrl.TrimEnd('/') + "/");
                http.Timeout = TimeSpan.FromSeconds(o.TimeoutSeconds);
            })
            .AddServiceDiscovery();

        // ContentService does not otherwise talk to the team service; membership decides
        // whether a match with a team's work is the uploader's own (composite authorship).
        if (!services.Any(d => d.ServiceType == typeof(TeamGrpcService.TeamGrpcServiceClient)))
        {
            services.AddGrpcClient<TeamGrpcService.TeamGrpcServiceClient>(o =>
                o.Address = new Uri(configuration["Grpc__CollaborationService"]
                                    ?? configuration["Grpc:CollaborationService"]
                                    ?? "https://collaborationservice"));
        }
        services.TryAddScoped<IAuthorshipFilter, AuthorshipFilter>();

        if (!options.Enabled)
            return services;

        services.AddSingleton(new CopyCheckQueue(options.QueueCapacity));
        services.AddScoped<CopyJobProcessor>();
        services.AddHostedService<CopyCheckWorker>();
        DecoratePostService(services);
        return services;
    }

    private static void DecoratePostService(IServiceCollection services)
    {
        var registration = services.LastOrDefault(d => d.ServiceType == typeof(IPostService))
                           ?? throw new InvalidOperationException("AddCopyDetection must be called after AddBusinessLayer.");
        services.Remove(registration);
        services.Add(new ServiceDescriptor(typeof(IPostService), sp =>
        {
            var inner = registration.ImplementationFactory?.Invoke(sp) as IPostService
                        ?? (IPostService)ActivatorUtilities.CreateInstance(sp, registration.ImplementationType!);
            return new CopyCheckingPostService(
                inner,
                sp.GetRequiredService<CopyCheckQueue>(),
                sp.GetRequiredService<ILogger<CopyCheckingPostService>>());
        }, registration.Lifetime));
    }
}
