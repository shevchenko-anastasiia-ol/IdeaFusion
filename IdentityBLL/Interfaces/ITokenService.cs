using IdentityBLL.DTOs;
using IdentityServiceDomain.Entities;

namespace IdentityBLL.Interfaces;

public interface ITokenService
{
    Task<AuthResponseDto> GenerateTokensAsync(ApplicationUser user, CancellationToken cancellationToken = default);
    Task<AuthResponseDto> RefreshTokensAsync(TokenRequestDto tokenRequestDto, CancellationToken cancellationToken = default);
    Task<bool> RevokeTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task RevokeAllUserTokensAsync(Guid userId, CancellationToken cancellationToken = default);
}