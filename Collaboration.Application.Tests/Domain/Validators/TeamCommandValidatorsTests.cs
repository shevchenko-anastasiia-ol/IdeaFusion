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
            TeamId = "team-1",
            MemberUserId = "user-1",
            MemberUsername = "alice", Role = "Developer"
        };

        validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void AddTeamMember_WhenTeamIdEmpty_FailsValidation()
    {
        var validator = new AddTeamMemberCommandValidator();
        var command = new AddTeamMemberCommand
        {
            TeamId = "",
            MemberUserId = "user-1",
            MemberUsername = "alice", Role = "Developer"
        };

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "TeamId" && e.ErrorMessage == "TeamId is required.");
    }

    [Fact]
    public void AddTeamMember_WhenMemberUserIdEmpty_FailsValidation()
    {
        var validator = new AddTeamMemberCommandValidator();
        var command = new AddTeamMemberCommand
        {
            TeamId = "team-1",
            MemberUserId = "",
            MemberUsername = "alice", Role = "Developer"
        };

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "MemberUserId" && e.ErrorMessage == "MemberUserId is required.");
    }

    [Fact]
    public void AddTeamMember_WhenMemberUsernameTooLong_FailsValidation()
    {
        var validator = new AddTeamMemberCommandValidator();
        var command = new AddTeamMemberCommand
        {
            TeamId = "team-1",
            MemberUserId = "user-1",
            MemberUsername = new string('x', 101), Role = "Developer"
        };

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "MemberUsername" &&
            e.ErrorMessage == "MemberUsername cannot exceed 100 characters.");
    }

    [Fact]
    public void AddTeamMember_WhenRoleEmpty_FailsValidation()
    {
        var validator = new AddTeamMemberCommandValidator();
        var command = new AddTeamMemberCommand
        {
            TeamId = "team-1",
            MemberUserId = "user-1",
            MemberUsername = "alice", Role = ""
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
            TeamId = "team-1",
            MemberUserId = "user-1",
            MemberUsername = "alice", Role = new string('x', 101)
        };

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "Role" &&
            e.ErrorMessage == "Role cannot exceed 100 characters.");
    }

    // ── AddRequiredRoleCommandValidator ───────────────────────────────────────

    [Fact]
    public void AddRequiredRole_WhenAllFieldsValid_PassesValidation()
    {
        var validator = new AddRequiredRoleCommandValidator();
        var command = new AddRequiredRoleCommand
        {
            TeamId = "team-1", Role = "Backend Developer"
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
            Description = "Needs .NET experience"
        };

        validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void AddRequiredRole_WhenTeamIdEmpty_FailsValidation()
    {
        var validator = new AddRequiredRoleCommandValidator();
        var command = new AddRequiredRoleCommand
        {
            TeamId = "", Role = "Developer"
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
            TeamId = "team-1", Role = ""
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
            TeamId = "team-1", Role = new string('x', 101)
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
            TeamId = "team-1", Role = "Developer", Description = null
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
            Description = new string('x', 501)
        };

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "Description" &&
            e.ErrorMessage == "Description cannot exceed 500 characters.");
    }

}

public class CreateTeamCommandValidatorTests
{
    private readonly CreateTeamCommandValidator _validator = new();

    private static CreateTeamCommand Valid() => new()
    {
        Name = "My Team", Description = "A description", Category = "Design",
        Tags = ["tag1"]
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
            Tags = ["tag1"]
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
            Tags = []
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
            Tags = []
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
            Tags = []
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
            Tags = []
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
            Tags = Enumerable.Range(0, 11).Select(i => $"tag{i}").ToList()
        });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "Tags" && e.ErrorMessage == "Team cannot have more than 10 tags.");
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
            Category = "Design", Tags = ["tag1"]
        });
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void UpdateTeam_WhenTeamIdEmpty_FailsValidation()
    {
        var result = _validator.Validate(new UpdateTeamCommand
        {
            TeamId = "", Name = "My Team", Description = "desc",
            Category = "Design", Tags = []
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
            Category = "Design", Tags = []
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
            Category = "Design", Tags = []
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
            Category = "Design", Tags = []
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
            Category = "Design", Tags = []
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
            Category = "", Tags = []
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
            Tags = Enumerable.Range(0, 11).Select(i => $"tag{i}").ToList()
        });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Tags");
    }

}

public class DeleteTeamCommandValidatorTests
{
    [Fact]
    public void DeleteTeam_WhenTeamIdValid_PassesValidation()
    {
        var result = new DeleteTeamCommandValidator().Validate(
            new DeleteTeamCommand { TeamId = "team-1" });
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void DeleteTeam_WhenTeamIdEmpty_FailsValidation()
    {
        var result = new DeleteTeamCommandValidator().Validate(
            new DeleteTeamCommand { TeamId = "" });
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
            TeamId = "team-1", Status = TeamStatus.Active
        });
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void SetTeamStatus_WhenTeamIdEmpty_FailsValidation()
    {
        var result = new SetTeamStatusCommandValidator().Validate(new SetTeamStatusCommand
        {
            TeamId = "", Status = TeamStatus.Active
        });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TeamId");
    }

    [Fact]
    public void SetTeamStatus_WhenStatusInvalid_FailsValidation()
    {
        var result = new SetTeamStatusCommandValidator().Validate(new SetTeamStatusCommand
        {
            TeamId = "team-1", Status = (TeamStatus)999
        });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "Status" && e.ErrorMessage == "Invalid team status.");
    }

}

public class RemoveTeamMemberCommandValidatorTests
{
    [Fact]
    public void RemoveTeamMember_WhenAllFieldsValid_PassesValidation()
    {
        var result = new RemoveTeamMemberCommandValidator().Validate(new RemoveTeamMemberCommand
        {
            TeamId = "team-1", MemberUserId = "member-id"
        });
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void RemoveTeamMember_WhenTeamIdEmpty_FailsValidation()
    {
        var result = new RemoveTeamMemberCommandValidator().Validate(new RemoveTeamMemberCommand
        {
            TeamId = "", MemberUserId = "member-id"
        });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "TeamId" && e.ErrorMessage == "TeamId is required.");
    }

    [Fact]
    public void RemoveTeamMember_WhenMemberUserIdEmpty_FailsValidation()
    {
        var result = new RemoveTeamMemberCommandValidator().Validate(new RemoveTeamMemberCommand
        {
            TeamId = "team-1", MemberUserId = ""
        });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "MemberUserId" && e.ErrorMessage == "MemberUserId is required.");
    }
}