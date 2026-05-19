using IdentityBLL.DTOs;
using IdentityBLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Minio;
using Minio.DataModel.Args;
using ServiceDefaults.Extensions;

namespace IdentityAPI.Controllers;

[ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ITokenService _tokenService;
        private readonly IMinioClient _minioClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IUserService userService,
            ITokenService tokenService,
            IMinioClient minioClient,
            IConfiguration configuration,
            ILogger<AuthController> logger)
        {
            _userService = userService;
            _tokenService = tokenService;
            _minioClient = minioClient;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Register a new user account.
        /// </summary>
        [HttpPost("register")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            _logger.LogInformation("User registration requested for {Email}", registerDto.Email);
            var result = await _userService.RegisterUserAsync(registerDto, cancellationToken);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            _logger.LogInformation("User {Email} registered successfully.", registerDto.Email);
            return CreatedAtAction(nameof(Register), new { registerDto.Email }, null);
        }

        /// <summary>
        /// Authenticate a user and issue access/refresh tokens.
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto loginDto, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            _logger.LogInformation("User {Email} attempting to log in.", loginDto.Email);
            var tokens = await _userService.LoginUserAsync(loginDto, cancellationToken);
            return Ok(tokens);
        }

        /// <summary>
        /// Get info about the currently authenticated user.
        /// </summary>
        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<UserDto>> GetCurrentUser(CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            var user = await _userService.GetUserByIdAsync(userId, cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found while requesting /me endpoint.", userId);
                return NotFound(new { message = "User not found." });
            }

            _logger.LogInformation("User {UserId} accessed /me endpoint.", userId);
            return Ok(user);
        }

        /// <summary>
        /// Refresh the access token using a valid refresh token.
        /// </summary>
        [HttpPost("refresh")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<AuthResponseDto>> RefreshToken([FromBody] TokenRequestDto tokenRequest, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            _logger.LogInformation("Token refresh requested.");
            var newTokens = await _tokenService.RefreshTokensAsync(tokenRequest, cancellationToken);
            return Ok(newTokens);
        }

        /// <summary>
        /// Revoke (invalidate) a specific refresh token.
        /// </summary>
        [HttpPost("revoke")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Revoke([FromBody] TokenRequestDto tokenRequest, CancellationToken cancellationToken)
        {
            var success = await _tokenService.RevokeTokenAsync(tokenRequest.RefreshToken, cancellationToken);

            if (!success)
                return BadRequest(new { message = "Invalid or already revoked token." });

            _logger.LogInformation("Successfully revoked refresh token for user {User}", User.Identity?.Name);
            return NoContent();
        }

        /// <summary>
        /// Log out current user (revoke all active refresh tokens).
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Logout(CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            await _tokenService.RevokeAllUserTokensAsync(userId, cancellationToken);
            _logger.LogInformation("User {UserId} logged out successfully (all tokens revoked).", userId);

            return NoContent();
        }

        /// <summary>
        /// Update the current user's profile (full name, specialization).
        /// </summary>
        [HttpPut("profile")]
        [Authorize]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto, CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            var updated = await _userService.UpdateProfileAsync(userId, dto, cancellationToken);
            if (updated == null)
                return NotFound(new { message = "User not found." });

            _logger.LogInformation("User {UserId} updated profile", userId);
            return Ok(updated);
        }

        /// <summary>
        /// Change the current user's password.
        /// </summary>
        [HttpPost("change-password")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var userId = User.GetUserId();
            var result = await _userService.ChangePasswordAsync(userId, dto, cancellationToken);
            if (!result.Succeeded)
                return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

            _logger.LogInformation("User {UserId} changed password", userId);
            return NoContent();
        }

        /// <summary>
        /// Upload or replace the current user's avatar image.
        /// </summary>
        [HttpPost("avatar")]
        [Authorize]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UploadAvatar(IFormFile file, CancellationToken cancellationToken)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file provided." });

            if (file.Length > 5 * 1024 * 1024)
                return BadRequest(new { message = "File exceeds 5 MB limit." });

            var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp", "image/gif" };
            if (!allowedTypes.Contains(file.ContentType.ToLowerInvariant()))
                return BadRequest(new { message = "Only JPEG, PNG, WebP or GIF images are allowed." });

            var userId = User.GetUserId();
            const string bucketName = "avatars";
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var objectName = $"{userId}/{Guid.NewGuid()}{ext}";

            var bucketExistsArgs = new BucketExistsArgs().WithBucket(bucketName);
            bool exists = await _minioClient.BucketExistsAsync(bucketExistsArgs, cancellationToken);
            if (!exists)
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

            var endpoint = _configuration["Minio:Endpoint"] ?? "localhost:9000";
            var avatarUrl = $"http://{endpoint}/{bucketName}/{objectName}";

            await _userService.UpdateAvatarUrlAsync(userId, avatarUrl, cancellationToken);
            _logger.LogInformation("User {UserId} updated avatar: {AvatarUrl}", userId, avatarUrl);

            return Ok(new { avatarUrl });
        }

        /// <summary>
        /// Upload any image file to storage and return its public URL.
        /// </summary>
        [HttpPost("upload")]
        [Authorize]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UploadFile(IFormFile file, CancellationToken cancellationToken)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file provided." });

            if (file.Length > 5 * 1024 * 1024)
                return BadRequest(new { message = "File exceeds 5 MB limit." });

            var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp", "image/gif" };
            if (!allowedTypes.Contains(file.ContentType.ToLowerInvariant()))
                return BadRequest(new { message = "Only JPEG, PNG, WebP or GIF images are allowed." });

            const string bucketName = "uploads";
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var objectName = $"{Guid.NewGuid()}{ext}";

            var bucketExistsArgs = new BucketExistsArgs().WithBucket(bucketName);
            bool exists = await _minioClient.BucketExistsAsync(bucketExistsArgs, cancellationToken);
            if (!exists)
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

            var endpoint = _configuration["Minio:Endpoint"] ?? "localhost:9000";
            var url = $"http://{endpoint}/{bucketName}/{objectName}";

            _logger.LogInformation("File uploaded: {Url}", url);
            return Ok(new { url });
        }
    }