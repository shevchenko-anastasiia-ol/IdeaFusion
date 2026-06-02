using ContentDomain.Exception;
using FluentAssertions;
using Xunit;

namespace Content.Tests.Domain;

public class BusinessConflictExceptionTests
{
    [Fact]
    public void DefaultConstructor_DoesNotThrow()
    {
        var act = () => new BusinessConflictException();
        act.Should().NotThrow();
    }

    [Fact]
    public void MessageConstructor_SetsMessage()
    {
        var ex = new BusinessConflictException("conflict occurred");
        ex.Message.Should().Be("conflict occurred");
    }

    [Fact]
    public void MessageAndInnerConstructor_SetsMessageAndInner()
    {
        var inner = new InvalidOperationException("inner");
        var ex    = new BusinessConflictException("outer", inner);

        ex.Message.Should().Be("outer");
        ex.InnerException.Should().BeSameAs(inner);
    }

    [Fact]
    public void IsAssignableFromSystemException()
    {
        var ex = new BusinessConflictException("x");
        ex.Should().BeAssignableTo<System.Exception>();
    }
}

public class NotFoundExceptionTests
{
    [Fact]
    public void DefaultConstructor_DoesNotThrow()
    {
        var act = () => new NotFoundException();
        act.Should().NotThrow();
    }

    [Fact]
    public void MessageConstructor_SetsMessage()
    {
        var ex = new NotFoundException("not found");
        ex.Message.Should().Be("not found");
    }

    [Fact]
    public void MessageAndInnerConstructor_SetsMessageAndInner()
    {
        var inner = new InvalidOperationException("inner");
        var ex    = new NotFoundException("outer", inner);

        ex.Message.Should().Be("outer");
        ex.InnerException.Should().BeSameAs(inner);
    }

    [Fact]
    public void IsAssignableFromSystemException()
    {
        var ex = new NotFoundException("x");
        ex.Should().BeAssignableTo<System.Exception>();
    }
}

public class ValidationExceptionTests
{
    [Fact]
    public void DefaultConstructor_DoesNotThrow()
    {
        var act = () => new ValidationException();
        act.Should().NotThrow();
    }

    [Fact]
    public void MessageConstructor_SetsMessage()
    {
        var ex = new ValidationException("invalid input");
        ex.Message.Should().Be("invalid input");
    }

    [Fact]
    public void MessageAndInnerConstructor_SetsMessageAndInner()
    {
        var inner = new ArgumentException("bad arg");
        var ex    = new ValidationException("validation failed", inner);

        ex.Message.Should().Be("validation failed");
        ex.InnerException.Should().BeSameAs(inner);
    }

    [Fact]
    public void IsAssignableFromSystemException()
    {
        var ex = new ValidationException("x");
        ex.Should().BeAssignableTo<System.Exception>();
    }
}
