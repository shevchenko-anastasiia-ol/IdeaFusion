using Collaboration.Application.Commands.Team;
using Collaboration.Application.Queries.Team;
using Collaboration.Domain.Entities;
using Collaboration.Domain.Entities.Parameters;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Collaboration.API.Controllers;

[Route("api/[controller]")]
public class TeamController : BaseApiController
{
    public TeamController(IMediator mediator) : base(mediator) { }
 
    // GET: api/team/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        try
        {
            var team = await _mediator.Send(new GetTeamByIdQuery { TeamId = id }, cancellationToken);
            if (team is null) return NotFound();
 
            AddETagHeader(GenerateETag(team.UpdatedAt ?? team.CreatedAt));
            return Ok(team);
        }
        catch (Exception ex) { return HandleException(ex); }
    }
 
    // GET: api/team
    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] TeamParameters parameters, CancellationToken cancellationToken)
    {
        try
        {
            var teams = await _mediator.Send(new GetTeamsPagedQuery { Parameters = parameters }, cancellationToken);
            return Ok(teams);
        }
        catch (Exception ex) { return HandleException(ex); }
    }
 
    // GET: api/team/search
    [HttpGet("search")]
    public async Task<IActionResult> SearchByName([FromQuery] string name, CancellationToken cancellationToken)
    {
        try
        {
            var teams = await _mediator.Send(new SearchTeamsByNameQuery { Name = name }, cancellationToken);
            return Ok(teams);
        }
        catch (Exception ex) { return HandleException(ex); }
    }
 
    // GET: api/team/status/{status}
    [HttpGet("status/{status}")]
    public async Task<IActionResult> GetByStatus(TeamStatus status, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            var teams = await _mediator.Send(new GetTeamsByStatusQuery
            {
                Status = status,
                PageNumber = pageNumber,
                PageSize = pageSize
            }, cancellationToken);
            return Ok(teams);
        }
        catch (Exception ex) { return HandleException(ex); }
    }
 
    // GET: api/team/category/{category}
    [HttpGet("category/{category}")]
    public async Task<IActionResult> GetByCategory(string category, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            var teams = await _mediator.Send(new GetTeamsByCategoryQuery
            {
                Category = category,
                PageNumber = pageNumber,
                PageSize = pageSize
            }, cancellationToken);
            return Ok(teams);
        }
        catch (Exception ex) { return HandleException(ex); }
    }
 
    // GET: api/team/member/{userId}
    [HttpGet("member/{userId}")]
    public async Task<IActionResult> GetByMember(string userId, CancellationToken cancellationToken)
    {
        try
        {
            var teams = await _mediator.Send(new GetTeamsByMemberQuery { UserId = userId }, cancellationToken);
            return Ok(teams);
        }
        catch (Exception ex) { return HandleException(ex); }
    }
 
    // POST: api/team
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTeamCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var team = await _mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = team.Id }, team);
        }
        catch (Exception ex) { return HandleException(ex); }
    }
 
    // PUT: api/team/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateTeamCommand command, CancellationToken cancellationToken)
    {
        try
        {
            if (command.TeamId != id)
                return BadRequest(new { message = "ID mismatch." });
 
            var requestETag = GetIfMatchHeader();
            var existing = await _mediator.Send(new GetTeamByIdQuery { TeamId = id }, cancellationToken);
            if (existing is null) return NotFound();
 
            var currentETag = GenerateETag(existing.UpdatedAt ?? existing.CreatedAt);
            if (!ValidateETag(requestETag, currentETag))
                return StatusCode(412, new { message = "ETag mismatch. The resource has been modified." });
 
            var updated = await _mediator.Send(command, cancellationToken);
            AddETagHeader(GenerateETag(updated.UpdatedAt ?? updated.CreatedAt));
            return Ok(updated);
        }
        catch (Exception ex) { return HandleException(ex); }
    }
 
    // PATCH: api/team/{id}/status
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> SetStatus(string id, [FromBody] SetTeamStatusCommand command, CancellationToken cancellationToken)
    {
        try
        {
            if (command.TeamId != id)
                return BadRequest(new { message = "ID mismatch." });
 
            var team = await _mediator.Send(command, cancellationToken);
            return Ok(team);
        }
        catch (Exception ex) { return HandleException(ex); }
    }
 
    // DELETE: api/team/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, [FromBody] DeleteTeamCommand command, CancellationToken cancellationToken)
    {
        try
        {
            if (command.TeamId != id)
                return BadRequest(new { message = "ID mismatch." });
 
            await _mediator.Send(command, cancellationToken);
            return NoContent();
        }
        catch (Exception ex) { return HandleException(ex); }
    }
 
    // POST: api/team/{id}/members
    [HttpPost("{id}/members")]
    public async Task<IActionResult> AddMember(string id, [FromBody] AddTeamMemberCommand command, CancellationToken cancellationToken)
    {
        try
        {
            if (command.TeamId != id)
                return BadRequest(new { message = "ID mismatch." });
 
            var team = await _mediator.Send(command, cancellationToken);
            return Ok(team);
        }
        catch (Exception ex) { return HandleException(ex); }
    }
 
    // DELETE: api/team/{id}/members/{userId}
    [HttpDelete("{id}/members/{userId}")]
    public async Task<IActionResult> RemoveMember(string id, string userId, [FromQuery] string requestedByUserId, CancellationToken cancellationToken)
    {
        try
        {
            var team = await _mediator.Send(new RemoveTeamMemberCommand
            {
                TeamId = id,
                UserId = userId,
                RequestedByUserId = requestedByUserId
            }, cancellationToken);
            return Ok(team);
        }
        catch (Exception ex) { return HandleException(ex); }
    }
 
    // POST: api/team/{id}/required-roles
    [HttpPost("{id}/required-roles")]
    public async Task<IActionResult> AddRequiredRole(string id, [FromBody] AddRequiredRoleCommand command, CancellationToken cancellationToken)
    {
        try
        {
            if (command.TeamId != id)
                return BadRequest(new { message = "ID mismatch." });
 
            var team = await _mediator.Send(command, cancellationToken);
            return Ok(team);
        }
        catch (Exception ex) { return HandleException(ex); }
    }
}