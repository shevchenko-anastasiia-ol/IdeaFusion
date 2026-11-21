using ContentBLL.DTO.Comment;
using ContentBLL.Validators.Comment;
using FluentAssertions;
using Xunit;

namespace Content.Tests.Validators;

public class CommentCreateDtoValidatorTests
{
    private readonly CommentCreateDtoValidator _validator = new();

    private static CommentCreateDto ValidDto() => new()
    {
        PostId = 1,
        Body = "Great post!"
    };

    [Fact]
    public void Valid_Dto_PassesValidation()
    {
        _validator.Validate(ValidDto()).IsValid.Should().BeTrue();
    }

    [Fact]
    public void PostIdZero_FailsValidation()
    {
        var dto = ValidDto();
        dto.PostId = 0;
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PostId");
    }

    [Fact]
    public void EmptyBody_FailsValidation()
    {
        var dto = ValidDto();
        dto.Body = "";
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Body");
    }

    [Fact]
    public void BodyTooLong_FailsValidation()
    {
        var dto = ValidDto();
        dto.Body = new string('X', 2001);
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Body");
    }

    [Fact]
    public void ValidParentCommentId_PassesValidation()
    {
        var dto = ValidDto();
        dto.ParentCommentId = 5;
        _validator.Validate(dto).IsValid.Should().BeTrue();
    }

    [Fact]
    public void ParentCommentIdZero_FailsValidation()
    {
        var dto = ValidDto();
        dto.ParentCommentId = 0;
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ParentCommentId");
    }

    [Fact]
    public void NullParentCommentId_PassesValidation()
    {
        var dto = ValidDto();
        dto.ParentCommentId = null;
        _validator.Validate(dto).IsValid.Should().BeTrue();
    }
}

public class CommentUpdateDtoValidatorTests
{
    private readonly CommentUpdateDtoValidator _validator = new();

    [Fact]
    public void Valid_Dto_PassesValidation()
    {
        var dto = new CommentUpdateDto { Body = "Updated body" };
        _validator.Validate(dto).IsValid.Should().BeTrue();
    }

    [Fact]
    public void EmptyBody_FailsValidation()
    {
        var dto = new CommentUpdateDto { Body = "" };
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Body");
    }

    [Fact]
    public void BodyTooLong_FailsValidation()
    {
        var dto = new CommentUpdateDto { Body = new string('Z', 2001) };
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void BodyTooLong_ReportsLengthError()
    {
        var result = _validator.Validate(new CommentUpdateDto { Body = new string('U', 101) });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Body");
    }
}