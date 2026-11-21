using System.Net;
using System.Net.Mail;
using System.Text;
using Collaboration.Domain.Entities;
using Collaboration.Domain.Interfaces;
using Microsoft.Extensions.Options;

namespace Collaboration.API.Notifications;

/// <summary>
/// Sends the queued letters one by one: checks the recipient's settings
/// and hands the letter to SMTP. The address was resolved
/// when the letter was queued. A letter that cannot be sent is logged and dropped.
/// </summary>
public sealed class EmailDispatcher(
    EmailQueue queue,
    IServiceScopeFactory scopes,
    IOptions<EmailOptions> options,
    ILogger<EmailDispatcher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        if (!settings.IsConfigured)
            logger.LogWarning("Email notifications are off: Email:Host / Email:User are not set (smtp-user, smtp-app-password in AppHost user secrets)");
        else
            logger.LogInformation("Email notifications go through {Host}:{Port} as {User}", settings.Host, settings.Port, settings.User);

        await foreach (var letter in queue.ReadAllAsync(stoppingToken))
        {
            if (!settings.IsConfigured) continue;
            try
            {
                await SendAsync(letter, settings, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Email to user {UserId} ({Kind}) was not sent", letter.RecipientUserId, letter.Kind);
            }
        }
    }

    private async Task SendAsync(EmailNotification letter, EmailOptions settings, CancellationToken ct)
    {
        // Being online no longer suppresses a message letter: somebody with the app open in a
        // background tab still missed it, and the 15-minute throttle keeps the inbox calm.
        using var scope = scopes.CreateScope();
        var prefs = await scope.ServiceProvider.GetRequiredService<INotificationSettingsRepository>()
            .GetByUserAsync(letter.RecipientUserId, ct);
        if (prefs is not null && !prefs.Allows(letter.Kind))
        {
            logger.LogInformation("Email \"{Subject}\" to user {UserId} skipped: {Kind} letters are switched off", letter.Subject, letter.RecipientUserId, letter.Kind);
            return;
        }

        if (string.IsNullOrWhiteSpace(letter.RecipientEmail))
        {
            logger.LogWarning("Email \"{Subject}\" to user {UserId} skipped: no address", letter.Subject, letter.RecipientUserId);
            return;
        }

        var from = new MailAddress(string.IsNullOrWhiteSpace(settings.From) ? settings.User! : settings.From, settings.FromName);
        using var message = new MailMessage(from, new MailAddress(letter.RecipientEmail, letter.RecipientName))
        {
            Subject = letter.Subject,
            Body = EmailTemplate.Html(letter, settings.FrontendUrl, letter.RecipientName),
            IsBodyHtml = true,
            BodyEncoding = Encoding.UTF8,
            SubjectEncoding = Encoding.UTF8,
        };
        message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(
            EmailTemplate.Text(letter, settings.FrontendUrl), Encoding.UTF8, "text/plain"));

        using var client = new SmtpClient(settings.Host, settings.Port)
        {
            EnableSsl = settings.EnableSsl,
            Credentials = new NetworkCredential(settings.User, settings.Password),
            DeliveryMethod = SmtpDeliveryMethod.Network,
        };
        await client.SendMailAsync(message, ct);
        logger.LogInformation("Email \"{Subject}\" sent to user {UserId} <{Email}>", letter.Subject, letter.RecipientUserId, letter.RecipientEmail);
    }
}

/// <summary>A plain, light letter: heading, a line of text, an optional quote and one button.</summary>
public static class EmailTemplate
{
    public static string Html(EmailNotification n, string frontendUrl, string? name)
    {
        string E(string? s) => WebUtility.HtmlEncode(s ?? string.Empty);
        var link = Url(frontendUrl, n.ActionPath);
        var settings = Url(frontendUrl, "/personal?tab=notifications");
        var quote = string.IsNullOrWhiteSpace(n.Quote)
            ? ""
            : $"""<blockquote style="margin:16px 0;padding:12px 16px;border-left:3px solid #7c5cff;background:#f5f3ff;color:#333;white-space:pre-wrap">{E(n.Quote)}</blockquote>""";
        return $"""
            <!DOCTYPE html>
            <html lang="uk"><body style="margin:0;padding:24px;background:#f4f4f6;font-family:Segoe UI,Arial,sans-serif;color:#1d1d22">
              <div style="max-width:560px;margin:0 auto;background:#fff;border-radius:12px;padding:28px">
                <div style="font-weight:700;font-size:15px;color:#7c5cff;margin-bottom:18px">IdeaFusion</div>
                <p style="margin:0 0 6px;color:#555">Привіт{(string.IsNullOrWhiteSpace(name) ? "" : ", " + E(name))}!</p>
                <h1 style="margin:0 0 12px;font-size:20px">{E(n.Heading)}</h1>
                <p style="margin:0;line-height:1.5">{E(n.Text)}</p>
                {quote}
                <p style="margin:24px 0 0"><a href="{E(link)}" style="display:inline-block;padding:11px 20px;border-radius:8px;background:#7c5cff;color:#fff;text-decoration:none;font-weight:600">{E(n.ActionLabel)}</a></p>
              </div>
              <p style="max-width:560px;margin:14px auto 0;font-size:12px;color:#888;text-align:center">
                Ви отримали цей лист, бо в IdeaFusion увімкнено сповіщення на пошту.
                <a href="{E(settings)}" style="color:#888">Налаштувати або вимкнути</a>
              </p>
            </body></html>
            """;
    }

    public static string Text(EmailNotification n, string frontendUrl)
    {
        var quote = string.IsNullOrWhiteSpace(n.Quote) ? "" : $"\n\n«{n.Quote}»";
        return $"{n.Heading}\n\n{n.Text}{quote}\n\n{n.ActionLabel}: {Url(frontendUrl, n.ActionPath)}\n\n" +
               $"Налаштувати листи: {Url(frontendUrl, "/personal?tab=notifications")}";
    }

    private static string Url(string baseUrl, string path) => baseUrl.TrimEnd('/') + path;
}
