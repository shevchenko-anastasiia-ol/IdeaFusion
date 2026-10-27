using FluentAssertions;
using SearchService.Search;

namespace SearchService.Tests;

public class SearchFiltersTests
{
    [Fact]
    public void Filters_survive_a_json_round_trip()
    {
        var filters = SearchFilters.Of(new SearchRequest
        {
            Kind = " vacancy ", Tags = ["3d", "animation"], TagMode = TagMatch.All, Role = "художник",
            From = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc), HasOpenVacancies = true, Sort = SearchSort.New,
        });
        var json = filters.ToJson();
        json.Should().Contain("\"tagMode\":\"all\"").And.Contain("\"sort\":\"new\"").And.NotContain("minLikes");
        SearchFilters.FromJson(json).Should().Be(filters);
        filters.ToRequest("q").Should().BeEquivalentTo(new SearchRequest
        {
            Query = "q", Kind = "vacancy", Tags = ["3d", "animation"], TagMode = TagMatch.All, Role = "художник",
            From = filters.From, HasOpenVacancies = true, Sort = SearchSort.New,
        });
    }

    [Fact]
    public void Fingerprint_ignores_case_spacing_and_tag_order()
    {
        var a = SearchFilters.Of(new SearchRequest { Tags = ["animation", "3d"] });
        var b = SearchFilters.Of(new SearchRequest { Tags = ["3d", "animation", "3d"] });
        SearchFilters.Fingerprint("  Ілюстрація ", a).Should().Be(SearchFilters.Fingerprint("ілюстрація", b));
        SearchFilters.Fingerprint("ілюстрація", a).Should().NotBe(SearchFilters.Fingerprint("ілюстрація", new SearchFilters()));
    }

    [Fact]
    public void Only_the_sort_order_leaves_filters_empty()
    {
        SearchFilters.Of(new SearchRequest { Sort = SearchSort.Popular, Kind = " " }).IsEmpty.Should().BeTrue();
        SearchFilters.Of(new SearchRequest { MinLikes = 1 }).IsEmpty.Should().BeFalse();
        SearchFilters.FromJson(null).IsEmpty.Should().BeTrue();
    }
}
