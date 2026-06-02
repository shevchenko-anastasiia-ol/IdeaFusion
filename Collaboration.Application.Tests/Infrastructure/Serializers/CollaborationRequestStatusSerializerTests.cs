using Collaboration.Domain.Entities;
using Collaboration.Infrastructure.Serializer;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using Xunit;

namespace Collaboration.Application.Tests.Infrastructure.Serializers;

public class CollaborationRequestStatusSerializerTests
{
    private readonly CollaborationRequestStatusSerializer _serializer = new();

    // ── Serialize ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(CollaborationRequestStatus.Pending,   "Pending")]
    [InlineData(CollaborationRequestStatus.Accepted,  "Accepted")]
    [InlineData(CollaborationRequestStatus.Rejected,  "Rejected")]
    [InlineData(CollaborationRequestStatus.Cancelled, "Cancelled")]
    public void Serialize_WritesStatusAsString(CollaborationRequestStatus status, string expected)
    {
        var doc = new BsonDocument();
        using var writer = new BsonDocumentWriter(doc);

        writer.WriteStartDocument();
        writer.WriteName("v");
        _serializer.Serialize(BsonSerializationContext.CreateRoot(writer), new BsonSerializationArgs(), status);
        writer.WriteEndDocument();

        doc["v"].AsString.Should().Be(expected);
    }

    // ── Deserialize from String ───────────────────────────────────────────────

    [Theory]
    [InlineData("Pending",   CollaborationRequestStatus.Pending)]
    [InlineData("Accepted",  CollaborationRequestStatus.Accepted)]
    [InlineData("Rejected",  CollaborationRequestStatus.Rejected)]
    [InlineData("Cancelled", CollaborationRequestStatus.Cancelled)]
    public void Deserialize_FromString_ReturnsCorrectStatus(string raw, CollaborationRequestStatus expected)
    {
        var doc = new BsonDocument("v", raw);
        using var reader = new BsonDocumentReader(doc);
        reader.ReadStartDocument();
        reader.ReadBsonType();
        reader.ReadName();

        var result = _serializer.Deserialize(BsonDeserializationContext.CreateRoot(reader), new BsonDeserializationArgs());

        result.Should().Be(expected);
    }

    // ── Deserialize from Int32 ────────────────────────────────────────────────

    [Theory]
    [InlineData(0, CollaborationRequestStatus.Pending)]
    [InlineData(1, CollaborationRequestStatus.Accepted)]
    [InlineData(2, CollaborationRequestStatus.Rejected)]
    [InlineData(3, CollaborationRequestStatus.Cancelled)]
    public void Deserialize_FromInt32_ReturnsCorrectStatus(int raw, CollaborationRequestStatus expected)
    {
        var doc = new BsonDocument("v", new BsonInt32(raw));
        using var reader = new BsonDocumentReader(doc);
        reader.ReadStartDocument();
        reader.ReadBsonType();
        reader.ReadName();

        var result = _serializer.Deserialize(BsonDeserializationContext.CreateRoot(reader), new BsonDeserializationArgs());

        result.Should().Be(expected);
    }

    // ── Deserialize from Int64 ────────────────────────────────────────────────

    [Theory]
    [InlineData(0L, CollaborationRequestStatus.Pending)]
    [InlineData(1L, CollaborationRequestStatus.Accepted)]
    public void Deserialize_FromInt64_ReturnsCorrectStatus(long raw, CollaborationRequestStatus expected)
    {
        var doc = new BsonDocument("v", new BsonInt64(raw));
        using var reader = new BsonDocumentReader(doc);
        reader.ReadStartDocument();
        reader.ReadBsonType();
        reader.ReadName();

        var result = _serializer.Deserialize(BsonDeserializationContext.CreateRoot(reader), new BsonDeserializationArgs());

        result.Should().Be(expected);
    }

    // ── Round-trip ────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(CollaborationRequestStatus.Pending)]
    [InlineData(CollaborationRequestStatus.Accepted)]
    [InlineData(CollaborationRequestStatus.Rejected)]
    [InlineData(CollaborationRequestStatus.Cancelled)]
    public void Serialize_ThenDeserialize_ReturnsOriginalValue(CollaborationRequestStatus status)
    {
        var doc = new BsonDocument();
        using var writer = new BsonDocumentWriter(doc);
        writer.WriteStartDocument();
        writer.WriteName("v");
        _serializer.Serialize(BsonSerializationContext.CreateRoot(writer), new BsonSerializationArgs(), status);
        writer.WriteEndDocument();

        using var reader = new BsonDocumentReader(doc);
        reader.ReadStartDocument();
        reader.ReadBsonType();
        reader.ReadName();
        var result = _serializer.Deserialize(BsonDeserializationContext.CreateRoot(reader), new BsonDeserializationArgs());

        result.Should().Be(status);
    }
}