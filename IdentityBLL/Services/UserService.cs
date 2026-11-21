using AutoMapper;
using IdentityBLL.DTOs;
using IdentityBLL.Interfaces;
using IdentityServiceDomain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IdentityBLL.Services;

public class UserService : IUserService
{
    /// <summary>
    /// Keycloak does not guarantee an email on every account (social logins can
    /// omit it), but the users table wants something addressable, so fall back to
    /// a deterministic placeholder rather than storing null.
    /// </summary>
    private const string PlaceholderEmailDomain = "@keycloak.local";

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMapper _mapper;
    private readonly ILogger<UserService> _logger;

    public UserService(
        UserManager<ApplicationUser> userManager,
        IMapper mapper,
        ILogger<UserService> logger)
    {
        _userManager = userManager;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IEnumerable<UserDto>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await _userManager.Users.ToListAsync(cancellationToken);
        return _mapper.Map<List<UserDto>>(users);
    }

    public async Task<UserDto?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        return user is null ? null : _mapper.Map<UserDto>(user);
    }

    public async Task<UserDto> GetOrCreateAsync(
        Guid subject,
        string? email,
        string? userName,
        CancellationToken cancellationToken = default)
    {
        var id = subject.ToString();

        var existing = await _userManager.FindByIdAsync(id);
        if (existing is not null)
            return _mapper.Map<UserDto>(existing);

        var resolvedEmail = string.IsNullOrWhiteSpace(email)
            ? $"{id}{PlaceholderEmailDomain}"
            : email.Trim();

        var user = new ApplicationUser
        {
            Id = subject,
            UserName = string.IsNullOrWhiteSpace(userName) ? resolvedEmail : userName.Trim(),
            Email = resolvedEmail,

            // There is no local password to verify: Keycloak owns credentials. The
            // sentinel keeps the row looking like a normal, validated account.
            PasswordHash = ApplicationUser.ExternalPasswordSentinel,
            SecurityStamp = Guid.NewGuid().ToString(),
        };

        _logger.LogInformation("Auto-provisioning profile for Keycloak subject {Subject}.", subject);

        var result = await _userManager.CreateAsync(user);

        if (result.Succeeded)
            return _mapper.Map<UserDto>(user);

        // A concurrent first request for the same subject can win the race and
        // insert the row. That is a success from the caller's point of view.
        var raced = await _userManager.FindByIdAsync(id);
        if (raced is not null)
            return _mapper.Map<UserDto>(raced);

        _logger.LogError(
            "Failed to auto-provision profile for subject {Subject}: {Errors}",
            subject,
            string.Join("; ", result.Errors.Select(e => e.Description)));

        throw new InvalidOperationException(
            $"Could not create a profile for subject {subject}.");
    }

    public async Task UpdateAvatarUrlAsync(Guid userId, string avatarUrl, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return;

        user.AvatarUrl = avatarUrl;
        await _userManager.UpdateAsync(user);
    }

    public async Task<UserDto?> UpdateProfileAsync(Guid userId, UpdateProfileDto dto, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return null;

        user.FullName = dto.FullName?.Trim();
        user.Specialization = dto.Specialization?.Trim();

        await _userManager.UpdateAsync(user);

        return _mapper.Map<UserDto>(user);
    }

    public async Task<IdentityResult> DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return IdentityResult.Failed(new IdentityError { Code = "UserNotFound", Description = "User not found." });

        return await _userManager.DeleteAsync(user);
    }
}
