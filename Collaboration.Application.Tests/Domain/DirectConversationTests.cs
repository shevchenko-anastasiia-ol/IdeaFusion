using Collaboration.Domain.Entities;
using Collaboration.Domain.Exceptions;
using Collaboration.Domain.ValueOfObjects;
using FluentAssertions;
using Xunit;

namespace Collaboration.Application.Tests.DomainTests;

public class DirectConversationTests
{
    private static readonly UserSnapshot Ann = new("11111111-1111-1111-1111-111111111111", "ann");
    private static readonly UserSnapshot Bob = new("22222222-2222-2222-2222-222222222222", "bob");

    private static DirectConversation Request() => new(Ann, Bob);
    private static ChatMessage From(DirectConversation c, UserSnapshot who, string text = "hi") => new(c.Id, who, text);

    [Fact]
    public void New_IsPendingRequestFromTheWriter()
    {
        var c = Request();
        c.Status.Should().Be(DirectConversationStatus.Pending);
        c.RequestedBy.Should().Be(Ann.UserId);
        c.Recipient.Should().Be(Bob.UserId);
        c.PairKey.Should().Be(DirectConversation.KeyFor(Bob.UserId, Ann.UserId));
    }

    [Fact]
    public void CannotWriteToYourself()
    {
        var act = () => new DirectConversation(Ann, Ann);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Pending_RequesterCanWriteUpToTheLimit()
    {
        var c = Request();
        for (var i = 0; i < DirectConversation.MaxPendingMessages; i++)
            c.RecordMessage(From(c, Ann));
        c.CanSend(Ann.UserId).Should().BeFalse();
        c.Invoking(x => x.RecordMessage(From(c, Ann))).Should().Throw<DomainException>();
    }

    [Fact]
    public void Pending_RecipientCannotReplyBeforeAccepting()
    {
        var c = Request();
        c.CanSend(Bob.UserId).Should().BeFalse();
        c.Invoking(x => x.RecordMessage(From(c, Bob))).Should().Throw<DomainException>();
    }

    [Fact]
    public void Accept_OpensTheChatForBoth()
    {
        var c = Request();
        c.Accept(Bob.UserId);
        c.Status.Should().Be(DirectConversationStatus.Accepted);
        c.CanSend(Ann.UserId).Should().BeTrue();
        c.CanSend(Bob.UserId).Should().BeTrue();
    }

    [Fact]
    public void OnlyTheRecipientCanAnswerTheRequest()
    {
        var c = Request();
        c.Invoking(x => x.Accept(Ann.UserId)).Should().Throw<ForbiddenException>();
        c.Invoking(x => x.Decline(Ann.UserId)).Should().Throw<ForbiddenException>();
    }

    [Fact]
    public void Decline_BlocksForBoth_AndOnlyTheDeclinerCanUnblock()
    {
        var c = Request();
        c.Decline(Bob.UserId);

        c.Status.Should().Be(DirectConversationStatus.Blocked);
        c.BlockedBy.Should().Be(Bob.UserId);
        c.CanSend(Ann.UserId).Should().BeFalse();
        c.CanSend(Bob.UserId).Should().BeFalse();
        c.Invoking(x => x.RecordMessage(From(c, Bob))).Should().Throw<ForbiddenException>();
        c.Invoking(x => x.Unblock(Ann.UserId)).Should().Throw<ForbiddenException>();

        c.Unblock(Bob.UserId);
        c.Status.Should().Be(DirectConversationStatus.Accepted);
        c.BlockedBy.Should().BeNull();
        c.CanSend(Ann.UserId).Should().BeTrue();
    }

    [Fact]
    public void EitherSideCanBlockAnOpenChat()
    {
        var c = Request();
        c.Accept(Bob.UserId);
        c.Block(Ann.UserId);
        c.BlockedBy.Should().Be(Ann.UserId);
        c.Invoking(x => x.Unblock(Bob.UserId)).Should().Throw<ForbiddenException>();
    }

    [Fact]
    public void Outsider_IsRejected()
    {
        var c = Request();
        var eve = new UserSnapshot("33333333-3333-3333-3333-333333333333", "eve");
        c.Invoking(x => x.Accept(eve.UserId)).Should().Throw<ForbiddenException>();
        c.Invoking(x => x.RecordMessage(From(c, eve))).Should().Throw<ForbiddenException>();
    }

    [Fact]
    public void RecordMessage_UpdatesPreview()
    {
        var c = Request();
        c.RecordMessage(From(c, Ann, "Привіт!"));
        c.LastMessagePreview.Should().Be("Привіт!");
        c.LastSenderId.Should().Be(Ann.UserId);
        c.LastMessageAt.Should().NotBeNull();
    }
}
