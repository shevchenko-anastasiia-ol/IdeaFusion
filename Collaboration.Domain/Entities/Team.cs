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
    public const string ManageVacancies = "canManageVacancies";
    public const string ReviewApplications = "canReviewApplications";
    public const string EditTeam = "canEditTeam";
}

/// <summary>
/// The roles a team member can hold. Only the platform can mint an Owner, and only
/// the platform can move somebody between Leader and Member, so the set of possible
/// roles is closed and small.
/// </summary>
public static class TeamRole
{
    public const string Owner = "Owner";
    public const string Leader = "Leader";
    public const string Member = "Member";

    public static readonly IReadOnlyList<string> All = [Owner, Leader, Member];

    public static bool IsKnown(string? role)
        => role is not null && All.Contains(role, StringComparer.Ordinal);

    /// <summary>Everything the Owner can do, spelled out rather than special-cased.</summary>
    public static readonly IReadOnlyList<string> AllPermissions =
    [
        TeamPermission.PublishPosts,
        TeamPermission.EditPosts,
        TeamPermission.InviteMembers,
        TeamPermission.RemoveMembers,
        TeamPermission.ManageVacancies,
        TeamPermission.ReviewApplications,
        TeamPermission.EditTeam
    ];

    /// <summary>
    /// The privileges a role carries before anybody narrows them. A Leader is a
    /// trusted deputy who runs the team day to day; the two things they do not get
    /// are rewriting the team itself and changing anybody's role, because those stay
    /// with the Owner and the platform respectively.
    /// </summary>
    public static IReadOnlyList<string> DefaultPermissionsFor(string role) => role switch
    {
        Owner => AllPermissions,
        Leader =>
        [
            TeamPermission.PublishPosts,
            TeamPermission.EditPosts,
            TeamPermission.InviteMembers,
            TeamPermission.RemoveMembers,
            TeamPermission.ManageVacancies,
            TeamPermission.ReviewApplications
        ],
        _ => [TeamPermission.PublishPosts]
    };
}

public class TeamMember
{
    [BsonElement("user")]
    public UserSnapshot User { get; set; } = null!;
 
    [BsonElement("role")]
    public string Role { get; set; } = TeamRole.Member;
 
    [BsonElement("joinedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("permissions")]
    public List<string> Permissions { get; set; } = [];

    /// <summary>
    /// What the member does in the team ("Веб-розробник", "UI Designer"): the free-text
    /// role of the invitation, request or vacancy they joined through. It names a craft,
    /// not a rank, so it never affects permissions; <see cref="Role"/> does that.
    /// </summary>
    [BsonElement("position")]
    [BsonIgnoreIfNull]
    public string? Position { get; set; }
 
    // Зручний accessor щоб не ламати запити по UserId
    [BsonIgnore]
    public string UserId => User.UserId;

    [BsonIgnore]
    public bool IsOwner => Role == TeamRole.Owner;

    [BsonIgnore]
    public bool IsLeader => Role == TeamRole.Leader;
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
        Members.Add(new TeamMember
        {
            User = owner,
            Role = TeamRole.Owner,
            Permissions = [.. TeamRole.AllPermissions]
        });
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
 
    /// <summary>
    /// Adds somebody who joined through an invitation, a collaboration request or a vacancy.
    /// They always come in as a plain <see cref="TeamRole.Member"/>; what they were invited
    /// for is kept as their <see cref="TeamMember.Position"/>.
    /// </summary>
    public void Join(UserSnapshot user, string? position)
        => AddMember(user, TeamRole.Member, position);

    public void AddMember(UserSnapshot user, string role, string? position = null)
    {
        if (Members.Any(m => m.UserId == user.UserId))
            throw new ConflictException("User is already a member of this team.");

        if (!TeamRole.IsKnown(role))
            throw new DomainException($"'{role}' is not a known team role.");

        // Ownership is not transferable by adding: a team has exactly one Owner, and
        // that is whoever created it. Handing out the role would let any member with
        // invite rights mint themselves a team they can then delete.
        if (role == TeamRole.Owner)
            throw new DomainException("A new member cannot be added as the team owner.");

        Members.Add(new TeamMember
        {
            User = user,
            Role = role,
            Permissions = [.. TeamRole.DefaultPermissionsFor(role)],
            Position = string.IsNullOrWhiteSpace(position) || TeamRole.IsKnown(position.Trim())
                ? null
                : position.Trim()
        });
    }
 
    public void RemoveMember(string userId, string requestedByUserId)
    {
        var member = Members.FirstOrDefault(m => m.UserId == userId)
            ?? throw new NotFoundException("Member not found in this team.");
        if (member.IsOwner)
            throw new DomainException("The team leader cannot be removed.");
        Members.Remove(member);
        MarkAsUpdated(requestedByUserId);
    }

    public bool HasPermission(string userId, string permission)
    {
        var member = Members.FirstOrDefault(m => m.UserId == userId);
        if (member is null)
            return false;

        // The Owner's privileges are not stored, they are implied, so that a demotion
        // or a permission edit can never leave an Owner with a smaller capability set.
        if (member.IsOwner)
            return true;

        // An explicit list, once somebody has narrowed it, is the whole truth.
        if (member.Permissions is { Count: > 0 })
            return member.Permissions.Contains(permission);

        return TeamRole.DefaultPermissionsFor(member.Role).Contains(permission);
    }

    public void EnsurePermission(string userId, string permission)
    {
        if (!HasPermission(userId, permission))
            throw new ForbiddenException($"You don't have the required permission '{permission}' for this team.");
    }

    public bool HasRole(string userId, string role)
        => Members.Any(m => m.UserId == userId && m.Role == role);

    public void EnsureRole(string userId, string role)
    {
        if (!HasRole(userId, role))
            throw new ForbiddenException($"Only a team {role.ToLowerInvariant()} can do this.");
    }

    /// <summary>Owner-only, for the actions that end or rebrand the team itself.</summary>
    public void EnsureOwner(string userId)
    {
        if (!HasRole(userId, TeamRole.Owner))
            throw new ForbiddenException("Only the team owner can do this.");
    }

    /// <summary>
    /// Moves a member between Leader and Member. Only the platform calls this: it is
    /// the one place where a role changes hands, and it is not something a team can
    /// do to itself. The Owner is out of reach in both directions, because the Owner
    /// is the team's identity rather than a rank within it.
    /// </summary>
    public void SetMemberRole(string targetMemberId, string newRole, string actingUserId)
    {
        if (!TeamRole.IsKnown(newRole))
            throw new DomainException($"'{newRole}' is not a known team role.");

        var member = Members.FirstOrDefault(m => m.UserId == targetMemberId)
            ?? throw new NotFoundException("Member not found in this team.");

        if (member.IsOwner)
            throw new DomainException("The team leader's role cannot be changed.");

        if (member.Role == newRole)
            return;

        member.Role = newRole;

        // A promotion hands back the role's full privilege set and a demotion takes
        // the member back to the plainest thing that role can do, so a member never
        // carries stale permissions from the rank they used to hold.
        member.Permissions = [.. TeamRole.DefaultPermissionsFor(newRole)];

        MarkAsUpdated(actingUserId);
    }

    public void SetMemberPermissions(string targetMemberId, List<string>? permissions, string actingUserId)
    {
        var actor = Members.FirstOrDefault(m => m.UserId == actingUserId)
            ?? throw new ForbiddenException("You are not a member of this team.");

        // Only the Owner narrows a member's privileges. Handing this to Leaders too
        // would make a Leader able to promote themselves by writing the promotion in.
        if (!actor.IsOwner)
            throw new ForbiddenException("Only the team leader can manage member permissions.");

        var member = Members.FirstOrDefault(m => m.UserId == targetMemberId)
            ?? throw new NotFoundException("Member not found in this team.");

        if (member.IsOwner)
            throw new DomainException("Cannot change permissions of the team leader.");

        var cleaned = (permissions ?? [])
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p.Trim())
            .Where(TeamRole.AllPermissions.Contains)
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
 
    /// <summary>
    /// Keeps "Кого шукаємо" in step with a vacancy whose role was renamed. A role the
    /// team does not list is left alone: not every vacancy comes from a required role.
    /// </summary>
    public void RenameRequiredRole(string oldRole, string newRole, string userId)
    {
        if (string.IsNullOrWhiteSpace(newRole) || string.Equals(oldRole, newRole, StringComparison.Ordinal))
            return;
        var required = RequiredRoles.FirstOrDefault(r => string.Equals(r.Role, oldRole, StringComparison.OrdinalIgnoreCase));
        if (required is null)
            return;
        required.Role = newRole.Trim();
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