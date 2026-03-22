using Collaboration.Domain.Exceptions;

namespace Collaboration.Domain.ValueOfObjects;

public sealed class UserSnapshot : Common.ValueObject
{
    public string UserId { get; }
    public string Username { get; }
    public string? AvatarUrl { get; }

    // Потрібен для MongoDB десеріалізації через серіалайзер
    private UserSnapshot()
    {
        UserId = string.Empty;
        Username = string.Empty;
    }

    public UserSnapshot(string userId, string username, string? avatarUrl = null)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new DomainException("UserId cannot be empty.");
        if (string.IsNullOrWhiteSpace(username))
            throw new DomainException("Username cannot be empty.");
        if (username.Length > 100)
            throw new DomainException("Username cannot exceed 100 characters.");

        UserId = userId.Trim();
        Username = username.Trim();
        AvatarUrl = avatarUrl?.Trim();
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return UserId;
        yield return Username;
        yield return AvatarUrl;
    }

    public UserSnapshot WithUpdatedUsername(string username) =>
        new(UserId, username, AvatarUrl);

    public UserSnapshot WithUpdatedAvatar(string? avatarUrl) =>
        new(UserId, Username, avatarUrl);

    public override string ToString() => Username;
}