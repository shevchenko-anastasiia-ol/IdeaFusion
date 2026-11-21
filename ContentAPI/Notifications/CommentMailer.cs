using System.Net.Http.Headers;
using System.Net.Http.Json;
using ContentDAL.Data;
using GrpcClients.Interfaces;
using Microsoft.EntityFrameworkCore;
using ServiceDefaults.Interfaces;

namespace ContentAPI.Notifications;

/// <summary>
/// Letters about comments: the post's author learns about a new comment, and the author of the
/// comment that was answered learns about the reply. Nobody is written to about their own comment,
/// and whoever switched "Коментарі" off in their settings (kept by CollaborationService) is skipped.
/// </summary>
public interface ICommentMailer
{
    /// <summary>Call inside the request that created the comment: its token is used to look people up.</summary>
    Task NotifyCreatedAsync(int commentId, CancellationToken ct);
}

public sealed class CommentMailer(
    ContentDbContext db,
    IUserGrpcClient users,
    ICurrentUserAccessor currentUser,
    IHttpClientFactory http,
    IHttpContextAccessor context,
    ModerationMailQueue queue,
    ILogger<CommentMailer> logger) : ICommentMailer
{
    public const string SettingsClient = "collaboration-settings";

    public async Task NotifyCreatedAsync(int commentId, CancellationToken ct)
    {
        try
        {
            var c = await db.Comments.AsNoTracking()
                .Where(x => x.CommentId == commentId)
                .Select(x => new
                {
                    x.PostId,
                    x.Body,
                    PostTitle = x.Post.Title,
                    PostAuthorId = x.Post.Author != null ? x.Post.Author.ExternalId : null,
                    PostCreatedBy = x.Post.CreatedBy,
                    ParentAuthorId = x.ParentComment != null ? x.ParentComment.Author.ExternalId : null,
                })
                .FirstOrDefaultAsync(ct);
            if (c is null) return;

            var me = currentUser.UserId?.ToString();
            var actor = currentUser.UserName ?? "Хтось";

            // A team post has no personal author: whoever published it gets the letter.
            var postOwner = c.PostAuthorId;
            if (string.IsNullOrWhiteSpace(postOwner) && !string.IsNullOrWhiteSpace(c.PostCreatedBy))
                postOwner = await db.PostAuthors.AsNoTracking()
                    .Where(a => a.UserName == c.PostCreatedBy && a.ExternalId != null)
                    .Select(a => a.ExternalId)
                    .FirstOrDefaultAsync(ct);

            var path = $"/posts/{c.PostId}";
            var quote = c.Body.Length <= 400 ? c.Body : c.Body[..400].TrimEnd() + "…";

            // The reply letter wins when one person is both: one letter, not two.
            if (Differs(c.ParentAuthorId, me))
                await SendAsync(c.ParentAuthorId!,
                    $"{actor} відповів(-ла) на ваш коментар",
                    $"{actor} відповідає вам",
                    $"Нова відповідь на ваш коментар до посту «{c.PostTitle}».",
                    quote, path, ct);

            if (Differs(postOwner, me) && !Same(postOwner, c.ParentAuthorId))
                await SendAsync(postOwner!,
                    $"Новий коментар до «{c.PostTitle}»",
                    $"{actor} прокоментував(-ла) ваш пост",
                    $"Під вашим постом «{c.PostTitle}» з’явився новий коментар.",
                    quote, path, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Could not prepare a comment letter for comment {CommentId}", commentId);
        }
    }

    private static bool Differs(string? id, string? me) => !string.IsNullOrWhiteSpace(id) && !Same(id, me);
    private static bool Same(string? a, string? b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    private async Task SendAsync(string recipientId, string subject, string heading, string text, string quote, string path, CancellationToken ct)
    {
        if (!Guid.TryParse(recipientId, out var id)) return;

        if (!await AllowsAsync(recipientId, ct))
        {
            logger.LogInformation("Comment letter \"{Subject}\" to {UserId} skipped: comment letters are switched off", subject, recipientId);
            return;
        }

        var user = await users.GetUserByIdAsync(id, ct);
        if (user is null || string.IsNullOrWhiteSpace(user.Email))
        {
            logger.LogWarning("Comment letter \"{Subject}\" skipped: no email address for {UserId}", subject, recipientId);
            return;
        }

        queue.Enqueue(new ModerationLetter(user.Email, user.UserName, subject, heading, text, "Відкрити пост", path)
        {
            Brand = "IdeaFusion",
            Footer = "Ви отримали цей лист, бо в IdeaFusion увімкнено сповіщення про коментарі. Вимкнути можна в особистому кабінеті → Сповіщення.",
            Quote = quote,
        });
    }

    /// <summary>Asks CollaborationService; if it cannot answer, the letter goes out (the default is "on").</summary>
    private async Task<bool> AllowsAsync(string userId, CancellationToken ct)
    {
        try
        {
            var client = http.CreateClient(SettingsClient);
            using var request = new HttpRequestMessage(HttpMethod.Get,
                $"api/collaboration/notification-settings/allows/{Uri.EscapeDataString(userId)}/Comments");
            var auth = context.HttpContext?.Request.Headers.Authorization.ToString();
            if (!string.IsNullOrWhiteSpace(auth))
                request.Headers.Authorization = AuthenticationHeaderValue.Parse(auth);
            using var response = await client.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Comment settings for {UserId} unavailable ({Status}); sending anyway", userId, (int)response.StatusCode);
                return true;
            }
            return await response.Content.ReadFromJsonAsync<bool>(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Comment settings for {UserId} unavailable; sending anyway", userId);
            return true;
        }
    }
}
