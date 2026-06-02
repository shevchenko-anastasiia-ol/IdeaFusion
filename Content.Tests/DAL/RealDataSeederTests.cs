using ContentDAL.Data;
using ContentDomain.Entity;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel.Args;
using Moq;
using System.Reflection;
using Xunit;

namespace Content.Tests.DAL;

public class RealDataSeederTests : IDisposable
{
    private readonly ContentDbContext _db;
    private readonly Mock<IMinioClient> _minioMock = new();
    private readonly Mock<ILogger<RealDataSeeder>> _loggerMock = new();

    public RealDataSeederTests()
    {
        var options = new DbContextOptionsBuilder<ContentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ContentDbContext(options);
    }

    public void Dispose() => _db.Dispose();

    private RealDataSeeder Seeder() =>
        new(_db, _minioMock.Object, _loggerMock.Object);

    // ── SeedAsync — bucket already exists ────────────────────────────────────

    [Fact]
    public async Task SeedAsync_WhenBucketExists_CallsBucketExistsAsync()
    {
        _minioMock
            .Setup(m => m.BucketExistsAsync(It.IsAny<BucketExistsArgs>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await Seeder().SeedAsync();

        _minioMock.Verify(
            m => m.BucketExistsAsync(It.IsAny<BucketExistsArgs>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SeedAsync_WhenBucketExists_DoesNotCallMakeBucket()
    {
        _minioMock
            .Setup(m => m.BucketExistsAsync(It.IsAny<BucketExistsArgs>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await Seeder().SeedAsync();

        _minioMock.Verify(
            m => m.MakeBucketAsync(It.IsAny<MakeBucketArgs>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── SeedAsync — bucket does not exist ─────────────────────────────────────

    [Fact]
    public async Task SeedAsync_WhenBucketNotExists_CreatesBucket()
    {
        _minioMock
            .Setup(m => m.BucketExistsAsync(It.IsAny<BucketExistsArgs>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _minioMock
            .Setup(m => m.MakeBucketAsync(It.IsAny<MakeBucketArgs>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _minioMock
            .Setup(m => m.SetPolicyAsync(It.IsAny<SetPolicyArgs>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await Seeder().SeedAsync();

        _minioMock.Verify(
            m => m.MakeBucketAsync(It.IsAny<MakeBucketArgs>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SeedAsync_WhenBucketNotExists_SetsPublicReadPolicy()
    {
        _minioMock
            .Setup(m => m.BucketExistsAsync(It.IsAny<BucketExistsArgs>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _minioMock
            .Setup(m => m.MakeBucketAsync(It.IsAny<MakeBucketArgs>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _minioMock
            .Setup(m => m.SetPolicyAsync(It.IsAny<SetPolicyArgs>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await Seeder().SeedAsync();

        _minioMock.Verify(
            m => m.SetPolicyAsync(It.IsAny<SetPolicyArgs>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── SeedAsync — MinIO failure is swallowed ────────────────────────────────

    [Fact]
    public async Task SeedAsync_WhenMinioThrows_DoesNotRethrow()
    {
        _minioMock
            .Setup(m => m.BucketExistsAsync(It.IsAny<BucketExistsArgs>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("MinIO unreachable"));

        Func<Task> act = () => Seeder().SeedAsync();

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SeedAsync_WhenMinioThrows_LogsWarning()
    {
        _minioMock
            .Setup(m => m.BucketExistsAsync(It.IsAny<BucketExistsArgs>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("MinIO unreachable"));

        await Seeder().SeedAsync();

        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // ── Logging ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task SeedAsync_LogsStartAndDoneMessages()
    {
        _minioMock
            .Setup(m => m.BucketExistsAsync(It.IsAny<BucketExistsArgs>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await Seeder().SeedAsync();

        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("start")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("done")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // ── GenerateSvg (private static) via reflection ───────────────────────────

    private static byte[] InvokeGenerateSvg(string label, string c1, string c2)
    {
        var method = typeof(RealDataSeeder)
            .GetMethod("GenerateSvg", BindingFlags.NonPublic | BindingFlags.Static)!;
        return (byte[])method.Invoke(null, new object[] { label, c1, c2 })!;
    }

    [Fact]
    public void GenerateSvg_ShortLabel_ReturnsSvgBytes()
    {
        var bytes = InvokeGenerateSvg("Hello", "#7c5cfc", "#3fcca0");
        bytes.Should().NotBeEmpty();
        System.Text.Encoding.UTF8.GetString(bytes).Should().Contain("<svg");
    }

    [Fact]
    public void GenerateSvg_LongLabel_IncludesSecondTextLine()
    {
        var svg = System.Text.Encoding.UTF8.GetString(
            InvokeGenerateSvg("This Is A Very Long Title Here", "#7c5cfc", "#3fcca0"));
        // words.Length > 4 → second <text> element generated
        svg.Should().Contain("<svg");
        svg.Should().NotBeEmpty();
    }

    [Fact]
    public void GenerateSvg_LabelWithAmpersand_EscapesCorrectly()
    {
        var svg = System.Text.Encoding.UTF8.GetString(
            InvokeGenerateSvg("& rocks", "#aaa", "#bbb"));
        svg.Should().Contain("&amp;");
    }

    [Fact]
    public void GenerateSvg_LabelWithLessThan_EscapesCorrectly()
    {
        var svg = System.Text.Encoding.UTF8.GetString(
            InvokeGenerateSvg("< value", "#aaa", "#bbb"));
        svg.Should().Contain("&lt;");
    }

    [Fact]
    public void GenerateSvg_LabelWithGreaterThan_EscapesCorrectly()
    {
        var svg = System.Text.Encoding.UTF8.GetString(
            InvokeGenerateSvg("> value", "#aaa", "#bbb"));
        svg.Should().Contain("&gt;");
    }

    [Fact]
    public void GenerateSvg_LabelWithQuote_EscapesCorrectly()
    {
        var svg = System.Text.Encoding.UTF8.GetString(
            InvokeGenerateSvg("\"Hi\" there", "#aaa", "#bbb"));
        svg.Should().Contain("&quot;");
    }

    // ── ObjectExistsAsync (private) via reflection ────────────────────────────

    private async Task<bool> InvokeObjectExistsAsync(string objectName)
    {
        var method = typeof(RealDataSeeder)
            .GetMethod("ObjectExistsAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;
        return await (Task<bool>)method.Invoke(Seeder(), new object[] { objectName })!;
    }

    [Fact]
    public async Task ObjectExistsAsync_WhenStatObjectSucceeds_ReturnsTrue()
    {
        _minioMock
            .Setup(m => m.StatObjectAsync(It.IsAny<StatObjectArgs>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Minio.DataModel.ObjectStat)null!);

        var exists = await InvokeObjectExistsAsync("seed/obj.svg");
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ObjectExistsAsync_WhenStatObjectThrows_ReturnsFalse()
    {
        _minioMock
            .Setup(m => m.StatObjectAsync(It.IsAny<StatObjectArgs>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("object not found"));

        var exists = await InvokeObjectExistsAsync("seed/missing.svg");
        exists.Should().BeFalse();
    }

    // ── UploadSvgAsync (private) via reflection ───────────────────────────────

    private async Task<string> InvokeUploadSvgAsync(string objectName, string label, string c1, string c2)
    {
        var method = typeof(RealDataSeeder)
            .GetMethod("UploadSvgAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;
        return await (Task<string>)method.Invoke(Seeder(), new object[] { objectName, label, c1, c2 })!;
    }

    [Fact]
    public async Task UploadSvgAsync_WhenPutObjectSucceeds_ReturnsObjectName()
    {
        _minioMock
            .Setup(m => m.PutObjectAsync(It.IsAny<PutObjectArgs>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Minio.DataModel.Response.PutObjectResponse)null!);

        var result = await InvokeUploadSvgAsync("seed/test.svg", "Test Label", "#7c5cfc", "#3fcca0");
        result.Should().Be("seed/test.svg");
    }

    [Fact]
    public async Task UploadSvgAsync_WhenPutObjectThrows_SwallowsExceptionAndReturnsObjectName()
    {
        _minioMock
            .Setup(m => m.PutObjectAsync(It.IsAny<PutObjectArgs>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("MinIO unavailable"));

        var result = await InvokeUploadSvgAsync("seed/fail.svg", "Fail Label", "#aaa", "#bbb");
        result.Should().Be("seed/fail.svg");
    }

    // ── BuildTagMapAsync (private) via reflection ─────────────────────────────

    [Fact]
    public async Task BuildTagMapAsync_WhenNoTags_ReturnsEmptyDictionary()
    {
        var method = typeof(RealDataSeeder)
            .GetMethod("BuildTagMapAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var result = await (Task<Dictionary<string, int>>)method.Invoke(Seeder(), null)!;
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task BuildTagMapAsync_WhenTagsExist_ReturnsDictionaryByName()
    {
        _db.Tags.AddRange(
            new Tag { Name = "music" },
            new Tag { Name = "art" });
        await _db.SaveChangesAsync();

        var method = typeof(RealDataSeeder)
            .GetMethod("BuildTagMapAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var result = await (Task<Dictionary<string, int>>)method.Invoke(Seeder(), null)!;

        result.Should().ContainKey("music");
        result.Should().ContainKey("art");
    }

    // ── ReuploadMissingImagesAsync (private) via reflection ───────────────────

    [Fact]
    public async Task ReuploadMissingImagesAsync_WhenObjectsMissing_UploadsAll()
    {
        // StatObject throws → ObjectExistsAsync returns false → UploadSvgAsync called
        _minioMock
            .Setup(m => m.StatObjectAsync(It.IsAny<StatObjectArgs>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("not found"));
        _minioMock
            .Setup(m => m.PutObjectAsync(It.IsAny<PutObjectArgs>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Minio.DataModel.Response.PutObjectResponse)null!);

        var method = typeof(RealDataSeeder)
            .GetMethod("ReuploadMissingImagesAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var act = async () => await (Task)method.Invoke(Seeder(), null)!;
        await act.Should().NotThrowAsync();

        _minioMock.Verify(
            m => m.PutObjectAsync(It.IsAny<PutObjectArgs>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task ReuploadMissingImagesAsync_WhenObjectsExist_SkipsUpload()
    {
        // StatObject succeeds → ObjectExistsAsync returns true → UploadSvgAsync not called
        _minioMock
            .Setup(m => m.StatObjectAsync(It.IsAny<StatObjectArgs>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Minio.DataModel.ObjectStat)null!);

        var method = typeof(RealDataSeeder)
            .GetMethod("ReuploadMissingImagesAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;
        await (Task)method.Invoke(Seeder(), null)!;

        _minioMock.Verify(
            m => m.PutObjectAsync(It.IsAny<PutObjectArgs>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
