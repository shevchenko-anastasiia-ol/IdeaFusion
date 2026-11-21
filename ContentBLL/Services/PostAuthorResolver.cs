using ContentBLL.Services.Interfaces;
using ContentDAL.UOW;
using ContentDomain.Exception;
using ServiceDefaults.Interfaces;

namespace ContentBLL.Services;

/// <summary>
/// Turns the caller's validated token into a PostAuthor row. Every write path in
/// the content service asks this who is calling, which is what makes the identity
/// columns in the request DTOs unnecessary.
/// </summary>
public sealed class PostAuthorResolver : IPostAuthorResolver
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserAccessor _currentUser;

    public PostAuthorResolver(IUnitOfWork uow, ICurrentUserAccessor currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<ResolvedPostAuthor> ResolveAsync(CancellationToken ct = default)
    {
        return await FindAsync(ct)
            ?? throw new NotFoundException("Профіль автора для поточного користувача ще не створено.");
    }

    public async Task<ResolvedPostAuthor?> FindAsync(CancellationToken ct = default)
    {
        var (externalId, _) = ReadCaller();

        var author = await _uow.PostAuthorRepository.GetByExternalIdAsync(externalId.ToString(), ct);
        if (author is null)
        {
            return null;
        }

        return new ResolvedPostAuthor(
            author.PostAuthorId,
            author.UserId,
            externalId,
            author.UserName,
            author.AvatarUrl,
            false);
    }

    public async Task<ResolvedPostAuthor> ResolveForWriteAsync(string? avatarUrl = null, CancellationToken ct = default)
    {
        var (externalId, userName) = ReadCaller();

        var (author, created) = await _uow.PostAuthorRepository.EnsureByExternalIdAsync(
            externalId.ToString(),
            userName,
            avatarUrl,
            ct);

        return new ResolvedPostAuthor(
            author.PostAuthorId,
            author.UserId,
            externalId,
            author.UserName,
            author.AvatarUrl,
            created);
    }

    public async Task<ResolvedPostAuthor?> FindByUserNameAsync(string userName, CancellationToken ct = default)
    {
        var author = await _uow.PostAuthorRepository.GetByUserNameAsync(userName, ct);
        if (author is null)
        {
            return null;
        }

        // Demo rows predate the realm and have no subject, so there is no Guid to
        // hand back for them. The caller asked by name, not by subject.
        return new ResolvedPostAuthor(
            author.PostAuthorId,
            author.UserId,
            Guid.TryParse(author.ExternalId, out var externalId) ? externalId : Guid.Empty,
            author.UserName,
            author.AvatarUrl,
            false);
    }

    private (Guid ExternalId, string UserName) ReadCaller()
    {
        if (!_currentUser.IsAuthenticated)
        {
            throw new ForbiddenException("Операція доступна лише автентифікованим користувачам.");
        }

        // "sub" is the only thing stable about a user in the realm; the display name
        // can be changed by the user at any time.
        var externalId = _currentUser.UserId
            ?? throw new ForbiddenException("Токен не містить subject.");

        var userName = _currentUser.UserName
            ?? _currentUser.Email
            ?? throw new ForbiddenException("Токен не містить імені користувача.");

        return (externalId, userName);
    }
}
