using IdentityBLL.DTOs;
using Microsoft.AspNetCore.Identity;

namespace IdentityBLL.Interfaces;

public interface IUserService
    {
        Task<IdentityResult> RegisterUserAsync(RegisterDto registerDto, CancellationToken cancellationToken = default);
        Task<AuthResponseDto> LoginUserAsync(LoginDto loginDto, CancellationToken cancellationToken = default);
        Task<IEnumerable<UserDto>> GetUsersAsync(CancellationToken cancellationToken = default);
        Task<UserDto?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<IEnumerable<RefreshTokenDto>> GetUserRefreshTokensAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<IdentityResult> AddUserToRoleAsync(Guid userId, string roleName, CancellationToken cancellationToken = default);
        Task<IdentityResult> RemoveUserFromRoleAsync(Guid userId, string roleName, CancellationToken cancellationToken = default);
        Task UpdateAvatarUrlAsync(Guid userId, string avatarUrl, CancellationToken cancellationToken = default);
        Task<UserDto?> UpdateProfileAsync(Guid userId, UpdateProfileDto dto, CancellationToken cancellationToken = default);
        Task<IdentityResult> ChangePasswordAsync(Guid userId, ChangePasswordDto dto, CancellationToken cancellationToken = default);
        Task<IdentityResult> DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default);
    }