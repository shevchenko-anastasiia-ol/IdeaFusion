using Grpc.Core;
using IdeaFusion.Grpc.Users;
using IdentityBLL.DTOs;
using IdentityBLL.Interfaces;
using IdentityServiceDomain.Entities;

namespace IdentityAPI.GrpcServices;

public class UserGrpcServiceImpl : UserGrpcService.UserGrpcServiceBase
{
    private readonly IUserService _userService;
    private readonly IAuditService _auditService;
    private readonly ILogger<UserGrpcServiceImpl> _logger;

    public UserGrpcServiceImpl(
        IUserService userService,
        IAuditService auditService,
        ILogger<UserGrpcServiceImpl> logger)
    {
        _userService = userService;
        _auditService = auditService;
        _logger = logger;
    }

    public override async Task<UserResponse> GetUserById(GetUserByIdRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.UserId, out var userId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid user ID format."));

        var user = await _userService.GetUserByIdAsync(userId, context.CancellationToken);

        if (user == null)
            throw new RpcException(new Status(StatusCode.NotFound, $"User {userId} not found."));

        return new UserResponse { User = ToUserDto(user) };
    }

    public override async Task<UsersResponse> GetUsersByIds(GetUsersByIdsRequest request, ServerCallContext context)
    {
        var response = new UsersResponse();

        var tasks = request.UserIds
            .Select(id => Guid.TryParse(id, out var guid)
                ? _userService.GetUserByIdAsync(guid, context.CancellationToken)
                : Task.FromResult<IdentityBLL.DTOs.UserDto?>(null));

        // One bad id in the batch is skipped rather than failing the whole call, so
        // a single malformed id cannot break a page of a list.
        var users = await Task.WhenAll(tasks);

        response.Users.AddRange(users
            .Where(u => u != null)
            .Select(ToUserDto));

        return response;
    }

    public override async Task<CheckUserExistsResponse> CheckUserExists(CheckUserExistsRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.UserId, out var userId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid user ID format."));

        var user = await _userService.GetUserByIdAsync(userId, context.CancellationToken);

        return new CheckUserExistsResponse { Exists = user != null };
    }

    public override async Task<RecordAuditResponse> RecordAudit(RecordAuditRequest request, ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.Action) ||
            string.IsNullOrWhiteSpace(request.TargetType) ||
            string.IsNullOrWhiteSpace(request.TargetId))
        {
            throw new RpcException(new Status(
                StatusCode.InvalidArgument,
                "action, target_type and target_id are required."));
        }

        // A caller that cannot name itself is not a caller we can attribute the
        // action to, so refuse rather than store a zero-actor entry.
        if (!Guid.TryParse(request.ActorId, out var actorId) || actorId == Guid.Empty)
        {
            throw new RpcException(new Status(
                StatusCode.InvalidArgument,
                "actor_id must be a non-empty GUID."));
        }

        var entry = await _auditService.RecordAsync(
            new AuditRecordDto
            {
                ActorId = actorId,
                ActorName = request.ActorName,
                Action = request.Action,
                TargetType = request.TargetType,
                TargetId = request.TargetId,
                TargetName = request.TargetName,
                Details = request.Details,
                Category = ParseCategory(request.Category)
            },
            context.CancellationToken);

        return new RecordAuditResponse
        {
            EntryId = entry.Id.ToString(),
            Recorded = true
        };
    }

    private static AuditCategory ParseCategory(string? category)
        => Enum.TryParse<AuditCategory>(category, ignoreCase: true, out var parsed)
            ? parsed
            : AuditCategory.User;

    /// <summary>
    /// Falls back to the email when no display name is set, so consumers always
    /// get something to render rather than an empty string.
    /// </summary>
    private static IdeaFusion.Grpc.Users.UserDto ToUserDto(IdentityBLL.DTOs.UserDto user) => new()
    {
        UserId = user.Id.ToString(),
        UserName = string.IsNullOrWhiteSpace(user.UserName) ? user.Email : user.UserName,
        Email = user.Email,
        AvatarUrl = user.AvatarUrl ?? string.Empty,
        CreatedAt = user.CreatedAt.ToString("O")
    };
}
