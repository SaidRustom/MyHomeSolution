using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyHomeSolution.Application.Common.Models;
using MyHomeSolution.Application.Features.Bills.Commands.AddBillReceipt;
using MyHomeSolution.Application.Features.Bills.Commands.CreateBill;
using MyHomeSolution.Application.Features.Bills.Commands.CreateBillFromReceipt;
using MyHomeSolution.Application.Features.Bills.Commands.DeleteBill;
using MyHomeSolution.Application.Features.Bills.Commands.MarkSplitAsPaid;
using MyHomeSolution.Application.Features.Bills.Commands.UpdateBill;
using MyHomeSolution.Application.Features.Bills.Common;
using MyHomeSolution.Application.Features.Bills.Queries.GetBillById;
using MyHomeSolution.Application.Features.Bills.Queries.GetBillReceipt;
using MyHomeSolution.Application.Features.Bills.Queries.GetBills;
using MyHomeSolution.Application.Features.Bills.Queries.GetSpendingSummary;
using MyHomeSolution.Application.Features.Bills.Queries.GetUserBalances;
using MyHomeSolution.Domain.Enums;
using ReceiptSplitRequest = MyHomeSolution.Application.Features.Bills.Commands.CreateBillFromReceipt.BillSplitRequest;

namespace MyHomeSolution.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class BillsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<BillBriefDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBills(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] BillCategory? category = null,
        [FromQuery] string? paidByUserId = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] DateTimeOffset? fromDate = null,
        [FromQuery] DateTimeOffset? toDate = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = null,
        [FromQuery] bool? isFullyPaid = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetBillsQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            Category = category,
            PaidByUserId = paidByUserId,
            SearchTerm = searchTerm,
            FromDate = fromDate,
            ToDate = toDate,
            SortBy = sortBy,
            SortDirection = sortDirection,
            IsFullyPaid = isFullyPaid
        };

        var result = await sender.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(BillDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBill(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetBillByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpGet("balances")]
    [ProducesResponseType(typeof(IReadOnlyList<UserBalanceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBalances(
        [FromQuery] string? counterpartyUserId = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetUserBalancesQuery { CounterpartyUserId = counterpartyUserId };
        var result = await sender.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("summary")]
    [ProducesResponseType(typeof(SpendingSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSpendingSummary(
        [FromQuery] DateTimeOffset? fromDate = null,
        [FromQuery] DateTimeOffset? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetSpendingSummaryQuery { FromDate = fromDate, ToDate = toDate };
        var result = await sender.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateBill(
        CreateBillCommand command, CancellationToken cancellationToken)
    {
        var id = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetBill), new { id }, id);
    }

    [HttpPost("from-receipt")]
    [ProducesResponseType(typeof(BillDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<IActionResult> CreateBillFromReceipt(
        IFormFile file,
        [FromQuery] BillCategory category = BillCategory.General,
        [FromQuery] string? splitUserIds = null,
        CancellationToken cancellationToken = default)
    {
        if (file.Length == 0)
            return BadRequest("File is empty.");

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };
        if (!allowedTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
            return BadRequest("Only JPEG, PNG, and WebP images are allowed.");

        var splits = ParseSplitUserIds(splitUserIds);

        await using var stream = file.OpenReadStream();
        var command = new CreateBillFromReceiptCommand
        {
            FileName = file.FileName,
            ContentType = file.ContentType,
            Content = stream,
            Category = category,
            Splits = splits
        };

        var result = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetBill), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateBill(
        Guid id, UpdateBillCommand command, CancellationToken cancellationToken)
    {
        if (id != command.Id)
            return BadRequest("Route id does not match command id.");

        await sender.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteBill(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteBillCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/receipt")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReceipt(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetBillReceiptQuery(id), cancellationToken);

        if (result is null)
            return NotFound("No receipt attached to this bill.");

        return File(result.Content, result.ContentType, result.FileName);
    }

    [HttpPost("{id:guid}/receipt")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddReceipt(
        Guid id, IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0)
            return BadRequest("File is empty.");

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp", "application/pdf" };
        if (!allowedTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
            return BadRequest("Only JPEG, PNG, WebP, and PDF files are allowed.");

        await using var stream = file.OpenReadStream();
        var command = new AddBillReceiptCommand
        {
            BillId = id,
            FileName = file.FileName,
            ContentType = file.ContentType,
            Content = stream
        };

        var receiptUrl = await sender.Send(command, cancellationToken);
        return Ok(new { receiptUrl });
    }

    [HttpPut("{billId:guid}/splits/{splitId:guid}/pay")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkSplitAsPaid(
        Guid billId, Guid splitId, CancellationToken cancellationToken)
    {
        await sender.Send(new MarkSplitAsPaidCommand(billId, splitId), cancellationToken);
        return NoContent();
    }

    private static List<ReceiptSplitRequest>? ParseSplitUserIds(string? splitUserIds)
    {
        if (string.IsNullOrWhiteSpace(splitUserIds))
            return null;

        return splitUserIds
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(id => new ReceiptSplitRequest { UserId = id })
            .ToList();
    }
}
