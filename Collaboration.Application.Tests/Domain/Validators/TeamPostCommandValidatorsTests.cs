using Collaboration.Application.Commands.TeamPost;
using Collaboration.Application.Validators;
using FluentAssertions;
using Xunit;

namespace Collaboration.Application.Tests.Validators;

public class CreateTeamPostCommandValidatorTests
{
    private readonly CreateTeamPostCommandValidator _validator = new();

    [Fact]
    public void CreateTeamPost_WhenAllFieldsValid_PassesValidation()
    {
        var result = _validator.Validate(new CreateTeamPostCommand
        {
            PostId = "post-1", TeamId = "team-1",
            AuthorUserId = "user-1", AuthorUsername = "alice",
            Title = "My Post Title"
        });
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateTeamPost_WhenPostIdEmpty_FailsValidation()
    {
        var result = _validator.Validate(new CreateTeamPostCommand
        {
            PostId = "", TeamId = "team-1",
            AuthorUserId = "user-1", AuthorUsername = "alice", Title = "Title"
        });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "PostId" && e.ErrorMessage == "PostId is required.");
    }

    [Fact]
    public void CreateTeamPost_WhenTeamIdEmpty_FailsValidation()
    {
        var result = _validator.Validate(new CreateTeamPostCommand
        {
            PostId = "post-1", TeamId = "",
            AuthorUserId = "user-1", AuthorUsername = "alice", Title = "Title"
        });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "TeamId" && e.ErrorMessage == "TeamId is required.");
    }

    [Fact]
    public void CreateTeamPost_WhenAuthorUserIdEmpty_FailsValidation()
    {
        var result = _validator.Validate(new CreateTeamPostCommand
        {
            PostId = "post-1", TeamId = "team-1",
            AuthorUserId = "", AuthorUsername = "alice", Title = "Title"
        });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "AuthorUserId" && e.ErrorMessage == "AuthorUserId is required.");
    }

    [Fact]
    public void CreateTeamPost_WhenAuthorUsernameEmpty_FailsValidation()
    {
        var result = _validator.Validate(new CreateTeamPostCommand
        {
            PostId = "post-1", TeamId = "team-1",
            AuthorUserId = "user-1", AuthorUsername = "", Title = "Title"
        });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "AuthorUsername" &&
            e.ErrorMessage == "AuthorUsername is required.");
    }

    [Fact]
    public void CreateTeamPost_WhenAuthorUsernameTooLong_FailsValidation()
    {
        var result = _validator.Validate(new CreateTeamPostCommand
        {
            PostId = "post-1", TeamId = "team-1",
            AuthorUserId = "user-1", AuthorUsername = new string('x', 101), Title = "Title"
        });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "AuthorUsername" &&
            e.ErrorMessage == "AuthorUsername cannot exceed 100 characters.");
    }

    [Fact]
    public void CreateTeamPost_WhenTitleEmpty_FailsValidation()
    {
        var result = _validator.Validate(new CreateTeamPostCommand
        {
            PostId = "post-1", TeamId = "team-1",
            AuthorUserId = "user-1", AuthorUsername = "alice", Title = ""
        });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "Title" && e.ErrorMessage == "Title is required.");
    }

    [Fact]
    public void CreateTeamPost_WhenTitleTooLong_FailsValidation()
    {
        var result = _validator.Validate(new CreateTeamPostCommand
        {
            PostId = "post-1", TeamId = "team-1",
            AuthorUserId = "user-1", AuthorUsername = "alice",
            Title = new string('x', 201)
        });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "Title" &&
            e.ErrorMessage == "Title cannot exceed 200 characters.");
    }
}

public class UpdateTeamPostTitleCommandValidatorTests
{
    private readonly UpdateTeamPostTitleCommandValidator _validator = new();

    [Fact]
    public void UpdateTeamPostTitle_WhenAllFieldsValid_PassesValidation()
    {
        var result = _validator.Validate(new UpdateTeamPostTitleCommand
        {
            PostId = "post-1", TeamId = "team-1", Title = "New Title", UserId = "user-1"
        });
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void UpdateTeamPostTitle_WhenPostIdEmpty_FailsValidation()
    {
        var result = _validator.Validate(new UpdateTeamPostTitleCommand
        {
            PostId = "", TeamId = "team-1", Title = "New Title", UserId = "user-1"
        });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "PostId" && e.ErrorMessage == "PostId is required.");
    }

    [Fact]
    public void UpdateTeamPostTitle_WhenTeamIdEmpty_FailsValidation()
    {
        var result = _validator.Validate(new UpdateTeamPostTitleCommand
        {
            PostId = "post-1", TeamId = "", Title = "New Title", UserId = "user-1"
        });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "TeamId" && e.ErrorMessage == "TeamId is required.");
    }

    [Fact]
    public void UpdateTeamPostTitle_WhenTitleEmpty_FailsValidation()
    {
        var result = _validator.Validate(new UpdateTeamPostTitleCommand
        {
            PostId = "post-1", TeamId = "team-1", Title = "", UserId = "user-1"
        });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "Title" && e.ErrorMessage == "Title is required.");
    }

    [Fact]
    public void UpdateTeamPostTitle_WhenTitleTooLong_FailsValidation()
    {
        var result = _validator.Validate(new UpdateTeamPostTitleCommand
        {
            PostId = "post-1", TeamId = "team-1",
            Title = new string('x', 201), UserId = "user-1"
        });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "Title" &&
            e.ErrorMessage == "Title cannot exceed 200 characters.");
    }

    [Fact]
    public void UpdateTeamPostTitle_WhenUserIdEmpty_FailsValidation()
    {
        var result = _validator.Validate(new UpdateTeamPostTitleCommand
        {
            PostId = "post-1", TeamId = "team-1", Title = "New Title", UserId = ""
        });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "UserId" && e.ErrorMessage == "UserId is required.");
    }
}

public class DeleteTeamPostCommandValidatorTests
{
    private readonly DeleteTeamPostCommandValidator _validator = new();

    [Fact]
    public void DeleteTeamPost_WhenAllFieldsValid_PassesValidation()
    {
        _validator.Validate(new DeleteTeamPostCommand
        {
            PostId = "post-1", TeamId = "team-1", UserId = "user-1"
        }).IsValid.Should().BeTrue();
    }

    [Fact]
    public void DeleteTeamPost_WhenPostIdEmpty_FailsValidation()
    {
        var result = _validator.Validate(new DeleteTeamPostCommand
        {
            PostId = "", TeamId = "team-1", UserId = "user-1"
        });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "PostId" && e.ErrorMessage == "PostId is required.");
    }

    [Fact]
    public void DeleteTeamPost_WhenTeamIdEmpty_FailsValidation()
    {
        var result = _validator.Validate(new DeleteTeamPostCommand
        {
            PostId = "post-1", TeamId = "", UserId = "user-1"
        });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "TeamId" && e.ErrorMessage == "TeamId is required.");
    }

    [Fact]
    public void DeleteTeamPost_WhenUserIdEmpty_FailsValidation()
    {
        var result = _validator.Validate(new DeleteTeamPostCommand
        {
            PostId = "post-1", TeamId = "team-1", UserId = ""
        });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "UserId" && e.ErrorMessage == "UserId is required.");
    }
}