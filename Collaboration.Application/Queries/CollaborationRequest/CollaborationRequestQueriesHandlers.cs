using Collaboration.Application.Interfaces.Queries;
using Collaboration.Domain.Common.Helpers;
using Collaboration.Domain.Exceptions;
using Collaboration.Domain.Interfaces;

namespace Collaboration.Application.Queries.CollaborationRequest;

public class GetCollaborationRequestByIdQueryHandler
    : IQueryHandler<GetCollaborationRequestByIdQuery, Domain.Entities.CollaborationRequest?>
{
    private readonly ICollaborationRequestRepository _repository;
 
    public GetCollaborationRequestByIdQueryHandler(ICollaborationRequestRepository repository)
    {
        _repository = repository;
    }
 
    public async Task<Domain.Entities.CollaborationRequest?> Handle(
        GetCollaborationRequestByIdQuery request, CancellationToken cancellationToken)
    {
        var result = await _repository.GetByIdAsync(request.RequestId, cancellationToken);
 
        if (result is null)
            throw new DomainException($"Collaboration request '{request.RequestId}' not found.");
 
        return result;
    }
}
 
public class GetCollaborationRequestsPagedQueryHandler
    : IQueryHandler<GetCollaborationRequestsPagedQuery, PagedList<Domain.Entities.CollaborationRequest>>
{
    private readonly ICollaborationRequestRepository _repository;
 
    public GetCollaborationRequestsPagedQueryHandler(ICollaborationRequestRepository repository)
    {
        _repository = repository;
    }
 
    public async Task<PagedList<Domain.Entities.CollaborationRequest>> Handle(
        GetCollaborationRequestsPagedQuery request, CancellationToken cancellationToken)
    {
        var p = request.Parameters;
        return await _repository.GetPagedFilteredAsync(
            p.TeamId, p.FromUserId, p.ToUserId, p.Role, p.Status,
            p.PageNumber, p.PageSize,
            ascending: false,
            cancellationToken);
    }
}
 
public class GetCollaborationRequestsByTeamQueryHandler
    : IQueryHandler<GetCollaborationRequestsByTeamQuery, IEnumerable<Domain.Entities.CollaborationRequest>>
{
    private readonly ICollaborationRequestRepository _repository;
 
    public GetCollaborationRequestsByTeamQueryHandler(ICollaborationRequestRepository repository)
    {
        _repository = repository;
    }
 
    public async Task<IEnumerable<Domain.Entities.CollaborationRequest>> Handle(
        GetCollaborationRequestsByTeamQuery request, CancellationToken cancellationToken)
    {
        return await _repository.GetByTeamIdAsync(request.TeamId, cancellationToken);
    }
}
 
public class GetCollaborationRequestsByUserQueryHandler
    : IQueryHandler<GetCollaborationRequestsByUserQuery, IEnumerable<Domain.Entities.CollaborationRequest>>
{
    private readonly ICollaborationRequestRepository _repository;
 
    public GetCollaborationRequestsByUserQueryHandler(ICollaborationRequestRepository repository)
    {
        _repository = repository;
    }
 
    public async Task<IEnumerable<Domain.Entities.CollaborationRequest>> Handle(
        GetCollaborationRequestsByUserQuery request, CancellationToken cancellationToken)
    {
        return await _repository.GetByFromUserIdAsync(request.UserId, cancellationToken);
    }
}