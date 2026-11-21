using ContentBLL.MappingProfiles;
using ContentBLL.Services;
using ContentBLL.Services.Interfaces;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ContentBLL;

public static class BLL_DI
{
    public static IServiceCollection AddBusinessLayer(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAutoMapper(cfg => { }, typeof(CommentMappingProfiles).Assembly);
        services.AddValidatorsFromAssembly(typeof(BLL_DI).Assembly);
        
        services.AddScoped<IPostService, PostService>();
        services.AddScoped<ICommentService, CommentService>();
        services.AddScoped<ILikeService, LikeService>();
        services.AddScoped<IPostViewService, PostViewService>();
        services.AddScoped<ISavedPostService, SavedPostService>();
        services.AddScoped<ITagService, TagService>();

        // The single source of "who is calling". Registered here rather than in the
        // API so every service can take it without the API having to pass it down.
        services.AddScoped<IPostAuthorResolver, PostAuthorResolver>();
 
        return services;
    }
}