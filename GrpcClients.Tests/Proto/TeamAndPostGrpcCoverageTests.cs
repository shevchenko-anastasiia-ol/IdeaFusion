using FluentAssertions;
using Google.Protobuf;
using Grpc.Core;
using IdeaFusion.Grpc.Posts;
using IdeaFusion.Grpc.Teams;
using System.Buffers;
using System.Reflection;
using Xunit;

namespace GrpcClients.Tests.Proto;

// ═══════════════════════════════════════════════════════════════════════════
// GetTeamByIdRequest
// ═══════════════════════════════════════════════════════════════════════════

public class GetTeamByIdRequestCoverageTests
{
    [Fact] public void Equals_WithDifferentType_ReturnsFalse() =>
        new GetTeamByIdRequest { TeamId = "t-1" }.Equals("str").Should().BeFalse();

    [Fact] public void Equals_SameReference_ReturnsTrue()
    {
        var req = new GetTeamByIdRequest { TeamId = "t-1" };
        req.Equals((object)req).Should().BeTrue();
    }

    [Fact] public void Equals_Null_ReturnsFalse() =>
        new GetTeamByIdRequest().Equals((GetTeamByIdRequest?)null).Should().BeFalse();

    [Fact] public void GetHashCode_EmptyMessage_ReturnsDeterministicValue() =>
        new GetTeamByIdRequest().GetHashCode().Should().Be(new GetTeamByIdRequest().GetHashCode());

    [Fact] public void GetHashCode_NonEmptyMessage_DifferentFromEmpty() =>
        new GetTeamByIdRequest { TeamId = "t-1" }.GetHashCode()
            .Should().NotBe(new GetTeamByIdRequest().GetHashCode());

    [Fact] public void GetHashCode_WithUnknownFields_Branches()
    {
        // CheckUserIsMemberRequest has field 2 (UserId) which becomes unknown in GetTeamByIdRequest
        var src = new CheckUserIsMemberRequest { TeamId = "t-1", UserId = "u-1" };
        var req = GetTeamByIdRequest.Parser.ParseFrom(src.ToByteArray());
        req.GetHashCode().Should().NotBe(0);
    }

    [Fact] public void ToString_ReturnsNonNullString() =>
        new GetTeamByIdRequest { TeamId = "t-1" }.ToString().Should().NotBeNull();

    [Fact] public void CalculateSize_EmptyMessage_ReturnsZero() =>
        new GetTeamByIdRequest().CalculateSize().Should().Be(0);

    [Fact] public void CalculateSize_WithUnknownFields_ReturnsPositive()
    {
        var src = new CheckUserIsMemberRequest { TeamId = "t-1", UserId = "u-1" };
        GetTeamByIdRequest.Parser.ParseFrom(src.ToByteArray()).CalculateSize().Should().BeGreaterThan(0);
    }

    [Fact] public void WriteTo_EmptyMessage_WritesZeroBytes() =>
        new GetTeamByIdRequest().ToByteArray().Should().BeEmpty();

    [Fact] public void MergeFrom_Null_DoesNotThrow()
    {
        var t = new GetTeamByIdRequest { TeamId = "t-1" };
        t.MergeFrom((GetTeamByIdRequest?)null!);
        t.TeamId.Should().Be("t-1");
    }

    [Fact] public void MergeFrom_EmptySource_DoesNotOverwrite()
    {
        var t = new GetTeamByIdRequest { TeamId = "t-1" };
        t.MergeFrom(new GetTeamByIdRequest());
        t.TeamId.Should().Be("t-1");
    }

    [Fact] public void MergeFrom_NonEmptySource_CopiesField()
    {
        var t = new GetTeamByIdRequest();
        t.MergeFrom(new GetTeamByIdRequest { TeamId = "t-2" });
        t.TeamId.Should().Be("t-2");
    }

    private static GetTeamByIdRequest WithUnknown() =>
        GetTeamByIdRequest.Parser.ParseFrom(
            new GetTeamByIdRequest { TeamId = "t-1" }.ToByteArray().Concat(new byte[] { 18, 1, 65 }).ToArray());

    [Fact] public void WriteTo_WithUnknownFields_WritesBytes() =>
        WithUnknown().ToByteArray().Should().NotBeEmpty();
}

// ═══════════════════════════════════════════════════════════════════════════
// GetTeamsByIdsRequest
// ═══════════════════════════════════════════════════════════════════════════

public class GetTeamsByIdsRequestCoverageTests
{
    [Fact] public void Equals_WithDifferentType_ReturnsFalse() =>
        new GetTeamsByIdsRequest().Equals(42).Should().BeFalse();

    [Fact] public void Equals_SameReference_ReturnsTrue()
    {
        var req = new GetTeamsByIdsRequest { TeamIds = { "t-1" } };
        req.Equals((object)req).Should().BeTrue();
    }

    [Fact] public void Equals_Null_ReturnsFalse() =>
        new GetTeamsByIdsRequest().Equals((GetTeamsByIdsRequest?)null).Should().BeFalse();

    [Fact] public void GetHashCode_EmptyMessage_ReturnsDeterministicValue() =>
        new GetTeamsByIdsRequest().GetHashCode().Should().Be(new GetTeamsByIdsRequest().GetHashCode());

    [Fact] public void ToString_ReturnsNonNullString() =>
        new GetTeamsByIdsRequest { TeamIds = { "t-1" } }.ToString().Should().NotBeNull();

    [Fact] public void CalculateSize_EmptyMessage_ReturnsZero() =>
        new GetTeamsByIdsRequest().CalculateSize().Should().Be(0);

    [Fact] public void WriteTo_EmptyMessage_WritesZeroBytes() =>
        new GetTeamsByIdsRequest().ToByteArray().Should().BeEmpty();

    [Fact] public void MergeFrom_Null_DoesNotThrow()
    {
        var t = new GetTeamsByIdsRequest { TeamIds = { "t-1" } };
        t.MergeFrom((GetTeamsByIdsRequest?)null!);
        t.TeamIds.Should().HaveCount(1);
    }

    [Fact] public void MergeFrom_NonEmptySource_AppendsList()
    {
        var t = new GetTeamsByIdsRequest();
        t.MergeFrom(new GetTeamsByIdsRequest { TeamIds = { "t-2", "t-3" } });
        t.TeamIds.Should().Contain("t-2").And.Contain("t-3");
    }

    private static GetTeamsByIdsRequest WithUnknown()
    {
        var bytes = new GetTeamsByIdsRequest { TeamIds = { "t-1" } }.ToByteArray().Concat(new byte[] { 18, 1, 65 }).ToArray();
        return GetTeamsByIdsRequest.Parser.ParseFrom(bytes);
    }

    [Fact] public void WriteTo_WithUnknownFields_WritesBytes() =>
        WithUnknown().ToByteArray().Should().NotBeEmpty();

    [Fact] public void GetHashCode_WithItems_DifferentFromEmpty() =>
        new GetTeamsByIdsRequest { TeamIds = { "t-1" } }.GetHashCode()
            .Should().NotBe(new GetTeamsByIdsRequest().GetHashCode());

    [Fact] public void GetHashCode_WithUnknownFields_Branches() =>
        WithUnknown().GetHashCode().Should().NotBe(0);

    [Fact] public void Equals_DifferentTeamIds_ReturnsFalse()
    {
        var r1 = new GetTeamsByIdsRequest { TeamIds = { "t-1" } };
        var r2 = new GetTeamsByIdsRequest { TeamIds = { "t-2" } };
        r1.Should().NotBe(r2);
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// GetTeamsPagedRequest
// ═══════════════════════════════════════════════════════════════════════════

public class GetTeamsPagedRequestCoverageTests
{
    [Fact] public void Equals_WithDifferentType_ReturnsFalse() =>
        new GetTeamsPagedRequest { PageNumber = 1 }.Equals("x").Should().BeFalse();

    [Fact] public void Equals_SameReference_ReturnsTrue()
    {
        var req = new GetTeamsPagedRequest { PageNumber = 1 };
        req.Equals((object)req).Should().BeTrue();
    }

    [Fact] public void Equals_Null_ReturnsFalse() =>
        new GetTeamsPagedRequest().Equals((GetTeamsPagedRequest?)null).Should().BeFalse();

    [Fact] public void GetHashCode_EmptyMessage_ReturnsDeterministicValue() =>
        new GetTeamsPagedRequest().GetHashCode().Should().Be(new GetTeamsPagedRequest().GetHashCode());

    [Fact] public void GetHashCode_NonEmptyMessage_DifferentFromEmpty() =>
        new GetTeamsPagedRequest { PageNumber = 1, PageSize = 10, OrderBy = "name" }.GetHashCode()
            .Should().NotBe(new GetTeamsPagedRequest().GetHashCode());

    [Fact] public void ToString_ReturnsNonNullString() =>
        new GetTeamsPagedRequest { PageNumber = 1 }.ToString().Should().NotBeNull();

    [Fact] public void CalculateSize_EmptyMessage_ReturnsZero() =>
        new GetTeamsPagedRequest().CalculateSize().Should().Be(0);

    [Fact] public void WriteTo_EmptyMessage_WritesZeroBytes() =>
        new GetTeamsPagedRequest().ToByteArray().Should().BeEmpty();

    [Fact] public void MergeFrom_Null_DoesNotThrow()
    {
        var t = new GetTeamsPagedRequest { PageNumber = 2 };
        t.MergeFrom((GetTeamsPagedRequest?)null!);
        t.PageNumber.Should().Be(2);
    }

    [Fact] public void MergeFrom_EmptySource_DoesNotOverwriteIntFields()
    {
        var t = new GetTeamsPagedRequest { PageNumber = 2, PageSize = 5 };
        t.MergeFrom(new GetTeamsPagedRequest());
        t.PageNumber.Should().Be(2);
        t.PageSize.Should().Be(5);
    }

    [Fact] public void MergeFrom_NonEmptySource_CopiesAllFields()
    {
        var t = new GetTeamsPagedRequest();
        t.MergeFrom(new GetTeamsPagedRequest
        {
            PageNumber = 3, PageSize = 20, OrderBy = "date",
            SearchText = "idea", Category = "Tech", Tag = "grpc", Status = "Active"
        });
        t.PageNumber.Should().Be(3);
        t.Status.Should().Be("Active");
    }

    private static GetTeamsPagedRequest WithUnknown()
    {
        var bytes = new GetTeamsPagedRequest { PageNumber = 1 }.ToByteArray().Concat(new byte[] { 66, 1, 65 }).ToArray();
        return GetTeamsPagedRequest.Parser.ParseFrom(bytes);
    }

    [Fact] public void WriteTo_WithUnknownFields_WritesBytes() =>
        WithUnknown().ToByteArray().Should().NotBeEmpty();

    [Fact] public void Equals_DifferentPageNumber_ReturnsFalse() =>
        new GetTeamsPagedRequest { PageNumber = 1 }.Equals(new GetTeamsPagedRequest { PageNumber = 2 }).Should().BeFalse();

    [Fact] public void Equals_DifferentPageSize_ReturnsFalse() =>
        new GetTeamsPagedRequest { PageSize = 5 }.Equals(new GetTeamsPagedRequest { PageSize = 10 }).Should().BeFalse();

    [Fact] public void Equals_DifferentOrderBy_ReturnsFalse() =>
        new GetTeamsPagedRequest { OrderBy = "name" }.Equals(new GetTeamsPagedRequest { OrderBy = "date" }).Should().BeFalse();

    [Fact] public void Equals_DifferentSearchText_ReturnsFalse() =>
        new GetTeamsPagedRequest { SearchText = "alpha" }.Equals(new GetTeamsPagedRequest { SearchText = "beta" }).Should().BeFalse();

    [Fact] public void Equals_DifferentCategory_ReturnsFalse() =>
        new GetTeamsPagedRequest { Category = "Tech" }.Equals(new GetTeamsPagedRequest { Category = "Art" }).Should().BeFalse();

    [Fact] public void Equals_DifferentTag_ReturnsFalse() =>
        new GetTeamsPagedRequest { Tag = "dotnet" }.Equals(new GetTeamsPagedRequest { Tag = "java" }).Should().BeFalse();

    [Fact] public void Equals_DifferentStatus_ReturnsFalse() =>
        new GetTeamsPagedRequest { Status = "Active" }.Equals(new GetTeamsPagedRequest { Status = "Archived" }).Should().BeFalse();

    [Fact] public void GetHashCode_WithSearchTextCategoryTagStatus_NonZero() =>
        new GetTeamsPagedRequest { SearchText = "x", Category = "Tech", Tag = "grpc", Status = "Active" }
            .GetHashCode().Should().NotBe(new GetTeamsPagedRequest().GetHashCode());

    [Fact] public void GetHashCode_WithUnknownFields_Branches() =>
        WithUnknown().GetHashCode().Should().NotBe(0);
}

// ═══════════════════════════════════════════════════════════════════════════
// CheckUserIsMemberRequest
// ═══════════════════════════════════════════════════════════════════════════

public class CheckUserIsMemberRequestCoverageTests
{
    [Fact] public void Equals_WithDifferentType_ReturnsFalse() =>
        new CheckUserIsMemberRequest { TeamId = "t-1" }.Equals(1.0).Should().BeFalse();

    [Fact] public void Equals_SameReference_ReturnsTrue()
    {
        var req = new CheckUserIsMemberRequest { TeamId = "t-1", UserId = "u-1" };
        req.Equals((object)req).Should().BeTrue();
    }

    [Fact] public void Equals_Null_ReturnsFalse() =>
        new CheckUserIsMemberRequest().Equals((CheckUserIsMemberRequest?)null).Should().BeFalse();

    [Fact] public void GetHashCode_EmptyMessage_ReturnsDeterministicValue() =>
        new CheckUserIsMemberRequest().GetHashCode()
            .Should().Be(new CheckUserIsMemberRequest().GetHashCode());

    [Fact] public void GetHashCode_NonEmpty_DifferentFromEmpty() =>
        new CheckUserIsMemberRequest { TeamId = "t-1", UserId = "u-1" }.GetHashCode()
            .Should().NotBe(new CheckUserIsMemberRequest().GetHashCode());

    [Fact] public void ToString_ReturnsNonNullString() =>
        new CheckUserIsMemberRequest { TeamId = "t-1" }.ToString().Should().NotBeNull();

    [Fact] public void CalculateSize_EmptyMessage_ReturnsZero() =>
        new CheckUserIsMemberRequest().CalculateSize().Should().Be(0);

    [Fact] public void WriteTo_EmptyMessage_WritesZeroBytes() =>
        new CheckUserIsMemberRequest().ToByteArray().Should().BeEmpty();

    [Fact] public void MergeFrom_Null_DoesNotThrow()
    {
        var t = new CheckUserIsMemberRequest { TeamId = "t-1" };
        t.MergeFrom((CheckUserIsMemberRequest?)null!);
        t.TeamId.Should().Be("t-1");
    }

    [Fact] public void MergeFrom_EmptySource_DoesNotOverwrite()
    {
        var t = new CheckUserIsMemberRequest { TeamId = "t-1", UserId = "u-1" };
        t.MergeFrom(new CheckUserIsMemberRequest());
        t.TeamId.Should().Be("t-1");
    }

    [Fact] public void MergeFrom_NonEmptySource_CopiesFields()
    {
        var t = new CheckUserIsMemberRequest();
        t.MergeFrom(new CheckUserIsMemberRequest { TeamId = "t-2", UserId = "u-2" });
        t.TeamId.Should().Be("t-2");
        t.UserId.Should().Be("u-2");
    }

    private static CheckUserIsMemberRequest WithUnknown()
    {
        var bytes = new CheckUserIsMemberRequest { TeamId = "t-1", UserId = "u-1" }.ToByteArray().Concat(new byte[] { 26, 1, 65 }).ToArray();
        return CheckUserIsMemberRequest.Parser.ParseFrom(bytes);
    }

    [Fact] public void WriteTo_WithUnknownFields_WritesBytes() =>
        WithUnknown().ToByteArray().Should().NotBeEmpty();

    [Fact] public void Equals_DifferentTeamId_ReturnsFalse() =>
        new CheckUserIsMemberRequest { TeamId = "t-1" }.Equals(new CheckUserIsMemberRequest { TeamId = "t-2" }).Should().BeFalse();

    [Fact] public void Equals_DifferentUserId_ReturnsFalse() =>
        new CheckUserIsMemberRequest { UserId = "u-1" }.Equals(new CheckUserIsMemberRequest { UserId = "u-2" }).Should().BeFalse();

    [Fact] public void GetHashCode_WithUnknownFields_Branches() =>
        WithUnknown().GetHashCode().Should().NotBe(0);
}

// ═══════════════════════════════════════════════════════════════════════════
// CheckUserIsMemberResponse
// ═══════════════════════════════════════════════════════════════════════════

public class CheckUserIsMemberResponseCoverageTests
{
    [Fact] public void Equals_WithDifferentType_ReturnsFalse() =>
        new CheckUserIsMemberResponse { IsMember = true }.Equals("x").Should().BeFalse();

    [Fact] public void Equals_SameReference_ReturnsTrue()
    {
        var r = new CheckUserIsMemberResponse { IsMember = true, Role = "Admin" };
        r.Equals((object)r).Should().BeTrue();
    }

    [Fact] public void Equals_Null_ReturnsFalse() =>
        new CheckUserIsMemberResponse().Equals((CheckUserIsMemberResponse?)null).Should().BeFalse();

    [Fact] public void GetHashCode_DefaultValues_ReturnsDeterministicValue() =>
        new CheckUserIsMemberResponse().GetHashCode()
            .Should().Be(new CheckUserIsMemberResponse().GetHashCode());

    [Fact] public void GetHashCode_IsMemberTrue_DifferentFromDefault() =>
        new CheckUserIsMemberResponse { IsMember = true }.GetHashCode()
            .Should().NotBe(new CheckUserIsMemberResponse().GetHashCode());

    [Fact] public void GetHashCode_RoleSet_DifferentFromDefault() =>
        new CheckUserIsMemberResponse { Role = "Member" }.GetHashCode()
            .Should().NotBe(new CheckUserIsMemberResponse().GetHashCode());

    [Fact] public void ToString_ReturnsNonNullString() =>
        new CheckUserIsMemberResponse { IsMember = true, Role = "Owner" }.ToString().Should().NotBeNull();

    [Fact] public void CalculateSize_DefaultValues_ReturnsZero() =>
        new CheckUserIsMemberResponse().CalculateSize().Should().Be(0);

    [Fact] public void CalculateSize_IsMemberTrue_ReturnsPositive() =>
        new CheckUserIsMemberResponse { IsMember = true }.CalculateSize().Should().BeGreaterThan(0);

    [Fact] public void CalculateSize_RoleSet_ReturnsPositive() =>
        new CheckUserIsMemberResponse { Role = "Member" }.CalculateSize().Should().BeGreaterThan(0);

    [Fact] public void WriteTo_DefaultValues_WritesZeroBytes() =>
        new CheckUserIsMemberResponse().ToByteArray().Should().BeEmpty();

    [Fact] public void WriteTo_IsMemberTrue_WritesBytes() =>
        new CheckUserIsMemberResponse { IsMember = true }.ToByteArray().Should().NotBeEmpty();

    [Fact] public void MergeFrom_Null_DoesNotThrow()
    {
        var t = new CheckUserIsMemberResponse { IsMember = true };
        t.MergeFrom((CheckUserIsMemberResponse?)null!);
        t.IsMember.Should().BeTrue();
    }

    [Fact] public void MergeFrom_EmptySource_DoesNotOverwrite()
    {
        var t = new CheckUserIsMemberResponse { IsMember = true, Role = "Admin" };
        t.MergeFrom(new CheckUserIsMemberResponse());
        t.IsMember.Should().BeTrue();
    }

    [Fact] public void MergeFrom_NonEmptySource_CopiesFields()
    {
        var t = new CheckUserIsMemberResponse();
        t.MergeFrom(new CheckUserIsMemberResponse { IsMember = true, Role = "Lead" });
        t.IsMember.Should().BeTrue();
        t.Role.Should().Be("Lead");
    }

    private static CheckUserIsMemberResponse WithUnknown()
    {
        var bytes = new CheckUserIsMemberResponse { IsMember = true, Role = "Admin" }.ToByteArray().Concat(new byte[] { 26, 1, 65 }).ToArray();
        return CheckUserIsMemberResponse.Parser.ParseFrom(bytes);
    }

    [Fact] public void WriteTo_WithUnknownFields_WritesBytes() =>
        WithUnknown().ToByteArray().Should().NotBeEmpty();

    [Fact] public void Equals_DifferentIsMember_ReturnsFalse() =>
        new CheckUserIsMemberResponse { IsMember = true }.Equals(new CheckUserIsMemberResponse { IsMember = false }).Should().BeFalse();

    [Fact] public void Equals_DifferentRole_ReturnsFalse() =>
        new CheckUserIsMemberResponse { Role = "Admin" }.Equals(new CheckUserIsMemberResponse { Role = "Member" }).Should().BeFalse();

    [Fact] public void GetHashCode_WithUnknownFields_Branches() =>
        WithUnknown().GetHashCode().Should().NotBe(0);
}

// ═══════════════════════════════════════════════════════════════════════════
// TeamResponse
// ═══════════════════════════════════════════════════════════════════════════

public class TeamResponseCoverageTests
{
    private static TeamDto SampleTeam() => new() { TeamId = "t-1", Name = "Alpha" };

    [Fact] public void Equals_WithDifferentType_ReturnsFalse() =>
        new TeamResponse { Team = SampleTeam() }.Equals(42).Should().BeFalse();

    [Fact] public void Equals_SameReference_ReturnsTrue()
    {
        var r = new TeamResponse { Team = SampleTeam() };
        r.Equals((object)r).Should().BeTrue();
    }

    [Fact] public void Equals_Null_ReturnsFalse() =>
        new TeamResponse().Equals((TeamResponse?)null).Should().BeFalse();

    [Fact] public void GetHashCode_EmptyMessage_ReturnsDeterministicValue() =>
        new TeamResponse().GetHashCode().Should().Be(new TeamResponse().GetHashCode());

    [Fact] public void GetHashCode_WithTeam_DifferentFromEmpty() =>
        new TeamResponse { Team = SampleTeam() }.GetHashCode()
            .Should().NotBe(new TeamResponse().GetHashCode());

    [Fact] public void ToString_ReturnsNonNullString() =>
        new TeamResponse { Team = SampleTeam() }.ToString().Should().NotBeNull();

    [Fact] public void CalculateSize_EmptyMessage_ReturnsZero() =>
        new TeamResponse().CalculateSize().Should().Be(0);

    [Fact] public void WriteTo_EmptyMessage_WritesZeroBytes() =>
        new TeamResponse().ToByteArray().Should().BeEmpty();

    [Fact] public void MergeFrom_Null_DoesNotThrow()
    {
        var t = new TeamResponse { Team = SampleTeam() };
        t.MergeFrom((TeamResponse?)null!);
        t.Team!.TeamId.Should().Be("t-1");
    }

    [Fact] public void MergeFrom_NonEmptySource_MergesTeam()
    {
        var t = new TeamResponse();
        t.MergeFrom(new TeamResponse { Team = new TeamDto { TeamId = "t-2" } });
        t.Team!.TeamId.Should().Be("t-2");
    }

    private static TeamResponse WithUnknown()
    {
        var bytes = new TeamResponse { Team = SampleTeam() }.ToByteArray().Concat(new byte[] { 18, 1, 65 }).ToArray();
        return TeamResponse.Parser.ParseFrom(bytes);
    }

    [Fact] public void WriteTo_WithUnknownFields_WritesBytes() =>
        WithUnknown().ToByteArray().Should().NotBeEmpty();

    [Fact] public void Clone_WithNullTeam_ReturnsCloneWithNullTeam()
    {
        var r     = new TeamResponse();
        var clone = r.Clone();
        clone.Team.Should().BeNull();
    }

    [Fact] public void Equals_DifferentTeam_ReturnsFalse()
    {
        var r1 = new TeamResponse { Team = new TeamDto { TeamId = "t-1" } };
        var r2 = new TeamResponse { Team = new TeamDto { TeamId = "t-2" } };
        r1.Should().NotBe(r2);
    }

    [Fact] public void ParseFrom_DuplicateTeamField_MergesIntoExisting()
    {
        var b1 = new TeamResponse { Team = new TeamDto { TeamId = "t-1" } }.ToByteArray();
        var b2 = new TeamResponse { Team = new TeamDto { Name = "Alpha" } }.ToByteArray();
        var parsed = TeamResponse.Parser.ParseFrom(b1.Concat(b2).ToArray());
        parsed.Team!.TeamId.Should().Be("t-1");
        parsed.Team.Name.Should().Be("Alpha");
    }

    [Fact] public void GetHashCode_WithUnknownFields_Branches() =>
        WithUnknown().GetHashCode().Should().NotBe(0);

    [Fact] public void MergeFrom_EmptySource_SkipsNullTeam()
    {
        var t = new TeamResponse { Team = SampleTeam() };
        t.MergeFrom(new TeamResponse()); // other.team_ == null → line 1233 FALSE
        t.Team!.TeamId.Should().Be("t-1");
    }

    [Fact] public void MergeFrom_ExistingTeam_DeepMerges()
    {
        var t = new TeamResponse { Team = new TeamDto { TeamId = "t-1" } };
        t.MergeFrom(new TeamResponse { Team = new TeamDto { Name = "Alpha" } }); // team_ not null → line 1234 FALSE
        t.Team!.TeamId.Should().Be("t-1");
        t.Team.Name.Should().Be("Alpha");
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// TeamsResponse
// ═══════════════════════════════════════════════════════════════════════════

public class TeamsResponseCoverageTests
{
    [Fact] public void Equals_WithDifferentType_ReturnsFalse() =>
        new TeamsResponse().Equals("x").Should().BeFalse();

    [Fact] public void Equals_SameReference_ReturnsTrue()
    {
        var r = new TeamsResponse { Teams = { new TeamDto { TeamId = "t-1" } } };
        r.Equals((object)r).Should().BeTrue();
    }

    [Fact] public void Equals_Null_ReturnsFalse() =>
        new TeamsResponse().Equals((TeamsResponse?)null).Should().BeFalse();

    [Fact] public void GetHashCode_EmptyMessage_ReturnsDeterministicValue() =>
        new TeamsResponse().GetHashCode().Should().Be(new TeamsResponse().GetHashCode());

    [Fact] public void ToString_ReturnsNonNullString() =>
        new TeamsResponse().ToString().Should().NotBeNull();

    [Fact] public void CalculateSize_EmptyMessage_ReturnsZero() =>
        new TeamsResponse().CalculateSize().Should().Be(0);

    [Fact] public void WriteTo_EmptyMessage_WritesZeroBytes() =>
        new TeamsResponse().ToByteArray().Should().BeEmpty();

    [Fact] public void MergeFrom_Null_DoesNotThrow()
    {
        var t = new TeamsResponse { Teams = { new TeamDto { TeamId = "t-1" } } };
        t.MergeFrom((TeamsResponse?)null!);
        t.Teams.Should().HaveCount(1);
    }

    [Fact] public void MergeFrom_NonEmptySource_AppendsList()
    {
        var t = new TeamsResponse();
        t.MergeFrom(new TeamsResponse { Teams = { new TeamDto { TeamId = "t-2" } } });
        t.Teams.Should().ContainSingle(x => x.TeamId == "t-2");
    }

    private static TeamsResponse WithUnknown()
    {
        var r = new TeamsResponse { Teams = { new TeamDto { TeamId = "t-1" } } };
        var bytes = r.ToByteArray().Concat(new byte[] { 18, 1, 65 }).ToArray();
        return TeamsResponse.Parser.ParseFrom(bytes);
    }

    [Fact] public void WriteTo_WithUnknownFields_WritesBytes() =>
        WithUnknown().ToByteArray().Should().NotBeEmpty();

    [Fact] public void Equals_DifferentTeams_ReturnsFalse()
    {
        var r1 = new TeamsResponse { Teams = { new TeamDto { TeamId = "t-1" } } };
        var r2 = new TeamsResponse { Teams = { new TeamDto { TeamId = "t-2" } } };
        r1.Should().NotBe(r2);
    }

    [Fact] public void GetHashCode_WithUnknownFields_Branches() =>
        WithUnknown().GetHashCode().Should().NotBe(0);
}

// ═══════════════════════════════════════════════════════════════════════════
// PagedTeamsResponse
// ═══════════════════════════════════════════════════════════════════════════

public class PagedTeamsResponseCoverageTests
{
    [Fact] public void Equals_WithDifferentType_ReturnsFalse() =>
        new PagedTeamsResponse { TotalCount = 5 }.Equals(5).Should().BeFalse();

    [Fact] public void Equals_SameReference_ReturnsTrue()
    {
        var r = new PagedTeamsResponse { TotalCount = 10, PageNumber = 1 };
        r.Equals((object)r).Should().BeTrue();
    }

    [Fact] public void Equals_Null_ReturnsFalse() =>
        new PagedTeamsResponse().Equals((PagedTeamsResponse?)null).Should().BeFalse();

    [Fact] public void GetHashCode_EmptyMessage_ReturnsDeterministicValue() =>
        new PagedTeamsResponse().GetHashCode().Should().Be(new PagedTeamsResponse().GetHashCode());

    [Fact] public void GetHashCode_NonEmptyIntFields_DifferentFromEmpty() =>
        new PagedTeamsResponse { TotalCount = 5, PageNumber = 1, PageSize = 10 }.GetHashCode()
            .Should().NotBe(new PagedTeamsResponse().GetHashCode());

    [Fact] public void ToString_ReturnsNonNullString() =>
        new PagedTeamsResponse { TotalCount = 2 }.ToString().Should().NotBeNull();

    [Fact] public void CalculateSize_EmptyMessage_ReturnsZero() =>
        new PagedTeamsResponse().CalculateSize().Should().Be(0);

    [Fact] public void CalculateSize_WithIntFields_ReturnsPositive() =>
        new PagedTeamsResponse { TotalCount = 1 }.CalculateSize().Should().BeGreaterThan(0);

    [Fact] public void WriteTo_EmptyMessage_WritesZeroBytes() =>
        new PagedTeamsResponse().ToByteArray().Should().BeEmpty();

    [Fact] public void WriteTo_NonEmpty_WritesBytes() =>
        new PagedTeamsResponse { TotalCount = 3, PageNumber = 1 }.ToByteArray().Should().NotBeEmpty();

    [Fact] public void MergeFrom_Null_DoesNotThrow()
    {
        var t = new PagedTeamsResponse { TotalCount = 5 };
        t.MergeFrom((PagedTeamsResponse?)null!);
        t.TotalCount.Should().Be(5);
    }

    [Fact] public void MergeFrom_EmptySource_DoesNotOverwriteIntFields()
    {
        var t = new PagedTeamsResponse { TotalCount = 10, PageNumber = 2, PageSize = 5 };
        t.MergeFrom(new PagedTeamsResponse());
        t.TotalCount.Should().Be(10);
    }

    [Fact] public void MergeFrom_NonEmptySource_CopiesIntFields()
    {
        var t = new PagedTeamsResponse();
        t.MergeFrom(new PagedTeamsResponse { TotalCount = 7, PageNumber = 3, PageSize = 20 });
        t.TotalCount.Should().Be(7);
        t.PageNumber.Should().Be(3);
        t.PageSize.Should().Be(20);
    }

    private static PagedTeamsResponse WithUnknown()
    {
        var r = new PagedTeamsResponse { TotalCount = 5, PageNumber = 1 };
        r.Items.Add(new TeamDto { TeamId = "t-1" });
        var bytes = r.ToByteArray().Concat(new byte[] { 42, 1, 65 }).ToArray();
        return PagedTeamsResponse.Parser.ParseFrom(bytes);
    }

    [Fact] public void WriteTo_WithUnknownFields_WritesBytes() =>
        WithUnknown().ToByteArray().Should().NotBeEmpty();

    [Fact] public void Equals_DifferentItems_ReturnsFalse()
    {
        var r1 = new PagedTeamsResponse(); r1.Items.Add(new TeamDto { TeamId = "t-1" });
        var r2 = new PagedTeamsResponse(); r2.Items.Add(new TeamDto { TeamId = "t-2" });
        r1.Should().NotBe(r2);
    }

    [Fact] public void Equals_DifferentTotalCount_ReturnsFalse() =>
        new PagedTeamsResponse { TotalCount = 10 }.Equals(new PagedTeamsResponse { TotalCount = 20 }).Should().BeFalse();

    [Fact] public void Equals_DifferentPageNumber_ReturnsFalse() =>
        new PagedTeamsResponse { PageNumber = 1 }.Equals(new PagedTeamsResponse { PageNumber = 2 }).Should().BeFalse();

    [Fact] public void Equals_DifferentPageSize_ReturnsFalse() =>
        new PagedTeamsResponse { PageSize = 5 }.Equals(new PagedTeamsResponse { PageSize = 10 }).Should().BeFalse();

    [Fact] public void GetHashCode_WithUnknownFields_Branches() =>
        WithUnknown().GetHashCode().Should().NotBe(0);
}

// ═══════════════════════════════════════════════════════════════════════════
// TeamDto
// ═══════════════════════════════════════════════════════════════════════════

public class TeamDtoCoverageTests
{
    private static TeamDto Full() => new()
    {
        TeamId = "t-1", Name = "Alpha", Description = "Desc",
        Category = "Tech", Status = "Active", AvatarUrl = "url",
        MembersCount = 3, CreatedAt = "2024-01-01",
        Tags = { "tag1" },
        Members = { new MemberDto { UserId = "u-1" } },
        RequiredRoles = { new RequiredRoleDto { Role = "Dev" } }
    };

    [Fact] public void Equals_WithDifferentType_ReturnsFalse() =>
        Full().Equals(new object()).Should().BeFalse();

    [Fact] public void Equals_SameReference_ReturnsTrue()
    {
        var dto = Full();
        dto.Equals((object)dto).Should().BeTrue();
    }

    [Fact] public void Equals_Null_ReturnsFalse() =>
        new TeamDto().Equals((TeamDto?)null).Should().BeFalse();

    [Fact] public void GetHashCode_EmptyMessage_ReturnsDeterministicValue() =>
        new TeamDto().GetHashCode().Should().Be(new TeamDto().GetHashCode());

    [Fact] public void GetHashCode_FullMessage_DifferentFromEmpty() =>
        Full().GetHashCode().Should().NotBe(new TeamDto().GetHashCode());

    [Fact] public void GetHashCode_MembersCount_BranchCovered() =>
        new TeamDto { MembersCount = 5 }.GetHashCode()
            .Should().NotBe(new TeamDto().GetHashCode());

    [Fact] public void ToString_ReturnsNonNullString() =>
        Full().ToString().Should().NotBeNull();

    [Fact] public void CalculateSize_EmptyMessage_ReturnsZero() =>
        new TeamDto().CalculateSize().Should().Be(0);

    [Fact] public void CalculateSize_FullMessage_ReturnsPositive() =>
        Full().CalculateSize().Should().BeGreaterThan(0);

    [Fact] public void WriteTo_EmptyMessage_WritesZeroBytes() =>
        new TeamDto().ToByteArray().Should().BeEmpty();

    [Fact] public void WriteTo_FullMessage_WritesBytes() =>
        Full().ToByteArray().Should().NotBeEmpty();

    [Fact] public void MergeFrom_Null_DoesNotThrow()
    {
        var t = new TeamDto { TeamId = "t-1" };
        t.MergeFrom((TeamDto?)null!);
        t.TeamId.Should().Be("t-1");
    }

    [Fact] public void MergeFrom_EmptySource_DoesNotOverwriteStringFields()
    {
        var t = new TeamDto { TeamId = "t-1", Name = "Alpha" };
        t.MergeFrom(new TeamDto());
        t.TeamId.Should().Be("t-1");
    }

    [Fact] public void MergeFrom_NonEmptySource_CopiesFields()
    {
        var t = new TeamDto();
        t.MergeFrom(new TeamDto
        {
            TeamId = "t-2", Name = "Beta", MembersCount = 4,
            Tags = { "x" }
        });
        t.TeamId.Should().Be("t-2");
        t.MembersCount.Should().Be(4);
        t.Tags.Should().Contain("x");
    }

    private static TeamDto WithUnknown()
    {
        var bytes = Full().ToByteArray().Concat(new byte[] { 98, 1, 65 }).ToArray();
        return TeamDto.Parser.ParseFrom(bytes);
    }

    [Fact] public void WriteTo_WithUnknownFields_WritesBytes() =>
        WithUnknown().ToByteArray().Should().NotBeEmpty();

    // Equals early-return branches for each field
    [Fact] public void Equals_DifferentTeamId_ReturnsFalse() =>
        new TeamDto { TeamId = "t-1" }.Equals(new TeamDto { TeamId = "t-2" }).Should().BeFalse();

    [Fact] public void Equals_DifferentName_ReturnsFalse() =>
        new TeamDto { Name = "Alpha" }.Equals(new TeamDto { Name = "Beta" }).Should().BeFalse();

    [Fact] public void Equals_DifferentDescription_ReturnsFalse() =>
        new TeamDto { Description = "a" }.Equals(new TeamDto { Description = "b" }).Should().BeFalse();

    [Fact] public void Equals_DifferentCategory_ReturnsFalse() =>
        new TeamDto { Category = "Tech" }.Equals(new TeamDto { Category = "Art" }).Should().BeFalse();

    [Fact] public void Equals_DifferentTags_ReturnsFalse()
    {
        var d1 = new TeamDto(); d1.Tags.Add("x");
        var d2 = new TeamDto(); d2.Tags.Add("y");
        d1.Should().NotBe(d2);
    }

    [Fact] public void Equals_DifferentStatus_ReturnsFalse() =>
        new TeamDto { Status = "Active" }.Equals(new TeamDto { Status = "Archived" }).Should().BeFalse();

    [Fact] public void Equals_DifferentAvatarUrl_ReturnsFalse() =>
        new TeamDto { AvatarUrl = "url-a" }.Equals(new TeamDto { AvatarUrl = "url-b" }).Should().BeFalse();

    [Fact] public void Equals_DifferentMembers_ReturnsFalse()
    {
        var d1 = new TeamDto(); d1.Members.Add(new MemberDto { UserId = "u-1" });
        var d2 = new TeamDto(); d2.Members.Add(new MemberDto { UserId = "u-2" });
        d1.Should().NotBe(d2);
    }

    [Fact] public void Equals_DifferentRequiredRoles_ReturnsFalse()
    {
        var d1 = new TeamDto(); d1.RequiredRoles.Add(new RequiredRoleDto { Role = "Dev" });
        var d2 = new TeamDto(); d2.RequiredRoles.Add(new RequiredRoleDto { Role = "QA" });
        d1.Should().NotBe(d2);
    }

    [Fact] public void Equals_DifferentMembersCount_ReturnsFalse() =>
        new TeamDto { MembersCount = 3 }.Equals(new TeamDto { MembersCount = 5 }).Should().BeFalse();

    [Fact] public void Equals_DifferentCreatedAt_ReturnsFalse() =>
        new TeamDto { CreatedAt = "2024-01-01" }.Equals(new TeamDto { CreatedAt = "2024-06-01" }).Should().BeFalse();

    // ParseFrom covering all fields in InternalMergeFrom
    [Fact] public void SerializeAndDeserialize_AllFields_CoversInternalMergePaths()
    {
        var parsed = TeamDto.Parser.ParseFrom(Full().ToByteArray());
        parsed.TeamId.Should().Be("t-1");
        parsed.Name.Should().Be("Alpha");
        parsed.MembersCount.Should().Be(3);
    }

    // Duplicate Members/RequiredRoles for non-null sub-message branches
    [Fact] public void ParseFrom_DuplicateMember_MergesIntoExisting()
    {
        var b1 = new TeamDto { Members = { new MemberDto { UserId = "u-1" } } }.ToByteArray();
        var b2 = new TeamDto { Members = { new MemberDto { UserName = "alice" } } }.ToByteArray();
        var parsed = TeamDto.Parser.ParseFrom(b1.Concat(b2).ToArray());
        parsed.Members.Should().HaveCount(2);
    }

    [Fact] public void GetHashCode_WithUnknownFields_Branches() =>
        WithUnknown().GetHashCode().Should().NotBe(0);

    [Fact] public void MergeFrom_WithDescriptionCategoryStatusAvatarUrlCreatedAt_CopiesAll()
    {
        var t = new TeamDto();
        t.MergeFrom(new TeamDto { Description = "Desc", Category = "Tech", Status = "Active", AvatarUrl = "url", CreatedAt = "2024-01-01" });
        t.Description.Should().Be("Desc");
        t.Category.Should().Be("Tech");
        t.Status.Should().Be("Active");
        t.AvatarUrl.Should().Be("url");
        t.CreatedAt.Should().Be("2024-01-01");
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// MemberDto
// ═══════════════════════════════════════════════════════════════════════════

public class MemberDtoCoverageTests
{
    private static MemberDto Full() => new()
    {
        UserId = "u-1", UserName = "alice",
        AvatarUrl = "https://cdn/a.png", Role = "Dev", JoinedAt = "2024-01-15"
    };

    [Fact] public void Equals_WithDifferentType_ReturnsFalse() =>
        Full().Equals("str").Should().BeFalse();

    [Fact] public void Equals_SameReference_ReturnsTrue()
    {
        var m = Full();
        m.Equals((object)m).Should().BeTrue();
    }

    [Fact] public void Equals_Null_ReturnsFalse() =>
        new MemberDto().Equals((MemberDto?)null).Should().BeFalse();

    [Fact] public void GetHashCode_EmptyMessage_ReturnsDeterministicValue() =>
        new MemberDto().GetHashCode().Should().Be(new MemberDto().GetHashCode());

    [Fact] public void GetHashCode_FullMessage_DifferentFromEmpty() =>
        Full().GetHashCode().Should().NotBe(new MemberDto().GetHashCode());

    [Fact] public void ToString_ReturnsNonNullString() =>
        Full().ToString().Should().NotBeNull();

    [Fact] public void CalculateSize_EmptyMessage_ReturnsZero() =>
        new MemberDto().CalculateSize().Should().Be(0);

    [Fact] public void CalculateSize_FullMessage_ReturnsPositive() =>
        Full().CalculateSize().Should().BeGreaterThan(0);

    [Fact] public void WriteTo_EmptyMessage_WritesZeroBytes() =>
        new MemberDto().ToByteArray().Should().BeEmpty();

    [Fact] public void MergeFrom_Null_DoesNotThrow()
    {
        var t = new MemberDto { UserId = "u-1" };
        t.MergeFrom((MemberDto?)null!);
        t.UserId.Should().Be("u-1");
    }

    [Fact] public void MergeFrom_EmptySource_DoesNotOverwrite()
    {
        var t = new MemberDto { UserId = "u-1", Role = "Dev" };
        t.MergeFrom(new MemberDto());
        t.UserId.Should().Be("u-1");
    }

    [Fact] public void MergeFrom_NonEmptySource_CopiesAllFields()
    {
        var t = new MemberDto();
        t.MergeFrom(Full());
        t.UserId.Should().Be("u-1");
        t.Role.Should().Be("Dev");
        t.JoinedAt.Should().Be("2024-01-15");
    }

    private static MemberDto WithUnknown()
    {
        var bytes = Full().ToByteArray().Concat(new byte[] { 50, 1, 65 }).ToArray();
        return MemberDto.Parser.ParseFrom(bytes);
    }

    [Fact] public void WriteTo_WithUnknownFields_WritesBytes() =>
        WithUnknown().ToByteArray().Should().NotBeEmpty();

    [Fact] public void Equals_DifferentUserId_ReturnsFalse() =>
        new MemberDto { UserId = "u-1" }.Equals(new MemberDto { UserId = "u-2" }).Should().BeFalse();

    [Fact] public void Equals_DifferentUserName_ReturnsFalse() =>
        new MemberDto { UserName = "alice" }.Equals(new MemberDto { UserName = "bob" }).Should().BeFalse();

    [Fact] public void Equals_DifferentAvatarUrl_ReturnsFalse() =>
        new MemberDto { AvatarUrl = "url-a" }.Equals(new MemberDto { AvatarUrl = "url-b" }).Should().BeFalse();

    [Fact] public void Equals_DifferentRole_ReturnsFalse() =>
        new MemberDto { Role = "Dev" }.Equals(new MemberDto { Role = "QA" }).Should().BeFalse();

    [Fact] public void Equals_DifferentJoinedAt_ReturnsFalse() =>
        new MemberDto { JoinedAt = "2024-01-01" }.Equals(new MemberDto { JoinedAt = "2024-06-01" }).Should().BeFalse();

    [Fact] public void GetHashCode_WithUnknownFields_Branches() =>
        WithUnknown().GetHashCode().Should().NotBe(0);
}

// ═══════════════════════════════════════════════════════════════════════════
// RequiredRoleDto
// ═══════════════════════════════════════════════════════════════════════════

public class RequiredRoleDtoCoverageTests
{
    [Fact] public void Equals_WithDifferentType_ReturnsFalse() =>
        new RequiredRoleDto { Role = "Dev" }.Equals(true).Should().BeFalse();

    [Fact] public void Equals_SameReference_ReturnsTrue()
    {
        var r = new RequiredRoleDto { Role = "Dev", Description = "Backend" };
        r.Equals((object)r).Should().BeTrue();
    }

    [Fact] public void Equals_Null_ReturnsFalse() =>
        new RequiredRoleDto().Equals((RequiredRoleDto?)null).Should().BeFalse();

    [Fact] public void GetHashCode_EmptyMessage_ReturnsDeterministicValue() =>
        new RequiredRoleDto().GetHashCode().Should().Be(new RequiredRoleDto().GetHashCode());

    [Fact] public void GetHashCode_NonEmpty_DifferentFromEmpty() =>
        new RequiredRoleDto { Role = "Dev", Description = "Backend" }.GetHashCode()
            .Should().NotBe(new RequiredRoleDto().GetHashCode());

    [Fact] public void ToString_ReturnsNonNullString() =>
        new RequiredRoleDto { Role = "Dev" }.ToString().Should().NotBeNull();

    [Fact] public void CalculateSize_EmptyMessage_ReturnsZero() =>
        new RequiredRoleDto().CalculateSize().Should().Be(0);

    [Fact] public void WriteTo_EmptyMessage_WritesZeroBytes() =>
        new RequiredRoleDto().ToByteArray().Should().BeEmpty();

    [Fact] public void MergeFrom_Null_DoesNotThrow()
    {
        var t = new RequiredRoleDto { Role = "Dev" };
        t.MergeFrom((RequiredRoleDto?)null!);
        t.Role.Should().Be("Dev");
    }

    [Fact] public void MergeFrom_EmptySource_DoesNotOverwrite()
    {
        var t = new RequiredRoleDto { Role = "Dev", Description = "Backend" };
        t.MergeFrom(new RequiredRoleDto());
        t.Role.Should().Be("Dev");
    }

    [Fact] public void MergeFrom_NonEmptySource_CopiesFields()
    {
        var t = new RequiredRoleDto();
        t.MergeFrom(new RequiredRoleDto { Role = "QA", Description = "Testing" });
        t.Role.Should().Be("QA");
        t.Description.Should().Be("Testing");
    }

    private static RequiredRoleDto WithUnknown()
    {
        var bytes = new RequiredRoleDto { Role = "Dev", Description = "Backend" }.ToByteArray().Concat(new byte[] { 26, 1, 65 }).ToArray();
        return RequiredRoleDto.Parser.ParseFrom(bytes);
    }

    [Fact] public void WriteTo_WithUnknownFields_WritesBytes() =>
        WithUnknown().ToByteArray().Should().NotBeEmpty();

    [Fact] public void Equals_DifferentRole_ReturnsFalse() =>
        new RequiredRoleDto { Role = "Dev" }.Equals(new RequiredRoleDto { Role = "QA" }).Should().BeFalse();

    [Fact] public void Equals_DifferentDescription_ReturnsFalse() =>
        new RequiredRoleDto { Description = "a" }.Equals(new RequiredRoleDto { Description = "b" }).Should().BeFalse();

    [Fact] public void GetHashCode_WithUnknownFields_Branches() =>
        WithUnknown().GetHashCode().Should().NotBe(0);
}

// ═══════════════════════════════════════════════════════════════════════════
// TeamGrpcService: BindService + TeamGrpcServiceBase
// ═══════════════════════════════════════════════════════════════════════════

public class TeamGrpcServiceBindingTests
{
    private sealed class StubImpl : TeamGrpcService.TeamGrpcServiceBase { }

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

    [Fact] public void BindService_WithImpl_ReturnsNonNullDefinition() =>
        TeamGrpcService.BindService(new StubImpl()).Should().NotBeNull();

    [Fact] public void BindService_Binder_WithNonNullImpl_BindsAllFourMethods()
    {
        var binder = new NoOpServiceBinder();
        TeamGrpcService.BindService(binder, new StubImpl());
        binder.MethodsAdded.Should().Be(4);
    }

    [Fact] public void BindService_Binder_WithNullImpl_BindsNullHandlers()
    {
        var binder = new NoOpServiceBinder();
        TeamGrpcService.BindService(binder, null!);
        binder.MethodsAdded.Should().Be(4);
    }

    [Fact]
    public async Task TeamGrpcServiceBase_GetTeamsPaged_ThrowsUnimplemented()
    {
        var stub = new StubImpl();
        var act  = async () => await stub.GetTeamsPaged(new GetTeamsPagedRequest(), null!);
        await act.Should().ThrowAsync<RpcException>()
            .Where(e => e.StatusCode == StatusCode.Unimplemented);
    }

    [Fact]
    public async Task TeamGrpcServiceBase_GetTeamById_ThrowsUnimplemented()
    {
        var stub = new StubImpl();
        var act  = async () => await stub.GetTeamById(new GetTeamByIdRequest(), null!);
        await act.Should().ThrowAsync<RpcException>()
            .Where(e => e.StatusCode == StatusCode.Unimplemented);
    }

    [Fact]
    public async Task TeamGrpcServiceBase_GetTeamsByIds_ThrowsUnimplemented()
    {
        var stub = new StubImpl();
        var act  = async () => await stub.GetTeamsByIds(new GetTeamsByIdsRequest(), null!);
        await act.Should().ThrowAsync<RpcException>()
            .Where(e => e.StatusCode == StatusCode.Unimplemented);
    }

    [Fact]
    public async Task TeamGrpcServiceBase_CheckUserIsMember_ThrowsUnimplemented()
    {
        var stub = new StubImpl();
        var act  = async () => await stub.CheckUserIsMember(new CheckUserIsMemberRequest(), null!);
        await act.Should().ThrowAsync<RpcException>()
            .Where(e => e.StatusCode == StatusCode.Unimplemented);
    }

    // ── __Helper_SerializeMessage / __Helper_DeserializeMessage coverage ────

    private sealed class FakeSerCtx : SerializationContext
    {
        private readonly ArrayBufferWriter<byte> _buf = new();
        public byte[] Written { get; private set; } = Array.Empty<byte>();
        public override void SetPayloadLength(int len) { }
        public override IBufferWriter<byte> GetBufferWriter() => _buf;
        public override void Complete() => Written = _buf.WrittenSpan.ToArray();
        public override void Complete(byte[] payload) => Written = payload;
    }

    private sealed class FakeDeserCtx : DeserializationContext
    {
        private readonly byte[] _data;
        public FakeDeserCtx(byte[] data) { _data = data; }
        public override int PayloadLength => _data.Length;
        public override ReadOnlySequence<byte> PayloadAsReadOnlySequence() => new ReadOnlySequence<byte>(_data);
        public override byte[] PayloadAsNewBuffer() => (byte[])_data.Clone();
    }

    private sealed class PlainTeamMsg : Google.Protobuf.IMessage<PlainTeamMsg>
    {
        public static readonly Google.Protobuf.MessageParser<PlainTeamMsg> Parser =
            new Google.Protobuf.MessageParser<PlainTeamMsg>(() => new PlainTeamMsg());
        public Google.Protobuf.Reflection.MessageDescriptor Descriptor => null!;
        public int CalculateSize() => 0;
        public void MergeFrom(Google.Protobuf.CodedInputStream input) { }
        public void WriteTo(Google.Protobuf.CodedOutputStream output) { }
        public void MergeFrom(PlainTeamMsg message) { }
        public PlainTeamMsg Clone() => new();
        public bool Equals(PlainTeamMsg? other) => other != null;
    }

    [Fact]
    public void Helper_SerializeMessage_IBufferMessage_CoversBufferPath()
    {
        var method = typeof(TeamGrpcService)
            .GetMethod("__Helper_SerializeMessage", BindingFlags.NonPublic | BindingFlags.Static)!;
        var ctx = new FakeSerCtx();
        method.Invoke(null, new object[] { new GetTeamByIdRequest { TeamId = "t-1" }, ctx });
        ctx.Written.Should().NotBeEmpty();
    }

    [Fact]
    public void Helper_SerializeMessage_NonIBufferMessage_CoversFallbackPath()
    {
        var method = typeof(TeamGrpcService)
            .GetMethod("__Helper_SerializeMessage", BindingFlags.NonPublic | BindingFlags.Static)!;
        var ctx = new FakeSerCtx();
        method.Invoke(null, new object[] { new PlainTeamMsg(), ctx });
        ctx.Written.Should().BeEmpty();
    }

    [Fact]
    public void Helper_DeserializeMessage_IBufferMessage_CoversBufferPath()
    {
        var bytes  = new GetTeamByIdRequest { TeamId = "t-1" }.ToByteArray();
        var method = typeof(TeamGrpcService)
            .GetMethod("__Helper_DeserializeMessage", BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(typeof(GetTeamByIdRequest));
        var result = (GetTeamByIdRequest)method.Invoke(
            null, new object[] { new FakeDeserCtx(bytes), GetTeamByIdRequest.Parser })!;
        result.TeamId.Should().Be("t-1");
    }

    [Fact]
    public void Helper_DeserializeMessage_NonIBufferMessage_CoversFallbackPath()
    {
        var method = typeof(TeamGrpcService)
            .GetMethod("__Helper_DeserializeMessage", BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(typeof(PlainTeamMsg));
        var result = (PlainTeamMsg)method.Invoke(
            null, new object[] { new FakeDeserCtx(Array.Empty<byte>()), PlainTeamMsg.Parser })!;
        result.Should().NotBeNull();
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// PostTeamDto
// ═══════════════════════════════════════════════════════════════════════════

public class PostTeamDtoCoverageTests
{
    private static PostTeamDto Full() =>
        new() { TeamId = "t-1", Name = "Alpha Team", AvatarUrl = "https://cdn/t.png" };

    [Fact] public void Equals_WithDifferentType_ReturnsFalse() =>
        Full().Equals(new object()).Should().BeFalse();

    [Fact] public void Equals_SameReference_ReturnsTrue()
    {
        var dto = Full();
        dto.Equals((object)dto).Should().BeTrue();
    }

    [Fact] public void Equals_Null_ReturnsFalse() =>
        new PostTeamDto().Equals((PostTeamDto?)null).Should().BeFalse();

    [Fact] public void GetHashCode_EmptyMessage_ReturnsDeterministicValue() =>
        new PostTeamDto().GetHashCode().Should().Be(new PostTeamDto().GetHashCode());

    [Fact] public void GetHashCode_FullMessage_DifferentFromEmpty() =>
        Full().GetHashCode().Should().NotBe(new PostTeamDto().GetHashCode());

    [Fact] public void ToString_ReturnsNonNullString() =>
        Full().ToString().Should().NotBeNull();

    [Fact] public void CalculateSize_EmptyMessage_ReturnsZero() =>
        new PostTeamDto().CalculateSize().Should().Be(0);

    [Fact] public void CalculateSize_FullMessage_ReturnsPositive() =>
        Full().CalculateSize().Should().BeGreaterThan(0);

    [Fact] public void WriteTo_EmptyMessage_WritesZeroBytes() =>
        new PostTeamDto().ToByteArray().Should().BeEmpty();

    [Fact] public void WriteTo_FullMessage_WritesBytes() =>
        Full().ToByteArray().Should().NotBeEmpty();

    [Fact] public void MergeFrom_Null_DoesNotThrow()
    {
        var t = Full();
        t.MergeFrom((PostTeamDto?)null!);
        t.TeamId.Should().Be("t-1");
    }

    [Fact] public void MergeFrom_EmptySource_DoesNotOverwrite()
    {
        var t = Full();
        t.MergeFrom(new PostTeamDto());
        t.TeamId.Should().Be("t-1");
        t.Name.Should().Be("Alpha Team");
    }

    [Fact] public void MergeFrom_NonEmptySource_CopiesFields()
    {
        var t = new PostTeamDto();
        t.MergeFrom(new PostTeamDto { TeamId = "t-2", Name = "Beta", AvatarUrl = "u" });
        t.TeamId.Should().Be("t-2");
        t.Name.Should().Be("Beta");
        t.AvatarUrl.Should().Be("u");
    }

    [Fact] public void SerializeAndDeserialize_PreservesAllFields()
    {
        var deserialized = PostTeamDto.Parser.ParseFrom(Full().ToByteArray());
        deserialized.TeamId.Should().Be("t-1");
        deserialized.Name.Should().Be("Alpha Team");
        deserialized.AvatarUrl.Should().Be("https://cdn/t.png");
    }

    private static PostTeamDto WithUnknown()
    {
        var bytes = Full().ToByteArray().Concat(new byte[] { 34, 1, 65 }).ToArray();
        return PostTeamDto.Parser.ParseFrom(bytes);
    }

    [Fact] public void WriteTo_WithUnknownFields_WritesBytes() =>
        WithUnknown().ToByteArray().Should().NotBeEmpty();

    [Fact] public void GetHashCode_WithUnknownFields_Branches() =>
        WithUnknown().GetHashCode().Should().NotBe(0);

    [Fact] public void CalculateSize_WithUnknownFields_ReturnsPositive() =>
        WithUnknown().CalculateSize().Should().BeGreaterThan(0);

    [Fact] public void Equals_DifferentName_ReturnsFalse() =>
        new PostTeamDto { Name = "Alpha" }.Equals(new PostTeamDto { Name = "Beta" }).Should().BeFalse();

    [Fact] public void Equals_DifferentAvatarUrl_ReturnsFalse() =>
        new PostTeamDto { AvatarUrl = "url-a" }.Equals(new PostTeamDto { AvatarUrl = "url-b" }).Should().BeFalse();
}

// ═══════════════════════════════════════════════════════════════════════════
// PostResponse
// ═══════════════════════════════════════════════════════════════════════════

public class PostResponseCoverageTests
{
    private static PostDto SamplePost() => new()
    {
        PostId    = "p-1",
        Title     = "Hello World",
        Status    = "Published",
        CreatedAt = "2024-06-01"
    };

    [Fact] public void Equals_WithDifferentType_ReturnsFalse() =>
        new PostResponse { Post = SamplePost() }.Equals("str").Should().BeFalse();

    [Fact] public void Equals_SameReference_ReturnsTrue()
    {
        var r = new PostResponse { Post = SamplePost() };
        r.Equals((object)r).Should().BeTrue();
    }

    [Fact] public void Equals_Null_ReturnsFalse() =>
        new PostResponse().Equals((PostResponse?)null).Should().BeFalse();

    [Fact] public void GetHashCode_EmptyMessage_ReturnsDeterministicValue() =>
        new PostResponse().GetHashCode().Should().Be(new PostResponse().GetHashCode());

    [Fact] public void GetHashCode_WithPost_DifferentFromEmpty() =>
        new PostResponse { Post = SamplePost() }.GetHashCode()
            .Should().NotBe(new PostResponse().GetHashCode());

    [Fact] public void ToString_ReturnsNonNullString() =>
        new PostResponse { Post = SamplePost() }.ToString().Should().NotBeNull();

    [Fact] public void CalculateSize_EmptyMessage_ReturnsZero() =>
        new PostResponse().CalculateSize().Should().Be(0);

    [Fact] public void CalculateSize_WithPost_ReturnsPositive() =>
        new PostResponse { Post = SamplePost() }.CalculateSize().Should().BeGreaterThan(0);

    [Fact] public void WriteTo_EmptyMessage_WritesZeroBytes() =>
        new PostResponse().ToByteArray().Should().BeEmpty();

    [Fact] public void WriteTo_WithPost_WritesBytes() =>
        new PostResponse { Post = SamplePost() }.ToByteArray().Should().NotBeEmpty();

    [Fact] public void MergeFrom_Null_DoesNotThrow()
    {
        var t = new PostResponse { Post = SamplePost() };
        t.MergeFrom((PostResponse?)null!);
        t.Post!.PostId.Should().Be("p-1");
    }

    [Fact] public void MergeFrom_NonEmptySource_MergesPost()
    {
        var t = new PostResponse();
        t.MergeFrom(new PostResponse { Post = new PostDto { PostId = "p-2" } });
        t.Post!.PostId.Should().Be("p-2");
    }

    [Fact] public void SerializeAndDeserialize_PreservesNestedPost()
    {
        var original     = new PostResponse { Post = SamplePost() };
        var deserialized = PostResponse.Parser.ParseFrom(original.ToByteArray());
        deserialized.Post!.PostId.Should().Be("p-1");
        deserialized.Post.Title.Should().Be("Hello World");
    }

    private static PostResponse WithUnknown()
    {
        var bytes = new PostResponse { Post = SamplePost() }.ToByteArray().Concat(new byte[] { 18, 1, 65 }).ToArray();
        return PostResponse.Parser.ParseFrom(bytes);
    }

    [Fact] public void WriteTo_WithUnknownFields_WritesBytes() =>
        WithUnknown().ToByteArray().Should().NotBeEmpty();

    [Fact] public void GetHashCode_WithUnknownFields_Branches() =>
        WithUnknown().GetHashCode().Should().NotBe(0);

    [Fact] public void Clone_WithNullPost_ReturnsNullPost()
    {
        var r = new PostResponse();
        r.Clone().Post.Should().BeNull();
    }

    [Fact] public void Equals_DifferentPost_ReturnsFalse()
    {
        var r1 = new PostResponse { Post = new PostDto { PostId = "p-1" } };
        var r2 = new PostResponse { Post = new PostDto { PostId = "p-2" } };
        r1.Should().NotBe(r2);
    }

    [Fact] public void ParseFrom_DuplicatePostField_MergesIntoExisting()
    {
        var b1 = new PostResponse { Post = new PostDto { PostId = "p-1" } }.ToByteArray();
        var b2 = new PostResponse { Post = new PostDto { Status = "Archived" } }.ToByteArray();
        var parsed = PostResponse.Parser.ParseFrom(b1.Concat(b2).ToArray());
        parsed.Post!.PostId.Should().Be("p-1");
        parsed.Post.Status.Should().Be("Archived");
    }

    [Fact] public void MergeFrom_EmptySource_SkipsNullPost()
    {
        var t = new PostResponse { Post = SamplePost() };
        t.MergeFrom(new PostResponse()); // post_ == null → FALSE branch
        t.Post!.PostId.Should().Be("p-1");
    }

    [Fact] public void MergeFrom_ExistingPost_DeepMerges()
    {
        var t = new PostResponse { Post = new PostDto { PostId = "p-1" } };
        t.MergeFrom(new PostResponse { Post = new PostDto { Title = "Updated" } }); // post_ not null → FALSE branch
        t.Post!.PostId.Should().Be("p-1");
        t.Post.Title.Should().Be("Updated");
    }
}