using Collaboration.Application.Commands.Team;
using Collaboration.Application.Validators;
using Collaboration.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace Collaboration.Application.Tests.Validators;

public class TeamCommandValidatorsTests
{
    // ── AddTeamMemberCommandValidator ─────────────────────────────────────────

    [Fact]
    public void AddTeamMember_WhenAllFieldsValid_PassesValidation()
    {
        var validator = new AddTeamMemberCommandValidator();
        var command = new AddTeamMemberCommand
        {
            TeamId = "team-1", UserId = "user-1",
            Username = "alice", Role = "Developer", RequestedByUserId = "owner-id"
        };

        validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void AddTeamMember_WhenTeamIdEmpty_FailsValidation()
    {
        var validator = new AddTeamMemberCommandValidator();
        var command = new AddTeamMemberCommand
        {
            TeamId = "", UserId = "user-1",
            Username = "alice", Role = "Developer", RequestedByUserId = "owner-id"
        };

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "TeamId" && e.ErrorMessage == "TeamId is required.");
    }

    [Fact]
    public void AddTeamMember_WhenUserIdEmpty_FailsValidation()
    {
        var validator = new AddTeamMemberCommandValidator();
        var command = new AddTeamMemberCommand
        {
            TeamId = "team-1", UserId = "",
            Username = "alice", Role = "Developer", RequestedByUserId = "owner-id"
        };

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "UserId" && e.ErrorMessage == "UserId is required.");
    }

    [Fact]
    public void AddTeamMember_WhenUsernameEmpty_FailsValidation()
    {
        var validator = new AddTeamMemberCommandValidator();
        var command = new AddTeamMemberCommand
        {
            TeamId = "team-1", UserId = "user-1",
            Username = "", Role = "Developer", RequestedByUserId = "owner-id"
        };

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "Username" && e.ErrorMessage == "Username is required.");
    }

    [Fact]
    public void AddTeamMember_WhenUsernameTooLong_FailsValidation()
    {
        var validator = new AddTeamMemberCommandValidator();
        var command = new AddTeamMemberCommand
        {
            TeamId = "team-1", UserId = "user-1",
            Username = new string('x', 101), Role = "Developer", RequestedByUserId = "owner-id"
        };

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "Username" &&
            e.ErrorMessage == "Username cannot exceed 100 characters.");
    }

    [Fact]
    public void AddTeamMember_WhenRoleEmpty_FailsValidation()
    {
        var validator = new AddTeamMemberCommandValidator();
        var command = new AddTeamMemberCommand
        {
            TeamId = "team-1", UserId = "user-1",
            Username = "alice", Role = "", RequestedByUserId = "owner-id"
        };

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "Role" && e.ErrorMessage == "Role is required.");
    }

    [Fact]
    public void AddTeamMember_WhenRoleTooLong_FailsValidation()
    {
        var validator = new AddTeamMemberCommandValidator();
        var command = new AddTeamMemberCommand
        {
            TeamId = "team-1", UserId = "user-1",
            Username = "alice", Role = new string('x', 101), RequestedByUserId = "owner-id"
        };

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "Role" &&
            e.ErrorMessage == "Role cannot exceed 100 characters.");
    }

    [Fact]
    public void AddTeamMember_WhenRequestedByUserIdEmpty_FailsValidation()
    {
        var validator = new AddTeamMemberCommandValidator();
        var command = new AddTeamMemberCommand
        {
            TeamId = "team-1", UserId = "user-1",
            Username = "alice", Role = "Developer", RequestedByUserId = ""
        };

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "RequestedByUserId" &&
            e.ErrorMessage == "RequestedByUserId is required.");
    }

    // ── AddRequiredRoleCommandValidator ───────────────────────────────────────

    [Fact]
    public void AddRequiredRole_WhenAllFieldsValid_PassesValidation()
    {
        var validator = new AddRequiredRoleCommandValidator();
        var command = new AddRequiredRoleCommand
        {
            TeamId = "team-1", Role = "Backend Developer", UserId = "owner-id"
        };

        validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void AddRequiredRole_WithOptionalDescription_PassesValidation()
    {
        var validator = new AddRequiredRoleCommandValidator();
        var command = new AddRequiredRoleCommand
        {
            TeamId = "team-1", Role = "Backend Developer",
            Description = "Needs .NET experience", UserId = "owner-id"
        };

        validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void AddRequiredRole_WhenTeamIdEmpty_FailsValidation()
    {
        var validator = new AddRequiredRoleCommandValidator();
        var command = new AddRequiredRoleCommand
        {
            TeamId = "", Role = "Developer", UserId = "owner-id"
        };

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "TeamId" && e.ErrorMessage == "TeamId is required.");
    }

    [Fact]
    public void AddRequiredRole_WhenRoleEmpty_FailsValidation()
    {
        var validator = new AddRequiredRoleCommandValidator();
        var command = new AddRequiredRoleCommand
        {
            TeamId = "team-1", Role = "", UserId = "owner-id"
        };

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "Role" && e.ErrorMessage == "Role is required.");
    }

    [Fact]
    public void AddRequiredRole_WhenRoleTooLong_FailsValidation()
    {
        var validator = new AddRequiredRoleCommandValidator();
        var command = new AddRequiredRoleCommand
        {
            TeamId = "team-1", Role = new string('x', 101), UserId = "owner-id"
        };

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "Role" &&
            e.ErrorMessage == "Role cannot exceed 100 characters.");
    }

    [Fact]
    public void AddRequiredRole_WhenDescriptionIsNull_SkipsDescriptionValidation()
    {
        var validator = new AddRequiredRoleCommandValidator();
        var command = new AddRequiredRoleCommand
        {
            TeamId = "team-1", Role = "Developer", Description = null, UserId = "owner-id"
        };

        validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void AddRequiredRole_WhenDescriptionTooLong_FailsValidation()
    {
        var validator = new AddRequiredRoleCommandValidator();
        var command = new AddRequiredRoleCommand
        {
            TeamId = "team-1", Role = "Developer",
            Description = new string('x', 501), UserId = "owner-id"
        };

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "Description" &&
            e.ErrorMessage == "Description cannot exceed 500 characters.");
    }

    [Fact]
    public void AddRequiredRole_WhenUserIdEmpty_FailsValidation()
    {
        var validator = new AddRequiredRoleCommandValidator();
        var command = new AddRequiredRoleCommand
        {
            TeamId = "team-1", Role = "Developer", UserId = ""
        };

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "UserId" && e.ErrorMessage == "UserId is required.");
    }
}

public class CreateTeamCommandValidatorTests
{
    private readonly CreateTeamCommandValidator _validator = new();

    private static CreateTeamCommand Valid() => new()
    {
        Name = "My Team", Description = "A description", Category = "Design",
        Tags = ["tag1"], UserId = "user-1", Username = "alice"
    };

    [Fact]
    public void CreateTeam_WhenAllFieldsValid_PassesValidation() =>
        _validator.Validate(Valid()).IsValid.Should().BeTrue();

    [Fact]
    public void CreateTeam_WhenNameEmpty_FailsValidation()
    {
        var result = _validator.Validate(new CreateTeamCommand
        {
            Name = "", Description = "A description", Category = "Design",
            Tags = ["tag1"], UserId = "user-1", Username = "alice"
        });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "Name" && e.ErrorMessage == "Team name is required.");
    }

    [Fact]
    public void CreateTeam_WhenNameTooLong_FailsValidation()
    {
        var result = _validator.Validate(new CreateTeamCommand
        {
            Name = new string('x', 101), Description = "A description", Category = "Design",
            Tags = [], UserId = "user-1", Username = "alice"
        });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "Name" && e.ErrorMessage == "Team name cannot exceed 100 characters.");
    }

    [Fact]
    public void CreateTeam_WhenDescriptionEmpty_FailsValidation()
    {
        var result = _validator.Validate(new CreateTeamCommand
        {
            Name = "My Team", Description = "", Category = "Design",
            Tags = [], UserId = "user-1", Username = "alice"
        });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "Description" && e.ErrorMessage == "Team description is required.");
    }

    [Fact]
    public void CreateTeam_WhenDescriptionTooLong_FailsValidation()
    {
        var result = _validator.Validate(new CreateTeamCommand
        {
            Name = "My Team", Description = new string('x', 1001), Category = "Design",
            Tags = [], UserId = "user-1", Username = "alice"
        });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "Description" &&
            e.ErrorMessage == "Team description cannot exceed 1000 characters.");
    }

    [Fact]
    public void CreateTeam_WhenCategoryEmpty_FailsValidation()
    {
        var result = _validator.Validate(new CreateTeamCommand
        {
            Name = "My Team", Description = "A description", Category = "",
            Tags = [], UserId = "user-1", Username = "alice"
        });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Category");
    }

    [Fact]
    public void CreateTeam_WhenTooManyTags_FailsValidation()
    {
        var result = _validator.Validate(new CreateTeamCommand
        {
            Name = "My Team", Description = "desc", Category = "Design",
            Tags = Enumerable.Range(0, 11).Select(i => $"tag{i}").ToList(),
            UserId = "user-1", Username = "alice"
        });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "Tags" && e.ErrorMessage == "Team cannot have more than 10 tags.");
    }

    [Fact]
    public void CreateTeam_WhenUserIdEmpty_FailsValidation()
    {
        var result = _validator.Validate(new CreateTeamCommand
        {
            Name = "My Team", Description = "desc", Category = "Design",
            Tags = [], UserId = "", Username = "alice"
        });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UserId");
    }

    [Fact]
    public void CreateTeam_WhenUsernameEmpty_FailsValidation()
    {
        var result = _validator.Validate(new CreateTeamCommand
        {
            Name = "My Team", Description = "desc", Category = "Design",
            Tags = [], UserId = "user-1", Username = ""
        });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "Username" && e.ErrorMessage == "Username is required.");
    }

    [Fact]
    public void CreateTeam_WhenUsernameTooLong_FailsValidation()
    {
        var result = _validator.Validate(new CreateTeamCommand
        {
            Name = "My Team", Description = "desc", Category = "Design",
            Tags = [], UserId = "user-1", Username = new string('x', 101)
        });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "Username" &&
            e.ErrorMessage == "Username cannot exceed 100 characters.");
    }
}

public class UpdateTeamCommandValidatorTests
{
    private readonly UpdateTeamCommandValidator _validator = new();

    [Fact]
    public void UpdateTeam_WhenAllFieldsValid_PassesValidation()
    {
        var result = _validator.Validate(new UpdateTeamCommand
        {
            TeamId = "team-1", Name = "My Team", Description = "A description",
            Category = "Design", Tags = ["tag1"], UserId = "user-1"
        });
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void UpdateTeam_WhenTeamIdEmpty_FailsValidation()
    {
        var result = _validator.Validate(new UpdateTeamCommand
        {
            TeamId = "", Name = "My Team", Description = "desc",
            Category = "Design", Tags = [], UserId = "user-1"
        });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TeamId");
    }

    [Fact]
    public void UpdateTeam_WhenNameEmpty_FailsValidation()
    {
        var result = _validator.Validate(new UpdateTeamCommand
        {
            TeamId = "team-1", Name = "", Description = "desc",
            Category = "Design", Tags = [], UserId = "user-1"
        });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void UpdateTeam_WhenNameTooLong_FailsValidation()
    {
        var result = _validator.Validate(new UpdateTeamCommand
        {
            TeamId = "team-1", Name = new string('x', 101), Description = "desc",
            Category = "Design", Tags = [], UserId = "user-1"
        });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "Name" && e.ErrorMessage == "Team name cannot exceed 100 characters.");
    }

    [Fact]
    public void UpdateTeam_WhenDescriptionEmpty_FailsValidation()
    {
        var result = _validator.Validate(new UpdateTeamCommand
        {
            TeamId = "team-1", Name = "My Team", Description = "",
            Category = "Design", Tags = [], UserId = "user-1"
        });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description");
    }

    [Fact]
    public void UpdateTeam_WhenDescriptionTooLong_FailsValidation()
    {
        var result = _validator.Validate(new UpdateTeamCommand
        {
            TeamId = "team-1", Name = "My Team", Description = new string('x', 1001),
            Category = "Design", Tags = [], UserId = "user-1"
        });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "Description" &&
            e.ErrorMessage == "Team description cannot exceed 1000 characters.");
    }

    [Fact]
    public void UpdateTeam_WhenCategoryEmpty_FailsValidation()
    {
        var result = _validator.Validate(new UpdateTeamCommand
        {
            TeamId = "team-1", Name = "My Team", Description = "desc",
            Category = "", Tags = [], UserId = "user-1"
        });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Category");
    }

    [Fact]
    public void UpdateTeam_WhenTooManyTags_FailsValidation()
    {
        var result = _validator.Validate(new UpdateTeamCommand
        {
            TeamId = "team-1", Name = "My Team", Description = "desc",
            Category = "Design",
            Tags = Enumerable.Range(0, 11).Select(i => $"tag{i}").ToList(),
            UserId = "user-1"
        });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Tags");
    }

    [Fact]
    public void UpdateTeam_WhenUserIdEmpty_FailsValidation()
    {
        var result = _validator.Validate(new UpdateTeamCommand
        {
            TeamId = "team-1", Name = "My Team", Description = "desc",
            Category = "Design", Tags = [], UserId = ""
        });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UserId");
    }
}

public class DeleteTeamCommandValidatorTests
{
    [Fact]
    public void DeleteTeam_WhenTeamIdValid_PassesValidation()
    {
        var result = new DeleteTeamCommandValidator().Validate(
            new DeleteTeamCommand { TeamId = "team-1", UserId = "user-1" });
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void DeleteTeam_WhenTeamIdEmpty_FailsValidation()
    {
        var result = new DeleteTeamCommandValidator().Validate(
            new DeleteTeamCommand { TeamId = "", UserId = "user-1" });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "TeamId" && e.ErrorMessage == "TeamId is required.");
    }
}

public class SetTeamStatusCommandValidatorTests
{
    [Fact]
    public void SetTeamStatus_WhenAllFieldsValid_PassesValidation()
    {
        var result = new SetTeamStatusCommandValidator().Validate(new SetTeamStatusCommand
        {
            TeamId = "team-1", Status = TeamStatus.Active, UserId = "user-1"
        });
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void SetTeamStatus_WhenTeamIdEmpty_FailsValidation()
    {
        var result = new SetTeamStatusCommandValidator().Validate(new SetTeamStatusCommand
        {
            TeamId = "", Status = TeamStatus.Active, UserId = "user-1"
        });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TeamId");
    }

    [Fact]
    public void SetTeamStatus_WhenStatusInvalid_FailsValidation()
    {
        var result = new SetTeamStatusCommandValidator().Validate(new SetTeamStatusCommand
        {
            TeamId = "team-1", Status = (TeamStatus)999, UserId = "user-1"
        });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "Status" && e.ErrorMessage == "Invalid team status.");
    }

    [Fact]
    public void SetTeamStatus_WhenUserIdEmpty_FailsValidation()
    {
        var result = new SetTeamStatusCommandValidator().Validate(new SetTeamStatusCommand
        {
            TeamId = "team-1", Status = TeamStatus.Active, UserId = ""
        });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UserId");
    }
}

public class RemoveTeamMemberCommandValidatorTests
{
    [Fact]
    public void RemoveTeamMember_WhenAllFieldsValid_PassesValidation()
    {
        var result = new RemoveTeamMemberCommandValidator().Validate(new RemoveTeamMemberCommand
        {
            TeamId = "team-1", UserId = "user-1", RequestedByUserId = "owner-id"
        });
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void RemoveTeamMember_WhenTeamIdEmpty_FailsValidation()
    {
        var result = new RemoveTeamMemberCommandValidator().Validate(new RemoveTeamMemberCommand
        {
            TeamId = "", UserId = "user-1", RequestedByUserId = "owner-id"
        });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "TeamId" && e.ErrorMessage == "TeamId is required.");
    }

    [Fact]
    public void RemoveTeamMember_WhenUserIdEmpty_FailsValidation()
    {
        var result = new RemoveTeamMemberCommandValidator().Validate(new RemoveTeamMemberCommand
        {
            TeamId = "team-1", UserId = "", RequestedByUserId = "owner-id"
        });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "UserId" && e.ErrorMessage == "UserId is required.");
    }

    [Fact]
    public void RemoveTeamMember_WhenRequestedByUserIdEmpty_FailsValidation()
    {
        var result = new RemoveTeamMemberCommandValidator().Validate(new RemoveTeamMemberCommand
        {
            TeamId = "team-1", UserId = "user-1", RequestedByUserId = ""
        });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "RequestedByUserId" &&
            e.ErrorMessage == "RequestedByUserId is required.");
    }
}