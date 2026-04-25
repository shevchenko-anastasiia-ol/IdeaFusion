using AutoMapper;
using Collaboration.Domain.Interfaces;
using Grpc.Core;
using IdeaFusion.Grpc.CollaborationRequests;
using IdeaFusion.Grpc.Teams;
using MediatR;
using Microsoft.Extensions.Logging;
using Collaboration.Application.Queries.CollaborationRequest;
using Collaboration.Application.Queries.Team;

namespace CollaborationGrpcService.Services;

public class CollaborationRequestGrpcServiceImpl : CollaborationRequestGrpcService.CollaborationRequestGrpcServiceBase
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;
    private readonly ILogger<CollaborationRequestGrpcServiceImpl> _logger;
 
    public CollaborationRequestGrpcServiceImpl(
        IMediator mediator,
        IMapper mapper,
        ILogger<CollaborationRequestGrpcServiceImpl> logger)
    {
        _mediator = mediator;
        _mapper   = mapper;
        _logger   = logger;
    }
 
    // ── GetRequestById ────────────────────────────────────────────────────────
 
    public override async Task<CollaborationRequestResponse> GetRequestById(
        GetRequestByIdRequest request,
        ServerCallContext context)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.RequestId))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "RequestId is required."));
 
            var query = new GetCollaborationRequestByIdQuery { RequestId = request.RequestId };
            var result = await _mediator.Send(query, context.CancellationToken);
 
            if (result is null)
                throw new RpcException(new Status(StatusCode.NotFound,
                    $"Collaboration request '{request.RequestId}' not found."));
 
            _logger.LogInformation("GetRequestById: returned request {RequestId}", request.RequestId);
 
            return new CollaborationRequestResponse
            {
                Request = _mapper.Map<CollaborationRequestDto>(result)
            };
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetRequestById gRPC call");
            throw new RpcException(new Status(StatusCode.Internal, ex.Message));
        }
    }
 
    // ── GetRequestsByTeam ─────────────────────────────────────────────────────
 
    public override async Task<CollaborationRequestsResponse> GetRequestsByTeam(
        GetRequestsByTeamRequest request,
        ServerCallContext context)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.TeamId))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "TeamId is required."));
 
            var query = new GetCollaborationRequestsByTeamQuery { TeamId = request.TeamId };
            var results = await _mediator.Send(query, context.CancellationToken);
 
            // Фільтр по статусу якщо передано
            if (!string.IsNullOrWhiteSpace(request.Status))
                results = results.Where(r => r.Status.ToString() == request.Status);
 
            var items = results.ToList();
 
            _logger.LogInformation(
                "GetRequestsByTeam: returned {Count} requests for team {TeamId} (status: {Status})",
                items.Count, request.TeamId, request.Status ?? "all");
 
            var response = new CollaborationRequestsResponse();
            response.Items.AddRange(items.Select(r => _mapper.Map<CollaborationRequestDto>(r)));
            return response;
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetRequestsByTeam gRPC call");
            throw new RpcException(new Status(StatusCode.Internal, ex.Message));
        }
    }
 
    // ── GetRequestsByUser ─────────────────────────────────────────────────────
 
    public override async Task<CollaborationRequestsResponse> GetRequestsByUser(
        GetRequestsByUserRequest request,
        ServerCallContext context)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.UserId))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "UserId is required."));
 
            var query = new GetCollaborationRequestsByUserQuery { UserId = request.UserId };
            var results = await _mediator.Send(query, context.CancellationToken);
 
            // Фільтр по статусу якщо передано
            if (!string.IsNullOrWhiteSpace(request.Status))
                results = results.Where(r => r.Status.ToString() == request.Status);
 
            var items = results.ToList();
 
            _logger.LogInformation(
                "GetRequestsByUser: returned {Count} requests for user {UserId} (status: {Status})",
                items.Count, request.UserId, request.Status ?? "all");
 
            var response = new CollaborationRequestsResponse();
            response.Items.AddRange(items.Select(r => _mapper.Map<CollaborationRequestDto>(r)));
            return response;
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetRequestsByUser gRPC call");
            throw new RpcException(new Status(StatusCode.Internal, ex.Message));
        }
    }
}