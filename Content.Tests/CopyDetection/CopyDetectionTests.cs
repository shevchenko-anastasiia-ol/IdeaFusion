using ContentAPI.CopyDetection;
using ContentBLL.DTO.Post;
using ContentBLL.Services.Interfaces;
using ContentDAL.Repository.Interfaces;
using ContentDAL.UOW;
using ContentDomain.Entity;
using FluentAssertions;
using Grpc.Core;
using IdeaFusion.Grpc.Teams;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Content.Tests.CopyDetection;

public class CopyCheckResultTests
{
    [Fact]
    public void WithMatches_RecomputesZoneFromBestRemainingMatch()
    {
        var result = new CopyCheckResult { Zone = CopyZones.Red, Score = 0.97, MatchType = "exact" };
        var remaining = new[]
        {
            new CopyMatch { MediaId = 2, PCopy = 0.4, Zone = CopyZones.Yellow, MatchType = "modified" },
            new CopyMatch { MediaId = 3, PCopy = 0.1, Zone = CopyZones.Green, MatchType = "none" },
        };

        var filtered = result.WithMatches(remaining);

        filtered.Zone.Should().Be(CopyZones.Yellow);
        filtered.Score.Should().Be(0.4);
        filtered.Best!.MediaId.Should().Be(2);
        result.WithMatches(Array.Empty<CopyMatch>()).Zone.Should().Be(CopyZones.Green);
    }
}

public class AuthorshipFilterTests
{
    private static AsyncUnaryCall<T> Call<T>(T response) =>
        new(Task.FromResult(response), Task.FromResult(new Metadata()), () => Status.DefaultSuccess, () => new Metadata(), () => { });

    private static CopyCheckResult Result(params CopyMatch[] matches) =>
        new CopyCheckResult().WithMatches(matches);

    [Fact]
    public async Task DropsOwnWorksAndWorksOfTeamsTheUploaderBelongsTo()
    {
        var teams = new Mock<TeamGrpcService.TeamGrpcServiceClient>();
        teams.Setup(t => t.CheckUserIsMemberAsync(It.Is<CheckUserIsMemberRequest>(r => r.TeamId == "my-team"),
                It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(Call(new CheckUserIsMemberResponse { IsMember = true }));
        teams.Setup(t => t.CheckUserIsMemberAsync(It.Is<CheckUserIsMemberRequest>(r => r.TeamId == "other-team"),
                It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(Call(new CheckUserIsMemberResponse { IsMember = false }));
        var filter = new AuthorshipFilter(teams.Object, NullLogger<AuthorshipFilter>.Instance);

        var result = await filter.ExcludeOwnWorksAsync(Result(
            new CopyMatch { MediaId = 1, OwnerUserId = "ME", PCopy = 0.99, Zone = CopyZones.Red },
            new CopyMatch { MediaId = 2, OwnerUserId = "x", TeamId = "my-team", PCopy = 0.95, Zone = CopyZones.Red },
            new CopyMatch { MediaId = 3, OwnerUserId = "y", TeamId = "other-team", PCopy = 0.6, Zone = CopyZones.Yellow }), "me");

        result.Matches.Select(m => m.MediaId).Should().Equal(3);
        result.Zone.Should().Be(CopyZones.Yellow);
    }

    [Fact]
    public async Task KeepsMatchWhenMembershipCannotBeChecked()
    {
        var teams = new Mock<TeamGrpcService.TeamGrpcServiceClient>();
        teams.Setup(t => t.CheckUserIsMemberAsync(It.IsAny<CheckUserIsMemberRequest>(),
                It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Throws(new RpcException(new Status(StatusCode.Unavailable, "down")));
        var filter = new AuthorshipFilter(teams.Object, NullLogger<AuthorshipFilter>.Instance);

        var result = await filter.ExcludeOwnWorksAsync(Result(
            new CopyMatch { MediaId = 5, OwnerUserId = "x", TeamId = "t", PCopy = 0.9, Zone = CopyZones.Red }), "me");

        result.Zone.Should().Be(CopyZones.Red);
    }
}

public class CopyJobProcessorTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IPostRepository> _posts = new();
    private readonly Mock<IImageCopyClient> _client = new();
    private readonly Mock<IAuthorshipFilter> _authorship = new();

    public CopyJobProcessorTests()
    {
        _uow.SetupGet(u => u.PostRepository).Returns(_posts.Object);
        _authorship.Setup(a => a.ExcludeOwnWorksAsync(It.IsAny<CopyCheckResult>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CopyCheckResult r, string _, CancellationToken _) => r);
    }

    private CopyJobProcessor Processor() =>
        new(_uow.Object, _client.Object, _authorship.Object, NullLogger<CopyJobProcessor>.Instance);

    private static Post TeamPost() => new()
    {
        PostId = 7,
        Status = PostStatus.Published,
        CreatedBy = "alice",
        Collaboration = new CollaborationSnapshot { CollaborationId = 3, ExternalId = "team-1", Name = "Team" },
        Media = new List<PostMedia>
        {
            new() { Id = 70, PostId = 7, Bucket = "posts", ObjectName = "media/a.png", ContentType = "image/png" },
            new() { Id = 71, PostId = 7, Bucket = "posts", ObjectName = "media/b.mp4", ContentType = "video/mp4" },
        },
    };

    [Fact]
    public async Task IndexPost_RecordsSuspiciousImagesAndIndexesEveryImage()
    {
        _posts.Setup(p => p.GetByIdAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(TeamPost());
        var suspicious = new CopyCheckResult().WithMatches(new[]
        {
            new CopyMatch { MediaId = 1, PostId = 1, OwnerUserId = "bob", PCopy = 0.98, Zone = CopyZones.Red, MatchType = "exact" },
        });
        _client.Setup(c => c.CheckObjectAsync("posts", "media/a.png", It.IsAny<IEnumerable<string>>(),
                It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(suspicious);

        await Processor().ProcessAsync(new CopyJob(CopyJobKind.IndexPost, 7), CancellationToken.None);

        _client.Verify(c => c.CheckObjectAsync("posts", "media/a.png",
            It.Is<IEnumerable<string>>(u => u.Single() == "username:alice"),
            It.Is<IEnumerable<string>>(t => t.Single() == "team-1"), It.IsAny<CancellationToken>()));
        _client.Verify(c => c.RecordCheckAsync(It.Is<CopyCheckRecordRequest>(r =>
            r.MediaId == 70 && r.TeamId == "team-1" && r.Result.Zone == CopyZones.Red), It.IsAny<CancellationToken>()), Times.Once);
        _client.Verify(c => c.IndexAsync(It.Is<CopyIndexRequest>(r => r.MediaId == 70 && r.TeamId == "team-1"),
            It.IsAny<CancellationToken>()), Times.Once);
        // Videos are neither checked nor indexed.
        _client.Verify(c => c.IndexAsync(It.Is<CopyIndexRequest>(r => r.MediaId == 71), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task IndexPost_GreenResultIsNotRecorded()
    {
        _posts.Setup(p => p.GetByIdAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(TeamPost());
        _client.Setup(c => c.CheckObjectAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>(),
                It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CopyCheckResult());

        await Processor().ProcessAsync(new CopyJob(CopyJobKind.IndexPost, 7), CancellationToken.None);

        _client.Verify(c => c.RecordCheckAsync(It.IsAny<CopyCheckRecordRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        _client.Verify(c => c.IndexAsync(It.IsAny<CopyIndexRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IndexPost_SkipsUnpublishedPosts()
    {
        var draft = TeamPost();
        draft.Status = PostStatus.Draft;
        _posts.Setup(p => p.GetByIdAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(draft);

        await Processor().ProcessAsync(new CopyJob(CopyJobKind.IndexPost, 7), CancellationToken.None);

        _client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RemovePost_DeletesFromIndex()
    {
        await Processor().ProcessAsync(new CopyJob(CopyJobKind.RemovePost, 9), CancellationToken.None);
        _client.Verify(c => c.DeletePostAsync(9, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void OwnerId_PrefersKeycloakSubject()
    {
        var post = new Post { CreatedBy = "alice", Author = new PostAuthor { ExternalId = "ABC-1", UserName = "alice" } };
        CopyJobProcessor.OwnerId(post).Should().Be("abc-1");
        CopyJobProcessor.OwnerId(new Post { CreatedBy = "bob" }).Should().Be("username:bob");
    }
}

public class CopyCheckingPostServiceTests
{
    private readonly Mock<IPostService> _inner = new();
    private readonly CopyCheckQueue _queue = new(10);

    private CopyCheckingPostService Service() =>
        new(_inner.Object, _queue, NullLogger<CopyCheckingPostService>.Instance);

    private async Task<List<CopyJob>> Drain()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        var jobs = new List<CopyJob>();
        try
        {
            await foreach (var job in _queue.ReadAllAsync(cts.Token))
                jobs.Add(job);
        }
        catch (OperationCanceledException) { }
        return jobs;
    }

    [Fact]
    public async Task Create_WithImages_QueuesIndexing()
    {
        _inner.Setup(s => s.CreateAsync(It.IsAny<PostCreateDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PostDto { PostId = 5 });
        var dto = new PostCreateDto { MediaFiles = new List<IFormFile> { new Mock<IFormFile>().Object } };

        var post = await Service().CreateAsync(dto);

        post.PostId.Should().Be(5);
        (await Drain()).Should().Equal(new CopyJob(CopyJobKind.IndexPost, 5));
    }

    [Fact]
    public async Task Create_WithoutMedia_QueuesNothing()
    {
        _inner.Setup(s => s.CreateAsync(It.IsAny<PostCreateDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PostDto { PostId = 5 });

        await Service().CreateAsync(new PostCreateDto());

        (await Drain()).Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteArchiveAndModerateDelete_RemoveFromIndex()
    {
        var service = Service();
        await service.DeleteAsync(1);
        await service.ArchiveAsync(2);
        await service.ModerateDeleteAsync(3);

        (await Drain()).Should().Equal(
            new CopyJob(CopyJobKind.RemovePost, 1),
            new CopyJob(CopyJobKind.RemovePost, 2),
            new CopyJob(CopyJobKind.RemovePost, 3));
    }

    [Fact]
    public async Task FailedCreate_QueuesNothing()
    {
        _inner.Setup(s => s.CreateAsync(It.IsAny<PostCreateDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("db down"));
        var dto = new PostCreateDto { MediaFiles = new List<IFormFile> { new Mock<IFormFile>().Object } };

        await Service().Invoking(s => s.CreateAsync(dto)).Should().ThrowAsync<InvalidOperationException>();
        (await Drain()).Should().BeEmpty();
    }
}

public class CopyDetectionRegistrationTests
{
    private sealed class FakePostService : IPostService
    {
        public Task<PostDto?> GetByIdAsync(int id, CancellationToken ct = default) => Task.FromResult<PostDto?>(null);
        public Task<IEnumerable<PostDto>> GetAllAsync(CancellationToken ct = default) => Task.FromResult(Enumerable.Empty<PostDto>());
        public Task<IEnumerable<PostDto>> GetByAuthorAsync(int postAuthorId, CancellationToken ct = default) => GetAllAsync(ct);
        public Task<IEnumerable<PostDto>> GetByUserIdAsync(int userId, CancellationToken ct = default) => GetAllAsync(ct);
        public Task<IEnumerable<PostDto>> GetByCollaborationAsync(int id, CancellationToken ct = default) => GetAllAsync(ct);
        public Task<IEnumerable<PostDto>> GetByTagAsync(int tagId, CancellationToken ct = default) => GetAllAsync(ct);
        public Task<IEnumerable<PostDto>> SearchAsync(string query, CancellationToken ct = default) => GetAllAsync(ct);
        public Task<PostDto> CreateAsync(PostCreateDto dto, CancellationToken ct = default) => Task.FromResult(new PostDto());
        public Task<PostDto> UpdateAsync(int id, PostUpdateDto dto, CancellationToken ct = default) => Task.FromResult(new PostDto());
        public Task DeleteAsync(int id, CancellationToken ct = default) => Task.CompletedTask;
        public Task ArchiveAsync(int id, CancellationToken ct = default) => Task.CompletedTask;
        public Task ModerateDeleteAsync(int id, CancellationToken ct = default) => Task.CompletedTask;
    }

    private static ServiceProvider Build(bool enabled)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["CopyDetection:Enabled"] = enabled.ToString() })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<IPostService, FakePostService>();
        services.AddCopyDetection(config);
        return services.BuildServiceProvider();
    }

    [Fact]
    public void Enabled_DecoratesPostService()
    {
        using var sp = Build(enabled: true);
        using var scope = sp.CreateScope();
        scope.ServiceProvider.GetRequiredService<IPostService>().Should().BeOfType<CopyCheckingPostService>();
        scope.ServiceProvider.GetRequiredService<IImageCopyClient>().Should().NotBeNull();
    }

    [Fact]
    public void Disabled_LeavesPostServiceAlone()
    {
        using var sp = Build(enabled: false);
        using var scope = sp.CreateScope();
        scope.ServiceProvider.GetRequiredService<IPostService>().Should().BeOfType<FakePostService>();
    }
}
