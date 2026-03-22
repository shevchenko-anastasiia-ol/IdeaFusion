using Collaboration.Application.Commands.CollaborationRequest;
using FluentValidation;

namespace Collaboration.Application.Validators;

public class CreateCollaborationRequestCommandValidator : AbstractValidator<CreateCollaborationRequestCommand>
{
    public CreateCollaborationRequestCommandValidator()
    {
        RuleFor(x => x.TeamId)
            .NotEmpty().WithMessage("TeamId is required.");
 
        RuleFor(x => x.FromUserId)
            .NotEmpty().WithMessage("FromUserId is required.");
 
        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Role is required.")
            .MaximumLength(100).WithMessage("Role cannot exceed 100 characters.");
 
        RuleFor(x => x.Message)
            .MaximumLength(1000).WithMessage("Message cannot exceed 1000 characters.")
            .When(x => x.Message is not null);
    }
}
 
public class AcceptCollaborationRequestCommandValidator : AbstractValidator<AcceptCollaborationRequestCommand>
{
    public AcceptCollaborationRequestCommandValidator()
    {
        RuleFor(x => x.RequestId)
            .NotEmpty().WithMessage("RequestId is required.");
 
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");
    }
}
 
public class RejectCollaborationRequestCommandValidator : AbstractValidator<RejectCollaborationRequestCommand>
{
    public RejectCollaborationRequestCommandValidator()
    {
        RuleFor(x => x.RequestId)
            .NotEmpty().WithMessage("RequestId is required.");
 
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");
    }
}
 
public class CancelCollaborationRequestCommandValidator : AbstractValidator<CancelCollaborationRequestCommand>
{
    public CancelCollaborationRequestCommandValidator()
    {
        RuleFor(x => x.RequestId)
            .NotEmpty().WithMessage("RequestId is required.");
 
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");
    }
}