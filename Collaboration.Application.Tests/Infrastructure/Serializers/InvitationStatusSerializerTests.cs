using Collaboration.Domain.Entities;
using Collaboration.Infrastructure.Serializer;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using Xunit;

namespace Collaboration.Application.Tests.Infrastructure.Serializers;

public class InvitationStatusSerializerTests
{
    private readonly InvitationStatusSerializer _serializer = new();

    // ── Serialize ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(InvitationStatus.Pending,  "Pending")]
    [InlineData(InvitationStatus.Accepted, "Accepted")]
    [InlineData(InvitationStatus.Declined, "Declined")]
    [InlineData(InvitationStatus.Expired,  "Expired")]
    [InlineData(InvitationStatus.Revoked,  "Revoked")]
    public void Serialize_WritesStatusAsString(InvitationStatus status, string expected)
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
    [InlineData("Pending",  InvitationStatus.Pending)]
    [InlineData("Accepted", InvitationStatus.Accepted)]
    [InlineData("Declined", InvitationStatus.Declined)]
    [InlineData("Expired",  InvitationStatus.Expired)]
    [InlineData("Revoked",  InvitationStatus.Revoked)]
    public void Deserialize_FromString_ReturnsCorrectStatus(string raw, InvitationStatus expected)
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
    [InlineData(0, InvitationStatus.Pending)]
    [InlineData(1, InvitationStatus.Accepted)]
    [InlineData(2, InvitationStatus.Declined)]
    [InlineData(3, InvitationStatus.Expired)]
    [InlineData(4, InvitationStatus.Revoked)]
    public void Deserialize_FromInt32_ReturnsCorrectStatus(int raw, InvitationStatus expected)
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
    [InlineData(0L, InvitationStatus.Pending)]
    [InlineData(2L, InvitationStatus.Declined)]
    [InlineData(4L, InvitationStatus.Revoked)]
    public void Deserialize_FromInt64_ReturnsCorrectStatus(long raw, InvitationStatus expected)
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
    [InlineData(InvitationStatus.Pending)]
    [InlineData(InvitationStatus.Accepted)]
    [InlineData(InvitationStatus.Declined)]
    [InlineData(InvitationStatus.Expired)]
    [InlineData(InvitationStatus.Revoked)]
    public void Serialize_ThenDeserialize_ReturnsOriginalValue(InvitationStatus status)
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