using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyHomeSolution.Application.Features.Shares.Commands.RevokeShare;
using MyHomeSolution.Application.Features.Shares.Commands.ShareEntity;
using MyHomeSolution.Application.Features.Shares.Commands.UpdateSharePermission;
using MyHomeSolution.Application.Features.Shares.Common;
using MyHomeSolution.Application.Features.Shares.Queries.GetEntityShares;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class SharesController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ShareDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetShares(
        [FromQuery] string entityType,
        [FromQuery] Guid entityId,
        CancellationToken cancellationToken)
    {
        var query = new GetEntitySharesQuery
        {
            EntityType = entityType,
            EntityId = entityId
        };

        var result = await sender.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ShareEntity(
        ShareEntityCommand command, CancellationToken cancellationToken)
    {
        var shareId = await sender.Send(command, cancellationToken);
        return CreatedAtAction(
            nameof(GetShares),
            new { entityType = command.EntityType, entityId = command.EntityId },
            shareId);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdatePermission(
        Guid id, [FromBody] UpdatePermissionRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateSharePermissionCommand
        {
            ShareId = id,
            Permission = request.Permission
        };

        await sender.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RevokeShare(
        Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new RevokeShareCommand(id), cancellationToken);
        return NoContent();
    }
}

public sealed record UpdatePermissionRequest(SharePermission Permission);
