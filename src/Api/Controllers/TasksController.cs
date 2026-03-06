using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyHomeSolution.Application.Features.Tasks.Commands.CreateTask;
using MyHomeSolution.Application.Features.Tasks.Commands.DeleteTask;
using MyHomeSolution.Application.Features.Tasks.Commands.UpdateTask;
using MyHomeSolution.Application.Features.Tasks.Common;
using MyHomeSolution.Application.Features.Tasks.Queries.GetTaskById;
using MyHomeSolution.Application.Features.Tasks.Queries.GetTasks;
using MyHomeSolution.Application.Features.Tasks.Queries.GetTodayTasks;
using MyHomeSolution.Application.Common.Models;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class TasksController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<TaskBriefDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTasks(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] TaskCategory? category = null,
        [FromQuery] TaskPriority? priority = null,
        [FromQuery] bool? isRecurring = null,
        [FromQuery] string? assignedToUserId = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] DateOnly? fromDate = null,
        [FromQuery] DateOnly? toDate = null,
        [FromQuery] bool? notCompletedOnly = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetTasksQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            Category = category,
            Priority = priority,
            IsRecurring = isRecurring,
            AssignedToUserId = assignedToUserId,
            SearchTerm = searchTerm,
            FromDate = fromDate,
            ToDate = toDate,
            NotCompletedOnly = notCompletedOnly
        };

        var result = await sender.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("today")]
    [ProducesResponseType(typeof(IReadOnlyCollection<TodayTaskDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTodayTasks(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetTodayTasksQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TaskDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTask(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetTaskByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateTask(
        CreateTaskCommand command, CancellationToken cancellationToken)
    {
        var id = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetTask), new { id }, id);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateTask(
        Guid id, UpdateTaskCommand command, CancellationToken cancellationToken)
    {
        if (id != command.Id)
            return BadRequest("Route id does not match command id.");

        await sender.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTask(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteTaskCommand(id), cancellationToken);
        return NoContent();
    }
}
