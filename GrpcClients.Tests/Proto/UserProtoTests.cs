using FluentAssertions;
using Google.Protobuf;
using IdeaFusion.Grpc.Users;
using Xunit;

namespace GrpcClients.Tests.Proto;

public class GetUserByIdRequestTests
{
    [Fact]
    public void Default_UserIdIsEmpty()
    {
        var req = new GetUserByIdRequest();
        req.UserId.Should().BeEmpty();
    }

    [Fact]
    public void WithUserId_ReturnsAssignedValue()
    {
        var req = new GetUserByIdRequest { UserId = "user-42" };
        req.UserId.Should().Be("user-42");
    }

    [Fact]
    public void SerializeAndDeserialize_PreservesUserId()
    {
        var req          = new GetUserByIdRequest { UserId = "user-abc" };
        var deserialized = GetUserByIdRequest.Parser.ParseFrom(req.ToByteArray());

        deserialized.UserId.Should().Be("user-abc");
    }

    [Fact]
    public void Equality_SameUserIdIsEqual()
    {
        var r1 = new GetUserByIdRequest { UserId = "u-1" };
        var r2 = new GetUserByIdRequest { UserId = "u-1" };

        r1.Should().Be(r2);
    }

    [Fact]
    public void Equality_DifferentUserIdsAreNotEqual()
    {
        var r1 = new GetUserByIdRequest { UserId = "u-1" };
        var r2 = new GetUserByIdRequest { UserId = "u-2" };

        r1.Should().NotBe(r2);
    }

    [Fact]
    public void Clone_CreatesEqualButDistinctInstance()
    {
        var original = new GetUserByIdRequest { UserId = "user-xyz" };
        var clone    = original.Clone();

        clone.Should().Be(original);
        clone.Should().NotBeSameAs(original);
    }
}

public class GetUsersByIdsRequestTests
{
    [Fact]
    public void Default_UserIdsIsEmpty()
    {
        var req = new GetUsersByIdsRequest();
        req.UserIds.Should().BeEmpty();
    }

    [Fact]
    public void AddUserIds_CountMatchesAddedIds()
    {
        var req = new GetUsersByIdsRequest();
        req.UserIds.Add("u-1");
        req.UserIds.Add("u-2");
        req.UserIds.Add("u-3");

        req.UserIds.Should().HaveCount(3);
    }

    [Fact]
    public void AddRange_PreservesInsertionOrder()
    {
        var req = new GetUsersByIdsRequest();
        req.UserIds.AddRange(new[] { "u-1", "u-2", "u-3" });

        req.UserIds[0].Should().Be("u-1");
        req.UserIds[1].Should().Be("u-2");
        req.UserIds[2].Should().Be("u-3");
    }

    [Fact]
    public void SerializeAndDeserialize_PreservesAllIds()
    {
        var original = new GetUsersByIdsRequest();
        original.UserIds.AddRange(new[] { "user-a", "user-b" });

        var deserialized = GetUsersByIdsRequest.Parser.ParseFrom(original.ToByteArray());

        deserialized.UserIds.Should().HaveCount(2);
        deserialized.UserIds.Should().Contain("user-a");
        deserialized.UserIds.Should().Contain("user-b");
    }

    [Fact]
    public void SerializeAndDeserialize_EmptyRequest_RemainsEmpty()
    {
        var original     = new GetUsersByIdsRequest();
        var deserialized = GetUsersByIdsRequest.Parser.ParseFrom(original.ToByteArray());

        deserialized.UserIds.Should().BeEmpty();
    }

    [Fact]
    public void Clone_CreatesEqualButDistinctInstance()
    {
        var original = new GetUsersByIdsRequest();
        original.UserIds.AddRange(new[] { "u-1", "u-2" });

        var clone = original.Clone();

        clone.Should().Be(original);
        clone.Should().NotBeSameAs(original);
    }
}

public class ValidateTokenRequestTests
{
    [Fact]
    public void Default_AccessTokenIsEmpty()
    {
        var req = new ValidateTokenRequest();
        req.AccessToken.Should().BeEmpty();
    }

    [Fact]
    public void WithAccessToken_ReturnsAssignedValue()
    {
        var token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9";
        var req   = new ValidateTokenRequest { AccessToken = token };

        req.AccessToken.Should().Be(token);
    }

    [Fact]
    public void SerializeAndDeserialize_PreservesAccessToken()
    {
        var token        = "test-jwt-token-value";
        var req          = new ValidateTokenRequest { AccessToken = token };
        var deserialized = ValidateTokenRequest.Parser.ParseFrom(req.ToByteArray());

        deserialized.AccessToken.Should().Be(token);
    }

    [Fact]
    public void Equality_SameTokenIsEqual()
    {
        var r1 = new ValidateTokenRequest { AccessToken = "token-abc" };
        var r2 = new ValidateTokenRequest { AccessToken = "token-abc" };

        r1.Should().Be(r2);
    }

    [Fact]
    public void Equality_DifferentTokensAreNotEqual()
    {
        var r1 = new ValidateTokenRequest { AccessToken = "token-1" };
        var r2 = new ValidateTokenRequest { AccessToken = "token-2" };

        r1.Should().NotBe(r2);
    }

    [Fact]
    public void Clone_CreatesEqualButDistinctInstance()
    {
        var original = new ValidateTokenRequest { AccessToken = "bearer-xyz" };
        var clone    = original.Clone();

        clone.Should().Be(original);
        clone.Should().NotBeSameAs(original);
    }
}

public class CheckUserExistsRequestTests
{
    [Fact]
    public void Default_UserIdIsEmpty()
    {
        var req = new CheckUserExistsRequest();
        req.UserId.Should().BeEmpty();
    }

    [Fact]
    public void WithUserId_ReturnsAssignedValue()
    {
        var req = new CheckUserExistsRequest { UserId = "user-99" };
        req.UserId.Should().Be("user-99");
    }

    [Fact]
    public void SerializeAndDeserialize_PreservesUserId()
    {
        var original     = new CheckUserExistsRequest { UserId = "user-check" };
        var deserialized = CheckUserExistsRequest.Parser.ParseFrom(original.ToByteArray());

        deserialized.UserId.Should().Be("user-check");
    }

    [Fact]
    public void Equality_SameUserIdIsEqual()
    {
        var r1 = new CheckUserExistsRequest { UserId = "u-1" };
        var r2 = new CheckUserExistsRequest { UserId = "u-1" };

        r1.Should().Be(r2);
    }

    [Fact]
    public void Equality_DifferentUserIdsAreNotEqual()
    {
        var r1 = new CheckUserExistsRequest { UserId = "u-1" };
        var r2 = new CheckUserExistsRequest { UserId = "u-2" };

        r1.Should().NotBe(r2);
    }

    [Fact]
    public void Clone_CreatesEqualButDistinctInstance()
    {
        var original = new CheckUserExistsRequest { UserId = "user-abc" };
        var clone    = original.Clone();

        clone.Should().Be(original);
        clone.Should().NotBeSameAs(original);
    }
}

public class UserDtoTests
{
    [Fact]
    public void Default_AllFieldsAreEmpty()
    {
        var dto = new UserDto();

        dto.UserId.Should().BeEmpty();
        dto.UserName.Should().BeEmpty();
        dto.Email.Should().BeEmpty();
        dto.AvatarUrl.Should().BeEmpty();
        dto.CreatedAt.Should().BeEmpty();
    }

    [Fact]
    public void Properties_CanBeSetAndRetrieved()
    {
        var dto = new UserDto
        {
            UserId    = "user-1",
            UserName  = "alice",
            Email     = "alice@example.com",
            AvatarUrl = "https://example.com/alice.jpg",
            CreatedAt = "2024-01-01T00:00:00Z"
        };

        dto.UserId.Should().Be("user-1");
        dto.UserName.Should().Be("alice");
        dto.Email.Should().Be("alice@example.com");
        dto.AvatarUrl.Should().Be("https://example.com/alice.jpg");
        dto.CreatedAt.Should().Be("2024-01-01T00:00:00Z");
    }

    [Fact]
    public void SerializeAndDeserialize_PreservesAllFields()
    {
        var original = new UserDto
        {
            UserId    = "u-42",
            UserName  = "bob",
            Email     = "bob@example.com",
            AvatarUrl = "https://cdn.example.com/bob.png",
            CreatedAt = "2024-03-15T08:00:00Z"
        };

        var deserialized = UserDto.Parser.ParseFrom(original.ToByteArray());

        deserialized.UserId.Should().Be("u-42");
        deserialized.UserName.Should().Be("bob");
        deserialized.Email.Should().Be("bob@example.com");
        deserialized.AvatarUrl.Should().Be("https://cdn.example.com/bob.png");
        deserialized.CreatedAt.Should().Be("2024-03-15T08:00:00Z");
    }

    [Fact]
    public void SerializeAndDeserialize_EmptyDto_RemainsEmpty()
    {
        var original     = new UserDto();
        var deserialized = UserDto.Parser.ParseFrom(original.ToByteArray());

        deserialized.UserId.Should().BeEmpty();
        deserialized.Email.Should().BeEmpty();
    }

    [Fact]
    public void Equality_SameValuesAreEqual()
    {
        var d1 = new UserDto { UserId = "u-1", UserName = "carol", Email = "carol@example.com" };
        var d2 = new UserDto { UserId = "u-1", UserName = "carol", Email = "carol@example.com" };

        d1.Should().Be(d2);
    }

    [Fact]
    public void Equality_DifferentEmailsAreNotEqual()
    {
        var d1 = new UserDto { UserId = "u-1", Email = "a@example.com" };
        var d2 = new UserDto { UserId = "u-1", Email = "b@example.com" };

        d1.Should().NotBe(d2);
    }

    [Fact]
    public void Clone_CreatesEqualButDistinctInstance()
    {
        var original = new UserDto
        {
            UserId = "u-1", UserName = "dave", Email = "dave@example.com", AvatarUrl = "url"
        };
        var clone = original.Clone();

        clone.Should().Be(original);
        clone.Should().NotBeSameAs(original);
    }
}

public class UserResponseTests
{
    [Fact]
    public void Default_UserIsNull()
    {
        var response = new UserResponse();
        response.User.Should().BeNull();
    }

    [Fact]
    public void WithUser_ReturnsAssignedDto()
    {
        var dto      = new UserDto { UserId = "u-1", UserName = "alice" };
        var response = new UserResponse { User = dto };

        response.User.Should().NotBeNull();
        response.User!.UserId.Should().Be("u-1");
        response.User.UserName.Should().Be("alice");
    }

    [Fact]
    public void SerializeAndDeserialize_PreservesNestedDto()
    {
        var response = new UserResponse
        {
            User = new UserDto
            {
                UserId   = "u-xyz",
                UserName = "test",
                Email    = "test@example.com"
            }
        };

        var deserialized = UserResponse.Parser.ParseFrom(response.ToByteArray());

        deserialized.User.Should().NotBeNull();
        deserialized.User!.UserId.Should().Be("u-xyz");
        deserialized.User.UserName.Should().Be("test");
        deserialized.User.Email.Should().Be("test@example.com");
    }

    [Fact]
    public void SerializeAndDeserialize_NullUser_ReturnsNullUser()
    {
        var response     = new UserResponse();
        var deserialized = UserResponse.Parser.ParseFrom(response.ToByteArray());

        deserialized.User.Should().BeNull();
    }

    [Fact]
    public void Equality_SameUserIsEqual()
    {
        var dto = new UserDto { UserId = "u-1" };
        var r1  = new UserResponse { User = dto };
        var r2  = new UserResponse { User = dto.Clone() };

        r1.Should().Be(r2);
    }

    [Fact]
    public void Clone_CreatesEqualButDistinctInstance()
    {
        var original = new UserResponse
        {
            User = new UserDto { UserId = "u-1", Email = "x@example.com" }
        };
        var clone = original.Clone();

        clone.Should().Be(original);
        clone.Should().NotBeSameAs(original);
    }
}

public class UsersResponseTests
{
    [Fact]
    public void Default_UsersIsEmpty()
    {
        var response = new UsersResponse();
        response.Users.Should().BeEmpty();
    }

    [Fact]
    public void AddUsers_CountMatchesAddedUsers()
    {
        var response = new UsersResponse();
        response.Users.Add(new UserDto { UserId = "u-1" });
        response.Users.Add(new UserDto { UserId = "u-2" });

        response.Users.Should().HaveCount(2);
    }

    [Fact]
    public void AddRange_PreservesInsertionOrder()
    {
        var response = new UsersResponse();
        response.Users.AddRange(new[]
        {
            new UserDto { UserId = "u-1" },
            new UserDto { UserId = "u-2" },
            new UserDto { UserId = "u-3" }
        });

        response.Users[0].UserId.Should().Be("u-1");
        response.Users[1].UserId.Should().Be("u-2");
        response.Users[2].UserId.Should().Be("u-3");
    }

    [Fact]
    public void SerializeAndDeserialize_PreservesAllUsers()
    {
        var response = new UsersResponse();
        response.Users.Add(new UserDto { UserId = "u-1", UserName = "alice" });
        response.Users.Add(new UserDto { UserId = "u-2", UserName = "bob" });

        var deserialized = UsersResponse.Parser.ParseFrom(response.ToByteArray());

        deserialized.Users.Should().HaveCount(2);
        deserialized.Users[0].UserId.Should().Be("u-1");
        deserialized.Users[0].UserName.Should().Be("alice");
        deserialized.Users[1].UserId.Should().Be("u-2");
    }

    [Fact]
    public void SerializeAndDeserialize_EmptyResponse_RemainsEmpty()
    {
        var response     = new UsersResponse();
        var deserialized = UsersResponse.Parser.ParseFrom(response.ToByteArray());

        deserialized.Users.Should().BeEmpty();
    }

    [Fact]
    public void Clone_CreatesEqualButDistinctInstance()
    {
        var response = new UsersResponse();
        response.Users.Add(new UserDto { UserId = "u-1" });
        response.Users.Add(new UserDto { UserId = "u-2" });

        var clone = response.Clone();

        clone.Should().Be(response);
        clone.Should().NotBeSameAs(response);
    }
}

public class ValidateTokenResponseTests
{
    [Fact]
    public void Default_IsValidIsFalseAndStringsAreEmpty()
    {
        var resp = new ValidateTokenResponse();

        resp.IsValid.Should().BeFalse();
        resp.UserId.Should().BeEmpty();
        resp.UserName.Should().BeEmpty();
        resp.Email.Should().BeEmpty();
    }

    [Fact]
    public void ValidToken_AllFieldsAreSet()
    {
        var resp = new ValidateTokenResponse
        {
            IsValid  = true,
            UserId   = "user-1",
            UserName = "alice",
            Email    = "alice@example.com"
        };

        resp.IsValid.Should().BeTrue();
        resp.UserId.Should().Be("user-1");
        resp.UserName.Should().Be("alice");
        resp.Email.Should().Be("alice@example.com");
    }

    [Fact]
    public void InvalidToken_IsValidIsFalseAndFieldsAreEmpty()
    {
        var resp = new ValidateTokenResponse { IsValid = false };

        resp.IsValid.Should().BeFalse();
        resp.UserId.Should().BeEmpty();
    }

    [Fact]
    public void SerializeAndDeserialize_ValidToken_PreservesAllFields()
    {
        var original = new ValidateTokenResponse
        {
            IsValid  = true,
            UserId   = "u-99",
            UserName = "bob",
            Email    = "bob@example.com"
        };

        var deserialized = ValidateTokenResponse.Parser.ParseFrom(original.ToByteArray());

        deserialized.IsValid.Should().BeTrue();
        deserialized.UserId.Should().Be("u-99");
        deserialized.UserName.Should().Be("bob");
        deserialized.Email.Should().Be("bob@example.com");
    }

    [Fact]
    public void SerializeAndDeserialize_InvalidToken_PreservesIsValid()
    {
        var original     = new ValidateTokenResponse { IsValid = false };
        var deserialized = ValidateTokenResponse.Parser.ParseFrom(original.ToByteArray());

        deserialized.IsValid.Should().BeFalse();
        deserialized.UserId.Should().BeEmpty();
    }

    [Fact]
    public void Equality_SameValuesAreEqual()
    {
        var r1 = new ValidateTokenResponse { IsValid = true, UserId = "u-1", Email = "a@b.com" };
        var r2 = new ValidateTokenResponse { IsValid = true, UserId = "u-1", Email = "a@b.com" };

        r1.Should().Be(r2);
    }

    [Fact]
    public void Equality_DifferentUserIdIsNotEqual()
    {
        var r1 = new ValidateTokenResponse { IsValid = true, UserId = "u-1" };
        var r2 = new ValidateTokenResponse { IsValid = true, UserId = "u-2" };

        r1.Should().NotBe(r2);
    }

    [Fact]
    public void Clone_CreatesEqualButDistinctInstance()
    {
        var original = new ValidateTokenResponse
        {
            IsValid = true, UserId = "u-1", UserName = "alice", Email = "alice@example.com"
        };
        var clone = original.Clone();

        clone.Should().Be(original);
        clone.Should().NotBeSameAs(original);
    }
}

public class CheckUserExistsResponseTests
{
    [Fact]
    public void Default_ExistsIsFalse()
    {
        var resp = new CheckUserExistsResponse();
        resp.Exists.Should().BeFalse();
    }

    [Fact]
    public void WithExistsTrue_ReturnsTrue()
    {
        var resp = new CheckUserExistsResponse { Exists = true };
        resp.Exists.Should().BeTrue();
    }

    [Fact]
    public void WithExistsFalse_ReturnsFalse()
    {
        var resp = new CheckUserExistsResponse { Exists = false };
        resp.Exists.Should().BeFalse();
    }

    [Fact]
    public void SerializeAndDeserialize_ExistsTrue_PreservesValue()
    {
        var original     = new CheckUserExistsResponse { Exists = true };
        var deserialized = CheckUserExistsResponse.Parser.ParseFrom(original.ToByteArray());

        deserialized.Exists.Should().BeTrue();
    }

    [Fact]
    public void SerializeAndDeserialize_ExistsFalse_PreservesValue()
    {
        var original     = new CheckUserExistsResponse { Exists = false };
        var deserialized = CheckUserExistsResponse.Parser.ParseFrom(original.ToByteArray());

        deserialized.Exists.Should().BeFalse();
    }

    [Fact]
    public void Equality_SameValueIsEqual()
    {
        var r1 = new CheckUserExistsResponse { Exists = true };
        var r2 = new CheckUserExistsResponse { Exists = true };

        r1.Should().Be(r2);
    }

    [Fact]
    public void Equality_DifferentValuesAreNotEqual()
    {
        var r1 = new CheckUserExistsResponse { Exists = true };
        var r2 = new CheckUserExistsResponse { Exists = false };

        r1.Should().NotBe(r2);
    }

    [Fact]
    public void Clone_CreatesEqualButDistinctInstance()
    {
        var original = new CheckUserExistsResponse { Exists = true };
        var clone    = original.Clone();

        clone.Should().Be(original);
        clone.Should().NotBeSameAs(original);
    }
}

public class UserReflectionTests
{
    [Fact]
    public void Descriptor_IsNotNull()
    {
        UserReflection.Descriptor.Should().NotBeNull();
    }

    [Fact]
    public void Descriptor_ContainsAllExpectedMessageTypes()
    {
        var messageNames = UserReflection.Descriptor.MessageTypes
            .Select(m => m.Name)
            .ToList();

        messageNames.Should().Contain("GetUserByIdRequest");
        messageNames.Should().Contain("GetUsersByIdsRequest");
        messageNames.Should().Contain("ValidateTokenRequest");
        messageNames.Should().Contain("CheckUserExistsRequest");
        messageNames.Should().Contain("UserResponse");
        messageNames.Should().Contain("UsersResponse");
        messageNames.Should().Contain("ValidateTokenResponse");
        messageNames.Should().Contain("CheckUserExistsResponse");
        messageNames.Should().Contain("UserDto");
    }

    [Fact]
    public void Descriptor_HasNineMessageTypes()
    {
        UserReflection.Descriptor.MessageTypes.Should().HaveCount(9);
    }

    [Fact]
    public void Descriptor_ContainsExpectedService()
    {
        var serviceNames = UserReflection.Descriptor.Services
            .Select(s => s.Name)
            .ToList();

        serviceNames.Should().Contain("UserGrpcService");
    }

    [Fact]
    public void Descriptor_UserDtoHasFiveFields()
    {
        var descriptor = UserReflection.Descriptor.MessageTypes
            .Single(m => m.Name == "UserDto");

        descriptor.Fields.InFieldNumberOrder().Should().HaveCount(5);
    }

    [Fact]
    public void Descriptor_UserDtoHasExpectedFieldNames()
    {
        var descriptor = UserReflection.Descriptor.MessageTypes
            .Single(m => m.Name == "UserDto");

        var fieldNames = descriptor.Fields.InFieldNumberOrder()
            .Select(f => f.Name)
            .ToList();

        fieldNames.Should().Contain("user_id");
        fieldNames.Should().Contain("user_name");
        fieldNames.Should().Contain("email");
        fieldNames.Should().Contain("avatar_url");
        fieldNames.Should().Contain("created_at");
    }

    [Fact]
    public void Descriptor_ValidateTokenResponseHasFourFields()
    {
        var descriptor = UserReflection.Descriptor.MessageTypes
            .Single(m => m.Name == "ValidateTokenResponse");

        descriptor.Fields.InFieldNumberOrder().Should().HaveCount(4);
    }
}

public class UserGrpcServiceDescriptorTests
{
    [Fact]
    public void Descriptor_IsNotNull()
    {
        UserGrpcService.Descriptor.Should().NotBeNull();
    }

    [Fact]
    public void Descriptor_HasExactlyFourMethods()
    {
        UserGrpcService.Descriptor.Methods.Should().HaveCount(4);
    }

    [Fact]
    public void Descriptor_ContainsAllExpectedMethodNames()
    {
        var methodNames = UserGrpcService.Descriptor.Methods
            .Select(m => m.Name)
            .ToList();

        methodNames.Should().Contain("GetUserById");
        methodNames.Should().Contain("GetUsersByIds");
        methodNames.Should().Contain("ValidateToken");
        methodNames.Should().Contain("CheckUserExists");
    }

    [Fact]
    public void Descriptor_FullNameIncludesPackageName()
    {
        UserGrpcService.Descriptor.FullName
            .Should().Be("users.UserGrpcService");
    }

    [Fact]
    public void Descriptor_AllMethodsAreUnary()
    {
        foreach (var method in UserGrpcService.Descriptor.Methods)
        {
            method.IsClientStreaming.Should().BeFalse();
            method.IsServerStreaming.Should().BeFalse();
        }
    }

    [Fact]
    public void Descriptor_GetUserById_HasCorrectInputAndOutputTypes()
    {
        var method = UserGrpcService.Descriptor.Methods
            .Single(m => m.Name == "GetUserById");

        method.InputType.Name.Should().Be("GetUserByIdRequest");
        method.OutputType.Name.Should().Be("UserResponse");
    }

    [Fact]
    public void Descriptor_GetUsersByIds_HasCorrectInputAndOutputTypes()
    {
        var method = UserGrpcService.Descriptor.Methods
            .Single(m => m.Name == "GetUsersByIds");

        method.InputType.Name.Should().Be("GetUsersByIdsRequest");
        method.OutputType.Name.Should().Be("UsersResponse");
    }

    [Fact]
    public void Descriptor_ValidateToken_HasCorrectInputAndOutputTypes()
    {
        var method = UserGrpcService.Descriptor.Methods
            .Single(m => m.Name == "ValidateToken");

        method.InputType.Name.Should().Be("ValidateTokenRequest");
        method.OutputType.Name.Should().Be("ValidateTokenResponse");
    }

    [Fact]
    public void Descriptor_CheckUserExists_HasCorrectInputAndOutputTypes()
    {
        var method = UserGrpcService.Descriptor.Methods
            .Single(m => m.Name == "CheckUserExists");

        method.InputType.Name.Should().Be("CheckUserExistsRequest");
        method.OutputType.Name.Should().Be("CheckUserExistsResponse");
    }
}
