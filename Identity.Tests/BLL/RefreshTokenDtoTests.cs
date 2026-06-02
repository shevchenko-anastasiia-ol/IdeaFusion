using FluentAssertions;
using IdentityBLL.DTOs;
using Xunit;

namespace Identity.Tests.BLL;

public class RefreshTokenDtoTests
{
    [Fact]
    public void Default_AllPropertiesHaveExpectedDefaults()
    {
        var dto = new RefreshTokenDto();

        dto.Id.Should().Be(0);
        dto.Token.Should().BeNull();
        dto.Expires.Should().Be(default);
        dto.IsRevoked.Should().BeFalse();
        dto.CreatedAt.Should().Be(default);
    }

    [Fact]
    public void Properties_CanBeSetAndRetrieved()
    {
        var expires   = DateTime.UtcNow.AddDays(7);
        var createdAt = DateTime.UtcNow;

        var dto = new RefreshTokenDto
        {
            Id        = 42,
            Token     = "my-refresh-token",
            Expires   = expires,
            IsRevoked = false,
            CreatedAt = createdAt
        };

        dto.Id.Should().Be(42);
        dto.Token.Should().Be("my-refresh-token");
        dto.Expires.Should().BeCloseTo(expires, TimeSpan.FromSeconds(1));
        dto.IsRevoked.Should().BeFalse();
        dto.CreatedAt.Should().BeCloseTo(createdAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void IsExpired_WhenExpiresInFuture_ReturnsFalse()
    {
        var dto = new RefreshTokenDto { Expires = DateTime.UtcNow.AddDays(1) };

        dto.IsExpired.Should().BeFalse();
    }

    [Fact]
    public void IsExpired_WhenExpiresInPast_ReturnsTrue()
    {
        var dto = new RefreshTokenDto { Expires = DateTime.UtcNow.AddDays(-1) };

        dto.IsExpired.Should().BeTrue();
    }

    [Fact]
    public void IsExpired_WhenExpiresIsExactlyNow_ReturnsTrue()
    {
        var now = DateTime.UtcNow;
        var dto = new RefreshTokenDto { Expires = now.AddMilliseconds(-1) };

        dto.IsExpired.Should().BeTrue();
    }

    [Fact]
    public void IsExpired_IsReadOnly_CannotBeSet()
    {
        var property = typeof(RefreshTokenDto).GetProperty(nameof(RefreshTokenDto.IsExpired));

        property!.CanWrite.Should().BeFalse();
    }

    [Fact]
    public void IsRevoked_CanBeSetToTrue()
    {
        var dto = new RefreshTokenDto { IsRevoked = true };

        dto.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public void Token_CanHoldLongBase64String()
    {
        var longToken = Convert.ToBase64String(new byte[64]);
        var dto       = new RefreshTokenDto { Token = longToken };

        dto.Token.Should().Be(longToken);
        dto.Token.Length.Should().BeGreaterThan(80);
    }
}
