using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyHomeSolution.Application.Features.AuditLogs.Common;
using MyHomeSolution.Application.Features.AuditLogs.Queries.GetEntityAuditHistory;

namespace MyHomeSolution.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class AuditLogsController(ISender sender) : ControllerBase
{
    [HttpGet("{entityName}/{entityId}")]
    [ProducesResponseType(typeof(IReadOnlyList<AuditLogDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEntityHistory(
        string entityName,
        string entityId,
        [FromQuery] int maxResults = 50,
        CancellationToken cancellationToken = default)
    {
        var query = new GetEntityAuditHistoryQuery
        {
            EntityName = entityName,
            EntityId = entityId,
            MaxResults = maxResults
        };

        var result = await sender.Send(query, cancellationToken);
        return Ok(result);
    }
}
