using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Grpc.Core;
using IdeaFusion.Grpc.Users;
using IdentityBLL.Configuration;
using IdentityBLL.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace IdentityAPI.GrpcServices;

public class UserGrpcServiceImpl : UserGrpcService.UserGrpcServiceBase
{
    private readonly IUserService _userService;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<UserGrpcServiceImpl> _logger;

    public UserGrpcServiceImpl(
        IUserService userService,
        IOptions<JwtSettings> jwtSettings,
        ILogger<UserGrpcServiceImpl> logger)
    {
        _userService = userService;
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
    }

    public override async Task<UserResponse> GetUserById(GetUserByIdRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.UserId, out var userId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid user ID format."));

        var user = await _userService.GetUserByIdAsync(userId, context.CancellationToken);

        if (user == null)
            throw new RpcException(new Status(StatusCode.NotFound, $"User {userId} not found."));

        return new UserResponse
        {
            User = new UserDto
            {
                UserId    = user.Id.ToString(),
                UserName  = user.Email,
                Email     = user.Email,
                AvatarUrl = string.Empty,
                CreatedAt = user.CreatedAt.ToString("O")
            }
        };
    }

    public override async Task<UsersResponse> GetUsersByIds(GetUsersByIdsRequest request, ServerCallContext context)
    {
        var response = new UsersResponse();

        var tasks = request.UserIds
            .Select(id => Guid.TryParse(id, out var guid)
                ? _userService.GetUserByIdAsync(guid, context.CancellationToken)
                : Task.FromResult<IdentityBLL.DTOs.UserDto?>(null));

        var users = await Task.WhenAll(tasks);

        response.Users.AddRange(users
            .Where(u => u != null)
            .Select(u => new UserDto
            {
                UserId    = u!.Id.ToString(),
                UserName  = u.Email,
                Email     = u.Email,
                AvatarUrl = string.Empty,
                CreatedAt = u.CreatedAt.ToString("O")
            }));

        return response;
    }

    public override Task<ValidateTokenResponse> ValidateToken(ValidateTokenRequest request, ServerCallContext context)
    {
        try
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));

            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParams = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey         = key,
                ValidateIssuer           = true,
                ValidIssuer              = _jwtSettings.Issuer,
                ValidateAudience         = true,
                ValidAudience            = _jwtSettings.Audience,
                ValidateLifetime         = true,
                ClockSkew                = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(
                request.AccessToken, validationParams, out _);

            var userId   = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                           ?? principal.FindFirstValue("sub")
                           ?? string.Empty;
            var userName = principal.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
            var email    = principal.FindFirstValue(ClaimTypes.Email)
                           ?? principal.FindFirstValue("email")
                           ?? string.Empty;

            return Task.FromResult(new ValidateTokenResponse
            {
                IsValid  = true,
                UserId   = userId,
                UserName = userName,
                Email    = email
            });
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning(ex, "Token validation failed.");
            return Task.FromResult(new ValidateTokenResponse { IsValid = false });
        }
    }

    public override async Task<CheckUserExistsResponse> CheckUserExists(CheckUserExistsRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.UserId, out var userId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid user ID format."));

        var user = await _userService.GetUserByIdAsync(userId, context.CancellationToken);

        return new CheckUserExistsResponse { Exists = user != null };
    }
}