using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyHomeSolution.Application.Common.Models;
using MyHomeSolution.Application.Features.ShoppingLists.Commands.AddShoppingItem;
using MyHomeSolution.Application.Features.ShoppingLists.Commands.AddShoppingItemFromBillItem;
using MyHomeSolution.Application.Features.ShoppingLists.Commands.CreateShoppingList;
using MyHomeSolution.Application.Features.ShoppingLists.Commands.DeleteShoppingList;
using MyHomeSolution.Application.Features.ShoppingLists.Commands.RemoveShoppingItem;
using MyHomeSolution.Application.Features.ShoppingLists.Commands.ToggleShoppingItem;
using MyHomeSolution.Application.Features.ShoppingLists.Commands.ToggleAllShoppingItems;
using MyHomeSolution.Application.Features.ShoppingLists.Commands.UpdateShoppingItem;
using MyHomeSolution.Application.Features.ShoppingLists.Commands.ProcessShoppingListReceipt;
using MyHomeSolution.Application.Features.ShoppingLists.Commands.ResolveCrossListMatch;
using MyHomeSolution.Application.Features.ShoppingLists.Commands.UpdateShoppingList;
using MyHomeSolution.Application.Features.ShoppingLists.Common;
using MyHomeSolution.Application.Features.ShoppingLists.Queries.GetShoppingListById;
using MyHomeSolution.Application.Features.ShoppingLists.Queries.GroupShoppingItems;
using MyHomeSolution.Application.Features.ShoppingLists.Queries.GetShoppingLists;
using MyHomeSolution.Domain.Enums;
using ReceiptSplitRequest = MyHomeSolution.Application.Features.ShoppingLists.Commands.ProcessShoppingListReceipt.ReceiptSplitRequest;

namespace MyHomeSolution.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class ShoppingListsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<ShoppingListBriefDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetShoppingLists(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] ShoppingListCategory? category = null,
        [FromQuery] bool? isCompleted = null,
        [FromQuery] string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetShoppingListsQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            Category = category,
            IsCompleted = isCompleted,
            SearchTerm = searchTerm
        };

        var result = await sender.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ShoppingListDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetShoppingList(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetShoppingListByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateShoppingList(
        CreateShoppingListCommand command, CancellationToken cancellationToken)
    {
        var id = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetShoppingList), new { id }, id);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateShoppingList(
        Guid id, UpdateShoppingListCommand command, CancellationToken cancellationToken)
    {
        if (id != command.Id)
            return BadRequest("Route id does not match command id.");

        await sender.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteShoppingList(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteShoppingListCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/items")]
    [ProducesResponseType(typeof(ShoppingItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddItem(
        Guid id, AddShoppingItemCommand command, CancellationToken cancellationToken)
    {
        if (id != command.ShoppingListId)
            return BadRequest("Route id does not match command shopping list id.");

        var result = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetShoppingList), new { id }, result);
    }

    [HttpPut("{id:guid}/items/{itemId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateItem(
        Guid id, Guid itemId, UpdateShoppingItemCommand command, CancellationToken cancellationToken)
    {
        if (id != command.ShoppingListId || itemId != command.ItemId)
            return BadRequest("Route ids do not match command ids.");

        await sender.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}/items/{itemId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveItem(
        Guid id, Guid itemId, CancellationToken cancellationToken)
    {
        await sender.Send(new RemoveShoppingItemCommand
        {
            ShoppingListId = id,
            ItemId = itemId
        }, cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/items/{itemId:guid}/toggle")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleItem(
        Guid id, Guid itemId, CancellationToken cancellationToken)
    {
        await sender.Send(new ToggleShoppingItemCommand
        {
            ShoppingListId = id,
            ItemId = itemId
        }, cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/items/toggle-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleAllItems(
        Guid id, [FromQuery] bool check, CancellationToken cancellationToken)
    {
        await sender.Send(new ToggleAllShoppingItemsCommand
        {
            ShoppingListId = id,
            Check = check
        }, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/items/from-bill-item")]
    [ProducesResponseType(typeof(ShoppingItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddItemFromBillItem(
        Guid id, AddShoppingItemFromBillItemCommand command, CancellationToken cancellationToken)
    {
        if (id != command.ShoppingListId)
            return BadRequest("Route id does not match command shopping list id.");

        var result = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetShoppingList), new { id }, result);
    }

    [HttpPost("{id:guid}/process-receipt")]
    [ProducesResponseType(typeof(ProcessReceiptResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<IActionResult> ProcessReceipt(
        Guid id,
        IFormFile file,
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
        var command = new ProcessShoppingListReceiptCommand
        {
            ShoppingListId = id,
            FileName = file.FileName,
            ContentType = file.ContentType,
            Content = stream,
            Splits = splits
        };

        var result = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetShoppingList), new { id }, result);
    }

    [HttpPost("{id:guid}/resolve-cross-list-match")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResolveCrossListMatch(
        Guid id, ResolveCrossListMatchCommand command, CancellationToken cancellationToken)
    {
        if (id != command.TargetShoppingListId)
            return BadRequest("Route id does not match command target shopping list id.");

        await sender.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/group-items")]
    [ProducesResponseType(typeof(ShoppingItemGroupResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GroupItems(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GroupShoppingItemsQuery(id), cancellationToken);
        return Ok(result);
    }

    private static List<ReceiptSplitRequest>? ParseSplitUserIds(string? splitUserIds)
    {
        if (string.IsNullOrWhiteSpace(splitUserIds))
            return null;

        return splitUserIds
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(entry =>
            {
                var parts = entry.Split(':', 2);
                var userId = parts[0];
                decimal? percentage = parts.Length > 1 && decimal.TryParse(parts[1], out var pct) ? pct : null;
                return new ReceiptSplitRequest { UserId = userId, Percentage = percentage };
            })
            .ToList();
    }
}
