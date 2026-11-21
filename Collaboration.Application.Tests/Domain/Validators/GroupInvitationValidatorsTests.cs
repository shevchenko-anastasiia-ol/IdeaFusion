using Collaboration.Application.Commands.GroupInvitation;
using Collaboration.Application.Validators;
using FluentAssertions;
using Xunit;

namespace Collaboration.Application.Tests.Validators;

public class GroupInvitationValidatorsTests
{
    // ── CreateGroupInvitationCommandValidator ─────────────────────────────────

    [Fact]
    public void CreateInvitation_WhenAllFieldsValid_PassesValidation()
    {
        var validator = new CreateGroupInvitationCommandValidator();
        var command = new CreateGroupInvitationCommand
        {
            TeamId = "team-1", InvitedUserId = "user-1",
            Role = "Developer", ExpirationDays = 7
        };

        validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateInvitation_WhenTeamIdEmpty_FailsValidation()
    {
        var validator = new CreateGroupInvitationCommandValidator();
        var command = new CreateGroupInvitationCommand
        {
            TeamId = "", InvitedUserId = "user-1",
            Role = "Developer", ExpirationDays = 7
        };

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "TeamId" && e.ErrorMessage == "TeamId is required.");
    }

    [Fact]
    public void CreateInvitation_WhenInvitedUserIdEmpty_FailsValidation()
    {
        var validator = new CreateGroupInvitationCommandValidator();
        var command = new CreateGroupInvitationCommand
        {
            TeamId = "team-1", InvitedUserId = "",
            Role = "Developer", ExpirationDays = 7
        };

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "InvitedUserId" && e.ErrorMessage == "InvitedUserId is required.");
    }

    [Fact]
    public void CreateInvitation_WhenRoleEmpty_FailsValidation()
    {
        var validator = new CreateGroupInvitationCommandValidator();
        var command = new CreateGroupInvitationCommand
        {
            TeamId = "team-1", InvitedUserId = "user-1",
            Role = "", ExpirationDays = 7
        };

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "Role" && e.ErrorMessage == "Role is required.");
    }

    [Fact]
    public void CreateInvitation_WhenRoleTooLong_FailsValidation()
    {
        var validator = new CreateGroupInvitationCommandValidator();
        var command = new CreateGroupInvitationCommand
        {
            TeamId = "team-1", InvitedUserId = "user-1",
            Role = new string('x', 101), ExpirationDays = 7
        };

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "Role" && e.ErrorMessage == "Role cannot exceed 100 characters.");
    }

    [Fact]
    public void CreateInvitation_WhenMessageIsNull_SkipsMessageValidation()
    {
        var validator = new CreateGroupInvitationCommandValidator();
        var command = new CreateGroupInvitationCommand
        {
            TeamId = "team-1", InvitedUserId = "user-1",
            Role = "Developer",
            Message = null, ExpirationDays = 7
        };

        validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateInvitation_WhenMessageTooLong_FailsValidation()
    {
        var validator = new CreateGroupInvitationCommandValidator();
        var command = new CreateGroupInvitationCommand
        {
            TeamId = "team-1", InvitedUserId = "user-1",
            Role = "Developer",
            Message = new string('x', 1001), ExpirationDays = 7
        };

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "Message" &&
            e.ErrorMessage == "Message cannot exceed 1000 characters.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(31)]
    public void CreateInvitation_WhenExpirationDaysOutOfRange_FailsValidation(int days)
    {
        var validator = new CreateGroupInvitationCommandValidator();
        var command = new CreateGroupInvitationCommand
        {
            TeamId = "team-1", InvitedUserId = "user-1",
            Role = "Developer", ExpirationDays = days
        };

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "ExpirationDays" &&
            e.ErrorMessage == "Expiration days must be between 1 and 30.");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(15)]
    [InlineData(30)]
    public void CreateInvitation_WhenExpirationDaysWithinRange_PassesValidation(int days)
    {
        var validator = new CreateGroupInvitationCommandValidator();
        var command = new CreateGroupInvitationCommand
        {
            TeamId = "team-1", InvitedUserId = "user-1",
            Role = "Developer", ExpirationDays = days
        };

        validator.Validate(command).IsValid.Should().BeTrue();
    }

    // ── AcceptGroupInvitationCommandValidator ─────────────────────────────────

    [Fact]
    public void AcceptInvitation_WhenAllFieldsValid_PassesValidation()
    {
        var validator = new AcceptGroupInvitationCommandValidator();
        var command = new AcceptGroupInvitationCommand
        {
            InvitationId = "inv-1"
        };

        validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void AcceptInvitation_WhenInvitationIdEmpty_FailsValidation()
    {
        var validator = new AcceptGroupInvitationCommandValidator();
        var command = new AcceptGroupInvitationCommand
        {
            InvitationId = ""
        };

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "InvitationId" && e.ErrorMessage == "InvitationId is required.");
    }

    // ── DeclineGroupInvitationCommandValidator ────────────────────────────────

    [Fact]
    public void DeclineInvitation_WhenAllFieldsValid_PassesValidation()
    {
        var validator = new DeclineGroupInvitationCommandValidator();
        var command = new DeclineGroupInvitationCommand
        {
            InvitationId = "inv-1"
        };

        validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void DeclineInvitation_WhenInvitationIdEmpty_FailsValidation()
    {
        var validator = new DeclineGroupInvitationCommandValidator();
        var command = new DeclineGroupInvitationCommand
        {
            InvitationId = ""
        };

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "InvitationId");
    }

    // ── RevokeGroupInvitationCommandValidator ─────────────────────────────────

    [Fact]
    public void RevokeInvitation_WhenAllFieldsValid_PassesValidation()
    {
        var validator = new RevokeGroupInvitationCommandValidator();
        var command = new RevokeGroupInvitationCommand
        {
            InvitationId = "inv-1"
        };

        validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void RevokeInvitation_WhenInvitationIdEmpty_FailsValidation()
    {
        var validator = new RevokeGroupInvitationCommandValidator();
        var command = new RevokeGroupInvitationCommand
        {
            InvitationId = ""
        };

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "InvitationId");
    }

}