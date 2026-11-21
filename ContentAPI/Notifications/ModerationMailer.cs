using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Channels;
using ContentDAL.Data;
using GrpcClients.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ContentAPI.Notifications;

/// <summary>
/// SMTP settings for ContentService's own letters (the "Email" section; AppHost passes the same
/// Gmail account as for Keycloak and Collaboration). Without a host and a user nothing is sent.
/// </summary>
public sealed class ContentEmailOptions
{
    public const string Section = "Email";

    public string? Host { get; set; }
    public int Port { get; set; } = 587;
    public string? User { get; set; }
    public string? Password { get; set; }
    public string? From { get; set; }
    public string FromName { get; set; } = "IdeaFusion";
    public bool EnableSsl { get; set; } = true;
    public string FrontendUrl { get; set; } = "http://localhost:5173";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Host) && !string.IsNullOrWhiteSpace(User);
}

public sealed record ModerationLetter(string Email, string? Name, string Subject, string Heading, string Text, string ActionLabel, string ActionPath)
{
    /// <summary>The small label above the heading.</summary>
    public string Brand { get; init; } = "IdeaFusion · модерація";
    /// <summary>The grey line under the letter.</summary>
    public string Footer { get; init; } = "Службовий лист щодо вашої роботи в IdeaFusion.";
    /// <summary>An optional quoted text (a comment), shown under the main line.</summary>
    public string? Quote { get; init; }
}

/// <summary>Letters waiting to be sent; the request that caused one never waits for SMTP.</summary>
public sealed class ModerationMailQueue
{
    private readonly Channel<ModerationLetter> _channel = Channel.CreateBounded<ModerationLetter>(
        new BoundedChannelOptions(500) { FullMode = BoundedChannelFullMode.DropOldest, SingleReader = true });

    public void Enqueue(ModerationLetter letter) => _channel.Writer.TryWrite(letter);
    public IAsyncEnumerable<ModerationLetter> ReadAllAsync(CancellationToken ct) => _channel.Reader.ReadAllAsync(ct);
}

/// <summary>
/// Moderation letters to authors. Service messages: unlike the social notifications they are
/// sent regardless of the author's email settings, because they concern their own work.
/// </summary>
public interface IModerationMailer
{
    /// <summary>
    /// Tells whoever published the post that it was found to be a copy and will be removed.
    /// Call it inside the moderator's request: the author's address is read from Identity with
    /// that request's token.
    /// </summary>
    Task NotifyCopyConfirmedAsync(int postId, CancellationToken ct);
}

public sealed class ModerationMailer(
    ContentDbContext db,
    IUserGrpcClient users,
    ModerationMailQueue queue,
    ILogger<ModerationMailer> logger) : IModerationMailer
{
    public async Task NotifyCopyConfirmedAsync(int postId, CancellationToken ct)
    {
        try
        {
            var post = await db.Posts.AsNoTracking()
                .Where(p => p.PostId == postId)
                .Select(p => new { p.Title, AuthorId = p.Author != null ? p.Author.ExternalId : null, p.CreatedBy })
                .FirstOrDefaultAsync(ct);
            if (post is null) return;

            // A personal post names its author; a team post names whoever published it.
            var ownerId = post.AuthorId;
            if (string.IsNullOrWhiteSpace(ownerId) && !string.IsNullOrWhiteSpace(post.CreatedBy))
                ownerId = await db.PostAuthors.AsNoTracking()
                    .Where(a => a.UserName == post.CreatedBy && a.ExternalId != null)
                    .Select(a => a.ExternalId)
                    .FirstOrDefaultAsync(ct);
            if (!Guid.TryParse(ownerId, out var id))
            {
                logger.LogWarning("Post {PostId} was confirmed a copy, but its author has no identity id to write to", postId);
                return;
            }

            var user = await users.GetUserByIdAsync(id, ct);
            if (user is null || string.IsNullOrWhiteSpace(user.Email))
            {
                logger.LogWarning("Post {PostId} was confirmed a copy, but no email address was found for {UserId}", postId, ownerId);
                return;
            }

            queue.Enqueue(new ModerationLetter(
                user.Email, user.UserName,
                $"Ваш пост «{post.Title}» визнано копією",
                "Пост визнано копією іншої роботи",
                $"Модератор перевірив ваш пост «{post.Title}» і визнав, що зображення в ньому повторює роботу іншого автора. " +
                "Пост буде видалено найближчим часом. Якщо це ваша робота або у вас є дозвіл автора, напишіть модераторам, " +
                "доки пост ще не видалено.",
                "Переглянути пост", $"/posts/{postId}"));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Could not prepare the copy notice for post {PostId}", postId);
        }
    }
}

/// <summary>Sends queued moderation letters one by one; a failed letter is logged and dropped.</summary>
public sealed class ModerationMailSender(
    ModerationMailQueue queue,
    IOptions<ContentEmailOptions> options,
    ILogger<ModerationMailSender> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var o = options.Value;
        if (!o.IsConfigured)
            logger.LogWarning("Moderation emails are off: Email:Host / Email:User are not set");

        await foreach (var letter in queue.ReadAllAsync(stoppingToken))
        {
            if (!o.IsConfigured) continue;
            try
            {
                var from = new MailAddress(string.IsNullOrWhiteSpace(o.From) ? o.User! : o.From, o.FromName);
                using var message = new MailMessage(from, new MailAddress(letter.Email, letter.Name))
                {
                    Subject = letter.Subject,
                    Body = Html(letter, o.FrontendUrl),
                    IsBodyHtml = true,
                    BodyEncoding = Encoding.UTF8,
                    SubjectEncoding = Encoding.UTF8,
                };
                using var client = new SmtpClient(o.Host, o.Port)
                {
                    EnableSsl = o.EnableSsl,
                    Credentials = new NetworkCredential(o.User, o.Password),
                };
                await client.SendMailAsync(message, stoppingToken);
                // The address is logged so a letter that "did not arrive" can be traced: a seeded
                // account's address (e.g. @ideafusion.com) is accepted by Gmail but never delivered.
                logger.LogInformation("Email \"{Subject}\" sent to {Email}", letter.Subject, letter.Email);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Moderation email \"{Subject}\" to {Email} was not sent", letter.Subject, letter.Email);
            }
        }
    }

    public static string Html(ModerationLetter l, string frontendUrl)
    {
        string E(string? s) => WebUtility.HtmlEncode(s ?? string.Empty);
        var link = frontendUrl.TrimEnd('/') + l.ActionPath;
        var quote = string.IsNullOrWhiteSpace(l.Quote)
            ? ""
            : $"""<blockquote style="margin:16px 0;padding:12px 16px;border-left:3px solid #7c5cff;background:#f5f3ff;color:#333;white-space:pre-wrap">{E(l.Quote)}</blockquote>""";
        return $"""
            <!DOCTYPE html>
            <html lang="uk"><body style="margin:0;padding:24px;background:#f4f4f6;font-family:Segoe UI,Arial,sans-serif;color:#1d1d22">
              <div style="max-width:560px;margin:0 auto;background:#fff;border-radius:12px;padding:28px">
                <div style="font-weight:700;font-size:15px;color:#7c5cff;margin-bottom:18px">{E(l.Brand)}</div>
                <p style="margin:0 0 6px;color:#555">Привіт{(string.IsNullOrWhiteSpace(l.Name) ? "" : ", " + E(l.Name))}!</p>
                <h1 style="margin:0 0 12px;font-size:20px">{E(l.Heading)}</h1>
                <p style="margin:0;line-height:1.5">{E(l.Text)}</p>
                {quote}
                <p style="margin:24px 0 0"><a href="{E(link)}" style="display:inline-block;padding:11px 20px;border-radius:8px;background:#7c5cff;color:#fff;text-decoration:none;font-weight:600">{E(l.ActionLabel)}</a></p>
              </div>
              <p style="max-width:560px;margin:14px auto 0;font-size:12px;color:#888;text-align:center">{E(l.Footer)}</p>
            </body></html>
            """;
    }
}
