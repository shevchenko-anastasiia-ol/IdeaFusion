using FluentAssertions;
using Google.Protobuf;
using Grpc.Core;
using IdeaFusion.Grpc.CollaborationRequests;
using IdeaFusion.Grpc.Posts;
using System.Buffers;
using System.Reflection;
using Xunit;

namespace GrpcClients.Tests.Proto;

// ═══════════════════════════════════════════════════════════════════════════
// GetPostByIdRequest
// ═══════════════════════════════════════════════════════════════════════════

public class GetPostByIdRequestCoverageTests
{
    // Cross-type: PostAuthorDto has fields 1(UserId=str), 2(UserName=str), 3(AvatarUrl=str).
    // Parsing as GetPostByIdRequest (field 1=PostId only) → fields 2/3 become _unknownFields.
    private static GetPostByIdRequest WithUnknown() =>
        GetPostByIdRequest.Parser.ParseFrom(
            new PostAuthorDto { UserId = "p-1", UserName = "alice" }.ToByteArray());

    [Fact] public void Equals_WithDifferentType_ReturnsFalse() =>
        new GetPostByIdRequest { PostId = "p-1" }.Equals(42).Should().BeFalse();

    [Fact] public void Equals_SameReference_ReturnsTrue()
    {
        var req = new GetPostByIdRequest { PostId = "p-1" };
        req.Equals((object)req).Should().BeTrue();
    }

    [Fact] public void Equals_Null_ReturnsFalse() =>
        new GetPostByIdRequest().Equals((GetPostByIdRequest?)null).Should().BeFalse();

    [Fact] public void GetHashCode_EmptyMessage_ReturnsDeterministicValue() =>
        new GetPostByIdRequest().GetHashCode().Should().Be(new GetPostByIdRequest().GetHashCode());

    [Fact] public void GetHashCode_NonEmpty_DifferentFromEmpty() =>
        new GetPostByIdRequest { PostId = "p-1" }.GetHashCode()
            .Should().NotBe(new GetPostByIdRequest().GetHashCode());

    [Fact] public void GetHashCode_WithUnknownFields_Branches() =>
        WithUnknown().GetHashCode().Should().NotBe(0);

    [Fact] public void ToString_ReturnsNonNullString() =>
        new GetPostByIdRequest { PostId = "p-1" }.ToString().Should().NotBeNull();

    [Fact] public void CalculateSize_EmptyMessage_ReturnsZero() =>
        new GetPostByIdRequest().CalculateSize().Should().Be(0);

    [Fact] public void CalculateSize_NonEmpty_ReturnsPositive() =>
        new GetPostByIdRequest { PostId = "p-1" }.CalculateSize().Should().BeGreaterThan(0);

    [Fact] public void CalculateSize_WithUnknownFields_ReturnsPositive() =>
        WithUnknown().CalculateSize().Should().BeGreaterThan(0);

    [Fact] public void WriteTo_EmptyMessage_WritesZeroBytes() =>
        new GetPostByIdRequest().ToByteArray().Should().BeEmpty();

    [Fact] public void WriteTo_NonEmpty_WritesBytes() =>
        new GetPostByIdRequest { PostId = "p-1" }.ToByteArray().Should().NotBeEmpty();

    [Fact] public void WriteTo_WithUnknownFields_WritesBytes() =>
        WithUnknown().ToByteArray().Should().NotBeEmpty();

    [Fact] public void MergeFrom_Null_DoesNotThrow()
    {
        var r = new GetPostByIdRequest { PostId = "p-1" };
        r.MergeFrom((GetPostByIdRequest?)null!);
        r.PostId.Should().Be("p-1");
    }

    [Fact] public void MergeFrom_EmptySource_DoesNotOverwrite()
    {
        var r = new GetPostByIdRequest { PostId = "p-1" };
        r.MergeFrom(new GetPostByIdRequest());
        r.PostId.Should().Be("p-1");
    }

    [Fact] public void MergeFrom_NonEmptySource_CopiesField()
    {
        var r = new GetPostByIdRequest();
        r.MergeFrom(new GetPostByIdRequest { PostId = "p-2" });
        r.PostId.Should().Be("p-2");
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// GetPostsPagedRequest
// ═══════════════════════════════════════════════════════════════════════════

public class GetPostsPagedRequestCoverageTests
{
    private static GetPostsPagedRequest Full() => new()
    {
        PageNumber = 2,
        PageSize   = 10,
        OrderBy    = "newest",
        SearchText = "idea",
        Tag        = "dotnet",
        Status     = "Published"
    };

    // Append bytes for field 7 (string, tag = (7<<3)|2 = 58, length 1, 'A' = 65)
    private static GetPostsPagedRequest WithUnknown()
    {
        var bytes = Full().ToByteArray().Concat(new byte[] { 58, 1, 65 }).ToArray();
        return GetPostsPagedRequest.Parser.ParseFrom(bytes);
    }

    [Fact] public void Equals_WithDifferentType_ReturnsFalse() =>
        Full().Equals("str").Should().BeFalse();

    [Fact] public void Equals_SameReference_ReturnsTrue()
    {
        var r = Full();
        r.Equals((object)r).Should().BeTrue();
    }

    [Fact] public void Equals_Null_ReturnsFalse() =>
        new GetPostsPagedRequest().Equals((GetPostsPagedRequest?)null).Should().BeFalse();

    [Fact] public void GetHashCode_EmptyMessage_ReturnsDeterministicValue() =>
        new GetPostsPagedRequest().GetHashCode().Should().Be(new GetPostsPagedRequest().GetHashCode());

    [Fact] public void GetHashCode_FullMessage_DifferentFromEmpty() =>
        Full().GetHashCode().Should().NotBe(new GetPostsPagedRequest().GetHashCode());

    [Fact] public void GetHashCode_WithUnknownFields_Branches() =>
        WithUnknown().GetHashCode().Should().NotBe(0);

    [Fact] public void ToString_ReturnsNonNullString() =>
        Full().ToString().Should().NotBeNull();

    [Fact] public void CalculateSize_EmptyMessage_ReturnsZero() =>
        new GetPostsPagedRequest().CalculateSize().Should().Be(0);

    [Fact] public void CalculateSize_PageNumber_NonZeroBranchCovered() =>
        new GetPostsPagedRequest { PageNumber = 3 }.CalculateSize().Should().BeGreaterThan(0);

    [Fact] public void CalculateSize_PageSize_NonZeroBranchCovered() =>
        new GetPostsPagedRequest { PageSize = 10 }.CalculateSize().Should().BeGreaterThan(0);

    [Fact] public void CalculateSize_FullMessage_ReturnsPositive() =>
        Full().CalculateSize().Should().BeGreaterThan(0);

    [Fact] public void CalculateSize_WithUnknownFields_ReturnsPositive() =>
        WithUnknown().CalculateSize().Should().BeGreaterThan(0);

    [Fact] public void WriteTo_EmptyMessage_WritesZeroBytes() =>
        new GetPostsPagedRequest().ToByteArray().Should().BeEmpty();

    [Fact] public void WriteTo_FullMessage_WritesBytes() =>
        Full().ToByteArray().Should().NotBeEmpty();

    [Fact] public void MergeFrom_Null_DoesNotThrow()
    {
        var r = Full();
        r.MergeFrom((GetPostsPagedRequest?)null!);
        r.PageNumber.Should().Be(2);
    }

    [Fact] public void MergeFrom_EmptySource_DoesNotOverwrite()
    {
        var r = Full();
        r.MergeFrom(new GetPostsPagedRequest());
        r.OrderBy.Should().Be("newest");
        r.PageNumber.Should().Be(2);
    }

    [Fact] public void MergeFrom_NonEmptySource_CopiesAllFields()
    {
        var r = new GetPostsPagedRequest();
        r.MergeFrom(Full());
        r.PageNumber.Should().Be(2);
        r.PageSize.Should().Be(10);
        r.OrderBy.Should().Be("newest");
        r.SearchText.Should().Be("idea");
        r.Tag.Should().Be("dotnet");
        r.Status.Should().Be("Published");
    }

    // WriteTo with unknown fields (lines 317-318)
    [Fact] public void WriteTo_WithUnknownFields_WritesBytes() =>
        WithUnknown().ToByteArray().Should().NotBeEmpty();

    // Equals early-return branches (lines 221–226)
    [Fact] public void Equals_DifferentPageNumber_ReturnsFalse() =>
        new GetPostsPagedRequest { PageNumber = 1 }
            .Equals(new GetPostsPagedRequest { PageNumber = 2 }).Should().BeFalse();

    [Fact] public void Equals_DifferentPageSize_ReturnsFalse() =>
        new GetPostsPagedRequest { PageSize = 5 }
            .Equals(new GetPostsPagedRequest { PageSize = 10 }).Should().BeFalse();

    [Fact] public void Equals_DifferentOrderBy_ReturnsFalse() =>
        new GetPostsPagedRequest { OrderBy = "newest" }
            .Equals(new GetPostsPagedRequest { OrderBy = "oldest" }).Should().BeFalse();

    [Fact] public void Equals_DifferentSearchText_ReturnsFalse() =>
        new GetPostsPagedRequest { SearchText = "idea" }
            .Equals(new GetPostsPagedRequest { SearchText = "code" }).Should().BeFalse();

    [Fact] public void Equals_DifferentTag_ReturnsFalse() =>
        new GetPostsPagedRequest { Tag = "dotnet" }
            .Equals(new GetPostsPagedRequest { Tag = "java" }).Should().BeFalse();

    [Fact] public void Equals_DifferentStatus_ReturnsFalse() =>
        new GetPostsPagedRequest { Status = "Published" }
            .Equals(new GetPostsPagedRequest { Status = "Draft" }).Should().BeFalse();
}

// ═══════════════════════════════════════════════════════════════════════════
// GetPostsByAuthorRequest
// ═══════════════════════════════════════════════════════════════════════════

public class GetPostsByAuthorRequestCoverageTests
{
    private static GetPostsByAuthorRequest Full() => new()
    {
        UserId = "u-1", PageNumber = 2, PageSize = 10, Status = "Published"
    };

    // Append field 5 (tag=(5<<3)|2=42, length=1, 'A'=65) → _unknownFields
    private static GetPostsByAuthorRequest WithUnknown()
    {
        var bytes = Full().ToByteArray().Concat(new byte[] { 42, 1, 65 }).ToArray();
        return GetPostsByAuthorRequest.Parser.ParseFrom(bytes);
    }

    [Fact] public void Equals_WithDifferentType_ReturnsFalse() =>
        Full().Equals(true).Should().BeFalse();

    [Fact] public void Equals_SameReference_ReturnsTrue()
    {
        var r = Full();
        r.Equals((object)r).Should().BeTrue();
    }

    [Fact] public void Equals_Null_ReturnsFalse() =>
        new GetPostsByAuthorRequest().Equals((GetPostsByAuthorRequest?)null).Should().BeFalse();

    [Fact] public void GetHashCode_EmptyMessage_ReturnsDeterministicValue() =>
        new GetPostsByAuthorRequest().GetHashCode().Should().Be(new GetPostsByAuthorRequest().GetHashCode());

    [Fact] public void GetHashCode_FullMessage_DifferentFromEmpty() =>
        Full().GetHashCode().Should().NotBe(new GetPostsByAuthorRequest().GetHashCode());

    [Fact] public void GetHashCode_WithUnknownFields_Branches() =>
        WithUnknown().GetHashCode().Should().NotBe(0);

    [Fact] public void ToString_ReturnsNonNullString() =>
        Full().ToString().Should().NotBeNull();

    [Fact] public void CalculateSize_EmptyMessage_ReturnsZero() =>
        new GetPostsByAuthorRequest().CalculateSize().Should().Be(0);

    [Fact] public void CalculateSize_PageNumber_NonZeroBranch() =>
        new GetPostsByAuthorRequest { PageNumber = 5 }.CalculateSize().Should().BeGreaterThan(0);

    [Fact] public void CalculateSize_PageSize_NonZeroBranch() =>
        new GetPostsByAuthorRequest { PageSize = 20 }.CalculateSize().Should().BeGreaterThan(0);

    [Fact] public void CalculateSize_FullMessage_ReturnsPositive() =>
        Full().CalculateSize().Should().BeGreaterThan(0);

    [Fact] public void CalculateSize_WithUnknownFields_ReturnsPositive() =>
        WithUnknown().CalculateSize().Should().BeGreaterThan(0);

    [Fact] public void WriteTo_EmptyMessage_WritesZeroBytes() =>
        new GetPostsByAuthorRequest().ToByteArray().Should().BeEmpty();

    [Fact] public void WriteTo_FullMessage_WritesBytes() =>
        Full().ToByteArray().Should().NotBeEmpty();

    [Fact] public void WriteTo_WithUnknownFields_WritesBytes() =>
        WithUnknown().ToByteArray().Should().NotBeEmpty();

    [Fact] public void MergeFrom_Null_DoesNotThrow()
    {
        var r = Full();
        r.MergeFrom((GetPostsByAuthorRequest?)null!);
        r.UserId.Should().Be("u-1");
    }

    [Fact] public void MergeFrom_EmptySource_DoesNotOverwrite()
    {
        var r = Full();
        r.MergeFrom(new GetPostsByAuthorRequest());
        r.UserId.Should().Be("u-1");
        r.PageNumber.Should().Be(2);
    }

    [Fact] public void MergeFrom_NonEmptySource_CopiesAllFields()
    {
        var r = new GetPostsByAuthorRequest();
        r.MergeFrom(Full());
        r.UserId.Should().Be("u-1");
        r.PageNumber.Should().Be(2);
        r.PageSize.Should().Be(10);
        r.Status.Should().Be("Published");
    }

    // Equals early-return branches for each field (lines 760–763)
    [Fact] public void Equals_DifferentUserId_ReturnsFalse() =>
        new GetPostsByAuthorRequest { UserId = "u-1" }
            .Equals(new GetPostsByAuthorRequest { UserId = "u-2" }).Should().BeFalse();

    [Fact] public void Equals_DifferentPageNumber_ReturnsFalse() =>
        new GetPostsByAuthorRequest { PageNumber = 1 }
            .Equals(new GetPostsByAuthorRequest { PageNumber = 2 }).Should().BeFalse();

    [Fact] public void Equals_DifferentPageSize_ReturnsFalse() =>
        new GetPostsByAuthorRequest { PageSize = 5 }
            .Equals(new GetPostsByAuthorRequest { PageSize = 10 }).Should().BeFalse();

    [Fact] public void Equals_DifferentStatus_ReturnsFalse() =>
        new GetPostsByAuthorRequest { Status = "Published" }
            .Equals(new GetPostsByAuthorRequest { Status = "Draft" }).Should().BeFalse();
}

// ═══════════════════════════════════════════════════════════════════════════
// GetPostsByTeamRequest
// ═══════════════════════════════════════════════════════════════════════════

public class GetPostsByTeamRequestCoverageTests
{
    private static GetPostsByTeamRequest Full() => new()
    {
        TeamId = "t-1", PageNumber = 1, PageSize = 15
    };

    // GetPostsByAuthorRequest has fields: UserId=1(str), ..., Status=4(str).
    // Serialize with only UserId and Status set (fields 2/3 zero, not written).
    // When parsed as GetPostsByTeamRequest (TeamId=1,PageNumber=2,PageSize=3) → field 4 unknown.
    private static GetPostsByTeamRequest WithUnknown()
    {
        var src   = new GetPostsByAuthorRequest { UserId = "t-1", Status = "Published" };
        var bytes = GetPostsByTeamRequest.Parser.ParseFrom(src.ToByteArray());
        return bytes;
    }

    [Fact] public void Equals_WithDifferentType_ReturnsFalse() =>
        Full().Equals(new object()).Should().BeFalse();

    [Fact] public void Equals_SameReference_ReturnsTrue()
    {
        var r = Full();
        r.Equals((object)r).Should().BeTrue();
    }

    [Fact] public void Equals_Null_ReturnsFalse() =>
        new GetPostsByTeamRequest().Equals((GetPostsByTeamRequest?)null).Should().BeFalse();

    [Fact] public void GetHashCode_EmptyMessage_ReturnsDeterministicValue() =>
        new GetPostsByTeamRequest().GetHashCode().Should().Be(new GetPostsByTeamRequest().GetHashCode());

    [Fact] public void GetHashCode_FullMessage_DifferentFromEmpty() =>
        Full().GetHashCode().Should().NotBe(new GetPostsByTeamRequest().GetHashCode());

    [Fact] public void GetHashCode_WithUnknownFields_Branches() =>
        WithUnknown().GetHashCode().Should().NotBe(0);

    [Fact] public void ToString_ReturnsNonNullString() =>
        Full().ToString().Should().NotBeNull();

    [Fact] public void CalculateSize_EmptyMessage_ReturnsZero() =>
        new GetPostsByTeamRequest().CalculateSize().Should().Be(0);

    [Fact] public void CalculateSize_PageNumber_NonZeroBranch() =>
        new GetPostsByTeamRequest { PageNumber = 3 }.CalculateSize().Should().BeGreaterThan(0);

    [Fact] public void CalculateSize_PageSize_NonZeroBranch() =>
        new GetPostsByTeamRequest { PageSize = 5 }.CalculateSize().Should().BeGreaterThan(0);

    [Fact] public void CalculateSize_FullMessage_ReturnsPositive() =>
        Full().CalculateSize().Should().BeGreaterThan(0);

    [Fact] public void CalculateSize_WithUnknownFields_ReturnsPositive() =>
        WithUnknown().CalculateSize().Should().BeGreaterThan(0);

    [Fact] public void WriteTo_EmptyMessage_WritesZeroBytes() =>
        new GetPostsByTeamRequest().ToByteArray().Should().BeEmpty();

    [Fact] public void WriteTo_FullMessage_WritesBytes() =>
        Full().ToByteArray().Should().NotBeEmpty();

    [Fact] public void WriteTo_WithUnknownFields_WritesBytes() =>
        WithUnknown().ToByteArray().Should().NotBeEmpty();

    [Fact] public void MergeFrom_Null_DoesNotThrow()
    {
        var r = Full();
        r.MergeFrom((GetPostsByTeamRequest?)null!);
        r.TeamId.Should().Be("t-1");
    }

    [Fact] public void MergeFrom_EmptySource_DoesNotOverwrite()
    {
        var r = Full();
        r.MergeFrom(new GetPostsByTeamRequest());
        r.TeamId.Should().Be("t-1");
        r.PageNumber.Should().Be(1);
    }

    [Fact] public void MergeFrom_NonEmptySource_CopiesAllFields()
    {
        var r = new GetPostsByTeamRequest();
        r.MergeFrom(Full());
        r.TeamId.Should().Be("t-1");
        r.PageNumber.Should().Be(1);
        r.PageSize.Should().Be(15);
    }

    // Equals early-return branches (lines 1048–1050)
    [Fact] public void Equals_DifferentTeamId_ReturnsFalse() =>
        new GetPostsByTeamRequest { TeamId = "t-1" }
            .Equals(new GetPostsByTeamRequest { TeamId = "t-2" }).Should().BeFalse();

    [Fact] public void Equals_DifferentPageNumber_ReturnsFalse() =>
        new GetPostsByTeamRequest { PageNumber = 1 }
            .Equals(new GetPostsByTeamRequest { PageNumber = 2 }).Should().BeFalse();

    [Fact] public void Equals_DifferentPageSize_ReturnsFalse() =>
        new GetPostsByTeamRequest { PageSize = 5 }
            .Equals(new GetPostsByTeamRequest { PageSize = 10 }).Should().BeFalse();
}

// ═══════════════════════════════════════════════════════════════════════════
// MediaDto
// ═══════════════════════════════════════════════════════════════════════════

public class MediaDtoCoverageTests
{
    private static MediaDto Full() => new()
    {
        ObjectName  = "photo.jpg",
        Bucket      = "images",
        ContentType = "image/jpeg"
    };

    // Append field 4 (tag=(4<<3)|2=34, length=1, 'A'=65) → _unknownFields
    private static MediaDto WithUnknown()
    {
        var bytes = Full().ToByteArray().Concat(new byte[] { 34, 1, 65 }).ToArray();
        return MediaDto.Parser.ParseFrom(bytes);
    }

    [Fact] public void Equals_WithDifferentType_ReturnsFalse() =>
        Full().Equals(42).Should().BeFalse();

    [Fact] public void Equals_SameReference_ReturnsTrue()
    {
        var m = Full();
        m.Equals((object)m).Should().BeTrue();
    }

    [Fact] public void Equals_Null_ReturnsFalse() =>
        new MediaDto().Equals((MediaDto?)null).Should().BeFalse();

    [Fact] public void GetHashCode_EmptyMessage_ReturnsDeterministicValue() =>
        new MediaDto().GetHashCode().Should().Be(new MediaDto().GetHashCode());

    [Fact] public void GetHashCode_FullMessage_DifferentFromEmpty() =>
        Full().GetHashCode().Should().NotBe(new MediaDto().GetHashCode());

    [Fact] public void GetHashCode_WithUnknownFields_Branches() =>
        WithUnknown().GetHashCode().Should().NotBe(0);

    [Fact] public void ToString_ReturnsNonNullString() =>
        Full().ToString().Should().NotBeNull();

    [Fact] public void CalculateSize_EmptyMessage_ReturnsZero() =>
        new MediaDto().CalculateSize().Should().Be(0);

    [Fact] public void CalculateSize_FullMessage_ReturnsPositive() =>
        Full().CalculateSize().Should().BeGreaterThan(0);

    [Fact] public void CalculateSize_WithUnknownFields_ReturnsPositive() =>
        WithUnknown().CalculateSize().Should().BeGreaterThan(0);

    [Fact] public void WriteTo_EmptyMessage_WritesZeroBytes() =>
        new MediaDto().ToByteArray().Should().BeEmpty();

    [Fact] public void WriteTo_FullMessage_WritesBytes() =>
        Full().ToByteArray().Should().NotBeEmpty();

    [Fact] public void MergeFrom_Null_DoesNotThrow()
    {
        var m = Full();
        m.MergeFrom((MediaDto?)null!);
        m.ObjectName.Should().Be("photo.jpg");
    }

    [Fact] public void MergeFrom_EmptySource_DoesNotOverwrite()
    {
        var m = Full();
        m.MergeFrom(new MediaDto());
        m.ObjectName.Should().Be("photo.jpg");
        m.Bucket.Should().Be("images");
    }

    [Fact] public void MergeFrom_NonEmptySource_CopiesAllFields()
    {
        var m = new MediaDto();
        m.MergeFrom(Full());
        m.ObjectName.Should().Be("photo.jpg");
        m.Bucket.Should().Be("images");
        m.ContentType.Should().Be("image/jpeg");
    }

    // WriteTo with unknown fields (line 3071-3072)
    [Fact] public void WriteTo_WithUnknownFields_WritesBytes() =>
        WithUnknown().ToByteArray().Should().NotBeEmpty();

    // Equals early-return branches (lines 3005–3007)
    [Fact] public void Equals_DifferentObjectName_ReturnsFalse() =>
        new MediaDto { ObjectName = "a.jpg" }
            .Equals(new MediaDto { ObjectName = "b.jpg" }).Should().BeFalse();

    [Fact] public void Equals_DifferentBucket_ReturnsFalse() =>
        new MediaDto { Bucket = "bucket-a" }
            .Equals(new MediaDto { Bucket = "bucket-b" }).Should().BeFalse();

    [Fact] public void Equals_DifferentContentType_ReturnsFalse() =>
        new MediaDto { ContentType = "image/jpeg" }
            .Equals(new MediaDto { ContentType = "image/png" }).Should().BeFalse();
}

// ═══════════════════════════════════════════════════════════════════════════
// PostAuthorDto
// ═══════════════════════════════════════════════════════════════════════════

public class PostAuthorDtoCoverageTests
{
    private static PostAuthorDto Full() => new()
    {
        UserId = "u-1", UserName = "alice", AvatarUrl = "https://cdn/a.png"
    };

    // PostDto fields 1(PostId=str), 2(Title=str), 4(ExternalLink=str).
    // Parsing as PostAuthorDto (fields 1-3) → field 4 becomes _unknownFields.
    private static PostAuthorDto WithUnknown() =>
        PostAuthorDto.Parser.ParseFrom(
            new PostDto { PostId = "u-1", Title = "alice", ExternalLink = "url" }.ToByteArray());

    [Fact] public void Equals_WithDifferentType_ReturnsFalse() =>
        Full().Equals("str").Should().BeFalse();

    [Fact] public void Equals_SameReference_ReturnsTrue()
    {
        var a = Full();
        a.Equals((object)a).Should().BeTrue();
    }

    [Fact] public void Equals_Null_ReturnsFalse() =>
        new PostAuthorDto().Equals((PostAuthorDto?)null).Should().BeFalse();

    [Fact] public void GetHashCode_EmptyMessage_ReturnsDeterministicValue() =>
        new PostAuthorDto().GetHashCode().Should().Be(new PostAuthorDto().GetHashCode());

    [Fact] public void GetHashCode_FullMessage_DifferentFromEmpty() =>
        Full().GetHashCode().Should().NotBe(new PostAuthorDto().GetHashCode());

    [Fact] public void GetHashCode_WithUnknownFields_Branches() =>
        WithUnknown().GetHashCode().Should().NotBe(0);

    [Fact] public void ToString_ReturnsNonNullString() =>
        Full().ToString().Should().NotBeNull();

    [Fact] public void CalculateSize_EmptyMessage_ReturnsZero() =>
        new PostAuthorDto().CalculateSize().Should().Be(0);

    [Fact] public void CalculateSize_FullMessage_ReturnsPositive() =>
        Full().CalculateSize().Should().BeGreaterThan(0);

    [Fact] public void CalculateSize_WithUnknownFields_ReturnsPositive() =>
        WithUnknown().CalculateSize().Should().BeGreaterThan(0);

    [Fact] public void WriteTo_EmptyMessage_WritesZeroBytes() =>
        new PostAuthorDto().ToByteArray().Should().BeEmpty();

    [Fact] public void WriteTo_FullMessage_WritesBytes() =>
        Full().ToByteArray().Should().NotBeEmpty();

    [Fact] public void MergeFrom_Null_DoesNotThrow()
    {
        var a = Full();
        a.MergeFrom((PostAuthorDto?)null!);
        a.UserId.Should().Be("u-1");
    }

    [Fact] public void MergeFrom_EmptySource_DoesNotOverwrite()
    {
        var a = Full();
        a.MergeFrom(new PostAuthorDto());
        a.UserId.Should().Be("u-1");
        a.UserName.Should().Be("alice");
    }

    [Fact] public void MergeFrom_NonEmptySource_CopiesAllFields()
    {
        var a = new PostAuthorDto();
        a.MergeFrom(Full());
        a.UserId.Should().Be("u-1");
        a.UserName.Should().Be("alice");
        a.AvatarUrl.Should().Be("https://cdn/a.png");
    }

    // WriteTo with unknown fields (lines 2540-2541)
    [Fact] public void WriteTo_WithUnknownFields_WritesBytes() =>
        WithUnknown().ToByteArray().Should().NotBeEmpty();

    // Equals early-return branches (lines 2474–2476)
    [Fact] public void Equals_DifferentUserId_ReturnsFalse() =>
        new PostAuthorDto { UserId = "u-1" }
            .Equals(new PostAuthorDto { UserId = "u-2" }).Should().BeFalse();

    [Fact] public void Equals_DifferentUserName_ReturnsFalse() =>
        new PostAuthorDto { UserName = "alice" }
            .Equals(new PostAuthorDto { UserName = "bob" }).Should().BeFalse();

    [Fact] public void Equals_DifferentAvatarUrl_ReturnsFalse() =>
        new PostAuthorDto { AvatarUrl = "https://cdn/a.png" }
            .Equals(new PostAuthorDto { AvatarUrl = "https://cdn/b.png" }).Should().BeFalse();
}

// ═══════════════════════════════════════════════════════════════════════════
// PostDto
// ═══════════════════════════════════════════════════════════════════════════

public class PostDtoCoverageTests
{
    private static PostDto Full()
    {
        var dto = new PostDto
        {
            PostId        = "p-1",
            Title         = "Hello",
            Description   = "Desc",
            ExternalLink  = "https://link.com",
            Status        = "Published",
            Author        = new PostAuthorDto { UserId = "u-1", UserName = "alice" },
            Team          = new PostTeamDto  { TeamId = "t-1", Name = "Alpha" },
            LikesCount    = 5,
            ViewsCount    = 100,
            CommentsCount = 3,
            CreatedAt     = "2024-01-01",
            UpdatedAt     = "2024-06-01"
        };
        dto.Tags.Add("dotnet");
        dto.Media.Add(new MediaDto { ObjectName = "img.jpg", Bucket = "b", ContentType = "image/jpeg" });
        return dto;
    }

    // Append field 15 (tag=(15<<3)|2=122, length=1, 'A'=65) → _unknownFields
    private static PostDto WithUnknown()
    {
        var bytes = new PostDto { PostId = "p-1" }.ToByteArray()
            .Concat(new byte[] { 122, 1, 65 }).ToArray();
        return PostDto.Parser.ParseFrom(bytes);
    }

    [Fact] public void Equals_WithDifferentType_ReturnsFalse() =>
        new PostDto { PostId = "p-1" }.Equals(new object()).Should().BeFalse();

    [Fact] public void Equals_SameReference_ReturnsTrue()
    {
        var d = Full();
        d.Equals((object)d).Should().BeTrue();
    }

    [Fact] public void Equals_Null_ReturnsFalse() =>
        new PostDto().Equals((PostDto?)null).Should().BeFalse();

    [Fact] public void GetHashCode_EmptyMessage_ReturnsDeterministicValue() =>
        new PostDto().GetHashCode().Should().Be(new PostDto().GetHashCode());

    [Fact] public void GetHashCode_FullMessage_DifferentFromEmpty() =>
        Full().GetHashCode().Should().NotBe(new PostDto().GetHashCode());

    [Fact] public void GetHashCode_WithUnknownFields_Branches() =>
        WithUnknown().GetHashCode().Should().NotBe(0);

    [Fact] public void ToString_ReturnsNonNullString() =>
        Full().ToString().Should().NotBeNull();

    [Fact] public void CalculateSize_EmptyMessage_ReturnsZero() =>
        new PostDto().CalculateSize().Should().Be(0);

    [Fact] public void CalculateSize_LikesCount_NonZeroBranch() =>
        new PostDto { LikesCount = 7 }.CalculateSize().Should().BeGreaterThan(0);

    [Fact] public void CalculateSize_ViewsCount_NonZeroBranch() =>
        new PostDto { ViewsCount = 10 }.CalculateSize().Should().BeGreaterThan(0);

    [Fact] public void CalculateSize_CommentsCount_NonZeroBranch() =>
        new PostDto { CommentsCount = 2 }.CalculateSize().Should().BeGreaterThan(0);

    [Fact] public void CalculateSize_WithAuthor_NonZeroBranch()
    {
        var d = new PostDto { Author = new PostAuthorDto { UserId = "u-1" } };
        d.CalculateSize().Should().BeGreaterThan(0);
    }

    [Fact] public void CalculateSize_WithTeam_NonZeroBranch()
    {
        var d = new PostDto { Team = new PostTeamDto { TeamId = "t-1" } };
        d.CalculateSize().Should().BeGreaterThan(0);
    }

    [Fact] public void CalculateSize_FullMessage_ReturnsPositive() =>
        Full().CalculateSize().Should().BeGreaterThan(0);

    [Fact] public void CalculateSize_WithUnknownFields_ReturnsPositive() =>
        WithUnknown().CalculateSize().Should().BeGreaterThan(0);

    [Fact] public void WriteTo_EmptyMessage_WritesZeroBytes() =>
        new PostDto().ToByteArray().Should().BeEmpty();

    [Fact] public void WriteTo_FullMessage_WritesBytes() =>
        Full().ToByteArray().Should().NotBeEmpty();

    [Fact] public void MergeFrom_Null_DoesNotThrow()
    {
        var d = new PostDto { PostId = "p-1" };
        d.MergeFrom((PostDto?)null!);
        d.PostId.Should().Be("p-1");
    }

    [Fact] public void MergeFrom_EmptySource_DoesNotOverwrite()
    {
        var d = new PostDto { PostId = "p-1", LikesCount = 5 };
        d.MergeFrom(new PostDto());
        d.PostId.Should().Be("p-1");
        d.LikesCount.Should().Be(5);
    }

    [Fact] public void MergeFrom_NonEmptySource_CopiesStringFields()
    {
        var d = new PostDto();
        d.MergeFrom(new PostDto { PostId = "p-2", Title = "T", Status = "Draft" });
        d.PostId.Should().Be("p-2");
        d.Title.Should().Be("T");
        d.Status.Should().Be("Draft");
    }

    [Fact] public void MergeFrom_NonEmptySource_CopiesIntFields()
    {
        var d = new PostDto();
        d.MergeFrom(new PostDto { LikesCount = 3, ViewsCount = 50, CommentsCount = 1 });
        d.LikesCount.Should().Be(3);
        d.ViewsCount.Should().Be(50);
        d.CommentsCount.Should().Be(1);
    }

    [Fact] public void MergeFrom_NonEmptySource_MergesAuthor()
    {
        var d = new PostDto();
        d.MergeFrom(new PostDto { Author = new PostAuthorDto { UserId = "u-1", UserName = "alice" } });
        d.Author.Should().NotBeNull();
        d.Author!.UserId.Should().Be("u-1");
    }

    [Fact] public void MergeFrom_NonEmptySource_MergesTeam()
    {
        var d = new PostDto();
        d.MergeFrom(new PostDto { Team = new PostTeamDto { TeamId = "t-2", Name = "Beta" } });
        d.Team.Should().NotBeNull();
        d.Team!.TeamId.Should().Be("t-2");
    }

    [Fact] public void MergeFrom_NonEmptySource_AddsTags()
    {
        var d = new PostDto();
        var src = new PostDto();
        src.Tags.Add("x");
        src.Tags.Add("y");
        d.MergeFrom(src);
        d.Tags.Should().ContainInOrder("x", "y");
    }

    [Fact] public void MergeFrom_NonEmptySource_AddsMedia()
    {
        var d = new PostDto();
        var src = new PostDto();
        src.Media.Add(new MediaDto { ObjectName = "a.jpg", Bucket = "b", ContentType = "image/jpeg" });
        d.MergeFrom(src);
        d.Media.Should().HaveCount(1);
        d.Media[0].ObjectName.Should().Be("a.jpg");
    }

    [Fact] public void MergeFrom_ExistingAuthor_DeepMerges()
    {
        var d = new PostDto { Author = new PostAuthorDto { UserId = "u-1" } };
        d.MergeFrom(new PostDto { Author = new PostAuthorDto { UserName = "alice" } });
        d.Author!.UserId.Should().Be("u-1");
        d.Author.UserName.Should().Be("alice");
    }

    // WriteTo with unknown fields (lines 2112-2113)
    [Fact] public void WriteTo_WithUnknownFields_WritesBytes() =>
        WithUnknown().ToByteArray().Should().NotBeEmpty();

    // Clone with null Author and Team (lines 1743-1744 false branches)
    [Fact]
    public void Clone_WithNullAuthorAndTeam_DoesNotThrow()
    {
        var d     = new PostDto { PostId = "p-1", Title = "T" };
        var clone = d.Clone();
        clone.Author.Should().BeNull();
        clone.Team.Should().BeNull();
    }

    // MergeFrom fields not covered by existing tests (lines 2178-2214)
    [Fact]
    public void MergeFrom_WithDescriptionExternalLinkCreatedAtUpdatedAt_CopiesAll()
    {
        var d = new PostDto();
        d.MergeFrom(new PostDto
        {
            Description  = "Desc",
            ExternalLink = "https://example.com",
            CreatedAt    = "2024-01-01",
            UpdatedAt    = "2024-06-01"
        });
        d.Description.Should().Be("Desc");
        d.ExternalLink.Should().Be("https://example.com");
        d.CreatedAt.Should().Be("2024-01-01");
        d.UpdatedAt.Should().Be("2024-06-01");
    }

    // MergeFrom with existing Team (line 2193 false branch)
    [Fact]
    public void MergeFrom_ExistingTeam_DeepMerges()
    {
        var d = new PostDto { Team = new PostTeamDto { TeamId = "t-1" } };
        d.MergeFrom(new PostDto { Team = new PostTeamDto { Name = "Alpha" } });
        d.Team!.TeamId.Should().Be("t-1");
        d.Team.Name.Should().Be("Alpha");
    }

    // ParseFrom covering ExternalLink, Team, ViewsCount, CommentsCount, UpdatedAt (lines 2319-2359)
    [Fact]
    public void SerializeAndDeserialize_AllFields_CoversInternalMergePaths()
    {
        var dto = new PostDto
        {
            PostId = "p-1", Title = "T", Description = "D",
            ExternalLink = "https://x.com", Status = "Published",
            Author = new PostAuthorDto { UserId = "u-1" },
            Team   = new PostTeamDto  { TeamId = "t-1", Name = "Alpha" },
            LikesCount = 5, ViewsCount = 100, CommentsCount = 3,
            CreatedAt = "2024-01-01", UpdatedAt = "2024-06-01"
        };
        dto.Tags.Add("x");
        var parsed = PostDto.Parser.ParseFrom(dto.ToByteArray());
        parsed.ExternalLink.Should().Be("https://x.com");
        parsed.Team.Should().NotBeNull();
        parsed.ViewsCount.Should().Be(100);
        parsed.CommentsCount.Should().Be(3);
        parsed.UpdatedAt.Should().Be("2024-06-01");
    }

    // ParseFrom duplicate-field patterns to cover author_ and team_ not-null branches
    [Fact]
    public void ParseFrom_DuplicateAuthorField_MergesIntoExisting()
    {
        var b1 = new PostDto { Author = new PostAuthorDto { UserId = "u-1" } }.ToByteArray();
        var b2 = new PostDto { Author = new PostAuthorDto { UserName = "alice" } }.ToByteArray();
        var parsed = PostDto.Parser.ParseFrom(b1.Concat(b2).ToArray());
        parsed.Author!.UserId.Should().Be("u-1");
        parsed.Author.UserName.Should().Be("alice");
    }

    [Fact]
    public void ParseFrom_DuplicateTeamField_MergesIntoExisting()
    {
        var b1 = new PostDto { Team = new PostTeamDto { TeamId = "t-1" } }.ToByteArray();
        var b2 = new PostDto { Team = new PostTeamDto { Name = "Alpha" } }.ToByteArray();
        var parsed = PostDto.Parser.ParseFrom(b1.Concat(b2).ToArray());
        parsed.Team!.TeamId.Should().Be("t-1");
        parsed.Team.Name.Should().Be("Alpha");
    }

    // Equals early-return branches (lines 1950-1961)
    [Fact] public void Equals_DifferentDescription_ReturnsFalse() =>
        new PostDto { Description = "a" }.Equals(new PostDto { Description = "b" }).Should().BeFalse();

    [Fact] public void Equals_DifferentExternalLink_ReturnsFalse() =>
        new PostDto { ExternalLink = "http://a.com" }
            .Equals(new PostDto { ExternalLink = "http://b.com" }).Should().BeFalse();

    [Fact] public void Equals_DifferentAuthor_ReturnsFalse()
    {
        var d1 = new PostDto { Author = new PostAuthorDto { UserId = "u-1" } };
        var d2 = new PostDto { Author = new PostAuthorDto { UserId = "u-2" } };
        d1.Should().NotBe(d2);
    }

    [Fact] public void Equals_DifferentTeam_ReturnsFalse()
    {
        var d1 = new PostDto { Team = new PostTeamDto { TeamId = "t-1" } };
        var d2 = new PostDto { Team = new PostTeamDto { TeamId = "t-2" } };
        d1.Should().NotBe(d2);
    }

    [Fact] public void Equals_DifferentTags_ReturnsFalse()
    {
        var d1 = new PostDto(); d1.Tags.Add("x");
        var d2 = new PostDto(); d2.Tags.Add("y");
        d1.Should().NotBe(d2);
    }

    [Fact] public void Equals_DifferentMedia_ReturnsFalse()
    {
        var d1 = new PostDto();
        d1.Media.Add(new MediaDto { ObjectName = "a.jpg", Bucket = "b", ContentType = "image/jpeg" });
        var d2 = new PostDto();
        d2.Media.Add(new MediaDto { ObjectName = "b.jpg", Bucket = "b", ContentType = "image/jpeg" });
        d1.Should().NotBe(d2);
    }

    [Fact] public void Equals_DifferentViewsCount_ReturnsFalse() =>
        new PostDto { ViewsCount = 10 }.Equals(new PostDto { ViewsCount = 20 }).Should().BeFalse();

    [Fact] public void Equals_DifferentCommentsCount_ReturnsFalse() =>
        new PostDto { CommentsCount = 1 }.Equals(new PostDto { CommentsCount = 2 }).Should().BeFalse();

    [Fact] public void Equals_DifferentCreatedAt_ReturnsFalse() =>
        new PostDto { CreatedAt = "2024-01-01" }
            .Equals(new PostDto { CreatedAt = "2024-06-01" }).Should().BeFalse();

    [Fact] public void Equals_DifferentUpdatedAt_ReturnsFalse() =>
        new PostDto { UpdatedAt = "2024-01-01" }
            .Equals(new PostDto { UpdatedAt = "2024-06-01" }).Should().BeFalse();

    // Remaining early-return branches: Title (1949), Status (1952), LikesCount (1957)
    [Fact] public void Equals_DifferentTitle_ReturnsFalse() =>
        new PostDto { Title = "Hello" }
            .Equals(new PostDto { Title = "World" }).Should().BeFalse();

    [Fact] public void Equals_DifferentStatus_ReturnsFalse() =>
        new PostDto { Status = "Published" }
            .Equals(new PostDto { Status = "Draft" }).Should().BeFalse();

    [Fact] public void Equals_DifferentLikesCount_ReturnsFalse() =>
        new PostDto { LikesCount = 5 }
            .Equals(new PostDto { LikesCount = 10 }).Should().BeFalse();

    // Copy-constructor null branches for Author/Team (lines 1743-1744 false branch)
    [Fact]
    public void CopyConstructor_NullAuthorAndTeam_ResultsInNullAuthorAndTeam()
    {
        var src  = new PostDto();  // author_ = null, team_ = null
        var copy = new PostDto(src);
        copy.Author.Should().BeNull();
        copy.Team.Should().BeNull();
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// PagedPostsResponse
// ═══════════════════════════════════════════════════════════════════════════

public class PagedPostsResponseCoverageTests
{
    private static PagedPostsResponse Full()
    {
        var r = new PagedPostsResponse { TotalCount = 10, PageNumber = 1, PageSize = 5 };
        r.Items.Add(new PostDto { PostId = "p-1", Title = "First" });
        return r;
    }

    // Append field 5 (tag=(5<<3)|2=42, length=1, 'A'=65) → _unknownFields
    private static PagedPostsResponse WithUnknown()
    {
        var bytes = Full().ToByteArray().Concat(new byte[] { 42, 1, 65 }).ToArray();
        return PagedPostsResponse.Parser.ParseFrom(bytes);
    }

    [Fact] public void Equals_WithDifferentType_ReturnsFalse() =>
        Full().Equals(new object()).Should().BeFalse();

    [Fact] public void Equals_SameReference_ReturnsTrue()
    {
        var r = Full();
        r.Equals((object)r).Should().BeTrue();
    }

    [Fact] public void Equals_Null_ReturnsFalse() =>
        new PagedPostsResponse().Equals((PagedPostsResponse?)null).Should().BeFalse();

    [Fact] public void GetHashCode_EmptyMessage_ReturnsDeterministicValue() =>
        new PagedPostsResponse().GetHashCode().Should().Be(new PagedPostsResponse().GetHashCode());

    [Fact] public void GetHashCode_FullMessage_DifferentFromEmpty() =>
        Full().GetHashCode().Should().NotBe(new PagedPostsResponse().GetHashCode());

    [Fact] public void GetHashCode_WithUnknownFields_Branches() =>
        WithUnknown().GetHashCode().Should().NotBe(0);

    [Fact] public void ToString_ReturnsNonNullString() =>
        Full().ToString().Should().NotBeNull();

    [Fact] public void CalculateSize_EmptyMessage_ReturnsZero() =>
        new PagedPostsResponse().CalculateSize().Should().Be(0);

    [Fact] public void CalculateSize_TotalCount_NonZeroBranch() =>
        new PagedPostsResponse { TotalCount = 50 }.CalculateSize().Should().BeGreaterThan(0);

    [Fact] public void CalculateSize_PageNumber_NonZeroBranch() =>
        new PagedPostsResponse { PageNumber = 2 }.CalculateSize().Should().BeGreaterThan(0);

    [Fact] public void CalculateSize_PageSize_NonZeroBranch() =>
        new PagedPostsResponse { PageSize = 10 }.CalculateSize().Should().BeGreaterThan(0);

    [Fact] public void CalculateSize_FullMessage_ReturnsPositive() =>
        Full().CalculateSize().Should().BeGreaterThan(0);

    [Fact] public void CalculateSize_WithUnknownFields_ReturnsPositive() =>
        WithUnknown().CalculateSize().Should().BeGreaterThan(0);

    [Fact] public void WriteTo_EmptyMessage_WritesZeroBytes() =>
        new PagedPostsResponse().ToByteArray().Should().BeEmpty();

    [Fact] public void WriteTo_FullMessage_WritesBytes() =>
        Full().ToByteArray().Should().NotBeEmpty();

    [Fact] public void MergeFrom_Null_DoesNotThrow()
    {
        var r = Full();
        r.MergeFrom((PagedPostsResponse?)null!);
        r.TotalCount.Should().Be(10);
    }

    [Fact] public void MergeFrom_EmptySource_DoesNotOverwriteInts()
    {
        var r = Full();
        r.MergeFrom(new PagedPostsResponse());
        r.TotalCount.Should().Be(10);
        r.PageNumber.Should().Be(1);
    }

    [Fact] public void MergeFrom_NonEmptySource_CopiesCounters()
    {
        var r = new PagedPostsResponse();
        r.MergeFrom(new PagedPostsResponse { TotalCount = 20, PageNumber = 3, PageSize = 5 });
        r.TotalCount.Should().Be(20);
        r.PageNumber.Should().Be(3);
        r.PageSize.Should().Be(5);
    }

    [Fact] public void MergeFrom_NonEmptySource_AddsItems()
    {
        var r   = new PagedPostsResponse();
        var src = new PagedPostsResponse();
        src.Items.Add(new PostDto { PostId = "p-1" });
        src.Items.Add(new PostDto { PostId = "p-2" });
        r.MergeFrom(src);
        r.Items.Should().HaveCount(2);
        r.Items[0].PostId.Should().Be("p-1");
    }

    // WriteTo with unknown fields (lines 1593-1594)
    [Fact] public void WriteTo_WithUnknownFields_WritesBytes() =>
        WithUnknown().ToByteArray().Should().NotBeEmpty();

    // Equals early-return branches (lines 1523–1526)
    [Fact] public void Equals_DifferentItems_ReturnsFalse()
    {
        var r1 = new PagedPostsResponse(); r1.Items.Add(new PostDto { PostId = "p-1" });
        var r2 = new PagedPostsResponse(); r2.Items.Add(new PostDto { PostId = "p-2" });
        r1.Should().NotBe(r2);
    }

    [Fact] public void Equals_DifferentTotalCount_ReturnsFalse() =>
        new PagedPostsResponse { TotalCount = 10 }
            .Equals(new PagedPostsResponse { TotalCount = 20 }).Should().BeFalse();

    [Fact] public void Equals_DifferentPageNumber_ReturnsFalse() =>
        new PagedPostsResponse { PageNumber = 1 }
            .Equals(new PagedPostsResponse { PageNumber = 2 }).Should().BeFalse();

    [Fact] public void Equals_DifferentPageSize_ReturnsFalse() =>
        new PagedPostsResponse { PageSize = 5 }
            .Equals(new PagedPostsResponse { PageSize = 10 }).Should().BeFalse();
}

// ═══════════════════════════════════════════════════════════════════════════
// PostGrpcService: BindService + PostGrpcServiceBase
// ═══════════════════════════════════════════════════════════════════════════

public class PostGrpcServiceBindingTests
{
    private sealed class StubImpl : PostGrpcService.PostGrpcServiceBase { }

    private sealed class NoOpServiceBinder : ServiceBinderBase
    {
        public int MethodsAdded { get; private set; }

        public override void AddMethod<TRequest, TResponse>(
            Method<TRequest, TResponse> method,
            UnaryServerMethod<TRequest, TResponse>? handler)
            where TRequest : class
            where TResponse : class
        {
            MethodsAdded++;
        }
    }

    [Fact] public void BindService_WithImpl_ReturnsNonNullDefinition() =>
        PostGrpcService.BindService(new StubImpl()).Should().NotBeNull();

    [Fact] public void BindService_Binder_WithNonNullImpl_BindsAllFourMethods()
    {
        var binder = new NoOpServiceBinder();
        PostGrpcService.BindService(binder, new StubImpl());
        binder.MethodsAdded.Should().Be(4);
    }

    [Fact] public void BindService_Binder_WithNullImpl_BindsNullHandlers()
    {
        var binder = new NoOpServiceBinder();
        PostGrpcService.BindService(binder, null!);
        binder.MethodsAdded.Should().Be(4);
    }

    [Fact]
    public async Task PostGrpcServiceBase_GetPostsPaged_ThrowsUnimplemented()
    {
        var stub = new StubImpl();
        var act  = async () => await stub.GetPostsPaged(new GetPostsPagedRequest(), null!);
        await act.Should().ThrowAsync<RpcException>()
            .Where(e => e.StatusCode == StatusCode.Unimplemented);
    }

    [Fact]
    public async Task PostGrpcServiceBase_GetPostById_ThrowsUnimplemented()
    {
        var stub = new StubImpl();
        var act  = async () => await stub.GetPostById(new GetPostByIdRequest(), null!);
        await act.Should().ThrowAsync<RpcException>()
            .Where(e => e.StatusCode == StatusCode.Unimplemented);
    }

    [Fact]
    public async Task PostGrpcServiceBase_GetPostsByAuthor_ThrowsUnimplemented()
    {
        var stub = new StubImpl();
        var act  = async () => await stub.GetPostsByAuthor(new GetPostsByAuthorRequest(), null!);
        await act.Should().ThrowAsync<RpcException>()
            .Where(e => e.StatusCode == StatusCode.Unimplemented);
    }

    [Fact]
    public async Task PostGrpcServiceBase_GetPostsByTeam_ThrowsUnimplemented()
    {
        var stub = new StubImpl();
        var act  = async () => await stub.GetPostsByTeam(new GetPostsByTeamRequest(), null!);
        await act.Should().ThrowAsync<RpcException>()
            .Where(e => e.StatusCode == StatusCode.Unimplemented);
    }

    // ── __Helper_SerializeMessage / __Helper_DeserializeMessage coverage ────

    private sealed class FakePostSerCtx : SerializationContext
    {
        private readonly ArrayBufferWriter<byte> _buf = new();
        public byte[] Written { get; private set; } = Array.Empty<byte>();
        public override void SetPayloadLength(int len) { }
        public override IBufferWriter<byte> GetBufferWriter() => _buf;
        public override void Complete() => Written = _buf.WrittenSpan.ToArray();
        public override void Complete(byte[] payload) => Written = payload;
    }

    private sealed class FakePostDeserCtx : DeserializationContext
    {
        private readonly byte[] _data;
        public FakePostDeserCtx(byte[] data) { _data = data; }
        public override int PayloadLength => _data.Length;
        public override ReadOnlySequence<byte> PayloadAsReadOnlySequence() => new ReadOnlySequence<byte>(_data);
        public override byte[] PayloadAsNewBuffer() => (byte[])_data.Clone();
    }

    private sealed class PlainPostMsg : Google.Protobuf.IMessage<PlainPostMsg>
    {
        public static readonly Google.Protobuf.MessageParser<PlainPostMsg> Parser =
            new Google.Protobuf.MessageParser<PlainPostMsg>(() => new PlainPostMsg());
        public Google.Protobuf.Reflection.MessageDescriptor Descriptor => null!;
        public int CalculateSize() => 0;
        public void MergeFrom(Google.Protobuf.CodedInputStream input) { }
        public void WriteTo(Google.Protobuf.CodedOutputStream output) { }
        public void MergeFrom(PlainPostMsg message) { }
        public PlainPostMsg Clone() => new();
        public bool Equals(PlainPostMsg? other) => other != null;
    }

    [Fact]
    public void Post_Helper_SerializeMessage_IBufferMessage_CoversBufferPath()
    {
        var method = typeof(PostGrpcService)
            .GetMethod("__Helper_SerializeMessage", BindingFlags.NonPublic | BindingFlags.Static)!;
        var ctx = new FakePostSerCtx();
        method.Invoke(null, new object[] { new GetPostByIdRequest { PostId = "p-1" }, ctx });
        ctx.Written.Should().NotBeEmpty();
    }

    [Fact]
    public void Post_Helper_SerializeMessage_NonIBufferMessage_CoversFallbackPath()
    {
        var method = typeof(PostGrpcService)
            .GetMethod("__Helper_SerializeMessage", BindingFlags.NonPublic | BindingFlags.Static)!;
        var ctx = new FakePostSerCtx();
        method.Invoke(null, new object[] { new PlainPostMsg(), ctx });
        ctx.Written.Should().BeEmpty();
    }

    [Fact]
    public void Post_Helper_DeserializeMessage_IBufferMessage_CoversBufferPath()
    {
        var bytes  = new GetPostByIdRequest { PostId = "p-1" }.ToByteArray();
        var method = typeof(PostGrpcService)
            .GetMethod("__Helper_DeserializeMessage", BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(typeof(GetPostByIdRequest));
        var result = (GetPostByIdRequest)method.Invoke(
            null, new object[] { new FakePostDeserCtx(bytes), GetPostByIdRequest.Parser })!;
        result.PostId.Should().Be("p-1");
    }

    [Fact]
    public void Post_Helper_DeserializeMessage_NonIBufferMessage_CoversFallbackPath()
    {
        var method = typeof(PostGrpcService)
            .GetMethod("__Helper_DeserializeMessage", BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(typeof(PlainPostMsg));
        var result = (PlainPostMsg)method.Invoke(
            null, new object[] { new FakePostDeserCtx(Array.Empty<byte>()), PlainPostMsg.Parser })!;
        result.Should().NotBeNull();
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// GetRequestByIdRequest
// ═══════════════════════════════════════════════════════════════════════════

public class GetRequestByIdRequestCoverageTests
{
    // GetRequestsByTeamRequest has fields: TeamId=1(str), Status=2(str).
    // Parsing as GetRequestByIdRequest (field 1=RequestId only) → field 2 unknown.
    private static GetRequestByIdRequest WithUnknown() =>
        GetRequestByIdRequest.Parser.ParseFrom(
            new GetRequestsByTeamRequest { TeamId = "req-1", Status = "Pending" }.ToByteArray());

    [Fact] public void Equals_WithDifferentType_ReturnsFalse() =>
        new GetRequestByIdRequest { RequestId = "r-1" }.Equals(true).Should().BeFalse();

    [Fact] public void Equals_SameReference_ReturnsTrue()
    {
        var r = new GetRequestByIdRequest { RequestId = "r-1" };
        r.Equals((object)r).Should().BeTrue();
    }

    [Fact] public void Equals_Null_ReturnsFalse() =>
        new GetRequestByIdRequest().Equals((GetRequestByIdRequest?)null).Should().BeFalse();

    [Fact] public void GetHashCode_EmptyMessage_ReturnsDeterministicValue() =>
        new GetRequestByIdRequest().GetHashCode().Should().Be(new GetRequestByIdRequest().GetHashCode());

    [Fact] public void GetHashCode_NonEmpty_DifferentFromEmpty() =>
        new GetRequestByIdRequest { RequestId = "r-1" }.GetHashCode()
            .Should().NotBe(new GetRequestByIdRequest().GetHashCode());

    [Fact] public void GetHashCode_WithUnknownFields_Branches() =>
        WithUnknown().GetHashCode().Should().NotBe(0);

    [Fact] public void ToString_ReturnsNonNullString() =>
        new GetRequestByIdRequest { RequestId = "r-1" }.ToString().Should().NotBeNull();

    [Fact] public void CalculateSize_EmptyMessage_ReturnsZero() =>
        new GetRequestByIdRequest().CalculateSize().Should().Be(0);

    [Fact] public void CalculateSize_NonEmpty_ReturnsPositive() =>
        new GetRequestByIdRequest { RequestId = "r-1" }.CalculateSize().Should().BeGreaterThan(0);

    [Fact] public void CalculateSize_WithUnknownFields_ReturnsPositive() =>
        WithUnknown().CalculateSize().Should().BeGreaterThan(0);

    [Fact] public void WriteTo_EmptyMessage_WritesZeroBytes() =>
        new GetRequestByIdRequest().ToByteArray().Should().BeEmpty();

    [Fact] public void WriteTo_NonEmpty_WritesBytes() =>
        new GetRequestByIdRequest { RequestId = "r-1" }.ToByteArray().Should().NotBeEmpty();

    [Fact] public void WriteTo_WithUnknownFields_WritesBytes() =>
        WithUnknown().ToByteArray().Should().NotBeEmpty();

    [Fact] public void MergeFrom_Null_DoesNotThrow()
    {
        var r = new GetRequestByIdRequest { RequestId = "r-1" };
        r.MergeFrom((GetRequestByIdRequest?)null!);
        r.RequestId.Should().Be("r-1");
    }

    [Fact] public void MergeFrom_EmptySource_DoesNotOverwrite()
    {
        var r = new GetRequestByIdRequest { RequestId = "r-1" };
        r.MergeFrom(new GetRequestByIdRequest());
        r.RequestId.Should().Be("r-1");
    }

    [Fact] public void MergeFrom_NonEmptySource_CopiesField()
    {
        var r = new GetRequestByIdRequest();
        r.MergeFrom(new GetRequestByIdRequest { RequestId = "r-2" });
        r.RequestId.Should().Be("r-2");
    }

    [Fact] public void Equals_DifferentRequestId_ReturnsFalse() =>
        new GetRequestByIdRequest { RequestId = "r-1" }
            .Equals(new GetRequestByIdRequest { RequestId = "r-2" }).Should().BeFalse();

    [Fact] public void Equals_SameRequestId_ReturnsTrue() =>
        new GetRequestByIdRequest { RequestId = "r-1" }
            .Equals(new GetRequestByIdRequest { RequestId = "r-1" }).Should().BeTrue();

    [Fact] public void SerializeAndDeserialize_NonEmptyRequestId_CoversParserPath()
    {
        var original = new GetRequestByIdRequest { RequestId = "req-xyz" };
        var parsed   = GetRequestByIdRequest.Parser.ParseFrom(original.ToByteArray());
        parsed.RequestId.Should().Be("req-xyz");
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// GetRequestsByTeamRequest
// ═══════════════════════════════════════════════════════════════════════════

public class GetRequestsByTeamRequestCoverageTests
{
    private static GetRequestsByTeamRequest Full() => new()
    {
        TeamId = "t-1", Status = "Pending"
    };

    // GetRequestsByUserRequest has fields: UserId=1(str), Status=2(str). Same structure.
    // Need field 3+. Append field 3 (tag=(3<<3)|2=26, length=1, 'A'=65).
    private static GetRequestsByTeamRequest WithUnknown()
    {
        var bytes = Full().ToByteArray().Concat(new byte[] { 26, 1, 65 }).ToArray();
        return GetRequestsByTeamRequest.Parser.ParseFrom(bytes);
    }

    [Fact] public void Equals_WithDifferentType_ReturnsFalse() =>
        Full().Equals(new object()).Should().BeFalse();

    [Fact] public void Equals_SameReference_ReturnsTrue()
    {
        var r = Full();
        r.Equals((object)r).Should().BeTrue();
    }

    [Fact] public void Equals_Null_ReturnsFalse() =>
        new GetRequestsByTeamRequest().Equals((GetRequestsByTeamRequest?)null).Should().BeFalse();

    [Fact] public void GetHashCode_EmptyMessage_ReturnsDeterministicValue() =>
        new GetRequestsByTeamRequest().GetHashCode().Should().Be(new GetRequestsByTeamRequest().GetHashCode());

    [Fact] public void GetHashCode_FullMessage_DifferentFromEmpty() =>
        Full().GetHashCode().Should().NotBe(new GetRequestsByTeamRequest().GetHashCode());

    [Fact] public void GetHashCode_WithUnknownFields_Branches() =>
        WithUnknown().GetHashCode().Should().NotBe(0);

    [Fact] public void ToString_ReturnsNonNullString() =>
        Full().ToString().Should().NotBeNull();

    [Fact] public void CalculateSize_EmptyMessage_ReturnsZero() =>
        new GetRequestsByTeamRequest().CalculateSize().Should().Be(0);

    [Fact] public void CalculateSize_FullMessage_ReturnsPositive() =>
        Full().CalculateSize().Should().BeGreaterThan(0);

    [Fact] public void CalculateSize_WithUnknownFields_ReturnsPositive() =>
        WithUnknown().CalculateSize().Should().BeGreaterThan(0);

    [Fact] public void WriteTo_EmptyMessage_WritesZeroBytes() =>
        new GetRequestsByTeamRequest().ToByteArray().Should().BeEmpty();

    [Fact] public void WriteTo_FullMessage_WritesBytes() =>
        Full().ToByteArray().Should().NotBeEmpty();

    [Fact] public void WriteTo_WithUnknownFields_WritesBytes() =>
        WithUnknown().ToByteArray().Should().NotBeEmpty();

    [Fact] public void MergeFrom_Null_DoesNotThrow()
    {
        var r = Full();
        r.MergeFrom((GetRequestsByTeamRequest?)null!);
        r.TeamId.Should().Be("t-1");
    }

    [Fact] public void MergeFrom_EmptySource_DoesNotOverwrite()
    {
        var r = Full();
        r.MergeFrom(new GetRequestsByTeamRequest());
        r.TeamId.Should().Be("t-1");
        r.Status.Should().Be("Pending");
    }

    [Fact] public void MergeFrom_NonEmptySource_CopiesAllFields()
    {
        var r = new GetRequestsByTeamRequest();
        r.MergeFrom(Full());
        r.TeamId.Should().Be("t-1");
        r.Status.Should().Be("Pending");
    }

    // Line 344: Equals early-return when TeamId differs
    [Fact] public void Equals_DifferentTeamId_ReturnsFalse() =>
        new GetRequestsByTeamRequest { TeamId = "t-1" }
            .Equals(new GetRequestsByTeamRequest { TeamId = "t-2" }).Should().BeFalse();

    [Fact] public void Equals_SameTeamId_DifferentStatus_ReturnsFalse() =>
        new GetRequestsByTeamRequest { TeamId = "t-1", Status = "Pending" }
            .Equals(new GetRequestsByTeamRequest { TeamId = "t-1", Status = "Accepted" }).Should().BeFalse();

    [Fact] public void Equals_SameTeamIdAndStatus_ReturnsTrue() =>
        new GetRequestsByTeamRequest { TeamId = "t-1", Status = "Pending" }
            .Equals(new GetRequestsByTeamRequest { TeamId = "t-1", Status = "Pending" }).Should().BeTrue();

    [Fact] public void SerializeAndDeserialize_NonEmptyFields_CoversParserPath()
    {
        var original = new GetRequestsByTeamRequest { TeamId = "team-abc", Status = "Accepted" };
        var parsed   = GetRequestsByTeamRequest.Parser.ParseFrom(original.ToByteArray());
        parsed.TeamId.Should().Be("team-abc");
        parsed.Status.Should().Be("Accepted");
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// GetRequestsByUserRequest
// ═══════════════════════════════════════════════════════════════════════════

public class GetRequestsByUserRequestCoverageTests
{
    private static GetRequestsByUserRequest Full() => new()
    {
        UserId = "u-1", Status = "Accepted"
    };

    // Append field 3 (tag=(3<<3)|2=26, length=1, 'A'=65) → _unknownFields
    private static GetRequestsByUserRequest WithUnknown()
    {
        var bytes = Full().ToByteArray().Concat(new byte[] { 26, 1, 65 }).ToArray();
        return GetRequestsByUserRequest.Parser.ParseFrom(bytes);
    }

    [Fact] public void Equals_WithDifferentType_ReturnsFalse() =>
        Full().Equals("str").Should().BeFalse();

    [Fact] public void Equals_SameReference_ReturnsTrue()
    {
        var r = Full();
        r.Equals((object)r).Should().BeTrue();
    }

    [Fact] public void Equals_Null_ReturnsFalse() =>
        new GetRequestsByUserRequest().Equals((GetRequestsByUserRequest?)null).Should().BeFalse();

    [Fact] public void GetHashCode_EmptyMessage_ReturnsDeterministicValue() =>
        new GetRequestsByUserRequest().GetHashCode().Should().Be(new GetRequestsByUserRequest().GetHashCode());

    [Fact] public void GetHashCode_FullMessage_DifferentFromEmpty() =>
        Full().GetHashCode().Should().NotBe(new GetRequestsByUserRequest().GetHashCode());

    [Fact] public void GetHashCode_WithUnknownFields_Branches() =>
        WithUnknown().GetHashCode().Should().NotBe(0);

    [Fact] public void ToString_ReturnsNonNullString() =>
        Full().ToString().Should().NotBeNull();

    [Fact] public void CalculateSize_EmptyMessage_ReturnsZero() =>
        new GetRequestsByUserRequest().CalculateSize().Should().Be(0);

    [Fact] public void CalculateSize_FullMessage_ReturnsPositive() =>
        Full().CalculateSize().Should().BeGreaterThan(0);

    [Fact] public void CalculateSize_WithUnknownFields_ReturnsPositive() =>
        WithUnknown().CalculateSize().Should().BeGreaterThan(0);

    [Fact] public void WriteTo_EmptyMessage_WritesZeroBytes() =>
        new GetRequestsByUserRequest().ToByteArray().Should().BeEmpty();

    [Fact] public void WriteTo_FullMessage_WritesBytes() =>
        Full().ToByteArray().Should().NotBeEmpty();

    [Fact] public void WriteTo_WithUnknownFields_WritesBytes() =>
        WithUnknown().ToByteArray().Should().NotBeEmpty();

    [Fact] public void MergeFrom_Null_DoesNotThrow()
    {
        var r = Full();
        r.MergeFrom((GetRequestsByUserRequest?)null!);
        r.UserId.Should().Be("u-1");
    }

    [Fact] public void MergeFrom_EmptySource_DoesNotOverwrite()
    {
        var r = Full();
        r.MergeFrom(new GetRequestsByUserRequest());
        r.UserId.Should().Be("u-1");
        r.Status.Should().Be("Accepted");
    }

    [Fact] public void MergeFrom_NonEmptySource_CopiesAllFields()
    {
        var r = new GetRequestsByUserRequest();
        r.MergeFrom(Full());
        r.UserId.Should().Be("u-1");
        r.Status.Should().Be("Accepted");
    }

    // Line 572: Equals early-return when Status differs (same UserId)
    [Fact] public void Equals_SameUserId_DifferentStatus_ReturnsFalse() =>
        new GetRequestsByUserRequest { UserId = "u-1", Status = "Pending" }
            .Equals(new GetRequestsByUserRequest { UserId = "u-1", Status = "Accepted" }).Should().BeFalse();

    [Fact] public void Equals_DifferentUserId_ReturnsFalse() =>
        new GetRequestsByUserRequest { UserId = "u-1", Status = "Pending" }
            .Equals(new GetRequestsByUserRequest { UserId = "u-2", Status = "Pending" }).Should().BeFalse();

    [Fact] public void Equals_SameUserIdAndStatus_ReturnsTrue() =>
        new GetRequestsByUserRequest { UserId = "u-1", Status = "Pending" }
            .Equals(new GetRequestsByUserRequest { UserId = "u-1", Status = "Pending" }).Should().BeTrue();

    [Fact] public void SerializeAndDeserialize_NonEmptyFields_CoversParserPath()
    {
        var original = new GetRequestsByUserRequest { UserId = "user-abc", Status = "Rejected" };
        var parsed   = GetRequestsByUserRequest.Parser.ParseFrom(original.ToByteArray());
        parsed.UserId.Should().Be("user-abc");
        parsed.Status.Should().Be("Rejected");
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// CollaborationRequestDto
// ═══════════════════════════════════════════════════════════════════════════

public class CollaborationRequestDtoCoverageTests
{
    private static CollaborationRequestDto Full() => new()
    {
        RequestId  = "req-1",
        TeamId     = "team-1",
        FromUserId = "user-1",
        ToUserId   = "user-2",
        Role       = "Developer",
        Message    = "Please join",
        Status     = "Pending",
        CreatedAt  = "2024-01-01",
        ResolvedAt = ""
    };

    // Append field 10 (tag=(10<<3)|2=82, length=1, 'A'=65) → _unknownFields
    private static CollaborationRequestDto WithUnknown()
    {
        var bytes = Full().ToByteArray().Concat(new byte[] { 82, 1, 65 }).ToArray();
        return CollaborationRequestDto.Parser.ParseFrom(bytes);
    }

    [Fact] public void Equals_WithDifferentType_ReturnsFalse() =>
        Full().Equals(new object()).Should().BeFalse();

    [Fact] public void Equals_SameReference_ReturnsTrue()
    {
        var d = Full();
        d.Equals((object)d).Should().BeTrue();
    }

    [Fact] public void Equals_Null_ReturnsFalse() =>
        new CollaborationRequestDto().Equals((CollaborationRequestDto?)null).Should().BeFalse();

    [Fact] public void GetHashCode_EmptyMessage_ReturnsDeterministicValue() =>
        new CollaborationRequestDto().GetHashCode().Should().Be(new CollaborationRequestDto().GetHashCode());

    [Fact] public void GetHashCode_FullMessage_DifferentFromEmpty() =>
        Full().GetHashCode().Should().NotBe(new CollaborationRequestDto().GetHashCode());

    [Fact] public void GetHashCode_WithUnknownFields_Branches() =>
        WithUnknown().GetHashCode().Should().NotBe(0);

    [Fact] public void ToString_ReturnsNonNullString() =>
        Full().ToString().Should().NotBeNull();

    [Fact] public void CalculateSize_EmptyMessage_ReturnsZero() =>
        new CollaborationRequestDto().CalculateSize().Should().Be(0);

    [Fact] public void CalculateSize_FullMessage_ReturnsPositive() =>
        Full().CalculateSize().Should().BeGreaterThan(0);

    [Fact] public void CalculateSize_WithUnknownFields_ReturnsPositive() =>
        WithUnknown().CalculateSize().Should().BeGreaterThan(0);

    [Fact] public void WriteTo_EmptyMessage_WritesZeroBytes() =>
        new CollaborationRequestDto().ToByteArray().Should().BeEmpty();

    [Fact] public void WriteTo_FullMessage_WritesBytes() =>
        Full().ToByteArray().Should().NotBeEmpty();

    [Fact] public void WriteTo_WithUnknownFields_WritesBytes() =>
        WithUnknown().ToByteArray().Should().NotBeEmpty();

    [Fact] public void MergeFrom_Null_DoesNotThrow()
    {
        var d = Full();
        d.MergeFrom((CollaborationRequestDto?)null!);
        d.RequestId.Should().Be("req-1");
    }

    [Fact] public void MergeFrom_EmptySource_DoesNotOverwrite()
    {
        var d = Full();
        d.MergeFrom(new CollaborationRequestDto());
        d.RequestId.Should().Be("req-1");
        d.Status.Should().Be("Pending");
    }

    [Fact] public void MergeFrom_NonEmptySource_CopiesAllFields()
    {
        var d = new CollaborationRequestDto();
        d.MergeFrom(Full());
        d.RequestId.Should().Be("req-1");
        d.TeamId.Should().Be("team-1");
        d.FromUserId.Should().Be("user-1");
        d.ToUserId.Should().Be("user-2");
        d.Role.Should().Be("Developer");
        d.Message.Should().Be("Please join");
        d.Status.Should().Be("Pending");
        d.CreatedAt.Should().Be("2024-01-01");
    }

    [Fact] public void MergeFrom_NonEmptyResolvedAt_CopiesResolvedAt()
    {
        var d = new CollaborationRequestDto();
        d.MergeFrom(new CollaborationRequestDto { ResolvedAt = "2024-06-02T10:00:00Z" });
        d.ResolvedAt.Should().Be("2024-06-02T10:00:00Z");
    }

    // Equals early-return branches for each field (lines 1274–1281)
    [Fact] public void Equals_DifferentTeamId_ReturnsFalse() =>
        new CollaborationRequestDto { TeamId = "t-1" }
            .Equals(new CollaborationRequestDto { TeamId = "t-2" }).Should().BeFalse();

    [Fact] public void Equals_DifferentFromUserId_ReturnsFalse() =>
        new CollaborationRequestDto { FromUserId = "u-1" }
            .Equals(new CollaborationRequestDto { FromUserId = "u-2" }).Should().BeFalse();

    [Fact] public void Equals_DifferentToUserId_ReturnsFalse() =>
        new CollaborationRequestDto { ToUserId = "u-1" }
            .Equals(new CollaborationRequestDto { ToUserId = "u-2" }).Should().BeFalse();

    [Fact] public void Equals_DifferentRole_ReturnsFalse() =>
        new CollaborationRequestDto { Role = "Developer" }
            .Equals(new CollaborationRequestDto { Role = "Designer" }).Should().BeFalse();

    [Fact] public void Equals_DifferentMessage_ReturnsFalse() =>
        new CollaborationRequestDto { Message = "hi" }
            .Equals(new CollaborationRequestDto { Message = "bye" }).Should().BeFalse();

    [Fact] public void Equals_DifferentStatus_ReturnsFalse() =>
        new CollaborationRequestDto { Status = "Pending" }
            .Equals(new CollaborationRequestDto { Status = "Accepted" }).Should().BeFalse();

    [Fact] public void Equals_DifferentCreatedAt_ReturnsFalse() =>
        new CollaborationRequestDto { CreatedAt = "2024-01-01" }
            .Equals(new CollaborationRequestDto { CreatedAt = "2024-06-01" }).Should().BeFalse();

    [Fact] public void Equals_DifferentResolvedAt_ReturnsFalse() =>
        new CollaborationRequestDto { ResolvedAt = "2024-01-01" }
            .Equals(new CollaborationRequestDto { ResolvedAt = "2024-06-01" }).Should().BeFalse();

    // GetHashCode with non-empty ResolvedAt (line 1297 true branch)
    [Fact] public void GetHashCode_WithNonEmptyResolvedAt_DifferentFromEmpty() =>
        new CollaborationRequestDto { ResolvedAt = "2024-06-02" }.GetHashCode()
            .Should().NotBe(new CollaborationRequestDto().GetHashCode());

    // CalculateSize / InternalWriteTo / InternalMergeFrom for ResolvedAt non-empty path
    [Fact] public void CalculateSize_WithNonEmptyResolvedAt_ReturnsPositive() =>
        new CollaborationRequestDto { ResolvedAt = "2024-06-02T10:00:00Z" }.CalculateSize().Should().BeGreaterThan(0);

    [Fact] public void WriteTo_WithNonEmptyResolvedAt_WritesBytes() =>
        new CollaborationRequestDto { ResolvedAt = "2024-06-02T10:00:00Z" }.ToByteArray().Should().NotBeEmpty();

    [Fact] public void SerializeAndDeserialize_WithNonEmptyResolvedAt_PreservesValue()
    {
        var original     = new CollaborationRequestDto { ResolvedAt = "2024-06-02T10:00:00Z" };
        var deserialized = CollaborationRequestDto.Parser.ParseFrom(original.ToByteArray());
        deserialized.ResolvedAt.Should().Be("2024-06-02T10:00:00Z");
    }

    [Fact] public void SerializeAndDeserialize_AllFieldsIncludingResolvedAt_CoversAllPaths()
    {
        var original = new CollaborationRequestDto
        {
            RequestId  = "req-1",
            TeamId     = "team-1",
            FromUserId = "user-1",
            ToUserId   = "user-2",
            Role       = "Developer",
            Message    = "Hello!",
            Status     = "Accepted",
            CreatedAt  = "2024-01-01T00:00:00Z",
            ResolvedAt = "2024-01-02T09:00:00Z"
        };
        var deserialized = CollaborationRequestDto.Parser.ParseFrom(original.ToByteArray());
        deserialized.ResolvedAt.Should().Be("2024-01-02T09:00:00Z");
        deserialized.RequestId.Should().Be("req-1");
    }

    [Fact] public void Equals_SameAllFields_ReturnsTrue() =>
        new CollaborationRequestDto { RequestId = "r-1", TeamId = "t-1", Status = "Pending" }
            .Equals(new CollaborationRequestDto { RequestId = "r-1", TeamId = "t-1", Status = "Pending" }).Should().BeTrue();
}

// ═══════════════════════════════════════════════════════════════════════════
// CollaborationRequestResponse
// ═══════════════════════════════════════════════════════════════════════════

public class CollaborationRequestResponseCoverageTests
{
    private static CollaborationRequestDto SampleDto() => new()
    {
        RequestId = "req-1", TeamId = "t-1", Status = "Pending"
    };

    // Append field 2 (tag=(2<<3)|2=18, length=1, 'A'=65) → _unknownFields
    private static CollaborationRequestResponse WithUnknown()
    {
        var bytes = new CollaborationRequestResponse { Request = SampleDto() }
            .ToByteArray().Concat(new byte[] { 18, 1, 65 }).ToArray();
        return CollaborationRequestResponse.Parser.ParseFrom(bytes);
    }

    [Fact] public void Equals_WithDifferentType_ReturnsFalse() =>
        new CollaborationRequestResponse { Request = SampleDto() }.Equals("str").Should().BeFalse();

    [Fact] public void Equals_SameReference_ReturnsTrue()
    {
        var r = new CollaborationRequestResponse { Request = SampleDto() };
        r.Equals((object)r).Should().BeTrue();
    }

    [Fact] public void Equals_Null_ReturnsFalse() =>
        new CollaborationRequestResponse().Equals((CollaborationRequestResponse?)null).Should().BeFalse();

    [Fact] public void GetHashCode_EmptyMessage_ReturnsDeterministicValue() =>
        new CollaborationRequestResponse().GetHashCode()
            .Should().Be(new CollaborationRequestResponse().GetHashCode());

    [Fact] public void GetHashCode_WithRequest_DifferentFromEmpty() =>
        new CollaborationRequestResponse { Request = SampleDto() }.GetHashCode()
            .Should().NotBe(new CollaborationRequestResponse().GetHashCode());

    [Fact] public void GetHashCode_WithUnknownFields_Branches() =>
        WithUnknown().GetHashCode().Should().NotBe(0);

    [Fact] public void ToString_ReturnsNonNullString() =>
        new CollaborationRequestResponse { Request = SampleDto() }.ToString().Should().NotBeNull();

    [Fact] public void CalculateSize_EmptyMessage_ReturnsZero() =>
        new CollaborationRequestResponse().CalculateSize().Should().Be(0);

    [Fact] public void CalculateSize_WithRequest_ReturnsPositive() =>
        new CollaborationRequestResponse { Request = SampleDto() }.CalculateSize().Should().BeGreaterThan(0);

    [Fact] public void CalculateSize_WithUnknownFields_ReturnsPositive() =>
        WithUnknown().CalculateSize().Should().BeGreaterThan(0);

    [Fact] public void WriteTo_EmptyMessage_WritesZeroBytes() =>
        new CollaborationRequestResponse().ToByteArray().Should().BeEmpty();

    [Fact] public void WriteTo_WithRequest_WritesBytes() =>
        new CollaborationRequestResponse { Request = SampleDto() }.ToByteArray().Should().NotBeEmpty();

    [Fact] public void WriteTo_WithUnknownFields_WritesBytes() =>
        WithUnknown().ToByteArray().Should().NotBeEmpty();

    [Fact] public void MergeFrom_Null_DoesNotThrow()
    {
        var r = new CollaborationRequestResponse { Request = SampleDto() };
        r.MergeFrom((CollaborationRequestResponse?)null!);
        r.Request!.RequestId.Should().Be("req-1");
    }

    [Fact] public void MergeFrom_EmptySource_DoesNotOverwrite()
    {
        var r = new CollaborationRequestResponse { Request = SampleDto() };
        r.MergeFrom(new CollaborationRequestResponse());
        r.Request!.RequestId.Should().Be("req-1");
    }

    [Fact] public void MergeFrom_NonEmptySource_MergesRequest()
    {
        var r = new CollaborationRequestResponse();
        r.MergeFrom(new CollaborationRequestResponse { Request = SampleDto() });
        r.Request.Should().NotBeNull();
        r.Request!.RequestId.Should().Be("req-1");
    }

    [Fact] public void MergeFrom_ExistingRequest_DeepMerges()
    {
        var r = new CollaborationRequestResponse { Request = new CollaborationRequestDto { RequestId = "req-1" } };
        r.MergeFrom(new CollaborationRequestResponse { Request = new CollaborationRequestDto { Status = "Accepted" } });
        r.Request!.RequestId.Should().Be("req-1");
        r.Request.Status.Should().Be("Accepted");
    }

    // Line 748: ternary false branch — copy-constructor with null request_
    [Fact]
    public void Clone_WithNullRequest_ReturnsCloneWithNullRequest()
    {
        var r     = new CollaborationRequestResponse();
        var clone = r.Clone();
        clone.Request.Should().BeNull();
    }

    // Line 785: Equals early-return when Request differs
    [Fact]
    public void Equals_DifferentRequest_ReturnsFalse()
    {
        var r1 = new CollaborationRequestResponse { Request = new CollaborationRequestDto { RequestId = "req-1" } };
        var r2 = new CollaborationRequestResponse { Request = new CollaborationRequestDto { RequestId = "req-2" } };
        r1.Should().NotBe(r2);
    }

    // Line 899: InternalMergeFrom false branch — request_ already set when second occurrence arrives
    [Fact]
    public void ParseFrom_DuplicateRequestField_MergesIntoExistingRequest()
    {
        var bytes1 = new CollaborationRequestResponse { Request = new CollaborationRequestDto { RequestId = "req-1" } }.ToByteArray();
        var bytes2 = new CollaborationRequestResponse { Request = new CollaborationRequestDto { Status = "Accepted" } }.ToByteArray();
        var parsed = CollaborationRequestResponse.Parser.ParseFrom(bytes1.Concat(bytes2).ToArray());
        parsed.Request!.RequestId.Should().Be("req-1");
        parsed.Request.Status.Should().Be("Accepted");
    }

    [Fact]
    public void Equals_EqualRequests_ReturnsTrue()
    {
        var dto = new CollaborationRequestDto { RequestId = "req-1", Status = "Pending" };
        var r1  = new CollaborationRequestResponse { Request = dto };
        var r2  = new CollaborationRequestResponse { Request = dto.Clone() };
        r1.Should().Be(r2);
    }

    [Fact]
    public void SerializeAndDeserialize_WithFullRequest_CoversAllParserPaths()
    {
        var original = new CollaborationRequestResponse
        {
            Request = new CollaborationRequestDto
            {
                RequestId = "req-1", TeamId = "team-1", Status = "Accepted",
                ResolvedAt = "2024-06-02T10:00:00Z"
            }
        };
        var parsed = CollaborationRequestResponse.Parser.ParseFrom(original.ToByteArray());
        parsed.Request!.RequestId.Should().Be("req-1");
        parsed.Request.ResolvedAt.Should().Be("2024-06-02T10:00:00Z");
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// CollaborationRequestsResponse
// ═══════════════════════════════════════════════════════════════════════════

public class CollaborationRequestsResponseCoverageTests
{
    private static CollaborationRequestsResponse WithItems()
    {
        var r = new CollaborationRequestsResponse();
        r.Items.Add(new CollaborationRequestDto { RequestId = "req-1", Status = "Pending" });
        r.Items.Add(new CollaborationRequestDto { RequestId = "req-2", Status = "Accepted" });
        return r;
    }

    // Append field 2 (tag=(2<<3)|2=18, length=1, 'A'=65) → _unknownFields
    private static CollaborationRequestsResponse WithUnknown()
    {
        var bytes = WithItems().ToByteArray().Concat(new byte[] { 18, 1, 65 }).ToArray();
        return CollaborationRequestsResponse.Parser.ParseFrom(bytes);
    }

    [Fact] public void Equals_WithDifferentType_ReturnsFalse() =>
        WithItems().Equals(42).Should().BeFalse();

    [Fact] public void Equals_SameReference_ReturnsTrue()
    {
        var r = WithItems();
        r.Equals((object)r).Should().BeTrue();
    }

    [Fact] public void Equals_Null_ReturnsFalse() =>
        new CollaborationRequestsResponse().Equals((CollaborationRequestsResponse?)null).Should().BeFalse();

    [Fact] public void GetHashCode_EmptyMessage_ReturnsDeterministicValue() =>
        new CollaborationRequestsResponse().GetHashCode()
            .Should().Be(new CollaborationRequestsResponse().GetHashCode());

    [Fact] public void GetHashCode_WithItems_DifferentFromEmpty() =>
        WithItems().GetHashCode().Should().NotBe(new CollaborationRequestsResponse().GetHashCode());

    [Fact] public void GetHashCode_WithUnknownFields_Branches() =>
        WithUnknown().GetHashCode().Should().NotBe(0);

    [Fact] public void ToString_ReturnsNonNullString() =>
        WithItems().ToString().Should().NotBeNull();

    [Fact] public void CalculateSize_EmptyMessage_ReturnsZero() =>
        new CollaborationRequestsResponse().CalculateSize().Should().Be(0);

    [Fact] public void CalculateSize_WithItems_ReturnsPositive() =>
        WithItems().CalculateSize().Should().BeGreaterThan(0);

    [Fact] public void CalculateSize_WithUnknownFields_ReturnsPositive() =>
        WithUnknown().CalculateSize().Should().BeGreaterThan(0);

    [Fact] public void WriteTo_EmptyMessage_WritesZeroBytes() =>
        new CollaborationRequestsResponse().ToByteArray().Should().BeEmpty();

    [Fact] public void WriteTo_WithItems_WritesBytes() =>
        WithItems().ToByteArray().Should().NotBeEmpty();

    [Fact] public void WriteTo_WithUnknownFields_WritesBytes() =>
        WithUnknown().ToByteArray().Should().NotBeEmpty();

    [Fact] public void MergeFrom_Null_DoesNotThrow()
    {
        var r = WithItems();
        r.MergeFrom((CollaborationRequestsResponse?)null!);
        r.Items.Should().HaveCount(2);
    }

    [Fact] public void MergeFrom_EmptySource_DoesNotClearItems()
    {
        var r = WithItems();
        r.MergeFrom(new CollaborationRequestsResponse());
        r.Items.Should().HaveCount(2);
    }

    [Fact] public void MergeFrom_NonEmptySource_AddsItems()
    {
        var r   = new CollaborationRequestsResponse();
        var src = new CollaborationRequestsResponse();
        src.Items.Add(new CollaborationRequestDto { RequestId = "req-3" });
        r.MergeFrom(src);
        r.Items.Should().HaveCount(1);
        r.Items[0].RequestId.Should().Be("req-3");
    }

    // Line 983: Equals early-return when Items differ
    [Fact]
    public void Equals_DifferentItems_ReturnsFalse()
    {
        var r1 = new CollaborationRequestsResponse();
        r1.Items.Add(new CollaborationRequestDto { RequestId = "req-1" });
        var r2 = new CollaborationRequestsResponse();
        r2.Items.Add(new CollaborationRequestDto { RequestId = "req-2" });
        r1.Should().NotBe(r2);
    }

    [Fact]
    public void Equals_SameItems_ReturnsTrue()
    {
        var dto = new CollaborationRequestDto { RequestId = "req-1" };
        var r1  = new CollaborationRequestsResponse();
        var r2  = new CollaborationRequestsResponse();
        r1.Items.Add(dto.Clone());
        r2.Items.Add(dto.Clone());
        r1.Should().Be(r2);
    }

    [Fact]
    public void Equals_BothEmpty_ReturnsTrue()
    {
        var r1 = new CollaborationRequestsResponse();
        var r2 = new CollaborationRequestsResponse();
        r1.Should().Be(r2);
    }

    [Fact]
    public void SerializeAndDeserialize_WithItems_CoversParserPath()
    {
        var original = new CollaborationRequestsResponse();
        original.Items.Add(new CollaborationRequestDto { RequestId = "req-1", Status = "Pending" });
        original.Items.Add(new CollaborationRequestDto { RequestId = "req-2", Status = "Accepted" });
        var parsed = CollaborationRequestsResponse.Parser.ParseFrom(original.ToByteArray());
        parsed.Items.Should().HaveCount(2);
        parsed.Items[0].RequestId.Should().Be("req-1");
        parsed.Items[1].RequestId.Should().Be("req-2");
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// CollaborationRequestGrpcService: BindService + base class
// ═══════════════════════════════════════════════════════════════════════════

public class CollaborationRequestGrpcServiceBindingTests
{
    private sealed class StubImpl : CollaborationRequestGrpcService.CollaborationRequestGrpcServiceBase { }

    private sealed class NoOpServiceBinder : ServiceBinderBase
    {
        public int MethodsAdded { get; private set; }

        public override void AddMethod<TRequest, TResponse>(
            Method<TRequest, TResponse> method,
            UnaryServerMethod<TRequest, TResponse>? handler)
            where TRequest : class
            where TResponse : class
        {
            MethodsAdded++;
        }
    }

    [Fact] public void BindService_WithImpl_ReturnsNonNullDefinition() =>
        CollaborationRequestGrpcService.BindService(new StubImpl()).Should().NotBeNull();

    [Fact] public void BindService_Binder_WithNonNullImpl_BindsAllThreeMethods()
    {
        var binder = new NoOpServiceBinder();
        CollaborationRequestGrpcService.BindService(binder, new StubImpl());
        binder.MethodsAdded.Should().Be(3);
    }

    [Fact] public void BindService_Binder_WithNullImpl_BindsNullHandlers()
    {
        var binder = new NoOpServiceBinder();
        CollaborationRequestGrpcService.BindService(binder, null!);
        binder.MethodsAdded.Should().Be(3);
    }

    [Fact]
    public async Task Base_GetRequestById_ThrowsUnimplemented()
    {
        var stub = new StubImpl();
        var act  = async () => await stub.GetRequestById(new GetRequestByIdRequest(), null!);
        await act.Should().ThrowAsync<RpcException>()
            .Where(e => e.StatusCode == StatusCode.Unimplemented);
    }

    [Fact]
    public async Task Base_GetRequestsByTeam_ThrowsUnimplemented()
    {
        var stub = new StubImpl();
        var act  = async () => await stub.GetRequestsByTeam(new GetRequestsByTeamRequest(), null!);
        await act.Should().ThrowAsync<RpcException>()
            .Where(e => e.StatusCode == StatusCode.Unimplemented);
    }

    [Fact]
    public async Task Base_GetRequestsByUser_ThrowsUnimplemented()
    {
        var stub = new StubImpl();
        var act  = async () => await stub.GetRequestsByUser(new GetRequestsByUserRequest(), null!);
        await act.Should().ThrowAsync<RpcException>()
            .Where(e => e.StatusCode == StatusCode.Unimplemented);
    }

    // ── Fake context helpers ──────────────────────────────────────────────────

    private sealed class FakeSerializationContext : SerializationContext
    {
        private readonly ArrayBufferWriter<byte> _buf = new();
        public byte[] Written { get; private set; } = Array.Empty<byte>();
        public override void SetPayloadLength(int len) { }
        public override IBufferWriter<byte> GetBufferWriter() => _buf;
        public override void Complete() => Written = _buf.WrittenSpan.ToArray();
        public override void Complete(byte[] payload) => Written = payload;
    }

    private sealed class FakeDeserializationContext : DeserializationContext
    {
        private readonly byte[] _data;
        public FakeDeserializationContext(byte[] data) { _data = data; }
        public override int PayloadLength => _data.Length;
        public override ReadOnlySequence<byte> PayloadAsReadOnlySequence() =>
            new ReadOnlySequence<byte>(_data);
        public override byte[] PayloadAsNewBuffer() => (byte[])_data.Clone();
    }

    // Minimal IMessage<T> that does NOT implement IBufferMessage — triggers fallback paths
    private sealed class PlainMsg : Google.Protobuf.IMessage<PlainMsg>
    {
        public static readonly Google.Protobuf.MessageParser<PlainMsg> Parser =
            new Google.Protobuf.MessageParser<PlainMsg>(() => new PlainMsg());
        public Google.Protobuf.Reflection.MessageDescriptor Descriptor => null!;
        public int CalculateSize() => 0;
        public void MergeFrom(Google.Protobuf.CodedInputStream input) { }
        public void WriteTo(Google.Protobuf.CodedOutputStream output) { }
        public void MergeFrom(PlainMsg message) { }
        public PlainMsg Clone() => new();
        public bool Equals(PlainMsg? other) => other != null;
    }

    // ── __Helper_SerializeMessage coverage ───────────────────────────────────

    [Fact]
    public void Helper_SerializeMessage_IBufferMessage_CoversBufferPath()
    {
        var method = typeof(CollaborationRequestGrpcService)
            .GetMethod("__Helper_SerializeMessage", BindingFlags.NonPublic | BindingFlags.Static)!;
        var ctx = new FakeSerializationContext();
        method.Invoke(null, new object[] { new GetRequestByIdRequest { RequestId = "r-1" }, ctx });
        ctx.Written.Should().NotBeEmpty();
    }

    [Fact]
    public void Helper_SerializeMessage_NonIBufferMessage_CoversFallbackPath()
    {
        var method = typeof(CollaborationRequestGrpcService)
            .GetMethod("__Helper_SerializeMessage", BindingFlags.NonPublic | BindingFlags.Static)!;
        var ctx = new FakeSerializationContext();
        method.Invoke(null, new object[] { new PlainMsg(), ctx });
        ctx.Written.Should().BeEmpty();
    }

    // ── __Helper_DeserializeMessage coverage ──────────────────────────────────

    [Fact]
    public void Helper_DeserializeMessage_IBufferMessage_CoversBufferPath()
    {
        var bytes  = new GetRequestByIdRequest { RequestId = "r-1" }.ToByteArray();
        var method = typeof(CollaborationRequestGrpcService)
            .GetMethod("__Helper_DeserializeMessage", BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(typeof(GetRequestByIdRequest));
        var result = (GetRequestByIdRequest)method.Invoke(
            null, new object[] { new FakeDeserializationContext(bytes), GetRequestByIdRequest.Parser })!;
        result.RequestId.Should().Be("r-1");
    }

    [Fact]
    public void Helper_DeserializeMessage_NonIBufferMessage_CoversFallbackPath()
    {
        var method = typeof(CollaborationRequestGrpcService)
            .GetMethod("__Helper_DeserializeMessage", BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(typeof(PlainMsg));
        var result = (PlainMsg)method.Invoke(
            null, new object[] { new FakeDeserializationContext(Array.Empty<byte>()), PlainMsg.Parser })!;
        result.Should().NotBeNull();
    }
}