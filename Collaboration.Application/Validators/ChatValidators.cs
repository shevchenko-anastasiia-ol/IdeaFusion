using Collaboration.Application.Commands.ChatMessage;
using Collaboration.Application.Queries.ChatMessage;
using Collaboration.Domain.ValueOfObjects;
using FluentValidation;

namespace Collaboration.Application.Validators;

public class SendChatMessageCommandValidator : AbstractValidator<SendChatMessageCommand>
{
    public SendChatMessageCommandValidator()
    {
        RuleFor(x => x.ChatId)
            .NotEmpty().WithMessage("ChatId is required.");

        // The sender is the token's subject. Nothing in the body can name it.
        // Text or at least one attachment: a photo can go without a caption.
        RuleFor(x => x.Text)
            .NotEmpty().WithMessage("Text is required.")
            .When(x => x.Attachments is not { Count: > 0 });
        RuleFor(x => x.Text)
            .MaximumLength(2000).WithMessage("Text cannot exceed 2000 characters.");

        RuleFor(x => x.Attachments)
            .Must(a => a is null || a.Count <= ChatAttachment.MaxPerMessage)
            .WithMessage($"A message cannot carry more than {ChatAttachment.MaxPerMessage} attachments.");

        RuleForEach(x => x.Attachments).ChildRules(a =>
        {
            a.RuleFor(x => x.Url)
                .NotEmpty().WithMessage("Attachment URL is required.")
                .MaximumLength(1000).WithMessage("Attachment URL is too long.")
                .Must(u => Uri.TryCreate(u, UriKind.Absolute, out var uri)
                           && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
                .WithMessage("Attachment URL must be an absolute http(s) link.");
            a.RuleFor(x => x.Size)
                .GreaterThanOrEqualTo(0).WithMessage("Attachment size cannot be negative.");
        });
    }
}

public class GetChatHistoryQueryValidator : AbstractValidator<GetChatHistoryQuery>
{
    public GetChatHistoryQueryValidator()
    {
        RuleFor(x => x.ChatId)
            .NotEmpty().WithMessage("ChatId is required.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("PageSize must be between 1 and 100.");
    }
}

// GetMyChatsQuery has nothing left to validate: it carries no input at all, because
// the subject is taken from the token.