using ContentBLL.DTO.Tag;
using FluentValidation;

namespace ContentBLL.Validators.Tag;

public class TagCreateDtoValidator : AbstractValidator<TagCreateDto>
{
    public TagCreateDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Назва тегу є обов'язковою.")
            .MaximumLength(50).WithMessage("Тег не може перевищувати 50 символів.")
            .Matches(@"^[a-z0-9\-]+$").WithMessage("Тег може містити лише малі літери, цифри та дефіс.");
    }
}