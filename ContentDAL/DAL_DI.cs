using System.Data;
using ContentDAL.Connection;
using ContentDAL.Data;
using ContentDAL.Repository;
using ContentDAL.Repository.Interfaces;
using ContentDAL.UOW;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace ContentDAL;

public static  class DAL_DI
{
    public static IServiceCollection AddDataAccess(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ContentDb");
 
        services.AddDbContext<ContentDbContext>(options =>
            options.UseNpgsql(connectionString));
 
        services.AddScoped<IDbConnection>(_ => new NpgsqlConnection(connectionString));
 
        services.AddScoped<IConnectionFactory>(_ => new ConnectionFactory(connectionString!));
 
        services.AddScoped<IPostRepository, PostRepository>();
        services.AddScoped<ICommentRepository, CommentRepository>();
        services.AddScoped<ILikeRepository, LikeRepository>();
        services.AddScoped<IPostViewRepository, PostViewRepository>();
        services.AddScoped<ISavedPostRepository, SavedPostRepository>();
        services.AddScoped<ITagRepository, TagRepository>();
 
        services.AddScoped<IUnitOfWork, UnitOfWork>();
 
        return services;
    }
}