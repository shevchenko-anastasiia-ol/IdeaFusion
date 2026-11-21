using System.Data;
using ContentDAL.Repository.Interfaces;
using ContentDomain.Entity;
using Dapper;
using Npgsql;

namespace ContentDAL.Repository;

public class PostAuthorRepository : IPostAuthorRepository
{
    private readonly IDbConnection _connection;
    protected readonly IDbTransaction? _transaction;

    public PostAuthorRepository(IDbConnection connection, IDbTransaction? transaction = null)
    {
        _connection = connection;
        _transaction = transaction;
    }

    private const string SelectColumns =
        "PostAuthorId, UserId, ExternalId, UserName, AvatarUrl, SyncedAt";

    /// <summary>SQLSTATE for unique_violation, the one the filtered index raises.</summary>
    private const string UniqueViolation = "23505";

    public async Task<PostAuthor?> GetByExternalIdAsync(string externalId, CancellationToken ct = default)
    {
        return await _connection.QuerySingleOrDefaultAsync<PostAuthor>(
            $"SELECT {SelectColumns} FROM PostAuthors WHERE ExternalId = @ExternalId LIMIT 1;",
            new { ExternalId = externalId },
            transaction: _transaction);
    }

    public async Task<PostAuthor?> GetByUserNameAsync(string userName, CancellationToken ct = default)
    {
        return await _connection.QuerySingleOrDefaultAsync<PostAuthor>(
            $"SELECT {SelectColumns} FROM PostAuthors WHERE UserName = @UserName LIMIT 1;",
            new { UserName = userName },
            transaction: _transaction);
    }

    public async Task<(PostAuthor Author, bool Created)> EnsureByExternalIdAsync(
        string externalId,
        string userName,
        string? avatarUrl = null,
        CancellationToken ct = default)
    {
        var existing = await GetByExternalIdAsync(externalId, ct);
        if (existing is not null)
        {
            await RefreshAsync(existing, userName, avatarUrl, ct);
            return (existing, false);
        }

        // The content-side UserId is still the column Likes and SavedPosts key on,
        // so a new row gets the next value in that space rather than a Keycloak id.
        var newUserId = await _connection.ExecuteScalarAsync<int>(
            "SELECT COALESCE(MAX(UserId), 200) + 1 FROM PostAuthors;",
            transaction: _transaction);

        try
        {
            var newPostAuthorId = await _connection.ExecuteScalarAsync<int>(
                @"INSERT INTO PostAuthors (UserId, ExternalId, UserName, AvatarUrl, SyncedAt)
                  VALUES (@UserId, @ExternalId, @UserName, @AvatarUrl, @SyncedAt)
                  RETURNING PostAuthorId;",
                new
                {
                    UserId = newUserId,
                    ExternalId = externalId,
                    UserName = userName,
                    AvatarUrl = avatarUrl,
                    SyncedAt = DateTime.UtcNow
                },
                transaction: _transaction);

            return (new PostAuthor
            {
                PostAuthorId = newPostAuthorId,
                UserId = newUserId,
                ExternalId = externalId,
                UserName = userName,
                AvatarUrl = avatarUrl,
                SyncedAt = DateTime.UtcNow
            }, true);
        }
        catch (PostgresException ex) when (ex.SqlState == UniqueViolation)
        {
            // Two concurrent first writes by the same subject. The filtered unique
            // index on externalid let exactly one of them through, so read the winner
            // instead of failing the loser.
            var winner = await GetByExternalIdAsync(externalId, ct);
            if (winner is null)
            {
                throw;
            }

            await RefreshAsync(winner, userName, avatarUrl, ct);
            return (winner, false);
        }
    }

    /// <summary>
    /// Brings the cached copy of the user back in line with the token. The display
    /// name always follows the token; the avatar only when the caller supplies a new
    /// one, since the token does not carry it.
    /// </summary>
    private async Task RefreshAsync(PostAuthor author, string userName, string? avatarUrl, CancellationToken ct)
    {
        var nameChanged = !string.Equals(author.UserName, userName, StringComparison.Ordinal);
        var avatarChanged = avatarUrl is not null
                            && !string.Equals(author.AvatarUrl, avatarUrl, StringComparison.Ordinal);

        if (!nameChanged && !avatarChanged)
        {
            return;
        }

        var syncedAt = DateTime.UtcNow;
        var effectiveAvatar = avatarUrl ?? author.AvatarUrl;

        await _connection.ExecuteAsync(
            @"UPDATE PostAuthors
              SET UserName  = @UserName,
                  AvatarUrl = @AvatarUrl,
                  SyncedAt  = @SyncedAt
              WHERE PostAuthorId = @PostAuthorId;",
            new
            {
                author.PostAuthorId,
                UserName = userName,
                AvatarUrl = effectiveAvatar,
                SyncedAt = syncedAt
            },
            transaction: _transaction);

        author.UserName = userName;
        author.AvatarUrl = effectiveAvatar;
        author.SyncedAt = syncedAt;
    }
}
