using Collaboration.Domain.Common;
using Collaboration.Domain.Exceptions;
using MongoDB.Bson.Serialization.Attributes;

namespace Collaboration.Domain.Entities;

public enum TeamVacancyStatus
{
    Open,
    Closed
}

[BsonCollection("TeamVacancies")]
public class TeamVacancy : BaseEntity
{
    [BsonElement("teamId")]
    public string TeamId { get; private set; } = string.Empty;

    [BsonElement("teamName")]
    public string TeamName { get; private set; } = string.Empty;

    [BsonElement("teamAvatarUrl")]
    [BsonIgnoreIfNull]
    public string? TeamAvatarUrl { get; private set; }

    [BsonElement("category")]
    public string Category { get; private set; } = string.Empty;

    [BsonElement("tags")]
    public List<string> Tags { get; private set; } = [];

    [BsonElement("role")]
    public string Role { get; private set; } = string.Empty;

    [BsonElement("description")]
    [BsonIgnoreIfNull]
    public string? Description { get; private set; }

    [BsonElement("requirements")]
    [BsonIgnoreIfNull]
    public string? Requirements { get; private set; }

    [BsonElement("status")]
    public TeamVacancyStatus Status { get; private set; } = TeamVacancyStatus.Open;

    [BsonConstructor]
    private TeamVacancy() { }

    public TeamVacancy(
        string teamId,
        string teamName,
        string category,
        List<string> tags,
        string role,
        string creatorUserId,
        string? description = null,
        string? requirements = null,
        string? teamAvatarUrl = null)
    {
        if (string.IsNullOrWhiteSpace(teamId))
            throw new DomainException("TeamId cannot be empty.");
        if (string.IsNullOrWhiteSpace(role))
            throw new DomainException("Role cannot be empty.");
        if (role.Length > 100)
            throw new DomainException("Role cannot exceed 100 characters.");
        if (description is { Length: > 2000 })
            throw new DomainException("Description cannot exceed 2000 characters.");
        if (requirements is { Length: > 2000 })
            throw new DomainException("Requirements cannot exceed 2000 characters.");

        TeamId = teamId;
        TeamName = teamName.Trim();
        TeamAvatarUrl = teamAvatarUrl;
        Category = category.Trim();
        Tags = tags ?? [];
        Role = role.Trim();
        Description = description?.Trim();
        Requirements = requirements?.Trim();
        MarkAsCreated(creatorUserId);
    }

    /// <summary>
    /// Rewrites what the vacancy asks for. Only an open vacancy can change: a closed one
    /// is the record of a hire and of the applications it received.
    /// </summary>
    public void Update(string role, string? description, string? requirements, string userId)
    {
        EnsureOpen();
        if (string.IsNullOrWhiteSpace(role))
            throw new DomainException("Role cannot be empty.");
        if (role.Length > 100)
            throw new DomainException("Role cannot exceed 100 characters.");
        if (description is { Length: > 2000 })
            throw new DomainException("Description cannot exceed 2000 characters.");
        if (requirements is { Length: > 2000 })
            throw new DomainException("Requirements cannot exceed 2000 characters.");

        Role = role.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        Requirements = string.IsNullOrWhiteSpace(requirements) ? null : requirements.Trim();
        MarkAsUpdated(userId);
    }

    public void Close(string userId)
    {
        EnsureOpen();
        Status = TeamVacancyStatus.Closed;
        MarkAsUpdated(userId);
    }

    public void UpdateTeamSnapshot(string teamName, string? teamAvatarUrl, string userId)
    {
        TeamName = teamName.Trim();
        TeamAvatarUrl = teamAvatarUrl;
        MarkAsUpdated(userId);
    }

    private void EnsureOpen()
    {
        if (Status != TeamVacancyStatus.Open)
            throw new DomainException($"Vacancy is already {Status}.");
    }
}