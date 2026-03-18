using System.Security.Claims;
using BlazorUI.Components.Scheduler;
using BlazorUI.Models.Common;
using BlazorUI.Models.Enums;
using BlazorUI.Models.Occurrences;
using BlazorUI.Models.Realtime;
using BlazorUI.Models.Tasks;
using BlazorUI.Services.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Radzen;
using Radzen.Blazor;

namespace BlazorUI.Pages.Scheduler;

public partial class SchedulerPage : IDisposable
{
    [Inject]
    ITaskService TaskService { get; set; } = default!;

    [Inject]
    IOccurrenceService OccurrenceService { get; set; } = default!;

    [Inject]
    DialogService DialogService { get; set; } = default!;

    [Inject]
    NotificationService Notifications { get; set; } = default!;

    [Inject]
    NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    INotificationHubClient NotificationHubClient { get; set; } = default!;

    [CascadingParameter]
    private Task<AuthenticationState> AuthState { get; set; } = default!;

    RadzenScheduler<SchedulerAppointment> _scheduler = default!;

    List<SchedulerAppointment> _appointments = [];

    bool IsLoading { get; set; }

    ApiProblemDetails? Error { get; set; }

    string? _currentUserId;

    // Stat counts
    int OverdueCount { get; set; }
    int PendingTodayCount { get; set; }
    int InProgressCount { get; set; }
    int CompletedTodayCount { get; set; }

    // Filter state
    SchedulerViewMode _filterViewMode;
    TaskCategory? _filterCategory;
    TaskPriority? _filterPriority;
    string? _filterAssignee;
    OccurrenceStatus? _filterStatus;
    bool? _filterIsRecurring;
    bool? _filterHasBill;

    // Selected day for the panel (defaults to today)
    DateOnly _selectedDate = DateOnly.FromDateTime(DateTime.Today);

    // Tracked scheduler date range
    DateOnly _rangeStart;
    DateOnly _rangeEnd;

    CancellationTokenSource _cts = new();

    protected override async Task OnInitializedAsync()
    {
        var state = await AuthState;
        _currentUserId = state.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? state.User.FindFirst("sub")?.Value;

        NotificationHubClient.OnUserNotification += HandleRealtimeNotification;

        await LoadDataAsync();
    }

    async Task LoadDataAsync()
    {
        IsLoading = true;
        Error = null;

        // Load today's tasks for stat cards
        var todayResult = await TaskService.GetTodayTasksAsync(_cts.Token);
        if (todayResult.IsSuccess)
        {
            var allOccurrences = todayResult.Value
                .SelectMany(t => t.Occurrences)
                .ToList();

            OverdueCount = allOccurrences.Count(o => o.Status == OccurrenceStatus.Overdue);
            PendingTodayCount = allOccurrences.Count(o => o.Status == OccurrenceStatus.Pending);
            InProgressCount = allOccurrences.Count(o => o.Status == OccurrenceStatus.InProgress);
            CompletedTodayCount = allOccurrences.Count(o => o.Status == OccurrenceStatus.Completed);
        }
        else
        {
            Error = todayResult.Problem;
        }

        IsLoading = false;
    }

    async Task OnSchedulerLoadData(SchedulerLoadDataEventArgs args)
    {
        _rangeStart = DateOnly.FromDateTime(args.Start);
        _rangeEnd = DateOnly.FromDateTime(args.End);

        await LoadAppointmentsAsync();
    }

    async Task LoadAppointmentsAsync()
    {
        // Map view mode to API filter flags
        bool? assignedByMe = _filterViewMode == SchedulerViewMode.AssignedByMe ? true : null;
        bool? myTasks = _filterViewMode == SchedulerViewMode.MyTasks ? true : null;
        bool? @private = _filterViewMode == SchedulerViewMode.Private ? true : null;
        bool? shared = _filterViewMode == SchedulerViewMode.Shared ? true : null;

        var result = await OccurrenceService.GetByDateRangeAsync(
            _rangeStart, _rangeEnd,
            assignedToUserId: _filterAssignee,
            status: _filterStatus,
            assignedByMe: assignedByMe,
            myTasks: myTasks,
            @private: @private,
            shared: shared,
            isRecurring: _filterIsRecurring,
            hasBill: _filterHasBill,
            category: _filterCategory,
            priority: _filterPriority,
            cancellationToken: _cts.Token);

        if (result.IsSuccess)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);

            _appointments = result.Value.Select(o =>
            {
                var displayDate = (o.DueDate < today
                    && o.Status is not OccurrenceStatus.Completed
                    && o.Status is not OccurrenceStatus.Skipped)
                    ? today
                    : o.DueDate;

                return new SchedulerAppointment
                {
                    OccurrenceId = o.Id,
                    TaskId = o.TaskId,
                    Text = o.Status is OccurrenceStatus.Overdue
                        ? $"⚠ {o.TaskTitle}"
                        : o.TaskTitle,
                    Start = displayDate.ToDateTime(TimeOnly.MinValue),
                    End = displayDate.ToDateTime(TimeOnly.MinValue).AddMinutes(o.EstimatedDurationMinutes ?? 60),
                    Status = o.Status,
                    Priority = o.TaskPriority,
                    Category = o.TaskCategory,
                    AssignedToUserId = o.AssignedToUserId,
                    BillId = o.BillId
                };
            }).ToList();
        }
    }

    void OnAppointmentRender(SchedulerAppointmentRenderEventArgs<SchedulerAppointment> args)
    {
        var color = args.Data.Status switch
        {
            OccurrenceStatus.Completed => "var(--rz-success)",
            OccurrenceStatus.Skipped => "var(--rz-base-400)",
            OccurrenceStatus.Overdue => "var(--rz-danger)",
            OccurrenceStatus.InProgress => "var(--rz-info)",
            _ => GetPriorityColor(args.Data.Priority)
        };

        args.Attributes["style"] = $"background: {color}; border-color: {color};";
    }

    static string GetPriorityColor(TaskPriority priority) => priority switch
    {
        TaskPriority.Critical => "var(--rz-danger)",
        TaskPriority.High => "var(--rz-warning)",
        TaskPriority.Medium => "var(--rz-info)",
        _ => "var(--rz-primary)"
    };

    async Task OnSlotSelectAsync(SchedulerSlotSelectEventArgs args)
    {
        var clickedDate = DateOnly.FromDateTime(args.Start);
        _selectedDate = clickedDate;
        await InvokeAsync(StateHasChanged);
    }

    async Task OnAppointmentSelectAsync(SchedulerAppointmentSelectEventArgs<SchedulerAppointment> args)
    {
        await OpenOccurrenceDialogAsync(args.Data);
    }

    async Task SelectTodayAsync()
    {
        _selectedDate = DateOnly.FromDateTime(DateTime.Today);
        await InvokeAsync(StateHasChanged);
    }

    async Task OpenCreateTaskDialogAsync(DateOnly? dueDate = null)
    {
        var model = new TaskFormModel();
        if (dueDate.HasValue)
        {
            model.DueDate = dueDate.Value;
        }

        var result = await DialogService.OpenAsync<TaskFormDialog>(
            "Create Task",
            new Dictionary<string, object>
            {
                { nameof(TaskFormDialog.Model), model },
                { nameof(TaskFormDialog.IsEdit), false }
            },
            new DialogOptions
            {
                Width = "700px",
                Height = "700px",
                Resizable = true,
                Draggable = true,
                CloseDialogOnOverlayClick = false,
                ShowClose = false
            });

        if (result is true)
        {
            await ReloadAllAsync();
        }
    }

    async Task OpenOccurrenceDialogAsync(SchedulerAppointment appointment)
    {
        var result = await DialogService.OpenAsync<OccurrenceDetailDialog>(
            appointment.Text,
            new Dictionary<string, object>
            {
                { nameof(OccurrenceDetailDialog.OccurrenceId), appointment.OccurrenceId },
                { nameof(OccurrenceDetailDialog.TaskId), appointment.TaskId }
            },
            new DialogOptions
            {
                Width = "560px",
                Resizable = true,
                Draggable = true,
                CloseDialogOnOverlayClick = false,
                ShowClose = true
            });

        if (result is true)
        {
            await ReloadAllAsync();
        }
    }

    async Task ReloadAllAsync()
    {
        await LoadDataAsync();
        await LoadAppointmentsAsync();
        await InvokeAsync(StateHasChanged);
    }

    // Filter callbacks
    async Task OnViewModeFilterChanged(SchedulerViewMode value)
    {
        _filterViewMode = value;
        await LoadAppointmentsAsync();
    }

    async Task OnCategoryFilterChanged(TaskCategory? value)
    {
        _filterCategory = value;
        await LoadAppointmentsAsync();
    }

    async Task OnPriorityFilterChanged(TaskPriority? value)
    {
        _filterPriority = value;
        await LoadAppointmentsAsync();
    }

    async Task OnAssigneeFilterChanged(string? value)
    {
        _filterAssignee = value;
        await LoadAppointmentsAsync();
    }

    async Task OnStatusFilterChanged(OccurrenceStatus? value)
    {
        _filterStatus = value;
        await LoadAppointmentsAsync();
    }

    async Task OnIsRecurringFilterChanged(bool? value)
    {
        _filterIsRecurring = value;
        await LoadAppointmentsAsync();
    }

    async Task OnHasBillFilterChanged(bool? value)
    {
        _filterHasBill = value;
        await LoadAppointmentsAsync();
    }

    async Task ResetFiltersAsync()
    {
        _filterViewMode = SchedulerViewMode.All;
        _filterCategory = null;
        _filterPriority = null;
        _filterAssignee = null;
        _filterStatus = null;
        _filterIsRecurring = null;
        _filterHasBill = null;
        await LoadAppointmentsAsync();
    }

    async Task OnPanelActionAsync()
    {
        await ReloadAllAsync();
    }

    void NavigateToTaskList()
    {
        NavigationManager.NavigateTo("/tasks");
    }

    public void Dispose()
    {
        NotificationHubClient.OnUserNotification -= HandleRealtimeNotification;
        _cts.Cancel();
        _cts.Dispose();
    }

    void HandleRealtimeNotification(UserPushNotification push)
    {
        var entityType = push.RelatedEntityType?.ToLowerInvariant();
        if (entityType is "householdtask" or "task" or "taskoccurrence" or "occurrence" or "bill")
        {
            InvokeAsync(async () =>
            {
                await ReloadAllAsync();
            });
        }
    }
}

public sealed class SchedulerAppointment
{
    public Guid OccurrenceId { get; set; }
    public Guid TaskId { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public OccurrenceStatus Status { get; set; }
    public TaskPriority Priority { get; set; }
    public TaskCategory Category { get; set; }
    public string? AssignedToUserId { get; set; }
    public Guid? BillId { get; set; }
}
