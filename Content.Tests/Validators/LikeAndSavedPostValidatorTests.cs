using ContentBLL.DTO.Like;
using ContentBLL.DTO.SavedPost;
using ContentBLL.Validators.Like;
using ContentBLL.Validators.SavedPost;
using FluentAssertions;
using Xunit;

namespace Content.Tests.Validators;

public class LikeCreateDtoValidatorTests
{
    private readonly LikeCreateDtoValidator _validator = new();

    [Fact]
    public void Valid_Dto_PassesValidation()
    {
        var dto = new LikeCreateDto { PostId = 1 };
        _validator.Validate(dto).IsValid.Should().BeTrue();
    }

    [Fact]
    public void PostIdZero_FailsValidation()
    {
        var dto = new LikeCreateDto { PostId = 0 };
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PostId");
    }

    [Fact]
    public void PostIdNegative_FailsValidation()
    {
        var dto = new LikeCreateDto { PostId = -1 };
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PostId");
    }
}

public class SavedPostCreateDtoValidatorTests
{
    private readonly SavedPostCreateDtoValidator _validator = new();

    [Fact]
    public void Valid_Dto_PassesValidation()
    {
        var dto = new SavedPostCreateDto { PostId = 1 };
        _validator.Validate(dto).IsValid.Should().BeTrue();
    }

    [Fact]
    public void PostIdZero_FailsValidation()
    {
        var dto = new SavedPostCreateDto { PostId = 0 };
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PostId");
    }

    [Fact]
    public void PostIdNegative_FailsValidation()
    {
        var dto = new SavedPostCreateDto { PostId = -1 };
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
    }
}