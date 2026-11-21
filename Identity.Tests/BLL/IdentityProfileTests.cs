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
}
