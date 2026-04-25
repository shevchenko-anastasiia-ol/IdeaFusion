using IdeaFusion.Grpc.Teams;
using Grpc.Core;
using GrpcClients.Interfaces;
using Microsoft.Extensions.Logging;

namespace IdeaFusion.GrpcClients.Clients
{
    public class TeamGrpcClient : ITeamGrpcClient
    {
        private readonly TeamGrpcService.TeamGrpcServiceClient _client;
        private readonly ILogger<TeamGrpcClient> _logger;

        public TeamGrpcClient(
            TeamGrpcService.TeamGrpcServiceClient client,
            ILogger<TeamGrpcClient> logger)
        {
            _client = client;
            _logger = logger;
        }

        public async Task<TeamDto?> GetTeamByIdAsync(Guid teamId, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _client.GetTeamByIdAsync(new GetTeamByIdRequest
                {
                    TeamId = teamId.ToString()
                }, cancellationToken: cancellationToken);

                return response.Team;
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
            {
                _logger.LogWarning(ex, "Team with ID {TeamId} not found.", teamId);
                return null;
            }
        }

        public async Task<IReadOnlyList<TeamDto>> GetTeamsByIdsAsync(
            IEnumerable<Guid> teamIds,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var request = new GetTeamsByIdsRequest();
                request.TeamIds.AddRange(teamIds.Select(id => id.ToString()));

                var response = await _client.GetTeamsByIdsAsync(request, cancellationToken: cancellationToken);

                return response.Teams;
            }
            catch (RpcException ex)
            {
                _logger.LogError(ex, "Failed to get teams by IDs.");
                return [];
            }
        }

        public async Task<PagedTeamsResponse?> GetTeamsPagedAsync(
            int pageNumber,
            int pageSize,
            string? orderBy = null,
            string? searchText = null,
            string? category = null,
            string? tag = null,
            string? status = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _client.GetTeamsPagedAsync(new GetTeamsPagedRequest
                {
                    PageNumber = pageNumber,
                    PageSize   = pageSize,
                    OrderBy    = orderBy    ?? string.Empty,
                    SearchText = searchText ?? string.Empty,
                    Category   = category   ?? string.Empty,
                    Tag        = tag        ?? string.Empty,
                    Status     = status     ?? string.Empty
                }, cancellationToken: cancellationToken);

                return response;
            }
            catch (RpcException ex)
            {
                _logger.LogError(ex, "Failed to get paged teams (page {PageNumber}, size {PageSize}).", pageNumber, pageSize);
                return null;
            }
        }

        public async Task<CheckUserIsMemberResponse?> CheckUserIsMemberAsync(
            Guid teamId,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _client.CheckUserIsMemberAsync(new CheckUserIsMemberRequest
                {
                    TeamId = teamId.ToString(),
                    UserId = userId.ToString()
                }, cancellationToken: cancellationToken);

                return response;
            }
            catch (RpcException ex)
            {
                _logger.LogError(ex, "Failed to check membership for user {UserId} in team {TeamId}.", userId, teamId);
                return null;
            }
        }
    }
}