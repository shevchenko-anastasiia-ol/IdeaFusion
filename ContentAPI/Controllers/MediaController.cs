using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Minio;
using Minio.DataModel.Args;

namespace ContentAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MediaController : ControllerBase
{
    private readonly IMinioClient _minio;
    private readonly string _bucket;
    private readonly string _endpoint;

    public MediaController(IMinioClient minio, IConfiguration cfg)
    {
        _minio    = minio;
        _bucket   = cfg["Minio:BucketName"] ?? "posts";
        _endpoint = (cfg["Minio:Endpoint"] ?? "localhost:9000").TrimEnd('/');
    }

    [HttpPost("upload")]
    [RequestSizeLimit(52_428_800)]
    public async Task<ActionResult<string>> Upload(IFormFile file, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file provided.");

        var bucketExists = await _minio.BucketExistsAsync(
            new BucketExistsArgs().WithBucket(_bucket), ct);
        if (!bucketExists)
        {
            await _minio.MakeBucketAsync(new MakeBucketArgs().WithBucket(_bucket), ct);
            var policy = $$"""{"Version":"2012-10-17","Statement":[{"Effect":"Allow","Principal":{"AWS":["*"]},"Action":["s3:GetObject"],"Resource":["arn:aws:s3:::{{_bucket}}/*"]}]}""";
            await _minio.SetPolicyAsync(new SetPolicyArgs().WithBucket(_bucket).WithPolicy(policy), ct);
        }

        // The original name stays in the message; the object key is only a guid and the
        // extension, so spaces or Cyrillic in a file name never end up in a URL.
        var ext = Path.GetExtension(file.FileName);
        if (ext.Length > 10 || ext.Any(c => !char.IsLetterOrDigit(c) && c != '.')) ext = "";
        var objectName  = $"media/{Guid.NewGuid()}{ext.ToLowerInvariant()}";
        var contentType = string.IsNullOrEmpty(file.ContentType) ? "application/octet-stream" : file.ContentType;

        await _minio.PutObjectAsync(new PutObjectArgs()
            .WithBucket(_bucket)
            .WithObject(objectName)
            .WithStreamData(file.OpenReadStream())
            .WithObjectSize(file.Length)
            .WithContentType(contentType), ct);

        var url = $"http://{_endpoint}/{_bucket}/{objectName}";
        return Ok(url);
    }
}