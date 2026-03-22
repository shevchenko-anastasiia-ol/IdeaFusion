using Collaboration.Domain.Entities;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Collaboration.Infrastructure.Serializer;

public class InvitationStatusSerializer : SerializerBase<InvitationStatus>
{
    public override InvitationStatus Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var value = context.Reader.ReadString();
        return Enum.Parse<InvitationStatus>(value);
    }
 
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, InvitationStatus value)
    {
        context.Writer.WriteString(value.ToString());
    }
}