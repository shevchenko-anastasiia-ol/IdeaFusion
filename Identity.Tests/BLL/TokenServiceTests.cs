using FluentAssertions;
using IdentityBLL.Configuration;
using IdentityBLL.DTOs;
using IdentityBLL.Services;
using IdentityDAL.Interfaces;
using IdentityServiceDomain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Security.Authentication;
using Xunit;

namespace Identity.Tests.BLL;

public class TokenServiceTests
{
    private readonly Mock<UserManager<ApplicationUser>>  _userManagerMock;
    private readonly Mock<IRefreshTokenRepository>       _repoMock = new();
    private readonly Mock<ILogger<TokenService>>         _loggerMock = new();
    private readonly IOptions<JwtSettings>               _jwtOptions;

    public TokenServiceTests()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _jwtOptions = Options.Create(new JwtSettings
        {
            Key                      = "a-super-secret-key-for-unit-tests!!", // 36 bytes
            Issuer                   = "test-issuer",
            Audience                 = "test-audience",
            AccessTokenDurationMinutes = 15,
            RefreshTokenDurationDays   = 7
        });
    }

    private TokenService CreateService() =>
        new(_jwtOptions, _userManagerMock.Object, _repoMock.Object, _loggerMock.Object);

    private static ApplicationUser MakeUser() => new()
    {
        Id       = Guid.NewGuid(),
        Email    = "user@example.com",
        UserName = "testuser"
    };

    private static RefreshToken MakeRefreshToken(
        ApplicationUser user,
        bool isExpired  = false,
        bool isRevoked  = false,
        bool isUsed     = false) => new()
    {
        Id        = 1,
        Token     = "stored-refresh-token",
        Expires   = isExpired ? DateTime.UtcNow.AddDays(-1) : DateTime.UtcNow.AddDays(7),
        IsRevoked = isRevoked,
        IsUsed    = isUsed,
        CreatedAt = DateTime.UtcNow,
        UserId    = user.Id,
        User      = user
    };

    // ── GenerateTokensAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GenerateTokensAsync_ReturnsAuthResponseWithNonEmptyAccessToken()
    {
        var user = MakeUser();
        _userManagerMock.Setup(m => m.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "User" });
        _repoMock.Setup(r => r.GetByTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);
        _repoMock.Setup(r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await CreateService().GenerateTokensAsync(user);

        result.Should().NotBeNull();
        result.AccessToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GenerateTokensAsync_ReturnsAuthResponseWithNonEmptyRefreshToken()
    {
        var user = MakeUser();
        _userManagerMock.Setup(m => m.GetRolesAsync(user))
            .ReturnsAsync(new List<string>());
        _repoMock.Setup(r => r.GetByTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);
        _repoMock.Setup(r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await CreateService().GenerateTokensAsync(user);

        result.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GenerateTokensAsync_SavesRefreshTokenToRepository()
    {
        var user = MakeUser();
        _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string>());
        _repoMock.Setup(r => r.GetByTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);
        _repoMock.Setup(r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await CreateService().GenerateTokensAsync(user);

        _repoMock.Verify(r => r.AddAsync(
            It.Is<RefreshToken>(t => t.UserId == user.Id),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GenerateTokensAsync_RetryOnTokenCollision_CallsAddOnce()
    {
        var user = MakeUser();
        var call = 0;
        _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string>());
        _repoMock.Setup(r => r.GetByTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                call++;
                return call == 1 ? MakeRefreshToken(user) : null;
            });
        _repoMock.Setup(r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await CreateService().GenerateTokensAsync(user);

        _repoMock.Verify(r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── RefreshTokensAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task RefreshTokensAsync_WhenTokenNotFound_ThrowsAuthenticationException()
    {
        _repoMock.Setup(r => r.GetByTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        var act = () => CreateService().RefreshTokensAsync(new TokenRequestDto { RefreshToken = "ghost" });

        await act.Should().ThrowAsync<AuthenticationException>()
            .WithMessage("*does not exist*");
    }

    [Fact]
    public async Task RefreshTokensAsync_WhenTokenExpired_ThrowsAuthenticationException()
    {
        var user  = MakeUser();
        var token = MakeRefreshToken(user, isExpired: true);
        _repoMock.Setup(r => r.GetByTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        var act = () => CreateService().RefreshTokensAsync(new TokenRequestDto { RefreshToken = token.Token });

        await act.Should().ThrowAsync<AuthenticationException>()
            .WithMessage("*expired*");
    }

    [Fact]
    public async Task RefreshTokensAsync_WhenTokenRevoked_ThrowsAuthenticationException()
    {
        var user  = MakeUser();
        var token = MakeRefreshToken(user, isRevoked: true);
        _repoMock.Setup(r => r.GetByTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        var act = () => CreateService().RefreshTokensAsync(new TokenRequestDto { RefreshToken = token.Token });

        await act.Should().ThrowAsync<AuthenticationException>()
            .WithMessage("*revoked*");
    }

    [Fact]
    public async Task RefreshTokensAsync_WhenTokenAlreadyUsed_ThrowsAuthenticationException()
    {
        var user  = MakeUser();
        var token = MakeRefreshToken(user, isUsed: true);
        _repoMock.Setup(r => r.GetByTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        var act = () => CreateService().RefreshTokensAsync(new TokenRequestDto { RefreshToken = token.Token });

        await act.Should().ThrowAsync<AuthenticationException>()
            .WithMessage("*already been used*");
    }

    [Fact]
    public async Task RefreshTokensAsync_ValidToken_MarksTokenAsUsed()
    {
        var user  = MakeUser();
        var token = MakeRefreshToken(user);
        _repoMock.Setup(r => r.GetByTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _userManagerMock.Setup(m => m.GetRolesAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(new List<string>());
        _repoMock.Setup(r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _repoMock.SetupSequence(r => r.GetByTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(token)
            .ReturnsAsync((RefreshToken?)null);

        await CreateService().RefreshTokensAsync(new TokenRequestDto { RefreshToken = token.Token });

        _repoMock.Verify(r => r.UpdateAsync(
            It.Is<RefreshToken>(t => t.IsUsed),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RefreshTokensAsync_ValidToken_ReturnsNewTokenPair()
    {
        var user  = MakeUser();
        var token = MakeRefreshToken(user);
        _repoMock.SetupSequence(r => r.GetByTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(token)
            .ReturnsAsync((RefreshToken?)null);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _userManagerMock.Setup(m => m.GetRolesAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(new List<string>());

        var result = await CreateService().RefreshTokensAsync(new TokenRequestDto { RefreshToken = token.Token });

        result.Should().NotBeNull();
        result.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
    }

    // ── RevokeTokenAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task RevokeTokenAsync_WhenTokenNotFound_ReturnsFalse()
    {
        _repoMock.Setup(r => r.GetByTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        var result = await CreateService().RevokeTokenAsync("nonexistent");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task RevokeTokenAsync_WhenTokenAlreadyRevoked_ReturnsFalse()
    {
        var user  = MakeUser();
        var token = MakeRefreshToken(user, isRevoked: true);
        _repoMock.Setup(r => r.GetByTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        var result = await CreateService().RevokeTokenAsync(token.Token);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task RevokeTokenAsync_ValidToken_SetsIsRevokedAndReturnsTrue()
    {
        var user  = MakeUser();
        var token = MakeRefreshToken(user);
        _repoMock.Setup(r => r.GetByTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await CreateService().RevokeTokenAsync(token.Token);

        result.Should().BeTrue();
        _repoMock.Verify(r => r.UpdateAsync(
            It.Is<RefreshToken>(t => t.IsRevoked),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── RevokeAllUserTokensAsync ──────────────────────────────────────────────

    [Fact]
    public async Task RevokeAllUserTokensAsync_WhenNoTokens_DoesNotCallUpdate()
    {
        var userId = Guid.NewGuid();
        _repoMock.Setup(r => r.GetUserTokensAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RefreshToken>());

        await CreateService().RevokeAllUserTokensAsync(userId);

        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RevokeAllUserTokensAsync_RevokesAllActiveTokens()
    {
        var user   = MakeUser();
        var tokens = new List<RefreshToken>
        {
            MakeRefreshToken(user),
            MakeRefreshToken(user)
        };
        _repoMock.Setup(r => r.GetUserTokensAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tokens);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await CreateService().RevokeAllUserTokensAsync(user.Id);

        _repoMock.Verify(r => r.UpdateAsync(
            It.Is<RefreshToken>(t => t.IsRevoked),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task RevokeAllUserTokensAsync_SkipsAlreadyRevokedTokens()
    {
        var user   = MakeUser();
        var tokens = new List<RefreshToken>
        {
            MakeRefreshToken(user),
            MakeRefreshToken(user, isRevoked: true)
        };
        _repoMock.Setup(r => r.GetUserTokensAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tokens);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await CreateService().RevokeAllUserTokensAsync(user.Id);

        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
