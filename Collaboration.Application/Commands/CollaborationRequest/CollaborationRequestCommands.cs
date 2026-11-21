using Collaboration.Application.Interfaces.Commands;

namespace Collaboration.Application.Commands.CollaborationRequest;

// The sender of a collaboration request is the caller, and so is whoever accepts,
// rejects or cancels it. None of these commands say who is acting.

public class CreateCollaborationRequestCommand : ICommand<Domain.Entities.CollaborationRequest>
{
    public string TeamId { get; init; } = default!;
    public string Role { get; init; } = default!;
    public string? Message { get; init; }
    public string? ToUserId { get; init; }
}
 
public class AcceptCollaborationRequestCommand : ICommand<Domain.Entities.CollaborationRequest>
{
    public string RequestId { get; init; } = default!;
}
 
public class RejectCollaborationRequestCommand : ICommand<Domain.Entities.CollaborationRequest>
{
    public string RequestId { get; init; } = default!;
}
 
public class CancelCollaborationRequestCommand : ICommand
{
    public string RequestId { get; init; } = default!;
}
