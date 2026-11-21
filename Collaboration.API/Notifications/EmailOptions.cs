namespace Collaboration.API.Notifications;

/// <summary>
/// SMTP and link settings, from the "Email" section (AppHost passes the same smtp-* values
/// it already gives Keycloak). Without a host and a user nothing is sent and the app works as before.
/// </summary>
public sealed class EmailOptions
{
    public const string Section = "Email";

    public string? Host { get; set; }
    public int Port { get; set; } = 587;
    public string? User { get; set; }
    public string? Password { get; set; }

    /// <summary>Sender address; the SMTP user when empty (Gmail rewrites it to that anyway).</summary>
    public string? From { get; set; }
    public string FromName { get; set; } = "IdeaFusion";

    /// <summary>STARTTLS on 587, as Gmail expects.</summary>
    public bool EnableSsl { get; set; } = true;

    /// <summary>Where links in the letters point: the web app.</summary>
    public string FrontendUrl { get; set; } = "http://localhost:5173";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Host) && !string.IsNullOrWhiteSpace(User);
}
