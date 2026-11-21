using FluentAssertions;
using SearchService.Normalization;

namespace SearchService.Tests;

public class TextNormalizerTests
{
    [Theory]
    [InlineData("  Hello,   World! ", "hello world")]
    [InlineData("Café crème", "cafe creme")]
    [InlineData("П'ять зірок", "пять зірок")]
    [InlineData("пʼять", "пять")]
    [InlineData("Їжак і йогурт", "їжак і йогурт")]
    [InlineData("UI/UX-дизайн 3D", "ui ux дизайн 3d")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void Normalize_lowercases_and_splits_words(string? input, string expected) =>
        TextNormalizer.Normalize(input).Should().Be(expected);

    [Theory]
    [InlineData("тест", "test")]
    [InlineData("ілюстрація", "iliustratsiia")]
    [InlineData("олена коваленко", "olena kovalenko")]
    [InlineData("згода", "zghoda")]
    [InlineData("юля яна єва їжак йосип", "yulia yana yeva yizhak yosyp")]
    [InlineData("щастя", "shchastia")]
    [InlineData("хмельницький", "khmelnytskyi")]
    [InlineData("3d art", "3d art")]
    public void Transliterate_follows_KMU_2010(string input, string expected) =>
        TextNormalizer.Transliterate(input).Should().Be(expected);

    [Theory]
    [InlineData("еуые", "test")]
    [InlineData("ntcn", "тест")]
    [InlineData("ghbdsn", "привіт")]
    [InlineData("vepbrf", "музика")]
    [InlineData("[jxe", "хочу")]
    [InlineData("руддщ", "hello")]
    public void SwapLayout_retypes_with_the_other_keyboard(string input, string expected) =>
        TextNormalizer.SwapLayout(input).Should().Be(expected);

    [Fact]
    public void SwapLayout_returns_null_without_letters() =>
        TextNormalizer.SwapLayout("123 !!").Should().BeNull();
}
