using Collaboration.Domain.Entities;
using Collaboration.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceDefaults.Extensions;

namespace Collaboration.API.Controllers;

/// <summary>The caller's email notification switches. Under /api/collaboration so the gateway already routes it.</summary>
[ApiController]
[Route("api/collaboration/notification-settings")]
[Authorize]
public class NotificationSettingsController(INotificationSettingsRepository settings) : ControllerBase
{
    public sealed record NotificationSettingsDto(bool EmailInvitations, bool EmailRequests, bool EmailMessages, bool EmailComments = true);

    private string Me => User.GetUserId().ToString();

    [HttpGet]
    public async Task<ActionResult<NotificationSettingsDto>> Get(CancellationToken ct)
    {
        var s = await settings.GetByUserAsync(Me, ct) ?? new NotificationSettings(Me);
        return new NotificationSettingsDto(s.EmailInvitations, s.EmailRequests, s.EmailMessages, s.EmailComments);
    }

    [HttpPut]
    public async Task<ActionResult<NotificationSettingsDto>> Put([FromBody] NotificationSettingsDto body, CancellationToken ct)
    {
        var me = Me;
        var s = await settings.GetByUserAsync(me, ct) ?? new NotificationSettings(me);
        s.Update(body.EmailInvitations, body.EmailRequests, body.EmailMessages, body.EmailComments, me);
        await settings.SaveAsync(s, ct);
        return new NotificationSettingsDto(s.EmailInvitations, s.EmailRequests, s.EmailMessages, s.EmailComments);
    }

    /// <summary>
    /// Whether <paramref name="userId"/> agreed to letters of <paramref name="kind"/>. For other
    /// services that send their own letters (ContentService: comments). Answers only yes/no.
    /// </summary>
    [HttpGet("allows/{userId}/{kind}")]
    public async Task<ActionResult<bool>> Allows(string userId, NotificationKind kind, CancellationToken ct)
    {
        var s = await settings.GetByUserAsync(userId, ct);
        return s?.Allows(kind) ?? true;
    }
}
