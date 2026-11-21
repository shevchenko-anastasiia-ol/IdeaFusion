using IdentityBLL.DTOs;
using IdentityBLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceDefaults.Security;

namespace IdentityAPI.Controllers;

/// <summary>
/// Read access to the audit trail. The full trail is administrator-only; moderators
/// get the content slice, which the service filters regardless of the query so a
/// crafted request cannot widen the scope.
/// </summary>
[ApiController]
[Route("api/audit")]
[Authorize]
public class AuditController : ControllerBase
{
    private readonly IAuditService _auditService;
    private readonly ILogger<AuditController> _logger;

    public AuditController(IAuditService auditService, ILogger<AuditController> logger)
    {
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>The full audit trail. Administrators only.</summary>
    [HttpGet]
    [Authorize(Policy = IdeaFusionPolicies.RequireAdministrator)]
    [ProducesResponseType(typeof(IReadOnlyList<AuditLogEntryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<AuditLogEntryDto>>> GetAuditTrail(
        [FromQuery] AuditQueryDto query,
        CancellationToken cancellationToken)
    {
        var entries = await _auditService.GetAsync(query, cancellationToken);

        _logger.LogInformation("Administrator read the audit trail ({Count} entries).", entries.Count);

        return Ok(entries);
    }

    /// <summary>
    /// The content slice of the trail. Moderators and administrators only.
    /// </summary>
    [HttpGet("moderation")]
    [Authorize(Policy = IdeaFusionPolicies.RequireModerator)]
    [ProducesResponseType(typeof(IReadOnlyList<AuditLogEntryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<AuditLogEntryDto>>> GetModerationTrail(
        [FromQuery] AuditQueryDto query,
        CancellationToken cancellationToken)
    {
        var entries = await _auditService.GetModerationAsync(query, cancellationToken);

        _logger.LogInformation("Moderator read the audit trail ({Count} entries).", entries.Count);

        return Ok(entries);
    }
}
