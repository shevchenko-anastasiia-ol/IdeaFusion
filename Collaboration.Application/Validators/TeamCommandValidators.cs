using Collaboration.Application.Commands.Team;
using Collaboration.Domain.Entities;
using FluentValidation;

namespace Collaboration.Application.Validators;

public class CreateTeamCommandValidator : AbstractValidator<CreateTeamCommand>
{
    public CreateTeamCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Team name is required.")
            .MaximumLength(100).WithMessage("Team name cannot exceed 100 characters.");
 
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Team description is required.")
            .MaximumLength(1000).WithMessage("Team description cannot exceed 1000 characters.");
 
        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("Team category is required.");
 
        RuleFor(x => x.Tags)
            .Must(tags => tags.Count <= 10).WithMessage("Team cannot have more than 10 tags.");
    }
}
 
public class UpdateTeamCommandValidator : AbstractValidator<UpdateTeamCommand>
{
    public UpdateTeamCommandValidator()
    {
        RuleFor(x => x.TeamId)
            .NotEmpty().WithMessage("TeamId is required.");
 
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Team name is required.")
            .MaximumLength(100).WithMessage("Team name cannot exceed 100 characters.");
 
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Team description is required.")
            .MaximumLength(1000).WithMessage("Team description cannot exceed 1000 characters.");
 
        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("Team category is required.");
 
        RuleFor(x => x.Tags)
            .Must(tags => tags.Count <= 10).WithMessage("Team cannot have more than 10 tags.");
    }
}
 
public class DeleteTeamCommandValidator : AbstractValidator<DeleteTeamCommand>
{
    public DeleteTeamCommandValidator()
    {
        RuleFor(x => x.TeamId)
            .NotEmpty().WithMessage("TeamId is required.");
    }
}
 
public class SetTeamStatusCommandValidator : AbstractValidator<SetTeamStatusCommand>
{
    public SetTeamStatusCommandValidator()
    {
        RuleFor(x => x.TeamId)
            .NotEmpty().WithMessage("TeamId is required.");
 
        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid team status.");
    }
}
 
public class AddTeamMemberCommandValidator : AbstractValidator<AddTeamMemberCommand>
{
    public AddTeamMemberCommandValidator()
    {
        RuleFor(x => x.TeamId)
            .NotEmpty().WithMessage("TeamId is required.");

        // The member being added, not the caller: only the target's subject is required.
        RuleFor(x => x.MemberUserId)
            .NotEmpty().WithMessage("MemberUserId is required.");

        // The name and avatar are resolved from Identity, so they are optional here.
        RuleFor(x => x.MemberUsername)
            .MaximumLength(100).WithMessage("MemberUsername cannot exceed 100 characters.")
            .When(x => x.MemberUsername is not null);
 
        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Role is required.")
            .Must(TeamRole.IsKnown).WithMessage("Role is not a known team role.");
    }
}
 
public class RemoveTeamMemberCommandValidator : AbstractValidator<RemoveTeamMemberCommand>
{
    public RemoveTeamMemberCommandValidator()
    {
        RuleFor(x => x.TeamId)
            .NotEmpty().WithMessage("TeamId is required.");
 
        RuleFor(x => x.MemberUserId)
            .NotEmpty().WithMessage("MemberUserId is required.");
    }
}
 
public class AddRequiredRoleCommandValidator : AbstractValidator<AddRequiredRoleCommand>
{
    public AddRequiredRoleCommandValidator()
    {
        RuleFor(x => x.TeamId)
            .NotEmpty().WithMessage("TeamId is required.");
  
        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Role is required.")
            .MaximumLength(100).WithMessage("Role cannot exceed 100 characters.");
  
        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.")
            .When(x => x.Description is not null);
    }
}

public class UpdateTeamMemberPermissionsCommandValidator : AbstractValidator<UpdateTeamMemberPermissionsCommand>
{
    public UpdateTeamMemberPermissionsCommandValidator()
    {
        RuleFor(x => x.TeamId)
            .NotEmpty().WithMessage("TeamId is required.");

        RuleFor(x => x.TargetMemberId)
            .NotEmpty().WithMessage("TargetMemberId is required.");

        RuleFor(x => x.Permissions)
            .Must(permissions => permissions.All(permission => TeamRole.AllPermissions.Contains(permission)))
            .WithMessage("One or more permissions are not recognized.");
    }
}

public class SetTeamMemberRoleCommandValidator : AbstractValidator<SetTeamMemberRoleCommand>
{
    public SetTeamMemberRoleCommandValidator()
    {
        RuleFor(x => x.TeamId)
            .NotEmpty().WithMessage("TeamId is required.");

        RuleFor(x => x.TargetMemberId)
            .NotEmpty().WithMessage("TargetMemberId is required.");

        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Role is required.")
            .Must(TeamRole.IsKnown).WithMessage("Role is not a known team role.")
            .Must(role => role != TeamRole.Owner)
            .WithMessage("A team's owner cannot be reassigned.");
    }
}