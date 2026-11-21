namespace ContentBLL.Services.Interfaces;

/// <summary>
/// The content-side identity of the caller, taken from the validated token rather
/// than from anything in the request body.
/// </summary>
public sealed record ResolvedPostAuthor(
    int PostAuthorId,
    int UserId,
    Guid ExternalId,
    string UserName,
    string? AvatarUrl,
    bool Created);

public interface IPostAuthorResolver
{
    /// <summary>
    /// Resolves the caller, requiring a row to already exist. For operations that
    /// act on something the caller created: no row means no such thing exists yet.
    /// </summary>
    Task<ResolvedPostAuthor> ResolveAsync(CancellationToken ct = default);

    /// <summary>
    /// Resolves the caller, or null when they have never written. For "does this
    /// caller own X" reads, where a missing row is the answer rather than an error.
    /// </summary>
    Task<ResolvedPostAuthor?> FindAsync(CancellationToken ct = default);

    /// <summary>
    /// Resolves the caller, creating the PostAuthor row the first time that
    /// subject writes. This is the only path that mints a row, and it keys off the
    /// token, so a caller cannot mint a row for somebody else.
    /// </summary>
    Task<ResolvedPostAuthor> ResolveForWriteAsync(string? avatarUrl = null, CancellationToken ct = default);

    /// <summary>Non-minting lookup, for the read-only by-username endpoint.</summary>
    Task<ResolvedPostAuthor?> FindByUserNameAsync(string userName, CancellationToken ct = default);
}
