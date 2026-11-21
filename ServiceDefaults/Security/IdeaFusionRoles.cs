namespace ServiceDefaults.Security;

/// <summary>
/// The three realm roles defined in infrastructure/IdeaFusion-realm.json.
/// moderator and administrator are composite, so each implies the ones below it.
/// </summary>
public static class IdeaFusionRoles
{
    public const string Author = "author";
    public const string Moderator = "moderator";
    public const string Administrator = "administrator";

    /// <summary>Roles accepted by the "moderator or above" check.</summary>
    public static readonly string[] ModeratorOrAbove = { Moderator, Administrator };

    /// <summary>Roles accepted by the "author or above" check.</summary>
    public static readonly string[] AuthorOrAbove = { Author, Moderator, Administrator };

    /// <summary>Roles accepted by the "administrator" check.</summary>
    public static readonly string[] AdministratorOnly = { Administrator };
}

/// <summary>
/// Named authorization policies shared by every service, so controllers can say
/// [Authorize(Policy = IdeaFusionPolicies.RequireModerator)] without repeating strings.
/// </summary>
public static class IdeaFusionPolicies
{
    public const string RequireAuthor = "RequireAuthor";
    public const string RequireModerator = "RequireModerator";
    public const string RequireAdministrator = "RequireAdministrator";
}
