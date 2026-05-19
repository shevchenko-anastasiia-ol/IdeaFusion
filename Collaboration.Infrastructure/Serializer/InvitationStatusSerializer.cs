using Collaboration.Domain.Entities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Collaboration.Infrastructure.Serializer;

public class InvitationStatusSerializer : SerializerBase<InvitationStatus>
{
    public override InvitationStatus Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var bsonType = context.Reader.CurrentBsonType;
        if (bsonType == BsonType.Int32)
            return (InvitationStatus)context.Reader.ReadInt32();
        if (bsonType == BsonType.Int64)
            return (InvitationStatus)(int)context.Reader.ReadInt64();
        return Enum.Parse<InvitationStatus>(context.Reader.ReadString());
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, InvitationStatus value)
    {
        context.Writer.WriteString(value.ToString());
    }
}