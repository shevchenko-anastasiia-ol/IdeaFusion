using Collaboration.Application.Interfaces.Queries;
using Collaboration.Domain.Common.Helpers;
using Collaboration.Domain.Entities.Parameters;
using Collaboration.Domain.Exceptions;
using Collaboration.Domain.Interfaces;

namespace Collaboration.Application.Queries.CollaborationRequest;

public class GetCollaborationRequestByIdQuery : IQuery<Domain.Entities.CollaborationRequest?>
{
    public string RequestId { get; init; } = default!;
}
 
public class GetCollaborationRequestsPagedQuery : IQuery<PagedList<Domain.Entities.CollaborationRequest>>
{
    public CollaborationRequestParameters Parameters { get; init; } = default!;
}
 
public class GetCollaborationRequestsByTeamQuery : IQuery<IEnumerable<Domain.Entities.CollaborationRequest>>
{
    public string TeamId { get; init; } = default!;
}
 
public class GetCollaborationRequestsByUserQuery : IQuery<IEnumerable<Domain.Entities.CollaborationRequest>>
{
    public string UserId { get; init; } = default!;
}
 
