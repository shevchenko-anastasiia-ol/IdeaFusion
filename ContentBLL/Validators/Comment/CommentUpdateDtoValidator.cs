using ContentBLL.DTO.Comment;
using FluentValidation;

namespace ContentBLL.Validators.Comment;

public class CommentUpdateDtoValidator : AbstractValidator<CommentUpdateDto>
{
    public CommentUpdateDtoValidator()
    {
        RuleFor(x => x.Body)
            .NotEmpty().WithMessage("Текст коментаря є обов'язковим.")
            .MaximumLength(2000).WithMessage("Коментар не може перевищувати 2000 символів.");
 
        RuleFor(x => x.UpdatedBy)
            .NotEmpty().WithMessage("UpdatedBy є обов'язковим.")
            .MaximumLength(100).WithMessage("UpdatedBy не може перевищувати 100 символів.");
    }
}