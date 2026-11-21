using Collaboration.Domain.Entities;
using Collaboration.Domain.Exceptions;
using Collaboration.Domain.ValueOfObjects;
using FluentAssertions;
using Xunit;

namespace Collaboration.Application.Tests.DomainTests;

public class ChatMessageTests
{
    private static readonly UserSnapshot Sender = new("user-1", "alice");

    private static ChatAttachment Photo(string name = "a.jpg") =>
        new("http://localhost:9000/posts/media/1.jpg", name, "image/jpeg", 1024);

    [Fact]
    public void Constructor_WithTextOnly_HasNoAttachments()
    {
        var m = new ChatMessage("team-1", Sender, " hi ");
        m.Text.Should().Be("hi");
        m.Attachments.Should().BeEmpty();
        m.Preview().Should().Be("hi");
    }

    [Fact]
    public void Constructor_WithAttachmentsAndNoText_IsAllowed()
    {
        var m = new ChatMessage("team-1", Sender, null, [Photo(), Photo("b.jpg")]);
        m.Text.Should().BeEmpty();
        m.Attachments.Should().HaveCount(2);
        m.Preview().Should().Be("📷 Фото (2)");
    }

    [Fact]
    public void Constructor_WithNeitherTextNorAttachments_Throws()
    {
        var act = () => new ChatMessage("team-1", Sender, "  ", []);
        act.Should().Throw<DomainException>().WithMessage("Text cannot be empty.");
    }

    [Fact]
    public void Constructor_WithTooManyAttachments_Throws()
    {
        var files = Enumerable.Range(0, ChatAttachment.MaxPerMessage + 1).Select(i => Photo($"{i}.jpg"));
        var act = () => new ChatMessage("team-1", Sender, null, files);
        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData("image/png", ChatAttachment.Image)]
    [InlineData("video/mp4", ChatAttachment.Video)]
    [InlineData("audio/mpeg", ChatAttachment.Audio)]
    [InlineData("application/pdf", ChatAttachment.File)]
    [InlineData(null, ChatAttachment.File)]
    public void Attachment_KindComesFromContentType(string? contentType, string kind)
    {
        new ChatAttachment("https://cdn.example.com/x", "x", contentType, 1).Kind.Should().Be(kind);
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("/relative/path.png")]
    [InlineData("")]
    public void Attachment_WithNonHttpUrl_Throws(string url)
    {
        var act = () => new ChatAttachment(url, "x", "image/png", 1);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Preview_ForSingleFile_UsesItsName()
    {
        var doc = new ChatAttachment("https://cdn.example.com/brief.pdf", "brief.pdf", "application/pdf", 10);
        new ChatMessage("team-1", Sender, null, [doc]).Preview().Should().Be("📎 brief.pdf");
    }
}
