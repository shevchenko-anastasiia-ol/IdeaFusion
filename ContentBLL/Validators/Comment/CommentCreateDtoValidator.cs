using ContentBLL.DTO.Comment;
using FluentValidation;

namespace ContentBLL.Validators.Comment;

public class CommentCreateDtoValidator : AbstractValidator<CommentCreateDto>
{
    public CommentCreateDtoValidator()
    {
        RuleFor(x => x.PostId)
            .GreaterThan(0).WithMessage("PostId має бути більше 0.");
 
        RuleFor(x => x.PostAuthorId)
            .GreaterThan(0).WithMessage("PostAuthorId має бути більше 0.");
 
        RuleFor(x => x.ParentCommentId)
            .GreaterThan(0).WithMessage("ParentCommentId має бути більше 0.")
            .When(x => x.ParentCommentId.HasValue);
 
        RuleFor(x => x.Body)
            .NotEmpty().WithMessage("Текст коментаря є обов'язковим.")
            .MaximumLength(2000).WithMessage("Коментар не може перевищувати 2000 символів.");
 
        RuleFor(x => x.CreatedBy)
            .NotEmpty().WithMessage("CreatedBy є обов'язковим.")
            .MaximumLength(100).WithMessage("CreatedBy не може перевищувати 100 символів.");
    }
}