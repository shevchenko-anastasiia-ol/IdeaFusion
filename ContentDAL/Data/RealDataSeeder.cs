using ContentDomain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel.Args;
using System.Text;

namespace ContentDAL.Data;

/// <summary>
/// Seeds real user posts and collaboration posts with MinIO images.
/// Runs after ContentDbSeed. Fully idempotent — checks by UserName / Name before inserting.
/// </summary>
public class RealDataSeeder
{
    private readonly ContentDbContext _context;
    private readonly IMinioClient _minio;
    private readonly ILogger<RealDataSeeder> _logger;

    private const string Bucket = "posts";

    // Real Identity users: content-side int UserId, Identity UserName
    private static readonly (int UserId, string UserName, string[] TagNames, (string Title, string Desc)[] Posts)[] Users =
    [
        (204, "Admin", ["photography", "street-art", "film"],
         [
             ("Вуличні портрети: серія 1",             "Знайомтесь із людьми Подолу через об'єктив."),
             ("Нічна зйомка: техніки та налаштування", "ISO, витримка, баланс білого — практичний гайд."),
             ("Пейзажі Київщини",                      "30 кадрів за один вихідний. Осінь 2025."),
         ]),
    ];

    // Real collaboration snapshots matching DatabaseSeeder team names
    private static readonly (int CollabId, string Name, (string Title, string Desc, string[] TagNames)[] Posts)[] Collabs =
    [
        (201, "Visual Stories",
         [
             ("Спільний фотопроєкт: міські пейзажі Києва", "Команда знімала вуличні сцени по всьому місту.", ["photography", "art"]),
             ("Комерційна зйомка для Kyiv Coffee Brand",    "Behind the scenes рекламного проєкту.",          ["photography", "branding"]),
         ]),
        (202, "Motion & Design Lab",
         [
             ("Анімований брендинг для стартапу: case study", "Від логотипу до motion-гайду за 3 тижні.",  ["animation", "branding"]),
             ("UI Motion Kit — Open Source Release",          "Бібліотека мікроанімацій для Figma і AE.",   ["animation", "ui-ux"]),
         ]),
        (203, "Soundwave Collective",
         [
             ("Запис EP у студії Soundwave: студійні нотатки", "Процес запису першого спільного релізу.", ["music", "wip"]),
         ]),
        (204, "Indie Game Dev Crew",
         [
             ("Pixel Art Assets — перший дропон гри", "Тайлсет та персонажі у стилі 16-bit.", ["gamedev", "art"]),
         ]),
    ];

    // Gradient pairs for SVG image generation
    private static readonly (string A, string B)[] Gradients =
    [
        ("#7c5cfc", "#3fcca0"), ("#f87c6b", "#ffb347"), ("#29b6f6", "#7c5cfc"),
        ("#3fcca0", "#29b6f6"), ("#e040fb", "#7c5cfc"), ("#ff6b6b", "#f87c6b"),
        ("#ffb347", "#3fcca0"), ("#29b6f6", "#e040fb"), ("#7c5cfc", "#ff6b6b"),
        ("#3fcca0", "#e040fb"), ("#29b6f6", "#ffb347"), ("#e040fb", "#3fcca0"),
    ];

    public RealDataSeeder(
        ContentDbContext context,
        IMinioClient minio,
        ILogger<RealDataSeeder> logger)
    {
        _context = context;
        _minio   = minio;
        _logger  = logger;
    }

    public async Task SeedAsync()
    {
        _logger.LogInformation("=== RealDataSeeder: start ===");
        await EnsureBucketAsync();
        _logger.LogInformation("=== RealDataSeeder: done (post seeding disabled) ===");
    }

    // Re-uploads all deterministic seed SVGs if the objects are missing from MinIO.
    // Called when DB records already exist to recover from a MinIO data loss.
    private async Task ReuploadMissingImagesAsync()
    {
        var gradIdx = 0;
        foreach (var (_, userName, _, userPosts) in Users)
        {
            foreach (var (title, _) in userPosts)
            {
                var objectName = $"seed/{userName.ToLower()}_{gradIdx % Gradients.Length}.svg";
                var grad = Gradients[gradIdx % Gradients.Length];
                gradIdx++;
                if (!await ObjectExistsAsync(objectName))
                    await UploadSvgAsync(objectName, title, grad.A, grad.B);
            }
        }
        foreach (var (collabId, collabName, collabPosts) in Collabs)
        {
            foreach (var (_, _, _) in collabPosts)
            {
                var objectName = $"seed/collab_{collabId}_{gradIdx % Gradients.Length}.svg";
                var grad = Gradients[gradIdx % Gradients.Length];
                gradIdx++;
                if (!await ObjectExistsAsync(objectName))
                    await UploadSvgAsync(objectName, collabName, grad.A, grad.B);
            }
        }
    }

    private async Task<bool> ObjectExistsAsync(string objectName)
    {
        try
        {
            await _minio.StatObjectAsync(new Minio.DataModel.Args.StatObjectArgs()
                .WithBucket(Bucket)
                .WithObject(objectName));
            return true;
        }
        catch
        {
            return false;
        }
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────

    private async Task EnsureBucketAsync()
    {
        try
        {
            var exists = await _minio.BucketExistsAsync(new BucketExistsArgs().WithBucket(Bucket));
            if (!exists)
            {
                await _minio.MakeBucketAsync(new MakeBucketArgs().WithBucket(Bucket));
                var policy = $$"""{"Version":"2012-10-17","Statement":[{"Effect":"Allow","Principal":{"AWS":["*"]},"Action":["s3:GetObject"],"Resource":["arn:aws:s3:::{{Bucket}}/*"]}]}""";
                await _minio.SetPolicyAsync(new SetPolicyArgs().WithBucket(Bucket).WithPolicy(policy));
                _logger.LogInformation("RealDataSeeder: created bucket '{Bucket}' with public-read policy", Bucket);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "RealDataSeeder: could not ensure MinIO bucket — images will be skipped");
        }
    }

    private async Task<string> UploadSvgAsync(string objectName, string label, string color1, string color2)
    {
        try
        {
            var bytes = GenerateSvg(label, color1, color2);
            using var ms = new MemoryStream(bytes);
            await _minio.PutObjectAsync(new PutObjectArgs()
                .WithBucket(Bucket)
                .WithObject(objectName)
                .WithStreamData(ms)
                .WithObjectSize(bytes.Length)
                .WithContentType("image/svg+xml"));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "RealDataSeeder: MinIO upload failed for '{Object}'", objectName);
        }
        return objectName;
    }

    private async Task<Dictionary<string, int>> BuildTagMapAsync()
    {
        var tags = await _context.Tags.ToListAsync();
        return tags.ToDictionary(t => t.Name, t => t.TagId, StringComparer.OrdinalIgnoreCase);
    }

    private static byte[] GenerateSvg(string label, string c1, string c2)
    {
        // Escape XML special characters
        label = label.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

        // Split long labels across two lines
        var words  = label.Split(' ');
        var half   = words.Length / 2;
        var line1  = string.Join(' ', words[..half]);
        var line2  = string.Join(' ', words[half..]);

        var svg = $"""
            <svg xmlns="http://www.w3.org/2000/svg" width="800" height="500">
              <defs>
                <linearGradient id="g" x1="0%" y1="0%" x2="100%" y2="100%">
                  <stop offset="0%"   stop-color="{c1}"/>
                  <stop offset="100%" stop-color="{c2}"/>
                </linearGradient>
                <filter id="blur"><feGaussianBlur stdDeviation="60"/></filter>
              </defs>
              <rect width="800" height="500" fill="url(#g)"/>
              <circle cx="150" cy="150" r="180" fill="{c2}" opacity="0.3" filter="url(#blur)"/>
              <circle cx="650" cy="380" r="160" fill="{c1}" opacity="0.3" filter="url(#blur)"/>
              <rect x="30" y="30" width="740" height="440" rx="24" fill="rgba(0,0,0,0.18)"/>
              <text x="400" y="{(words.Length > 4 ? 230 : 265)}"
                    text-anchor="middle" font-family="Inter,Arial,sans-serif"
                    font-size="38" font-weight="700" fill="rgba(255,255,255,0.95)">{line1}</text>
              {(words.Length > 4 ? $"""<text x="400" y="285" text-anchor="middle" font-family="Inter,Arial,sans-serif" font-size="38" font-weight="700" fill="rgba(255,255,255,0.95)">{line2}</text>""" : "")}
            </svg>
            """;

        return Encoding.UTF8.GetBytes(svg);
    }
}