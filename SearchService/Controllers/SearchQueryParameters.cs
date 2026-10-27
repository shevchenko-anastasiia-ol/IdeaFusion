using Microsoft.AspNetCore.Mvc;
using SearchService.Search;

namespace SearchService.Controllers;

/// <summary>Query string of GET /api/search.</summary>
public sealed class SearchQueryParameters
{
    /// <summary>The query; empty lists documents by the chosen sort.</summary>
    public string? Q { get; set; }

    /// <summary>post, team, vacancy or user.</summary>
    public string? Kind { get; set; }

    /// <summary>A category name, e.g. "Музика".</summary>
    public string? Category { get; set; }

    /// <summary>Tag filter, repeatable; any spelling or synonym ("3-д", "#3D").</summary>
    [FromQuery(Name = "tag")]
    public string[]? Tags { get; set; }

    /// <summary>any (default): at least one of the tags; all: every tag.</summary>
    public TagMatch TagMode { get; set; } = TagMatch.Any;

    /// <summary>Created at or after, ISO 8601.</summary>
    public DateTime? From { get; set; }

    /// <summary>Created before, ISO 8601.</summary>
    public DateTime? To { get; set; }

    public int? MinLikes { get; set; }

    /// <summary>Only teams with open vacancies (other kinds are kept).</summary>
    public bool HasOpenVacancies { get; set; }

    /// <summary>Role or specialization, matched fuzzily.</summary>
    public string? Role { get; set; }

    /// <summary>User id: their posts and profile.</summary>
    public string? Author { get; set; }

    /// <summary>Team id: the team, its vacancies and posts.</summary>
    public string? Team { get; set; }

    /// <summary>relevance (default), new or popular.</summary>
    public SearchSort Sort { get; set; } = SearchSort.Relevance;

    /// <summary>nextCursor of the previous page.</summary>
    public string? Cursor { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    public SearchRequest ToRequest() => new()
    {
        Query = Q, Kind = Kind, Category = Category, Tags = Tags ?? [], TagMode = TagMode,
        From = From, To = To, MinLikes = MinLikes, HasOpenVacancies = HasOpenVacancies, Role = Role,
        AuthorId = Author, TeamId = Team, Sort = Sort, Cursor = Cursor, Page = Page, PageSize = PageSize,
    };
}
