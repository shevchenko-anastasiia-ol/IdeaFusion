using Collaboration.Application.Commands.CollaborationRequest;
using Collaboration.Application.Commands.DirectMessage;
using Collaboration.Application.Commands.GroupInvitation;
using Collaboration.Application.Commands.Vacancy;
using Collaboration.Application.Interfaces.Services;
using Collaboration.Domain.Entities;
using Collaboration.Domain.Interfaces;
using GrpcClients.Interfaces;
using MediatR.Pipeline;

namespace Collaboration.API.Notifications;

/// <summary>
/// Turns finished commands into letters. They run after a command succeeded (a failed command
/// sends nothing), only queue the letter, and never throw: a notification problem must not
/// undo or fail the action itself. Nobody is emailed about their own action.
/// </summary>
public abstract class NotificationPostProcessor(
    EmailQueue queue,
    ICallerContext caller,
    ITeamRepository teams,
    IUserGrpcClient users,
    ILogger logger)
{
    protected ILogger Log => logger;

    protected string ActorName
    {
        get
        {
            try { return caller.Username; } catch { return "Хтось"; }
        }
    }

    protected string? ActorId
    {
        get
        {
            try { return caller.UserId; } catch { return null; }
        }
    }

    protected async Task<Team?> TeamAsync(string teamId, CancellationToken ct)
    {
        try { return await teams.GetByIdAsync(teamId, ct); } catch { return null; }
    }

    protected static string? OwnerOf(Team? team) => team?.Members.FirstOrDefault(m => m.IsOwner)?.UserId;

    /// <summary>
    /// Looks the recipient's address up now, while the caller's token is at hand (Identity answers
    /// only authenticated calls, and the background sender has no token), then queues the letter.
    /// </summary>
    protected async Task SendAsync(string? recipientId, NotificationKind kind, string subject, string heading, string text,
        string actionLabel, string actionPath, string? quote, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(recipientId) || recipientId == ActorId) return;
        if (!Guid.TryParse(recipientId, out var id)) return;

        var user = await users.GetUserByIdAsync(id, ct);
        if (user is null || string.IsNullOrWhiteSpace(user.Email))
        {
            logger.LogWarning("No email address for user {UserId}; letter \"{Subject}\" skipped", recipientId, subject);
            return;
        }
        queue.Enqueue(new EmailNotification(recipientId, kind, subject, heading, text, actionLabel, actionPath, Trim(quote))
        {
            RecipientEmail = user.Email,
            RecipientName = user.UserName,
        });
    }

    protected async Task SafeAsync(Func<Task> build)
    {
        try { await build(); }
        catch (Exception ex) { logger.LogWarning(ex, "Could not prepare an email notification"); }
    }

    private static string? Trim(string? s) =>
        string.IsNullOrWhiteSpace(s) ? null : s.Length <= 400 ? s.Trim() : s[..400].TrimEnd() + "…";
}

// ── Team invitations ─────────────────────────────────────────────────────

public sealed class InvitationSentEmail(EmailQueue q, ICallerContext c, ITeamRepository t, IUserGrpcClient u, ILogger<InvitationSentEmail> l)
    : NotificationPostProcessor(q, c, t, u, l), IRequestPostProcessor<CreateGroupInvitationCommand, GroupInvitation>
{
    public Task Process(CreateGroupInvitationCommand request, GroupInvitation inv, CancellationToken ct) => SafeAsync(async () =>
    {
        var team = await TeamAsync(inv.TeamId, ct);
        var name = team?.Name ?? "команди";
        await SendAsync(inv.InvitedUserId, NotificationKind.Invitations,
            $"Запрошення до команди «{name}»",
            $"{ActorName} запрошує вас до команди «{name}»",
            $"Роль: {inv.Role}. Запрошення діє до {inv.ExpiresAt:dd.MM.yyyy}.",
            "Переглянути запрошення", "/team/invitations", inv.Message, ct);
    });
}

public sealed class InvitationAcceptedEmail(EmailQueue q, ICallerContext c, ITeamRepository t, IUserGrpcClient u, ILogger<InvitationAcceptedEmail> l)
    : NotificationPostProcessor(q, c, t, u, l), IRequestPostProcessor<AcceptGroupInvitationCommand, GroupInvitation>
{
    public Task Process(AcceptGroupInvitationCommand request, GroupInvitation inv, CancellationToken ct) => SafeAsync(async () =>
    {
        var team = await TeamAsync(inv.TeamId, ct);
        await SendAsync(inv.InvitedByUserId, NotificationKind.Invitations,
            $"{ActorName} приєднується до «{team?.Name}»",
            $"{ActorName} прийняв(-ла) ваше запрошення",
            $"Тепер у команді «{team?.Name}» є {inv.Role.ToLowerInvariant()}.",
            "Відкрити команду", $"/teams/{inv.TeamId}?tab=members", null, ct);
    });
}

public sealed class InvitationDeclinedEmail(EmailQueue q, ICallerContext c, ITeamRepository t, IUserGrpcClient u, ILogger<InvitationDeclinedEmail> l)
    : NotificationPostProcessor(q, c, t, u, l), IRequestPostProcessor<DeclineGroupInvitationCommand, GroupInvitation>
{
    public Task Process(DeclineGroupInvitationCommand request, GroupInvitation inv, CancellationToken ct) => SafeAsync(async () =>
    {
        var team = await TeamAsync(inv.TeamId, ct);
        await SendAsync(inv.InvitedByUserId, NotificationKind.Invitations,
            $"Запрошення до «{team?.Name}» відхилено",
            $"{ActorName} відхилив(-ла) ваше запрошення",
            $"Запрошення до команди «{team?.Name}» на роль «{inv.Role}» не прийнято. Роль можна запропонувати комусь іншому.",
            "Відкрити команду", $"/teams/{inv.TeamId}", null, ct);
    });
}

// ── Join requests ────────────────────────────────────────────────────────

public sealed class JoinRequestSentEmail(EmailQueue q, ICallerContext c, ITeamRepository t, IUserGrpcClient u, ILogger<JoinRequestSentEmail> l)
    : NotificationPostProcessor(q, c, t, u, l), IRequestPostProcessor<CreateCollaborationRequestCommand, CollaborationRequest>
{
    public Task Process(CreateCollaborationRequestCommand request, CollaborationRequest req, CancellationToken ct) => SafeAsync(async () =>
    {
        var team = await TeamAsync(req.TeamId, ct);
        await SendAsync(req.ToUserId ?? OwnerOf(team), NotificationKind.Requests,
            $"Новий запит до команди «{team?.Name}»",
            $"{ActorName} хоче приєднатися до «{team?.Name}»",
            $"Роль: {req.Role}.",
            "Розглянути запит", $"/teams/{req.TeamId}?tab=requests", req.Message, ct);
    });
}

public sealed class JoinRequestAcceptedEmail(EmailQueue q, ICallerContext c, ITeamRepository t, IUserGrpcClient u, ILogger<JoinRequestAcceptedEmail> l)
    : NotificationPostProcessor(q, c, t, u, l), IRequestPostProcessor<AcceptCollaborationRequestCommand, CollaborationRequest>
{
    public Task Process(AcceptCollaborationRequestCommand request, CollaborationRequest req, CancellationToken ct) => SafeAsync(async () =>
    {
        var team = await TeamAsync(req.TeamId, ct);
        await SendAsync(req.FromUserId, NotificationKind.Requests,
            $"Вас прийнято до «{team?.Name}»",
            "Ваш запит прийнято",
            $"Тепер ви в команді «{team?.Name}» як «{req.Role}».",
            "Відкрити команду", $"/teams/{req.TeamId}", null, ct);
    });
}

public sealed class JoinRequestRejectedEmail(EmailQueue q, ICallerContext c, ITeamRepository t, IUserGrpcClient u, ILogger<JoinRequestRejectedEmail> l)
    : NotificationPostProcessor(q, c, t, u, l), IRequestPostProcessor<RejectCollaborationRequestCommand, CollaborationRequest>
{
    public Task Process(RejectCollaborationRequestCommand request, CollaborationRequest req, CancellationToken ct) => SafeAsync(async () =>
    {
        var team = await TeamAsync(req.TeamId, ct);
        await SendAsync(req.FromUserId, NotificationKind.Requests,
            $"Відповідь від «{team?.Name}»",
            "Запит не прийнято",
            $"Команда «{team?.Name}» не прийняла запит на роль «{req.Role}». Перегляньте інші вакансії — можливо, там шукають саме вас.",
            "Переглянути вакансії", "/vacancies", null, ct);
    });
}

// ── Vacancy applications ─────────────────────────────────────────────────

public sealed class ApplicationSentEmail(EmailQueue q, ICallerContext c, ITeamRepository t, IUserGrpcClient u, ILogger<ApplicationSentEmail> l)
    : NotificationPostProcessor(q, c, t, u, l), IRequestPostProcessor<ApplyToVacancyCommand, VacancyApplication>
{
    public Task Process(ApplyToVacancyCommand request, VacancyApplication app, CancellationToken ct) => SafeAsync(async () =>
    {
        var team = await TeamAsync(app.TeamId, ct);
        await SendAsync(OwnerOf(team), NotificationKind.Requests,
            $"Новий відгук на вакансію «{app.Role}»",
            $"{ActorName} відгукнувся(-лась) на «{app.Role}»",
            $"Вакансія команди «{team?.Name}».",
            "Переглянути відгук", $"/vacancies/{app.VacancyId}", app.Message, ct);
    });
}

public sealed class ApplicationAcceptedEmail(EmailQueue q, ICallerContext c, ITeamRepository t, IUserGrpcClient u, ILogger<ApplicationAcceptedEmail> l)
    : NotificationPostProcessor(q, c, t, u, l), IRequestPostProcessor<AcceptVacancyApplicationCommand, VacancyApplication>
{
    public Task Process(AcceptVacancyApplicationCommand request, VacancyApplication app, CancellationToken ct) => SafeAsync(async () =>
    {
        var team = await TeamAsync(app.TeamId, ct);
        await SendAsync(app.Applicant.UserId, NotificationKind.Requests,
            $"Ваш відгук на «{app.Role}» прийнято",
            "Вас узяли в команду",
            $"Команда «{team?.Name}» прийняла ваш відгук на вакансію «{app.Role}».",
            "Відкрити команду", $"/teams/{app.TeamId}", null, ct);
    });
}

public sealed class ApplicationRejectedEmail(EmailQueue q, ICallerContext c, ITeamRepository t, IUserGrpcClient u, ILogger<ApplicationRejectedEmail> l)
    : NotificationPostProcessor(q, c, t, u, l), IRequestPostProcessor<RejectVacancyApplicationCommand, VacancyApplication>
{
    public Task Process(RejectVacancyApplicationCommand request, VacancyApplication app, CancellationToken ct) => SafeAsync(async () =>
    {
        var team = await TeamAsync(app.TeamId, ct);
        await SendAsync(app.Applicant.UserId, NotificationKind.Requests,
            $"Відповідь на ваш відгук «{app.Role}»",
            "Відгук не прийнято",
            $"Команда «{team?.Name}» не прийняла ваш відгук на вакансію «{app.Role}».",
            "Інші вакансії", "/vacancies", null, ct);
    });
}

// ── Direct messages ──────────────────────────────────────────────────────

public sealed class DirectMessageEmail(
    EmailQueue q, ICallerContext c, ITeamRepository t, IUserGrpcClient u, MessageEmailThrottle throttle, ILogger<DirectMessageEmail> l)
    : NotificationPostProcessor(q, c, t, u, l), IRequestPostProcessor<SendDirectMessageCommand, DirectMessageSent>
{
    public Task Process(SendDirectMessageCommand request, DirectMessageSent sent, CancellationToken ct) => SafeAsync(async () =>
    {
        var conversation = sent.Conversation;
        var senderId = sent.Message.Sender.UserId;
        var recipient = conversation.OtherId(senderId);
        var preview = sent.Message.Preview();

        // The first message of a request always gets a letter; later ones at most every 5 minutes.
        var isNewRequest = conversation.Status == DirectConversationStatus.Pending && conversation.PendingMessageCount == 1;
        if (!isNewRequest && !throttle.ShouldSend(conversation.Id, recipient))
        {
            Log.LogInformation("Message letter to {UserId} skipped: one was already sent for this chat in the last {Minutes} min",
                recipient, MessageEmailThrottle.Window.TotalMinutes);
            return;
        }

        if (isNewRequest)
            await SendAsync(recipient, NotificationKind.Messages,
                $"{ActorName} хоче з вами листуватися",
                "Новий запит на листування",
                $"{ActorName} написав(-ла) вам уперше. Прийміть запит, щоб відповісти, або відхиліть — тоді писати вам більше не зможуть.",
                "Відкрити профіль", $"/users/{senderId}", preview, ct);
        else if (conversation.Status != DirectConversationStatus.Blocked)
            await SendAsync(recipient, NotificationKind.Messages,
                $"Нове повідомлення від {ActorName}",
                $"{ActorName} пише вам",
                "Поки вас не було, прийшло нове повідомлення.",
                "Відповісти", $"/users/{senderId}", preview, ct);
    });
}

public sealed class DirectRequestAcceptedEmail(EmailQueue q, ICallerContext c, ITeamRepository t, IUserGrpcClient u, ILogger<DirectRequestAcceptedEmail> l)
    : NotificationPostProcessor(q, c, t, u, l), IRequestPostProcessor<ChangeDirectConversationCommand, DirectConversation>
{
    // Only acceptance is announced; a decline or a block is never mailed to the other person.
    public Task Process(ChangeDirectConversationCommand request, DirectConversation conversation, CancellationToken ct) => SafeAsync(async () =>
    {
        if (request.Action == DirectConversationAction.Accept)
            await SendAsync(conversation.RequestedBy, NotificationKind.Messages,
                $"{ActorName} прийняв(-ла) ваш запит на листування",
                "Запит на листування прийнято",
                $"Тепер ви з {ActorName} можете вільно переписуватися.",
                "Відкрити профіль", $"/users/{ActorId}", null, ct);
    });
}

// ── Team chat ────────────────────────────────────────────────────────────

/// <summary>
/// A team chat message goes to every member except the sender. Same rhythm as direct messages:
/// at most one letter per chat and member every <see cref="MessageEmailThrottle.Window"/>.
/// </summary>
public sealed class TeamChatMessageEmail(
    EmailQueue q, ICallerContext c, ITeamRepository t, IUserGrpcClient u, MessageEmailThrottle throttle, ILogger<TeamChatMessageEmail> l)
    : NotificationPostProcessor(q, c, t, u, l),
      IRequestPostProcessor<Collaboration.Application.Commands.ChatMessage.SendChatMessageCommand, ChatMessage>
{
    public Task Process(Collaboration.Application.Commands.ChatMessage.SendChatMessageCommand request, ChatMessage message, CancellationToken ct) => SafeAsync(async () =>
    {
        var team = await TeamAsync(message.ChatId, ct);
        if (team is null) return;

        var senderId = message.Sender.UserId;
        var recipients = team.Members
            .Select(m => m.UserId)
            .Where(id => !string.IsNullOrWhiteSpace(id) && id != senderId)
            .Distinct()
            .ToList();
        var preview = message.Preview();

        foreach (var recipient in recipients)
        {
            if (!throttle.ShouldSend($"team:{team.Id}", recipient))
            {
                Log.LogInformation("Team chat letter to {UserId} skipped: one was already sent for «{Team}» in the last {Minutes} min",
                    recipient, team.Name, MessageEmailThrottle.Window.TotalMinutes);
                continue;
            }
            await SendAsync(recipient, NotificationKind.Messages,
                $"Нове повідомлення в чаті «{team.Name}»",
                $"{ActorName} пише в чаті команди «{team.Name}»",
                "У чаті вашої команди нове повідомлення.",
                "Відкрити команду", $"/teams/{team.Id}", preview, ct);
        }
    });
}
