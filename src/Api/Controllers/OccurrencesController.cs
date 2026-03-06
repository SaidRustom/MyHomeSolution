using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyHomeSolution.Application.Common.Models;
using MyHomeSolution.Application.Features.Occurrences.Commands.CompleteOccurrence;
using MyHomeSolution.Application.Features.Occurrences.Commands.SkipOccurrence;
using MyHomeSolution.Application.Features.Occurrences.Queries.GetOccurrencesByTask;
using MyHomeSolution.Application.Features.Tasks.Common;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class OccurrencesController(ISender sender) : ControllerBase
{
    [HttpGet("by-task/{taskId:guid}")]
    [ProducesResponseType(typeof(PaginatedList<OccurrenceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByTask(
        Guid taskId,
        [FromQuery] OccurrenceStatus? status = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetOccurrencesByTaskQuery
        {
            HouseholdTaskId = taskId,
            Status = status,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var result = await sender.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/complete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Complete(
        Guid id, [FromBody] CompleteOccurrenceRequest? request,
        CancellationToken cancellationToken)
    {
        var command = new CompleteOccurrenceCommand
        {
            OccurrenceId = id,
            Notes = request?.Notes
        };

        await sender.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/skip")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Skip(
        Guid id, [FromBody] SkipOccurrenceRequest? request,
        CancellationToken cancellationToken)
    {
        var command = new SkipOccurrenceCommand
        {
            OccurrenceId = id,
            Notes = request?.Notes
        };

        await sender.Send(command, cancellationToken);
        return NoContent();
    }
}

public sealed record CompleteOccurrenceRequest(string? Notes);
public sealed record SkipOccurrenceRequest(string? Notes);
