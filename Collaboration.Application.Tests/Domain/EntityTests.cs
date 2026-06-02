using Collaboration.Domain.Entities;
using Collaboration.Domain.Exceptions;
using Collaboration.Domain.ValueOfObjects;
using FluentAssertions;
using Xunit;

namespace Collaboration.Application.Tests.EntityTests;

// ── BaseEntity ────────────────────────────────────────────────────────────────

public class BaseEntityTests
{
    private class ConcreteEntity : global::Collaboration.Domain.Common.BaseEntity
    {
        public ConcreteEntity() { }
    }

    [Fact]
    public void Constructor_SetsIdAndCreatedAt()
    {
        var entity = new ConcreteEntity();
        entity.Id.Should().NotBeNullOrEmpty();
        entity.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        entity.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void MarkAsCreated_SetsCreatedByAndCreatedAt()
    {
        var entity = new ConcreteEntity();
        entity.MarkAsCreated("user-1");
        entity.CreatedBy.Should().Be("user-1");
        entity.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void MarkAsCreated_WhenUserIdNull_Throws()
    {
        var entity = new ConcreteEntity();
        entity.Invoking(e => e.MarkAsCreated(null!))
            .Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void MarkAsUpdated_SetsUpdatedByAndUpdatedAt()
    {
        var entity = new ConcreteEntity();
        entity.MarkAsUpdated("user-2");
        entity.UpdatedBy.Should().Be("user-2");
        entity.UpdatedAt.Should().NotBeNull();
        entity.UpdatedAt!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void MarkAsDeleted_SetsIsDeletedAndUpdatedAt()
    {
        var entity = new ConcreteEntity();
        entity.MarkAsDeleted();
        entity.IsDeleted.Should().BeTrue();
        entity.UpdatedAt.Should().NotBeNull();
    }
}

// ── UserSnapshot ──────────────────────────────────────────────────────────────

public class UserSnapshotTests
{
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        var snap = new UserSnapshot("user-1", "alice", "https://img/avatar.png");
        snap.UserId.Should().Be("user-1");
        snap.Username.Should().Be("alice");
        snap.AvatarUrl.Should().Be("https://img/avatar.png");
    }

    [Fact]
    public void Constructor_TrimsWhitespace()
    {
        var snap = new UserSnapshot("  user-1  ", "  alice  ", "  url  ");
        snap.UserId.Should().Be("user-1");
        snap.Username.Should().Be("alice");
        snap.AvatarUrl.Should().Be("url");
    }

    [Fact]
    public void Constructor_WithoutAvatar_AvatarUrlIsNull()
    {
        var snap = new UserSnapshot("user-1", "alice");
        snap.AvatarUrl.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WhenUserIdEmpty_Throws(string userId)
    {
        var act = () => new UserSnapshot(userId, "alice");
        act.Should().Throw<DomainException>().WithMessage("UserId cannot be empty.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WhenUsernameEmpty_Throws(string username)
    {
        var act = () => new UserSnapshot("user-1", username);
        act.Should().Throw<DomainException>().WithMessage("Username cannot be empty.");
    }

    [Fact]
    public void Constructor_WhenUsernameTooLong_Throws()
    {
        var act = () => new UserSnapshot("user-1", new string('x', 101));
        act.Should().Throw<DomainException>().WithMessage("Username cannot exceed 100 characters.");
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var a = new UserSnapshot("user-1", "alice", "url");
        var b = new UserSnapshot("user-1", "alice", "url");
        a.Should().Be(b);
        (a == b).Should().BeTrue();
        (a != b).Should().BeFalse();
    }

    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var a = new UserSnapshot("user-1", "alice");
        var b = new UserSnapshot("user-2", "bob");
        a.Should().NotBe(b);
        (a == b).Should().BeFalse();
        (a != b).Should().BeTrue();
    }

    [Fact]
    public void Equality_NullRight_ReturnsFalse()
    {
        var a = new UserSnapshot("user-1", "alice");
        (a == null).Should().BeFalse();
    }

    [Fact]
    public void Equality_BothNull_ReturnsTrue()
    {
        UserSnapshot? a = null;
        UserSnapshot? b = null;
        (a == b).Should().BeTrue();
    }

    [Fact]
    public void GetHashCode_SameValues_SameHash()
    {
        var a = new UserSnapshot("user-1", "alice");
        var b = new UserSnapshot("user-1", "alice");
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void WithUpdatedUsername_ReturnsNewInstanceWithUpdatedUsername()
    {
        var snap = new UserSnapshot("user-1", "alice", "url");
        var updated = snap.WithUpdatedUsername("bob");
        updated.Username.Should().Be("bob");
        updated.UserId.Should().Be("user-1");
        updated.AvatarUrl.Should().Be("url");
        updated.Should().NotBeSameAs(snap);
    }

    [Fact]
    public void WithUpdatedAvatar_ReturnsNewInstanceWithUpdatedAvatar()
    {
        var snap = new UserSnapshot("user-1", "alice", "old-url");
        var updated = snap.WithUpdatedAvatar("new-url");
        updated.AvatarUrl.Should().Be("new-url");
        updated.Username.Should().Be("alice");
    }

    [Fact]
    public void WithUpdatedAvatar_CanSetNull()
    {
        var snap = new UserSnapshot("user-1", "alice", "url");
        var updated = snap.WithUpdatedAvatar(null);
        updated.AvatarUrl.Should().BeNull();
    }

    [Fact]
    public void ToString_ReturnsUsername()
    {
        var snap = new UserSnapshot("user-1", "alice");
        snap.ToString().Should().Be("alice");
    }
}

// ── Team entity ───────────────────────────────────────────────────────────────

public class TeamEntityTests
{
    private static UserSnapshot Owner => new("owner-1", "OwnerUser");

    private static Team CreateTeam(string name = "My Team") =>
        new(name, "A description", "Technology", ["tag1"], Owner);

    [Fact]
    public void Constructor_SetsAllProperties()
    {
        var team = new Team("Alpha", "Desc", "Tech", ["ai"], Owner);
        team.Name.Should().Be("Alpha");
        team.Description.Should().Be("Desc");
        team.Category.Should().Be("Tech");
        team.Tags.Should().ContainSingle("ai");
        team.Status.Should().Be(TeamStatus.Searching);
        team.Members.Should().ContainSingle(m => m.UserId == "owner-1" && m.Role == "Owner");
        team.CreatedBy.Should().Be("owner-1");
        team.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void Constructor_NullTags_InitializesEmptyList()
    {
        var team = new Team("Alpha", "Desc", "Tech", null!, Owner);
        team.Tags.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithAvatarUrl_SetsAvatarUrl()
    {
        var team = new Team("Alpha", "Desc", "Tech", [], Owner, "https://img/avatar.png");
        team.AvatarUrl.Should().Be("https://img/avatar.png");
    }

    // SetName

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void SetName_WhenEmpty_Throws(string name)
    {
        var team = CreateTeam();
        team.Invoking(t => t.SetName(name))
            .Should().Throw<DomainException>().WithMessage("Team name cannot be empty.");
    }

    [Fact]
    public void SetName_WhenTooLong_Throws()
    {
        var team = CreateTeam();
        team.Invoking(t => t.SetName(new string('x', 101)))
            .Should().Throw<DomainException>().WithMessage("Team name cannot exceed 100 characters.");
    }

    [Fact]
    public void SetName_ValidName_TrimmedAndSet()
    {
        var team = CreateTeam();
        team.SetName("  New Name  ");
        team.Name.Should().Be("New Name");
    }

    // SetDescription

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void SetDescription_WhenEmpty_Throws(string desc)
    {
        var team = CreateTeam();
        team.Invoking(t => t.SetDescription(desc))
            .Should().Throw<DomainException>().WithMessage("Team description cannot be empty.");
    }

    [Fact]
    public void SetDescription_WhenTooLong_Throws()
    {
        var team = CreateTeam();
        team.Invoking(t => t.SetDescription(new string('x', 1001)))
            .Should().Throw<DomainException>().WithMessage("Team description cannot exceed 1000 characters.");
    }

    [Fact]
    public void SetDescription_ValidDesc_TrimmedAndSet()
    {
        var team = CreateTeam();
        team.SetDescription("  Nice description  ");
        team.Description.Should().Be("Nice description");
    }

    // SetCategory

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void SetCategory_WhenEmpty_Throws(string cat)
    {
        var team = CreateTeam();
        team.Invoking(t => t.SetCategory(cat))
            .Should().Throw<DomainException>().WithMessage("Team category cannot be empty.");
    }

    [Fact]
    public void SetCategory_Valid_TrimmedAndSet()
    {
        var team = CreateTeam();
        team.SetCategory("  Science  ");
        team.Category.Should().Be("Science");
    }

    // SetStatus

    [Fact]
    public void SetStatus_ChangesStatusAndMarksUpdated()
    {
        var team = CreateTeam();
        team.SetStatus(TeamStatus.Active, "user-1");
        team.Status.Should().Be(TeamStatus.Active);
        team.UpdatedBy.Should().Be("user-1");
    }

    // AddMember

    [Fact]
    public void AddMember_AddsNewMember()
    {
        var team = CreateTeam();
        var newUser = new UserSnapshot("user-2", "Bob");
        team.AddMember(newUser, "Developer");
        team.Members.Should().Contain(m => m.UserId == "user-2" && m.Role == "Developer");
    }

    [Fact]
    public void AddMember_WhenAlreadyMember_Throws()
    {
        var team = CreateTeam();
        team.Invoking(t => t.AddMember(Owner, "Developer"))
            .Should().Throw<DomainException>().WithMessage("User is already a member of this team.");
    }

    // RemoveMember

    [Fact]
    public void RemoveMember_RemovesMemberAndMarksUpdated()
    {
        var team = CreateTeam();
        var newUser = new UserSnapshot("user-2", "Bob");
        team.AddMember(newUser, "Developer");
        team.RemoveMember("user-2", "owner-1");
        team.Members.Should().NotContain(m => m.UserId == "user-2");
        team.UpdatedBy.Should().Be("owner-1");
    }

    [Fact]
    public void RemoveMember_WhenNotFound_Throws()
    {
        var team = CreateTeam();
        team.Invoking(t => t.RemoveMember("nonexistent", "owner-1"))
            .Should().Throw<DomainException>().WithMessage("Member not found in this team.");
    }

    // UpdateMemberSnapshot

    [Fact]
    public void UpdateMemberSnapshot_UpdatesExistingMember()
    {
        var team = CreateTeam();
        var updated = new UserSnapshot("owner-1", "UpdatedName", "new-url");
        team.UpdateMemberSnapshot(updated);
        team.Members.First(m => m.UserId == "owner-1").User.Username.Should().Be("UpdatedName");
        team.UpdatedBy.Should().Be("owner-1");
    }

    [Fact]
    public void UpdateMemberSnapshot_WhenUserNotMember_IsNoOp()
    {
        var team = CreateTeam();
        var stranger = new UserSnapshot("stranger", "Stranger");
        team.Invoking(t => t.UpdateMemberSnapshot(stranger)).Should().NotThrow();
    }

    // AddRequiredRole

    [Fact]
    public void AddRequiredRole_AddsRoleAndMarksUpdated()
    {
        var team = CreateTeam();
        team.AddRequiredRole("Backend Dev", "C# developer", "owner-1");
        team.RequiredRoles.Should().ContainSingle(r => r.Role == "Backend Dev");
        team.UpdatedBy.Should().Be("owner-1");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AddRequiredRole_WhenEmpty_Throws(string role)
    {
        var team = CreateTeam();
        team.Invoking(t => t.AddRequiredRole(role, null, "owner-1"))
            .Should().Throw<DomainException>().WithMessage("Required role cannot be empty.");
    }

    // Update

    [Fact]
    public void Update_SetsAllFieldsAndMarksUpdated()
    {
        var team = CreateTeam();
        team.Update("New Name", "New Desc", "New Cat", ["tag2"], "owner-1");
        team.Name.Should().Be("New Name");
        team.Description.Should().Be("New Desc");
        team.Category.Should().Be("New Cat");
        team.Tags.Should().ContainSingle("tag2");
        team.UpdatedBy.Should().Be("owner-1");
    }

    [Fact]
    public void Update_NullTags_SetsEmptyList()
    {
        var team = CreateTeam();
        team.Update("Name", "Desc", "Cat", null!, "owner-1");
        team.Tags.Should().BeEmpty();
    }

    // SetAvatarUrl

    [Fact]
    public void SetAvatarUrl_SetsValueAndMarksUpdated()
    {
        var team = CreateTeam();
        team.SetAvatarUrl("https://new-url", "owner-1");
        team.AvatarUrl.Should().Be("https://new-url");
        team.UpdatedBy.Should().Be("owner-1");
    }

    [Fact]
    public void SetAvatarUrl_CanBeSetToNull()
    {
        var team = new Team("Alpha", "Desc", "Tech", [], Owner, "url");
        team.SetAvatarUrl(null, "owner-1");
        team.AvatarUrl.Should().BeNull();
    }

    // MarkAsDeleted (inherited)

    [Fact]
    public void MarkAsDeleted_SetsIsDeletedTrue()
    {
        var team = CreateTeam();
        team.MarkAsDeleted();
        team.IsDeleted.Should().BeTrue();
    }
}

// ── TeamPost entity ───────────────────────────────────────────────────────────

public class TeamPostEntityTests
{
    private static UserSnapshot Author => new("author-1", "AuthorUser");

    [Fact]
    public void Constructor_SetsAllProperties()
    {
        var post = new TeamPost("post-1", "team-1", Author, "My Title");
        post.PostId.Should().Be("post-1");
        post.TeamId.Should().Be("team-1");
        post.Author.Should().Be(Author);
        post.Title.Should().Be("My Title");
        post.PublishedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        post.CreatedBy.Should().Be("author-1");
        post.IsDeleted.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WhenPostIdEmpty_Throws(string postId)
    {
        var act = () => new TeamPost(postId, "team-1", Author, "Title");
        act.Should().Throw<DomainException>().WithMessage("PostId cannot be empty.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WhenTeamIdEmpty_Throws(string teamId)
    {
        var act = () => new TeamPost("post-1", teamId, Author, "Title");
        act.Should().Throw<DomainException>().WithMessage("TeamId cannot be empty.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WhenTitleEmpty_Throws(string title)
    {
        var act = () => new TeamPost("post-1", "team-1", Author, title);
        act.Should().Throw<DomainException>().WithMessage("Title cannot be empty.");
    }

    [Fact]
    public void Constructor_WhenTitleTooLong_Throws()
    {
        var act = () => new TeamPost("post-1", "team-1", Author, new string('x', 201));
        act.Should().Throw<DomainException>().WithMessage("Title cannot exceed 200 characters.");
    }

    [Fact]
    public void Constructor_WhenAuthorNull_Throws()
    {
        var act = () => new TeamPost("post-1", "team-1", null!, "Title");
        act.Should().Throw<DomainException>().WithMessage("Author cannot be null.");
    }

    [Fact]
    public void Constructor_TrimsTitleWhitespace()
    {
        var post = new TeamPost("post-1", "team-1", Author, "  Trimmed Title  ");
        post.Title.Should().Be("Trimmed Title");
    }

    // UpdateTitle

    [Fact]
    public void UpdateTitle_UpdatesTitleAndMarksUpdated()
    {
        var post = new TeamPost("post-1", "team-1", Author, "Old Title");
        post.UpdateTitle("New Title", "user-1");
        post.Title.Should().Be("New Title");
        post.UpdatedBy.Should().Be("user-1");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateTitle_WhenEmpty_Throws(string title)
    {
        var post = new TeamPost("post-1", "team-1", Author, "Title");
        post.Invoking(p => p.UpdateTitle(title, "user-1"))
            .Should().Throw<DomainException>().WithMessage("Title cannot be empty.");
    }

    [Fact]
    public void UpdateTitle_WhenTooLong_Throws()
    {
        var post = new TeamPost("post-1", "team-1", Author, "Title");
        post.Invoking(p => p.UpdateTitle(new string('x', 201), "user-1"))
            .Should().Throw<DomainException>().WithMessage("Title cannot exceed 200 characters.");
    }

    [Fact]
    public void UpdateTitle_TrimsWhitespace()
    {
        var post = new TeamPost("post-1", "team-1", Author, "Old Title");
        post.UpdateTitle("  Trimmed  ", "user-1");
        post.Title.Should().Be("Trimmed");
    }

    // UpdateAuthorSnapshot

    [Fact]
    public void UpdateAuthorSnapshot_UpdatesAuthorAndMarksUpdated()
    {
        var post = new TeamPost("post-1", "team-1", Author, "Title");
        var updated = new UserSnapshot("author-1", "NewName", "new-url");
        post.UpdateAuthorSnapshot(updated);
        post.Author.Username.Should().Be("NewName");
        post.UpdatedBy.Should().Be("author-1");
    }

    [Fact]
    public void UpdateAuthorSnapshot_WhenNull_Throws()
    {
        var post = new TeamPost("post-1", "team-1", Author, "Title");
        post.Invoking(p => p.UpdateAuthorSnapshot(null!))
            .Should().Throw<DomainException>().WithMessage("Author cannot be null.");
    }
}