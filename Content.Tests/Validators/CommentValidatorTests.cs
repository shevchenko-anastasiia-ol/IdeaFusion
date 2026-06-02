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
        PostAuthorId = 2,
        Body = "Great post!",
        CreatedBy = "alice"
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
    public void PostAuthorIdZero_FailsValidation()
    {
        var dto = ValidDto();
        dto.PostAuthorId = 0;
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PostAuthorId");
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
    public void EmptyCreatedBy_FailsValidation()
    {
        var dto = ValidDto();
        dto.CreatedBy = "";
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CreatedBy");
    }

    [Fact]
    public void CreatedByTooLong_FailsValidation()
    {
        var dto = ValidDto();
        dto.CreatedBy = new string('A', 101);
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CreatedBy");
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
        var dto = new CommentUpdateDto { Body = "Updated body", UpdatedBy = "alice" };
        _validator.Validate(dto).IsValid.Should().BeTrue();
    }

    [Fact]
    public void EmptyBody_FailsValidation()
    {
        var dto = new CommentUpdateDto { Body = "", UpdatedBy = "alice" };
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Body");
    }

    [Fact]
    public void BodyTooLong_FailsValidation()
    {
        var dto = new CommentUpdateDto { Body = new string('Z', 2001), UpdatedBy = "alice" };
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void EmptyUpdatedBy_FailsValidation()
    {
        var dto = new CommentUpdateDto { Body = "Text", UpdatedBy = "" };
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UpdatedBy");
    }

    [Fact]
    public void UpdatedByTooLong_FailsValidation()
    {
        var dto = new CommentUpdateDto { Body = "Text", UpdatedBy = new string('U', 101) };
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
    }
}