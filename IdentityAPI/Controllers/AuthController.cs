using IdentityAPI.Keycloak;
using IdentityBLL.DTOs;
using IdentityBLL.Interfaces;
using IdentityServiceDomain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Minio;
using Minio.DataModel.Args;
using ServiceDefaults.Interfaces;
using ServiceDefaults.Security;

namespace IdentityAPI.Controllers;

/// <summary>
/// Profile and account administration. Keycloak owns credentials and roles, so
/// this controller never issues or refreshes a token: it reads the caller's
/// identity from the validated access token, mirrors it into a profile row, and
/// proxies the few Keycloak admin operations the product needs.
/// </summary>
[ApiController]
[Route("api/auth")]
[Authorize]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IAuditService _auditService;
    private readonly IKeycloakAdminClient _keycloakAdmin;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IMinioClient _minioClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IUserService userService,
        IAuditService auditService,
        IKeycloakAdminClient keycloakAdmin,
        ICurrentUserAccessor currentUser,
        IMinioClient minioClient,
        IConfiguration configuration,
        ILogger<AuthController> logger)
    {
        _userService = userService;
        _auditService = auditService;
        _keycloakAdmin = keycloakAdmin;
        _currentUser = currentUser;
        _minioClient = minioClient;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// The caller's profile, created on first call. The row is keyed by the token's
    /// "sub", so signing in through any IdP lands on the same profile.
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(CurrentUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CurrentUserDto>> GetCurrentUser(CancellationToken cancellationToken)
    {
        if (!TryGetSubject(out var subject))
            return Unauthorized(new { message = "Token does not carry a usable subject." });

        var existed = await _userService.GetUserByIdAsync(subject, cancellationToken) is not null;

        var user = await _userService.GetOrCreateAsync(
            subject, _currentUser.Email, _currentUser.UserName, cancellationToken);

        _logger.LogInformation(
            "User {UserId} read /me ({Mode}).", subject, existed ? "existing" : "provisioned");

        return Ok(new CurrentUserDto
        {
            Id = user.Id,
            Email = user.Email,
            UserName = user.UserName,
            FullName = user.FullName,
            Specialization = user.Specialization,
            AvatarUrl = user.AvatarUrl,
            CreatedAt = user.CreatedAt,
            JustProvisioned = !existed,

            // Authoritative source of roles is the token, not the database.
            Roles = _currentUser.Roles.ToList()
        });
    }

    /// <summary>
    /// Whether the caller has an authenticator app, plus the Keycloak account
    /// console URL where it is configured. Read from the user's credentials in
    /// Keycloak (an "otp" credential means an authenticator app is set up).
    /// </summary>
    [HttpGet("me/2fa")]
    [ProducesResponseType(typeof(TwoFactorStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<TwoFactorStatusDto>> GetTwoFactorStatus(CancellationToken cancellationToken)
    {
        if (!TryGetSubject(out var subject))
            return Unauthorized(new { message = "Token does not carry a usable subject." });

        try
        {
            var configured = await _keycloakAdmin.IsOtpConfiguredAsync(subject, cancellationToken);

            return Ok(new TwoFactorStatusDto
            {
                TwoFactorConfigured = configured,
                AccountConsoleUrl = _keycloakAdmin.AccountConsoleUrl
            });
        }
        catch (KeycloakAdminException ex)
        {
            _logger.LogError(ex, "Could not read 2FA status for {UserId}.", subject);
            return StatusCode(StatusCodes.Status502BadGateway,
                new { message = "Two-factor status is temporarily unavailable." });
        }
    }

    /// <summary>Search Keycloak users. Administrators only.</summary>
    [HttpGet("users")]
    [Authorize(Policy = IdeaFusionPolicies.RequireAdministrator)]
    [ProducesResponseType(typeof(IReadOnlyList<KeycloakUserSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<IReadOnlyList<KeycloakUserSummaryDto>>> SearchUsers(
        [FromQuery] string? search,
        [FromQuery] int first = 0,
        [FromQuery] int max = 25,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var users = await _keycloakAdmin.SearchUsersAsync(search, first, max, cancellationToken);

            return Ok(users.Select(u => new KeycloakUserSummaryDto
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                EmailVerified = u.EmailVerified,
                Enabled = u.Enabled,
                Roles = u.RealmRoles,
                TwoFactorConfigured = u.TwoFactorConfigured,
                CreatedAt = u.CreatedAt
            }).ToList());
        }
        catch (KeycloakAdminException ex)
        {
            _logger.LogError(ex, "Keycloak user search failed for '{Search}'.", search);
            return StatusCode(StatusCodes.Status502BadGateway,
                new { message = "User search is temporarily unavailable." });
        }
    }

    /// <summary>Grant realm roles to a user. Administrators only. Audited.</summary>
    [HttpPost("users/{userId:guid}/roles")]
    [Authorize(Policy = IdeaFusionPolicies.RequireAdministrator)]
    [ProducesResponseType(typeof(UserRolesResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public Task<ActionResult<UserRolesResultDto>> GrantRoles(
        Guid userId,
        [FromBody] UpdateUserRolesDto dto,
        CancellationToken cancellationToken)
        => ChangeRolesAsync(userId, dto, grant: true, cancellationToken);

    /// <summary>Revoke realm roles from a user. Administrators only. Audited.</summary>
    [HttpDelete("users/{userId:guid}/roles")]
    [Authorize(Policy = IdeaFusionPolicies.RequireAdministrator)]
    [ProducesResponseType(typeof(UserRolesResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public Task<ActionResult<UserRolesResultDto>> RevokeRoles(
        Guid userId,
        [FromBody] UpdateUserRolesDto dto,
        CancellationToken cancellationToken)
        => ChangeRolesAsync(userId, dto, grant: false, cancellationToken);

    /// <summary>
    /// Update the current user's profile (full name, specialization).
    /// </summary>
    [HttpPut("profile")]
    [ProducesResponseType(typeof(CurrentUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CurrentUserDto>> UpdateProfile([FromBody] UpdateProfileDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        if (!TryGetSubject(out var subject))
            return Unauthorized(new { message = "Token does not carry a usable subject." });

        var updated = await _userService.UpdateProfileAsync(subject, dto, cancellationToken);
        if (updated == null)
            return NotFound(new { message = "User not found." });

        await WriteAuditAsync(
            action: "user.profile.update",
            targetId: subject.ToString(),
            targetName: updated.Email,
            category: AuditCategory.User,
            details: $"{{\"fullName\":{System.Text.Json.JsonSerializer.Serialize(dto.FullName)}}}",
            cancellationToken);

        return Ok(ToCurrentUser(updated, justProvisioned: false));
    }

    /// <summary>Upload or replace the current user's avatar image.</summary>
    [HttpPost("avatar")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadAvatar(IFormFile file, CancellationToken cancellationToken)
    {
        if (!TryValidateImage(file, out var error))
            return BadRequest(new { message = error });

        if (!TryGetSubject(out var subject))
            return Unauthorized(new { message = "Token does not carry a usable subject." });

        const string bucketName = "avatars";
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var objectName = $"{subject}/{Guid.NewGuid()}{ext}";

        await UploadToMinioAsync(bucketName, objectName, file, cancellationToken);

        var endpoint = _configuration["Minio:Endpoint"] ?? "localhost:9000";
        var avatarUrl = $"http://{endpoint}/{bucketName}/{objectName}";

        await _userService.UpdateAvatarUrlAsync(subject, avatarUrl, cancellationToken);
        _logger.LogInformation("User {UserId} updated avatar: {AvatarUrl}", subject, avatarUrl);

        return Ok(new { avatarUrl });
    }

    /// <summary>Upload any image file to storage and return its public URL.</summary>
    [HttpPost("upload")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadFile(IFormFile file, CancellationToken cancellationToken)
    {
        if (!TryValidateImage(file, out var error))
            return BadRequest(new { message = error });

        const string bucketName = "uploads";
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var objectName = $"{Guid.NewGuid()}{ext}";

        await UploadToMinioAsync(bucketName, objectName, file, cancellationToken);

        var endpoint = _configuration["Minio:Endpoint"] ?? "localhost:9000";
        var url = $"http://{endpoint}/{bucketName}/{objectName}";

        _logger.LogInformation("File uploaded: {Url}", url);

        return Ok(new { url });
    }

    // ── Internals ──────────────────────────────────────────

    private async Task<ActionResult<UserRolesResultDto>> ChangeRolesAsync(
        Guid userId,
        UpdateUserRolesDto dto,
        bool grant,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        if (dto.Roles.Count == 0)
            return BadRequest(new { message = "At least one role is required." });

        if (!TryGetSubject(out var actorId))
            return Unauthorized(new { message = "Token does not carry a usable subject." });

        // Removing your own administrator role would leave the realm with nobody
        // able to put it back, so that one change is refused.
        if (!grant &&
            userId == actorId &&
            dto.Roles.Any(r => r.Equals(ServiceDefaults.Security.IdeaFusionRoles.Administrator, StringComparison.OrdinalIgnoreCase)))
        {
            return Conflict(new { message = "You cannot revoke your own administrator role." });
        }

        IReadOnlyList<string> roles;
        try
        {
            roles = grant
                ? await _keycloakAdmin.AddRolesAsync(userId, dto.Roles, cancellationToken)
                : await _keycloakAdmin.RemoveRolesAsync(userId, dto.Roles, cancellationToken);
        }
        catch (KeycloakAdminException ex) when (ex.StatusCode is System.Net.HttpStatusCode.NotFound)
        {
            return NotFound(new { message = "User not found in Keycloak." });
        }
        catch (KeycloakAdminException ex)
        {
            _logger.LogError(ex, "Role {Verb} failed for user {UserId}.", grant ? "grant" : "revoke", userId);
            return StatusCode(StatusCodes.Status502BadGateway,
                new { message = "Role change is temporarily unavailable." });
        }

        // The audit write is what makes a role change accountable, so a failure
        // here fails the request rather than silently dropping the record.
        await WriteAuditAsync(
            action: grant ? "user.role.grant" : "user.role.revoke",
            targetId: userId.ToString(),
            targetName: null,
            category: AuditCategory.User,
            details: System.Text.Json.JsonSerializer.Serialize(new
            {
                requested = dto.Roles,
                effective = roles
            }),
            cancellationToken);

        return Ok(new UserRolesResultDto { UserId = userId, Roles = roles });
    }

    private Task WriteAuditAsync(
        string action,
        string targetId,
        string? targetName,
        AuditCategory category,
        string? details,
        CancellationToken cancellationToken)
    {
        if (!TryGetSubject(out var actorId))
        {
            _logger.LogError("Refusing to write audit entry '{Action}' with no identifiable actor.", action);
            throw new UnauthorizedAccessException("Audit entry has no identifiable actor.");
        }

        return _auditService.RecordAsync(
            new AuditRecordDto
            {
                ActorId = actorId,
                ActorName = _currentUser.UserName ?? _currentUser.Email,
                Action = action,
                TargetType = "user",
                TargetId = targetId,
                TargetName = targetName,
                Details = details,
                Category = category
            },
            cancellationToken);
    }

    private CurrentUserDto ToCurrentUser(UserDto user, bool justProvisioned) => new()
    {
        Id = user.Id,
        Email = user.Email,
        UserName = user.UserName,
        FullName = user.FullName,
        Specialization = user.Specialization,
        AvatarUrl = user.AvatarUrl,
        CreatedAt = user.CreatedAt,
        JustProvisioned = justProvisioned,
        Roles = _currentUser.Roles.ToList()
    };

    private bool TryGetSubject(out Guid subject)
    {
        if (_currentUser.UserId is { } id && id != Guid.Empty)
        {
            subject = id;
            return true;
        }

        subject = Guid.Empty;
        return false;
    }

    private static bool TryValidateImage(IFormFile? file, out string error)
    {
        if (file == null || file.Length == 0)
        {
            error = "No file provided.";
            return false;
        }

        if (file.Length > 5 * 1024 * 1024)
        {
            error = "File exceeds 5 MB limit.";
            return false;
        }

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp", "image/gif" };
        if (!allowedTypes.Contains(file.ContentType?.ToLowerInvariant()))
        {
            error = "Only JPEG, PNG, WebP or GIF images are allowed.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private async Task UploadToMinioAsync(
        string bucketName,
        string objectName,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        var bucketExistsArgs = new BucketExistsArgs().WithBucket(bucketName);
        if (!await _minioClient.BucketExistsAsync(bucketExistsArgs, cancellationToken))
        {
            await _minioClient.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucketName), cancellationToken);
            var policy = $@"{{""Version"":""2012-10-17"",""Statement"":[{{""Effect"":""Allow"",""Principal"":{{""AWS"":[""*""]}},""Action"":[""s3:GetObject""],""Resource"":[""arn:aws:s3:::{bucketName}/*""]}}]}}";
            await _minioClient.SetPolicyAsync(new SetPolicyArgs().WithBucket(bucketName).WithPolicy(policy), cancellationToken);
        }

        using var stream = file.OpenReadStream();
        var putArgs = new PutObjectArgs()
            .WithBucket(bucketName)
            .WithObject(objectName)
            .WithStreamData(stream)
            .WithObjectSize(file.Length)
            .WithContentType(file.ContentType);

        await _minioClient.PutObjectAsync(putArgs, cancellationToken);
    }
}
