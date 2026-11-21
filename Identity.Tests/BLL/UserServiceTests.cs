using AutoMapper;
using FluentAssertions;
using IdentityBLL.DTOs;
using IdentityBLL.Interfaces;
using IdentityBLL.MappingProfiles;
using IdentityBLL.Services;
using IdentityServiceDomain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Identity.Tests.BLL;

public class UserServiceTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<ILogger<UserService>>        _loggerMock = new();
    private readonly IMapper                           _mapper;

    public UserServiceTests()
    {
        var userStore = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _mapper = new MapperConfiguration(
                cfg => cfg.AddProfile<IdentityProfile>(),
                LoggerFactory.Create(_ => { }))
            .CreateMapper();
    }

    private UserService CreateService() => new(
        _userManagerMock.Object,
        _mapper,
        _loggerMock.Object);

    private static ApplicationUser MakeUser(
        Guid? id = null,
        string email = "user@example.com",
        string userName = "testuser") => new()
    {
        Id       = id ?? Guid.NewGuid(),
        Email    = email,
        UserName = userName
    };

    // ── GetOrCreateAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetOrCreate_ReturnsTheExistingRowWithoutCreatingOne()
    {
        var subject = Guid.NewGuid();
        var existing = MakeUser(subject, "anna@ideafusion.com", "AnnoGray");

        _userManagerMock.Setup(m => m.FindByIdAsync(subject.ToString())).ReturnsAsync(existing);
        _userManagerMock.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success);

        var result = await CreateService().GetOrCreateAsync(subject, "anna@ideafusion.com", "AnnoGray");

        result.Id.Should().Be(subject);
        result.Email.Should().Be("anna@ideafusion.com");
        _userManagerMock.Verify(m => m.CreateAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Fact]
    public async Task GetOrCreate_CreatesTheRowKeyedBySubjectOnFirstCall()
    {
        var subject = Guid.NewGuid();

        _userManagerMock.Setup(m => m.FindByIdAsync(subject.ToString())).ReturnsAsync((ApplicationUser?)null);

        ApplicationUser? captured = null;
        _userManagerMock.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>()))
            .Callback<ApplicationUser>(u => captured = u)
            .ReturnsAsync(IdentityResult.Success);

        var result = await CreateService().GetOrCreateAsync(subject, "anna@ideafusion.com", "AnnoGray");

        result.Id.Should().Be(subject);

        captured.Should().NotBeNull();
        captured!.Email.Should().Be("anna@ideafusion.com");
        captured.UserName.Should().Be("AnnoGray");
    }

    [Fact]
    public async Task GetOrCreate_StoresTheSentinelPasswordHash()
    {
        var subject = Guid.NewGuid();

        _userManagerMock.Setup(m => m.FindByIdAsync(subject.ToString())).ReturnsAsync((ApplicationUser?)null);

        ApplicationUser? captured = null;
        _userManagerMock.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>()))
            .Callback<ApplicationUser>(u => captured = u)
            .ReturnsAsync(IdentityResult.Success);

        await CreateService().GetOrCreateAsync(subject, "a@b.com", "a");

        captured!.PasswordHash.Should().Be(ApplicationUser.ExternalPasswordSentinel);
    }

    [Fact]
    public async Task GetOrCreate_FallsBackToAPlaceholderEmailWhenTheTokenHasNone()
    {
        var subject = Guid.NewGuid();

        _userManagerMock.Setup(m => m.FindByIdAsync(subject.ToString())).ReturnsAsync((ApplicationUser?)null);

        ApplicationUser? captured = null;
        _userManagerMock.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>()))
            .Callback<ApplicationUser>(u => captured = u)
            .ReturnsAsync(IdentityResult.Success);

        var result = await CreateService().GetOrCreateAsync(subject, null, null);

        result.Email.Should().Be($"{subject}@keycloak.local");
        captured!.UserName.Should().Be($"{subject}@keycloak.local");
    }

    [Fact]
    public async Task GetOrCreate_FallsBackToTheEmailWhenTheTokenHasNoUsername()
    {
        var subject = Guid.NewGuid();

        _userManagerMock.Setup(m => m.FindByIdAsync(subject.ToString())).ReturnsAsync((ApplicationUser?)null);

        ApplicationUser? captured = null;
        _userManagerMock.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>()))
            .Callback<ApplicationUser>(u => captured = u)
            .ReturnsAsync(IdentityResult.Success);

        await CreateService().GetOrCreateAsync(subject, "anna@ideafusion.com", "   ");

        captured!.UserName.Should().Be("anna@ideafusion.com");
    }

    [Fact]
    public async Task GetOrCreate_TreatsALostCreationRaceAsSuccess()
    {
        var subject = Guid.NewGuid();
        var winner = MakeUser(subject, "anna@ideafusion.com", "AnnoGray");

        // First lookup misses, but by the time the insert is rejected a concurrent
        // request has already created the row.
        _userManagerMock.SetupSequence(m => m.FindByIdAsync(subject.ToString()))
            .ReturnsAsync((ApplicationUser?)null)
            .ReturnsAsync(winner);

        _userManagerMock.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "duplicate key" }));

        var result = await CreateService().GetOrCreateAsync(subject, "anna@ideafusion.com", "AnnoGray");

        result.Id.Should().Be(subject);
        result.Email.Should().Be("anna@ideafusion.com");
    }

    [Fact]
    public async Task GetOrCreate_ThrowsWhenCreationFailsAndTheRowIsStillAbsent()
    {
        var subject = Guid.NewGuid();

        _userManagerMock.Setup(m => m.FindByIdAsync(subject.ToString())).ReturnsAsync((ApplicationUser?)null);
        _userManagerMock.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "validator said no" }));

        var act = () => CreateService().GetOrCreateAsync(subject, "a@b.com", "a");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*profile*");
    }

    [Fact]
    public async Task GetOrCreate_TrimsWhitespaceFromTheEmailAndUsername()
    {
        var subject = Guid.NewGuid();

        _userManagerMock.Setup(m => m.FindByIdAsync(subject.ToString())).ReturnsAsync((ApplicationUser?)null);

        ApplicationUser? captured = null;
        _userManagerMock.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>()))
            .Callback<ApplicationUser>(u => captured = u)
            .ReturnsAsync(IdentityResult.Success);

        await CreateService().GetOrCreateAsync(subject, "  anna@ideafusion.com  ", "  AnnoGray  ");

        captured!.Email.Should().Be("anna@ideafusion.com");
        captured.UserName.Should().Be("AnnoGray");
    }

    [Fact]
    public async Task GetOrCreate_LeavesRolesEmpty_BecauseRolesComeFromTheToken()
    {
        var subject = Guid.NewGuid();

        _userManagerMock.Setup(m => m.FindByIdAsync(subject.ToString())).ReturnsAsync((ApplicationUser?)null);
        _userManagerMock.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success);

        var result = await CreateService().GetOrCreateAsync(subject, "a@b.com", "a");

        result.Roles.Should().BeEmpty();
    }

    // ── GetUserByIdAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetUserById_ReturnsNullWhenTheRowIsAbsent()
    {
        var subject = Guid.NewGuid();
        _userManagerMock.Setup(m => m.FindByIdAsync(subject.ToString())).ReturnsAsync((ApplicationUser?)null);

        (await CreateService().GetUserByIdAsync(subject)).Should().BeNull();
    }

    [Fact]
    public async Task GetUserById_MapsTheStoredProfile()
    {
        var subject = Guid.NewGuid();
        _userManagerMock.Setup(m => m.FindByIdAsync(subject.ToString()))
            .ReturnsAsync(MakeUser(subject, "anna@ideafusion.com", "AnnoGray"));

        var result = await CreateService().GetUserByIdAsync(subject);

        result!.Id.Should().Be(subject);
        result.UserName.Should().Be("AnnoGray");
    }

    // ── UpdateAvatarUrlAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAvatarUrl_SetsAndPersistsTheUrl()
    {
        var subject = Guid.NewGuid();
        var user = MakeUser(subject);
        _userManagerMock.Setup(m => m.FindByIdAsync(subject.ToString())).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

        await CreateService().UpdateAvatarUrlAsync(subject, "http://minio/a.png");

        user.AvatarUrl.Should().Be("http://minio/a.png");
        _userManagerMock.Verify(m => m.UpdateAsync(user), Times.Once);
    }

    [Fact]
    public async Task UpdateAvatarUrl_DoesNothingWhenTheRowIsAbsent()
    {
        var subject = Guid.NewGuid();
        _userManagerMock.Setup(m => m.FindByIdAsync(subject.ToString())).ReturnsAsync((ApplicationUser?)null);

        await CreateService().UpdateAvatarUrlAsync(subject, "http://minio/a.png");

        _userManagerMock.Verify(m => m.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    // ── UpdateProfileAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateProfile_ReturnsNullWhenTheRowIsAbsent()
    {
        var subject = Guid.NewGuid();
        _userManagerMock.Setup(m => m.FindByIdAsync(subject.ToString())).ReturnsAsync((ApplicationUser?)null);

        (await CreateService().UpdateProfileAsync(subject, new UpdateProfileDto { FullName = "Anna" }))
            .Should().BeNull();
    }

    [Fact]
    public async Task UpdateProfile_TrimsAndStoresFullNameAndSpecialization()
    {
        var subject = Guid.NewGuid();
        var user = MakeUser(subject);
        _userManagerMock.Setup(m => m.FindByIdAsync(subject.ToString())).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

        var result = await CreateService().UpdateProfileAsync(subject, new UpdateProfileDto
        {
            FullName = "  Anna Gray  ",
            Specialization = "  Backend  "
        });

        user.FullName.Should().Be("Anna Gray");
        user.Specialization.Should().Be("Backend");
        result!.FullName.Should().Be("Anna Gray");
    }

    // ── DeleteUserAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteUser_ReportsNotFoundWhenTheRowIsAbsent()
    {
        var subject = Guid.NewGuid();
        _userManagerMock.Setup(m => m.FindByIdAsync(subject.ToString())).ReturnsAsync((ApplicationUser?)null);

        var result = await CreateService().DeleteUserAsync(subject);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "UserNotFound");
    }

    [Fact]
    public async Task DeleteUser_DelegatesToUserManager()
    {
        var subject = Guid.NewGuid();
        var user = MakeUser(subject);
        _userManagerMock.Setup(m => m.FindByIdAsync(subject.ToString())).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.DeleteAsync(user)).ReturnsAsync(IdentityResult.Success);

        (await CreateService().DeleteUserAsync(subject)).Succeeded.Should().BeTrue();
        _userManagerMock.Verify(m => m.DeleteAsync(user), Times.Once);
    }
}
