using Collaboration.Domain.Entities;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Collaboration.Infrastructure.Data;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;
    public IMongoDatabase Database => _database;
 
    public MongoDbContext(IOptions<MongoDbSettings> settings)
    {
        var mongoSettings = MongoClientSettings.FromConnectionString(settings.Value.ConnectionString);
        mongoSettings.MaxConnectionPoolSize = settings.Value.MaxConnectionPoolSize;
        mongoSettings.MinConnectionPoolSize = settings.Value.MinConnectionPoolSize;
        mongoSettings.ConnectTimeout = TimeSpan.FromSeconds(settings.Value.ConnectTimeoutSeconds);
        mongoSettings.SocketTimeout = TimeSpan.FromSeconds(settings.Value.SocketTimeoutSeconds);
 
        var client = new MongoClient(mongoSettings);
        _database = client.GetDatabase(settings.Value.DatabaseName);
    }
 
    public IMongoCollection<Team> Teams
        => _database.GetCollection<Team>("Teams");
 
    public IMongoCollection<CollaborationRequest> CollaborationRequests
        => _database.GetCollection<CollaborationRequest>("CollaborationRequests");
 
    public IMongoCollection<GroupInvitation> GroupInvitations
        => _database.GetCollection<GroupInvitation>("GroupInvitations");
 
    public IMongoCollection<TeamPost> TeamPosts
        => _database.GetCollection<TeamPost>("TeamPosts");
 
    public IClientSessionHandle StartSession() => _database.Client.StartSession();
}