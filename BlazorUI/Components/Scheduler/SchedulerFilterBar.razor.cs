using BlazorUI.Models.Enums;
using Microsoft.AspNetCore.Components;

namespace BlazorUI.Components.Scheduler;

public partial class SchedulerFilterBar
{
    [Parameter]
    public TaskCategory? Category { get; set; }

    [Parameter]
    public EventCallback<TaskCategory?> CategoryChanged { get; set; }

    [Parameter]
    public TaskPriority? Priority { get; set; }

    [Parameter]
    public EventCallback<TaskPriority?> PriorityChanged { get; set; }

    [Parameter]
    public OccurrenceStatus? Status { get; set; }

    [Parameter]
    public EventCallback<OccurrenceStatus?> StatusChanged { get; set; }

    [Parameter]
    public string? AssignedToUserId { get; set; }

    [Parameter]
    public EventCallback<string?> AssignedToUserIdChanged { get; set; }

    [Parameter]
    public EventCallback OnReset { get; set; }

    IEnumerable<TaskCategory> Categories => Enum.GetValues<TaskCategory>();
    IEnumerable<TaskPriority> Priorities => Enum.GetValues<TaskPriority>();
    IEnumerable<OccurrenceStatus> Statuses => Enum.GetValues<OccurrenceStatus>();

    bool HasActiveFilters =>
        Category.HasValue || Priority.HasValue || Status.HasValue || !string.IsNullOrEmpty(AssignedToUserId);

    async Task OnCategoryChanged(TaskCategory? value) => await CategoryChanged.InvokeAsync(value);
    async Task OnPriorityChanged(TaskPriority? value) => await PriorityChanged.InvokeAsync(value);
    async Task OnStatusChanged(OccurrenceStatus? value) => await StatusChanged.InvokeAsync(value);
    async Task OnAssigneeChanged(string? value) => await AssignedToUserIdChanged.InvokeAsync(value);

    async Task ResetAsync() => await OnReset.InvokeAsync();
}
