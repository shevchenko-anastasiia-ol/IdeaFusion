using Collaboration.Application.Commands.CollaborationRequest;
using Collaboration.Application.Queries.CollaborationRequest;
using Collaboration.Domain.Entities.Parameters;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Collaboration.API.Controllers;

[Route("api/collaboration-requests")]
public class CollaborationRequestController : BaseApiController
{
    public CollaborationRequestController(IMediator mediator) : base(mediator) { }
 
    // GET: api/collaboration-requests/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        try
        {
            var request = await _mediator.Send(new GetCollaborationRequestByIdQuery { RequestId = id }, cancellationToken);
            if (request is null) return NotFound();
            return Ok(request);
        }
        catch (Exception ex) { return HandleException(ex); }
    }
 
    // GET: api/collaboration-requests
    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] CollaborationRequestParameters parameters, CancellationToken cancellationToken)
    {
        try
        {
            var requests = await _mediator.Send(new GetCollaborationRequestsPagedQuery { Parameters = parameters }, cancellationToken);
            return Ok(requests);
        }
        catch (Exception ex) { return HandleException(ex); }
    }
 
    // GET: api/collaboration-requests/team/{teamId}
    [HttpGet("team/{teamId}")]
    public async Task<IActionResult> GetByTeam(string teamId, CancellationToken cancellationToken)
    {
        try
        {
            var requests = await _mediator.Send(new GetCollaborationRequestsByTeamQuery { TeamId = teamId }, cancellationToken);
            return Ok(requests);
        }
        catch (Exception ex) { return HandleException(ex); }
    }
 
    // GET: api/collaboration-requests/user/{userId}
    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetByUser(string userId, CancellationToken cancellationToken)
    {
        try
        {
            var requests = await _mediator.Send(new GetCollaborationRequestsByUserQuery { UserId = userId }, cancellationToken);
            return Ok(requests);
        }
        catch (Exception ex) { return HandleException(ex); }
    }
 
    // POST: api/collaboration-requests
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCollaborationRequestCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var request = await _mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = request.Id }, request);
        }
        catch (Exception ex) { return HandleException(ex); }
    }
 
    // PATCH: api/collaboration-requests/{id}/accept
    [HttpPatch("{id}/accept")]
    public async Task<IActionResult> Accept(string id, [FromBody] AcceptCollaborationRequestCommand command, CancellationToken cancellationToken)
    {
        try
        {
            if (command.RequestId != id)
                return BadRequest(new { message = "ID mismatch." });
 
            var request = await _mediator.Send(command, cancellationToken);
            return Ok(request);
        }
        catch (Exception ex) { return HandleException(ex); }
    }
 
    // PATCH: api/collaboration-requests/{id}/reject
    [HttpPatch("{id}/reject")]
    public async Task<IActionResult> Reject(string id, [FromBody] RejectCollaborationRequestCommand command, CancellationToken cancellationToken)
    {
        try
        {
            if (command.RequestId != id)
                return BadRequest(new { message = "ID mismatch." });
 
            var request = await _mediator.Send(command, cancellationToken);
            return Ok(request);
        }
        catch (Exception ex) { return HandleException(ex); }
    }
 
    // PATCH: api/collaboration-requests/{id}/cancel
    [HttpPatch("{id}/cancel")]
    public async Task<IActionResult> Cancel(string id, [FromBody] CancelCollaborationRequestCommand command, CancellationToken cancellationToken)
    {
        try
        {
            if (command.RequestId != id)
                return BadRequest(new { message = "ID mismatch." });
 
            await _mediator.Send(command, cancellationToken);
            return NoContent();
        }
        catch (Exception ex) { return HandleException(ex); }
    }
}