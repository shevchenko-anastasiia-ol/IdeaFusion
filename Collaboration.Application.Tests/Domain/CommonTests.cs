using Collaboration.Domain.Common;
using Collaboration.Domain.Common.Helpers;
using Collaboration.Domain.Entities;
using Collaboration.Domain.ValueOfObjects;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Xunit;

namespace Collaboration.Application.Tests.CommonTests;

// ── BsonCollectionAttribute ───────────────────────────────────────────────────

public class BsonCollectionAttributeTests
{
    [Fact]
    public void Constructor_SetsCollectionName()
    {
        var attr = new BsonCollectionAttribute("Teams");
        attr.CollectionName.Should().Be("Teams");
    }

    [Fact]
    public void Attribute_IsAppliedToTeamClass()
    {
        var attr = typeof(Team)
            .GetCustomAttributes(typeof(BsonCollectionAttribute), false)
            .Cast<BsonCollectionAttribute>()
            .SingleOrDefault();

        attr.Should().NotBeNull();
        attr!.CollectionName.Should().Be("Teams");
    }

    [Fact]
    public void Attribute_TargetsClassOnly()
    {
        var usage = typeof(BsonCollectionAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .Single();

        usage.ValidOn.Should().Be(AttributeTargets.Class);
    }
}

// ── PagedList<T> ──────────────────────────────────────────────────────────────

public class PagedListTests
{
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        var items = new List<string> { "a", "b", "c" };
        var paged = new PagedList<string>(items, 30, 2, 10, "cursor-abc");

        paged.TotalCount.Should().Be(30);
        paged.CurrentPage.Should().Be(2);
        paged.PageSize.Should().Be(10);
        paged.TotalPages.Should().Be(3);
        paged.NextCursor.Should().Be("cursor-abc");
        paged.Should().BeEquivalentTo(items);
    }

    [Fact]
    public void Constructor_WhenNoCursorPassed_NextCursorIsNull()
    {
        var paged = new PagedList<string>(["x"], 1, 1, 10);
        paged.NextCursor.Should().BeNull();
    }

    [Fact]
    public void TotalPages_RoundsUpForPartialPage()
    {
        var paged = new PagedList<string>([], 11, 1, 10);
        paged.TotalPages.Should().Be(2);
    }

    [Fact]
    public void TotalPages_WhenExactMultiple_NoExtraTotalPage()
    {
        var paged = new PagedList<string>([], 20, 1, 10);
        paged.TotalPages.Should().Be(2);
    }

    [Fact]
    public void TotalPages_WhenZeroItems_IsZero()
    {
        var paged = new PagedList<string>([], 0, 1, 10);
        paged.TotalPages.Should().Be(0);
    }

    [Fact]
    public void Constructor_AddsItemsToList()
    {
        var items = new List<int> { 1, 2, 3 };
        var paged = new PagedList<int>(items, 3, 1, 10);
        paged.Should().ContainInOrder(1, 2, 3);
    }

    // ── FromCursor ────────────────────────────────────────────────────────────

    [Fact]
    public void FromCursor_WhenItemsCountLessThanPageSize_NextCursorIsNull()
    {
        var request = new CollaborationRequest("team-1", "user-1", "Dev", null);
        var paged = PagedList<CollaborationRequest>.FromCursor([request], pageSize: 10);

        paged.NextCursor.Should().BeNull();
    }

    [Fact]
    public void FromCursor_WhenItemsCountEqualsPageSize_NextCursorIsLastEntityId()
    {
        var req1 = new CollaborationRequest("team-1", "user-1", "Dev", null);
        var req2 = new CollaborationRequest("team-1", "user-2", "Dev", null);
        var items = new List<CollaborationRequest> { req1, req2 };

        var paged = PagedList<CollaborationRequest>.FromCursor(items, pageSize: 2);

        paged.NextCursor.Should().Be(req2.Id);
    }

    [Fact]
    public void FromCursor_SetsPageToOneAndCountToItemsCount()
    {
        var req = new CollaborationRequest("team-1", "user-1", "Dev", null);
        var paged = PagedList<CollaborationRequest>.FromCursor([req], pageSize: 5);

        paged.CurrentPage.Should().Be(1);
        paged.TotalCount.Should().Be(1);
    }
}

// ── SortHelper<T> ─────────────────────────────────────────────────────────────

public class SortHelperTests
{
    private static BsonDocument Render(MongoDB.Driver.SortDefinition<BsonDocument> sortDef) =>
        sortDef.Render(new MongoDB.Driver.RenderArgs<BsonDocument>(BsonDocumentSerializer.Instance, BsonSerializer.SerializerRegistry));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ApplySort_WhenNullOrWhitespace_ReturnsAscendingById(string? orderBy)
    {
        var sort = new SortHelper<BsonDocument>().ApplySort(orderBy);
        var rendered = Render(sort);
        rendered["_id"].AsInt32.Should().Be(1);
    }

    [Fact]
    public void ApplySort_SingleFieldAscending_BuildsCorrectSort()
    {
        var sort = new SortHelper<BsonDocument>().ApplySort("name");
        var rendered = Render(sort);
        rendered["name"].AsInt32.Should().Be(1);
    }

    [Fact]
    public void ApplySort_SingleFieldDescending_BuildsCorrectSort()
    {
        var sort = new SortHelper<BsonDocument>().ApplySort("name desc");
        var rendered = Render(sort);
        rendered["name"].AsInt32.Should().Be(-1);
    }

    [Fact]
    public void ApplySort_MultipleFields_BuildsCombinedSort()
    {
        var sort = new SortHelper<BsonDocument>().ApplySort("name,age desc");
        var rendered = Render(sort);
        rendered["name"].AsInt32.Should().Be(1);
        rendered["age"].AsInt32.Should().Be(-1);
    }

    [Fact]
    public void ApplySort_WhitespaceParamInList_IsSkipped()
    {
        var sort = new SortHelper<BsonDocument>().ApplySort("name, ,age");
        var rendered = Render(sort);
        rendered["name"].AsInt32.Should().Be(1);
        rendered["age"].AsInt32.Should().Be(1);
        rendered.Contains("").Should().BeFalse();
    }

    [Fact]
    public void ApplySort_FieldWithDescSuffix_CaseInsensitive()
    {
        var sort = new SortHelper<BsonDocument>().ApplySort("name DESC");
        var rendered = Render(sort);
        rendered["name"].AsInt32.Should().Be(-1);
    }
}