using Collaboration.Domain.Exceptions;
using FluentAssertions;
using FluentValidation.Results;
using Xunit;

namespace Collaboration.Application.Tests.ExceptionTests;

public class DomainExceptionTests
{
    [Fact]
    public void Constructor_SetsMessage()
    {
        var ex = new DomainException("something went wrong");
        ex.Message.Should().Be("something went wrong");
    }

    [Fact]
    public void IsException()
    {
        new DomainException("x").Should().BeAssignableTo<Exception>();
    }
}

public class NotFoundExceptionTests
{
    [Fact]
    public void Constructor_SetsMessage()
    {
        var ex = new NotFoundException("entity not found");
        ex.Message.Should().Be("entity not found");
    }

    [Fact]
    public void IsDomainException()
    {
        new NotFoundException("x").Should().BeAssignableTo<DomainException>();
    }
}

public class ConflictExceptionTests
{
    [Fact]
    public void Constructor_SetsMessage()
    {
        var ex = new ConflictException("already exists");
        ex.Message.Should().Be("already exists");
    }

    [Fact]
    public void IsDomainException()
    {
        new ConflictException("x").Should().BeAssignableTo<DomainException>();
    }
}

public class MongoDbWriteExceptionTests
{
    [Fact]
    public void Constructor_WithMessage_SetsMessage()
    {
        var ex = new MongoDbWriteException("write failed");
        ex.Message.Should().Be("write failed");
        ex.InnerException.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithMessageAndInner_SetsBoth()
    {
        var inner = new Exception("inner");
        var ex = new MongoDbWriteException("write failed", inner);
        ex.Message.Should().Be("write failed");
        ex.InnerException.Should().BeSameAs(inner);
    }

    [Fact]
    public void IsException()
    {
        new MongoDbWriteException("x").Should().BeAssignableTo<Exception>();
    }
}

public class MongoDbConnectionExceptionTests
{
    [Fact]
    public void Constructor_WithMessage_SetsMessage()
    {
        var ex = new MongoDbConnectionException("connection failed");
        ex.Message.Should().Be("connection failed");
        ex.InnerException.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithMessageAndInner_SetsBoth()
    {
        var inner = new Exception("inner");
        var ex = new MongoDbConnectionException("connection failed", inner);
        ex.Message.Should().Be("connection failed");
        ex.InnerException.Should().BeSameAs(inner);
    }

    [Fact]
    public void IsException()
    {
        new MongoDbConnectionException("x").Should().BeAssignableTo<Exception>();
    }
}

public class ConcurrencyExceptionTests
{
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        var ex = new ConcurrencyException("entity-42", expectedVersion: 3, actualVersion: 5);
        ex.EntityId.Should().Be("entity-42");
        ex.ExpectedVersion.Should().Be(3);
        ex.ActualVersion.Should().Be(5);
    }

    [Fact]
    public void Message_ContainsEntityIdAndVersions()
    {
        var ex = new ConcurrencyException("entity-42", 3, 5);
        ex.Message.Should().Contain("entity-42")
            .And.Contain("3")
            .And.Contain("5");
    }

    [Fact]
    public void IsException()
    {
        new ConcurrencyException("id", 0, 1).Should().BeAssignableTo<Exception>();
    }
}

public class CustomValidationExceptionTests
{
    [Fact]
    public void Constructor_SetsFailures()
    {
        var failures = new[]
        {
            new ValidationFailure("Name", "Name is required."),
            new ValidationFailure("Email", "Email is invalid.")
        };
        var ex = new CustomValidationException(failures);
        ex.Errors.Should().HaveCount(2);
        ex.Errors.Should().Contain(f => f.PropertyName == "Name");
        ex.Errors.Should().Contain(f => f.PropertyName == "Email");
    }

    [Fact]
    public void IsValidationException()
    {
        var ex = new CustomValidationException([]);
        ex.Should().BeAssignableTo<FluentValidation.ValidationException>();
    }
}