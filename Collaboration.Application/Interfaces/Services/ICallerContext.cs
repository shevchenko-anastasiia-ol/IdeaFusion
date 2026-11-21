using Collaboration.Domain.Exceptions;
using Collaboration.Domain.ValueOfObjects;
using ServiceDefaults.Interfaces;

namespace Collaboration.Application.Interfaces.Services;

/// <summary>
/// The identity every command and query runs as, taken from the validated token.
/// <para>
/// This is the shape <c>ChatHub.cs</c> already used: read the subject off the caller,
/// never off a parameter. Handlers take this instead of an <c>ICurrentUserAccessor</c>
/// directly so that "who is acting" has exactly one answer in the application layer.
/// </para>
/// </summary>
public interface ICallerContext
{
    /// <summary>The Keycloak subject, as the string form stored in the documents.</summary>
    string UserId { get; }

    string Username { get; }

    string? AvatarUrl { get; }

    bool IsAdministrator { get; }

    bool IsModerator { get; }

    /// <summary>The caller as a document snapshot, for anything that stores one.</summary>
    UserSnapshot ToSnapshot();
}

public sealed class CallerContext : ICallerContext
{
    private readonly ICurrentUserAccessor _currentUser;

    public CallerContext(ICurrentUserAccessor currentUser)
    {
        _currentUser = currentUser;
    }

    public string UserId
    {
        get
        {
            if (!_currentUser.IsAuthenticated)
                throw new ForbiddenException("This operation requires an authenticated user.");

            // "sub" is the only stable identifier in the realm. A display name can be
            // changed by its owner at any moment, so it cannot identify anything.
            var sub = _currentUser.UserId
                ?? throw new ForbiddenException("The access token carries no subject.");

            return sub.ToString();
        }
    }

    public string Username
    {
        get
        {
            if (!_currentUser.IsAuthenticated)
                throw new ForbiddenException("This operation requires an authenticated user.");

            return _currentUser.UserName
                ?? _currentUser.Email
                ?? UserId;
        }
    }

    // Keycloak tokens carry no avatar, so this is only ever filled in from a profile
    // endpoint the caller has just called. Null means "keep whatever is stored".
    public string? AvatarUrl => _currentUser.AvatarUrl;

    public bool IsAdministrator => _currentUser.IsAdministrator;

    public bool IsModerator => _currentUser.IsModerator;

    public UserSnapshot ToSnapshot() => new(UserId, Username, AvatarUrl);
}
