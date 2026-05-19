using Collaboration.Domain.Entities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Collaboration.Infrastructure.Serializer;

public class TeamStatusSerializer : SerializerBase<TeamStatus>
{
    public override TeamStatus Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var bsonType = context.Reader.CurrentBsonType;
        if (bsonType == BsonType.Int32)
            return (TeamStatus)context.Reader.ReadInt32();
        if (bsonType == BsonType.Int64)
            return (TeamStatus)(int)context.Reader.ReadInt64();
        return Enum.Parse<TeamStatus>(context.Reader.ReadString());
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TeamStatus value)
    {
        context.Writer.WriteString(value.ToString());
    }
}