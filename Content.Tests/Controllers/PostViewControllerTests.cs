using ContentAPI.Controllers;
using ContentBLL.Services.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Content.Tests.Controllers;

public class PostViewControllerTests
{
    private readonly Mock<IPostViewService> _serviceMock = new();
    private PostViewController Ctrl() => new(_serviceMock.Object);

    // ── Record ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Record_ValidArgs_ReturnsNoContent()
    {
        _serviceMock
            .Setup(s => s.RecordAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await Ctrl().Record(1, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Record_PassesPostIdToService()
    {
        _serviceMock
            .Setup(s => s.RecordAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await Ctrl().Record(10, CancellationToken.None);

        _serviceMock.Verify(
            s => s.RecordAsync(10, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Record_CallsServiceExactlyOnce()
    {
        _serviceMock
            .Setup(s => s.RecordAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await Ctrl().Record(1, CancellationToken.None);

        _serviceMock.Verify(
            s => s.RecordAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── Count ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Count_ValidPostId_ReturnsOk()
    {
        _serviceMock
            .Setup(s => s.CountByPostAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        var result = await Ctrl().Count(1, CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Count_ValidPostId_ReturnsExpectedCount()
    {
        _serviceMock
            .Setup(s => s.CountByPostAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);

        var result = await Ctrl().Count(7, CancellationToken.None);

        var ok = (OkObjectResult)result.Result!;
        ok.Value.Should().Be(42);
    }

    [Fact]
    public async Task Count_ZeroViews_ReturnsZero()
    {
        _serviceMock
            .Setup(s => s.CountByPostAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var result = await Ctrl().Count(2, CancellationToken.None);

        var ok = (OkObjectResult)result.Result!;
        ok.Value.Should().Be(0);
    }

    [Fact]
    public async Task Count_PassesPostIdToService()
    {
        _serviceMock
            .Setup(s => s.CountByPostAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        await Ctrl().Count(55, CancellationToken.None);

        _serviceMock.Verify(
            s => s.CountByPostAsync(55, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Count_CallsServiceExactlyOnce()
    {
        _serviceMock
            .Setup(s => s.CountByPostAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        await Ctrl().Count(1, CancellationToken.None);

        _serviceMock.Verify(
            s => s.CountByPostAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
