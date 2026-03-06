using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyHomeSolution.Application.Common.Models;
using MyHomeSolution.Application.Features.Notifications.Commands.CreateNotification;
using MyHomeSolution.Application.Features.Notifications.Commands.DeleteNotification;
using MyHomeSolution.Application.Features.Notifications.Commands.MarkAllAsRead;
using MyHomeSolution.Application.Features.Notifications.Commands.MarkAsRead;
using MyHomeSolution.Application.Features.Notifications.Common;
using MyHomeSolution.Application.Features.Notifications.Queries.GetNotificationById;
using MyHomeSolution.Application.Features.Notifications.Queries.GetNotifications;
using MyHomeSolution.Application.Features.Notifications.Queries.GetUnreadCount;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class NotificationsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<NotificationBriefDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool? isRead = null,
        [FromQuery] NotificationType? type = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetNotificationsQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            IsRead = isRead,
            Type = type
        };

        var result = await sender.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(NotificationDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetNotification(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetNotificationByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUnreadCount(CancellationToken cancellationToken)
    {
        var count = await sender.Send(new GetUnreadNotificationCountQuery(), cancellationToken);
        return Ok(count);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateNotification(
        CreateNotificationCommand command, CancellationToken cancellationToken)
    {
        var id = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetNotification), new { id }, id);
    }

    [HttpPut("{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new MarkNotificationAsReadCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPut("read-all")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken)
    {
        var count = await sender.Send(new MarkAllNotificationsAsReadCommand(), cancellationToken);
        return Ok(count);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteNotification(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteNotificationCommand(id), cancellationToken);
        return NoContent();
    }
}
