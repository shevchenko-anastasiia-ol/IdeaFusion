using Collaboration.Domain.Common;
using Collaboration.Domain.Exceptions;
using Collaboration.Domain.ValueOfObjects;
using MongoDB.Bson.Serialization.Attributes;

namespace Collaboration.Domain.Entities;

[BsonCollection("ChatMessages")]
public class ChatMessage : BaseEntity
{
    // Id групи = Id команди (одна команда — один чат)
    [BsonElement("chatId")]
    public string ChatId { get; private set; } = string.Empty;

    // Денормалізований snapshot відправника з UserService
    [BsonElement("sender")]
    public UserSnapshot Sender { get; private set; } = null!;

    [BsonElement("text")]
    public string Text { get; private set; } = string.Empty;

    // Messages written before attachments existed have no such field and read back empty.
    [BsonElement("attachments")]
    public List<ChatAttachment> Attachments { get; private set; } = [];

    [BsonConstructor]
    private ChatMessage() { }

    /// <summary>A message is text, attachments or both, but never empty.</summary>
    /// <remarks>
    /// The last parameter is deliberately not named after the <see cref="Attachments"/> property.
    /// The Mongo driver turns a constructor whose parameter names all match properties into a
    /// creator map and then requires the types to match exactly; IEnumerable vs List made that
    /// map unusable and broke every read of this collection. With a non-matching name the driver
    /// keeps using the private constructor and setters, as before.
    /// </remarks>
    public ChatMessage(string chatId, UserSnapshot sender, string? text, IEnumerable<ChatAttachment>? files = null)
    {
        if (string.IsNullOrWhiteSpace(chatId))
            throw new DomainException("ChatId cannot be empty.");

        var list = files?.ToList() ?? [];
        if (list.Count > ChatAttachment.MaxPerMessage)
            throw new DomainException($"A message cannot carry more than {ChatAttachment.MaxPerMessage} attachments.");
        if (string.IsNullOrWhiteSpace(text) && list.Count == 0)
            throw new DomainException("Text cannot be empty.");
        if (text is { Length: > 2000 })
            throw new DomainException("Text cannot exceed 2000 characters.");

        ChatId = chatId;
        Sender = sender ?? throw new DomainException("Sender cannot be null.");
        Text = text?.Trim() ?? string.Empty;
        Attachments = list;
        MarkAsCreated(sender.UserId);
    }

    /// <summary>One line for chat lists: the text, or what was sent when there is none.</summary>
    public string Preview()
    {
        if (Text.Length > 0 || Attachments.Count == 0)
            return Text;
        if (Attachments.All(a => a.Kind == ChatAttachment.Image))
            return Attachments.Count == 1 ? "📷 Фото" : $"📷 Фото ({Attachments.Count})";
        if (Attachments.Count == 1 && Attachments[0].Kind == ChatAttachment.Video)
            return "🎬 Відео";
        return Attachments.Count == 1 ? $"📎 {Attachments[0].Name ?? "Файл"}" : $"📎 Файли ({Attachments.Count})";
    }

    public void UpdateText(string text, string userId)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new DomainException("Text cannot be empty.");
        if (text.Length > 2000)
            throw new DomainException("Text cannot exceed 2000 characters.");

        Text = text.Trim();
        MarkAsUpdated(userId);
    }
}