using Collaboration.Domain.Common;
using FluentAssertions;
using Xunit;

namespace Collaboration.Application.Tests.DomainTests;

public class BaseEntityTests
{
    private class ConcreteEntity : BaseEntity { }

    [Fact]
    public void NewEntity_HasGeneratedId()
    {
        var entity = new ConcreteEntity();
        entity.Id.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void NewEntity_IsNotDeleted()
    {
        var entity = new ConcreteEntity();
        entity.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void MarkAsCreated_SetsCreatedByAndCreatedAt()
    {
        var entity = new ConcreteEntity();
        var before = DateTime.UtcNow;

        entity.MarkAsCreated("user-1");

        entity.CreatedBy.Should().Be("user-1");
        entity.CreatedAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void MarkAsCreated_WithNullUserId_ThrowsArgumentNullException()
    {
        var entity = new ConcreteEntity();
        var act = () => entity.MarkAsCreated(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void MarkAsUpdated_SetsUpdatedByAndUpdatedAt()
    {
        var entity = new ConcreteEntity();
        var before = DateTime.UtcNow;

        entity.MarkAsUpdated("user-2");

        entity.UpdatedBy.Should().Be("user-2");
        entity.UpdatedAt.Should().NotBeNull();
        entity.UpdatedAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void MarkAsUpdated_WithNullUserId_ThrowsArgumentNullException()
    {
        var entity = new ConcreteEntity();
        var act = () => entity.MarkAsUpdated(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void MarkAsDeleted_SetsIsDeletedToTrue()
    {
        var entity = new ConcreteEntity();

        entity.MarkAsDeleted();

        entity.IsDeleted.Should().BeTrue();
        entity.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void TwoEntities_HaveDifferentIds()
    {
        var e1 = new ConcreteEntity();
        var e2 = new ConcreteEntity();
        e1.Id.Should().NotBe(e2.Id);
    }
}