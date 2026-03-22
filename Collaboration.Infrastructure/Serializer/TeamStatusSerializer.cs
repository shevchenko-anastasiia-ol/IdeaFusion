using Collaboration.Domain.Entities;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Collaboration.Infrastructure.Serializer;

public class TeamStatusSerializer : SerializerBase<TeamStatus>
{
    public override TeamStatus Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var value = context.Reader.ReadString();
        return Enum.Parse<TeamStatus>(value);
    }
 
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TeamStatus value)
    {
        context.Writer.WriteString(value.ToString());
    }
}