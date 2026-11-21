using IdentityBLL.DTOs;
using Microsoft.AspNetCore.Identity;

namespace IdentityBLL.Interfaces;

public interface IUserService
{
    Task<IEnumerable<UserDto>> GetUsersAsync(CancellationToken cancellationToken = default);

    Task<UserDto?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the profile row for a Keycloak subject, creating it on first sight.
    /// The row is keyed by "sub", so the same Keycloak account always maps to the
    /// same profile regardless of what the user renames themselves to.
    /// </summary>
    Task<UserDto> GetOrCreateAsync(
        Guid subject,
        string? email,
        string? userName,
        CancellationToken cancellationToken = default);

    Task UpdateAvatarUrlAsync(Guid userId, string avatarUrl, CancellationToken cancellationToken = default);

    Task<UserDto?> UpdateProfileAsync(Guid userId, UpdateProfileDto dto, CancellationToken cancellationToken = default);

    Task<IdentityResult> DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
