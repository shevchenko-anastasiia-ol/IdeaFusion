using Collaboration.Domain.ValueOfObjects;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Collaboration.Infrastructure.Serializers;

public class UserSnapshotSerializer : SerializerBase<UserSnapshot>
{
    public override UserSnapshot Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var reader = context.Reader;

        string userId = string.Empty;
        string username = string.Empty;
        string? avatarUrl = null;

        reader.ReadStartDocument();

        while (reader.ReadBsonType() != BsonType.EndOfDocument)
        {
            var fieldName = reader.ReadName();

            switch (fieldName)
            {
                case "userId":
                    userId = reader.ReadString();
                    break;
                case "username":
                    username = reader.ReadString();
                    break;
                case "avatarUrl":
                    if (reader.CurrentBsonType == BsonType.Null)
                    {
                        reader.ReadNull();
                        avatarUrl = null;
                    }
                    else
                    {
                        avatarUrl = reader.ReadString();
                    }
                    break;
                default:
                    reader.SkipValue();
                    break;
            }
        }

        reader.ReadEndDocument();

        return new UserSnapshot(userId, username, avatarUrl);
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, UserSnapshot value)
    {
        var writer = context.Writer;

        writer.WriteStartDocument();
        writer.WriteString("userId", value.UserId);
        writer.WriteString("username", value.Username);

        if (value.AvatarUrl is not null)
            writer.WriteString("avatarUrl", value.AvatarUrl);

        writer.WriteEndDocument();
    }
}