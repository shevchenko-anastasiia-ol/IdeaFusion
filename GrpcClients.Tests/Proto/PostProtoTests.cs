using FluentAssertions;
using Google.Protobuf;
using IdeaFusion.Grpc.Posts;
using Xunit;

namespace GrpcClients.Tests.Proto;

public class GetPostsPagedRequestTests
{
    [Fact]
    public void Default_AllFieldsAreDefaultValues()
    {
        var req = new GetPostsPagedRequest();

        req.PageNumber.Should().Be(0);
        req.PageSize.Should().Be(0);
        req.OrderBy.Should().BeEmpty();
        req.SearchText.Should().BeEmpty();
        req.Tag.Should().BeEmpty();
        req.Status.Should().BeEmpty();
    }

    [Fact]
    public void Properties_CanBeSetAndRetrieved()
    {
        var req = new GetPostsPagedRequest
        {
            PageNumber = 2,
            PageSize   = 10,
            OrderBy    = "createdAt",
            SearchText = "idea",
            Tag        = "dotnet",
            Status     = "Published"
        };

        req.PageNumber.Should().Be(2);
        req.PageSize.Should().Be(10);
        req.OrderBy.Should().Be("createdAt");
        req.SearchText.Should().Be("idea");
        req.Tag.Should().Be("dotnet");
        req.Status.Should().Be("Published");
    }

    [Fact]
    public void SerializeAndDeserialize_PreservesAllFields()
    {
        var original = new GetPostsPagedRequest
        {
            PageNumber = 3,
            PageSize   = 20,
            OrderBy    = "title",
            SearchText = "test",
            Tag        = "grpc",
            Status     = "Archived"
        };

        var deserialized = GetPostsPagedRequest.Parser.ParseFrom(original.ToByteArray());

        deserialized.PageNumber.Should().Be(3);
        deserialized.PageSize.Should().Be(20);
        deserialized.OrderBy.Should().Be("title");
        deserialized.SearchText.Should().Be("test");
        deserialized.Tag.Should().Be("grpc");
        deserialized.Status.Should().Be("Archived");
    }

    [Fact]
    public void Equality_SameValuesAreEqual()
    {
        var r1 = new GetPostsPagedRequest { PageNumber = 1, PageSize = 5, Status = "Published" };
        var r2 = new GetPostsPagedRequest { PageNumber = 1, PageSize = 5, Status = "Published" };

        r1.Should().Be(r2);
    }

    [Fact]
    public void Clone_CreatesEqualButDistinctInstance()
    {
        var original = new GetPostsPagedRequest { PageNumber = 2, PageSize = 15, OrderBy = "date" };
        var clone    = original.Clone();

        clone.Should().Be(original);
        clone.Should().NotBeSameAs(original);
    }
}

public class GetPostByIdRequestTests
{
    [Fact]
    public void Default_PostIdIsEmpty()
    {
        var req = new GetPostByIdRequest();
        req.PostId.Should().BeEmpty();
    }

    [Fact]
    public void WithPostId_ReturnsAssignedValue()
    {
        var id  = "post-123";
        var req = new GetPostByIdRequest { PostId = id };

        req.PostId.Should().Be(id);
    }

    [Fact]
    public void SerializeAndDeserialize_PreservesPostId()
    {
        var req          = new GetPostByIdRequest { PostId = "abc-456" };
        var deserialized = GetPostByIdRequest.Parser.ParseFrom(req.ToByteArray());

        deserialized.PostId.Should().Be("abc-456");
    }

    [Fact]
    public void Equality_SamePostIdIsEqual()
    {
        var r1 = new GetPostByIdRequest { PostId = "post-1" };
        var r2 = new GetPostByIdRequest { PostId = "post-1" };

        r1.Should().Be(r2);
    }

    [Fact]
    public void Equality_DifferentPostIdsAreNotEqual()
    {
        var r1 = new GetPostByIdRequest { PostId = "post-1" };
        var r2 = new GetPostByIdRequest { PostId = "post-2" };

        r1.Should().NotBe(r2);
    }

    [Fact]
    public void Clone_CreatesEqualButDistinctInstance()
    {
        var original = new GetPostByIdRequest { PostId = "post-xyz" };
        var clone    = original.Clone();

        clone.Should().Be(original);
        clone.Should().NotBeSameAs(original);
    }
}

public class GetPostsByAuthorRequestTests
{
    [Fact]
    public void Default_AllFieldsAreDefaultValues()
    {
        var req = new GetPostsByAuthorRequest();

        req.UserId.Should().BeEmpty();
        req.PageNumber.Should().Be(0);
        req.PageSize.Should().Be(0);
        req.Status.Should().BeEmpty();
    }

    [Fact]
    public void Properties_CanBeSetAndRetrieved()
    {
        var req = new GetPostsByAuthorRequest
        {
            UserId     = "user-1",
            PageNumber = 1,
            PageSize   = 10,
            Status     = "Published"
        };

        req.UserId.Should().Be("user-1");
        req.PageNumber.Should().Be(1);
        req.PageSize.Should().Be(10);
        req.Status.Should().Be("Published");
    }

    [Fact]
    public void SerializeAndDeserialize_PreservesAllFields()
    {
        var original = new GetPostsByAuthorRequest
        {
            UserId     = "user-99",
            PageNumber = 2,
            PageSize   = 25,
            Status     = "Archived"
        };

        var deserialized = GetPostsByAuthorRequest.Parser.ParseFrom(original.ToByteArray());

        deserialized.UserId.Should().Be("user-99");
        deserialized.PageNumber.Should().Be(2);
        deserialized.PageSize.Should().Be(25);
        deserialized.Status.Should().Be("Archived");
    }

    [Fact]
    public void Equality_SameValuesAreEqual()
    {
        var r1 = new GetPostsByAuthorRequest { UserId = "u-1", PageNumber = 1 };
        var r2 = new GetPostsByAuthorRequest { UserId = "u-1", PageNumber = 1 };

        r1.Should().Be(r2);
    }

    [Fact]
    public void Clone_CreatesEqualButDistinctInstance()
    {
        var original = new GetPostsByAuthorRequest { UserId = "u-1", PageSize = 5 };
        var clone    = original.Clone();

        clone.Should().Be(original);
        clone.Should().NotBeSameAs(original);
    }
}

public class GetPostsByTeamRequestTests
{
    [Fact]
    public void Default_AllFieldsAreDefaultValues()
    {
        var req = new GetPostsByTeamRequest();

        req.TeamId.Should().BeEmpty();
        req.PageNumber.Should().Be(0);
        req.PageSize.Should().Be(0);
    }

    [Fact]
    public void Properties_CanBeSetAndRetrieved()
    {
        var req = new GetPostsByTeamRequest
        {
            TeamId     = "team-1",
            PageNumber = 1,
            PageSize   = 15
        };

        req.TeamId.Should().Be("team-1");
        req.PageNumber.Should().Be(1);
        req.PageSize.Should().Be(15);
    }

    [Fact]
    public void SerializeAndDeserialize_PreservesAllFields()
    {
        var original = new GetPostsByTeamRequest
        {
            TeamId     = "team-abc",
            PageNumber = 3,
            PageSize   = 10
        };

        var deserialized = GetPostsByTeamRequest.Parser.ParseFrom(original.ToByteArray());

        deserialized.TeamId.Should().Be("team-abc");
        deserialized.PageNumber.Should().Be(3);
        deserialized.PageSize.Should().Be(10);
    }

    [Fact]
    public void Equality_SameValuesAreEqual()
    {
        var r1 = new GetPostsByTeamRequest { TeamId = "t-1", PageSize = 10 };
        var r2 = new GetPostsByTeamRequest { TeamId = "t-1", PageSize = 10 };

        r1.Should().Be(r2);
    }

    [Fact]
    public void Clone_CreatesEqualButDistinctInstance()
    {
        var original = new GetPostsByTeamRequest { TeamId = "t-1", PageNumber = 2 };
        var clone    = original.Clone();

        clone.Should().Be(original);
        clone.Should().NotBeSameAs(original);
    }
}

public class MediaDtoTests
{
    [Fact]
    public void Default_AllFieldsAreEmpty()
    {
        var dto = new MediaDto();

        dto.ObjectName.Should().BeEmpty();
        dto.Bucket.Should().BeEmpty();
        dto.ContentType.Should().BeEmpty();
    }

    [Fact]
    public void Properties_CanBeSetAndRetrieved()
    {
        var dto = new MediaDto
        {
            ObjectName  = "photo.jpg",
            Bucket      = "images",
            ContentType = "image/jpeg"
        };

        dto.ObjectName.Should().Be("photo.jpg");
        dto.Bucket.Should().Be("images");
        dto.ContentType.Should().Be("image/jpeg");
    }

    [Fact]
    public void SerializeAndDeserialize_PreservesAllFields()
    {
        var original = new MediaDto
        {
            ObjectName  = "video.mp4",
            Bucket      = "videos",
            ContentType = "video/mp4"
        };

        var deserialized = MediaDto.Parser.ParseFrom(original.ToByteArray());

        deserialized.ObjectName.Should().Be("video.mp4");
        deserialized.Bucket.Should().Be("videos");
        deserialized.ContentType.Should().Be("video/mp4");
    }

    [Fact]
    public void Equality_SameValuesAreEqual()
    {
        var d1 = new MediaDto { ObjectName = "img.png", Bucket = "b", ContentType = "image/png" };
        var d2 = new MediaDto { ObjectName = "img.png", Bucket = "b", ContentType = "image/png" };

        d1.Should().Be(d2);
    }

    [Fact]
    public void Clone_CreatesEqualButDistinctInstance()
    {
        var original = new MediaDto { ObjectName = "x.jpg", Bucket = "bkt", ContentType = "image/jpeg" };
        var clone    = original.Clone();

        clone.Should().Be(original);
        clone.Should().NotBeSameAs(original);
    }
}

public class PostAuthorDtoTests
{
    [Fact]
    public void Default_AllFieldsAreEmpty()
    {
        var dto = new PostAuthorDto();

        dto.UserId.Should().BeEmpty();
        dto.UserName.Should().BeEmpty();
        dto.AvatarUrl.Should().BeEmpty();
    }

    [Fact]
    public void Properties_CanBeSetAndRetrieved()
    {
        var dto = new PostAuthorDto
        {
            UserId    = "user-1",
            UserName  = "alice",
            AvatarUrl = "https://example.com/alice.jpg"
        };

        dto.UserId.Should().Be("user-1");
        dto.UserName.Should().Be("alice");
        dto.AvatarUrl.Should().Be("https://example.com/alice.jpg");
    }

    [Fact]
    public void SerializeAndDeserialize_PreservesAllFields()
    {
        var original = new PostAuthorDto
        {
            UserId    = "u-42",
            UserName  = "bob",
            AvatarUrl = "https://cdn.example.com/bob.png"
        };

        var deserialized = PostAuthorDto.Parser.ParseFrom(original.ToByteArray());

        deserialized.UserId.Should().Be("u-42");
        deserialized.UserName.Should().Be("bob");
        deserialized.AvatarUrl.Should().Be("https://cdn.example.com/bob.png");
    }

    [Fact]
    public void Equality_SameValuesAreEqual()
    {
        var a1 = new PostAuthorDto { UserId = "u-1", UserName = "carol" };
        var a2 = new PostAuthorDto { UserId = "u-1", UserName = "carol" };

        a1.Should().Be(a2);
    }

    [Fact]
    public void Clone_CreatesEqualButDistinctInstance()
    {
        var original = new PostAuthorDto { UserId = "u-1", UserName = "dave", AvatarUrl = "url" };
        var clone    = original.Clone();

        clone.Should().Be(original);
        clone.Should().NotBeSameAs(original);
    }
}

public class PostTeamDtoTests
{
    [Fact]
    public void Default_AllFieldsAreEmpty()
    {
        var dto = new PostTeamDto();

        dto.TeamId.Should().BeEmpty();
        dto.Name.Should().BeEmpty();
        dto.AvatarUrl.Should().BeEmpty();
    }

    [Fact]
    public void Properties_CanBeSetAndRetrieved()
    {
        var dto = new PostTeamDto
        {
            TeamId    = "team-1",
            Name      = "Dev Team",
            AvatarUrl = "https://example.com/team.png"
        };

        dto.TeamId.Should().Be("team-1");
        dto.Name.Should().Be("Dev Team");
        dto.AvatarUrl.Should().Be("https://example.com/team.png");
    }

    [Fact]
    public void SerializeAndDeserialize_PreservesAllFields()
    {
        var original = new PostTeamDto
        {
            TeamId    = "t-99",
            Name      = "Alpha",
            AvatarUrl = "https://cdn.example.com/alpha.png"
        };

        var deserialized = PostTeamDto.Parser.ParseFrom(original.ToByteArray());

        deserialized.TeamId.Should().Be("t-99");
        deserialized.Name.Should().Be("Alpha");
        deserialized.AvatarUrl.Should().Be("https://cdn.example.com/alpha.png");
    }

    [Fact]
    public void Equality_SameValuesAreEqual()
    {
        var t1 = new PostTeamDto { TeamId = "t-1", Name = "Alpha" };
        var t2 = new PostTeamDto { TeamId = "t-1", Name = "Alpha" };

        t1.Should().Be(t2);
    }

    [Fact]
    public void Clone_CreatesEqualButDistinctInstance()
    {
        var original = new PostTeamDto { TeamId = "t-1", Name = "Beta", AvatarUrl = "url" };
        var clone    = original.Clone();

        clone.Should().Be(original);
        clone.Should().NotBeSameAs(original);
    }
}

public class PostDtoTests
{
    [Fact]
    public void Default_StringFieldsAreEmpty()
    {
        var dto = new PostDto();

        dto.PostId.Should().BeEmpty();
        dto.Title.Should().BeEmpty();
        dto.Description.Should().BeEmpty();
        dto.ExternalLink.Should().BeEmpty();
        dto.Status.Should().BeEmpty();
        dto.CreatedAt.Should().BeEmpty();
        dto.UpdatedAt.Should().BeEmpty();
    }

    [Fact]
    public void Default_IntFieldsAreZero()
    {
        var dto = new PostDto();

        dto.LikesCount.Should().Be(0);
        dto.ViewsCount.Should().Be(0);
        dto.CommentsCount.Should().Be(0);
    }

    [Fact]
    public void Default_AuthorAndTeamAreNull()
    {
        var dto = new PostDto();

        dto.Author.Should().BeNull();
        dto.Team.Should().BeNull();
    }

    [Fact]
    public void Default_TagsAndMediaAreEmpty()
    {
        var dto = new PostDto();

        dto.Tags.Should().BeEmpty();
        dto.Media.Should().BeEmpty();
    }

    [Fact]
    public void Properties_CanBeSetAndRetrieved()
    {
        var author = new PostAuthorDto { UserId = "u-1", UserName = "alice" };
        var media  = new MediaDto { ObjectName = "img.jpg", Bucket = "b", ContentType = "image/jpeg" };

        var dto = new PostDto
        {
            PostId       = "post-1",
            Title        = "Hello World",
            Description  = "A test post",
            ExternalLink = "https://example.com",
            Status       = "Published",
            Author       = author,
            LikesCount   = 5,
            ViewsCount   = 100,
            CommentsCount = 3,
            CreatedAt    = "2024-01-01T00:00:00Z",
            UpdatedAt    = "2024-06-01T00:00:00Z"
        };
        dto.Tags.Add("dotnet");
        dto.Tags.Add("grpc");
        dto.Media.Add(media);

        dto.PostId.Should().Be("post-1");
        dto.Title.Should().Be("Hello World");
        dto.Description.Should().Be("A test post");
        dto.ExternalLink.Should().Be("https://example.com");
        dto.Status.Should().Be("Published");
        dto.Author.Should().Be(author);
        dto.LikesCount.Should().Be(5);
        dto.ViewsCount.Should().Be(100);
        dto.CommentsCount.Should().Be(3);
        dto.Tags.Should().HaveCount(2);
        dto.Media.Should().HaveCount(1);
    }

    [Fact]
    public void SerializeAndDeserialize_PreservesAllFields()
    {
        var original = new PostDto
        {
            PostId      = "p-42",
            Title       = "Test",
            Description = "Desc",
            Status      = "Published",
            Author      = new PostAuthorDto { UserId = "u-1", UserName = "alice" },
            LikesCount  = 7,
            CreatedAt   = "2024-03-15T08:00:00Z"
        };
        original.Tags.Add("tag1");
        original.Media.Add(new MediaDto { ObjectName = "a.jpg", Bucket = "bkt", ContentType = "image/jpeg" });

        var deserialized = PostDto.Parser.ParseFrom(original.ToByteArray());

        deserialized.PostId.Should().Be("p-42");
        deserialized.Title.Should().Be("Test");
        deserialized.Status.Should().Be("Published");
        deserialized.Author.Should().NotBeNull();
        deserialized.Author!.UserId.Should().Be("u-1");
        deserialized.LikesCount.Should().Be(7);
        deserialized.Tags.Should().ContainSingle().Which.Should().Be("tag1");
        deserialized.Media.Should().HaveCount(1);
    }

    [Fact]
    public void WithTeam_TeamIsSet()
    {
        var dto = new PostDto
        {
            Team = new PostTeamDto { TeamId = "t-1", Name = "Alpha" }
        };

        dto.Team.Should().NotBeNull();
        dto.Team!.TeamId.Should().Be("t-1");
        dto.Author.Should().BeNull();
    }

    [Fact]
    public void Equality_SameValuesAreEqual()
    {
        var d1 = new PostDto { PostId = "p-1", Title = "T", Status = "Published" };
        var d2 = new PostDto { PostId = "p-1", Title = "T", Status = "Published" };

        d1.Should().Be(d2);
    }

    [Fact]
    public void Clone_CreatesEqualButDistinctInstance()
    {
        var original = new PostDto { PostId = "p-1", Title = "Clone test" };
        original.Tags.Add("x");

        var clone = original.Clone();

        clone.Should().Be(original);
        clone.Should().NotBeSameAs(original);
    }
}

public class PostResponseTests
{
    [Fact]
    public void Default_PostIsNull()
    {
        var response = new PostResponse();
        response.Post.Should().BeNull();
    }

    [Fact]
    public void WithPost_ReturnsAssignedDto()
    {
        var dto      = new PostDto { PostId = "p-1", Title = "Hello" };
        var response = new PostResponse { Post = dto };

        response.Post.Should().NotBeNull();
        response.Post!.PostId.Should().Be("p-1");
        response.Post.Title.Should().Be("Hello");
    }

    [Fact]
    public void SerializeAndDeserialize_PreservesNestedDto()
    {
        var response = new PostResponse
        {
            Post = new PostDto { PostId = "p-xyz", Title = "Test", Status = "Published" }
        };

        var deserialized = PostResponse.Parser.ParseFrom(response.ToByteArray());

        deserialized.Post.Should().NotBeNull();
        deserialized.Post!.PostId.Should().Be("p-xyz");
        deserialized.Post.Title.Should().Be("Test");
        deserialized.Post.Status.Should().Be("Published");
    }

    [Fact]
    public void Equality_SamePostIsEqual()
    {
        var dto = new PostDto { PostId = "p-1" };
        var r1  = new PostResponse { Post = dto };
        var r2  = new PostResponse { Post = dto.Clone() };

        r1.Should().Be(r2);
    }

    [Fact]
    public void Clone_CreatesEqualButDistinctInstance()
    {
        var original = new PostResponse { Post = new PostDto { PostId = "p-1", Status = "Draft" } };
        var clone    = original.Clone();

        clone.Should().Be(original);
        clone.Should().NotBeSameAs(original);
    }
}

public class PagedPostsResponseTests
{
    [Fact]
    public void Default_ItemsIsEmptyAndCountersAreZero()
    {
        var response = new PagedPostsResponse();

        response.Items.Should().BeEmpty();
        response.TotalCount.Should().Be(0);
        response.PageNumber.Should().Be(0);
        response.PageSize.Should().Be(0);
    }

    [Fact]
    public void AddItems_CountMatchesAddedItems()
    {
        var response = new PagedPostsResponse { TotalCount = 2, PageNumber = 1, PageSize = 10 };
        response.Items.Add(new PostDto { PostId = "p-1" });
        response.Items.Add(new PostDto { PostId = "p-2" });

        response.Items.Should().HaveCount(2);
        response.TotalCount.Should().Be(2);
    }

    [Fact]
    public void AddRange_PreservesInsertionOrder()
    {
        var items = new[]
        {
            new PostDto { PostId = "p-1" },
            new PostDto { PostId = "p-2" },
            new PostDto { PostId = "p-3" }
        };

        var response = new PagedPostsResponse();
        response.Items.AddRange(items);

        response.Items[0].PostId.Should().Be("p-1");
        response.Items[1].PostId.Should().Be("p-2");
        response.Items[2].PostId.Should().Be("p-3");
    }

    [Fact]
    public void SerializeAndDeserialize_PreservesAllFields()
    {
        var response = new PagedPostsResponse { TotalCount = 50, PageNumber = 2, PageSize = 10 };
        response.Items.Add(new PostDto { PostId = "p-1", Title = "First" });
        response.Items.Add(new PostDto { PostId = "p-2", Title = "Second" });

        var deserialized = PagedPostsResponse.Parser.ParseFrom(response.ToByteArray());

        deserialized.Items.Should().HaveCount(2);
        deserialized.Items[0].PostId.Should().Be("p-1");
        deserialized.Items[0].Title.Should().Be("First");
        deserialized.Items[1].PostId.Should().Be("p-2");
        deserialized.TotalCount.Should().Be(50);
        deserialized.PageNumber.Should().Be(2);
        deserialized.PageSize.Should().Be(10);
    }

    [Fact]
    public void SerializeAndDeserialize_EmptyResponse_RemainsEmpty()
    {
        var response     = new PagedPostsResponse();
        var deserialized = PagedPostsResponse.Parser.ParseFrom(response.ToByteArray());

        deserialized.Items.Should().BeEmpty();
    }

    [Fact]
    public void Clone_CreatesEqualButDistinctInstance()
    {
        var response = new PagedPostsResponse { TotalCount = 5, PageNumber = 1, PageSize = 5 };
        response.Items.Add(new PostDto { PostId = "p-1" });

        var clone = response.Clone();

        clone.Should().Be(response);
        clone.Should().NotBeSameAs(response);
    }
}

public class PostReflectionTests
{
    [Fact]
    public void Descriptor_IsNotNull()
    {
        PostReflection.Descriptor.Should().NotBeNull();
    }

    [Fact]
    public void Descriptor_ContainsAllExpectedMessageTypes()
    {
        var messageNames = PostReflection.Descriptor.MessageTypes
            .Select(m => m.Name)
            .ToList();

        messageNames.Should().Contain("GetPostsPagedRequest");
        messageNames.Should().Contain("GetPostByIdRequest");
        messageNames.Should().Contain("GetPostsByAuthorRequest");
        messageNames.Should().Contain("GetPostsByTeamRequest");
        messageNames.Should().Contain("PostResponse");
        messageNames.Should().Contain("PagedPostsResponse");
        messageNames.Should().Contain("PostDto");
        messageNames.Should().Contain("PostAuthorDto");
        messageNames.Should().Contain("PostTeamDto");
        messageNames.Should().Contain("MediaDto");
    }

    [Fact]
    public void Descriptor_HasTenMessageTypes()
    {
        PostReflection.Descriptor.MessageTypes.Should().HaveCount(10);
    }

    [Fact]
    public void Descriptor_ContainsExpectedService()
    {
        var serviceNames = PostReflection.Descriptor.Services
            .Select(s => s.Name)
            .ToList();

        serviceNames.Should().Contain("PostGrpcService");
    }

    [Fact]
    public void Descriptor_PostDtoHasFourteenFields()
    {
        var postDtoDescriptor = PostReflection.Descriptor.MessageTypes
            .Single(m => m.Name == "PostDto");

        postDtoDescriptor.Fields.InFieldNumberOrder().Should().HaveCount(14);
    }

    [Fact]
    public void Descriptor_PostDtoHasExpectedFieldNames()
    {
        var postDtoDescriptor = PostReflection.Descriptor.MessageTypes
            .Single(m => m.Name == "PostDto");

        var fieldNames = postDtoDescriptor.Fields.InFieldNumberOrder()
            .Select(f => f.Name)
            .ToList();

        fieldNames.Should().Contain("post_id");
        fieldNames.Should().Contain("title");
        fieldNames.Should().Contain("description");
        fieldNames.Should().Contain("external_link");
        fieldNames.Should().Contain("status");
        fieldNames.Should().Contain("author");
        fieldNames.Should().Contain("team");
        fieldNames.Should().Contain("tags");
        fieldNames.Should().Contain("media");
        fieldNames.Should().Contain("likes_count");
        fieldNames.Should().Contain("views_count");
        fieldNames.Should().Contain("comments_count");
        fieldNames.Should().Contain("created_at");
        fieldNames.Should().Contain("updated_at");
    }
}

public class PostGrpcServiceDescriptorTests
{
    [Fact]
    public void Descriptor_IsNotNull()
    {
        PostGrpcService.Descriptor.Should().NotBeNull();
    }

    [Fact]
    public void Descriptor_HasExactlyFourMethods()
    {
        PostGrpcService.Descriptor.Methods.Should().HaveCount(4);
    }

    [Fact]
    public void Descriptor_ContainsAllExpectedMethodNames()
    {
        var methodNames = PostGrpcService.Descriptor.Methods
            .Select(m => m.Name)
            .ToList();

        methodNames.Should().Contain("GetPostsPaged");
        methodNames.Should().Contain("GetPostById");
        methodNames.Should().Contain("GetPostsByAuthor");
        methodNames.Should().Contain("GetPostsByTeam");
    }

    [Fact]
    public void Descriptor_FullNameIncludesPackageName()
    {
        PostGrpcService.Descriptor.FullName
            .Should().Be("posts.PostGrpcService");
    }

    [Fact]
    public void Descriptor_AllMethodsAreUnary()
    {
        foreach (var method in PostGrpcService.Descriptor.Methods)
        {
            method.IsClientStreaming.Should().BeFalse();
            method.IsServerStreaming.Should().BeFalse();
        }
    }

    [Fact]
    public void Descriptor_GetPostsPaged_HasCorrectInputAndOutputTypes()
    {
        var method = PostGrpcService.Descriptor.Methods
            .Single(m => m.Name == "GetPostsPaged");

        method.InputType.Name.Should().Be("GetPostsPagedRequest");
        method.OutputType.Name.Should().Be("PagedPostsResponse");
    }

    [Fact]
    public void Descriptor_GetPostById_HasCorrectInputAndOutputTypes()
    {
        var method = PostGrpcService.Descriptor.Methods
            .Single(m => m.Name == "GetPostById");

        method.InputType.Name.Should().Be("GetPostByIdRequest");
        method.OutputType.Name.Should().Be("PostResponse");
    }

    [Fact]
    public void Descriptor_GetPostsByAuthor_HasCorrectInputAndOutputTypes()
    {
        var method = PostGrpcService.Descriptor.Methods
            .Single(m => m.Name == "GetPostsByAuthor");

        method.InputType.Name.Should().Be("GetPostsByAuthorRequest");
        method.OutputType.Name.Should().Be("PagedPostsResponse");
    }

    [Fact]
    public void Descriptor_GetPostsByTeam_HasCorrectInputAndOutputTypes()
    {
        var method = PostGrpcService.Descriptor.Methods
            .Single(m => m.Name == "GetPostsByTeam");

        method.InputType.Name.Should().Be("GetPostsByTeamRequest");
        method.OutputType.Name.Should().Be("PagedPostsResponse");
    }
}
