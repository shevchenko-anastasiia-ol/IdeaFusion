using ContentBLL.DTO.Tag;
using ContentBLL.Validators.Tag;
using FluentAssertions;
using Xunit;

namespace Content.Tests.Validators;

public class TagCreateDtoValidatorTests
{
    private readonly TagCreateDtoValidator _validator = new();

    [Fact]
    public void Valid_LowercaseWithDigits_PassesValidation()
    {
        var dto = new TagCreateDto { Name = "dotnet9" };
        _validator.Validate(dto).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Valid_LowercaseWithHyphen_PassesValidation()
    {
        var dto = new TagCreateDto { Name = "clean-code" };
        _validator.Validate(dto).IsValid.Should().BeTrue();
    }

    [Fact]
    public void EmptyName_FailsValidation()
    {
        var dto = new TagCreateDto { Name = "" };
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void NameTooLong_FailsValidation()
    {
        var dto = new TagCreateDto { Name = new string('a', 51) };
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void UppercaseLetters_FailsValidation()
    {
        var dto = new TagCreateDto { Name = "DotNet" };
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void SpecialCharacters_FailsValidation()
    {
        var dto = new TagCreateDto { Name = "tag name" };
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void NameWithUnderscores_FailsValidation()
    {
        var dto = new TagCreateDto { Name = "tag_name" };
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void MaxLengthName_PassesValidation()
    {
        var dto = new TagCreateDto { Name = new string('a', 50) };
        _validator.Validate(dto).IsValid.Should().BeTrue();
    }
}