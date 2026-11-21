using ContentBLL.DTO.Post;
using ContentBLL.Validators;
using FluentAssertions;
using Xunit;

namespace Content.Tests.Validators;

public class PostCreateDtoValidatorTests
{
    private readonly PostCreateDtoValidator _validator = new();

    private static PostCreateDto ValidDto() => new()
    {
        Title = "Correct Title",
        Description = "Some description",
        // No collaboration snapshot means a personal post, and the author comes
        // from the token rather than the body.
        CollaborationSnapshotId = null,
        TagIds = new List<int> { 1, 2 }
    };

    [Fact]
    public void Valid_Dto_PassesValidation()
    {
        var result = _validator.Validate(ValidDto());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void EmptyTitle_FailsValidation()
    {
        var dto = ValidDto();
        dto.Title = "";
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Title");
    }

    [Fact]
    public void TitleTooLong_FailsValidation()
    {
        var dto = ValidDto();
        dto.Title = new string('A', 201);
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Title");
    }

    [Fact]
    public void DescriptionTooLong_FailsValidation()
    {
        var dto = ValidDto();
        dto.Description = new string('X', 2001);
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description");
    }

    [Fact]
    public void InvalidExternalLink_FailsValidation()
    {
        var dto = ValidDto();
        dto.ExternalLink = "not-a-url";
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ExternalLink");
    }

    [Fact]
    public void ValidExternalLink_PassesValidation()
    {
        var dto = ValidDto();
        dto.ExternalLink = "https://example.com/project";
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void WithCollaborationSnapshot_PassesValidation()
    {
        var dto = ValidDto();
        dto.CollaborationSnapshotId = 2;
        _validator.Validate(dto).IsValid.Should().BeTrue();
    }

    [Fact]
    public void CollaborationSnapshotIdZero_FailsValidation()
    {
        var dto = ValidDto();
        dto.CollaborationSnapshotId = 0;
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CollaborationSnapshotId");
    }

    [Fact]
    public void NegativeTagId_FailsValidation()
    {
        var dto = ValidDto();
        dto.TagIds = new List<int> { -1, 2 };
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TagIds");
    }

    [Fact]
    public void EmptyTagIds_PassesValidation()
    {
        var dto = ValidDto();
        dto.TagIds = new List<int>();
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeTrue();
    }
}

public class PostUpdateDtoValidatorTests
{
    private readonly PostUpdateDtoValidator _validator = new();

    private static PostUpdateDto ValidDto() => new()
    {
        Title = "Updated Title",
        TagIds = new List<int> { 1 }
    };

    [Fact]
    public void Valid_Dto_PassesValidation()
    {
        _validator.Validate(ValidDto()).IsValid.Should().BeTrue();
    }

    [Fact]
    public void EmptyTitle_FailsValidation()
    {
        var dto = ValidDto();
        dto.Title = "";
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Title");
    }

    [Fact]
    public void TitleTooLong_FailsValidation()
    {
        var dto = ValidDto();
        dto.Title = new string('B', 201);
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void InvalidExternalLink_FailsValidation()
    {
        var dto = ValidDto();
        dto.ExternalLink = "bad-url";
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void NegativeTagId_FailsValidation()
    {
        var dto = ValidDto();
        dto.TagIds = new List<int> { 0 };
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
    }
}