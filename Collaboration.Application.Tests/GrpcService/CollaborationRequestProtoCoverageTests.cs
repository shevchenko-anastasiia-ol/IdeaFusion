using FluentAssertions;
using Google.Protobuf;
using IdeaFusion.Grpc.CollaborationRequests;
using Xunit;

namespace Collaboration.Application.Tests.GrpcService;

// ═══════════════════════════════════════════════════════════════════════════════
// CollaborationRequestDto – complete branch / line coverage
// ═══════════════════════════════════════════════════════════════════════════════

public class CollaborationRequestDtoCoverageTests
{
    // ── helpers ──────────────────────────────────────────────────────────────────

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
        ResolvedAt = "2024-01-02"
    };

    // Append field 10 (tag = (10<<3)|2 = 82, length 1, 'A' = 65) → unknown field
    private static CollaborationRequestDto WithUnknown()
    {
        var bytes = Full().ToByteArray().Concat(new byte[] { 82, 1, 65 }).ToArray();
        return CollaborationRequestDto.Parser.ParseFrom(bytes);
    }

    // ── Descriptor ────────────────────────────────────────────────────────────

    [Fact]
    public void StaticDescriptor_IsNotNull() =>
        CollaborationRequestDto.Descriptor.Should().NotBeNull();

    [Fact]
    public void InterfaceDescriptor_MatchesStaticDescriptor()
    {
        IMessage dto = new CollaborationRequestDto();
        dto.Descriptor.Should().BeSameAs(CollaborationRequestDto.Descriptor);
    }

    // ── Equals(object) ────────────────────────────────────────────────────────

    [Fact]
    public void Equals_Object_WithNull_ReturnsFalse() =>
        new CollaborationRequestDto().Equals((object?)null).Should().BeFalse();

    [Fact]
    public void Equals_Object_WithDifferentType_ReturnsFalse() =>
        new CollaborationRequestDto().Equals("string").Should().BeFalse();

    [Fact]
    public void Equals_Object_WithSelf_ReturnsTrue()
    {
        var d = Full();
        d.Equals((object)d).Should().BeTrue();
    }

    [Fact]
    public void Equals_Object_WithEqualInstance_ReturnsTrue() =>
        Full().Equals((object)Full()).Should().BeTrue();

    // ── Equals(CollaborationRequestDto) ──────────────────────────────────────

    [Fact]
    public void Equals_Typed_WithNull_ReturnsFalse() =>
        Full().Equals((CollaborationRequestDto?)null).Should().BeFalse();

    [Fact]
    public void Equals_Typed_SameReference_ReturnsTrue()
    {
        var d = Full();
        d.Equals(d).Should().BeTrue();
    }

    [Fact]
    public void Equals_Typed_AllFieldsEqual_ReturnsTrue() =>
        Full().Equals(Full()).Should().BeTrue();

    [Fact]
    public void Equals_Typed_DiffRequestId_ReturnsFalse() =>
        Full().Equals(new CollaborationRequestDto { RequestId = "other" }).Should().BeFalse();

    [Fact]
    public void Equals_Typed_DiffTeamId_ReturnsFalse()
    {
        var a = Full();
        var b = Full();
        b.TeamId = "X";
        a.Equals(b).Should().BeFalse();
    }

    [Fact]
    public void Equals_Typed_DiffFromUserId_ReturnsFalse()
    {
        var a = Full(); var b = Full(); b.FromUserId = "X";
        a.Equals(b).Should().BeFalse();
    }

    [Fact]
    public void Equals_Typed_DiffToUserId_ReturnsFalse()
    {
        var a = Full(); var b = Full(); b.ToUserId = "X";
        a.Equals(b).Should().BeFalse();
    }

    [Fact]
    public void Equals_Typed_DiffRole_ReturnsFalse()
    {
        var a = Full(); var b = Full(); b.Role = "X";
        a.Equals(b).Should().BeFalse();
    }

    [Fact]
    public void Equals_Typed_DiffMessage_ReturnsFalse()
    {
        var a = Full(); var b = Full(); b.Message = "X";
        a.Equals(b).Should().BeFalse();
    }

    [Fact]
    public void Equals_Typed_DiffStatus_ReturnsFalse()
    {
        var a = Full(); var b = Full(); b.Status = "X";
        a.Equals(b).Should().BeFalse();
    }

    [Fact]
    public void Equals_Typed_DiffCreatedAt_ReturnsFalse()
    {
        var a = Full(); var b = Full(); b.CreatedAt = "X";
        a.Equals(b).Should().BeFalse();
    }

    [Fact]
    public void Equals_Typed_DiffResolvedAt_ReturnsFalse()
    {
        var a = Full(); var b = Full(); b.ResolvedAt = "X";
        a.Equals(b).Should().BeFalse();
    }

    // ── GetHashCode ───────────────────────────────────────────────────────────

    [Fact]
    public void GetHashCode_EmptyMessage_IsDeterministic() =>
        new CollaborationRequestDto().GetHashCode()
            .Should().Be(new CollaborationRequestDto().GetHashCode());

    [Fact]
    public void GetHashCode_FullMessage_DiffersFromEmpty() =>
        Full().GetHashCode().Should().NotBe(new CollaborationRequestDto().GetHashCode());

    [Fact]
    public void GetHashCode_EachFieldContributes()
    {
        // Every single-field message should produce a different hash than empty
        var fields = new[]
        {
            new CollaborationRequestDto { RequestId  = "x" },
            new CollaborationRequestDto { TeamId     = "x" },
            new CollaborationRequestDto { FromUserId = "x" },
            new CollaborationRequestDto { ToUserId   = "x" },
            new CollaborationRequestDto { Role       = "x" },
            new CollaborationRequestDto { Message    = "x" },
            new CollaborationRequestDto { Status     = "x" },
            new CollaborationRequestDto { CreatedAt  = "x" },
            new CollaborationRequestDto { ResolvedAt = "x" },
        };
        foreach (var f in fields)
            f.GetHashCode().Should().NotBe(new CollaborationRequestDto().GetHashCode());
    }

    [Fact]
    public void GetHashCode_WithUnknownFields_IncludesThemInHash() =>
        WithUnknown().GetHashCode().Should().NotBe(new CollaborationRequestDto().GetHashCode());

    // ── ToString ──────────────────────────────────────────────────────────────

    [Fact]
    public void ToString_EmptyMessage_ReturnsNonNull() =>
        new CollaborationRequestDto().ToString().Should().NotBeNull();

    [Fact]
    public void ToString_FullMessage_ReturnsNonNull() =>
        Full().ToString().Should().NotBeNull();

    // ── CalculateSize ─────────────────────────────────────────────────────────

    [Fact]
    public void CalculateSize_Empty_ReturnsZero() =>
        new CollaborationRequestDto().CalculateSize().Should().Be(0);

    [Fact]
    public void CalculateSize_Full_ReturnsPositive() =>
        Full().CalculateSize().Should().BeGreaterThan(0);

    [Fact]
    public void CalculateSize_EachFieldAddsSize()
    {
        var fields = new[]
        {
            new CollaborationRequestDto { RequestId  = "x" },
            new CollaborationRequestDto { TeamId     = "x" },
            new CollaborationRequestDto { FromUserId = "x" },
            new CollaborationRequestDto { ToUserId   = "x" },
            new CollaborationRequestDto { Role       = "x" },
            new CollaborationRequestDto { Message    = "x" },
            new CollaborationRequestDto { Status     = "x" },
            new CollaborationRequestDto { CreatedAt  = "x" },
            new CollaborationRequestDto { ResolvedAt = "x" },
        };
        foreach (var f in fields)
            f.CalculateSize().Should().BeGreaterThan(0);
    }

    [Fact]
    public void CalculateSize_WithUnknownFields_ReturnsPositive() =>
        WithUnknown().CalculateSize().Should().BeGreaterThan(0);

    // ── WriteTo (via ToByteArray which calls InternalWriteTo) ─────────────────

    [Fact]
    public void WriteTo_Empty_ProducesZeroBytes() =>
        new CollaborationRequestDto().ToByteArray().Should().BeEmpty();

    [Fact]
    public void WriteTo_Full_ProducesBytes() =>
        Full().ToByteArray().Should().NotBeEmpty();

    [Fact]
    public void WriteTo_WithUnknownFields_ProducesBytes() =>
        WithUnknown().ToByteArray().Should().NotBeEmpty();

    [Fact]
    public void WriteTo_EachFieldSerialised()
    {
        new CollaborationRequestDto { RequestId  = "x" }.ToByteArray().Should().NotBeEmpty();
        new CollaborationRequestDto { TeamId     = "x" }.ToByteArray().Should().NotBeEmpty();
        new CollaborationRequestDto { FromUserId = "x" }.ToByteArray().Should().NotBeEmpty();
        new CollaborationRequestDto { ToUserId   = "x" }.ToByteArray().Should().NotBeEmpty();
        new CollaborationRequestDto { Role       = "x" }.ToByteArray().Should().NotBeEmpty();
        new CollaborationRequestDto { Message    = "x" }.ToByteArray().Should().NotBeEmpty();
        new CollaborationRequestDto { Status     = "x" }.ToByteArray().Should().NotBeEmpty();
        new CollaborationRequestDto { CreatedAt  = "x" }.ToByteArray().Should().NotBeEmpty();
        new CollaborationRequestDto { ResolvedAt = "x" }.ToByteArray().Should().NotBeEmpty();
    }

    // ── MergeFrom(CollaborationRequestDto) ───────────────────────────────────

    [Fact]
    public void MergeFrom_Typed_Null_DoesNotThrowAndPreservesValues()
    {
        var d = Full();
        d.MergeFrom((CollaborationRequestDto?)null!);
        d.RequestId.Should().Be("req-1");
    }

    [Fact]
    public void MergeFrom_Typed_EmptySource_DoesNotOverwrite()
    {
        var d = Full();
        d.MergeFrom(new CollaborationRequestDto());
        d.RequestId.Should().Be("req-1");
        d.TeamId.Should().Be("team-1");
    }

    [Fact]
    public void MergeFrom_Typed_FullSource_CopiesAllFields()
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
        d.ResolvedAt.Should().Be("2024-01-02");
    }

    [Fact]
    public void MergeFrom_Typed_EachFieldMergedIndividually()
    {
        void Check(CollaborationRequestDto src, Action<CollaborationRequestDto> assert)
        {
            var d = new CollaborationRequestDto();
            d.MergeFrom(src);
            assert(d);
        }

        Check(new CollaborationRequestDto { RequestId  = "x" }, d => d.RequestId.Should().Be("x"));
        Check(new CollaborationRequestDto { TeamId     = "x" }, d => d.TeamId.Should().Be("x"));
        Check(new CollaborationRequestDto { FromUserId = "x" }, d => d.FromUserId.Should().Be("x"));
        Check(new CollaborationRequestDto { ToUserId   = "x" }, d => d.ToUserId.Should().Be("x"));
        Check(new CollaborationRequestDto { Role       = "x" }, d => d.Role.Should().Be("x"));
        Check(new CollaborationRequestDto { Message    = "x" }, d => d.Message.Should().Be("x"));
        Check(new CollaborationRequestDto { Status     = "x" }, d => d.Status.Should().Be("x"));
        Check(new CollaborationRequestDto { CreatedAt  = "x" }, d => d.CreatedAt.Should().Be("x"));
        Check(new CollaborationRequestDto { ResolvedAt = "x" }, d => d.ResolvedAt.Should().Be("x"));
    }

    // ── MergeFrom(CodedInputStream) / InternalMergeFrom via ParseFrom ─────────

    [Fact]
    public void ParseFrom_Full_RestoresAllFields()
    {
        var deserialized = CollaborationRequestDto.Parser.ParseFrom(Full().ToByteArray());
        deserialized.Should().Be(Full());
    }

    [Fact]
    public void ParseFrom_WithUnknownFields_PreservesMainFields()
    {
        var d = WithUnknown();
        d.RequestId.Should().Be("req-1");
    }

    [Fact]
    public void ParseFrom_Empty_ProducesDefaultInstance()
    {
        var d = CollaborationRequestDto.Parser.ParseFrom(Array.Empty<byte>());
        d.RequestId.Should().BeEmpty();
        d.TeamId.Should().BeEmpty();
    }

    // ── Clone ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Clone_ProducesEqualButDistinctInstance()
    {
        var original = Full();
        var clone    = original.Clone();
        clone.Should().Be(original);
        clone.Should().NotBeSameAs(original);
    }

    [Fact]
    public void Clone_WithUnknownFields_IsEqual()
    {
        var original = WithUnknown();
        var clone    = original.Clone();
        clone.Should().Be(original);
    }

    // ── Field number constants ─────────────────────────────────────────────────

    [Fact]
    public void FieldNumbers_AreCorrect()
    {
        CollaborationRequestDto.RequestIdFieldNumber.Should().Be(1);
        CollaborationRequestDto.TeamIdFieldNumber.Should().Be(2);
        CollaborationRequestDto.FromUserIdFieldNumber.Should().Be(3);
        CollaborationRequestDto.ToUserIdFieldNumber.Should().Be(4);
        CollaborationRequestDto.RoleFieldNumber.Should().Be(5);
        CollaborationRequestDto.MessageFieldNumber.Should().Be(6);
        CollaborationRequestDto.StatusFieldNumber.Should().Be(7);
        CollaborationRequestDto.CreatedAtFieldNumber.Should().Be(8);
        CollaborationRequestDto.ResolvedAtFieldNumber.Should().Be(9);
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// CollaborationRequestResponse – complete branch / line coverage
// ═══════════════════════════════════════════════════════════════════════════════

public class CollaborationRequestResponseCoverageTests
{
    // ── helpers ──────────────────────────────────────────────────────────────────

    private static CollaborationRequestDto SampleDto() => new()
    {
        RequestId = "req-1", TeamId = "team-1", Status = "Pending"
    };

    private static CollaborationRequestResponse WithRequest() =>
        new() { Request = SampleDto() };

    // Append field 2 (tag=(2<<3)|2=18, length 1, 'A'=65) → unknown field
    private static CollaborationRequestResponse WithUnknown()
    {
        var bytes = WithRequest().ToByteArray().Concat(new byte[] { 18, 1, 65 }).ToArray();
        return CollaborationRequestResponse.Parser.ParseFrom(bytes);
    }

    // ── Descriptor ────────────────────────────────────────────────────────────

    [Fact]
    public void StaticDescriptor_IsNotNull() =>
        CollaborationRequestResponse.Descriptor.Should().NotBeNull();

    [Fact]
    public void InterfaceDescriptor_MatchesStaticDescriptor()
    {
        IMessage resp = new CollaborationRequestResponse();
        resp.Descriptor.Should().BeSameAs(CollaborationRequestResponse.Descriptor);
    }

    // ── Equals(object) ────────────────────────────────────────────────────────

    [Fact]
    public void Equals_Object_WithNull_ReturnsFalse() =>
        new CollaborationRequestResponse().Equals((object?)null).Should().BeFalse();

    [Fact]
    public void Equals_Object_WithDifferentType_ReturnsFalse() =>
        new CollaborationRequestResponse().Equals(42).Should().BeFalse();

    [Fact]
    public void Equals_Object_WithSelf_ReturnsTrue()
    {
        var r = WithRequest();
        r.Equals((object)r).Should().BeTrue();
    }

    [Fact]
    public void Equals_Object_WithEqualInstance_ReturnsTrue() =>
        WithRequest().Equals((object)WithRequest()).Should().BeTrue();

    // ── Equals(CollaborationRequestResponse) ─────────────────────────────────

    [Fact]
    public void Equals_Typed_WithNull_ReturnsFalse() =>
        WithRequest().Equals((CollaborationRequestResponse?)null).Should().BeFalse();

    [Fact]
    public void Equals_Typed_SameReference_ReturnsTrue()
    {
        var r = WithRequest();
        r.Equals(r).Should().BeTrue();
    }

    [Fact]
    public void Equals_Typed_SameRequest_ReturnsTrue() =>
        WithRequest().Equals(WithRequest()).Should().BeTrue();

    [Fact]
    public void Equals_Typed_DifferentRequest_ReturnsFalse()
    {
        var a = new CollaborationRequestResponse { Request = new CollaborationRequestDto { RequestId = "a" } };
        var b = new CollaborationRequestResponse { Request = new CollaborationRequestDto { RequestId = "b" } };
        a.Equals(b).Should().BeFalse();
    }

    [Fact]
    public void Equals_Typed_NullVsNonNullRequest_ReturnsFalse() =>
        new CollaborationRequestResponse().Equals(WithRequest()).Should().BeFalse();

    // ── GetHashCode ───────────────────────────────────────────────────────────

    [Fact]
    public void GetHashCode_EmptyMessage_IsDeterministic() =>
        new CollaborationRequestResponse().GetHashCode()
            .Should().Be(new CollaborationRequestResponse().GetHashCode());

    [Fact]
    public void GetHashCode_WithRequest_DiffersFromEmpty() =>
        WithRequest().GetHashCode()
            .Should().NotBe(new CollaborationRequestResponse().GetHashCode());

    [Fact]
    public void GetHashCode_WithUnknownFields_IncludesThem() =>
        WithUnknown().GetHashCode().Should().NotBe(0);

    // ── ToString ──────────────────────────────────────────────────────────────

    [Fact]
    public void ToString_EmptyMessage_ReturnsNonNull() =>
        new CollaborationRequestResponse().ToString().Should().NotBeNull();

    [Fact]
    public void ToString_WithRequest_ReturnsNonNull() =>
        WithRequest().ToString().Should().NotBeNull();

    // ── CalculateSize ─────────────────────────────────────────────────────────

    [Fact]
    public void CalculateSize_Empty_ReturnsZero() =>
        new CollaborationRequestResponse().CalculateSize().Should().Be(0);

    [Fact]
    public void CalculateSize_WithRequest_ReturnsPositive() =>
        WithRequest().CalculateSize().Should().BeGreaterThan(0);

    [Fact]
    public void CalculateSize_WithUnknownFields_ReturnsPositive() =>
        WithUnknown().CalculateSize().Should().BeGreaterThan(0);

    // ── WriteTo (via ToByteArray / InternalWriteTo) ───────────────────────────

    [Fact]
    public void WriteTo_Empty_ProducesZeroBytes() =>
        new CollaborationRequestResponse().ToByteArray().Should().BeEmpty();

    [Fact]
    public void WriteTo_WithRequest_ProducesBytes() =>
        WithRequest().ToByteArray().Should().NotBeEmpty();

    [Fact]
    public void WriteTo_WithUnknownFields_ProducesBytes() =>
        WithUnknown().ToByteArray().Should().NotBeEmpty();

    // ── MergeFrom(CollaborationRequestResponse) ───────────────────────────────

    [Fact]
    public void MergeFrom_Typed_Null_PreservesExistingRequest()
    {
        var r = WithRequest();
        r.MergeFrom((CollaborationRequestResponse?)null!);
        r.Request!.RequestId.Should().Be("req-1");
    }

    [Fact]
    public void MergeFrom_Typed_EmptySource_PreservesExistingRequest()
    {
        var r = WithRequest();
        r.MergeFrom(new CollaborationRequestResponse());
        r.Request!.RequestId.Should().Be("req-1");
    }

    [Fact]
    public void MergeFrom_Typed_SetsRequestWhenTargetHasNone()
    {
        var r = new CollaborationRequestResponse();
        r.MergeFrom(WithRequest());
        r.Request.Should().NotBeNull();
        r.Request!.RequestId.Should().Be("req-1");
    }

    [Fact]
    public void MergeFrom_Typed_DeepMergesIntoExistingRequest()
    {
        var r = new CollaborationRequestResponse
        {
            Request = new CollaborationRequestDto { RequestId = "req-1" }
        };
        r.MergeFrom(new CollaborationRequestResponse
        {
            Request = new CollaborationRequestDto { Status = "Accepted" }
        });
        r.Request!.RequestId.Should().Be("req-1");
        r.Request.Status.Should().Be("Accepted");
    }

    // ── MergeFrom(CodedInputStream) / InternalMergeFrom via ParseFrom ─────────

    [Fact]
    public void ParseFrom_WithRequest_RestoresRequest()
    {
        var deserialized = CollaborationRequestResponse.Parser.ParseFrom(WithRequest().ToByteArray());
        deserialized.Request!.RequestId.Should().Be("req-1");
    }

    [Fact]
    public void ParseFrom_WithUnknownFields_PreservesMainRequest()
    {
        var r = WithUnknown();
        r.Request!.RequestId.Should().Be("req-1");
    }

    [Fact]
    public void ParseFrom_Empty_ProducesDefaultInstance() =>
        CollaborationRequestResponse.Parser.ParseFrom(Array.Empty<byte>())
            .Request.Should().BeNull();

    // ── Clone ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Clone_Empty_ProducesEqualDistinctInstance()
    {
        var original = new CollaborationRequestResponse();
        var clone    = original.Clone();
        clone.Should().Be(original);
        clone.Should().NotBeSameAs(original);
    }

    [Fact]
    public void Clone_WithRequest_PreservesRequest()
    {
        var original = WithRequest();
        var clone    = original.Clone();
        clone.Should().Be(original);
        clone.Request!.RequestId.Should().Be("req-1");
    }

    [Fact]
    public void Clone_WithUnknownFields_IsEqual()
    {
        var original = WithUnknown();
        var clone    = original.Clone();
        clone.Should().Be(original);
    }

    // ── Field number constant ──────────────────────────────────────────────────

    [Fact]
    public void FieldNumber_Request_IsOne() =>
        CollaborationRequestResponse.RequestFieldNumber.Should().Be(1);
}
