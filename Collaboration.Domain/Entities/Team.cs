using Collaboration.Domain.Common;
using Collaboration.Domain.Exceptions;
using Collaboration.Domain.ValueOfObjects;
using MongoDB.Bson.Serialization.Attributes;

namespace Collaboration.Domain.Entities;

public enum TeamStatus
{
    Active,
    Searching,
    Completed
}

public static class TeamPermission
{
    public const string PublishPosts = "canPublishPosts";
    public const string EditPosts = "canEditPosts";
    public const string InviteMembers = "canInviteMembers";
    public const string RemoveMembers = "canRemoveMembers";
    public const string EditTeam = "canEditTeam";
}

public class TeamMember
{
    [BsonElement("user")]
    public UserSnapshot User { get; set; } = null!;
 
    [BsonElement("role")]
    public string Role { get; set; } = string.Empty;
 
    [BsonElement("joinedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("permissions")]
    public List<string> Permissions { get; set; } = [];
 
    // Зручний accessor щоб не ламати запити по UserId
    [BsonIgnore]
    public string UserId => User.UserId;
}
 
public class RequiredRole
{
    [BsonElement("role")]
    public string Role { get; set; } = string.Empty;
 
    [BsonElement("description")]
    [BsonIgnoreIfNull]
    public string? Description { get; set; }
}
 
[BsonCollection("Teams")]
public class Team : BaseEntity
{
    [BsonElement("name")]
    public string Name { get; private set; } = string.Empty;
 
    [BsonElement("description")]
    public string Description { get; private set; } = string.Empty;
 
    [BsonElement("category")]
    public string Category { get; private set; } = string.Empty;
 
    [BsonElement("tags")]
    public List<string> Tags { get; private set; } = [];
 
    [BsonElement("status")]
    public TeamStatus Status { get; private set; } = TeamStatus.Searching;
 
    [BsonElement("members")]
    public List<TeamMember> Members { get; private set; } = [];
 
    [BsonElement("requiredRoles")]
    public List<RequiredRole> RequiredRoles { get; private set; } = [];

    [BsonElement("avatarUrl")]
    [BsonIgnoreIfNull]
    public string? AvatarUrl { get; private set; }

    [BsonElement("openVacancyCount")]
    public int OpenVacancyCount { get; private set; }

    [BsonConstructor]
    private Team() { }

    public Team(string name, string description, string category, List<string> tags, UserSnapshot owner, string? avatarUrl = null)
    {
        SetName(name);
        SetDescription(description);
        SetCategory(category);
        Tags = tags ?? [];
        AvatarUrl = avatarUrl;
        Members.Add(new TeamMember { User = owner, Role = "Owner" });
        MarkAsCreated(owner.UserId);
    }

    public void SetAvatarUrl(string? avatarUrl, string userId)
    {
        AvatarUrl = avatarUrl;
        MarkAsUpdated(userId);
    }
 
    public void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Team name cannot be empty.");
        if (name.Length > 100)
            throw new DomainException("Team name cannot exceed 100 characters.");
        Name = name.Trim();
    }
 
    public void SetDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("Team description cannot be empty.");
        if (description.Length > 1000)
            throw new DomainException("Team description cannot exceed 1000 characters.");
        Description = description.Trim();
    }
 
    public void SetCategory(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
            throw new DomainException("Team category cannot be empty.");
        Category = category.Trim();
    }
 
    public void SetStatus(TeamStatus status, string userId)
    {
        Status = status;
        MarkAsUpdated(userId);
    }
 
    public void AddMember(UserSnapshot user, string role)
    {
        if (Members.Any(m => m.UserId == user.UserId))
            throw new DomainException("User is already a member of this team.");
        Members.Add(new TeamMember { User = user, Role = role });
    }
 
    public void RemoveMember(string userId, string requestedByUserId)
    {
        var member = Members.FirstOrDefault(m => m.UserId == userId)
            ?? throw new DomainException("Member not found in this team.");
        if (member.Role == "Owner")
            throw new DomainException("The team leader cannot be removed.");
        Members.Remove(member);
        MarkAsUpdated(requestedByUserId);
    }

    public bool HasPermission(string userId, string permission)
    {
        var member = Members.FirstOrDefault(m => m.UserId == userId);
        if (member is null)
            return false;

        if (member.Role == "Owner")
            return true;

        if (member.Permissions is null || member.Permissions.Count == 0)
            return permission == TeamPermission.PublishPosts;

        return member.Permissions.Contains(permission);
    }

    public void EnsurePermission(string userId, string permission)
    {
        if (!HasPermission(userId, permission))
            throw new DomainException($"You don't have the required permission '{permission}' for this team.");
    }

    public void EnsureLeader(string userId)
    {
        if (!Members.Any(m => m.UserId == userId && m.Role == "Owner"))
            throw new DomainException("Only the team leader can do this.");
    }

    public void SetMemberPermissions(string targetMemberId, List<string>? permissions, string actingUserId)
    {
        var actor = Members.FirstOrDefault(m => m.UserId == actingUserId)
            ?? throw new DomainException("You are not a member of this team.");

        if (actor.Role != "Owner")
            throw new DomainException("Only the team leader can manage member permissions.");

        var member = Members.FirstOrDefault(m => m.UserId == targetMemberId)
            ?? throw new DomainException("Member not found in this team.");

        if (member.Role == "Owner")
            throw new DomainException("Cannot change permissions of the team leader.");

        var known = new[]
        {
            TeamPermission.PublishPosts, TeamPermission.EditPosts, TeamPermission.InviteMembers,
            TeamPermission.RemoveMembers, TeamPermission.EditTeam
        };

        var cleaned = (permissions ?? [])
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p.Trim())
            .Where(known.Contains)
            .Distinct()
            .ToList();

        member.Permissions = cleaned;
        MarkAsUpdated(actingUserId);
    }
 
    public void UpdateMemberSnapshot(UserSnapshot updatedUser)
    {
        var member = Members.FirstOrDefault(m => m.UserId == updatedUser.UserId);
        if (member is null) return;
        member.User = updatedUser;
        MarkAsUpdated(updatedUser.UserId);
    }
 
    public void AddRequiredRole(string role, string? description, string userId)
    {
        if (string.IsNullOrWhiteSpace(role))
            throw new DomainException("Required role cannot be empty.");
        RequiredRoles.Add(new RequiredRole { Role = role.Trim(), Description = description });
        MarkAsUpdated(userId);
    }
 
    public void Update(string name, string description, string category, List<string> tags, string userId)
    {
        SetName(name);
        SetDescription(description);
        SetCategory(category);
        Tags = tags ?? [];
        MarkAsUpdated(userId);
    }

    public void IncrementOpenVacancyCount(string userId)
    {
        OpenVacancyCount++;
        MarkAsUpdated(userId);
    }

    public void DecrementOpenVacancyCount(string userId)
    {
        if (OpenVacancyCount > 0)
        {
            OpenVacancyCount--;
            MarkAsUpdated(userId);
        }
    }
}