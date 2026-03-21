using ContentBLL.DTO.Like;
using FluentValidation;

namespace ContentBLL.Validators.Like;

public class LikeCreateDtoValidator : AbstractValidator<LikeCreateDto>
{
    public LikeCreateDtoValidator()
    {
        RuleFor(x => x.PostId)
            .GreaterThan(0).WithMessage("PostId має бути більше 0.");
 
        RuleFor(x => x.UserId)
            .GreaterThan(0).WithMessage("UserId має бути більше 0.");
    }
}