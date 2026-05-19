using IdentityBLL.DTOs;
using IdentityBLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityAPI.Controllers;

[ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IUserService userService, ILogger<UsersController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        /// <summary>
        /// Get a list of all registered users.
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<UserDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers(CancellationToken cancellationToken)
        {
            _logger.LogInformation("User requested user list");
            var users = await _userService.GetUsersAsync(cancellationToken);
            return Ok(users);
        }

        /// <summary>
        /// Get a specific user by ID.
        /// </summary>
        [HttpGet("{userId:guid}")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserDto>> GetUser(Guid userId, CancellationToken cancellationToken)
        {
            _logger.LogInformation("User requested user {UserId}", userId);
            var user = await _userService.GetUserByIdAsync(userId, cancellationToken);
            if (user == null)
                return NotFound(new { message = "User not found." });
            return Ok(user);
        }

        /// <summary>
        /// Get all refresh tokens for a specific user. Admin only.
        /// </summary>
        [HttpGet("{userId:guid}/tokens")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(IEnumerable<RefreshTokenDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<RefreshTokenDto>>> GetUserTokens(Guid userId, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Admin requested refresh tokens for user {UserId}", userId);
            var tokens = await _userService.GetUserRefreshTokensAsync(userId, cancellationToken);
            return Ok(tokens);
        }

        /// <summary>
        /// Add a user to a specific role. Admin only.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost("roles/add")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddUserToRole([FromBody] UserRoleDto dto, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Admin adding user {UserId} to role {Role}", dto.UserId, dto.RoleName);
            var result = await _userService.AddUserToRoleAsync(dto.UserId, dto.RoleName, cancellationToken);
            if (!result.Succeeded)
                return BadRequest(result.Errors);
            return NoContent();
        }

        /// <summary>
        /// Remove a user from a specific role. Admin only.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost("roles/remove")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RemoveUserFromRole([FromBody] UserRoleDto dto, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Admin removing user {UserId} from role {Role}", dto.UserId, dto.RoleName);
            var result = await _userService.RemoveUserFromRoleAsync(dto.UserId, dto.RoleName, cancellationToken);
            if (!result.Succeeded)
                return BadRequest(result.Errors);
            return NoContent();
        }

        /// <summary>
        /// Permanently delete a user account. Admin only.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpDelete("{userId:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteUser(Guid userId, CancellationToken cancellationToken)
        {
            var currentUserIdStr = User.FindFirst("sub")?.Value;
            if (currentUserIdStr != null && Guid.TryParse(currentUserIdStr, out var currentUserId) && userId == currentUserId)
                return BadRequest(new { message = "Admins cannot delete their own account." });

            _logger.LogInformation("Admin deleting user {UserId}", userId);
            var result = await _userService.DeleteUserAsync(userId, cancellationToken);
            if (!result.Succeeded)
            {
                if (result.Errors.Any(e => e.Code == "UserNotFound"))
                    return NotFound(new { message = "User not found." });
                return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
            }
            return NoContent();
        }
    }