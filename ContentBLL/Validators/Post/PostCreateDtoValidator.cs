using ContentBLL.DTO.Post;
using FluentValidation;

namespace ContentBLL.Validators;

public class PostCreateDtoValidator : AbstractValidator<PostCreateDto>
{
    public PostCreateDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Заголовок є обов'язковим.")
            .MaximumLength(200).WithMessage("Заголовок не може перевищувати 200 символів.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Опис не може перевищувати 2000 символів.")
            .When(x => x.Description != null);

        RuleFor(x => x.ExternalLink)
            .MaximumLength(500).WithMessage("Посилання не може перевищувати 500 символів.")
            .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _))
            .WithMessage("ExternalLink має бути валідним URL.")
            .When(x => !string.IsNullOrEmpty(x.ExternalLink));

        // The author side of the constraint is satisfied by the token, so a personal
        // post is the one that arrives without a collaboration snapshot.
        RuleFor(x => x.CollaborationSnapshotId)
            .GreaterThan(0).WithMessage("CollaborationSnapshotId має бути більше 0.")
            .When(x => x.CollaborationSnapshotId.HasValue);

        // Додатково можна перевіряти TagIds
        RuleFor(x => x.TagIds)
            .NotNull().WithMessage("TagIds не може бути null.")
            .Must(tags => tags.All(t => t > 0))
            .WithMessage("TagIds мають містити тільки додатні числа.");
    }
}
