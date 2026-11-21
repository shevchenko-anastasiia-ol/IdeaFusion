using ContentBLL.Services.Interfaces;
using ContentDomain.Entity;
using ContentDomain.Exception;
using ServiceDefaults.Interfaces;

namespace ContentBLL.Services;

/// <summary>
/// The single rule for "may this caller change this row": the author, or a
/// moderator. Both post and comment writes go through here so the answer and the
/// wording stay the same across the service.
/// </summary>
internal static class OwnershipGuard
{
    internal static void EnsurePostOwnerOrModerator(
        Post post,
        ResolvedPostAuthor caller,
        ICurrentUserAccessor currentUser)
    {
        if (currentUser.IsModerator)
        {
            return;
        }

        if (post.PostAuthorId is null || post.PostAuthorId != caller.PostAuthorId)
        {
            throw new ForbiddenException(
                $"Пост з id={post.PostId} належить іншому автору. " +
                "Редагувати або видалити його може лише автор або модератор.");
        }
    }

    internal static void EnsureCommentOwnerOrModerator(
        Comment comment,
        ResolvedPostAuthor caller,
        ICurrentUserAccessor currentUser)
    {
        if (currentUser.IsModerator)
        {
            return;
        }

        if (comment.PostAuthorId != caller.PostAuthorId)
        {
            throw new ForbiddenException(
                $"Коментар з id={comment.CommentId} належить іншому автору. " +
                "Редагувати або видалити його може лише автор або модератор.");
        }
    }

    /// <summary>
    /// Guard for the moderation endpoints themselves. The [Authorize] policy is the
    /// first line; this repeats it so the services stay safe when called directly.
    /// </summary>
    internal static void EnsureModerator(ICurrentUserAccessor currentUser, string action)
    {
        if (!currentUser.IsModerator)
        {
            throw new ForbiddenException($"Дія '{action}' доступна лише модераторам.");
        }
    }
}
