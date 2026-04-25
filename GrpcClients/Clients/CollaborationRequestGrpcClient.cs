using IdeaFusion.Grpc.CollaborationRequests;
using Grpc.Core;
using GrpcClients.Interfaces;
using Microsoft.Extensions.Logging;

namespace IdeaFusion.GrpcClients.Clients
{
    public class CollaborationRequestGrpcClient : ICollaborationRequestGrpcClient
    {
        private readonly CollaborationRequestGrpcService.CollaborationRequestGrpcServiceClient _client;
        private readonly ILogger<CollaborationRequestGrpcClient> _logger;

        public CollaborationRequestGrpcClient(
            CollaborationRequestGrpcService.CollaborationRequestGrpcServiceClient client,
            ILogger<CollaborationRequestGrpcClient> logger)
        {
            _client = client;
            _logger = logger;
        }

        public async Task<CollaborationRequestDto?> GetRequestByIdAsync(Guid requestId, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _client.GetRequestByIdAsync(new GetRequestByIdRequest
                {
                    RequestId = requestId.ToString()
                }, cancellationToken: cancellationToken);

                return response.Request;
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
            {
                _logger.LogWarning(ex, "Collaboration request with ID {RequestId} not found.", requestId);
                return null;
            }
        }

        public async Task<IReadOnlyList<CollaborationRequestDto>> GetRequestsByTeamAsync(
            Guid teamId,
            string? status = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _client.GetRequestsByTeamAsync(new GetRequestsByTeamRequest
                {
                    TeamId = teamId.ToString(),
                    Status = status ?? string.Empty
                }, cancellationToken: cancellationToken);

                return response.Items;
            }
            catch (RpcException ex)
            {
                _logger.LogError(ex, "Failed to get collaboration requests for team {TeamId}.", teamId);
                return [];
            }
        }

        public async Task<IReadOnlyList<CollaborationRequestDto>> GetRequestsByUserAsync(
            Guid userId,
            string? status = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _client.GetRequestsByUserAsync(new GetRequestsByUserRequest
                {
                    UserId = userId.ToString(),
                    Status = status ?? string.Empty
                }, cancellationToken: cancellationToken);

                return response.Items;
            }
            catch (RpcException ex)
            {
                _logger.LogError(ex, "Failed to get collaboration requests for user {UserId}.", userId);
                return [];
            }
        }
    }
}