using Collaboration.Application.Commands.CollaborationRequest;
using Collaboration.Application.Validators;
using FluentAssertions;
using Xunit;

namespace Collaboration.Application.Tests.Validators;

public class CollaborationRequestValidatorsTests
{
    // ── CreateCollaborationRequestCommandValidator ────────────────────────────

    [Fact]
    public void CreateRequest_WhenAllFieldsValid_PassesValidation()
    {
        var validator = new CreateCollaborationRequestCommandValidator();
        var command = new CreateCollaborationRequestCommand
        {
            TeamId = "team-1", Role = "Developer"
        };

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateRequest_WithOptionalMessage_PassesValidation()
    {
        var validator = new CreateCollaborationRequestCommandValidator();
        var command = new CreateCollaborationRequestCommand
        {
            TeamId = "team-1", Role = "Developer",
            Message = "Please accept me."
        };

        validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "TeamId is required.")]
    [InlineData(null!, "TeamId is required.")]
    public void CreateRequest_WhenTeamIdEmpty_FailsValidation(string teamId, string expectedMessage)
    {
        var validator = new CreateCollaborationRequestCommandValidator();
        var command = new CreateCollaborationRequestCommand
        {
            TeamId = teamId, Role = "Developer"
        };

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "TeamId" && e.ErrorMessage == expectedMessage);
    }

    [Fact]
    public void CreateRequest_WhenRoleEmpty_FailsValidation()
    {
        var validator = new CreateCollaborationRequestCommandValidator();
        var command = new CreateCollaborationRequestCommand
        {
            TeamId = "team-1", Role = ""
        };

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "Role" && e.ErrorMessage == "Role is required.");
    }

    [Fact]
    public void CreateRequest_WhenRoleTooLong_FailsValidation()
    {
        var validator = new CreateCollaborationRequestCommandValidator();
        var command = new CreateCollaborationRequestCommand
        {
            TeamId = "team-1", Role = new string('x', 101)
        };

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "Role" && e.ErrorMessage == "Role cannot exceed 100 characters.");
    }

    [Fact]
    public void CreateRequest_WhenMessageIsNull_SkipsMessageValidation()
    {
        var validator = new CreateCollaborationRequestCommandValidator();
        var command = new CreateCollaborationRequestCommand
        {
            TeamId = "team-1", Role = "Developer", Message = null
        };

        validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateRequest_WhenMessageTooLong_FailsValidation()
    {
        var validator = new CreateCollaborationRequestCommandValidator();
        var command = new CreateCollaborationRequestCommand
        {
            TeamId = "team-1", Role = "Developer",
            Message = new string('x', 1001)
        };

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "Message" &&
            e.ErrorMessage == "Message cannot exceed 1000 characters.");
    }

    // ── AcceptCollaborationRequestCommandValidator ────────────────────────────

    [Fact]
    public void AcceptRequest_WhenAllFieldsValid_PassesValidation()
    {
        var validator = new AcceptCollaborationRequestCommandValidator();
        var command = new AcceptCollaborationRequestCommand
        {
            RequestId = "req-1"
        };

        validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void AcceptRequest_WhenRequestIdEmpty_FailsValidation()
    {
        var validator = new AcceptCollaborationRequestCommandValidator();
        var command = new AcceptCollaborationRequestCommand
        {
            RequestId = ""
        };

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "RequestId" && e.ErrorMessage == "RequestId is required.");
    }

    // ── CancelCollaborationRequestCommandValidator ────────────────────────────

    [Fact]
    public void CancelRequest_WhenAllFieldsValid_PassesValidation()
    {
        var validator = new CancelCollaborationRequestCommandValidator();
        var command = new CancelCollaborationRequestCommand
        {
            RequestId = "req-1"
        };

        validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void CancelRequest_WhenRequestIdEmpty_FailsValidation()
    {
        var validator = new CancelCollaborationRequestCommandValidator();
        var command = new CancelCollaborationRequestCommand
        {
            RequestId = ""
        };

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "RequestId" && e.ErrorMessage == "RequestId is required.");
    }

    // ── RejectCollaborationRequestCommandValidator ────────────────────────────

    [Fact]
    public void RejectRequest_WhenAllFieldsValid_PassesValidation()
    {
        var validator = new RejectCollaborationRequestCommandValidator();
        var command = new RejectCollaborationRequestCommand
        {
            RequestId = "req-1"
        };

        validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void RejectRequest_WhenRequestIdEmpty_FailsValidation()
    {
        var validator = new RejectCollaborationRequestCommandValidator();
        var command = new RejectCollaborationRequestCommand
        {
            RequestId = ""
        };

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RequestId");
    }

}