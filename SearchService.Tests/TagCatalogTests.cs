using FluentAssertions;
using SearchService.Indexing;
using SearchService.Normalization;

namespace SearchService.Tests;

public class TagCatalogTests
{
    [Theory]
    [InlineData("#3D", "3d")]
    [InlineData("3d", "3d")]
    [InlineData("3-д", "3d")]
    [InlineData("3 Д", "3d")]
    [InlineData("UI/UX", "uiux")]
    [InlineData("ui-ux", "uiux")]
    [InlineData("Street_Art", "streetart")]
    [InlineData("Ілюстрація", "iliustratsiia")]
    [InlineData("#", "")]
    public void TagKey_ignores_case_hash_separators_and_script(string tag, string expected) =>
        TextNormalizer.TagKey(tag).Should().Be(expected);

    private static readonly TagCatalog Catalog = TagCatalog.Build(
        [("illustration", "illustration"), ("ілюстрація", "illustration"), ("ux", "ui-ux"), ("ui-ux", "ui-ux")],
        [("illustration", "Арт"), ("ui-ux", "UI/UX"), ("design", "Дизайн")]);

    [Theory]
    [InlineData("Ілюстрація", "illustration")]
    [InlineData("#illustration", "illustration")]
    [InlineData("UX", "uiux")]
    [InlineData("UI/UX", "uiux")]
    [InlineData("figma", "figma")]
    public void Key_maps_spellings_and_synonyms_to_one_tag(string tag, string expected) =>
        Catalog.Key(tag).Should().Be(expected);

    [Fact]
    public void Categories_follow_synonyms_and_accept_category_names()
    {
        Catalog.Categories("ілюстрація").Should().Equal("Арт");
        Catalog.Categories("ux").Should().Equal("UI/UX");
        Catalog.Categories("дизайн").Should().Equal("Дизайн"); // a team's own category name
        Catalog.Categories("figma").Should().BeEmpty();
    }

    [Fact]
    public void Synonym_group_lists_every_name_and_is_labelled_by_the_canonical_tag()
    {
        Catalog.Synonyms("illustration").Should().BeEquivalentTo("illustration", "ілюстрація");
        Catalog.Label("illustration").Should().Be("illustration");
        Catalog.Label("figma").Should().BeNull();
    }

    [Fact]
    public void Indexed_row_carries_canonical_keys_and_synonyms_in_the_tag_text()
    {
        var row = SearchIndexWriter.BuildRow(new SourceDocument
        {
            Kind = DocumentKinds.Post, SourceId = "1", Title = "t", Tags = ["#Illustration", "ілюстрація", "UX"],
        }, Catalog);

        row.TagKeys.Should().BeEquivalentTo("illustration", "uiux");
        row.Categories.Should().BeEquivalentTo("Арт", "UI/UX");
        row.TagText.Should().Contain("ілюстрація").And.Contain("illustration").And.Contain("ui-ux");
    }
}
