using IdeaFusion.Grpc.Users;
using Grpc.Core;
using GrpcClients.Interfaces;
using GrpcClients.Models;
using Microsoft.Extensions.Logging;

namespace IdeaFusion.GrpcClients.Clients
{
    public class UserGrpcClient : IUserGrpcClient
    {
        private readonly UserGrpcService.UserGrpcServiceClient _client;
        private readonly ILogger<UserGrpcClient> _logger;

        public UserGrpcClient(
            UserGrpcService.UserGrpcServiceClient client,
            ILogger<UserGrpcClient> logger)
        {
            _client = client;
            _logger = logger;
        }

        public async Task<UserDto?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _client.GetUserByIdAsync(new GetUserByIdRequest
                {
                    UserId = userId.ToString()
                }, cancellationToken: cancellationToken);

                return response.User;
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
            {
                _logger.LogWarning(ex, "User with ID {UserId} not found.", userId);
                return null;
            }
        }

        public async Task<IReadOnlyList<UserDto>> GetUsersByIdsAsync(
            IEnumerable<Guid> userIds,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var request = new GetUsersByIdsRequest();
                request.UserIds.AddRange(userIds.Select(id => id.ToString()));

                var response = await _client.GetUsersByIdsAsync(request, cancellationToken: cancellationToken);

                return response.Users;
            }
            catch (RpcException ex)
            {
                _logger.LogError(ex, "Failed to get users by IDs.");
                return [];
            }
        }

        public async Task<ValidatedTokenResult?> ValidateTokenAsync(
            string accessToken,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _client.ValidateTokenAsync(new ValidateTokenRequest
                {
                    AccessToken = accessToken
                }, cancellationToken: cancellationToken);

                return new ValidatedTokenResult(
                    IsValid:  response.IsValid,
                    UserId:   response.UserId,
                    UserName: response.UserName,
                    Email:    response.Email);
            }
            catch (RpcException ex)
            {
                _logger.LogError(ex, "Failed to validate access token.");
                return null;
            }
        }

        public async Task<bool> CheckUserExistsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _client.CheckUserExistsAsync(new CheckUserExistsRequest
                {
                    UserId = userId.ToString()
                }, cancellationToken: cancellationToken);

                return response.Exists;
            }
            catch (RpcException ex)
            {
                _logger.LogError(ex, "Failed to check existence of user {UserId}.", userId);
                return false;
            }
        }
    }
}