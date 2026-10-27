using FluentAssertions;
using SearchService.Indexing;

namespace SearchService.Tests;

public class TextPartsTests
{
    [Theory]
    [InlineData("olena.k@example.com", "olena.k")]
    [InlineData("olena_k", "olena_k")]
    [InlineData("@example.com", null)]
    [InlineData("  ", null)]
    [InlineData(null, null)]
    public void PublicName_never_keeps_an_email_address(string? userName, string? expected) =>
        TextParts.PublicName(userName).Should().Be(expected);

    [Fact]
    public void Join_skips_empty_parts() =>
        TextParts.Join("a", null, " ", "b").Should().Be("a · b");
}
