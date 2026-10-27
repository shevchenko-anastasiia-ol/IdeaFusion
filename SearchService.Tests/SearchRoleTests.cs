using FluentAssertions;
using SearchService.Indexing;
using SearchService.Search;
using Docs = SearchService.Tests.SampleDocuments;

namespace SearchService.Tests;

/// <summary>Stage 4: the role directory, teams found by their open vacancies, role autocomplete.</summary>
public sealed class SearchRoleTests(SearchDatabaseFixture db) : IClassFixture<SearchDatabaseFixture>
{
    private SearchQueryService Service()
    {
        Skip.If(db.SkipReason is not null, db.SkipReason);
        return new SearchQueryService(db.Index!, new TagCatalogCache(db.Index!));
    }

    private async Task<string[]> Ids(SearchRequest request) =>
        (await Service().SearchAsync(request, default)).Items.Select(i => i.Id).ToArray();

    [SkippableFact]
    public async Task Role_directory_matches_across_languages()
    {
        (await Ids(new() { Role = "illustrator", Kind = "user" })).Should().Equal(Docs.Olena.Id);
        (await Ids(new() { Role = "artist", Kind = "vacancy" })).Should().Equal(Docs.ModelerVacancy.Id);
        (await Ids(new() { Role = "animator" })).Should().Equal(Docs.AnimationTeam.Id);
        (await Ids(new() { Role = "fybvfnjh" })).Should().BeEmpty("the wrong layout is handled by the autocomplete, not the filter");
    }

    [SkippableFact]
    public async Task Team_looking_for_a_role_is_found_by_its_open_vacancy()
    {
        (await Ids(new() { Kind = "team", HasOpenVacancies = true, Role = "художник" })).Should().Equal(Docs.AnimationTeam.Id);
        (await Ids(new() { Kind = "team", Role = "3d artist" })).Should().Equal(Docs.AnimationTeam.Id);
        (await Ids(new() { Kind = "team", Role = "поет" })).Should().BeEmpty();
    }

    [SkippableFact]
    public async Task Role_autocomplete_by_prefix_synonym_layout_and_typo()
    {
        var service = Service();
        (await service.RolesAsync("худ", 5, default)).Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new RoleSuggestion { Label = "3D художник", Vacancies = 1 });
        (await service.RolesAsync("illustr", 5, default)).Select(r => r.Label).Should().Equal("Ілюстраторка");
        (await service.RolesAsync("fybvfnjh", 5, default)).Select(r => r.Label).Should().Equal("Аніматор 2D");
        (await service.RolesAsync("анниматор", 5, default)).Select(r => r.Label).Should().Equal("Аніматор 2D");

        var top = await service.RolesAsync(null, 10, default);
        top.Should().HaveCount(3);
        top[0].Label.Should().Be("3D художник");
    }
}
