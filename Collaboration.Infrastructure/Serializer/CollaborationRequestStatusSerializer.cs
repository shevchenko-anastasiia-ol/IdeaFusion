using Collaboration.Domain.Entities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Collaboration.Infrastructure.Serializer;

public class CollaborationRequestStatusSerializer : SerializerBase<CollaborationRequestStatus>
{
    public override CollaborationRequestStatus Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var bsonType = context.Reader.CurrentBsonType;
        if (bsonType == BsonType.Int32)
            return (CollaborationRequestStatus)context.Reader.ReadInt32();
        if (bsonType == BsonType.Int64)
            return (CollaborationRequestStatus)(int)context.Reader.ReadInt64();
        return Enum.Parse<CollaborationRequestStatus>(context.Reader.ReadString());
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, CollaborationRequestStatus value)
    {
        context.Writer.WriteString(value.ToString());
    }
}