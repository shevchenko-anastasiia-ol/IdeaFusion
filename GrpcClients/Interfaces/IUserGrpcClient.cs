using GrpcClients.Models;
using IdeaFusion.Grpc.Users;

namespace GrpcClients.Interfaces;

public interface IUserGrpcClient
{
    Task<UserDto?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserDto>> GetUsersByIdsAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default);
    Task<bool> CheckUserExistsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Appends an entry to the Identity service's audit trail on behalf of the
    /// calling service. Returns false when the entry could not be written.
    /// </summary>
    Task<bool> RecordAuditAsync(AuditRecordMessage record, CancellationToken cancellationToken = default);
}
