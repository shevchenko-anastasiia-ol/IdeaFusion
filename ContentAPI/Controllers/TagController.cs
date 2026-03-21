using ContentBLL.DTO.Tag;
using ContentBLL.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ContentAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TagController : ControllerBase
{
    private readonly ITagService _tagService;
 
    public TagController(ITagService tagService)
    {
        _tagService = tagService;
    }
 
    // GET: api/tag
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TagDto>>> GetAll(CancellationToken ct)
    {
        var tags = await _tagService.GetAllAsync(ct);
        return Ok(tags);
    }
 
    // GET: api/tag/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<TagDto>> GetById(int id, CancellationToken ct)
    {
        var tag = await _tagService.GetByIdAsync(id, ct);
        if (tag is null)
            return NotFound(new ProblemDetails { Title = $"Тег з id={id} не знайдено." });
 
        return Ok(tag);
    }
 
    // GET: api/tag/by-post/{postId}
    [HttpGet("by-post/{postId:int}")]
    public async Task<ActionResult<IEnumerable<TagDto>>> GetByPost(int postId, CancellationToken ct)
    {
        var tags = await _tagService.GetByPostIdAsync(postId, ct);
        return Ok(tags);
    }
 
    // POST: api/tag
    [HttpPost]
    public async Task<ActionResult<TagDto>> Create([FromBody] TagCreateDto dto, CancellationToken ct)
    {
        try
        {
            var tag = await _tagService.CreateAsync(dto, ct);
            return CreatedAtAction(nameof(GetById), new { id = tag.TagId }, tag);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ProblemDetails { Title = ex.Message });
        }
    }
 
    // DELETE: api/tag/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        try
        {
            await _tagService.DeleteAsync(id, ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Title = ex.Message });
        }
    }
}