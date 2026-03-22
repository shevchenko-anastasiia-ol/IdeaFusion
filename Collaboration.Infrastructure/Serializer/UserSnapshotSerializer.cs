using Collaboration.Domain.ValueOfObjects;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Collaboration.Infrastructure.Serializer;

public class UserSnapshotSerializer : SerializerBase<UserSnapshot>
{
    public override UserSnapshot Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var doc = BsonDocumentSerializer.Instance.Deserialize(context, args);
 
        var userId = doc.GetValue("userId", BsonNull.Value) != BsonNull.Value
            ? doc["userId"].AsString
            : null;

        var username = doc.GetValue("username", BsonNull.Value) != BsonNull.Value
            ? doc["username"].AsString
            : null;

        var avatarUrl = doc.TryGetValue("avatarUrl", out var av) && av != BsonNull.Value
            ? av.AsString
            : null;
 
        return new UserSnapshot(userId, username, avatarUrl);
    }
 
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, UserSnapshot value)
    {
        var doc = new BsonDocument
        {
            { "userId", value.UserId },
            { "username", value.Username },
            { "avatarUrl", value.AvatarUrl != null ? (BsonValue)value.AvatarUrl : BsonNull.Value }
        };
 
        BsonDocumentSerializer.Instance.Serialize(context, args, doc);
    }
}