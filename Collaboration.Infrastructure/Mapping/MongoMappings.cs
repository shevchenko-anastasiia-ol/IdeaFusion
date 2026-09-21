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
    private static volatile bool _registered;
    private static readonly object _lock = new();

    public static void Register()
    {
        lock (_lock)
        {
            if (_registered) return;
            _registered = true;

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

            // UserSnapshot — custom serializer only; BsonClassMap conflicts with it.
            BsonSerializer.RegisterSerializer(new UserSnapshotSerializer());

            // Class maps — guard against the TOCTOU race where MongoDB's auto-mapping
            // (triggered by concurrent tests that use real MongoDbContext) registers a
            // class map between our IsClassMapRegistered check and RegisterClassMap call.
            RegisterClassMapSafe<BaseEntity>(cm =>
            {
                cm.AutoMap();
                cm.SetIsRootClass(true);
            });
            RegisterClassMapSafe<TeamMember>(cm =>
            {
                cm.AutoMap();
                cm.SetIgnoreExtraElements(true);
            });
            RegisterClassMapSafe<RequiredRole>(cm =>
            {
                cm.AutoMap();
                cm.SetIgnoreExtraElements(true);
            });
            RegisterClassMapSafe<Team>(cm =>
            {
                cm.AutoMap();
                cm.SetIgnoreExtraElements(true);
            });
            RegisterClassMapSafe<CollaborationRequest>(cm =>
            {
                cm.AutoMap();
                cm.SetIgnoreExtraElements(true);
            });
            RegisterClassMapSafe<GroupInvitation>(cm =>
            {
                cm.AutoMap();
                cm.SetIgnoreExtraElements(true);
            });
            RegisterClassMapSafe<TeamPost>(cm =>
            {
                cm.AutoMap();
                cm.SetIgnoreExtraElements(true);
            });
            RegisterClassMapSafe<ChatMessage>(cm =>
            {
                cm.AutoMap();
                cm.SetIgnoreExtraElements(true);
            });
            RegisterClassMapSafe<TeamVacancy>(cm =>
            {
                cm.AutoMap();
                cm.SetIgnoreExtraElements(true);
            });
            RegisterClassMapSafe<VacancyApplication>(cm =>
            {
                cm.AutoMap();
                cm.SetIgnoreExtraElements(true);
            });
        }
    }

    private static void RegisterClassMapSafe<T>(Action<BsonClassMap<T>> init)
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(T))) return;
        try
        {
            BsonClassMap.RegisterClassMap(init);
        }
        catch (ArgumentException) { }
    }
}