using FluentAssertions;
using Google.Protobuf;
using IdeaFusion.Grpc.CollaborationRequests;
using Xunit;

namespace GrpcClients.Tests.Proto;

public class CollaborationRequestDtoTests
{
    [Fact]
    public void Default_AllFieldsAreEmpty()
    {
        var dto = new CollaborationRequestDto();

        dto.RequestId.Should().BeEmpty();
        dto.TeamId.Should().BeEmpty();
        dto.FromUserId.Should().BeEmpty();
        dto.ToUserId.Should().BeEmpty();
        dto.Role.Should().BeEmpty();
        dto.Message.Should().BeEmpty();
        dto.Status.Should().BeEmpty();
        dto.CreatedAt.Should().BeEmpty();
        dto.ResolvedAt.Should().BeEmpty();
    }

    [Fact]
    public void Properties_CanBeSetAndRetrieved()
    {
        var requestId  = Guid.NewGuid().ToString();
        var teamId     = Guid.NewGuid().ToString();
        var fromUserId = Guid.NewGuid().ToString();
        var toUserId   = Guid.NewGuid().ToString();

        var dto = new CollaborationRequestDto
        {
            RequestId  = requestId,
            TeamId     = teamId,
            FromUserId = fromUserId,
            ToUserId   = toUserId,
            Role       = "Developer",
            Message    = "Please join our team",
            Status     = "Pending",
            CreatedAt  = "2024-01-01T00:00:00.0000000Z",
            ResolvedAt = ""
        };

        dto.RequestId.Should().Be(requestId);
        dto.TeamId.Should().Be(teamId);
        dto.FromUserId.Should().Be(fromUserId);
        dto.ToUserId.Should().Be(toUserId);
        dto.Role.Should().Be("Developer");
        dto.Message.Should().Be("Please join our team");
        dto.Status.Should().Be("Pending");
        dto.CreatedAt.Should().Be("2024-01-01T00:00:00.0000000Z");
        dto.ResolvedAt.Should().BeEmpty();
    }

    [Fact]
    public void SerializeAndDeserialize_PreservesAllFields()
    {
        var original = new CollaborationRequestDto
        {
            RequestId  = "req-1",
            TeamId     = "team-1",
            FromUserId = "user-1",
            ToUserId   = "user-2",
            Role       = "Designer",
            Message    = "Hello!",
            Status     = "Accepted",
            CreatedAt  = "2024-06-01T12:00:00.0000000Z",
            ResolvedAt = "2024-06-02T09:00:00.0000000Z"
        };

        var deserialized = CollaborationRequestDto.Parser.ParseFrom(original.ToByteArray());

        deserialized.RequestId.Should().Be(original.RequestId);
        deserialized.TeamId.Should().Be(original.TeamId);
        deserialized.FromUserId.Should().Be(original.FromUserId);
        deserialized.ToUserId.Should().Be(original.ToUserId);
        deserialized.Role.Should().Be(original.Role);
        deserialized.Message.Should().Be(original.Message);
        deserialized.Status.Should().Be(original.Status);
        deserialized.CreatedAt.Should().Be(original.CreatedAt);
        deserialized.ResolvedAt.Should().Be(original.ResolvedAt);
    }

    [Fact]
    public void Equality_SameValuesAreEqual()
    {
        var dto1 = new CollaborationRequestDto { RequestId = "req-1", Role = "Developer", Status = "Pending" };
        var dto2 = new CollaborationRequestDto { RequestId = "req-1", Role = "Developer", Status = "Pending" };

        dto1.Should().Be(dto2);
    }

    [Fact]
    public void Equality_DifferentRequestIdsAreNotEqual()
    {
        var dto1 = new CollaborationRequestDto { RequestId = "req-1" };
        var dto2 = new CollaborationRequestDto { RequestId = "req-2" };

        dto1.Should().NotBe(dto2);
    }

    [Fact]
    public void Clone_CreatesEqualButDistinctInstance()
    {
        var original = new CollaborationRequestDto
        {
            RequestId = "req-1",
            Role      = "Developer",
            Status    = "Pending"
        };

        var clone = original.Clone();

        clone.Should().Be(original);
        clone.Should().NotBeSameAs(original);
    }

    [Theory]
    [InlineData("Pending")]
    [InlineData("Accepted")]
    [InlineData("Rejected")]
    [InlineData("Cancelled")]
    public void Status_AcceptsAllValidValues(string status)
    {
        var dto = new CollaborationRequestDto { Status = status };
        dto.Status.Should().Be(status);
    }
}

public class CollaborationRequestResponseTests
{
    [Fact]
    public void Default_RequestIsNull()
    {
        var response = new CollaborationRequestResponse();
        response.Request.Should().BeNull();
    }

    [Fact]
    public void WithRequest_ReturnsAssignedDto()
    {
        var dto      = new CollaborationRequestDto { RequestId = "req-1", Role = "Designer" };
        var response = new CollaborationRequestResponse { Request = dto };

        response.Request.Should().NotBeNull();
        response.Request!.RequestId.Should().Be("req-1");
        response.Request.Role.Should().Be("Designer");
    }

    [Fact]
    public void SerializeAndDeserialize_PreservesNestedDto()
    {
        var response = new CollaborationRequestResponse
        {
            Request = new CollaborationRequestDto
            {
                RequestId = "req-abc",
                TeamId    = "team-x",
                Status    = "Accepted"
            }
        };

        var deserialized = CollaborationRequestResponse.Parser.ParseFrom(response.ToByteArray());

        deserialized.Request.Should().NotBeNull();
        deserialized.Request!.RequestId.Should().Be("req-abc");
        deserialized.Request.TeamId.Should().Be("team-x");
        deserialized.Request.Status.Should().Be("Accepted");
    }

    [Fact]
    public void Equality_SameRequestsAreEqual()
    {
        var dto = new CollaborationRequestDto { RequestId = "req-1" };

        var r1 = new CollaborationRequestResponse { Request = dto };
        var r2 = new CollaborationRequestResponse { Request = dto.Clone() };

        r1.Should().Be(r2);
    }

    [Fact]
    public void Clone_CreatesEqualButDistinctInstance()
    {
        var original = new CollaborationRequestResponse
        {
            Request = new CollaborationRequestDto { RequestId = "req-1", Status = "Pending" }
        };

        var clone = original.Clone();

        clone.Should().Be(original);
        clone.Should().NotBeSameAs(original);
    }
}

public class CollaborationRequestsResponseTests
{
    [Fact]
    public void Default_ItemsCollectionIsEmpty()
    {
        var response = new CollaborationRequestsResponse();
        response.Items.Should().BeEmpty();
    }

    [Fact]
    public void AddItems_CountMatchesAddedItems()
    {
        var response = new CollaborationRequestsResponse();
        response.Items.Add(new CollaborationRequestDto { RequestId = "req-1" });
        response.Items.Add(new CollaborationRequestDto { RequestId = "req-2" });

        response.Items.Should().HaveCount(2);
    }

    [Fact]
    public void AddRange_PreservesInsertionOrder()
    {
        var items = new[]
        {
            new CollaborationRequestDto { RequestId = "req-1" },
            new CollaborationRequestDto { RequestId = "req-2" },
            new CollaborationRequestDto { RequestId = "req-3" }
        };

        var response = new CollaborationRequestsResponse();
        response.Items.AddRange(items);

        response.Items[0].RequestId.Should().Be("req-1");
        response.Items[1].RequestId.Should().Be("req-2");
        response.Items[2].RequestId.Should().Be("req-3");
    }

    [Fact]
    public void SerializeAndDeserialize_PreservesAllItems()
    {
        var response = new CollaborationRequestsResponse();
        response.Items.Add(new CollaborationRequestDto { RequestId = "req-1", Role = "Developer" });
        response.Items.Add(new CollaborationRequestDto { RequestId = "req-2", Role = "Designer" });

        var deserialized = CollaborationRequestsResponse.Parser.ParseFrom(response.ToByteArray());

        deserialized.Items.Should().HaveCount(2);
        deserialized.Items[0].RequestId.Should().Be("req-1");
        deserialized.Items[0].Role.Should().Be("Developer");
        deserialized.Items[1].RequestId.Should().Be("req-2");
        deserialized.Items[1].Role.Should().Be("Designer");
    }

    [Fact]
    public void SerializeAndDeserialize_EmptyResponse_RemainsEmpty()
    {
        var response     = new CollaborationRequestsResponse();
        var deserialized = CollaborationRequestsResponse.Parser.ParseFrom(response.ToByteArray());

        deserialized.Items.Should().BeEmpty();
    }

    [Fact]
    public void Clone_CreatesEqualButDistinctInstance()
    {
        var response = new CollaborationRequestsResponse();
        response.Items.Add(new CollaborationRequestDto { RequestId = "req-1" });

        var clone = response.Clone();

        clone.Should().Be(response);
        clone.Should().NotBeSameAs(response);
    }
}

public class GetRequestByIdRequestTests
{
    [Fact]
    public void Default_RequestIdIsEmpty()
    {
        var request = new GetRequestByIdRequest();
        request.RequestId.Should().BeEmpty();
    }

    [Fact]
    public void WithRequestId_ReturnsAssignedValue()
    {
        var id      = Guid.NewGuid().ToString();
        var request = new GetRequestByIdRequest { RequestId = id };

        request.RequestId.Should().Be(id);
    }

    [Fact]
    public void SerializeAndDeserialize_PreservesRequestId()
    {
        var id      = Guid.NewGuid().ToString();
        var request = new GetRequestByIdRequest { RequestId = id };

        var deserialized = GetRequestByIdRequest.Parser.ParseFrom(request.ToByteArray());

        deserialized.RequestId.Should().Be(id);
    }

    [Fact]
    public void Equality_SameRequestIdIsEqual()
    {
        var id = Guid.NewGuid().ToString();

        var r1 = new GetRequestByIdRequest { RequestId = id };
        var r2 = new GetRequestByIdRequest { RequestId = id };

        r1.Should().Be(r2);
    }

    [Fact]
    public void Equality_DifferentRequestIdsAreNotEqual()
    {
        var r1 = new GetRequestByIdRequest { RequestId = "id-1" };
        var r2 = new GetRequestByIdRequest { RequestId = "id-2" };

        r1.Should().NotBe(r2);
    }

    [Fact]
    public void Clone_CreatesEqualButDistinctInstance()
    {
        var original = new GetRequestByIdRequest { RequestId = "req-xyz" };
        var clone    = original.Clone();

        clone.Should().Be(original);
        clone.Should().NotBeSameAs(original);
    }
}

public class GetRequestsByTeamRequestTests
{
    [Fact]
    public void Default_AllFieldsAreEmpty()
    {
        var request = new GetRequestsByTeamRequest();

        request.TeamId.Should().BeEmpty();
        request.Status.Should().BeEmpty();
    }

    [Fact]
    public void WithTeamIdOnly_StatusRemainsEmpty()
    {
        var teamId  = Guid.NewGuid().ToString();
        var request = new GetRequestsByTeamRequest { TeamId = teamId };

        request.TeamId.Should().Be(teamId);
        request.Status.Should().BeEmpty();
    }

    [Fact]
    public void WithTeamIdAndStatus_BothFieldsReturned()
    {
        var teamId  = Guid.NewGuid().ToString();
        var request = new GetRequestsByTeamRequest { TeamId = teamId, Status = "Pending" };

        request.TeamId.Should().Be(teamId);
        request.Status.Should().Be("Pending");
    }

    [Theory]
    [InlineData("Pending")]
    [InlineData("Accepted")]
    [InlineData("Rejected")]
    [InlineData("Cancelled")]
    [InlineData("")]
    public void Status_AcceptsAllValidValues(string status)
    {
        var request = new GetRequestsByTeamRequest { TeamId = "team-1", Status = status };
        request.Status.Should().Be(status);
    }

    [Fact]
    public void SerializeAndDeserialize_PreservesTeamIdAndStatus()
    {
        var teamId  = Guid.NewGuid().ToString();
        var request = new GetRequestsByTeamRequest { TeamId = teamId, Status = "Accepted" };

        var deserialized = GetRequestsByTeamRequest.Parser.ParseFrom(request.ToByteArray());

        deserialized.TeamId.Should().Be(teamId);
        deserialized.Status.Should().Be("Accepted");
    }

    [Fact]
    public void SerializeAndDeserialize_EmptyStatusOmittedFromWire()
    {
        var request = new GetRequestsByTeamRequest { TeamId = "team-1", Status = "" };

        var deserialized = GetRequestsByTeamRequest.Parser.ParseFrom(request.ToByteArray());

        deserialized.Status.Should().BeEmpty();
    }

    [Fact]
    public void Equality_SameFieldsAreEqual()
    {
        var r1 = new GetRequestsByTeamRequest { TeamId = "team-1", Status = "Pending" };
        var r2 = new GetRequestsByTeamRequest { TeamId = "team-1", Status = "Pending" };

        r1.Should().Be(r2);
    }

    [Fact]
    public void Equality_DifferentStatusIsNotEqual()
    {
        var r1 = new GetRequestsByTeamRequest { TeamId = "team-1", Status = "Pending" };
        var r2 = new GetRequestsByTeamRequest { TeamId = "team-1", Status = "Accepted" };

        r1.Should().NotBe(r2);
    }

    [Fact]
    public void Clone_CreatesEqualButDistinctInstance()
    {
        var original = new GetRequestsByTeamRequest { TeamId = "team-1", Status = "Rejected" };
        var clone    = original.Clone();

        clone.Should().Be(original);
        clone.Should().NotBeSameAs(original);
    }
}

public class GetRequestsByUserRequestTests
{
    [Fact]
    public void Default_AllFieldsAreEmpty()
    {
        var request = new GetRequestsByUserRequest();

        request.UserId.Should().BeEmpty();
        request.Status.Should().BeEmpty();
    }

    [Fact]
    public void WithUserIdOnly_StatusRemainsEmpty()
    {
        var userId  = Guid.NewGuid().ToString();
        var request = new GetRequestsByUserRequest { UserId = userId };

        request.UserId.Should().Be(userId);
        request.Status.Should().BeEmpty();
    }

    [Fact]
    public void WithUserIdAndStatus_BothFieldsReturned()
    {
        var userId  = Guid.NewGuid().ToString();
        var request = new GetRequestsByUserRequest { UserId = userId, Status = "Rejected" };

        request.UserId.Should().Be(userId);
        request.Status.Should().Be("Rejected");
    }

    [Theory]
    [InlineData("Pending")]
    [InlineData("Accepted")]
    [InlineData("Rejected")]
    [InlineData("Cancelled")]
    [InlineData("")]
    public void Status_AcceptsAllValidValues(string status)
    {
        var request = new GetRequestsByUserRequest { UserId = "user-1", Status = status };
        request.Status.Should().Be(status);
    }

    [Fact]
    public void SerializeAndDeserialize_PreservesUserIdAndStatus()
    {
        var userId  = Guid.NewGuid().ToString();
        var request = new GetRequestsByUserRequest { UserId = userId, Status = "Cancelled" };

        var deserialized = GetRequestsByUserRequest.Parser.ParseFrom(request.ToByteArray());

        deserialized.UserId.Should().Be(userId);
        deserialized.Status.Should().Be("Cancelled");
    }

    [Fact]
    public void SerializeAndDeserialize_EmptyStatusOmittedFromWire()
    {
        var request      = new GetRequestsByUserRequest { UserId = "user-1", Status = "" };
        var deserialized = GetRequestsByUserRequest.Parser.ParseFrom(request.ToByteArray());

        deserialized.Status.Should().BeEmpty();
    }

    [Fact]
    public void Equality_SameFieldsAreEqual()
    {
        var r1 = new GetRequestsByUserRequest { UserId = "user-1", Status = "Accepted" };
        var r2 = new GetRequestsByUserRequest { UserId = "user-1", Status = "Accepted" };

        r1.Should().Be(r2);
    }

    [Fact]
    public void Equality_DifferentUserIdsAreNotEqual()
    {
        var r1 = new GetRequestsByUserRequest { UserId = "user-1" };
        var r2 = new GetRequestsByUserRequest { UserId = "user-2" };

        r1.Should().NotBe(r2);
    }

    [Fact]
    public void Clone_CreatesEqualButDistinctInstance()
    {
        var original = new GetRequestsByUserRequest { UserId = "user-1", Status = "Pending" };
        var clone    = original.Clone();

        clone.Should().Be(original);
        clone.Should().NotBeSameAs(original);
    }
}

public class CollaborationRequestReflectionTests
{
    [Fact]
    public void Descriptor_IsNotNull()
    {
        CollaborationRequestReflection.Descriptor.Should().NotBeNull();
    }

    [Fact]
    public void Descriptor_ContainsAllExpectedMessageTypes()
    {
        var messageNames = CollaborationRequestReflection.Descriptor.MessageTypes
            .Select(m => m.Name)
            .ToList();

        messageNames.Should().Contain("CollaborationRequestDto");
        messageNames.Should().Contain("CollaborationRequestResponse");
        messageNames.Should().Contain("CollaborationRequestsResponse");
        messageNames.Should().Contain("GetRequestByIdRequest");
        messageNames.Should().Contain("GetRequestsByTeamRequest");
        messageNames.Should().Contain("GetRequestsByUserRequest");
    }

    [Fact]
    public void Descriptor_ContainsExpectedService()
    {
        var serviceNames = CollaborationRequestReflection.Descriptor.Services
            .Select(s => s.Name)
            .ToList();

        serviceNames.Should().Contain("CollaborationRequestGrpcService");
    }

    [Fact]
    public void Descriptor_CollaborationRequestDtoHasNineFields()
    {
        var dtoDescriptor = CollaborationRequestReflection.Descriptor.MessageTypes
            .Single(m => m.Name == "CollaborationRequestDto");

        dtoDescriptor.Fields.InFieldNumberOrder().Should().HaveCount(9);
    }

    [Fact]
    public void Descriptor_CollaborationRequestDtoHasExpectedFieldNames()
    {
        var dtoDescriptor = CollaborationRequestReflection.Descriptor.MessageTypes
            .Single(m => m.Name == "CollaborationRequestDto");

        var fieldNames = dtoDescriptor.Fields.InFieldNumberOrder()
            .Select(f => f.Name)
            .ToList();

        fieldNames.Should().Contain("request_id");
        fieldNames.Should().Contain("team_id");
        fieldNames.Should().Contain("from_user_id");
        fieldNames.Should().Contain("to_user_id");
        fieldNames.Should().Contain("role");
        fieldNames.Should().Contain("message");
        fieldNames.Should().Contain("status");
        fieldNames.Should().Contain("created_at");
        fieldNames.Should().Contain("resolved_at");
    }
}

public class CollaborationRequestGrpcServiceDescriptorTests
{
    [Fact]
    public void Descriptor_IsNotNull()
    {
        CollaborationRequestGrpcService.Descriptor.Should().NotBeNull();
    }

    [Fact]
    public void Descriptor_HasExactlyThreeMethods()
    {
        CollaborationRequestGrpcService.Descriptor.Methods.Should().HaveCount(3);
    }

    [Fact]
    public void Descriptor_ContainsAllExpectedMethodNames()
    {
        var methodNames = CollaborationRequestGrpcService.Descriptor.Methods
            .Select(m => m.Name)
            .ToList();

        methodNames.Should().Contain("GetRequestById");
        methodNames.Should().Contain("GetRequestsByTeam");
        methodNames.Should().Contain("GetRequestsByUser");
    }

    [Fact]
    public void Descriptor_FullNameIncludesPackageName()
    {
        CollaborationRequestGrpcService.Descriptor.FullName
            .Should().Be("collaboration_requests.CollaborationRequestGrpcService");
    }

    [Fact]
    public void Descriptor_GetRequestById_HasCorrectInputAndOutputTypes()
    {
        var method = CollaborationRequestGrpcService.Descriptor.Methods
            .Single(m => m.Name == "GetRequestById");

        method.InputType.Name.Should().Be("GetRequestByIdRequest");
        method.OutputType.Name.Should().Be("CollaborationRequestResponse");
    }

    [Fact]
    public void Descriptor_GetRequestsByTeam_HasCorrectInputAndOutputTypes()
    {
        var method = CollaborationRequestGrpcService.Descriptor.Methods
            .Single(m => m.Name == "GetRequestsByTeam");

        method.InputType.Name.Should().Be("GetRequestsByTeamRequest");
        method.OutputType.Name.Should().Be("CollaborationRequestsResponse");
    }

    [Fact]
    public void Descriptor_GetRequestsByUser_HasCorrectInputAndOutputTypes()
    {
        var method = CollaborationRequestGrpcService.Descriptor.Methods
            .Single(m => m.Name == "GetRequestsByUser");

        method.InputType.Name.Should().Be("GetRequestsByUserRequest");
        method.OutputType.Name.Should().Be("CollaborationRequestsResponse");
    }

    [Fact]
    public void Descriptor_AllMethodsAreUnary()
    {
        foreach (var method in CollaborationRequestGrpcService.Descriptor.Methods)
        {
            method.IsClientStreaming.Should().BeFalse();
            method.IsServerStreaming.Should().BeFalse();
        }
    }
}
