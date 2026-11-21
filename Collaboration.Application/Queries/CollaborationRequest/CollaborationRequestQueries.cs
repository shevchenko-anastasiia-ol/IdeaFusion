using Collaboration.Application.Interfaces.Queries;
using Collaboration.Domain.Common.Helpers;
using Collaboration.Domain.Entities.Parameters;

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
 
/// <summary>The requests the caller has sent. No user id: it is the token's.</summary>
public class GetMyCollaborationRequestsQuery : IQuery<IEnumerable<Domain.Entities.CollaborationRequest>>
{
}

/// <summary>
/// The requests a named subject has sent.
/// <para>
/// Service-to-service only: the gRPC surface uses it so another backend can render a
/// user's request list, and there is no user token in that call. It must never be
/// routed from a controller, because on the HTTP side the subject is the token's.
/// </para>
/// </summary>
public class GetCollaborationRequestsByUserQuery : IQuery<IEnumerable<Domain.Entities.CollaborationRequest>>
{
    public string UserId { get; init; } = default!;
}
 
/// <summary>
/// A single request by id, without the end-user permission check.
/// <para>
/// Service-to-service only, for the same reason as
/// <see cref="GetCollaborationRequestsByUserQuery"/>. The HTTP route uses
/// <see cref="GetCollaborationRequestByIdQuery"/>, which is permission-checked.
/// </para>
/// </summary>
public class GetCollaborationRequestByIdInternalQuery : IQuery<Domain.Entities.CollaborationRequest?>
{
    public string RequestId { get; init; } = default!;
}
 
/// <summary>
/// A team's requests, without the end-user permission check.
/// <para>
/// Service-to-service only. The HTTP route uses
/// <see cref="GetCollaborationRequestsByTeamQuery"/>, which is permission-checked.
/// </para>
/// </summary>
public class GetCollaborationRequestsByTeamInternalQuery : IQuery<IEnumerable<Domain.Entities.CollaborationRequest>>
{
    public string TeamId { get; init; } = default!;
}