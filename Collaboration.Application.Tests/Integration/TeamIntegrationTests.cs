using System.Net;
using System.Net.Http.Json;
using Collaboration.Application.Commands.Team;
using Collaboration.Domain.Common.Helpers;
using Collaboration.Domain.Entities;
using Collaboration.Domain.ValueOfObjects;
using FluentAssertions;
using Moq;
using Xunit;

namespace Collaboration.Application.Tests.Integration;

/// <summary>
/// Integration tests for TeamController — the full HTTP pipeline (routing → MediatR
/// pipeline behaviors → command/query handlers) runs with mocked ITeamRepository.
/// </summary>
public class TeamIntegrationTests : IClassFixture<CollaborationWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CollaborationWebApplicationFactory _factory;

    public TeamIntegrationTests(CollaborationWebApplicationFactory factory)
    {
        _factory = factory;
        _client  = factory.CreateClient();
    }

    private static Team BuildTeam(string name = "Alpha Team")
    {
        var owner = new UserSnapshot("user-1", "testuser", null);
        return new Team(name, "A test team description", "Technology", new List<string> { "dotnet" }, owner);
    }

    private static CreateTeamCommand ValidCreateCommand(string name = "Alpha Team") => new()
    {
        Name        = name,
        Description = "A test team description",
        Category    = "Technology",
        Tags        = new List<string> { "dotnet" },
        UserId      = "user-1",
        Username    = "testuser",
    };

    // ─── POST /api/team ───────────────────────────────────────────────────────

    [Fact]
    public async Task Create_ValidCommand_Returns201WithTeam()
    {
        _factory.TeamRepoMock
            .Setup(r => r.ExistsByNameAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _factory.TeamRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<Team>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Team t, CancellationToken _) => t);

        var response = await _client.PostAsJsonAsync("/api/team", ValidCreateCommand("New Team"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<TeamResponseBody>();
        body.Should().NotBeNull();
        body!.Name.Should().Be("New Team");
    }

    [Fact]
    public async Task Create_MissingName_Returns400()
    {
        var command = new CreateTeamCommand
        {
            Name        = "",          // ← empty triggers FluentValidation
            Description = "Description",
            Category    = "Technology",
            Tags        = new List<string>(),
            UserId      = "user-1",
            Username    = "testuser",
        };

        var response = await _client.PostAsJsonAsync("/api/team", command);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_MissingUserId_Returns400()
    {
        var command = new CreateTeamCommand
        {
            Name        = "Some Team",
            Description = "Desc",
            Category    = "Technology",
            Tags        = new List<string>(),
            UserId      = "",          // ← empty triggers FluentValidation
            Username    = "testuser",
        };

        var response = await _client.PostAsJsonAsync("/api/team", command);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_DuplicateName_Returns400()
    {
        _factory.TeamRepoMock
            .Setup(r => r.ExistsByNameAsync("Duplicate Team", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var response = await _client.PostAsJsonAsync("/api/team", ValidCreateCommand("Duplicate Team"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ─── GET /api/team/{id} ───────────────────────────────────────────────────

    [Fact]
    public async Task GetById_WhenFound_Returns200WithETag()
    {
        var team = BuildTeam("Find Me");
        _factory.TeamRepoMock
            .Setup(r => r.GetByIdAsync(team.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(team);

        var response = await _client.GetAsync($"/api/team/{team.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.ETag.Should().NotBeNull();

        var body = await response.Content.ReadFromJsonAsync<TeamResponseBody>();
        body!.Name.Should().Be("Find Me");
    }

    [Fact]
    public async Task GetById_WhenNotFound_Returns400()
    {
        // GetTeamByIdQueryHandler throws DomainException when not found,
        // which BaseApiController.HandleException maps to BadRequest (400).
        _factory.TeamRepoMock
            .Setup(r => r.GetByIdAsync("nonexistent-id", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Team?)null);

        var response = await _client.GetAsync("/api/team/nonexistent-id");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ─── GET /api/team (paged) ────────────────────────────────────────────────

    [Fact]
    public async Task GetPaged_Returns200WithPagedResult()
    {
        var team = BuildTeam("Paged Team");
        var pagedList = new PagedList<Team>(new List<Team> { team }, 1, 1, 10);

        _factory.TeamRepoMock
            .Setup(r => r.GetPagedFilteredAsync(
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<TeamStatus?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string?>(), It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedList);

        var response = await _client.GetAsync("/api/team?pageNumber=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ─── GET /api/team/search ─────────────────────────────────────────────────

    [Fact]
    public async Task SearchByName_Returns200WithResults()
    {
        _factory.TeamRepoMock
            .Setup(r => r.SearchByNameAsync("Alpha", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Team> { BuildTeam("Alpha Team") });

        var response = await _client.GetAsync("/api/team/search?name=Alpha");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ─── GET /api/team/member/{userId} ────────────────────────────────────────

    [Fact]
    public async Task GetByMember_Returns200()
    {
        _factory.TeamRepoMock
            .Setup(r => r.GetByMemberIdAsync("user-42", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Team> { BuildTeam() });

        var response = await _client.GetAsync("/api/team/member/user-42");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ─── DELETE /api/team/{id} ────────────────────────────────────────────────

    [Fact]
    public async Task Delete_ExistingTeam_Returns204()
    {
        var team = BuildTeam("Deletable");
        _factory.TeamRepoMock
            .Setup(r => r.GetByIdAsync(team.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(team);
        _factory.TeamRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<Team>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Team t, CancellationToken _) => t);

        var response = await _client.DeleteAsync($"/api/team/{team.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_NonExistentTeam_Returns400()
    {
        _factory.TeamRepoMock
            .Setup(r => r.GetByIdAsync("bad-id", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Team?)null);

        var response = await _client.DeleteAsync("/api/team/bad-id");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ─── POST /api/team/{id}/members ──────────────────────────────────────────

    [Fact]
    public async Task AddMember_ValidRequest_Returns200()
    {
        var team = BuildTeam("Member Team");
        _factory.TeamRepoMock
            .Setup(r => r.GetByIdAsync(team.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(team);
        _factory.TeamRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<Team>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Team t, CancellationToken _) => t);

        var command = new AddTeamMemberCommand
        {
            TeamId            = team.Id,
            UserId            = "new-user-2",
            Username          = "NewMember",
            Role              = "Developer",
            RequestedByUserId = "user-1",
        };

        var response = await _client.PostAsJsonAsync($"/api/team/{team.Id}/members", command);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ─── PATCH /api/team/{id}/status ─────────────────────────────────────────

    [Fact]
    public async Task SetStatus_ValidRequest_Returns200()
    {
        var team = BuildTeam("Status Team");
        _factory.TeamRepoMock
            .Setup(r => r.GetByIdAsync(team.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(team);
        _factory.TeamRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<Team>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Team t, CancellationToken _) => t);

        var command = new SetTeamStatusCommand
        {
            TeamId = team.Id,
            Status = TeamStatus.Active,
            UserId = "user-1",
        };

        var response = await _client.PatchAsJsonAsync($"/api/team/{team.Id}/status", command);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

// Minimal DTO used only to deserialise the Team JSON response body in tests.
file sealed class TeamResponseBody
{
    public string Id   { get; set; } = null!;
    public string Name { get; set; } = null!;
}
