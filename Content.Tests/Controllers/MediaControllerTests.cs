using ContentAPI.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Minio;
using Minio.DataModel.Args;
using Moq;
using Xunit;

namespace Content.Tests.Controllers;

public class MediaControllerTests
{
    private readonly Mock<IMinioClient> _minioMock = new();

    private static IConfiguration BuildConfig(string bucket = "test-bucket", string endpoint = "localhost:9000") =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Minio:BucketName"] = bucket,
                ["Minio:Endpoint"]   = endpoint
            })
            .Build();

    private MediaController Ctrl(IConfiguration? cfg = null) =>
        new(_minioMock.Object, cfg ?? BuildConfig());

    private static Mock<IFormFile> MakeFile(
        string name        = "photo.jpg",
        long   size        = 1024,
        string contentType = "image/jpeg")
    {
        var file = new Mock<IFormFile>();
        file.Setup(f => f.FileName).Returns(name);
        file.Setup(f => f.Length).Returns(size);
        file.Setup(f => f.ContentType).Returns(contentType);
        file.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(new byte[size < 0 ? 0 : (int)size]));
        return file;
    }

    private void SetupMinioUpload(bool bucketExists = true)
    {
        _minioMock
            .Setup(m => m.BucketExistsAsync(It.IsAny<BucketExistsArgs>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(bucketExists);

        if (!bucketExists)
        {
            _minioMock
                .Setup(m => m.MakeBucketAsync(It.IsAny<MakeBucketArgs>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _minioMock
                .Setup(m => m.SetPolicyAsync(It.IsAny<SetPolicyArgs>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        }

        _minioMock
            .Setup(m => m.PutObjectAsync(It.IsAny<PutObjectArgs>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Minio.DataModel.Response.PutObjectResponse)null!);
    }

    // ── Null / empty file guard ────────────────────────────────────────────────

    [Fact]
    public async Task Upload_NullFile_ReturnsBadRequest()
    {
        var result = await Ctrl().Upload(null!, CancellationToken.None);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Upload_EmptyFile_ReturnsBadRequest()
    {
        var file   = MakeFile(size: 0);
        var result = await Ctrl().Upload(file.Object, CancellationToken.None);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Upload_NullFile_DoesNotCallMinio()
    {
        await Ctrl().Upload(null!, CancellationToken.None);

        _minioMock.Verify(
            m => m.PutObjectAsync(It.IsAny<PutObjectArgs>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── Bucket creation ────────────────────────────────────────────────────────

    [Fact]
    public async Task Upload_BucketAlreadyExists_SkipsMakeBucket()
    {
        SetupMinioUpload(bucketExists: true);

        await Ctrl().Upload(MakeFile().Object, CancellationToken.None);

        _minioMock.Verify(
            m => m.MakeBucketAsync(It.IsAny<MakeBucketArgs>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Upload_BucketNotExists_CreatesBucket()
    {
        SetupMinioUpload(bucketExists: false);

        await Ctrl().Upload(MakeFile().Object, CancellationToken.None);

        _minioMock.Verify(
            m => m.MakeBucketAsync(It.IsAny<MakeBucketArgs>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Upload_BucketNotExists_SetsPublicReadPolicy()
    {
        SetupMinioUpload(bucketExists: false);

        await Ctrl().Upload(MakeFile().Object, CancellationToken.None);

        _minioMock.Verify(
            m => m.SetPolicyAsync(It.IsAny<SetPolicyArgs>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── Successful upload ──────────────────────────────────────────────────────

    [Fact]
    public async Task Upload_ValidFile_CallsPutObject()
    {
        SetupMinioUpload();

        await Ctrl().Upload(MakeFile().Object, CancellationToken.None);

        _minioMock.Verify(
            m => m.PutObjectAsync(It.IsAny<PutObjectArgs>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Upload_ValidFile_ReturnsOk()
    {
        SetupMinioUpload();

        var result = await Ctrl().Upload(MakeFile().Object, CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Upload_ValidFile_UrlStartsWithConfiguredEndpoint()
    {
        SetupMinioUpload();

        var result = await Ctrl().Upload(MakeFile("img.jpg").Object, CancellationToken.None);

        var url = (string)((OkObjectResult)result.Result!).Value!;
        url.Should().StartWith("http://localhost:9000/test-bucket/media/");
    }

    [Fact]
    public async Task Upload_ValidFile_UrlContainsOriginalFileName()
    {
        SetupMinioUpload();

        var result = await Ctrl().Upload(MakeFile("my-photo.jpg").Object, CancellationToken.None);

        var url = (string)((OkObjectResult)result.Result!).Value!;
        url.Should().Contain("my-photo.jpg");
    }

    [Fact]
    public async Task Upload_ValidFile_UrlContainsBucketName()
    {
        SetupMinioUpload();

        var result = await Ctrl().Upload(MakeFile().Object, CancellationToken.None);

        var url = (string)((OkObjectResult)result.Result!).Value!;
        url.Should().Contain("test-bucket");
    }

    // ── Configuration defaults ────────────────────────────────────────────────

    [Fact]
    public async Task Upload_NoMinioConfig_UsesDefaultBucketNamePosts()
    {
        var emptyCfg = new ConfigurationBuilder().Build();
        var ctrl     = new MediaController(_minioMock.Object, emptyCfg);
        SetupMinioUpload();

        var result = await ctrl.Upload(MakeFile().Object, CancellationToken.None);

        var url = (string)((OkObjectResult)result.Result!).Value!;
        url.Should().Contain("/posts/");
    }

    [Fact]
    public async Task Upload_NoMinioConfig_UsesDefaultEndpointLocalhost9000()
    {
        var emptyCfg = new ConfigurationBuilder().Build();
        var ctrl     = new MediaController(_minioMock.Object, emptyCfg);
        SetupMinioUpload();

        var result = await ctrl.Upload(MakeFile().Object, CancellationToken.None);

        var url = (string)((OkObjectResult)result.Result!).Value!;
        url.Should().StartWith("http://localhost:9000/");
    }

    [Fact]
    public async Task Upload_EndpointWithTrailingSlash_SlashIsTrimmed()
    {
        var cfg  = BuildConfig(endpoint: "storage.example.com/");
        var ctrl = new MediaController(_minioMock.Object, cfg);
        SetupMinioUpload();

        var result = await ctrl.Upload(MakeFile().Object, CancellationToken.None);

        var url = (string)((OkObjectResult)result.Result!).Value!;
        url.Should().NotContain("//test-bucket");
    }
}
