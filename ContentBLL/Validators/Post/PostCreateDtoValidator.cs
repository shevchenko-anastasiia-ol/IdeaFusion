using ContentBLL.DTO.Post;
using FluentValidation;

namespace ContentBLL.Validators;

public class PostCreateDtoValidator: AbstractValidator<PostCreateDto>
{
    public PostCreateDtoValidator()
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
 
        RuleFor(x => x)
            .Must(x => x.PostAuthorId.HasValue ^ x.CollaborationSnapshotId.HasValue)
            .WithMessage("Пост має належати або автору, або колаборації — але не обом і не нікому.");
 
        RuleFor(x => x.CreatedBy)
            .NotEmpty().WithMessage("CreatedBy є обов'язковим.")
            .MaximumLength(100).WithMessage("CreatedBy не може перевищувати 100 символів.");
    }
}
 
