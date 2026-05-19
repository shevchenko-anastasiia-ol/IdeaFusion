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

public class TeamMember
{
    [BsonElement("user")]
    public UserSnapshot User { get; set; } = null!;
 
    [BsonElement("role")]
    public string Role { get; set; } = string.Empty;
 
    [BsonElement("joinedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
 
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
        Members.Remove(member);
        MarkAsUpdated(requestedByUserId);
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
}