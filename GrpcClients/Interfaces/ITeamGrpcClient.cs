using IdeaFusion.Grpc.Teams;

namespace GrpcClients.Interfaces;

public interface ITeamGrpcClient
{
    Task<TeamDto?> GetTeamByIdAsync(Guid teamId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TeamDto>> GetTeamsByIdsAsync(IEnumerable<Guid> teamIds, CancellationToken cancellationToken = default);
    Task<PagedTeamsResponse?> GetTeamsPagedAsync(int pageNumber, int pageSize, string? orderBy = null, string? searchText = null, string? category = null, string? tag = null, string? status = null, CancellationToken cancellationToken = default);
    Task<CheckUserIsMemberResponse?> CheckUserIsMemberAsync(Guid teamId, Guid userId, CancellationToken cancellationToken = default);
}