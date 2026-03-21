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
 
        return services;
    }
}