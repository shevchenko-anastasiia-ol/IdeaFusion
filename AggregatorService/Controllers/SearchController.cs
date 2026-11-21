using AggregatorService.Clients;
using AggregatorService.DTO;
using Microsoft.AspNetCore.Mvc;

namespace AggregatorService.Controllers;

[ApiController]
[Route("api/aggregator/search")]
public class SearchController : ControllerBase
{
    private readonly ContentClient _contentClient;
    private readonly CollaborationClient _collaborationClient;

    public SearchController(ContentClient contentClient, CollaborationClient collaborationClient)
    {
        _contentClient = contentClient;
        _collaborationClient = collaborationClient;
    }

    [HttpGet("unified")]
    public async Task<IActionResult> UnifiedSearch(
        [FromQuery] string q,
        [FromQuery] string? type = "all", // all, post, vacancy
        [FromQuery] string? category = null,
        [FromQuery] string? tags = null,
        CancellationToken ct = default)
    {
        var tasks = new List<Task>();
        Task<List<AggregatorPostDto>> postsTask = null;
        Task<object>? vacanciesTask = null;

        if (type == "all" || type == "post")
        {
            postsTask = _contentClient.SearchPostsAsync(q, category, tags?.Split(','), ct);
            tasks.Add(postsTask);
        }

        if (type == "all" || type == "vacancy")
        {
            vacanciesTask = _collaborationClient.SearchVacanciesAsync(q, category, null, true, 1, 20, ct);
            tasks.Add(vacanciesTask);
        }

        await Task.WhenAll(tasks);

        return Ok(new
        {
            Posts = postsTask?.Result ?? new List<AggregatorPostDto>(),
            Vacancies = vacanciesTask?.Result ?? new { Items = new List<object>(), TotalCount = 0 }
        });
    }
}
