namespace Collaboration.Application.DTOs;

public record ChatMessageDto(
    string Id,
    string ChatId,
    string SenderId,
    string SenderName,
    string? SenderAvatarUrl,
    string Text,
    DateTime SentAt,
    IReadOnlyList<ChatAttachmentDto> Attachments)
{
    public static ChatMessageDto From(Domain.Entities.ChatMessage message) => new(
        message.Id,
        message.ChatId,
        message.Sender.UserId,
        message.Sender.Username,
        message.Sender.AvatarUrl,
        message.Text,
        message.CreatedAt,
        (message.Attachments ?? [])
            .Select(a => new ChatAttachmentDto(a.Url, a.Kind, a.Name, a.ContentType, a.Size))
            .ToList());
}

public record ChatAttachmentDto(string Url, string Kind, string? Name, string? ContentType, long Size);

public record ChatSummaryDto(
    string ChatId,
    string TeamId,
    string TeamName,
    string? TeamAvatarUrl,
    int MemberCount,
    string? LastMessage,
    string? LastSenderName,
    DateTime? LastMessageAt,
    DateTime? CreatedAt);