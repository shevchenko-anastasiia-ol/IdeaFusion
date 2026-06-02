using Collaboration.Domain.Entities;
using Collaboration.Infrastructure.Serializer;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using Xunit;

namespace Collaboration.Application.Tests.Infrastructure.Serializers;

public class TeamStatusSerializerTests
{
    private readonly TeamStatusSerializer _serializer = new();

    // ── Serialize ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(TeamStatus.Active,    "Active")]
    [InlineData(TeamStatus.Searching, "Searching")]
    [InlineData(TeamStatus.Completed, "Completed")]
    public void Serialize_WritesStatusAsString(TeamStatus status, string expected)
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
    [InlineData("Active",    TeamStatus.Active)]
    [InlineData("Searching", TeamStatus.Searching)]
    [InlineData("Completed", TeamStatus.Completed)]
    public void Deserialize_FromString_ReturnsCorrectStatus(string raw, TeamStatus expected)
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
    [InlineData(0, TeamStatus.Active)]
    [InlineData(1, TeamStatus.Searching)]
    [InlineData(2, TeamStatus.Completed)]
    public void Deserialize_FromInt32_ReturnsCorrectStatus(int raw, TeamStatus expected)
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
    [InlineData(0L, TeamStatus.Active)]
    [InlineData(1L, TeamStatus.Searching)]
    [InlineData(2L, TeamStatus.Completed)]
    public void Deserialize_FromInt64_ReturnsCorrectStatus(long raw, TeamStatus expected)
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
    [InlineData(TeamStatus.Active)]
    [InlineData(TeamStatus.Searching)]
    [InlineData(TeamStatus.Completed)]
    public void Serialize_ThenDeserialize_ReturnsOriginalValue(TeamStatus status)
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