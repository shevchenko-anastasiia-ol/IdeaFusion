using AutoMapper;
using FluentAssertions;
using Identity.Tests.Helpers;
using IdentityBLL.DTOs;
using IdentityBLL.MappingProfiles;
using IdentityBLL.Interfaces;
using IdentityBLL.Services;
using IdentityDAL.Interfaces;
using IdentityServiceDomain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Authentication;
using Xunit;

namespace Identity.Tests.BLL;

public class UserServiceTests
{
    private readonly Mock<UserManager<ApplicationUser>>  _userManagerMock;
    private readonly Mock<RoleManager<ApplicationRole>>  _roleManagerMock;
    private readonly Mock<ITokenService>                 _tokenServiceMock = new();
    private readonly Mock<IRefreshTokenRepository>       _repoMock         = new();
    private readonly Mock<ILogger<UserService>>          _loggerMock       = new();
    private readonly IMapper                             _mapper;

    public UserServiceTests()
    {
        var userStore = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        var roleStore = new Mock<IRoleStore<ApplicationRole>>();
        _roleManagerMock = new Mock<RoleManager<ApplicationRole>>(
            roleStore.Object, null!, null!, null!, null!);

        _mapper = new MapperConfiguration(
                cfg => cfg.AddProfile<IdentityProfile>(),
                LoggerFactory.Create(_ => { }))
            .CreateMapper();
    }

    private UserService CreateService() => new(
        _userManagerMock.Object,
        _roleManagerMock.Object,
        _tokenServiceMock.Object,
        _mapper,
        _repoMock.Object,
        _loggerMock.Object);

    private static ApplicationUser MakeUser(string email = "user@example.com", string userName = "testuser") => new()
    {
        Id       = Guid.NewGuid(),
        Email    = email,
        UserName = userName
    };

    // ── RegisterUserAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task RegisterUserAsync_WhenSuccessful_ReturnsSucceededResult()
    {
        _userManagerMock.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), "User"))
            .ReturnsAsync(IdentityResult.Success);

        var result = await CreateService().RegisterUserAsync(
            new RegisterDto { UserName = "alice", Email = "alice@example.com", Password = "Pass@1234" });

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task RegisterUserAsync_WhenSuccessful_AddsUserToUserRole()
    {
        _userManagerMock.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), "User"))
            .ReturnsAsync(IdentityResult.Success);

        await CreateService().RegisterUserAsync(
            new RegisterDto { UserName = "alice", Email = "alice@example.com", Password = "Pass@1234" });

        _userManagerMock.Verify(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), "User"), Times.Once);
    }

    [Fact]
    public async Task RegisterUserAsync_WhenCreationFails_DoesNotAddToRole()
    {
        _userManagerMock.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Duplicate email." }));

        await CreateService().RegisterUserAsync(
            new RegisterDto { UserName = "alice", Email = "dup@example.com", Password = "Pass@1234" });

        _userManagerMock.Verify(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RegisterUserAsync_WhenCreationFails_ReturnsFailedResult()
    {
        var error = new IdentityError { Description = "Weak password." };
        _userManagerMock.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(error));

        var result = await CreateService().RegisterUserAsync(
            new RegisterDto { UserName = "bob", Email = "bob@example.com", Password = "weak" });

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Description == "Weak password.");
    }

    // ── LoginUserAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task LoginUserAsync_WhenUserNotFound_ThrowsAuthenticationException()
    {
        _userManagerMock.Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((ApplicationUser?)null);

        var act = () => CreateService().LoginUserAsync(
            new LoginDto { Email = "ghost@example.com", Password = "Pass@1234" });

        await act.Should().ThrowAsync<AuthenticationException>()
            .WithMessage("*Invalid email or password*");
    }

    [Fact]
    public async Task LoginUserAsync_WhenPasswordInvalid_ThrowsAuthenticationException()
    {
        var user = MakeUser();
        _userManagerMock.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.CheckPasswordAsync(user, It.IsAny<string>())).ReturnsAsync(false);

        var act = () => CreateService().LoginUserAsync(
            new LoginDto { Email = user.Email!, Password = "WrongPass" });

        await act.Should().ThrowAsync<AuthenticationException>()
            .WithMessage("*Invalid email or password*");
    }

    [Fact]
    public async Task LoginUserAsync_WhenCredentialsValid_ReturnsAuthResponse()
    {
        var user     = MakeUser();
        var expected = new AuthResponseDto { AccessToken = "jwt", RefreshToken = "rt" };
        _userManagerMock.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.CheckPasswordAsync(user, "Pass@1234")).ReturnsAsync(true);
        _tokenServiceMock.Setup(t => t.GenerateTokensAsync(user, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await CreateService().LoginUserAsync(
            new LoginDto { Email = user.Email!, Password = "Pass@1234" });

        result.Should().Be(expected);
    }

    // ── GetUsersAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetUsersAsync_WhenNoUsers_ReturnsEmptyList()
    {
        _userManagerMock.Setup(m => m.Users)
            .Returns(new List<ApplicationUser>().AsAsyncQueryable());

        var result = await CreateService().GetUsersAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUsersAsync_MapsUsersAndEnrichesRoles()
    {
        var user = MakeUser("alice@example.com", "alice");
        _userManagerMock.Setup(m => m.Users)
            .Returns(new List<ApplicationUser> { user }.AsAsyncQueryable());
        _userManagerMock.Setup(m => m.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "User" });

        var result = (await CreateService().GetUsersAsync()).ToList();

        result.Should().HaveCount(1);
        result[0].Email.Should().Be("alice@example.com");
        result[0].Roles.Should().ContainSingle().Which.Should().Be("User");
    }

    // ── GetUserByIdAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetUserByIdAsync_WhenUserNotFound_ReturnsNull()
    {
        _userManagerMock.Setup(m => m.FindByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((ApplicationUser?)null);

        var result = await CreateService().GetUserByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetUserByIdAsync_WhenFound_ReturnsMappedUserWithRoles()
    {
        var user = MakeUser();
        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Admin" });

        var result = await CreateService().GetUserByIdAsync(user.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
        result.Roles.Should().ContainSingle().Which.Should().Be("Admin");
    }

    // ── GetUserRefreshTokensAsync ─────────────────────────────────────────────

    [Fact]
    public async Task GetUserRefreshTokensAsync_ReturnsMappedTokens()
    {
        var userId = Guid.NewGuid();
        var token = new RefreshToken
        {
            Id = 1, Token = "tok", Expires = DateTime.UtcNow.AddDays(7),
            UserId = userId, User = new ApplicationUser { Email = "u@u.com" }
        };
        _repoMock.Setup(r => r.GetUserTokensAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RefreshToken> { token });

        var result = (await CreateService().GetUserRefreshTokensAsync(userId)).ToList();

        result.Should().HaveCount(1);
        result[0].Token.Should().Be("tok");
    }

    // ── AddUserToRoleAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task AddUserToRoleAsync_WhenUserNotFound_ReturnsFailedResult()
    {
        _userManagerMock.Setup(m => m.FindByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((ApplicationUser?)null);

        var result = await CreateService().AddUserToRoleAsync(Guid.NewGuid(), "Admin");

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Description == "User not found.");
    }

    [Fact]
    public async Task AddUserToRoleAsync_WhenRoleNotFound_ReturnsFailedResult()
    {
        var user = MakeUser();
        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _roleManagerMock.Setup(m => m.RoleExistsAsync("NonExistent")).ReturnsAsync(false);

        var result = await CreateService().AddUserToRoleAsync(user.Id, "NonExistent");

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Description == "Role not found.");
    }

    [Fact]
    public async Task AddUserToRoleAsync_WhenBothExist_AddsToRole()
    {
        var user = MakeUser();
        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _roleManagerMock.Setup(m => m.RoleExistsAsync("Admin")).ReturnsAsync(true);
        _userManagerMock.Setup(m => m.AddToRoleAsync(user, "Admin")).ReturnsAsync(IdentityResult.Success);

        var result = await CreateService().AddUserToRoleAsync(user.Id, "Admin");

        result.Succeeded.Should().BeTrue();
    }

    // ── RemoveUserFromRoleAsync ───────────────────────────────────────────────

    [Fact]
    public async Task RemoveUserFromRoleAsync_WhenUserNotFound_ReturnsFailedResult()
    {
        _userManagerMock.Setup(m => m.FindByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((ApplicationUser?)null);

        var result = await CreateService().RemoveUserFromRoleAsync(Guid.NewGuid(), "User");

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Description == "User not found.");
    }

    [Fact]
    public async Task RemoveUserFromRoleAsync_WhenUserFound_RemovesFromRole()
    {
        var user = MakeUser();
        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.RemoveFromRoleAsync(user, "User")).ReturnsAsync(IdentityResult.Success);

        var result = await CreateService().RemoveUserFromRoleAsync(user.Id, "User");

        result.Succeeded.Should().BeTrue();
    }

    // ── UpdateAvatarUrlAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAvatarUrlAsync_WhenUserNotFound_DoesNotCallUpdate()
    {
        _userManagerMock.Setup(m => m.FindByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((ApplicationUser?)null);

        await CreateService().UpdateAvatarUrlAsync(Guid.NewGuid(), "https://example.com/avatar.jpg");

        _userManagerMock.Verify(m => m.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAvatarUrlAsync_WhenUserFound_UpdatesAvatarUrl()
    {
        var user    = MakeUser();
        var newUrl  = "https://cdn.example.com/new-avatar.jpg";
        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

        await CreateService().UpdateAvatarUrlAsync(user.Id, newUrl);

        user.AvatarUrl.Should().Be(newUrl);
        _userManagerMock.Verify(m => m.UpdateAsync(user), Times.Once);
    }

    // ── UpdateProfileAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateProfileAsync_WhenUserNotFound_ReturnsNull()
    {
        _userManagerMock.Setup(m => m.FindByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((ApplicationUser?)null);

        var result = await CreateService().UpdateProfileAsync(Guid.NewGuid(),
            new UpdateProfileDto { FullName = "Alice" });

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateProfileAsync_TrimsAndSavesFullNameAndSpecialization()
    {
        var user = MakeUser();
        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string>());

        var result = await CreateService().UpdateProfileAsync(user.Id,
            new UpdateProfileDto { FullName = "  Alice Smith  ", Specialization = "  Developer  " });

        user.FullName.Should().Be("Alice Smith");
        user.Specialization.Should().Be("Developer");
    }

    [Fact]
    public async Task UpdateProfileAsync_WhenSuccessful_ReturnsMappedUserDto()
    {
        var user = MakeUser();
        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "User" });

        var result = await CreateService().UpdateProfileAsync(user.Id,
            new UpdateProfileDto { FullName = "Alice", Specialization = "Dev" });

        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
    }

    // ── ChangePasswordAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task ChangePasswordAsync_WhenUserNotFound_ReturnsFailedResult()
    {
        _userManagerMock.Setup(m => m.FindByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((ApplicationUser?)null);

        var result = await CreateService().ChangePasswordAsync(Guid.NewGuid(),
            new ChangePasswordDto { CurrentPassword = "Old@1234", NewPassword = "New@1234" });

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Description == "User not found.");
    }

    [Fact]
    public async Task ChangePasswordAsync_WhenUserFound_ChangesPassword()
    {
        var user = MakeUser();
        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.ChangePasswordAsync(user, "Old@1234", "New@1234"))
            .ReturnsAsync(IdentityResult.Success);

        var result = await CreateService().ChangePasswordAsync(user.Id,
            new ChangePasswordDto { CurrentPassword = "Old@1234", NewPassword = "New@1234" });

        result.Succeeded.Should().BeTrue();
    }

    // ── DeleteUserAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteUserAsync_WhenUserNotFound_ReturnsFailedResult()
    {
        _userManagerMock.Setup(m => m.FindByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((ApplicationUser?)null);

        var result = await CreateService().DeleteUserAsync(Guid.NewGuid());

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "UserNotFound");
    }

    [Fact]
    public async Task DeleteUserAsync_WhenUserFound_DeletesAndReturnsSuccess()
    {
        var user = MakeUser();
        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.DeleteAsync(user)).ReturnsAsync(IdentityResult.Success);

        var result = await CreateService().DeleteUserAsync(user.Id);

        result.Succeeded.Should().BeTrue();
        _userManagerMock.Verify(m => m.DeleteAsync(user), Times.Once);
    }
}
