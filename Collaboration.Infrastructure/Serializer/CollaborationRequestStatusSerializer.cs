using Collaboration.Domain.Entities;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Collaboration.Infrastructure.Serializer;

public class CollaborationRequestStatusSerializer : SerializerBase<CollaborationRequestStatus>
{
    public override CollaborationRequestStatus Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var value = context.Reader.ReadString();
        return Enum.Parse<CollaborationRequestStatus>(value);
    }
 
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, CollaborationRequestStatus value)
    {
        context.Writer.WriteString(value.ToString());
    }
}