using BlazorUI.Models.Enums;
using Microsoft.AspNetCore.Components;

namespace BlazorUI.Components.Scheduler;

public enum SchedulerViewMode
{
    All,
    MyTasks,
    AssignedByMe,
    Private,
    Shared
}

public partial class SchedulerFilterBar
{
    [Parameter]
    public SchedulerViewMode ViewMode { get; set; }

    [Parameter]
    public EventCallback<SchedulerViewMode> ViewModeChanged { get; set; }

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
    public bool? IsRecurring { get; set; }

    [Parameter]
    public EventCallback<bool?> IsRecurringChanged { get; set; }

    [Parameter]
    public bool? HasBill { get; set; }

    [Parameter]
    public EventCallback<bool?> HasBillChanged { get; set; }

    [Parameter]
    public EventCallback OnReset { get; set; }

    IEnumerable<TaskCategory> Categories => Enum.GetValues<TaskCategory>();
    IEnumerable<TaskPriority> Priorities => Enum.GetValues<TaskPriority>();
    IEnumerable<OccurrenceStatus> Statuses => Enum.GetValues<OccurrenceStatus>();

    IEnumerable<SchedulerViewMode> ViewModes => Enum.GetValues<SchedulerViewMode>();

    static readonly Dictionary<bool, string> TaskTypeOptions = new()
    {
        { true, "Recurring" },
        { false, "One-time" }
    };

    static readonly Dictionary<bool, string> BillOptions = new()
    {
        { true, "With Bills" },
        { false, "Without Bills" }
    };

    bool HasActiveFilters =>
        ViewMode != SchedulerViewMode.All
        || Category.HasValue
        || Priority.HasValue
        || Status.HasValue
        || !string.IsNullOrEmpty(AssignedToUserId)
        || IsRecurring.HasValue
        || HasBill.HasValue;

    string GetViewModeLabel(SchedulerViewMode mode) => mode switch
    {
        SchedulerViewMode.All => "All Tasks",
        SchedulerViewMode.MyTasks => "My Tasks",
        SchedulerViewMode.AssignedByMe => "Assigned by Me",
        SchedulerViewMode.Private => "Private",
        SchedulerViewMode.Shared => "Shared",
        _ => mode.ToString()
    };

    async Task OnViewModeChanged(SchedulerViewMode value) => await ViewModeChanged.InvokeAsync(value);
    async Task OnCategoryChanged(TaskCategory? value) => await CategoryChanged.InvokeAsync(value);
    async Task OnPriorityChanged(TaskPriority? value) => await PriorityChanged.InvokeAsync(value);
    async Task OnStatusChanged(OccurrenceStatus? value) => await StatusChanged.InvokeAsync(value);
    async Task OnAssigneeChanged(string? value) => await AssignedToUserIdChanged.InvokeAsync(value);
    async Task OnIsRecurringChanged(bool? value) => await IsRecurringChanged.InvokeAsync(value);
    async Task OnHasBillChanged(bool? value) => await HasBillChanged.InvokeAsync(value);

    async Task ResetAsync() => await OnReset.InvokeAsync();
}
