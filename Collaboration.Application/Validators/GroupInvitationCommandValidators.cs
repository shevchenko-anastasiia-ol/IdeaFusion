using Collaboration.Application.Commands.GroupInvitation;
using FluentValidation;

namespace Collaboration.Application.Validators;

public class CreateGroupInvitationCommandValidator : AbstractValidator<CreateGroupInvitationCommand>
{
    public CreateGroupInvitationCommandValidator()
    {
        RuleFor(x => x.TeamId)
            .NotEmpty().WithMessage("TeamId is required.");
 
        RuleFor(x => x.InvitedUserId)
            .NotEmpty().WithMessage("InvitedUserId is required.");
 
        RuleFor(x => x.InvitedByUserId)
            .NotEmpty().WithMessage("InvitedByUserId is required.");
 
        RuleFor(x => x.InvitedUserId)
            .NotEqual(x => x.InvitedByUserId)
            .WithMessage("Cannot invite yourself.");
 
        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Role is required.")
            .MaximumLength(100).WithMessage("Role cannot exceed 100 characters.");
 
        RuleFor(x => x.Message)
            .MaximumLength(1000).WithMessage("Message cannot exceed 1000 characters.")
            .When(x => x.Message is not null);
 
        RuleFor(x => x.ExpirationDays)
            .InclusiveBetween(1, 30)
            .WithMessage("Expiration days must be between 1 and 30.");
    }
}
 
public class AcceptGroupInvitationCommandValidator : AbstractValidator<AcceptGroupInvitationCommand>
{
    public AcceptGroupInvitationCommandValidator()
    {
        RuleFor(x => x.InvitationId)
            .NotEmpty().WithMessage("InvitationId is required.");
 
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");
    }
}
 
public class DeclineGroupInvitationCommandValidator : AbstractValidator<DeclineGroupInvitationCommand>
{
    public DeclineGroupInvitationCommandValidator()
    {
        RuleFor(x => x.InvitationId)
            .NotEmpty().WithMessage("InvitationId is required.");
 
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");
    }
}
 
public class RevokeGroupInvitationCommandValidator : AbstractValidator<RevokeGroupInvitationCommand>
{
    public RevokeGroupInvitationCommandValidator()
    {
        RuleFor(x => x.InvitationId)
            .NotEmpty().WithMessage("InvitationId is required.");
 
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");
    }
}