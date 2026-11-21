using Collaboration.Application.Interfaces.Queries;
using Collaboration.Domain.Common.Helpers;
using Collaboration.Domain.Entities.Parameters;

namespace Collaboration.Application.Queries.GroupInvitation;

public class GetGroupInvitationByIdQuery : IQuery<Domain.Entities.GroupInvitation?>
{
    public string InvitationId { get; init; } = default!;
}
 
public class GetGroupInvitationsPagedQuery : IQuery<PagedList<Domain.Entities.GroupInvitation>>
{
    public GroupInvitationParameters Parameters { get; init; } = default!;
}
 
public class GetGroupInvitationsByTeamQuery : IQuery<IEnumerable<Domain.Entities.GroupInvitation>>
{
    public string TeamId { get; init; } = default!;
}
 
/// <summary>The invitations addressed to the caller. No user id: it is the token's.</summary>
public class GetMyGroupInvitationsQuery : IQuery<IEnumerable<Domain.Entities.GroupInvitation>>
{
}
 
public class GetExpiredGroupInvitationsQuery : IQuery<IEnumerable<Domain.Entities.GroupInvitation>> { }