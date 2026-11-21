using Collaboration.API.Services;
using Collaboration.Application.Commands.Vacancy;
using Collaboration.Application.DTOs;
using Collaboration.Application.Queries.Vacancy;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Collaboration.API.Controllers;

[Route("api/vacancies")]
[Authorize]
public class VacancyController : BaseApiController
{
    private readonly ICollaborationAuditWriter _audit;

    public VacancyController(IMediator mediator, ICollaborationAuditWriter audit) : base(mediator)
    {
        _audit = audit;
    }

    // POST: api/vacancies
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTeamVacancyCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var vacancy = await _mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = vacancy.Id }, vacancy);
        }
        catch (Exception ex) { return HandleException(ex); }
    }

    // PUT: api/vacancies/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateTeamVacancyCommand command, CancellationToken cancellationToken)
    {
        try
        {
            if (command.VacancyId != id)
                return BadRequest(new { message = "ID mismatch." });

            var vacancy = await _mediator.Send(command, cancellationToken);
            return Ok(vacancy);
        }
        catch (Exception ex) { return HandleException(ex); }
    }

    // POST: api/vacancies/{id}/close
    [HttpPost("{id}/close")]
    public async Task<IActionResult> Close(string id, [FromBody] CloseTeamVacancyCommand command, CancellationToken cancellationToken)
    {
        try
        {
            if (command.VacancyId != id)
                return BadRequest(new { message = "ID mismatch." });

            var vacancy = await _mediator.Send(command, cancellationToken);
            return Ok(vacancy);
        }
        catch (Exception ex) { return HandleException(ex); }
    }

    // GET: api/vacancies?query=&category=&role=&openOnly=&teamId=&pageNumber=&pageSize=
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string? query,
        [FromQuery] string? category,
        [FromQuery] string? role,
        [FromQuery] string? teamId,
        [FromQuery] bool openOnly = true,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var paged = await _mediator.Send(new SearchVacanciesQuery
            {
                Query = query,
                Category = category,
                Role = role,
                TeamId = teamId,
                OpenOnly = openOnly,
                PageNumber = pageNumber,
                PageSize = pageSize
            }, cancellationToken);

            return Ok(VacancyPageDto.From(paged));
        }
        catch (Exception ex) { return HandleException(ex); }
    }

    // GET: api/vacancies/team/{teamId}?pageNumber=&pageSize=
    [AllowAnonymous]
    [HttpGet("team/{teamId}")]
    public async Task<IActionResult> GetByTeam(
        string teamId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var paged = await _mediator.Send(new GetTeamVacanciesQuery
            {
                TeamId = teamId,
                PageNumber = pageNumber,
                PageSize = pageSize
            }, cancellationToken);

            return Ok(VacancyPageDto.From(paged));
        }
        catch (Exception ex) { return HandleException(ex); }
    }

    // GET: api/vacancies/{id}
    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        try
        {
            var vacancy = await _mediator.Send(new GetTeamVacancyByIdQuery { VacancyId = id }, cancellationToken);
            return Ok(vacancy);
        }
        catch (Exception ex) { return HandleException(ex); }
    }

    // POST: api/vacancies/{id}/apply
    [HttpPost("{id}/apply")]
    public async Task<IActionResult> Apply(string id, [FromBody] ApplyToVacancyCommand command, CancellationToken cancellationToken)
    {
        try
        {
            if (command.VacancyId != id)
                return BadRequest(new { message = "ID mismatch." });

            var application = await _mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id }, application);
        }
        catch (Exception ex) { return HandleException(ex); }
    }

    // GET: api/vacancies/{id}/applications
    // Who may read the applicant list is decided in the handler against the token: the
    // caller has to hold the team's review privilege. No userId is taken from the caller.
    [HttpGet("{id}/applications")]
    public async Task<IActionResult> GetApplications(string id, CancellationToken cancellationToken)
    {
        try
        {
            var applications = await _mediator.Send(new GetVacancyApplicationsQuery
            {
                VacancyId = id
            }, cancellationToken);

            return Ok(applications);
        }
        catch (Exception ex) { return HandleException(ex); }
    }

    // POST: api/vacancies/applications/{applicationId}/accept
    [HttpPost("applications/{applicationId}/accept")]
    public async Task<IActionResult> AcceptApplication(
        string applicationId, [FromBody] AcceptVacancyApplicationCommand command, CancellationToken cancellationToken)
    {
        try
        {
            if (command.ApplicationId != applicationId)
                return BadRequest(new { message = "ID mismatch." });

            var application = await _mediator.Send(command, cancellationToken);

            await _audit.RecordAsync(
                action: "AcceptVacancyApplication",
                targetType: "VacancyApplication",
                targetId: application.Id,
                targetName: application.Applicant.Username,
                details: $"Application {application.Id} from {application.Applicant.UserId} accepted for team {application.TeamId}.",
                cancellationToken: cancellationToken);

            return Ok(application);
        }
        catch (Exception ex) { return HandleException(ex); }
    }

    // POST: api/vacancies/applications/{applicationId}/reject
    [HttpPost("applications/{applicationId}/reject")]
    public async Task<IActionResult> RejectApplication(
        string applicationId, [FromBody] RejectVacancyApplicationCommand command, CancellationToken cancellationToken)
    {
        try
        {
            if (command.ApplicationId != applicationId)
                return BadRequest(new { message = "ID mismatch." });

            var application = await _mediator.Send(command, cancellationToken);

            await _audit.RecordAsync(
                action: "RejectVacancyApplication",
                targetType: "VacancyApplication",
                targetId: application.Id,
                targetName: application.Applicant.Username,
                details: $"Application {application.Id} from {application.Applicant.UserId} rejected for team {application.TeamId}.",
                cancellationToken: cancellationToken);

            return Ok(application);
        }
        catch (Exception ex) { return HandleException(ex); }
    }

    // POST: api/vacancies/applications/{applicationId}/cancel
    [HttpPost("applications/{applicationId}/cancel")]
    public async Task<IActionResult> CancelApplication(
        string applicationId, [FromBody] CancelVacancyApplicationCommand command, CancellationToken cancellationToken)
    {
        try
        {
            if (command.ApplicationId != applicationId)
                return BadRequest(new { message = "ID mismatch." });

            await _mediator.Send(command, cancellationToken);
            return Ok(new { message = "Application cancelled." });
        }
        catch (Exception ex) { return HandleException(ex); }
    }

    // GET: api/vacancies/applications/mine
    // The caller's own applications. The userId parameter is gone.
    [HttpGet("applications/mine")]
    public async Task<IActionResult> GetMyApplications(CancellationToken cancellationToken)
    {
        try
        {
            var applications = await _mediator.Send(new GetMyVacancyApplicationsQuery(), cancellationToken);
            return Ok(applications);
        }
        catch (Exception ex) { return HandleException(ex); }
    }
}