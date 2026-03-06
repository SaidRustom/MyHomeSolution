using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyHomeSolution.Application.Common.Models;
using MyHomeSolution.Application.Features.Occurrences.Commands.CompleteOccurrence;
using MyHomeSolution.Application.Features.Occurrences.Commands.RescheduleOccurrence;
using MyHomeSolution.Application.Features.Occurrences.Commands.SkipOccurrence;
using MyHomeSolution.Application.Features.Occurrences.Commands.StartOccurrence;
using MyHomeSolution.Application.Features.Occurrences.Queries.GetOccurrencesByDateRange;
using MyHomeSolution.Application.Features.Occurrences.Queries.GetOccurrencesByTask;
using MyHomeSolution.Application.Features.Occurrences.Queries.GetUpcomingOccurrences;
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

    [HttpGet("by-date-range")]
    [ProducesResponseType(typeof(IReadOnlyCollection<CalendarOccurrenceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByDateRange(
        [FromQuery] DateOnly startDate,
        [FromQuery] DateOnly endDate,
        [FromQuery] string? assignedToUserId = null,
        [FromQuery] OccurrenceStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetOccurrencesByDateRangeQuery
        {
            StartDate = startDate,
            EndDate = endDate,
            AssignedToUserId = assignedToUserId,
            Status = status
        };

        var result = await sender.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("upcoming")]
    [ProducesResponseType(typeof(PaginatedList<CalendarOccurrenceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUpcoming(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetUpcomingOccurrencesQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var result = await sender.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/start")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Start(
        Guid id, [FromBody] StartOccurrenceRequest? request,
        CancellationToken cancellationToken)
    {
        var command = new StartOccurrenceCommand
        {
            OccurrenceId = id,
            Notes = request?.Notes
        };

        await sender.Send(command, cancellationToken);
        return NoContent();
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

    [HttpPost("{id:guid}/reschedule")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Reschedule(
        Guid id, [FromBody] RescheduleOccurrenceRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RescheduleOccurrenceCommand
        {
            OccurrenceId = id,
            NewDueDate = request.NewDueDate,
            Notes = request.Notes
        };

        await sender.Send(command, cancellationToken);
        return NoContent();
    }
}

public sealed record StartOccurrenceRequest(string? Notes);
public sealed record CompleteOccurrenceRequest(string? Notes);
public sealed record SkipOccurrenceRequest(string? Notes);
public sealed record RescheduleOccurrenceRequest(DateOnly NewDueDate, string? Notes);
