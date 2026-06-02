using AutoMapper;
using FluentAssertions;
using IdentityBLL.DTOs;
using IdentityBLL.MappingProfiles;
using IdentityServiceDomain.Entities;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Identity.Tests.BLL;

public class IdentityProfileTests
{
    private readonly IMapper _mapper;

    private static MapperConfiguration BuildConfig() =>
        new(cfg => cfg.AddProfile<IdentityProfile>(), LoggerFactory.Create(_ => { }));

    public IdentityProfileTests()
    {
        _mapper = BuildConfig().CreateMapper();
    }

    [Fact]
    public void Configuration_MapperCanBeCreatedWithoutThrowing()
    {
        var act = () => BuildConfig().CreateMapper();
        act.Should().NotThrow();
    }

    // ── RegisterDto → ApplicationUser ─────────────────────────────────────────

    [Fact]
    public void RegisterDto_To_ApplicationUser_MapsUserName()
    {
        var dto  = new RegisterDto { UserName = "alice", Email = "alice@example.com", Password = "Pass@1234" };
        var user = _mapper.Map<ApplicationUser>(dto);

        user.UserName.Should().Be("alice");
    }

    [Fact]
    public void RegisterDto_To_ApplicationUser_MapsEmail()
    {
        var dto  = new RegisterDto { UserName = "alice", Email = "alice@example.com", Password = "Pass@1234" };
        var user = _mapper.Map<ApplicationUser>(dto);

        user.Email.Should().Be("alice@example.com");
    }

    [Fact]
    public void RegisterDto_To_ApplicationUser_DoesNotMapPassword()
    {
        var dto  = new RegisterDto { UserName = "alice", Email = "alice@example.com", Password = "Pass@1234" };
        var user = _mapper.Map<ApplicationUser>(dto);

        user.PasswordHash.Should().BeNull();
    }

    // ── ApplicationUser → UserDto ──────────────────────────────────────────────

    [Fact]
    public void ApplicationUser_To_UserDto_MapsId()
    {
        var id   = Guid.NewGuid();
        var user = new ApplicationUser { Id = id, Email = "a@b.com" };
        var dto  = _mapper.Map<UserDto>(user);

        dto.Id.Should().Be(id);
    }

    [Fact]
    public void ApplicationUser_To_UserDto_MapsEmail()
    {
        var user = new ApplicationUser { Email = "test@example.com" };
        var dto  = _mapper.Map<UserDto>(user);

        dto.Email.Should().Be("test@example.com");
    }

    [Fact]
    public void ApplicationUser_To_UserDto_MapsUserName()
    {
        var user = new ApplicationUser { UserName = "bob", Email = "b@b.com" };
        var dto  = _mapper.Map<UserDto>(user);

        dto.UserName.Should().Be("bob");
    }

    [Fact]
    public void ApplicationUser_To_UserDto_MapsAvatarUrl()
    {
        var user = new ApplicationUser { Email = "a@b.com", AvatarUrl = "https://example.com/avatar.jpg" };
        var dto  = _mapper.Map<UserDto>(user);

        dto.AvatarUrl.Should().Be("https://example.com/avatar.jpg");
    }

    [Fact]
    public void ApplicationUser_To_UserDto_MapsFullName()
    {
        var user = new ApplicationUser { Email = "a@b.com", FullName = "Alice Smith" };
        var dto  = _mapper.Map<UserDto>(user);

        dto.FullName.Should().Be("Alice Smith");
    }

    [Fact]
    public void ApplicationUser_To_UserDto_MapsSpecialization()
    {
        var user = new ApplicationUser { Email = "a@b.com", Specialization = "Backend Developer" };
        var dto  = _mapper.Map<UserDto>(user);

        dto.Specialization.Should().Be("Backend Developer");
    }

    [Fact]
    public void ApplicationUser_To_UserDto_MapsCreatedAt()
    {
        var created = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc);
        var user    = new ApplicationUser { Email = "a@b.com", CreatedAt = created };
        var dto     = _mapper.Map<UserDto>(user);

        dto.CreatedAt.Should().Be(created);
    }

    [Fact]
    public void ApplicationUser_To_UserDto_RolesIsIgnored_DefaultsToEmptyList()
    {
        var user = new ApplicationUser { Email = "a@b.com" };
        var dto  = _mapper.Map<UserDto>(user);

        dto.Roles.Should().NotBeNull();
        dto.Roles.Should().BeEmpty();
    }

    // ── RefreshToken → RefreshTokenDto ────────────────────────────────────────

    [Fact]
    public void RefreshToken_To_RefreshTokenDto_MapsId()
    {
        var token = BuildToken(id: 7);
        var dto   = _mapper.Map<RefreshTokenDto>(token);

        dto.Id.Should().Be(7);
    }

    [Fact]
    public void RefreshToken_To_RefreshTokenDto_MapsToken()
    {
        var token = BuildToken(tokenValue: "tok-abc");
        var dto   = _mapper.Map<RefreshTokenDto>(token);

        dto.Token.Should().Be("tok-abc");
    }

    [Fact]
    public void RefreshToken_To_RefreshTokenDto_MapsExpires()
    {
        var expires = DateTime.UtcNow.AddDays(7);
        var token   = BuildToken(expires: expires);
        var dto     = _mapper.Map<RefreshTokenDto>(token);

        dto.Expires.Should().BeCloseTo(expires, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void RefreshToken_To_RefreshTokenDto_MapsIsRevoked()
    {
        var token = BuildToken(isRevoked: true);
        var dto   = _mapper.Map<RefreshTokenDto>(token);

        dto.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public void RefreshToken_To_RefreshTokenDto_MapsCreatedAt()
    {
        var created = new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var token   = BuildToken(createdAt: created);
        var dto     = _mapper.Map<RefreshTokenDto>(token);

        dto.CreatedAt.Should().Be(created);
    }

    [Fact]
    public void RefreshToken_To_RefreshTokenDto_IsExpiredComputedFromExpires_Future()
    {
        var token = BuildToken(expires: DateTime.UtcNow.AddDays(1));
        var dto   = _mapper.Map<RefreshTokenDto>(token);

        dto.IsExpired.Should().BeFalse();
    }

    [Fact]
    public void RefreshToken_To_RefreshTokenDto_IsExpiredComputedFromExpires_Past()
    {
        var token = BuildToken(expires: DateTime.UtcNow.AddDays(-1));
        var dto   = _mapper.Map<RefreshTokenDto>(token);

        dto.IsExpired.Should().BeTrue();
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private static RefreshToken BuildToken(
        int      id         = 1,
        string   tokenValue = "tok",
        DateTime? expires   = null,
        bool     isRevoked  = false,
        DateTime? createdAt = null) => new()
    {
        Id        = id,
        Token     = tokenValue,
        Expires   = expires   ?? DateTime.UtcNow.AddDays(7),
        IsRevoked = isRevoked,
        CreatedAt = createdAt ?? DateTime.UtcNow,
        UserId    = Guid.NewGuid(),
        User      = new ApplicationUser { Email = "u@u.com" }
    };
}
