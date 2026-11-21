using Collaboration.Domain.Common;
using Collaboration.Domain.Exceptions;
using Collaboration.Domain.ValueOfObjects;
using MongoDB.Bson.Serialization.Attributes;

namespace Collaboration.Domain.Entities;

public enum VacancyApplicationStatus
{
    Pending,
    Accepted,
    Rejected,
    Cancelled
}

[BsonCollection("VacancyApplications")]
public class VacancyApplication : BaseEntity
{
    [BsonElement("vacancyId")]
    public string VacancyId { get; private set; } = string.Empty;

    [BsonElement("teamId")]
    public string TeamId { get; private set; } = string.Empty;

    [BsonElement("applicant")]
    public UserSnapshot Applicant { get; private set; } = null!;

    [BsonElement("role")]
    public string Role { get; private set; } = string.Empty;

    [BsonElement("message")]
    [BsonIgnoreIfNull]
    public string? Message { get; private set; }

    [BsonElement("status")]
    public VacancyApplicationStatus Status { get; private set; } = VacancyApplicationStatus.Pending;

    [BsonElement("resolvedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    [BsonIgnoreIfNull]
    public DateTime? ResolvedAt { get; private set; }

    [BsonConstructor]
    private VacancyApplication() { }

    public VacancyApplication(string vacancyId, string teamId, UserSnapshot applicant, string role, string? message)
    {
        if (string.IsNullOrWhiteSpace(vacancyId))
            throw new DomainException("VacancyId cannot be empty.");
        if (string.IsNullOrWhiteSpace(teamId))
            throw new DomainException("TeamId cannot be empty.");
        if (string.IsNullOrWhiteSpace(role))
            throw new DomainException("Role cannot be empty.");
        if (message is { Length: > 1000 })
            throw new DomainException("Message cannot exceed 1000 characters.");

        VacancyId = vacancyId;
        TeamId = teamId;
        Applicant = applicant;
        Role = role.Trim();
        Message = message?.Trim();
        MarkAsCreated(applicant.UserId);
    }

    public void Accept(string userId)
    {
        EnsurePending();
        Status = VacancyApplicationStatus.Accepted;
        ResolvedAt = DateTime.UtcNow;
        MarkAsUpdated(userId);
    }

    public void Reject(string userId)
    {
        EnsurePending();
        Status = VacancyApplicationStatus.Rejected;
        ResolvedAt = DateTime.UtcNow;
        MarkAsUpdated(userId);
    }

    public void Cancel(string userId)
    {
        EnsurePending();
        Status = VacancyApplicationStatus.Cancelled;
        ResolvedAt = DateTime.UtcNow;
        MarkAsUpdated(userId);
    }

    public void UpdateApplicantSnapshot(UserSnapshot updated)
    {
        Applicant = Applicant
            .WithUpdatedUsername(updated.Username)
            .WithUpdatedAvatar(updated.AvatarUrl);
        MarkAsUpdated(updated.UserId);
    }

    private void EnsurePending()
    {
        if (Status != VacancyApplicationStatus.Pending)
            throw new DomainException($"Application is already {Status}.");
    }
}