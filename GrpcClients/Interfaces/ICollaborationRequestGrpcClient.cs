using IdeaFusion.Grpc.CollaborationRequests;

namespace GrpcClients.Interfaces;

public interface ICollaborationRequestGrpcClient
{
    Task<CollaborationRequestDto?> GetRequestByIdAsync(Guid requestId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CollaborationRequestDto>> GetRequestsByTeamAsync(Guid teamId, string? status = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CollaborationRequestDto>> GetRequestsByUserAsync(Guid userId, string? status = null, CancellationToken cancellationToken = default);
}