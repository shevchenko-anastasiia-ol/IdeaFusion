using Collaboration.Application.Commands.TeamPost;
using FluentValidation;

namespace Collaboration.Application.Validators;

public class CreateTeamPostCommandValidator : AbstractValidator<CreateTeamPostCommand>
{
    public CreateTeamPostCommandValidator()
    {
        RuleFor(x => x.PostId)
            .NotEmpty().WithMessage("PostId is required.");
 
        RuleFor(x => x.TeamId)
            .NotEmpty().WithMessage("TeamId is required.");
 
        RuleFor(x => x.AuthorUserId)
            .NotEmpty().WithMessage("AuthorUserId is required.");
 
        RuleFor(x => x.AuthorUsername)
            .NotEmpty().WithMessage("AuthorUsername is required.")
            .MaximumLength(100).WithMessage("AuthorUsername cannot exceed 100 characters.");
 
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters.");
    }
}
 
public class UpdateTeamPostTitleCommandValidator : AbstractValidator<UpdateTeamPostTitleCommand>
{
    public UpdateTeamPostTitleCommandValidator()
    {
        RuleFor(x => x.PostId)
            .NotEmpty().WithMessage("PostId is required.");
 
        RuleFor(x => x.TeamId)
            .NotEmpty().WithMessage("TeamId is required.");
 
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters.");
 
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");
    }
}
 
public class DeleteTeamPostCommandValidator : AbstractValidator<DeleteTeamPostCommand>
{
    public DeleteTeamPostCommandValidator()
    {
        RuleFor(x => x.PostId)
            .NotEmpty().WithMessage("PostId is required.");
 
        RuleFor(x => x.TeamId)
            .NotEmpty().WithMessage("TeamId is required.");
 
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");
    }
}