using Collaboration.Domain.ValueOfObjects;
using Collaboration.Infrastructure.Serializers;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using Xunit;

namespace Collaboration.Application.Tests.Infrastructure.Serializers;

public class UserSnapshotSerializerTests
{
    private readonly UserSnapshotSerializer _serializer = new();

    // ── Serialize ─────────────────────────────────────────────────────────────

    [Fact]
    public void Serialize_WithAllFields_WritesUserIdUsernameAndAvatarUrl()
    {
        var snapshot = new UserSnapshot("user-1", "alice", "https://cdn.example.com/alice.jpg");
        var doc      = Serialize(snapshot);

        doc["userId"].AsString.Should().Be("user-1");
        doc["username"].AsString.Should().Be("alice");
        doc["avatarUrl"].AsString.Should().Be("https://cdn.example.com/alice.jpg");
    }

    [Fact]
    public void Serialize_WithNullAvatarUrl_DoesNotWriteAvatarUrlKey()
    {
        var snapshot = new UserSnapshot("user-2", "bob", null);
        var doc      = Serialize(snapshot);

        doc["userId"].AsString.Should().Be("user-2");
        doc["username"].AsString.Should().Be("bob");
        doc.Contains("avatarUrl").Should().BeFalse();
    }

    [Fact]
    public void Serialize_PreservesUserIdAndUsername()
    {
        var snapshot = new UserSnapshot("abc-123", "charlie_42");
        var doc      = Serialize(snapshot);

        doc["userId"].AsString.Should().Be("abc-123");
        doc["username"].AsString.Should().Be("charlie_42");
    }

    // ── Deserialize ───────────────────────────────────────────────────────────

    [Fact]
    public void Deserialize_WithAllFields_ReturnsCorrectSnapshot()
    {
        var raw = new BsonDocument
        {
            { "userId",    "user-10" },
            { "username",  "diana" },
            { "avatarUrl", "https://cdn.example.com/diana.png" }
        };

        var result = Deserialize(raw);

        result.UserId.Should().Be("user-10");
        result.Username.Should().Be("diana");
        result.AvatarUrl.Should().Be("https://cdn.example.com/diana.png");
    }

    [Fact]
    public void Deserialize_WithNullAvatarUrl_ReturnsSnapshotWithNullAvatar()
    {
        var raw = new BsonDocument
        {
            { "userId",    "user-11" },
            { "username",  "eve" },
            { "avatarUrl", BsonNull.Value }
        };

        var result = Deserialize(raw);

        result.UserId.Should().Be("user-11");
        result.Username.Should().Be("eve");
        result.AvatarUrl.Should().BeNull();
    }

    [Fact]
    public void Deserialize_WithMissingAvatarUrl_ReturnsSnapshotWithNullAvatar()
    {
        var raw = new BsonDocument
        {
            { "userId",   "user-12" },
            { "username", "frank" }
        };

        var result = Deserialize(raw);

        result.UserId.Should().Be("user-12");
        result.Username.Should().Be("frank");
        result.AvatarUrl.Should().BeNull();
    }

    [Fact]
    public void Deserialize_WithUnknownFields_SkipsThemAndReturnsCorrectSnapshot()
    {
        var raw = new BsonDocument
        {
            { "userId",       "user-13" },
            { "username",     "grace" },
            { "unknownField", "ignored-value" }
        };

        var result = Deserialize(raw);

        result.UserId.Should().Be("user-13");
        result.Username.Should().Be("grace");
    }

    // ── Round-trip ────────────────────────────────────────────────────────────

    [Fact]
    public void Serialize_ThenDeserialize_ReturnsEquivalentSnapshot()
    {
        var original = new UserSnapshot("rt-user", "roundtrip_user", "https://cdn.example.com/rt.jpg");
        var doc      = Serialize(original);
        var result   = Deserialize(doc);

        result.UserId.Should().Be(original.UserId);
        result.Username.Should().Be(original.Username);
        result.AvatarUrl.Should().Be(original.AvatarUrl);
    }

    [Fact]
    public void Serialize_ThenDeserialize_WithNullAvatar_ReturnsEquivalentSnapshot()
    {
        var original = new UserSnapshot("rt-user-2", "no_avatar_user");
        var doc      = Serialize(original);
        var result   = Deserialize(doc);

        result.UserId.Should().Be(original.UserId);
        result.Username.Should().Be(original.Username);
        result.AvatarUrl.Should().BeNull();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private BsonDocument Serialize(UserSnapshot snapshot)
    {
        var doc = new BsonDocument();
        using var writer = new BsonDocumentWriter(doc);
        _serializer.Serialize(BsonSerializationContext.CreateRoot(writer), new BsonSerializationArgs(), snapshot);
        return doc;
    }

    private UserSnapshot Deserialize(BsonDocument raw)
    {
        using var reader = new BsonDocumentReader(raw);
        return _serializer.Deserialize(BsonDeserializationContext.CreateRoot(reader), new BsonDeserializationArgs());
    }
}