using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace MyHomeSolution.Infrastructure.Hubs;

[Authorize]
public sealed class TaskHub : Hub
{
    public Task JoinTaskGroup(Guid taskId)
        => Groups.AddToGroupAsync(Context.ConnectionId, FormatGroupName(taskId));

    public Task LeaveTaskGroup(Guid taskId)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, FormatGroupName(taskId));

    public static string FormatGroupName(Guid taskId) => $"task-{taskId}";
}
