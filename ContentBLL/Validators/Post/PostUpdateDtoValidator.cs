using ContentBLL.DTO.Post;
using FluentValidation;

namespace ContentBLL.Validators;

public class PostUpdateDtoValidator : AbstractValidator<PostUpdateDto>
{
    public PostUpdateDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Заголовок є обов'язковим.")
            .MaximumLength(200).WithMessage("Заголовок не може перевищувати 200 символів.");
 
        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Опис не може перевищувати 2000 символів.")
            .When(x => x.Description != null);
 
        RuleFor(x => x.MediaUrl)
            .MaximumLength(500).WithMessage("URL медіа не може перевищувати 500 символів.")
            .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _)).WithMessage("MediaUrl має бути валідним URL.")
            .When(x => !string.IsNullOrEmpty(x.MediaUrl));
 
        RuleFor(x => x.ExternalLink)
            .MaximumLength(500).WithMessage("Посилання не може перевищувати 500 символів.")
            .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _)).WithMessage("ExternalLink має бути валідним URL.")
            .When(x => !string.IsNullOrEmpty(x.ExternalLink));
 
        RuleFor(x => x.UpdatedBy)
            .NotEmpty().WithMessage("UpdatedBy є обов'язковим.")
            .MaximumLength(100).WithMessage("UpdatedBy не може перевищувати 100 символів.");
    }
}