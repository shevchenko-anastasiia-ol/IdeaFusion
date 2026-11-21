using Collaboration.Application.Commands.GroupInvitation;
using Collaboration.Domain.Entities;
using FluentValidation;

namespace Collaboration.Application.Validators;

public class CreateGroupInvitationCommandValidator : AbstractValidator<CreateGroupInvitationCommand>
{
    public CreateGroupInvitationCommandValidator()
    {
        RuleFor(x => x.TeamId)
            .NotEmpty().WithMessage("TeamId is required.");
 
        // The invitee is the subject of the invitation; the inviter is the token, so
        // there is nothing left here to compare the two against.
        RuleFor(x => x.InvitedUserId)
            .NotEmpty().WithMessage("InvitedUserId is required.");
 
        // The role here is the position the person is invited for ("Веб-розробник"),
        // not an access rank: everyone who accepts joins as a plain Member.
        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Role is required.")
            .MaximumLength(100).WithMessage("Role cannot exceed 100 characters.")
            .Must(role => role != TeamRole.Owner)
            .WithMessage("An invitation cannot grant ownership.");
 
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
    }
}
 
public class DeclineGroupInvitationCommandValidator : AbstractValidator<DeclineGroupInvitationCommand>
{
    public DeclineGroupInvitationCommandValidator()
    {
        RuleFor(x => x.InvitationId)
            .NotEmpty().WithMessage("InvitationId is required.");
    }
}
 
public class RevokeGroupInvitationCommandValidator : AbstractValidator<RevokeGroupInvitationCommand>
{
    public RevokeGroupInvitationCommandValidator()
    {
        RuleFor(x => x.InvitationId)
            .NotEmpty().WithMessage("InvitationId is required.");
    }
}