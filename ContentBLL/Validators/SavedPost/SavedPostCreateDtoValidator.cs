using ContentBLL.DTO.SavedPost;
using FluentValidation;

namespace ContentBLL.Validators.SavedPost;

public class SavedPostCreateDtoValidator : AbstractValidator<SavedPostCreateDto>
{
    public SavedPostCreateDtoValidator()
    {
        RuleFor(x => x.PostId)
            .GreaterThan(0).WithMessage("PostId має бути більше 0.");
    }
}
