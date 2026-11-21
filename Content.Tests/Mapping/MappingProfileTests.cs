using AutoMapper;
using ContentBLL.DTO.Comment;
using ContentBLL.DTO.Like;
using ContentBLL.DTO.Post;
using ContentBLL.DTO.SavedPost;
using ContentBLL.DTO.Tag;
using ContentBLL.MappingProfiles;
using ContentDomain.Entity;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Content.Tests.Mapping;

public class MappingProfileTests
{
    // ── Configuration validity ────────────────────────────────────────────────

    [Fact]
    public void PostMappingProfiles_ConfigurationIsValid() =>
        new MapperConfiguration(cfg => cfg.AddProfile<PostMappingProfiles>(),
            LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.None)))
            .AssertConfigurationIsValid();

    [Fact]
    public void CommentMappingProfiles_ConfigurationIsValid() =>
        new MapperConfiguration(cfg => cfg.AddProfile<CommentMappingProfiles>(),
            LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.None)))
            .AssertConfigurationIsValid();

    [Fact]
    public void LikeMappingProfiles_ConfigurationIsValid() =>
        new MapperConfiguration(cfg => cfg.AddProfile<LikeMappingProfiles>(),
            LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.None)))
            .AssertConfigurationIsValid();

    [Fact]
    public void TagMappingProfiles_ConfigurationIsValid() =>
        new MapperConfiguration(cfg => cfg.AddProfile<TagMappingProfiles>(),
            LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.None)))
            .AssertConfigurationIsValid();

    [Fact]
    public void SavedPostMappingProfiles_ConfigurationIsValid() =>
        new MapperConfiguration(cfg => cfg.AddProfile<SavedPostMappingProfiles>(),
            LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.None)))
            .AssertConfigurationIsValid();

    // ── Post mappings ─────────────────────────────────────────────────────────

    [Fact]
    public void Post_MapsTo_PostDto_WithStatusString()
    {
        var mapper = BuildMapper();
        var post = new Post { PostId = 1, Title = "Test Post", Status = PostStatus.Published, CreatedBy = "alice" };

        var dto = mapper.Map<PostDto>(post);

        dto.PostId.Should().Be(1);
        dto.Title.Should().Be("Test Post");
        dto.Status.Should().Be("Published");
    }

    [Fact]
    public void Post_MapsTo_PostDto_WithArchivedStatus()
    {
        var mapper = BuildMapper();
        var post = new Post { PostId = 2, Title = "T", Status = PostStatus.Archived };

        var dto = mapper.Map<PostDto>(post);

        dto.Status.Should().Be("Archived");
    }

    [Fact]
    public void Post_MapsTo_PostDto_WithDraftStatus()
    {
        var mapper = BuildMapper();
        var post = new Post { PostId = 3, Title = "Draft", Status = PostStatus.Draft };

        var dto = mapper.Map<PostDto>(post);

        dto.Status.Should().Be("Draft");
    }

    [Fact]
    public void Post_MapsTo_PostDto_TagsFromPostTags()
    {
        var mapper = BuildMapper();
        var tag = new Tag { TagId = 1, Name = "dotnet" };
        var post = new Post
        {
            PostId = 4,
            Title  = "T",
            PostTags = new List<PostTag> { new() { Tag = tag } }
        };

        var dto = mapper.Map<PostDto>(post);

        dto.Tags.Should().ContainSingle().Which.Should().Be("dotnet");
    }

    [Fact]
    public void Post_MapsTo_PostDto_MediaUrlsFromMedia()
    {
        var mapper = BuildMapper();
        var post = new Post
        {
            PostId = 5,
            Title  = "T",
            Media  = new List<PostMedia> { new() { ObjectName = "media/img.jpg" } }
        };

        var dto = mapper.Map<PostDto>(post);

        dto.MediaUrls.Should().ContainSingle().Which.Should().Be("media/img.jpg");
    }

    [Fact]
    public void PostCreateDto_MapsTo_Post()
    {
        var mapper    = BuildMapper();
        var createDto = new PostCreateDto
        {
            Title = "New Post", Description = "Desc",
            Status = "Published"
        };

        var post = mapper.Map<Post>(createDto);

        post.Title.Should().Be("New Post");
        post.Status.Should().Be(PostStatus.Published);
        post.PostId.Should().Be(0);
    }

    [Fact]
    public void PostCreateDto_MapsTo_Post_DraftStatus()
    {
        var mapper    = BuildMapper();
        var createDto = new PostCreateDto { Title = "T", Status = "Draft" };

        var post = mapper.Map<Post>(createDto);

        post.Status.Should().Be(PostStatus.Draft);
    }

    [Fact]
    public void PostCreateDto_WithInvalidStatus_DefaultsToPublished()
    {
        var mapper    = BuildMapper();
        var createDto = new PostCreateDto { Title = "T", Status = "BadStatus" };

        var post = mapper.Map<Post>(createDto);

        post.Status.Should().Be(PostStatus.Published);
    }

    [Fact]
    public void PostCreateDto_WithNullStatus_DefaultsToPublished()
    {
        var mapper    = BuildMapper();
        var createDto = new PostCreateDto { Title = "T", Status = null };

        var post = mapper.Map<Post>(createDto);

        post.Status.Should().Be(PostStatus.Published);
    }

    [Fact]
    public void PostCreateDto_PostId_IsIgnored()
    {
        var mapper    = BuildMapper();
        var createDto = new PostCreateDto { Title = "T" };

        var post = mapper.Map<Post>(createDto);

        post.PostId.Should().Be(0);
    }

    [Fact]
    public void PostUpdateDto_MapsTo_ExistingPost_Title()
    {
        var mapper    = BuildMapper();
        var existing  = new Post { PostId = 5, Title = "Old", Status = PostStatus.Published };
        var updateDto = new PostUpdateDto { Title = "Updated", TagIds = new() };

        mapper.Map(updateDto, existing);

        existing.Title.Should().Be("Updated");
    }

    [Fact]
    public void PostUpdateDto_DoesNotOverwrite_PostId()
    {
        var mapper    = BuildMapper();
        var existing  = new Post { PostId = 5, Title = "Old", Status = PostStatus.Published };
        var updateDto = new PostUpdateDto { Title = "New", TagIds = new() };

        mapper.Map(updateDto, existing);

        existing.PostId.Should().Be(5);
    }

    [Fact]
    public void PostUpdateDto_DoesNotOverwrite_Status()
    {
        var mapper    = BuildMapper();
        var existing  = new Post { PostId = 5, Title = "Old", Status = PostStatus.Draft };
        var updateDto = new PostUpdateDto { Title = "New", TagIds = new() };

        mapper.Map(updateDto, existing);

        existing.Status.Should().Be(PostStatus.Draft);
    }

    [Fact]
    public void PostAuthor_MapsTo_AuthorDto()
    {
        var mapper = BuildMapper();
        var author = new PostAuthor { PostAuthorId = 1, UserId = 100, UserName = "alice" };

        var dto = mapper.Map<AuthorDto>(author);

        dto.PostAuthorId.Should().Be(1);
        dto.UserName.Should().Be("alice");
    }

    [Fact]
    public void PostAuthor_MapsTo_AuthorDto_AvatarUrl()
    {
        var mapper = BuildMapper();
        var author = new PostAuthor { PostAuthorId = 2, UserId = 5, UserName = "bob", AvatarUrl = "https://img.png" };

        var dto = mapper.Map<AuthorDto>(author);

        dto.AvatarUrl.Should().Be("https://img.png");
    }

    [Fact]
    public void CollaborationSnapshot_MapsTo_CollaborationDto()
    {
        var mapper  = BuildMapper();
        var collab  = new CollaborationSnapshot
        {
            CollaborationSnapshotId = 1,
            CollaborationId = 42,
            Name            = "Team A",
            ExternalId      = "ext-1"
        };

        var dto = mapper.Map<CollaborationDto>(collab);

        dto.CollaborationSnapshotId.Should().Be(1);
        dto.Name.Should().Be("Team A");
        dto.ExternalId.Should().Be("ext-1");
    }

    // ── Comment mappings ──────────────────────────────────────────────────────

    [Fact]
    public void Comment_MapsTo_CommentDto()
    {
        var mapper  = BuildMapper();
        var comment = new Comment { CommentId = 3, PostId = 1, Body = "Nice post", CreatedBy = "bob" };

        var dto = mapper.Map<CommentDto>(comment);

        dto.CommentId.Should().Be(3);
        dto.Body.Should().Be("Nice post");
    }

    [Fact]
    public void Comment_MapsTo_CommentDto_PostId()
    {
        var mapper  = BuildMapper();
        var comment = new Comment { CommentId = 1, PostId = 7, Body = "Hi" };

        var dto = mapper.Map<CommentDto>(comment);

        dto.PostId.Should().Be(7);
    }

    [Fact]
    public void CommentCreateDto_MapsTo_Comment()
    {
        var mapper    = BuildMapper();
        var createDto = new CommentCreateDto { PostId = 2, Body = "Hello" };

        var comment = mapper.Map<Comment>(createDto);

        comment.PostId.Should().Be(2);
        comment.Body.Should().Be("Hello");
    }

    [Fact]
    public void CommentCreateDto_MapsTo_Comment_CommentId_IsIgnored()
    {
        var mapper    = BuildMapper();
        var createDto = new CommentCreateDto { PostId = 2, Body = "Hi" };

        var comment = mapper.Map<Comment>(createDto);

        comment.CommentId.Should().Be(0);
    }

    [Fact]
    public void CommentUpdateDto_MapsTo_ExistingComment_Body()
    {
        var mapper    = BuildMapper();
        var existing  = new Comment { CommentId = 7, PostId = 1, Body = "Old body" };
        var updateDto = new CommentUpdateDto { Body = "New body" };

        mapper.Map(updateDto, existing);

        existing.Body.Should().Be("New body");
    }

    [Fact]
    public void CommentUpdateDto_DoesNotOverwrite_CommentId()
    {
        var mapper    = BuildMapper();
        var existing  = new Comment { CommentId = 7, PostId = 1, Body = "Old" };
        var updateDto = new CommentUpdateDto { Body = "New" };

        mapper.Map(updateDto, existing);

        existing.CommentId.Should().Be(7);
    }

    [Fact]
    public void CommentUpdateDto_DoesNotOverwrite_PostId()
    {
        var mapper    = BuildMapper();
        var existing  = new Comment { CommentId = 1, PostId = 9, Body = "Old" };
        var updateDto = new CommentUpdateDto { Body = "New" };

        mapper.Map(updateDto, existing);

        existing.PostId.Should().Be(9);
    }

    // ── Like mappings ─────────────────────────────────────────────────────────

    [Fact]
    public void Like_MapsTo_LikeDto()
    {
        var mapper = BuildMapper();
        var like   = new Like { LikeId = 1, PostId = 2, UserId = 3 };

        var dto = mapper.Map<LikeDto>(like);

        dto.LikeId.Should().Be(1);
        dto.PostId.Should().Be(2);
        dto.UserId.Should().Be(3);
    }

    [Fact]
    public void LikeCreateDto_MapsTo_Like()
    {
        var mapper    = BuildMapper();
        var createDto = new LikeCreateDto { PostId = 5 };

        var like = mapper.Map<Like>(createDto);

        like.PostId.Should().Be(5);
        like.UserId.Should().Be(0);
    }

    [Fact]
    public void LikeCreateDto_MapsTo_Like_LikeId_IsIgnored()
    {
        var mapper    = BuildMapper();
        var createDto = new LikeCreateDto { PostId = 1 };

        var like = mapper.Map<Like>(createDto);

        like.LikeId.Should().Be(0);
    }

    // ── Tag mappings ──────────────────────────────────────────────────────────

    [Fact]
    public void Tag_MapsTo_TagDto()
    {
        var mapper = BuildMapper();
        var tag    = new Tag { TagId = 1, Name = "dotnet" };

        var dto = mapper.Map<TagDto>(tag);

        dto.TagId.Should().Be(1);
        dto.Name.Should().Be("dotnet");
    }

    [Fact]
    public void TagCreateDto_MapsTo_Tag()
    {
        var mapper    = BuildMapper();
        var createDto = new TagCreateDto { Name = "azure" };

        var tag = mapper.Map<Tag>(createDto);

        tag.Name.Should().Be("azure");
    }

    [Fact]
    public void TagCreateDto_MapsTo_Tag_TagId_IsIgnored()
    {
        var mapper    = BuildMapper();
        var createDto = new TagCreateDto { Name = "test" };

        var tag = mapper.Map<Tag>(createDto);

        tag.TagId.Should().Be(0);
    }

    // ── SavedPost mappings ────────────────────────────────────────────────────

    [Fact]
    public void SavedPostCreateDto_MapsTo_SavedPost()
    {
        var mapper    = BuildMapper();
        var createDto = new SavedPostCreateDto { PostId = 3 };

        var savedPost = mapper.Map<SavedPost>(createDto);

        savedPost.PostId.Should().Be(3);
        savedPost.UserId.Should().Be(0);
    }

    [Fact]
    public void SavedPostCreateDto_MapsTo_SavedPost_SavedPostId_IsIgnored()
    {
        var mapper    = BuildMapper();
        var createDto = new SavedPostCreateDto { PostId = 1 };

        var savedPost = mapper.Map<SavedPost>(createDto);

        savedPost.SavedPostId.Should().Be(0);
    }

    [Fact]
    public void SavedPost_MapsTo_SavedPostDto_WithPostTitle()
    {
        var mapper   = BuildMapper();
        var post     = new Post { PostId = 3, Title = "The Post" };
        var savedPost = new SavedPost { SavedPostId = 1, PostId = 3, UserId = 7, Post = post };

        var dto = mapper.Map<SavedPostDto>(savedPost);

        dto.SavedPostId.Should().Be(1);
        dto.PostTitle.Should().Be("The Post");
    }

    [Fact]
    public void SavedPost_MapsTo_SavedPostDto_UserId()
    {
        var mapper    = BuildMapper();
        var post      = new Post { PostId = 1, Title = "T" };
        var savedPost = new SavedPost { SavedPostId = 2, PostId = 1, UserId = 42, Post = post };

        var dto = mapper.Map<SavedPostDto>(savedPost);

        dto.UserId.Should().Be(42);
    }

    // ── helper ────────────────────────────────────────────────────────────────

    private static IMapper BuildMapper() =>
        new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<PostMappingProfiles>();
            cfg.AddProfile<CommentMappingProfiles>();
            cfg.AddProfile<LikeMappingProfiles>();
            cfg.AddProfile<TagMappingProfiles>();
            cfg.AddProfile<SavedPostMappingProfiles>();
        }, LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.None))).CreateMapper();
}
