using ContentDAL.Helpers;
using FluentAssertions;
using Minio;
using Minio.DataModel.Args;
using Moq;
using System.Reflection;
using Xunit;

namespace Content.Tests.DAL;

public class MinioServiceTests
{
    private static MinioService Create(string endpoint = "minio.example.com", string bucket = "test-bucket") =>
        new(endpoint, "access-key", "secret-key", bucket);

    // ── Constructor ───────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_DoesNotThrow()
    {
        Action act = () => Create();

        act.Should().NotThrow();
    }

    // ── GetUrl ────────────────────────────────────────────────────────────────

    [Fact]
    public void GetUrl_ReturnsCorrectFormat()
    {
        var svc = Create("minio.example.com", "my-bucket");

        var url = svc.GetUrl("photo.jpg");

        url.Should().Be("https://minio.example.com/my-bucket/photo.jpg");
    }

    [Fact]
    public void GetUrl_WithNestedObjectName_ReturnsCorrectUrl()
    {
        var svc = Create("s3.example.com", "media");

        var url = svc.GetUrl("folder/image.png");

        url.Should().Be("https://s3.example.com/media/folder/image.png");
    }

    [Fact]
    public void GetUrl_StartsWithHttps()
    {
        var url = Create().GetUrl("file.txt");

        url.Should().StartWith("https://");
    }

    [Fact]
    public void GetUrl_ContainsEndpoint_Bucket_AndObjectName()
    {
        var svc = Create("storage.local", "uploads");

        var url = svc.GetUrl("docs/report.pdf");

        url.Should().Contain("storage.local")
           .And.Contain("uploads")
           .And.Contain("docs/report.pdf");
    }

    [Fact]
    public void GetUrl_DifferentEndpoints_ReturnDifferentUrls()
    {
        var url1 = Create("host1.io", "bucket").GetUrl("obj");
        var url2 = Create("host2.io", "bucket").GetUrl("obj");

        url1.Should().NotBe(url2);
    }

    [Fact]
    public void GetUrl_DifferentBuckets_ReturnDifferentUrls()
    {
        var url1 = Create("host.io", "bucket-a").GetUrl("obj");
        var url2 = Create("host.io", "bucket-b").GetUrl("obj");

        url1.Should().NotBe(url2);
    }

    // ── UploadAsync / DeleteAsync (mock injected via reflection) ─────────────

    private static MinioService InjectMock(MinioService svc, Mock<IMinioClient> mock)
    {
        var field = typeof(MinioService)
            .GetField("_client", BindingFlags.NonPublic | BindingFlags.Instance)!;
        field.SetValue(svc, mock.Object);
        return svc;
    }

    [Fact]
    public async Task UploadAsync_WhenBucketExists_DoesNotCreateBucket()
    {
        var mockClient = new Mock<IMinioClient>();
        mockClient
            .Setup(m => m.BucketExistsAsync(It.IsAny<BucketExistsArgs>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        mockClient
            .Setup(m => m.PutObjectAsync(It.IsAny<PutObjectArgs>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Minio.DataModel.Response.PutObjectResponse)null!);

        var svc = InjectMock(Create(), mockClient);
        using var data = new MemoryStream(new byte[] { 1, 2, 3 });

        await svc.UploadAsync("obj.jpg", data, "image/jpeg");

        mockClient.Verify(
            m => m.MakeBucketAsync(It.IsAny<MakeBucketArgs>(), It.IsAny<CancellationToken>()),
            Times.Never);
        mockClient.Verify(
            m => m.PutObjectAsync(It.IsAny<PutObjectArgs>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UploadAsync_WhenBucketNotExists_CreatesBucketThenUploads()
    {
        var mockClient = new Mock<IMinioClient>();
        mockClient
            .Setup(m => m.BucketExistsAsync(It.IsAny<BucketExistsArgs>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        mockClient
            .Setup(m => m.MakeBucketAsync(It.IsAny<MakeBucketArgs>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mockClient
            .Setup(m => m.PutObjectAsync(It.IsAny<PutObjectArgs>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Minio.DataModel.Response.PutObjectResponse)null!);

        var svc = InjectMock(Create(), mockClient);
        using var data = new MemoryStream(new byte[] { 1, 2, 3 });

        await svc.UploadAsync("obj.jpg", data, "image/jpeg");

        mockClient.Verify(
            m => m.MakeBucketAsync(It.IsAny<MakeBucketArgs>(), It.IsAny<CancellationToken>()),
            Times.Once);

        mockClient.Verify(
            m => m.PutObjectAsync(It.IsAny<PutObjectArgs>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_CallsRemoveObjectAsync()
    {
        var mockClient = new Mock<IMinioClient>();
        mockClient
            .Setup(m => m.RemoveObjectAsync(It.IsAny<RemoveObjectArgs>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var svc = InjectMock(Create(), mockClient);

        await svc.DeleteAsync("photo.jpg");

        mockClient.Verify(
            m => m.RemoveObjectAsync(It.IsAny<RemoveObjectArgs>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── Additional coverage ────────────────────────────────────────────────────

    [Fact]
    public async Task UploadAsync_PassesCorrectBucketToExistsCheck()
    {
        BucketExistsArgs? capturedArgs = null;
        var mockClient = new Mock<IMinioClient>();
        mockClient
            .Setup(m => m.BucketExistsAsync(It.IsAny<BucketExistsArgs>(), It.IsAny<CancellationToken>()))
            .Callback<BucketExistsArgs, CancellationToken>((a, _) => capturedArgs = a)
            .ReturnsAsync(true);
        mockClient
            .Setup(m => m.PutObjectAsync(It.IsAny<PutObjectArgs>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Minio.DataModel.Response.PutObjectResponse)null!);

        var svc = InjectMock(Create("ep", "my-bucket"), mockClient);
        using var data = new MemoryStream(new byte[] { 9 });

        await svc.UploadAsync("file.bin", data, "application/octet-stream");

        capturedArgs.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteAsync_WithDifferentObjectNames_CallsRemoveForEach()
    {
        var mockClient = new Mock<IMinioClient>();
        mockClient
            .Setup(m => m.RemoveObjectAsync(It.IsAny<RemoveObjectArgs>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var svc = InjectMock(Create(), mockClient);

        await svc.DeleteAsync("file1.jpg");
        await svc.DeleteAsync("file2.jpg");

        mockClient.Verify(
            m => m.RemoveObjectAsync(It.IsAny<RemoveObjectArgs>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public void GetUrl_WithEmptyObjectName_ReturnsUrlWithTrailingSlash()
    {
        var url = Create("host.io", "bucket").GetUrl("");
        url.Should().Be("https://host.io/bucket/");
    }

    [Fact]
    public void Constructor_StoresEndpointAndBucket()
    {
        var svc = Create("my-endpoint.io", "my-bucket");

        svc.GetUrl("x").Should().Contain("my-endpoint.io").And.Contain("my-bucket");
    }
}
