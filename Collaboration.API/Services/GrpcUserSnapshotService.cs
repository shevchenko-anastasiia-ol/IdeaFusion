using Collaboration.Application.Interfaces.Services;
using GrpcClients.Interfaces;

namespace Collaboration.API.Services;

public class GrpcUserSnapshotService : IUserSnapshotService
{
    private readonly IUserGrpcClient _grpcClient;

    public GrpcUserSnapshotService(IUserGrpcClient grpcClient)
    {
        _grpcClient = grpcClient;
    }

    public async Task<(string Username, string? AvatarUrl)> GetUserSnapshotDataAsync(
        string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (Guid.TryParse(userId, out var guid))
            {
                var dto = await _grpcClient.GetUserByIdAsync(guid, cancellationToken);
                if (dto != null)
                    return (dto.UserName, string.IsNullOrEmpty(dto.AvatarUrl) ? null : dto.AvatarUrl);
            }
        }
        catch
        {
            // gRPC unavailable — fall through to userId fallback
        }
        return (userId, null);
    }
}