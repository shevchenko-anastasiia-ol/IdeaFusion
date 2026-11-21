using Collaboration.API.Notifications;
using Collaboration.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace Collaboration.Application.Tests.API;

public class EmailNotificationTests
{
    [Fact]
    public void Settings_AreAllOnByDefault_AndSwitchOffPerKind()
    {
        var s = new NotificationSettings("user-1");
        s.Allows(NotificationKind.Invitations).Should().BeTrue();
        s.Allows(NotificationKind.Requests).Should().BeTrue();
        s.Allows(NotificationKind.Messages).Should().BeTrue();

        s.Update(invitations: true, requests: false, messages: false, "user-1");
        s.Allows(NotificationKind.Invitations).Should().BeTrue();
        s.Allows(NotificationKind.Requests).Should().BeFalse();
        s.Allows(NotificationKind.Messages).Should().BeFalse();
        s.Allows(NotificationKind.Comments).Should().BeTrue("the old overload leaves comments as they were");
    }

    [Fact]
    public void Settings_CommentsCanBeSwitchedOff()
    {
        var s = new NotificationSettings("user-1");
        s.Allows(NotificationKind.Comments).Should().BeTrue();
        s.Update(invitations: true, requests: true, messages: true, comments: false, "user-1");
        s.Allows(NotificationKind.Comments).Should().BeFalse();
        s.Allows(NotificationKind.Messages).Should().BeTrue();
    }

    [Fact]
    public void Presence_CountsEveryOpenConnection()
    {
        var p = new PresenceTracker();
        p.Connected("u");
        p.Connected("u");
        p.Disconnected("u");
        p.IsOnline("u").Should().BeTrue("a second tab is still open");
        p.Disconnected("u");
        p.IsOnline("u").Should().BeFalse();
        p.Disconnected("u");
        p.IsOnline("u").Should().BeFalse();
    }

    [Fact]
    public void MessageThrottle_LetsOneLetterThroughPerConversationAndRecipient()
    {
        var t = new MessageEmailThrottle();
        t.ShouldSend("c1", "bob").Should().BeTrue();
        t.ShouldSend("c1", "bob").Should().BeFalse();
        t.ShouldSend("c1", "ann").Should().BeTrue();
        t.ShouldSend("c2", "bob").Should().BeTrue();
    }

    [Fact]
    public void Template_EscapesUserText_AndLinksToTheApp()
    {
        var letter = new EmailNotification("u", NotificationKind.Invitations, "S", "<b>Ann</b> invites you",
            "Role: dev", "Open", "/team/invitations", "<script>alert(1)</script>");

        var html = EmailTemplate.Html(letter, "http://localhost:5173/", "Bob");

        html.Should().Contain("&lt;b&gt;Ann&lt;/b&gt;").And.NotContain("<script>");
        html.Should().Contain("http://localhost:5173/team/invitations");
        html.Should().Contain("http://localhost:5173/personal?tab=notifications");
        EmailTemplate.Text(letter, "http://localhost:5173").Should().Contain("Open: http://localhost:5173/team/invitations");
    }

    [Fact]
    public async Task Queue_DropsAnIdenticalLetterQueuedTwice()
    {
        var q = new EmailQueue();
        var letter = new EmailNotification("u", NotificationKind.Requests, "S", "H", "T", "A", "/x");
        q.Enqueue(letter);
        q.Enqueue(letter);
        q.Enqueue(letter with { Text = "other" });

        var read = new List<EmailNotification>();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        var act = async () =>
        {
            await foreach (var n in q.ReadAllAsync(cts.Token)) read.Add(n);
        };
        await act.Should().ThrowAsync<OperationCanceledException>();
        read.Should().HaveCount(2);
    }
}
