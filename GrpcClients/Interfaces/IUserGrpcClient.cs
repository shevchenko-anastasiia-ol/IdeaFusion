using GrpcClients.Models;
using IdeaFusion.Grpc.Users;

namespace GrpcClients.Interfaces;

public interface IUserGrpcClient
{
    Task<UserDto?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserDto>> GetUsersByIdsAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default);
    Task<ValidatedTokenResult?> ValidateTokenAsync(string accessToken, CancellationToken cancellationToken = default);
    Task<bool> CheckUserExistsAsync(Guid userId, CancellationToken cancellationToken = default);
}