using Collaboration.Application.Commands.CollaborationRequest;
using Collaboration.Domain.Entities;
using FluentValidation;

namespace Collaboration.Application.Validators;

public class CreateCollaborationRequestCommandValidator : AbstractValidator<CreateCollaborationRequestCommand>
{
    public CreateCollaborationRequestCommandValidator()
    {
        RuleFor(x => x.TeamId)
            .NotEmpty().WithMessage("TeamId is required.");
 
        // The position the person asks to join as; access is always a plain Member.
        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Role is required.")
            .MaximumLength(100).WithMessage("Role cannot exceed 100 characters.");
 
        RuleFor(x => x.Message)
            .MaximumLength(1000).WithMessage("Message cannot exceed 1000 characters.")
            .When(x => x.Message is not null);

        // A request addressed to somebody is still the caller's own; comparing it with
        // the token's subject is done in the handler, where the token is available.
        RuleFor(x => x.ToUserId)
            .MaximumLength(100).WithMessage("ToUserId cannot exceed 100 characters.")
            .When(x => x.ToUserId is not null);
    }
}
 
public class AcceptCollaborationRequestCommandValidator : AbstractValidator<AcceptCollaborationRequestCommand>
{
    public AcceptCollaborationRequestCommandValidator()
    {
        RuleFor(x => x.RequestId)
            .NotEmpty().WithMessage("RequestId is required.");
    }
}
 
public class RejectCollaborationRequestCommandValidator : AbstractValidator<RejectCollaborationRequestCommand>
{
    public RejectCollaborationRequestCommandValidator()
    {
        RuleFor(x => x.RequestId)
            .NotEmpty().WithMessage("RequestId is required.");
    }
}
 
public class CancelCollaborationRequestCommandValidator : AbstractValidator<CancelCollaborationRequestCommand>
{
    public CancelCollaborationRequestCommandValidator()
    {
        RuleFor(x => x.RequestId)
            .NotEmpty().WithMessage("RequestId is required.");
    }
}