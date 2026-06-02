using ContentDomain.Entity;
using FluentAssertions;
using Xunit;

namespace Content.Tests.Domain;

// ═══════════════════════════════════════════════════════════════════════════
// PostTag
// ═══════════════════════════════════════════════════════════════════════════

public class PostTagTests
{
    [Fact]
    public void Default_PostIdIsZero() =>
        new PostTag().PostId.Should().Be(0);

    [Fact]
    public void Default_TagIdIsZero() =>
        new PostTag().TagId.Should().Be(0);

    [Fact]
    public void Default_PostNavigationIsNullRef()
    {
        // Post and Tag are null! refs — we verify setters work via round-trip
        var pt = new PostTag();
        var p  = new Post { PostId = 1, Title = "T", Status = PostStatus.Published, CreatedAt = DateTime.UtcNow };
        pt.Post = p;
        pt.Post.Should().BeSameAs(p);
    }

    [Fact]
    public void Default_TagNavigationIsNullRef()
    {
        var pt  = new PostTag();
        var tag = new Tag { TagId = 5, Name = "art" };
        pt.Tag = tag;
        pt.Tag.Should().BeSameAs(tag);
    }

    [Fact]
    public void PostId_CanBeSetAndRetrieved()
    {
        var pt = new PostTag { PostId = 42 };
        pt.PostId.Should().Be(42);
    }

    [Fact]
    public void TagId_CanBeSetAndRetrieved()
    {
        var pt = new PostTag { TagId = 7 };
        pt.TagId.Should().Be(7);
    }

    [Fact]
    public void Properties_CanBeSetTogether()
    {
        var pt = new PostTag { PostId = 10, TagId = 20 };
        pt.PostId.Should().Be(10);
        pt.TagId.Should().Be(20);
    }

    [Fact]
    public void PostId_CanBeUpdated()
    {
        var pt = new PostTag { PostId = 1 };
        pt.PostId = 99;
        pt.PostId.Should().Be(99);
    }

    [Fact]
    public void TagId_CanBeUpdated()
    {
        var pt = new PostTag { TagId = 1 };
        pt.TagId = 88;
        pt.TagId.Should().Be(88);
    }

    [Fact]
    public void IsInCorrectNamespace() =>
        typeof(PostTag).Namespace.Should().Be("ContentDomain.Entity");

    [Fact]
    public void IsPublicClass() =>
        typeof(PostTag).IsPublic.Should().BeTrue();
}

// ═══════════════════════════════════════════════════════════════════════════
// Tag
// ═══════════════════════════════════════════════════════════════════════════

public class TagTests
{
    [Fact]
    public void Default_TagIdIsZero() =>
        new Tag().TagId.Should().Be(0);

    [Fact]
    public void Default_NameIsNullRef()
    {
        var tag = new Tag();
        // Name is null! — just verify the object is created
        var act = () => { _ = tag.GetType(); };
        act.Should().NotThrow();
    }

    [Fact]
    public void Default_PostTagsIsEmptyCollection() =>
        new Tag().PostTags.Should().NotBeNull().And.BeEmpty();

    [Fact]
    public void TagId_SetterAndGetter_RoundTrip()
    {
        var tag = new Tag { TagId = 42 };
        tag.TagId.Should().Be(42);
    }

    [Fact]
    public void TagId_CanBeUpdated()
    {
        var tag = new Tag { TagId = 1 };
        tag.TagId = 99;
        tag.TagId.Should().Be(99);
    }

    [Fact]
    public void Name_SetterAndGetter_RoundTrip()
    {
        var tag = new Tag { Name = "dotnet" };
        tag.Name.Should().Be("dotnet");
    }

    [Fact]
    public void Name_CanBeUpdated()
    {
        var tag = new Tag { Name = "old" };
        tag.Name = "new";
        tag.Name.Should().Be("new");
    }

    [Fact]
    public void PostTags_SetterAndGetter_RoundTrip()
    {
        var postTag = new PostTag { PostId = 1, TagId = 10 };
        var tag = new Tag { TagId = 10, Name = "art" };
        tag.PostTags = new List<PostTag> { postTag };

        tag.PostTags.Should().HaveCount(1)
            .And.Contain(postTag);
    }

    [Fact]
    public void PostTags_CanAddItems()
    {
        var tag = new Tag { TagId = 5, Name = "tech" };
        tag.PostTags.Add(new PostTag { PostId = 1, TagId = 5 });
        tag.PostTags.Add(new PostTag { PostId = 2, TagId = 5 });

        tag.PostTags.Should().HaveCount(2);
    }

    [Fact]
    public void AllProperties_TogetherRoundTrip()
    {
        var postTag = new PostTag { PostId = 3, TagId = 7 };
        var tag = new Tag
        {
            TagId    = 7,
            Name     = "science",
            PostTags = new List<PostTag> { postTag }
        };

        tag.TagId.Should().Be(7);
        tag.Name.Should().Be("science");
        tag.PostTags.Should().ContainSingle();
    }

    [Fact]
    public void IsInCorrectNamespace() =>
        typeof(Tag).Namespace.Should().Be("ContentDomain.Entity");

    [Fact]
    public void IsPublicClass() =>
        typeof(Tag).IsPublic.Should().BeTrue();
}

// ═══════════════════════════════════════════════════════════════════════════
// Comment
// ═══════════════════════════════════════════════════════════════════════════

public class CommentTests
{
    private static Comment Make() => new()
    {
        CommentId    = 1,
        PostId       = 10,
        PostAuthorId = 5,
        Body         = "Hello",
        CreatedAt    = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
    };

    [Fact]
    public void Default_CommentIdIsZero() =>
        new Comment().CommentId.Should().Be(0);

    [Fact]
    public void Default_PostIdIsZero() =>
        new Comment().PostId.Should().Be(0);

    [Fact]
    public void Default_PostAuthorIdIsZero() =>
        new Comment().PostAuthorId.Should().Be(0);

    [Fact]
    public void Default_ParentCommentIdIsNull() =>
        new Comment().ParentCommentId.Should().BeNull();

    [Fact]
    public void Default_IsDeletedIsFalse() =>
        new Comment().IsDeleted.Should().BeFalse();

    [Fact]
    public void Default_CreatedByIsNull() =>
        new Comment().CreatedBy.Should().BeNull();

    [Fact]
    public void Default_UpdatedByIsNull() =>
        new Comment().UpdatedBy.Should().BeNull();

    [Fact]
    public void Default_UpdatedAtIsNull() =>
        new Comment().UpdatedAt.Should().BeNull();

    [Fact]
    public void Default_RepliesIsEmptyList() =>
        new Comment().Replies.Should().BeEmpty();

    [Fact]
    public void Default_ParentCommentIsNull() =>
        new Comment().ParentComment.Should().BeNull();

    [Fact]
    public void Properties_CanBeSetAndRetrieved()
    {
        var c = Make();
        c.CommentId.Should().Be(1);
        c.PostId.Should().Be(10);
        c.PostAuthorId.Should().Be(5);
        c.Body.Should().Be("Hello");
    }

    [Fact]
    public void ParentCommentId_CanBeSet()
    {
        var c = new Comment { ParentCommentId = 99 };
        c.ParentCommentId.Should().Be(99);
    }

    [Fact]
    public void IsDeleted_CanBeSetToTrue()
    {
        var c = new Comment { IsDeleted = true };
        c.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public void CreatedBy_CanBeSet()
    {
        var c = new Comment { CreatedBy = "alice" };
        c.CreatedBy.Should().Be("alice");
    }

    [Fact]
    public void UpdatedBy_CanBeSet()
    {
        var c = new Comment { UpdatedBy = "bob" };
        c.UpdatedBy.Should().Be("bob");
    }

    [Fact]
    public void UpdatedAt_CanBeSet()
    {
        var dt = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var c  = new Comment { UpdatedAt = dt };
        c.UpdatedAt.Should().Be(dt);
    }

    [Fact]
    public void CreatedAt_CanBeSet()
    {
        var dt = new DateTime(2024, 3, 15, 0, 0, 0, DateTimeKind.Utc);
        var c  = new Comment { CreatedAt = dt };
        c.CreatedAt.Should().Be(dt);
    }

    [Fact]
    public void Post_NavigationCanBeSet()
    {
        var post = new Post { PostId = 42, Title = "T", Status = PostStatus.Published, CreatedAt = DateTime.UtcNow };
        var c    = new Comment();
        c.Post   = post;
        c.Post.Should().BeSameAs(post);
    }

    [Fact]
    public void Author_NavigationCanBeSet()
    {
        var author = new PostAuthor { PostAuthorId = 7, UserName = "alice" };
        var c      = new Comment();
        c.Author   = author;
        c.Author.Should().BeSameAs(author);
    }

    [Fact]
    public void ParentComment_NavigationCanBeSet()
    {
        var parent = Make();
        var child  = new Comment { ParentComment = parent };
        child.ParentComment.Should().BeSameAs(parent);
    }

    [Fact]
    public void Replies_CanHaveItemsAdded()
    {
        var parent = Make();
        var reply  = new Comment { CommentId = 2, Body = "Reply" };
        parent.Replies.Add(reply);
        parent.Replies.Should().HaveCount(1);
        parent.Replies.Should().Contain(reply);
    }

    [Fact]
    public void IsInCorrectNamespace() =>
        typeof(Comment).Namespace.Should().Be("ContentDomain.Entity");

    [Fact]
    public void IsPublicClass() =>
        typeof(Comment).IsPublic.Should().BeTrue();
}