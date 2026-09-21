using Collaboration.Application.Commands.Team;
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
 
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");
 
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required.")
            .MaximumLength(100).WithMessage("Username cannot exceed 100 characters.");
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
 
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");
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
 
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");
    }
}
 
public class AddTeamMemberCommandValidator : AbstractValidator<AddTeamMemberCommand>
{
    public AddTeamMemberCommandValidator()
    {
        RuleFor(x => x.TeamId)
            .NotEmpty().WithMessage("TeamId is required.");
 
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");
 
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required.")
            .MaximumLength(100).WithMessage("Username cannot exceed 100 characters.");
 
        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Role is required.")
            .MaximumLength(100).WithMessage("Role cannot exceed 100 characters.");
 
        RuleFor(x => x.RequestedByUserId)
            .NotEmpty().WithMessage("RequestedByUserId is required.");
    }
}
 
public class RemoveTeamMemberCommandValidator : AbstractValidator<RemoveTeamMemberCommand>
{
    public RemoveTeamMemberCommandValidator()
    {
        RuleFor(x => x.TeamId)
            .NotEmpty().WithMessage("TeamId is required.");
 
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");
 
        RuleFor(x => x.RequestedByUserId)
            .NotEmpty().WithMessage("RequestedByUserId is required.");
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
  
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");
    }
}

public class UpdateTeamMemberPermissionsCommandValidator : AbstractValidator<UpdateTeamMemberPermissionsCommand>
{
    private static readonly string[] Known = new[]
    {
        Collaboration.Domain.Entities.TeamPermission.PublishPosts,
        Collaboration.Domain.Entities.TeamPermission.EditPosts,
        Collaboration.Domain.Entities.TeamPermission.InviteMembers,
        Collaboration.Domain.Entities.TeamPermission.RemoveMembers,
        Collaboration.Domain.Entities.TeamPermission.EditTeam
    };

    public UpdateTeamMemberPermissionsCommandValidator()
    {
        RuleFor(x => x.TeamId)
            .NotEmpty().WithMessage("TeamId is required.");

        RuleFor(x => x.TargetMemberId)
            .NotEmpty().WithMessage("TargetMemberId is required.");

        RuleFor(x => x.Permissions)
            .Must(permissions => permissions.All(permission => Known.Contains(permission)))
            .WithMessage("One or more permissions are not recognized.");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");
    }
}