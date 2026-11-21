using ContentDomain.Entity;

namespace ContentDAL.Repository.Interfaces;

public interface IPostAuthorRepository
{
    /// <summary>
    /// The author row for a Keycloak subject, or null. The one lookup every
    /// authenticated write goes through, so the caller is never taken from the body.
    /// </summary>
    Task<PostAuthor?> GetByExternalIdAsync(string externalId, CancellationToken ct = default);

    /// <summary>
    /// The author row for a display name, or null. Read-only: the demo rows are
    /// keyed this way and nothing may be created off a name a client chose.
    /// </summary>
    Task<PostAuthor?> GetByUserNameAsync(string userName, CancellationToken ct = default);

    /// <summary>
    /// Resolves the author for a Keycloak subject, inserting the row the first time
    /// that subject writes. Returns the row and whether it was just created.
    /// </summary>
    Task<(PostAuthor Author, bool Created)> EnsureByExternalIdAsync(
        string externalId,
        string userName,
        string? avatarUrl = null,
        CancellationToken ct = default);
}
