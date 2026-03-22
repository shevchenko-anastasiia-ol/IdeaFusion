using Collaboration.Domain.Entities;
using Collaboration.Infrastructure.Serializer;
using MongoDB.Bson.Serialization;

namespace Collaboration.Infrastructure.Mapping;

public static class MongoMappings
{
    public static void Register()
    {
        BsonSerializer.RegisterSerializer(new TeamStatusSerializer());
        BsonSerializer.RegisterSerializer(new CollaborationRequestStatusSerializer());
        BsonSerializer.RegisterSerializer(new InvitationStatusSerializer());
        BsonSerializer.RegisterSerializer(new UserSnapshotSerializer());
 
        if (!BsonClassMap.IsClassMapRegistered(typeof(Team)))
        {
            BsonClassMap.RegisterClassMap<Team>(cm =>
            {
                cm.AutoMap();
                cm.MapIdProperty(t => t.Id);
                cm.MapProperty(t => t.Name);
                cm.MapProperty(t => t.Description);
                cm.MapProperty(t => t.Category);
                cm.MapProperty(t => t.Tags);
                cm.MapProperty(t => t.Status);
                cm.MapProperty(t => t.Members);
                cm.MapProperty(t => t.RequiredRoles);
                cm.MapProperty(t => t.CreatedAt);
                cm.MapProperty(t => t.CreatedBy);
                cm.MapProperty(t => t.UpdatedAt);
                cm.MapProperty(t => t.UpdatedBy);
                cm.MapProperty(t => t.IsDeleted);
            });
        }
 
        if (!BsonClassMap.IsClassMapRegistered(typeof(CollaborationRequest)))
        {
            BsonClassMap.RegisterClassMap<CollaborationRequest>(cm =>
            {
                cm.AutoMap();
                cm.MapIdProperty(r => r.Id);
                cm.MapProperty(r => r.TeamId);
                cm.MapProperty(r => r.FromUserId);
                cm.MapProperty(r => r.ToUserId);
                cm.MapProperty(r => r.Role);
                cm.MapProperty(r => r.Message);
                cm.MapProperty(r => r.Status);
                cm.MapProperty(r => r.ResolvedAt);
                cm.MapProperty(r => r.CreatedAt);
                cm.MapProperty(r => r.CreatedBy);
                cm.MapProperty(r => r.UpdatedAt);
                cm.MapProperty(r => r.UpdatedBy);
                cm.MapProperty(r => r.IsDeleted);
            });
        }
 
        if (!BsonClassMap.IsClassMapRegistered(typeof(GroupInvitation)))
        {
            BsonClassMap.RegisterClassMap<GroupInvitation>(cm =>
            {
                cm.AutoMap();
                cm.MapIdProperty(i => i.Id);
                cm.MapProperty(i => i.TeamId);
                cm.MapProperty(i => i.InvitedUserId);
                cm.MapProperty(i => i.InvitedByUserId);
                cm.MapProperty(i => i.Role);
                cm.MapProperty(i => i.Message);
                cm.MapProperty(i => i.Status);
                cm.MapProperty(i => i.ExpiresAt);
                cm.MapProperty(i => i.ResolvedAt);
                cm.MapProperty(i => i.CreatedAt);
                cm.MapProperty(i => i.CreatedBy);
                cm.MapProperty(i => i.UpdatedAt);
                cm.MapProperty(i => i.UpdatedBy);
                cm.MapProperty(i => i.IsDeleted);
            });
        }
 
        // Вкладені класи
        if (!BsonClassMap.IsClassMapRegistered(typeof(TeamMember)))
        {
            BsonClassMap.RegisterClassMap<TeamMember>(cm =>
            {
                cm.AutoMap();
                cm.MapProperty(m => m.UserId);
                cm.MapProperty(m => m.Role);
                cm.MapProperty(m => m.JoinedAt);
            });
        }
 
        if (!BsonClassMap.IsClassMapRegistered(typeof(RequiredRole)))
        {
            BsonClassMap.RegisterClassMap<RequiredRole>(cm =>
            {
                cm.AutoMap();
                cm.MapProperty(r => r.Role);
                cm.MapProperty(r => r.Description);
            });
        }
 
        if (!BsonClassMap.IsClassMapRegistered(typeof(TeamPost)))
        {
            BsonClassMap.RegisterClassMap<TeamPost>(cm =>
            {
                cm.AutoMap();
                cm.MapIdProperty(p => p.Id);
                cm.MapProperty(p => p.PostId);
                cm.MapProperty(p => p.TeamId);
                cm.MapProperty(p => p.Author);
                cm.MapProperty(p => p.Title);
                cm.MapProperty(p => p.PublishedAt);
                cm.MapProperty(p => p.CreatedAt);
                cm.MapProperty(p => p.CreatedBy);
                cm.MapProperty(p => p.UpdatedAt);
                cm.MapProperty(p => p.UpdatedBy);
                cm.MapProperty(p => p.IsDeleted);
            });
        }
    }
}