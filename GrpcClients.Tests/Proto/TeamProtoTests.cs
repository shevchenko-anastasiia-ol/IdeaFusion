using FluentAssertions;
using Google.Protobuf;
using IdeaFusion.Grpc.Teams;
using Xunit;

namespace GrpcClients.Tests.Proto;

public class CheckUserIsMemberRequestTests
{
    [Fact]
    public void Default_AllFieldsAreEmpty()
    {
        var req = new CheckUserIsMemberRequest();

        req.TeamId.Should().BeEmpty();
        req.UserId.Should().BeEmpty();
    }

    [Fact]
    public void Properties_CanBeSetAndRetrieved()
    {
        var req = new CheckUserIsMemberRequest
        {
            TeamId = "team-1",
            UserId = "user-1"
        };

        req.TeamId.Should().Be("team-1");
        req.UserId.Should().Be("user-1");
    }

    [Fact]
    public void SerializeAndDeserialize_PreservesAllFields()
    {
        var original = new CheckUserIsMemberRequest
        {
            TeamId = "team-abc",
            UserId = "user-xyz"
        };

        var deserialized = CheckUserIsMemberRequest.Parser.ParseFrom(original.ToByteArray());

        deserialized.TeamId.Should().Be("team-abc");
        deserialized.UserId.Should().Be("user-xyz");
    }

    [Fact]
    public void Equality_SameValuesAreEqual()
    {
        var r1 = new CheckUserIsMemberRequest { TeamId = "t-1", UserId = "u-1" };
        var r2 = new CheckUserIsMemberRequest { TeamId = "t-1", UserId = "u-1" };

        r1.Should().Be(r2);
    }

    [Fact]
    public void Equality_DifferentUserIdIsNotEqual()
    {
        var r1 = new CheckUserIsMemberRequest { TeamId = "t-1", UserId = "u-1" };
        var r2 = new CheckUserIsMemberRequest { TeamId = "t-1", UserId = "u-2" };

        r1.Should().NotBe(r2);
    }

    [Fact]
    public void Clone_CreatesEqualButDistinctInstance()
    {
        var original = new CheckUserIsMemberRequest { TeamId = "t-1", UserId = "u-1" };
        var clone    = original.Clone();

        clone.Should().Be(original);
        clone.Should().NotBeSameAs(original);
    }
}

public class CheckUserIsMemberResponseTests
{
    [Fact]
    public void Default_IsMemberIsFalseAndRoleIsEmpty()
    {
        var resp = new CheckUserIsMemberResponse();

        resp.IsMember.Should().BeFalse();
        resp.Role.Should().BeEmpty();
    }

    [Fact]
    public void WithIsMemberTrue_ReturnsTrue()
    {
        var resp = new CheckUserIsMemberResponse { IsMember = true, Role = "Owner" };

        resp.IsMember.Should().BeTrue();
        resp.Role.Should().Be("Owner");
    }

    [Fact]
    public void WithMemberRole_ReturnsCorrectRole()
    {
        var resp = new CheckUserIsMemberResponse { IsMember = true, Role = "Member" };

        resp.Role.Should().Be("Member");
    }

    [Fact]
    public void NonMember_HasEmptyRole()
    {
        var resp = new CheckUserIsMemberResponse { IsMember = false, Role = "" };

        resp.IsMember.Should().BeFalse();
        resp.Role.Should().BeEmpty();
    }

    [Fact]
    public void SerializeAndDeserialize_PreservesAllFields()
    {
        var original = new CheckUserIsMemberResponse { IsMember = true, Role = "Developer" };

        var deserialized = CheckUserIsMemberResponse.Parser.ParseFrom(original.ToByteArray());

        deserialized.IsMember.Should().BeTrue();
        deserialized.Role.Should().Be("Developer");
    }

    [Fact]
    public void SerializeAndDeserialize_NonMember_PreservesFields()
    {
        var original     = new CheckUserIsMemberResponse { IsMember = false };
        var deserialized = CheckUserIsMemberResponse.Parser.ParseFrom(original.ToByteArray());

        deserialized.IsMember.Should().BeFalse();
        deserialized.Role.Should().BeEmpty();
    }

    [Fact]
    public void Equality_SameValuesAreEqual()
    {
        var r1 = new CheckUserIsMemberResponse { IsMember = true, Role = "Owner" };
        var r2 = new CheckUserIsMemberResponse { IsMember = true, Role = "Owner" };

        r1.Should().Be(r2);
    }

    [Fact]
    public void Clone_CreatesEqualButDistinctInstance()
    {
        var original = new CheckUserIsMemberResponse { IsMember = true, Role = "Member" };
        var clone    = original.Clone();

        clone.Should().Be(original);
        clone.Should().NotBeSameAs(original);
    }
}

public class GetTeamByIdRequestTests
{
    [Fact]
    public void Default_TeamIdIsEmpty()
    {
        var req = new GetTeamByIdRequest();
        req.TeamId.Should().BeEmpty();
    }

    [Fact]
    public void WithTeamId_ReturnsAssignedValue()
    {
        var req = new GetTeamByIdRequest { TeamId = "team-42" };
        req.TeamId.Should().Be("team-42");
    }

    [Fact]
    public void SerializeAndDeserialize_PreservesTeamId()
    {
        var req          = new GetTeamByIdRequest { TeamId = "team-xyz" };
        var deserialized = GetTeamByIdRequest.Parser.ParseFrom(req.ToByteArray());

        deserialized.TeamId.Should().Be("team-xyz");
    }

    [Fact]
    public void Equality_SameTeamIdIsEqual()
    {
        var r1 = new GetTeamByIdRequest { TeamId = "t-1" };
        var r2 = new GetTeamByIdRequest { TeamId = "t-1" };

        r1.Should().Be(r2);
    }

    [Fact]
    public void Equality_DifferentTeamIdsAreNotEqual()
    {
        var r1 = new GetTeamByIdRequest { TeamId = "t-1" };
        var r2 = new GetTeamByIdRequest { TeamId = "t-2" };

        r1.Should().NotBe(r2);
    }

    [Fact]
    public void Clone_CreatesEqualButDistinctInstance()
    {
        var original = new GetTeamByIdRequest { TeamId = "team-abc" };
        var clone    = original.Clone();

        clone.Should().Be(original);
        clone.Should().NotBeSameAs(original);
    }
}

public class GetTeamsByIdsRequestTests
{
    [Fact]
    public void Default_TeamIdsIsEmpty()
    {
        var req = new GetTeamsByIdsRequest();
        req.TeamIds.Should().BeEmpty();
    }

    [Fact]
    public void AddTeamIds_CountMatchesAddedIds()
    {
        var req = new GetTeamsByIdsRequest();
        req.TeamIds.Add("t-1");
        req.TeamIds.Add("t-2");
        req.TeamIds.Add("t-3");

        req.TeamIds.Should().HaveCount(3);
    }

    [Fact]
    public void AddRange_PreservesInsertionOrder()
    {
        var req = new GetTeamsByIdsRequest();
        req.TeamIds.AddRange(new[] { "t-1", "t-2", "t-3" });

        req.TeamIds[0].Should().Be("t-1");
        req.TeamIds[1].Should().Be("t-2");
        req.TeamIds[2].Should().Be("t-3");
    }

    [Fact]
    public void SerializeAndDeserialize_PreservesAllIds()
    {
        var original = new GetTeamsByIdsRequest();
        original.TeamIds.AddRange(new[] { "team-a", "team-b" });

        var deserialized = GetTeamsByIdsRequest.Parser.ParseFrom(original.ToByteArray());

        deserialized.TeamIds.Should().HaveCount(2);
        deserialized.TeamIds.Should().Contain("team-a");
        deserialized.TeamIds.Should().Contain("team-b");
    }

    [Fact]
    public void SerializeAndDeserialize_EmptyRequest_RemainsEmpty()
    {
        var original     = new GetTeamsByIdsRequest();
        var deserialized = GetTeamsByIdsRequest.Parser.ParseFrom(original.ToByteArray());

        deserialized.TeamIds.Should().BeEmpty();
    }

    [Fact]
    public void Clone_CreatesEqualButDistinctInstance()
    {
        var original = new GetTeamsByIdsRequest();
        original.TeamIds.AddRange(new[] { "t-1", "t-2" });

        var clone = original.Clone();

        clone.Should().Be(original);
        clone.Should().NotBeSameAs(original);
    }
}

public class GetTeamsPagedRequestTests
{
    [Fact]
    public void Default_AllFieldsAreDefaultValues()
    {
        var req = new GetTeamsPagedRequest();

        req.PageNumber.Should().Be(0);
        req.PageSize.Should().Be(0);
        req.OrderBy.Should().BeEmpty();
        req.SearchText.Should().BeEmpty();
        req.Category.Should().BeEmpty();
        req.Tag.Should().BeEmpty();
        req.Status.Should().BeEmpty();
    }

    [Fact]
    public void Properties_CanBeSetAndRetrieved()
    {
        var req = new GetTeamsPagedRequest
        {
            PageNumber = 2,
            PageSize   = 10,
            OrderBy    = "newest",
            SearchText = "dev",
            Category   = "Design",
            Tag        = "ux",
            Status     = "Active"
        };

        req.PageNumber.Should().Be(2);
        req.PageSize.Should().Be(10);
        req.OrderBy.Should().Be("newest");
        req.SearchText.Should().Be("dev");
        req.Category.Should().Be("Design");
        req.Tag.Should().Be("ux");
        req.Status.Should().Be("Active");
    }

    [Fact]
    public void SerializeAndDeserialize_PreservesAllFields()
    {
        var original = new GetTeamsPagedRequest
        {
            PageNumber = 3,
            PageSize   = 20,
            OrderBy    = "members",
            SearchText = "alpha",
            Category   = "Tech",
            Tag        = "grpc",
            Status     = "Searching"
        };

        var deserialized = GetTeamsPagedRequest.Parser.ParseFrom(original.ToByteArray());

        deserialized.PageNumber.Should().Be(3);
        deserialized.PageSize.Should().Be(20);
        deserialized.OrderBy.Should().Be("members");
        deserialized.SearchText.Should().Be("alpha");
        deserialized.Category.Should().Be("Tech");
        deserialized.Tag.Should().Be("grpc");
        deserialized.Status.Should().Be("Searching");
    }

    [Fact]
    public void Equality_SameValuesAreEqual()
    {
        var r1 = new GetTeamsPagedRequest { PageNumber = 1, PageSize = 5, Status = "Active" };
        var r2 = new GetTeamsPagedRequest { PageNumber = 1, PageSize = 5, Status = "Active" };

        r1.Should().Be(r2);
    }

    [Fact]
    public void Clone_CreatesEqualButDistinctInstance()
    {
        var original = new GetTeamsPagedRequest { PageNumber = 2, PageSize = 15, OrderBy = "newest" };
        var clone    = original.Clone();

        clone.Should().Be(original);
        clone.Should().NotBeSameAs(original);
    }
}

public class MemberDtoTests
{
    [Fact]
    public void Default_AllFieldsAreEmpty()
    {
        var dto = new MemberDto();

        dto.UserId.Should().BeEmpty();
        dto.UserName.Should().BeEmpty();
        dto.AvatarUrl.Should().BeEmpty();
        dto.Role.Should().BeEmpty();
        dto.JoinedAt.Should().BeEmpty();
    }

    [Fact]
    public void Properties_CanBeSetAndRetrieved()
    {
        var dto = new MemberDto
        {
            UserId    = "user-1",
            UserName  = "alice",
            AvatarUrl = "https://example.com/alice.jpg",
            Role      = "Owner",
            JoinedAt  = "2024-01-15T10:00:00Z"
        };

        dto.UserId.Should().Be("user-1");
        dto.UserName.Should().Be("alice");
        dto.AvatarUrl.Should().Be("https://example.com/alice.jpg");
        dto.Role.Should().Be("Owner");
        dto.JoinedAt.Should().Be("2024-01-15T10:00:00Z");
    }

    [Fact]
    public void SerializeAndDeserialize_PreservesAllFields()
    {
        var original = new MemberDto
        {
            UserId    = "user-99",
            UserName  = "bob",
            AvatarUrl = "https://cdn.example.com/bob.png",
            Role      = "Member",
            JoinedAt  = "2024-03-01T08:00:00Z"
        };

        var deserialized = MemberDto.Parser.ParseFrom(original.ToByteArray());

        deserialized.UserId.Should().Be("user-99");
        deserialized.UserName.Should().Be("bob");
        deserialized.AvatarUrl.Should().Be("https://cdn.example.com/bob.png");
        deserialized.Role.Should().Be("Member");
        deserialized.JoinedAt.Should().Be("2024-03-01T08:00:00Z");
    }

    [Fact]
    public void Equality_SameValuesAreEqual()
    {
        var m1 = new MemberDto { UserId = "u-1", Role = "Owner" };
        var m2 = new MemberDto { UserId = "u-1", Role = "Owner" };

        m1.Should().Be(m2);
    }

    [Fact]
    public void Equality_DifferentRolesAreNotEqual()
    {
        var m1 = new MemberDto { UserId = "u-1", Role = "Owner" };
        var m2 = new MemberDto { UserId = "u-1", Role = "Member" };

        m1.Should().NotBe(m2);
    }

    [Fact]
    public void Clone_CreatesEqualButDistinctInstance()
    {
        var original = new MemberDto
        {
            UserId = "u-1", UserName = "alice", AvatarUrl = "url", Role = "Owner", JoinedAt = "2024-01-01"
        };
        var clone = original.Clone();

        clone.Should().Be(original);
        clone.Should().NotBeSameAs(original);
    }
}

public class PagedTeamsResponseTests
{
    [Fact]
    public void Default_ItemsIsEmptyAndCountersAreZero()
    {
        var response = new PagedTeamsResponse();

        response.Items.Should().BeEmpty();
        response.TotalCount.Should().Be(0);
        response.PageNumber.Should().Be(0);
        response.PageSize.Should().Be(0);
    }

    [Fact]
    public void AddItems_CountMatchesAddedItems()
    {
        var response = new PagedTeamsResponse { TotalCount = 2, PageNumber = 1, PageSize = 10 };
        response.Items.Add(new TeamDto { TeamId = "t-1" });
        response.Items.Add(new TeamDto { TeamId = "t-2" });

        response.Items.Should().HaveCount(2);
        response.TotalCount.Should().Be(2);
    }

    [Fact]
    public void AddRange_PreservesInsertionOrder()
    {
        var items = new[]
        {
            new TeamDto { TeamId = "t-1" },
            new TeamDto { TeamId = "t-2" },
            new TeamDto { TeamId = "t-3" }
        };

        var response = new PagedTeamsResponse();
        response.Items.AddRange(items);

        response.Items[0].TeamId.Should().Be("t-1");
        response.Items[1].TeamId.Should().Be("t-2");
        response.Items[2].TeamId.Should().Be("t-3");
    }

    [Fact]
    public void SerializeAndDeserialize_PreservesAllFields()
    {
        var response = new PagedTeamsResponse { TotalCount = 50, PageNumber = 2, PageSize = 10 };
        response.Items.Add(new TeamDto { TeamId = "t-1", Name = "Alpha" });
        response.Items.Add(new TeamDto { TeamId = "t-2", Name = "Beta" });

        var deserialized = PagedTeamsResponse.Parser.ParseFrom(response.ToByteArray());

        deserialized.Items.Should().HaveCount(2);
        deserialized.Items[0].TeamId.Should().Be("t-1");
        deserialized.Items[0].Name.Should().Be("Alpha");
        deserialized.Items[1].TeamId.Should().Be("t-2");
        deserialized.TotalCount.Should().Be(50);
        deserialized.PageNumber.Should().Be(2);
        deserialized.PageSize.Should().Be(10);
    }

    [Fact]
    public void SerializeAndDeserialize_EmptyResponse_RemainsEmpty()
    {
        var response     = new PagedTeamsResponse();
        var deserialized = PagedTeamsResponse.Parser.ParseFrom(response.ToByteArray());

        deserialized.Items.Should().BeEmpty();
    }

    [Fact]
    public void Clone_CreatesEqualButDistinctInstance()
    {
        var response = new PagedTeamsResponse { TotalCount = 5, PageNumber = 1, PageSize = 5 };
        response.Items.Add(new TeamDto { TeamId = "t-1" });

        var clone = response.Clone();

        clone.Should().Be(response);
        clone.Should().NotBeSameAs(response);
    }
}

public class RequiredRoleDtoTests
{
    [Fact]
    public void Default_AllFieldsAreEmpty()
    {
        var dto = new RequiredRoleDto();

        dto.Role.Should().BeEmpty();
        dto.Description.Should().BeEmpty();
    }

    [Fact]
    public void Properties_CanBeSetAndRetrieved()
    {
        var dto = new RequiredRoleDto
        {
            Role        = "Designer",
            Description = "UI/UX design skills required"
        };

        dto.Role.Should().Be("Designer");
        dto.Description.Should().Be("UI/UX design skills required");
    }

    [Fact]
    public void SerializeAndDeserialize_PreservesAllFields()
    {
        var original = new RequiredRoleDto
        {
            Role        = "Backend Developer",
            Description = "Experience with .NET and gRPC"
        };

        var deserialized = RequiredRoleDto.Parser.ParseFrom(original.ToByteArray());

        deserialized.Role.Should().Be("Backend Developer");
        deserialized.Description.Should().Be("Experience with .NET and gRPC");
    }

    [Fact]
    public void Equality_SameValuesAreEqual()
    {
        var r1 = new RequiredRoleDto { Role = "Tester", Description = "QA" };
        var r2 = new RequiredRoleDto { Role = "Tester", Description = "QA" };

        r1.Should().Be(r2);
    }

    [Fact]
    public void Equality_DifferentRolesAreNotEqual()
    {
        var r1 = new RequiredRoleDto { Role = "Designer" };
        var r2 = new RequiredRoleDto { Role = "Developer" };

        r1.Should().NotBe(r2);
    }

    [Fact]
    public void Clone_CreatesEqualButDistinctInstance()
    {
        var original = new RequiredRoleDto { Role = "DevOps", Description = "CI/CD pipelines" };
        var clone    = original.Clone();

        clone.Should().Be(original);
        clone.Should().NotBeSameAs(original);
    }
}

public class TeamDtoTests
{
    [Fact]
    public void Default_StringFieldsAreEmpty()
    {
        var dto = new TeamDto();

        dto.TeamId.Should().BeEmpty();
        dto.Name.Should().BeEmpty();
        dto.Description.Should().BeEmpty();
        dto.Category.Should().BeEmpty();
        dto.Status.Should().BeEmpty();
        dto.AvatarUrl.Should().BeEmpty();
        dto.CreatedAt.Should().BeEmpty();
    }

    [Fact]
    public void Default_MembersCountIsZero()
    {
        var dto = new TeamDto();
        dto.MembersCount.Should().Be(0);
    }

    [Fact]
    public void Default_CollectionsAreEmpty()
    {
        var dto = new TeamDto();

        dto.Tags.Should().BeEmpty();
        dto.Members.Should().BeEmpty();
        dto.RequiredRoles.Should().BeEmpty();
    }

    [Fact]
    public void Properties_CanBeSetAndRetrieved()
    {
        var dto = new TeamDto
        {
            TeamId       = "team-1",
            Name         = "Alpha Team",
            Description  = "A great team",
            Category     = "Tech",
            Status       = "Active",
            AvatarUrl    = "https://example.com/alpha.png",
            MembersCount = 5,
            CreatedAt    = "2024-01-01T00:00:00Z"
        };

        dto.TeamId.Should().Be("team-1");
        dto.Name.Should().Be("Alpha Team");
        dto.Description.Should().Be("A great team");
        dto.Category.Should().Be("Tech");
        dto.Status.Should().Be("Active");
        dto.AvatarUrl.Should().Be("https://example.com/alpha.png");
        dto.MembersCount.Should().Be(5);
        dto.CreatedAt.Should().Be("2024-01-01T00:00:00Z");
    }

    [Fact]
    public void AddTags_CountMatchesAddedTags()
    {
        var dto = new TeamDto();
        dto.Tags.Add("dotnet");
        dto.Tags.Add("grpc");
        dto.Tags.Add("csharp");

        dto.Tags.Should().HaveCount(3);
        dto.Tags.Should().Contain("grpc");
    }

    [Fact]
    public void AddMembers_CountMatchesAddedMembers()
    {
        var dto = new TeamDto();
        dto.Members.Add(new MemberDto { UserId = "u-1", Role = "Owner" });
        dto.Members.Add(new MemberDto { UserId = "u-2", Role = "Member" });

        dto.Members.Should().HaveCount(2);
        dto.Members[0].UserId.Should().Be("u-1");
        dto.Members[1].Role.Should().Be("Member");
    }

    [Fact]
    public void AddRequiredRoles_CountMatchesAddedRoles()
    {
        var dto = new TeamDto();
        dto.RequiredRoles.Add(new RequiredRoleDto { Role = "Designer" });
        dto.RequiredRoles.Add(new RequiredRoleDto { Role = "Developer" });

        dto.RequiredRoles.Should().HaveCount(2);
    }

    [Fact]
    public void SerializeAndDeserialize_PreservesAllFields()
    {
        var original = new TeamDto
        {
            TeamId       = "t-99",
            Name         = "Beta",
            Description  = "Beta team desc",
            Category     = "Design",
            Status       = "Searching",
            AvatarUrl    = "https://cdn.example.com/beta.png",
            MembersCount = 3,
            CreatedAt    = "2024-06-15T12:00:00Z"
        };
        original.Tags.Add("ui");
        original.Tags.Add("design");
        original.Members.Add(new MemberDto { UserId = "u-1", Role = "Owner" });
        original.RequiredRoles.Add(new RequiredRoleDto { Role = "Designer", Description = "UX skills" });

        var deserialized = TeamDto.Parser.ParseFrom(original.ToByteArray());

        deserialized.TeamId.Should().Be("t-99");
        deserialized.Name.Should().Be("Beta");
        deserialized.Category.Should().Be("Design");
        deserialized.Status.Should().Be("Searching");
        deserialized.MembersCount.Should().Be(3);
        deserialized.Tags.Should().HaveCount(2);
        deserialized.Tags.Should().Contain("ui");
        deserialized.Members.Should().HaveCount(1);
        deserialized.Members[0].UserId.Should().Be("u-1");
        deserialized.RequiredRoles.Should().HaveCount(1);
        deserialized.RequiredRoles[0].Role.Should().Be("Designer");
    }

    [Fact]
    public void Equality_SameValuesAreEqual()
    {
        var d1 = new TeamDto { TeamId = "t-1", Name = "Alpha", Status = "Active" };
        var d2 = new TeamDto { TeamId = "t-1", Name = "Alpha", Status = "Active" };

        d1.Should().Be(d2);
    }

    [Fact]
    public void Clone_CreatesEqualButDistinctInstance()
    {
        var original = new TeamDto { TeamId = "t-1", Name = "Clone test" };
        original.Tags.Add("x");
        original.Members.Add(new MemberDto { UserId = "u-1" });

        var clone = original.Clone();

        clone.Should().Be(original);
        clone.Should().NotBeSameAs(original);
    }
}

public class TeamResponseTests
{
    [Fact]
    public void Default_TeamIsNull()
    {
        var response = new TeamResponse();
        response.Team.Should().BeNull();
    }

    [Fact]
    public void WithTeam_ReturnsAssignedDto()
    {
        var dto      = new TeamDto { TeamId = "t-1", Name = "Alpha" };
        var response = new TeamResponse { Team = dto };

        response.Team.Should().NotBeNull();
        response.Team!.TeamId.Should().Be("t-1");
        response.Team.Name.Should().Be("Alpha");
    }

    [Fact]
    public void SerializeAndDeserialize_PreservesNestedDto()
    {
        var response = new TeamResponse
        {
            Team = new TeamDto { TeamId = "t-xyz", Name = "Beta", Status = "Active" }
        };

        var deserialized = TeamResponse.Parser.ParseFrom(response.ToByteArray());

        deserialized.Team.Should().NotBeNull();
        deserialized.Team!.TeamId.Should().Be("t-xyz");
        deserialized.Team.Name.Should().Be("Beta");
        deserialized.Team.Status.Should().Be("Active");
    }

    [Fact]
    public void SerializeAndDeserialize_WithNestedCollections_PreservesData()
    {
        var team = new TeamDto { TeamId = "t-1", Name = "Alpha" };
        team.Tags.Add("dotnet");
        team.Members.Add(new MemberDto { UserId = "u-1", Role = "Owner" });
        var response = new TeamResponse { Team = team };

        var deserialized = TeamResponse.Parser.ParseFrom(response.ToByteArray());

        deserialized.Team!.Tags.Should().ContainSingle().Which.Should().Be("dotnet");
        deserialized.Team.Members.Should().HaveCount(1);
    }

    [Fact]
    public void Equality_SameTeamIsEqual()
    {
        var dto = new TeamDto { TeamId = "t-1" };
        var r1  = new TeamResponse { Team = dto };
        var r2  = new TeamResponse { Team = dto.Clone() };

        r1.Should().Be(r2);
    }

    [Fact]
    public void Clone_CreatesEqualButDistinctInstance()
    {
        var original = new TeamResponse { Team = new TeamDto { TeamId = "t-1", Status = "Active" } };
        var clone    = original.Clone();

        clone.Should().Be(original);
        clone.Should().NotBeSameAs(original);
    }
}

public class TeamsResponseTests
{
    [Fact]
    public void Default_TeamsIsEmpty()
    {
        var response = new TeamsResponse();
        response.Teams.Should().BeEmpty();
    }

    [Fact]
    public void AddTeams_CountMatchesAddedTeams()
    {
        var response = new TeamsResponse();
        response.Teams.Add(new TeamDto { TeamId = "t-1" });
        response.Teams.Add(new TeamDto { TeamId = "t-2" });

        response.Teams.Should().HaveCount(2);
    }

    [Fact]
    public void AddRange_PreservesInsertionOrder()
    {
        var response = new TeamsResponse();
        response.Teams.AddRange(new[]
        {
            new TeamDto { TeamId = "t-1" },
            new TeamDto { TeamId = "t-2" },
            new TeamDto { TeamId = "t-3" }
        });

        response.Teams[0].TeamId.Should().Be("t-1");
        response.Teams[1].TeamId.Should().Be("t-2");
        response.Teams[2].TeamId.Should().Be("t-3");
    }

    [Fact]
    public void SerializeAndDeserialize_PreservesAllTeams()
    {
        var response = new TeamsResponse();
        response.Teams.Add(new TeamDto { TeamId = "t-1", Name = "Alpha" });
        response.Teams.Add(new TeamDto { TeamId = "t-2", Name = "Beta" });

        var deserialized = TeamsResponse.Parser.ParseFrom(response.ToByteArray());

        deserialized.Teams.Should().HaveCount(2);
        deserialized.Teams[0].TeamId.Should().Be("t-1");
        deserialized.Teams[0].Name.Should().Be("Alpha");
        deserialized.Teams[1].TeamId.Should().Be("t-2");
    }

    [Fact]
    public void SerializeAndDeserialize_EmptyResponse_RemainsEmpty()
    {
        var response     = new TeamsResponse();
        var deserialized = TeamsResponse.Parser.ParseFrom(response.ToByteArray());

        deserialized.Teams.Should().BeEmpty();
    }

    [Fact]
    public void Clone_CreatesEqualButDistinctInstance()
    {
        var response = new TeamsResponse();
        response.Teams.Add(new TeamDto { TeamId = "t-1" });
        response.Teams.Add(new TeamDto { TeamId = "t-2" });

        var clone = response.Clone();

        clone.Should().Be(response);
        clone.Should().NotBeSameAs(response);
    }
}

public class TeamReflectionTests
{
    [Fact]
    public void Descriptor_IsNotNull()
    {
        TeamReflection.Descriptor.Should().NotBeNull();
    }

    [Fact]
    public void Descriptor_ContainsAllExpectedMessageTypes()
    {
        var messageNames = TeamReflection.Descriptor.MessageTypes
            .Select(m => m.Name)
            .ToList();

        messageNames.Should().Contain("GetTeamsPagedRequest");
        messageNames.Should().Contain("GetTeamByIdRequest");
        messageNames.Should().Contain("GetTeamsByIdsRequest");
        messageNames.Should().Contain("CheckUserIsMemberRequest");
        messageNames.Should().Contain("TeamResponse");
        messageNames.Should().Contain("TeamsResponse");
        messageNames.Should().Contain("PagedTeamsResponse");
        messageNames.Should().Contain("CheckUserIsMemberResponse");
        messageNames.Should().Contain("TeamDto");
        messageNames.Should().Contain("MemberDto");
        messageNames.Should().Contain("RequiredRoleDto");
    }

    [Fact]
    public void Descriptor_HasElevenMessageTypes()
    {
        TeamReflection.Descriptor.MessageTypes.Should().HaveCount(11);
    }

    [Fact]
    public void Descriptor_ContainsExpectedService()
    {
        var serviceNames = TeamReflection.Descriptor.Services
            .Select(s => s.Name)
            .ToList();

        serviceNames.Should().Contain("TeamGrpcService");
    }

    [Fact]
    public void Descriptor_TeamDtoHasElevenFields()
    {
        var teamDtoDescriptor = TeamReflection.Descriptor.MessageTypes
            .Single(m => m.Name == "TeamDto");

        teamDtoDescriptor.Fields.InFieldNumberOrder().Should().HaveCount(11);
    }

    [Fact]
    public void Descriptor_TeamDtoHasExpectedFieldNames()
    {
        var teamDtoDescriptor = TeamReflection.Descriptor.MessageTypes
            .Single(m => m.Name == "TeamDto");

        var fieldNames = teamDtoDescriptor.Fields.InFieldNumberOrder()
            .Select(f => f.Name)
            .ToList();

        fieldNames.Should().Contain("team_id");
        fieldNames.Should().Contain("name");
        fieldNames.Should().Contain("description");
        fieldNames.Should().Contain("category");
        fieldNames.Should().Contain("tags");
        fieldNames.Should().Contain("status");
        fieldNames.Should().Contain("avatar_url");
        fieldNames.Should().Contain("members");
        fieldNames.Should().Contain("required_roles");
        fieldNames.Should().Contain("members_count");
        fieldNames.Should().Contain("created_at");
    }

    [Fact]
    public void Descriptor_MemberDtoHasFiveFields()
    {
        var descriptor = TeamReflection.Descriptor.MessageTypes
            .Single(m => m.Name == "MemberDto");

        descriptor.Fields.InFieldNumberOrder().Should().HaveCount(5);
    }

    [Fact]
    public void Descriptor_RequiredRoleDtoHasTwoFields()
    {
        var descriptor = TeamReflection.Descriptor.MessageTypes
            .Single(m => m.Name == "RequiredRoleDto");

        descriptor.Fields.InFieldNumberOrder().Should().HaveCount(2);
    }
}

public class TeamGrpcServiceDescriptorTests
{
    [Fact]
    public void Descriptor_IsNotNull()
    {
        TeamGrpcService.Descriptor.Should().NotBeNull();
    }

    [Fact]
    public void Descriptor_HasExactlyFourMethods()
    {
        TeamGrpcService.Descriptor.Methods.Should().HaveCount(4);
    }

    [Fact]
    public void Descriptor_ContainsAllExpectedMethodNames()
    {
        var methodNames = TeamGrpcService.Descriptor.Methods
            .Select(m => m.Name)
            .ToList();

        methodNames.Should().Contain("GetTeamsPaged");
        methodNames.Should().Contain("GetTeamById");
        methodNames.Should().Contain("GetTeamsByIds");
        methodNames.Should().Contain("CheckUserIsMember");
    }

    [Fact]
    public void Descriptor_FullNameIncludesPackageName()
    {
        TeamGrpcService.Descriptor.FullName
            .Should().Be("teams.TeamGrpcService");
    }

    [Fact]
    public void Descriptor_AllMethodsAreUnary()
    {
        foreach (var method in TeamGrpcService.Descriptor.Methods)
        {
            method.IsClientStreaming.Should().BeFalse();
            method.IsServerStreaming.Should().BeFalse();
        }
    }

    [Fact]
    public void Descriptor_GetTeamsPaged_HasCorrectInputAndOutputTypes()
    {
        var method = TeamGrpcService.Descriptor.Methods
            .Single(m => m.Name == "GetTeamsPaged");

        method.InputType.Name.Should().Be("GetTeamsPagedRequest");
        method.OutputType.Name.Should().Be("PagedTeamsResponse");
    }

    [Fact]
    public void Descriptor_GetTeamById_HasCorrectInputAndOutputTypes()
    {
        var method = TeamGrpcService.Descriptor.Methods
            .Single(m => m.Name == "GetTeamById");

        method.InputType.Name.Should().Be("GetTeamByIdRequest");
        method.OutputType.Name.Should().Be("TeamResponse");
    }

    [Fact]
    public void Descriptor_GetTeamsByIds_HasCorrectInputAndOutputTypes()
    {
        var method = TeamGrpcService.Descriptor.Methods
            .Single(m => m.Name == "GetTeamsByIds");

        method.InputType.Name.Should().Be("GetTeamsByIdsRequest");
        method.OutputType.Name.Should().Be("TeamsResponse");
    }

    [Fact]
    public void Descriptor_CheckUserIsMember_HasCorrectInputAndOutputTypes()
    {
        var method = TeamGrpcService.Descriptor.Methods
            .Single(m => m.Name == "CheckUserIsMember");

        method.InputType.Name.Should().Be("CheckUserIsMemberRequest");
        method.OutputType.Name.Should().Be("CheckUserIsMemberResponse");
    }
}

public class PostGrpcServiceAdditionalTests
{
    [Fact]
    public void Descriptor_IsNotNull()
    {
        IdeaFusion.Grpc.Posts.PostGrpcService.Descriptor.Should().NotBeNull();
    }

    [Fact]
    public void Descriptor_FullNameIsPostsPackage()
    {
        IdeaFusion.Grpc.Posts.PostGrpcService.Descriptor.FullName
            .Should().Be("posts.PostGrpcService");
    }

    [Fact]
    public void Descriptor_AllMethodsAreUnary()
    {
        foreach (var method in IdeaFusion.Grpc.Posts.PostGrpcService.Descriptor.Methods)
        {
            method.IsClientStreaming.Should().BeFalse();
            method.IsServerStreaming.Should().BeFalse();
        }
    }

    [Fact]
    public void Descriptor_HasExactlyFourMethods()
    {
        IdeaFusion.Grpc.Posts.PostGrpcService.Descriptor.Methods.Should().HaveCount(4);
    }
}
