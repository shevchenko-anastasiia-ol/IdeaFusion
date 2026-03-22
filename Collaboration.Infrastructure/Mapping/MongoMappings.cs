using Collaboration.Domain.Common;
using Collaboration.Domain.Entities;
using Collaboration.Domain.ValueOfObjects;
using Collaboration.Infrastructure.Serializer;
using Collaboration.Infrastructure.Serializers;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;

namespace Collaboration.Infrastructure.Mapping;

public static class MongoMappings
{
    public static void Register()
    {
        var pack = new ConventionPack
        {
            new IgnoreIfNullConvention(true),
            new IgnoreExtraElementsConvention(true)
        };
        ConventionRegistry.Register("CollaborationConventions", pack, _ => true);

        // Enum serializers
        BsonSerializer.RegisterSerializer(new TeamStatusSerializer());
        BsonSerializer.RegisterSerializer(new CollaborationRequestStatusSerializer());
        BsonSerializer.RegisterSerializer(new InvitationStatusSerializer());

        // UserSnapshot — реєструємо кастомний серіалайзер
        // НЕ реєструємо BsonClassMap для UserSnapshot — вони конфліктують
        BsonSerializer.RegisterSerializer(new UserSnapshotSerializer());

        // BaseEntity
        if (!BsonClassMap.IsClassMapRegistered(typeof(BaseEntity)))
        {
            BsonClassMap.RegisterClassMap<BaseEntity>(cm =>
            {
                cm.AutoMap();
                cm.SetIsRootClass(true);
            });
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(TeamMember)))
        {
            BsonClassMap.RegisterClassMap<TeamMember>(cm =>
            {
                cm.AutoMap();
                cm.SetIgnoreExtraElements(true);
            });
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(RequiredRole)))
        {
            BsonClassMap.RegisterClassMap<RequiredRole>(cm =>
            {
                cm.AutoMap();
                cm.SetIgnoreExtraElements(true);
            });
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(Team)))
        {
            BsonClassMap.RegisterClassMap<Team>(cm =>
            {
                cm.AutoMap();
                cm.SetIgnoreExtraElements(true);
            });
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(CollaborationRequest)))
        {
            BsonClassMap.RegisterClassMap<CollaborationRequest>(cm =>
            {
                cm.AutoMap();
                cm.SetIgnoreExtraElements(true);
            });
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(GroupInvitation)))
        {
            BsonClassMap.RegisterClassMap<GroupInvitation>(cm =>
            {
                cm.AutoMap();
                cm.SetIgnoreExtraElements(true);
            });
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(TeamPost)))
        {
            BsonClassMap.RegisterClassMap<TeamPost>(cm =>
            {
                cm.AutoMap();
                cm.SetIgnoreExtraElements(true);
            });
        }
    }
}