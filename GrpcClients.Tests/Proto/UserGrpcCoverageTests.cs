using FluentAssertions;
using Google.Protobuf;
using Grpc.Core;
using IdeaFusion.Grpc.Users;
using System.Buffers;
using System.Reflection;
using Xunit;

namespace GrpcClients.Tests.Proto;

/// <summary>
/// Covers branches in generated protobuf/gRPC code that are not reached by the basic
/// round-trip tests: self-equality, wrong-type equality, empty-message hashing/sizing,
/// MergeFrom edge cases, unknown-field tracking, BindService, and default base-class
/// method stubs.
/// </summary>

// ── GetUserByIdRequest ─────────────────────────────────────────────────────

public class GetUserByIdRequestCoverageTests
{
    [Fact]
    public void Equals_WithDifferentType_ReturnsFalse()
    {
        var req = new GetUserByIdRequest { UserId = "u-1" };
        req.Equals("not a request").Should().BeFalse();
    }

    [Fact]
    public void Equals_SameReference_ReturnsTrue()
    {
        var req = new GetUserByIdRequest { UserId = "u-1" };
        req.Equals((object)req).Should().BeTrue();
    }

    [Fact]
    public void Equals_Null_ReturnsFalse()
    {
        var req = new GetUserByIdRequest { UserId = "u-1" };
        req.Equals((GetUserByIdRequest?)null).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_EmptyMessage_ReturnsDeterministicValue()
    {
        var hash = new GetUserByIdRequest().GetHashCode();
        hash.Should().Be(new GetUserByIdRequest().GetHashCode());
    }

    [Fact]
    public void GetHashCode_NonEmptyMessage_ReturnsDifferentFromEmpty()
    {
        var empty    = new GetUserByIdRequest().GetHashCode();
        var nonEmpty = new GetUserByIdRequest { UserId = "u-1" }.GetHashCode();
        nonEmpty.Should().NotBe(empty);
    }

    [Fact]
    public void GetHashCode_WithUnknownFields_IncludesThemInHash()
    {
        // Parse a UserDto (more fields) as GetUserByIdRequest → extra fields become _unknownFields
        var withExtra = new UserDto { UserId = "u-1", UserName = "alice" };
        var req       = GetUserByIdRequest.Parser.ParseFrom(withExtra.ToByteArray());

        req.UserId.Should().Be("u-1");
        req.GetHashCode().Should().NotBe(0); // _unknownFields branch hit
    }

    [Fact]
    public void ToString_ReturnsNonNullString()
    {
        var req = new GetUserByIdRequest { UserId = "u-1" };
        req.ToString().Should().NotBeNull();
    }

    [Fact]
    public void CalculateSize_EmptyMessage_ReturnsZero()
    {
        new GetUserByIdRequest().CalculateSize().Should().Be(0);
    }

    [Fact]
    public void CalculateSize_WithUnknownFields_ReturnsPositive()
    {
        var withExtra = new UserDto { UserId = "u-1", UserName = "alice" };
        var req       = GetUserByIdRequest.Parser.ParseFrom(withExtra.ToByteArray());
        req.CalculateSize().Should().BeGreaterThan(0);
    }

    [Fact]
    public void WriteTo_EmptyMessage_WritesZeroBytes()
    {
        var req   = new GetUserByIdRequest();
        var bytes = req.ToByteArray();
        bytes.Should().BeEmpty();
    }

    [Fact]
    public void MergeFrom_Null_DoesNotThrow()
    {
        var target = new GetUserByIdRequest { UserId = "u-1" };
        target.MergeFrom((GetUserByIdRequest?)null!);
        target.UserId.Should().Be("u-1");
    }

    [Fact]
    public void MergeFrom_EmptySource_DoesNotOverwriteExistingField()
    {
        var target = new GetUserByIdRequest { UserId = "u-1" };
        target.MergeFrom(new GetUserByIdRequest());
        target.UserId.Should().Be("u-1");
    }

    [Fact]
    public void MergeFrom_NonEmptySource_CopiesField()
    {
        var target = new GetUserByIdRequest();
        target.MergeFrom(new GetUserByIdRequest { UserId = "u-2" });
        target.UserId.Should().Be("u-2");
    }

    [Fact]
    public void FieldNumber_UserIdFieldNumber_IsOne()
    {
        GetUserByIdRequest.UserIdFieldNumber.Should().Be(1);
    }

    private static GetUserByIdRequest WithUnknown() =>
        GetUserByIdRequest.Parser.ParseFrom(
            new UserDto { UserId = "u-1", UserName = "alice" }.ToByteArray());

    [Fact] public void WriteTo_WithUnknownFields_WritesBytes() =>
        WithUnknown().ToByteArray().Should().NotBeEmpty();
}

// ── GetUsersByIdsRequest ───────────────────────────────────────────────────

public class GetUsersByIdsRequestCoverageTests
{
    [Fact]
    public void Equals_WithDifferentType_ReturnsFalse()
    {
        var req = new GetUsersByIdsRequest();
        req.Equals(42).Should().BeFalse();
    }

    [Fact]
    public void Equals_SameReference_ReturnsTrue()
    {
        var req = new GetUsersByIdsRequest();
        req.UserIds.Add("u-1");
        req.Equals((object)req).Should().BeTrue();
    }

    [Fact]
    public void Equals_Null_ReturnsFalse()
    {
        var req = new GetUsersByIdsRequest();
        req.Equals((GetUsersByIdsRequest?)null).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_EmptyMessage_ReturnsDeterministicValue()
    {
        new GetUsersByIdsRequest().GetHashCode()
            .Should().Be(new GetUsersByIdsRequest().GetHashCode());
    }

    [Fact]
    public void ToString_ReturnsNonNullString()
    {
        new GetUsersByIdsRequest().ToString().Should().NotBeNull();
    }

    [Fact]
    public void CalculateSize_EmptyMessage_ReturnsZero()
    {
        new GetUsersByIdsRequest().CalculateSize().Should().Be(0);
    }

    [Fact]
    public void WriteTo_EmptyMessage_WritesZeroBytes()
    {
        new GetUsersByIdsRequest().ToByteArray().Should().BeEmpty();
    }

    [Fact]
    public void MergeFrom_Null_DoesNotThrow()
    {
        var target = new GetUsersByIdsRequest();
        target.UserIds.Add("u-1");
        target.MergeFrom((GetUsersByIdsRequest?)null!);
        target.UserIds.Should().HaveCount(1);
    }

    [Fact]
    public void MergeFrom_NonEmptySource_AppendsList()
    {
        var target = new GetUsersByIdsRequest();
        target.MergeFrom(new GetUsersByIdsRequest { UserIds = { "u-2", "u-3" } });
        target.UserIds.Should().Contain("u-2").And.Contain("u-3");
    }

    [Fact]
    public void FieldNumber_UserIdsFieldNumber_IsOne()
    {
        GetUsersByIdsRequest.UserIdsFieldNumber.Should().Be(1);
    }

    private static GetUsersByIdsRequest WithUnknown()
    {
        var bytes = new GetUsersByIdsRequest { UserIds = { "u-1" } }.ToByteArray().Concat(new byte[] { 18, 1, 65 }).ToArray();
        return GetUsersByIdsRequest.Parser.ParseFrom(bytes);
    }

    [Fact] public void WriteTo_WithUnknownFields_WritesBytes() =>
        WithUnknown().ToByteArray().Should().NotBeEmpty();

    [Fact] public void GetHashCode_WithItems_DifferentFromEmpty() =>
        new GetUsersByIdsRequest { UserIds = { "u-1" } }.GetHashCode()
            .Should().NotBe(new GetUsersByIdsRequest().GetHashCode());

    [Fact] public void GetHashCode_WithUnknownFields_Branches() =>
        WithUnknown().GetHashCode().Should().NotBe(0);

    [Fact] public void Equals_DifferentUserIds_ReturnsFalse()
    {
        var r1 = new GetUsersByIdsRequest { UserIds = { "u-1" } };
        var r2 = new GetUsersByIdsRequest { UserIds = { "u-2" } };
        r1.Should().NotBe(r2);
    }
}

// ── ValidateTokenRequest ───────────────────────────────────────────────────

public class ValidateTokenRequestCoverageTests
{
    [Fact]
    public void Equals_WithDifferentType_ReturnsFalse()
    {
        new ValidateTokenRequest { AccessToken = "t" }.Equals(new object()).Should().BeFalse();
    }

    [Fact]
    public void Equals_SameReference_ReturnsTrue()
    {
        var req = new ValidateTokenRequest { AccessToken = "t" };
        req.Equals((object)req).Should().BeTrue();
    }

    [Fact]
    public void Equals_Null_ReturnsFalse()
    {
        new ValidateTokenRequest().Equals((ValidateTokenRequest?)null).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_EmptyMessage_ReturnsDeterministicValue()
    {
        new ValidateTokenRequest().GetHashCode()
            .Should().Be(new ValidateTokenRequest().GetHashCode());
    }

    [Fact]
    public void GetHashCode_NonEmptyMessage_DifferentFromEmpty()
    {
        new ValidateTokenRequest { AccessToken = "tok" }.GetHashCode()
            .Should().NotBe(new ValidateTokenRequest().GetHashCode());
    }

    [Fact]
    public void ToString_ReturnsNonNullString()
    {
        new ValidateTokenRequest { AccessToken = "tok" }.ToString().Should().NotBeNull();
    }

    [Fact]
    public void CalculateSize_EmptyMessage_ReturnsZero()
    {
        new ValidateTokenRequest().CalculateSize().Should().Be(0);
    }

    [Fact]
    public void WriteTo_EmptyMessage_WritesZeroBytes()
    {
        new ValidateTokenRequest().ToByteArray().Should().BeEmpty();
    }

    [Fact]
    public void MergeFrom_Null_DoesNotThrow()
    {
        var target = new ValidateTokenRequest { AccessToken = "t" };
        target.MergeFrom((ValidateTokenRequest?)null!);
        target.AccessToken.Should().Be("t");
    }

    [Fact]
    public void MergeFrom_EmptySource_DoesNotOverwrite()
    {
        var target = new ValidateTokenRequest { AccessToken = "t" };
        target.MergeFrom(new ValidateTokenRequest());
        target.AccessToken.Should().Be("t");
    }

    [Fact]
    public void MergeFrom_NonEmptySource_CopiesField()
    {
        var target = new ValidateTokenRequest();
        target.MergeFrom(new ValidateTokenRequest { AccessToken = "new-token" });
        target.AccessToken.Should().Be("new-token");
    }

    [Fact]
    public void FieldNumber_AccessTokenFieldNumber_IsOne()
    {
        ValidateTokenRequest.AccessTokenFieldNumber.Should().Be(1);
    }

    private static ValidateTokenRequest WithUnknown()
    {
        var bytes = new ValidateTokenRequest { AccessToken = "tok" }.ToByteArray().Concat(new byte[] { 18, 1, 65 }).ToArray();
        return ValidateTokenRequest.Parser.ParseFrom(bytes);
    }

    [Fact] public void WriteTo_WithUnknownFields_WritesBytes() =>
        WithUnknown().ToByteArray().Should().NotBeEmpty();

    [Fact] public void GetHashCode_WithUnknownFields_Branches() =>
        WithUnknown().GetHashCode().Should().NotBe(0);
}

// ── CheckUserExistsRequest ─────────────────────────────────────────────────

public class CheckUserExistsRequestCoverageTests
{
    [Fact]
    public void Equals_WithDifferentType_ReturnsFalse()
    {
        new CheckUserExistsRequest { UserId = "u-1" }.Equals(123).Should().BeFalse();
    }

    [Fact]
    public void Equals_SameReference_ReturnsTrue()
    {
        var req = new CheckUserExistsRequest { UserId = "u-1" };
        req.Equals((object)req).Should().BeTrue();
    }

    [Fact]
    public void Equals_Null_ReturnsFalse()
    {
        new CheckUserExistsRequest().Equals((CheckUserExistsRequest?)null).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_EmptyAndNonEmpty_AreDifferent()
    {
        var empty    = new CheckUserExistsRequest().GetHashCode();
        var nonEmpty = new CheckUserExistsRequest { UserId = "u-1" }.GetHashCode();
        nonEmpty.Should().NotBe(empty);
    }

    [Fact]
    public void ToString_ReturnsNonNullString()
    {
        new CheckUserExistsRequest { UserId = "u-1" }.ToString().Should().NotBeNull();
    }

    [Fact]
    public void CalculateSize_EmptyMessage_ReturnsZero()
    {
        new CheckUserExistsRequest().CalculateSize().Should().Be(0);
    }

    [Fact]
    public void WriteTo_EmptyMessage_WritesZeroBytes()
    {
        new CheckUserExistsRequest().ToByteArray().Should().BeEmpty();
    }

    [Fact]
    public void MergeFrom_Null_DoesNotThrow()
    {
        var target = new CheckUserExistsRequest { UserId = "u-1" };
        target.MergeFrom((CheckUserExistsRequest?)null!);
        target.UserId.Should().Be("u-1");
    }

    [Fact]
    public void MergeFrom_EmptySource_DoesNotOverwrite()
    {
        var target = new CheckUserExistsRequest { UserId = "u-1" };
        target.MergeFrom(new CheckUserExistsRequest());
        target.UserId.Should().Be("u-1");
    }

    [Fact]
    public void MergeFrom_NonEmptySource_CopiesField()
    {
        var target = new CheckUserExistsRequest();
        target.MergeFrom(new CheckUserExistsRequest { UserId = "u-99" });
        target.UserId.Should().Be("u-99");
    }

    private static CheckUserExistsRequest WithUnknown()
    {
        var bytes = new CheckUserExistsRequest { UserId = "u-1" }.ToByteArray().Concat(new byte[] { 18, 1, 65 }).ToArray();
        return CheckUserExistsRequest.Parser.ParseFrom(bytes);
    }

    [Fact] public void WriteTo_WithUnknownFields_WritesBytes() =>
        WithUnknown().ToByteArray().Should().NotBeEmpty();

    [Fact] public void GetHashCode_WithUnknownFields_Branches() =>
        WithUnknown().GetHashCode().Should().NotBe(0);
}

// ── UserDto ────────────────────────────────────────────────────────────────

public class UserDtoCoverageTests
{
    [Fact]
    public void Equals_WithDifferentType_ReturnsFalse()
    {
        new UserDto { UserId = "u-1" }.Equals("string").Should().BeFalse();
    }

    [Fact]
    public void Equals_SameReference_ReturnsTrue()
    {
        var dto = new UserDto { UserId = "u-1" };
        dto.Equals((object)dto).Should().BeTrue();
    }

    [Fact]
    public void Equals_Null_ReturnsFalse()
    {
        new UserDto().Equals((UserDto?)null).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_EmptyMessage_ReturnsDeterministicValue()
    {
        new UserDto().GetHashCode().Should().Be(new UserDto().GetHashCode());
    }

    [Fact]
    public void GetHashCode_NonEmptyMessage_DifferentFromEmpty()
    {
        new UserDto { UserId = "u-1", Email = "a@b.com" }.GetHashCode()
            .Should().NotBe(new UserDto().GetHashCode());
    }

    [Fact]
    public void ToString_ReturnsNonNullString()
    {
        new UserDto { UserId = "u-1" }.ToString().Should().NotBeNull();
    }

    [Fact]
    public void CalculateSize_EmptyMessage_ReturnsZero()
    {
        new UserDto().CalculateSize().Should().Be(0);
    }

    [Fact]
    public void WriteTo_EmptyMessage_WritesZeroBytes()
    {
        new UserDto().ToByteArray().Should().BeEmpty();
    }

    [Fact]
    public void MergeFrom_Null_DoesNotThrow()
    {
        var target = new UserDto { UserId = "u-1" };
        target.MergeFrom((UserDto?)null!);
        target.UserId.Should().Be("u-1");
    }

    [Fact]
    public void MergeFrom_EmptySource_DoesNotOverwriteExistingFields()
    {
        var target = new UserDto { UserId = "u-1", Email = "a@b.com" };
        target.MergeFrom(new UserDto());
        target.UserId.Should().Be("u-1");
        target.Email.Should().Be("a@b.com");
    }

    [Fact]
    public void MergeFrom_NonEmptySource_CopiesPopulatedFields()
    {
        var target = new UserDto();
        target.MergeFrom(new UserDto { UserId = "u-2", Email = "b@c.com" });
        target.UserId.Should().Be("u-2");
        target.Email.Should().Be("b@c.com");
    }

    private static UserDto WithUnknown()
    {
        var bytes = new UserDto { UserId = "u-1", UserName = "alice", Email = "a@b.com", AvatarUrl = "url", CreatedAt = "2024-01-01" }
            .ToByteArray().Concat(new byte[] { 50, 1, 65 }).ToArray();
        return UserDto.Parser.ParseFrom(bytes);
    }

    [Fact] public void WriteTo_WithUnknownFields_WritesBytes() =>
        WithUnknown().ToByteArray().Should().NotBeEmpty();

    [Fact] public void MergeFrom_AllFields_CopiesAll()
    {
        var target = new UserDto();
        target.MergeFrom(new UserDto { UserId = "u-1", UserName = "alice", Email = "a@b.com", AvatarUrl = "url", CreatedAt = "2024-01-01" });
        target.UserName.Should().Be("alice");
        target.AvatarUrl.Should().Be("url");
        target.CreatedAt.Should().Be("2024-01-01");
    }

    [Fact] public void SerializeAndDeserialize_AllFields_CoversInternalMergePaths()
    {
        var dto = new UserDto { UserId = "u-1", UserName = "alice", Email = "a@b.com", AvatarUrl = "url", CreatedAt = "2024-01-01" };
        var parsed = UserDto.Parser.ParseFrom(dto.ToByteArray());
        parsed.UserName.Should().Be("alice");
        parsed.AvatarUrl.Should().Be("url");
        parsed.CreatedAt.Should().Be("2024-01-01");
    }

    [Fact] public void Equals_DifferentUserId_ReturnsFalse() =>
        new UserDto { UserId = "u-1" }.Equals(new UserDto { UserId = "u-2" }).Should().BeFalse();

    [Fact] public void Equals_DifferentUserName_ReturnsFalse() =>
        new UserDto { UserName = "alice" }.Equals(new UserDto { UserName = "bob" }).Should().BeFalse();

    [Fact] public void Equals_DifferentEmail_ReturnsFalse() =>
        new UserDto { Email = "a@b.com" }.Equals(new UserDto { Email = "c@d.com" }).Should().BeFalse();

    [Fact] public void Equals_DifferentAvatarUrl_ReturnsFalse() =>
        new UserDto { AvatarUrl = "url-a" }.Equals(new UserDto { AvatarUrl = "url-b" }).Should().BeFalse();

    [Fact] public void Equals_DifferentCreatedAt_ReturnsFalse() =>
        new UserDto { CreatedAt = "2024-01-01" }.Equals(new UserDto { CreatedAt = "2024-06-01" }).Should().BeFalse();

    [Fact] public void GetHashCode_WithUnknownFields_Branches() =>
        WithUnknown().GetHashCode().Should().NotBe(0);

    [Fact] public void GetHashCode_WithUserNameAvatarUrlCreatedAt_NonZero() =>
        new UserDto { UserName = "alice", AvatarUrl = "url", CreatedAt = "2024-01-01" }
            .GetHashCode().Should().NotBe(new UserDto().GetHashCode());
}

// ── UserResponse ───────────────────────────────────────────────────────────

public class UserResponseCoverageTests
{
    [Fact]
    public void Equals_WithDifferentType_ReturnsFalse()
    {
        new UserResponse().Equals("other").Should().BeFalse();
    }

    [Fact]
    public void Equals_SameReference_ReturnsTrue()
    {
        var resp = new UserResponse { User = new UserDto { UserId = "u-1" } };
        resp.Equals((object)resp).Should().BeTrue();
    }

    [Fact]
    public void Equals_Null_ReturnsFalse()
    {
        new UserResponse().Equals((UserResponse?)null).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_EmptyMessage_ReturnsDeterministicValue()
    {
        new UserResponse().GetHashCode().Should().Be(new UserResponse().GetHashCode());
    }

    [Fact]
    public void GetHashCode_NonNullUser_DifferentFromEmpty()
    {
        var withUser = new UserResponse { User = new UserDto { UserId = "u-1" } };
        withUser.GetHashCode().Should().NotBe(new UserResponse().GetHashCode());
    }

    [Fact]
    public void ToString_ReturnsNonNullString()
    {
        new UserResponse { User = new UserDto { UserId = "u-1" } }.ToString().Should().NotBeNull();
    }

    [Fact]
    public void CalculateSize_EmptyMessage_ReturnsZero()
    {
        new UserResponse().CalculateSize().Should().Be(0);
    }

    [Fact]
    public void WriteTo_EmptyMessage_WritesZeroBytes()
    {
        new UserResponse().ToByteArray().Should().BeEmpty();
    }

    [Fact]
    public void MergeFrom_Null_DoesNotThrow()
    {
        var resp = new UserResponse { User = new UserDto { UserId = "u-1" } };
        resp.MergeFrom((UserResponse?)null!);
        resp.User!.UserId.Should().Be("u-1");
    }

    [Fact]
    public void MergeFrom_NonEmptySource_MergesUser()
    {
        var target = new UserResponse();
        target.MergeFrom(new UserResponse { User = new UserDto { UserId = "u-2" } });
        target.User.Should().NotBeNull();
        target.User!.UserId.Should().Be("u-2");
    }

    private static UserResponse WithUnknown()
    {
        var bytes = new UserResponse { User = new UserDto { UserId = "u-1" } }.ToByteArray().Concat(new byte[] { 18, 1, 65 }).ToArray();
        return UserResponse.Parser.ParseFrom(bytes);
    }

    [Fact] public void WriteTo_WithUnknownFields_WritesBytes() =>
        WithUnknown().ToByteArray().Should().NotBeEmpty();

    [Fact] public void Clone_WithNullUser_ReturnsCloneWithNullUser()
    {
        var r     = new UserResponse();
        var clone = r.Clone();
        clone.User.Should().BeNull();
    }

    [Fact] public void Equals_DifferentUser_ReturnsFalse()
    {
        var r1 = new UserResponse { User = new UserDto { UserId = "u-1" } };
        var r2 = new UserResponse { User = new UserDto { UserId = "u-2" } };
        r1.Should().NotBe(r2);
    }

    [Fact] public void ParseFrom_DuplicateUserField_MergesIntoExisting()
    {
        var b1 = new UserResponse { User = new UserDto { UserId = "u-1" } }.ToByteArray();
        var b2 = new UserResponse { User = new UserDto { UserName = "alice" } }.ToByteArray();
        var parsed = UserResponse.Parser.ParseFrom(b1.Concat(b2).ToArray());
        parsed.User!.UserId.Should().Be("u-1");
        parsed.User.UserName.Should().Be("alice");
    }

    [Fact] public void GetHashCode_WithUnknownFields_Branches() =>
        WithUnknown().GetHashCode().Should().NotBe(0);

    [Fact] public void MergeFrom_EmptySource_SkipsNullUser()
    {
        var r = new UserResponse { User = new UserDto { UserId = "u-1" } };
        r.MergeFrom(new UserResponse()); // other.user_ == null → line 956 FALSE
        r.User!.UserId.Should().Be("u-1");
    }

    [Fact] public void MergeFrom_ExistingUser_DeepMerges()
    {
        var r = new UserResponse { User = new UserDto { UserId = "u-1" } };
        r.MergeFrom(new UserResponse { User = new UserDto { UserName = "alice" } }); // user_ not null → line 957 FALSE
        r.User!.UserId.Should().Be("u-1");
        r.User.UserName.Should().Be("alice");
    }
}

// ── UsersResponse ──────────────────────────────────────────────────────────

public class UsersResponseCoverageTests
{
    [Fact]
    public void Equals_WithDifferentType_ReturnsFalse()
    {
        new UsersResponse().Equals(42).Should().BeFalse();
    }

    [Fact]
    public void Equals_SameReference_ReturnsTrue()
    {
        var resp = new UsersResponse();
        resp.Users.Add(new UserDto { UserId = "u-1" });
        resp.Equals((object)resp).Should().BeTrue();
    }

    [Fact]
    public void Equals_Null_ReturnsFalse()
    {
        new UsersResponse().Equals((UsersResponse?)null).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_EmptyMessage_ReturnsDeterministicValue()
    {
        new UsersResponse().GetHashCode().Should().Be(new UsersResponse().GetHashCode());
    }

    [Fact]
    public void ToString_ReturnsNonNullString()
    {
        new UsersResponse().ToString().Should().NotBeNull();
    }

    [Fact]
    public void CalculateSize_EmptyMessage_ReturnsZero()
    {
        new UsersResponse().CalculateSize().Should().Be(0);
    }

    [Fact]
    public void WriteTo_EmptyMessage_WritesZeroBytes()
    {
        new UsersResponse().ToByteArray().Should().BeEmpty();
    }

    [Fact]
    public void MergeFrom_Null_DoesNotThrow()
    {
        var resp = new UsersResponse();
        resp.Users.Add(new UserDto { UserId = "u-1" });
        resp.MergeFrom((UsersResponse?)null!);
        resp.Users.Should().HaveCount(1);
    }

    [Fact]
    public void MergeFrom_NonEmptySource_AppendsList()
    {
        var target = new UsersResponse();
        target.MergeFrom(new UsersResponse { Users = { new UserDto { UserId = "u-2" } } });
        target.Users.Should().ContainSingle(u => u.UserId == "u-2");
    }

    private static UsersResponse WithUnknown()
    {
        var r = new UsersResponse { Users = { new UserDto { UserId = "u-1" } } };
        var bytes = r.ToByteArray().Concat(new byte[] { 18, 1, 65 }).ToArray();
        return UsersResponse.Parser.ParseFrom(bytes);
    }

    [Fact] public void WriteTo_WithUnknownFields_WritesBytes() =>
        WithUnknown().ToByteArray().Should().NotBeEmpty();

    [Fact] public void Equals_DifferentUsers_ReturnsFalse()
    {
        var r1 = new UsersResponse { Users = { new UserDto { UserId = "u-1" } } };
        var r2 = new UsersResponse { Users = { new UserDto { UserId = "u-2" } } };
        r1.Should().NotBe(r2);
    }

    [Fact] public void GetHashCode_WithUnknownFields_Branches() =>
        WithUnknown().GetHashCode().Should().NotBe(0);
}

// ── ValidateTokenResponse ──────────────────────────────────────────────────

public class ValidateTokenResponseCoverageTests
{
    [Fact]
    public void Equals_WithDifferentType_ReturnsFalse()
    {
        new ValidateTokenResponse { IsValid = true }.Equals(new object()).Should().BeFalse();
    }

    [Fact]
    public void Equals_SameReference_ReturnsTrue()
    {
        var resp = new ValidateTokenResponse { IsValid = true, UserId = "u-1" };
        resp.Equals((object)resp).Should().BeTrue();
    }

    [Fact]
    public void Equals_Null_ReturnsFalse()
    {
        new ValidateTokenResponse().Equals((ValidateTokenResponse?)null).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_EmptyMessage_ReturnsDeterministicValue()
    {
        new ValidateTokenResponse().GetHashCode()
            .Should().Be(new ValidateTokenResponse().GetHashCode());
    }

    [Fact]
    public void GetHashCode_ValidToken_DifferentFromEmpty()
    {
        var filled = new ValidateTokenResponse { IsValid = true, UserId = "u-1", Email = "a@b.com" };
        filled.GetHashCode().Should().NotBe(new ValidateTokenResponse().GetHashCode());
    }

    [Fact]
    public void ToString_ReturnsNonNullString()
    {
        new ValidateTokenResponse { IsValid = true }.ToString().Should().NotBeNull();
    }

    [Fact]
    public void CalculateSize_EmptyMessage_ReturnsZero()
    {
        new ValidateTokenResponse().CalculateSize().Should().Be(0);
    }

    [Fact]
    public void WriteTo_EmptyMessage_WritesZeroBytes()
    {
        new ValidateTokenResponse().ToByteArray().Should().BeEmpty();
    }

    [Fact]
    public void MergeFrom_Null_DoesNotThrow()
    {
        var resp = new ValidateTokenResponse { IsValid = true, UserId = "u-1" };
        resp.MergeFrom((ValidateTokenResponse?)null!);
        resp.UserId.Should().Be("u-1");
    }

    [Fact]
    public void MergeFrom_EmptySource_DoesNotOverwrite()
    {
        var target = new ValidateTokenResponse { IsValid = true, UserId = "u-1" };
        target.MergeFrom(new ValidateTokenResponse());
        target.UserId.Should().Be("u-1");
    }

    [Fact]
    public void MergeFrom_NonEmptySource_CopiesFields()
    {
        var target = new ValidateTokenResponse();
        target.MergeFrom(new ValidateTokenResponse { IsValid = true, UserId = "u-2", Email = "b@c.com" });
        target.IsValid.Should().BeTrue();
        target.UserId.Should().Be("u-2");
        target.Email.Should().Be("b@c.com");
    }

    private static ValidateTokenResponse WithUnknown()
    {
        var bytes = new ValidateTokenResponse { IsValid = true, UserId = "u-1", UserName = "alice", Email = "a@b.com" }
            .ToByteArray().Concat(new byte[] { 42, 1, 65 }).ToArray();
        return ValidateTokenResponse.Parser.ParseFrom(bytes);
    }

    [Fact] public void WriteTo_WithUnknownFields_WritesBytes() =>
        WithUnknown().ToByteArray().Should().NotBeEmpty();

    [Fact] public void Equals_DifferentIsValid_ReturnsFalse() =>
        new ValidateTokenResponse { IsValid = true }.Equals(new ValidateTokenResponse { IsValid = false }).Should().BeFalse();

    [Fact] public void Equals_DifferentUserId_ReturnsFalse() =>
        new ValidateTokenResponse { UserId = "u-1" }.Equals(new ValidateTokenResponse { UserId = "u-2" }).Should().BeFalse();

    [Fact] public void Equals_DifferentUserName_ReturnsFalse() =>
        new ValidateTokenResponse { UserName = "alice" }.Equals(new ValidateTokenResponse { UserName = "bob" }).Should().BeFalse();

    [Fact] public void Equals_DifferentEmail_ReturnsFalse() =>
        new ValidateTokenResponse { Email = "a@b.com" }.Equals(new ValidateTokenResponse { Email = "c@d.com" }).Should().BeFalse();

    [Fact] public void MergeFrom_WithUserName_CopiesUserName()
    {
        var t = new ValidateTokenResponse();
        t.MergeFrom(new ValidateTokenResponse { UserName = "alice" });
        t.UserName.Should().Be("alice");
    }

    [Fact] public void SerializeAndDeserialize_AllFields_CoversInternalMergePaths()
    {
        var dto = new ValidateTokenResponse { IsValid = true, UserId = "u-1", UserName = "alice", Email = "a@b.com" };
        var parsed = ValidateTokenResponse.Parser.ParseFrom(dto.ToByteArray());
        parsed.IsValid.Should().BeTrue();
        parsed.UserName.Should().Be("alice");
    }

    [Fact] public void GetHashCode_WithUnknownFields_Branches() =>
        WithUnknown().GetHashCode().Should().NotBe(0);

    [Fact] public void GetHashCode_WithUserName_NonZero() =>
        new ValidateTokenResponse { UserName = "alice" }.GetHashCode()
            .Should().NotBe(new ValidateTokenResponse().GetHashCode());
}

// ── CheckUserExistsResponse ────────────────────────────────────────────────

public class CheckUserExistsResponseCoverageTests
{
    [Fact]
    public void Equals_WithDifferentType_ReturnsFalse()
    {
        new CheckUserExistsResponse { Exists = true }.Equals("str").Should().BeFalse();
    }

    [Fact]
    public void Equals_SameReference_ReturnsTrue()
    {
        var resp = new CheckUserExistsResponse { Exists = true };
        resp.Equals((object)resp).Should().BeTrue();
    }

    [Fact]
    public void Equals_Null_ReturnsFalse()
    {
        new CheckUserExistsResponse().Equals((CheckUserExistsResponse?)null).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_ExistsFalse_DifferentFromExistsTrue()
    {
        var withFalse = new CheckUserExistsResponse { Exists = false }.GetHashCode();
        var withTrue  = new CheckUserExistsResponse { Exists = true  }.GetHashCode();
        withTrue.Should().NotBe(withFalse);
    }

    [Fact]
    public void ToString_ReturnsNonNullString()
    {
        new CheckUserExistsResponse { Exists = true }.ToString().Should().NotBeNull();
    }

    [Fact]
    public void CalculateSize_ExistsFalse_ReturnsZero()
    {
        new CheckUserExistsResponse { Exists = false }.CalculateSize().Should().Be(0);
    }

    [Fact]
    public void CalculateSize_ExistsTrue_ReturnsPositive()
    {
        new CheckUserExistsResponse { Exists = true }.CalculateSize().Should().BeGreaterThan(0);
    }

    [Fact]
    public void WriteTo_ExistsFalse_WritesZeroBytes()
    {
        new CheckUserExistsResponse { Exists = false }.ToByteArray().Should().BeEmpty();
    }

    [Fact]
    public void WriteTo_ExistsTrue_WritesBytes()
    {
        new CheckUserExistsResponse { Exists = true }.ToByteArray().Should().NotBeEmpty();
    }

    [Fact]
    public void MergeFrom_Null_DoesNotThrow()
    {
        var resp = new CheckUserExistsResponse { Exists = true };
        resp.MergeFrom((CheckUserExistsResponse?)null!);
        resp.Exists.Should().BeTrue();
    }

    [Fact]
    public void MergeFrom_ExistsTrue_SetsExists()
    {
        var target = new CheckUserExistsResponse { Exists = false };
        target.MergeFrom(new CheckUserExistsResponse { Exists = true });
        target.Exists.Should().BeTrue();
    }

    private static CheckUserExistsResponse WithUnknown()
    {
        var bytes = new CheckUserExistsResponse { Exists = true }.ToByteArray().Concat(new byte[] { 18, 1, 65 }).ToArray();
        return CheckUserExistsResponse.Parser.ParseFrom(bytes);
    }

    [Fact] public void WriteTo_WithUnknownFields_WritesBytes() =>
        WithUnknown().ToByteArray().Should().NotBeEmpty();

    [Fact] public void Equals_DifferentExists_ReturnsFalse() =>
        new CheckUserExistsResponse { Exists = true }.Equals(new CheckUserExistsResponse { Exists = false }).Should().BeFalse();

    [Fact] public void GetHashCode_WithUnknownFields_Branches() =>
        WithUnknown().GetHashCode().Should().NotBe(0);

    [Fact] public void MergeFrom_FalseExists_DoesNotSetExists()
    {
        var t = new CheckUserExistsResponse { Exists = true };
        t.MergeFrom(new CheckUserExistsResponse { Exists = false }); // FALSE branch at 1635
        t.Exists.Should().BeTrue();
    }
}

// ── UserGrpcService: BindService + UserGrpcServiceBase ────────────────────

public class UserGrpcServiceBindingTests
{
    private sealed class StubImpl : UserGrpcService.UserGrpcServiceBase { }

    // ServiceBinderBase.AddMethod throws NotImplementedException by default in Grpc.Core;
    // subclass it to allow BindService(ServiceBinderBase, ...) calls to succeed.
    private sealed class NoOpServiceBinder : ServiceBinderBase
    {
        public int MethodsAdded { get; private set; }

        public override void AddMethod<TRequest, TResponse>(
            Method<TRequest, TResponse> method,
            UnaryServerMethod<TRequest, TResponse>? handler)
            where TRequest : class
            where TResponse : class
        {
            MethodsAdded++;
        }
    }

    [Fact]
    public void BindService_WithImpl_ReturnsNonNullDefinition()
    {
        var definition = UserGrpcService.BindService(new StubImpl());
        definition.Should().NotBeNull();
    }

    [Fact]
    public void BindService_Binder_WithNonNullImpl_BindsAllFourMethods()
    {
        var binder = new NoOpServiceBinder();
        UserGrpcService.BindService(binder, new StubImpl());
        binder.MethodsAdded.Should().Be(4);
    }

    [Fact]
    public void BindService_Binder_WithNullImpl_BindsNullHandlers()
    {
        var binder = new NoOpServiceBinder();
        UserGrpcService.BindService(binder, null!);
        binder.MethodsAdded.Should().Be(4);
    }

    [Fact]
    public async Task UserGrpcServiceBase_GetUserById_ThrowsRpcExceptionUnimplemented()
    {
        var stub = new StubImpl();
        var act  = async () => await stub.GetUserById(new GetUserByIdRequest(), null!);
        await act.Should().ThrowAsync<RpcException>()
            .Where(e => e.StatusCode == StatusCode.Unimplemented);
    }

    [Fact]
    public async Task UserGrpcServiceBase_GetUsersByIds_ThrowsRpcExceptionUnimplemented()
    {
        var stub = new StubImpl();
        var act  = async () => await stub.GetUsersByIds(new GetUsersByIdsRequest(), null!);
        await act.Should().ThrowAsync<RpcException>()
            .Where(e => e.StatusCode == StatusCode.Unimplemented);
    }

    [Fact]
    public async Task UserGrpcServiceBase_ValidateToken_ThrowsRpcExceptionUnimplemented()
    {
        var stub = new StubImpl();
        var act  = async () => await stub.ValidateToken(new ValidateTokenRequest(), null!);
        await act.Should().ThrowAsync<RpcException>()
            .Where(e => e.StatusCode == StatusCode.Unimplemented);
    }

    [Fact]
    public async Task UserGrpcServiceBase_CheckUserExists_ThrowsRpcExceptionUnimplemented()
    {
        var stub = new StubImpl();
        var act  = async () => await stub.CheckUserExists(new CheckUserExistsRequest(), null!);
        await act.Should().ThrowAsync<RpcException>()
            .Where(e => e.StatusCode == StatusCode.Unimplemented);
    }

    // ── __Helper_SerializeMessage / __Helper_DeserializeMessage coverage ───

    private sealed class FakeUserSerCtx : SerializationContext
    {
        private readonly ArrayBufferWriter<byte> _buf = new();
        public byte[] Written { get; private set; } = Array.Empty<byte>();
        public override void SetPayloadLength(int len) { }
        public override IBufferWriter<byte> GetBufferWriter() => _buf;
        public override void Complete() => Written = _buf.WrittenSpan.ToArray();
        public override void Complete(byte[] payload) => Written = payload;
    }

    private sealed class FakeUserDeserCtx : DeserializationContext
    {
        private readonly byte[] _data;
        public FakeUserDeserCtx(byte[] data) { _data = data; }
        public override int PayloadLength => _data.Length;
        public override ReadOnlySequence<byte> PayloadAsReadOnlySequence() => new ReadOnlySequence<byte>(_data);
        public override byte[] PayloadAsNewBuffer() => (byte[])_data.Clone();
    }

    private sealed class PlainUserMsg : Google.Protobuf.IMessage<PlainUserMsg>
    {
        public static readonly Google.Protobuf.MessageParser<PlainUserMsg> Parser =
            new Google.Protobuf.MessageParser<PlainUserMsg>(() => new PlainUserMsg());
        public Google.Protobuf.Reflection.MessageDescriptor Descriptor => null!;
        public int CalculateSize() => 0;
        public void MergeFrom(Google.Protobuf.CodedInputStream input) { }
        public void WriteTo(Google.Protobuf.CodedOutputStream output) { }
        public void MergeFrom(PlainUserMsg message) { }
        public PlainUserMsg Clone() => new();
        public bool Equals(PlainUserMsg? other) => other != null;
    }

    [Fact]
    public void Helper_SerializeMessage_IBufferMessage_CoversBufferPath()
    {
        var method = typeof(UserGrpcService)
            .GetMethod("__Helper_SerializeMessage", BindingFlags.NonPublic | BindingFlags.Static)!;
        var ctx = new FakeUserSerCtx();
        method.Invoke(null, new object[] { new GetUserByIdRequest { UserId = "u-1" }, ctx });
        ctx.Written.Should().NotBeEmpty();
    }

    [Fact]
    public void Helper_SerializeMessage_NonIBufferMessage_CoversFallbackPath()
    {
        var method = typeof(UserGrpcService)
            .GetMethod("__Helper_SerializeMessage", BindingFlags.NonPublic | BindingFlags.Static)!;
        var ctx = new FakeUserSerCtx();
        method.Invoke(null, new object[] { new PlainUserMsg(), ctx });
        ctx.Written.Should().BeEmpty();
    }

    [Fact]
    public void Helper_DeserializeMessage_IBufferMessage_CoversBufferPath()
    {
        var bytes  = new GetUserByIdRequest { UserId = "u-1" }.ToByteArray();
        var method = typeof(UserGrpcService)
            .GetMethod("__Helper_DeserializeMessage", BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(typeof(GetUserByIdRequest));
        var result = (GetUserByIdRequest)method.Invoke(
            null, new object[] { new FakeUserDeserCtx(bytes), GetUserByIdRequest.Parser })!;
        result.UserId.Should().Be("u-1");
    }

    [Fact]
    public void Helper_DeserializeMessage_NonIBufferMessage_CoversFallbackPath()
    {
        var method = typeof(UserGrpcService)
            .GetMethod("__Helper_DeserializeMessage", BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(typeof(PlainUserMsg));
        var result = (PlainUserMsg)method.Invoke(
            null, new object[] { new FakeUserDeserCtx(Array.Empty<byte>()), PlainUserMsg.Parser })!;
        result.Should().NotBeNull();
    }
}