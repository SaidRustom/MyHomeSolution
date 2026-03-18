using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyHomeSolution.Application.Common.Models;
using MyHomeSolution.Application.Features.Budgets.Commands.CreateBudget;
using MyHomeSolution.Application.Features.Budgets.Commands.DeleteBudget;
using MyHomeSolution.Application.Features.Budgets.Commands.EditBudgetOccurrenceAmount;
using MyHomeSolution.Application.Features.Budgets.Commands.TransferBudgetFunds;
using MyHomeSolution.Application.Features.Budgets.Commands.UpdateBudget;
using MyHomeSolution.Application.Features.Budgets.Common;
using MyHomeSolution.Application.Features.Budgets.Queries.GetBudgetById;
using MyHomeSolution.Application.Features.Budgets.Queries.GetBudgetOccurrences;
using MyHomeSolution.Application.Features.Budgets.Queries.GetBudgets;
using MyHomeSolution.Application.Features.Budgets.Queries.GetBudgetSummary;
using MyHomeSolution.Application.Features.Budgets.Queries.GetBudgetTree;
using MyHomeSolution.Application.Features.Budgets.Queries.GetBudgetTrends;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class BudgetsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<BudgetBriefDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBudgets(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] BudgetCategory? category = null,
        [FromQuery] BudgetPeriod? period = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] bool? isRecurring = null,
        [FromQuery] bool? isOverBudget = null,
        [FromQuery] Guid? parentBudgetId = null,
        [FromQuery] bool? rootOnly = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetBudgetsQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            Category = category,
            Period = period,
            SearchTerm = searchTerm,
            IsRecurring = isRecurring,
            IsOverBudget = isOverBudget,
            ParentBudgetId = parentBudgetId,
            RootOnly = rootOnly,
            SortBy = sortBy,
            SortDirection = sortDirection
        };

        var result = await sender.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(BudgetDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBudget(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetBudgetByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}/occurrences")]
    [ProducesResponseType(typeof(IReadOnlyList<BudgetOccurrenceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOccurrences(
        Guid id,
        [FromQuery] DateTimeOffset? fromDate = null,
        [FromQuery] DateTimeOffset? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetBudgetOccurrencesQuery
        {
            BudgetId = id,
            FromDate = fromDate,
            ToDate = toDate
        };

        var result = await sender.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("summary")]
    [ProducesResponseType(typeof(BudgetSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummary(
        [FromQuery] DateTimeOffset? fromDate = null,
        [FromQuery] DateTimeOffset? toDate = null,
        [FromQuery] BudgetCategory? category = null,
        [FromQuery] BudgetPeriod? period = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetBudgetSummaryQuery
        {
            FromDate = fromDate,
            ToDate = toDate,
            Category = category,
            Period = period
        };

        var result = await sender.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("tree")]
    [ProducesResponseType(typeof(IReadOnlyList<BudgetTreeNodeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTree(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetBudgetTreeQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("trends")]
    [ProducesResponseType(typeof(BudgetTrendsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTrends(
        [FromQuery] Guid? budgetId = null,
        [FromQuery] int periods = 6,
        [FromQuery] DateTimeOffset? asOfDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetBudgetTrendsQuery
        {
            BudgetId = budgetId,
            Periods = periods,
            AsOfDate = asOfDate
        };

        var result = await sender.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateBudget(
        CreateBudgetCommand command, CancellationToken cancellationToken)
    {
        var id = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetBudget), new { id }, id);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateBudget(
        Guid id, UpdateBudgetCommand command, CancellationToken cancellationToken)
    {
        if (id != command.Id)
            return BadRequest("Route id does not match command id.");

        await sender.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteBudget(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteBudgetCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPut("occurrences/{occurrenceId:guid}/amount")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> EditOccurrenceAmount(
        Guid occurrenceId,
        EditBudgetOccurrenceAmountCommand command,
        CancellationToken cancellationToken)
    {
        if (occurrenceId != command.OccurrenceId)
            return BadRequest("Route occurrence id does not match command occurrence id.");

        await sender.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpPost("transfers")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TransferFunds(
        TransferBudgetFundsCommand command, CancellationToken cancellationToken)
    {
        var id = await sender.Send(command, cancellationToken);
        return Created($"api/budgets/transfers/{id}", id);
    }
}
