using IdentityBLL.DTOs;
using IdentityBLL.Interfaces;
using IdentityServiceDomain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceDefaults.Interfaces;
using ServiceDefaults.Security;

namespace IdentityAPI.Controllers;

/// <summary>
/// Local profile records. Keycloak owns the user list and the roles, so the
/// searchable, authoritative user view lives on <c>/api/auth/users</c>; this
/// controller only exposes the profile rows this service mirrors.
/// </summary>
[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        IUserService userService,
        IAuditService auditService,
        ICurrentUserAccessor currentUser,
        ILogger<UsersController> logger)
    {
        _userService = userService;
        _auditService = auditService;
        _currentUser = currentUser;
        _logger = logger;
    }

    /// <summary>
    /// All mirrored profiles. Administrators only: the list covers every account
    /// in the realm, so it is not something anonymous callers may enumerate.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = IdeaFusionPolicies.RequireAdministrator)]
    [ProducesResponseType(typeof(IEnumerable<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers(CancellationToken cancellationToken)
    {
        var users = await _userService.GetUsersAsync(cancellationToken);
        return Ok(users);
    }

    /// <summary>
    /// Get a specific mirrored profile by ID.
    /// </summary>
    [HttpGet("{userId:guid}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetUser(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _userService.GetUserByIdAsync(userId, cancellationToken);
        if (user == null)
            return NotFound(new { message = "User not found." });

        return Ok(user);
    }

    /// <summary>
    /// Permanently delete a mirrored profile. Administrators only. Audited.
    /// This removes the local profile row; the Keycloak account itself is unaffected.
    /// </summary>
    [HttpDelete("{userId:guid}")]
    [Authorize(Policy = IdeaFusionPolicies.RequireAdministrator)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser(Guid userId, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is { } currentUserId && userId == currentUserId)
            return BadRequest(new { message = "Administrators cannot delete their own account." });

        var result = await _userService.DeleteUserAsync(userId, cancellationToken);
        if (!result.Succeeded)
        {
            if (result.Errors.Any(e => e.Code == "UserNotFound"))
                return NotFound(new { message = "User not found." });

            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }

        await _auditService.RecordAsync(
            new AuditRecordDto
            {
                ActorId = _currentUser.UserId ?? Guid.Empty,
                ActorName = _currentUser.UserName ?? _currentUser.Email,
                Action = "user.profile.delete",
                TargetType = "user",
                TargetId = userId.ToString(),
                Category = AuditCategory.User
            },
            cancellationToken);

        _logger.LogInformation("Administrator deleted profile {UserId}.", userId);

        return NoContent();
    }
}
