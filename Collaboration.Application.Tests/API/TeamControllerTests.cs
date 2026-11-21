using Collaboration.API.Controllers;
using Collaboration.API.Services;
using Collaboration.Application.Commands.Team;
using Collaboration.Application.Interfaces.Services;
using Collaboration.Application.Queries.Team;
using Collaboration.Domain.Common.Helpers;
using Collaboration.Domain.Entities;
using Collaboration.Domain.Exceptions;
using Collaboration.Domain.ValueOfObjects;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Collaboration.Application.Tests.API;

public class TeamControllerTests
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<ICallerContext> _callerMock = new();
    private readonly Mock<ICollaborationAuditWriter> _auditMock = new();
    private readonly TeamController _controller;

    public TeamControllerTests()
    {
        _controller = new TeamController(_mediatorMock.Object, _callerMock.Object, _auditMock.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    private static Domain.Entities.Team CreateTeam()
    {
        var owner = new UserSnapshot("owner-id", "owner-username");
        return new Domain.Entities.Team("Test Team", "Description", "Design", ["design"], owner);
    }

    private static PagedList<Domain.Entities.Team> CreatePagedList(params Domain.Entities.Team[] teams) =>
        new(teams.ToList(), teams.Length, 1, 10);

    // ── GET api/team/{id} ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_WhenTeamFound_ReturnsOk()
    {
        var team = CreateTeam();
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetTeamByIdQuery>(), default)).ReturnsAsync(team);

        var result = await _controller.GetById("team-1", default);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(team);
    }

    [Fact]
    public async Task GetById_WhenNotFoundException_ReturnsNotFound()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetTeamByIdQuery>(), default))
                     .ThrowsAsync(new NotFoundException("not found"));

        var result = await _controller.GetById("missing", default);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetById_WhenDomainException_ReturnsBadRequest()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetTeamByIdQuery>(), default))
                     .ThrowsAsync(new DomainException("error"));

        var result = await _controller.GetById("team-1", default);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ── GET api/team ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPaged_ReturnsOkWithPagedList()
    {
        var paged = CreatePagedList(CreateTeam());
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetTeamsPagedQuery>(), default)).ReturnsAsync(paged);

        var result = await _controller.GetPaged(new(), default);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(paged);
    }

    // ── GET api/team/search ───────────────────────────────────────────────────

    [Fact]
    public async Task SearchByName_ReturnsOkWithTeams()
    {
        var teams = new List<Domain.Entities.Team> { CreateTeam() };
        _mediatorMock.Setup(m => m.Send(It.IsAny<SearchTeamsByNameQuery>(), default)).ReturnsAsync(teams);

        var result = await _controller.SearchByName("Test", default);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(teams);
    }

    // ── GET api/team/status/{status} ──────────────────────────────────────────

    [Fact]
    public async Task GetByStatus_ReturnsOkWithPagedList()
    {
        var paged = CreatePagedList(CreateTeam());
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetTeamsByStatusQuery>(), default)).ReturnsAsync(paged);

        var result = await _controller.GetByStatus(TeamStatus.Active, 1, 10, default);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(paged);
    }

    // ── GET api/team/category/{category} ──────────────────────────────────────

    [Fact]
    public async Task GetByCategory_ReturnsOkWithPagedList()
    {
        var paged = CreatePagedList(CreateTeam());
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetTeamsByCategoryQuery>(), default)).ReturnsAsync(paged);

        var result = await _controller.GetByCategory("Design", 1, 10, default);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(paged);
    }

    // ── GET api/team/mine ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetMyTeams_ReturnsOkWithTeams()
    {
        var teams = new List<Domain.Entities.Team> { CreateTeam() };
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetMyTeamsQuery>(), default)).ReturnsAsync(teams);

        var result = await _controller.GetMyTeams(default);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(teams);
    }

    // ── POST api/team ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_WhenSuccess_ReturnsCreatedAtAction()
    {
        var team = CreateTeam();
        _mediatorMock.Setup(m => m.Send(It.IsAny<CreateTeamCommand>(), default)).ReturnsAsync(team);

        var command = new CreateTeamCommand { Name = "Team", Description = "desc", Category = "Design" };
        var result = await _controller.Create(command, default);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(_controller.GetById));
        created.Value.Should().Be(team);
    }

    [Fact]
    public async Task Create_WhenDomainException_ReturnsBadRequest()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<CreateTeamCommand>(), default))
                     .ThrowsAsync(new DomainException("Name exists"));

        var command = new CreateTeamCommand { Name = "Team", Description = "d", Category = "X" };
        var result = await _controller.Create(command, default);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ── PUT api/team/{id} ─────────────────────────────────────────────────────

    [Fact]
    public async Task Update_WhenIdMismatch_ReturnsBadRequest()
    {
        var command = new UpdateTeamCommand { TeamId = "other-id", Name = "X", Description = "d", Category = "C" };
        var result = await _controller.Update("team-1", command, default);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Update_WhenSuccess_ReturnsOk()
    {
        var existing = CreateTeam();
        var updated = CreateTeam();
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetTeamByIdQuery>(), default)).ReturnsAsync(existing);
        _mediatorMock.Setup(m => m.Send(It.IsAny<UpdateTeamCommand>(), default)).ReturnsAsync(updated);

        var command = new UpdateTeamCommand { TeamId = "team-1", Name = "New", Description = "d", Category = "C" };
        var result = await _controller.Update("team-1", command, default);

        result.Should().BeOfType<OkObjectResult>();
    }

    // ── PATCH api/team/{id}/status ────────────────────────────────────────────

    [Fact]
    public async Task SetStatus_WhenIdMismatch_ReturnsBadRequest()
    {
        var command = new SetTeamStatusCommand { TeamId = "other", Status = TeamStatus.Active };
        var result = await _controller.SetStatus("team-1", command, default);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task SetStatus_WhenSuccess_ReturnsOk()
    {
        var team = CreateTeam();
        _mediatorMock.Setup(m => m.Send(It.IsAny<SetTeamStatusCommand>(), default)).ReturnsAsync(team);

        var command = new SetTeamStatusCommand { TeamId = "team-1", Status = TeamStatus.Active };
        var result = await _controller.SetStatus("team-1", command, default);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(team);
    }

    // ── DELETE api/team/{id} ──────────────────────────────────────────────────

    /*[Fact]
    public async Task Delete_WhenSuccess_ReturnsNoContent()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<DeleteTeamCommand>(), default)).ReturnsAsync(Unit.Value);

        var result = await _controller.Delete("team-1", default);

        result.Should().BeOfType<NoContentResult>();
    }*/

    /*[Fact]
    public async Task Delete_WhenNotFoundException_ReturnsNotFound()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<DeleteTeamCommand>(), default))
                     .ThrowsAsync(new NotFoundException("not found"));

        var result = await _controller.Delete("missing", default);

        result.Should().BeOfType<NotFoundObjectResult>();
    }*/

    // ── POST api/team/{id}/members ────────────────────────────────────────────

    [Fact]
    public async Task AddMember_WhenIdMismatch_ReturnsBadRequest()
    {
        var command = new AddTeamMemberCommand { TeamId = "other", MemberUserId = "u2", MemberUsername = "bob", Role = "Dev" };
        var result = await _controller.AddMember("team-1", command, default);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task AddMember_WhenSuccess_ReturnsOk()
    {
        var team = CreateTeam();
        _mediatorMock.Setup(m => m.Send(It.IsAny<AddTeamMemberCommand>(), default)).ReturnsAsync(team);

        var command = new AddTeamMemberCommand { TeamId = "team-1", MemberUserId = "u2", MemberUsername = "bob", Role = "Dev" };
        var result = await _controller.AddMember("team-1", command, default);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(team);
    }

    // ── DELETE api/team/{id}/members/{userId} ─────────────────────────────────

    [Fact]
    public async Task RemoveMember_WhenSuccess_ReturnsOk()
    {
        var team = CreateTeam();
        _mediatorMock.Setup(m => m.Send(It.IsAny<RemoveTeamMemberCommand>(), default)).ReturnsAsync(team);

        var result = await _controller.RemoveMember("team-1", "u2", default);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(team);
    }

    // ── POST api/team/{id}/required-roles ─────────────────────────────────────

    [Fact]
    public async Task AddRequiredRole_WhenIdMismatch_ReturnsBadRequest()
    {
        var command = new AddRequiredRoleCommand { TeamId = "other", Role = "Dev" };
        var result = await _controller.AddRequiredRole("team-1", command, default);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task AddRequiredRole_WhenSuccess_ReturnsOk()
    {
        var team = CreateTeam();
        _mediatorMock.Setup(m => m.Send(It.IsAny<AddRequiredRoleCommand>(), default)).ReturnsAsync(team);

        var command = new AddRequiredRoleCommand { TeamId = "team-1", Role = "Dev" };
        var result = await _controller.AddRequiredRole("team-1", command, default);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(team);
    }

    // ── PATCH api/team/{id}/avatar-url ────────────────────────────────────────

    [Fact]
    public async Task SetAvatarUrl_WhenIdMismatch_ReturnsBadRequest()
    {
        var command = new SetTeamAvatarUrlCommand { TeamId = "other", AvatarUrl = "url" };
        var result = await _controller.SetAvatarUrl("team-1", command, default);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task SetAvatarUrl_WhenSuccess_ReturnsOk()
    {
        var team = CreateTeam();
        _mediatorMock.Setup(m => m.Send(It.IsAny<SetTeamAvatarUrlCommand>(), default)).ReturnsAsync(team);

        var command = new SetTeamAvatarUrlCommand { TeamId = "team-1", AvatarUrl = "https://avatar.png" };
        var result = await _controller.SetAvatarUrl("team-1", command, default);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(team);
    }

    // ── GetById: null + UpdatedAt branches ───────────────────────────────────

    [Fact]
    public async Task GetById_WhenNull_ReturnsNotFound()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetTeamByIdQuery>(), default))
                     .ReturnsAsync((Domain.Entities.Team?)null);

        var result = await _controller.GetById("missing", default);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetById_WhenTeamHasUpdatedAt_AddsETagFromUpdatedAt()
    {
        var team = CreateTeam();
        team.MarkAsUpdated("owner-id");
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetTeamByIdQuery>(), default)).ReturnsAsync(team);

        var result = await _controller.GetById("team-1", default);

        result.Should().BeOfType<OkObjectResult>();
        _controller.Response.Headers.Should().ContainKey("ETag");
    }

    // ── Update: null + ETag mismatch + exception branches ────────────────────

    [Fact]
    public async Task Update_WhenTeamNotFound_ReturnsNotFound()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetTeamByIdQuery>(), default))
                     .ReturnsAsync((Domain.Entities.Team?)null);

        var command = new UpdateTeamCommand { TeamId = "team-1", Name = "X", Description = "d", Category = "C" };
        var result = await _controller.Update("team-1", command, default);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Update_WhenETagMismatch_Returns412()
    {
        var existing = CreateTeam();
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetTeamByIdQuery>(), default)).ReturnsAsync(existing);
        _controller.Request.Headers["If-Match"] = "\"non-matching-etag\"";

        var command = new UpdateTeamCommand { TeamId = "team-1", Name = "X", Description = "d", Category = "C" };
        var result = await _controller.Update("team-1", command, default);

        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(412);
    }

    [Fact]
    public async Task Update_WhenTeamHasUpdatedAt_ReturnsOkWithETag()
    {
        var existing = CreateTeam();
        existing.MarkAsUpdated("owner-id");
        var updated = CreateTeam();
        updated.MarkAsUpdated("owner-id");
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetTeamByIdQuery>(), default)).ReturnsAsync(existing);
        _mediatorMock.Setup(m => m.Send(It.IsAny<UpdateTeamCommand>(), default)).ReturnsAsync(updated);

        var command = new UpdateTeamCommand { TeamId = "team-1", Name = "New", Description = "d", Category = "C" };
        var result = await _controller.Update("team-1", command, default);

        result.Should().BeOfType<OkObjectResult>();
        _controller.Response.Headers.Should().ContainKey("ETag");
    }

    // ── catch block coverage ──────────────────────────────────────────────────

    [Fact]
    public async Task GetPaged_WhenException_ReturnsBadRequest()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetTeamsPagedQuery>(), default))
                     .ThrowsAsync(new DomainException("error"));

        var result = await _controller.GetPaged(new(), default);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task SearchByName_WhenException_ReturnsBadRequest()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<SearchTeamsByNameQuery>(), default))
                     .ThrowsAsync(new DomainException("error"));

        var result = await _controller.SearchByName("Test", default);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetByStatus_WhenException_ReturnsBadRequest()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetTeamsByStatusQuery>(), default))
                     .ThrowsAsync(new DomainException("error"));

        var result = await _controller.GetByStatus(TeamStatus.Active, 1, 10, default);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetByCategory_WhenException_ReturnsBadRequest()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetTeamsByCategoryQuery>(), default))
                     .ThrowsAsync(new DomainException("error"));

        var result = await _controller.GetByCategory("Design", 1, 10, default);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetMyTeams_WhenException_ReturnsBadRequest()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetMyTeamsQuery>(), default))
                     .ThrowsAsync(new DomainException("error"));

        var result = await _controller.GetMyTeams(default);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Update_WhenException_ReturnsBadRequest()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetTeamByIdQuery>(), default))
                     .ThrowsAsync(new DomainException("error"));

        var command = new UpdateTeamCommand { TeamId = "team-1", Name = "X", Description = "d", Category = "C" };
        var result = await _controller.Update("team-1", command, default);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task SetStatus_WhenException_ReturnsBadRequest()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<SetTeamStatusCommand>(), default))
                     .ThrowsAsync(new DomainException("error"));

        var command = new SetTeamStatusCommand { TeamId = "team-1", Status = TeamStatus.Active };
        var result = await _controller.SetStatus("team-1", command, default);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task AddMember_WhenException_ReturnsBadRequest()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<AddTeamMemberCommand>(), default))
                     .ThrowsAsync(new DomainException("already member"));

        var command = new AddTeamMemberCommand { TeamId = "team-1", MemberUserId = "u2", MemberUsername = "bob", Role = "Dev" };
        var result = await _controller.AddMember("team-1", command, default);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task RemoveMember_WhenException_ReturnsBadRequest()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<RemoveTeamMemberCommand>(), default))
                     .ThrowsAsync(new DomainException("not member"));

        var result = await _controller.RemoveMember("team-1", "u2", default);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task AddRequiredRole_WhenException_ReturnsBadRequest()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<AddRequiredRoleCommand>(), default))
                     .ThrowsAsync(new DomainException("error"));

        var command = new AddRequiredRoleCommand { TeamId = "team-1", Role = "Dev" };
        var result = await _controller.AddRequiredRole("team-1", command, default);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task SetAvatarUrl_WhenException_ReturnsBadRequest()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<SetTeamAvatarUrlCommand>(), default))
                     .ThrowsAsync(new DomainException("error"));

        var command = new SetTeamAvatarUrlCommand { TeamId = "team-1", AvatarUrl = "url" };
        var result = await _controller.SetAvatarUrl("team-1", command, default);

        result.Should().BeOfType<BadRequestObjectResult>();
    }
}