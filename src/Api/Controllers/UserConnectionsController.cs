using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyHomeSolution.Application.Common.Models;
using MyHomeSolution.Application.Features.UserConnections.Commands.AcceptConnectionRequest;
using MyHomeSolution.Application.Features.UserConnections.Commands.CancelConnectionRequest;
using MyHomeSolution.Application.Features.UserConnections.Commands.DeclineConnectionRequest;
using MyHomeSolution.Application.Features.UserConnections.Commands.RemoveConnection;
using MyHomeSolution.Application.Features.UserConnections.Commands.SendConnectionRequest;
using MyHomeSolution.Application.Features.UserConnections.Common;
using MyHomeSolution.Application.Features.UserConnections.Queries.GetConnections;
using MyHomeSolution.Application.Features.UserConnections.Queries.GetPendingRequests;
using MyHomeSolution.Application.Features.UserConnections.Queries.GetSharedHistory;
using MyHomeSolution.Application.Features.UserConnections.Queries.SearchConnectedUsers;
using MyHomeSolution.Application.Features.Users.Common;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Api.Controllers;

[ApiController]
[Route("api/user-connections")]
[Authorize]
public sealed class UserConnectionsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<UserConnectionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConnections(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] ConnectionStatus? status = null,
        [FromQuery] string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetConnectionsQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            Status = status,
            SearchTerm = searchTerm
        };

        var result = await sender.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("pending")]
    [ProducesResponseType(typeof(IReadOnlyList<UserConnectionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingRequests(
        [FromQuery] bool sent = false,
        CancellationToken cancellationToken = default)
    {
        var query = new GetPendingRequestsQuery { Sent = sent };
        var result = await sender.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("friends/search")]
    [ProducesResponseType(typeof(IReadOnlyList<UserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchConnectedUsers(
        [FromQuery] string? searchTerm = null,
        [FromQuery] int maxResults = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new SearchConnectedUsersQuery
        {
            SearchTerm = searchTerm,
            MaxResults = maxResults
        };

        var result = await sender.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SendConnectionRequest(
        SendConnectionRequestCommand command, CancellationToken cancellationToken)
    {
        var id = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetConnections), null, id);
    }

    [HttpPut("{id:guid}/accept")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AcceptRequest(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new AcceptConnectionRequestCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/decline")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeclineRequest(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeclineConnectionRequestCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CancelRequest(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new CancelConnectionRequestCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RemoveConnection(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new RemoveConnectionCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpGet("{userId}/shared-history")]
    [ProducesResponseType(typeof(SharedHistoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSharedHistory(
        string userId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetSharedHistoryQuery(userId), cancellationToken);
        return Ok(result);
    }
}
