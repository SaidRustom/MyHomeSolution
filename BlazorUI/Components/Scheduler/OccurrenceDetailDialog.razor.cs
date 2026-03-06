using BlazorUI.Components.Common;
using BlazorUI.Models.Common;
using BlazorUI.Models.Enums;
using BlazorUI.Models.Occurrences;
using BlazorUI.Models.Tasks;
using BlazorUI.Services.Contracts;
using Microsoft.AspNetCore.Components;
using Radzen;

using TaskOccurrenceDto = BlazorUI.Models.Tasks.OccurrenceDto;

namespace BlazorUI.Components.Scheduler;

public partial class OccurrenceDetailDialog
{
    [Inject]
    DialogService DialogService { get; set; } = default!;

    [Inject]
    ITaskService TaskService { get; set; } = default!;

    [Inject]
    IOccurrenceService OccurrenceService { get; set; } = default!;

    [Inject]
    NotificationService Notifications { get; set; } = default!;

    [Inject]
    NavigationManager NavigationManager { get; set; } = default!;

    [Parameter]
    public Guid OccurrenceId { get; set; }

    [Parameter]
    public Guid TaskId { get; set; }

    TaskDetailDto? Task { get; set; }
    TaskOccurrenceDto? Occurrence { get; set; }

    bool IsLoading { get; set; }
    bool IsBusy { get; set; }
    string? Error { get; set; }

    bool _showReschedule;
    DateOnly? _rescheduleDate;
    string? _rescheduleNotes;

    bool _showNotesInput;
    string? _actionNotes;

    protected override async Task OnInitializedAsync()
    {
        await LoadDataAsync();
    }

    async Task LoadDataAsync()
    {
        IsLoading = true;
        Error = null;

        var taskResult = await TaskService.GetTaskByIdAsync(TaskId);
        if (taskResult.IsSuccess)
        {
            Task = taskResult.Value;
            Occurrence = Task.Occurrences.FirstOrDefault(o => o.Id == OccurrenceId);

            if (Occurrence is null)
                Error = "Occurrence not found.";
        }
        else
        {
            Error = taskResult.Problem.ToUserMessage();
        }

        IsLoading = false;
    }

    async Task StartAsync()
    {
        if (Occurrence is null) return;
        IsBusy = true;

        var request = new StartOccurrenceRequest { Notes = _actionNotes };
        var result = await OccurrenceService.StartAsync(Occurrence.Id, request);

        if (result.IsSuccess)
        {
            Notifications.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Info,
                Summary = "Task Started",
                Detail = $"'{Task?.Title}' is now in progress.",
                Duration = 4000
            });
            DialogService.Close(true);
        }
        else
        {
            Error = result.Problem.ToUserMessage();
        }

        IsBusy = false;
    }

    async Task CompleteAsync()
    {
        if (Occurrence is null) return;
        IsBusy = true;

        var request = new CompleteOccurrenceRequest { Notes = _actionNotes };
        var result = await OccurrenceService.CompleteAsync(Occurrence.Id, request);

        if (result.IsSuccess)
        {
            Notifications.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = "Task Completed",
                Detail = $"'{Task?.Title}' has been marked as complete.",
                Duration = 4000
            });
            DialogService.Close(true);
        }
        else
        {
            Error = result.Problem.ToUserMessage();
        }

        IsBusy = false;
    }

    async Task SkipAsync()
    {
        if (Occurrence is null) return;
        IsBusy = true;

        var request = new SkipOccurrenceRequest { Notes = _actionNotes };
        var result = await OccurrenceService.SkipAsync(Occurrence.Id, request);

        if (result.IsSuccess)
        {
            Notifications.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Warning,
                Summary = "Task Skipped",
                Detail = $"'{Task?.Title}' occurrence has been skipped.",
                Duration = 4000
            });
            DialogService.Close(true);
        }
        else
        {
            Error = result.Problem.ToUserMessage();
        }

        IsBusy = false;
    }

    async Task RescheduleAsync()
    {
        if (Occurrence is null || _rescheduleDate is null) return;
        IsBusy = true;

        var request = new RescheduleOccurrenceRequest
        {
            NewDueDate = _rescheduleDate.Value,
            Notes = _rescheduleNotes
        };
        var result = await OccurrenceService.RescheduleAsync(Occurrence.Id, request);

        if (result.IsSuccess)
        {
            Notifications.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Info,
                Summary = "Task Rescheduled",
                Detail = $"'{Task?.Title}' moved to {_rescheduleDate.Value:MMM dd, yyyy}.",
                Duration = 4000
            });
            DialogService.Close(true);
        }
        else
        {
            Error = result.Problem.ToUserMessage();
        }

        IsBusy = false;
    }

    void NavigateToTask()
    {
        DialogService.Close(true);
        NavigationManager.NavigateTo($"/tasks/{TaskId}");
    }

    void NavigateToBill()
    {
        if (Occurrence?.BillId is not null)
        {
            DialogService.Close(true);
            NavigationManager.NavigateTo($"/bills/{Occurrence.BillId}");
        }
    }

    static StatusSeverity GetStatusSeverity(OccurrenceStatus status) => status switch
    {
        OccurrenceStatus.Completed => StatusSeverity.Success,
        OccurrenceStatus.InProgress => StatusSeverity.Info,
        OccurrenceStatus.Overdue => StatusSeverity.Danger,
        OccurrenceStatus.Skipped => StatusSeverity.Secondary,
        _ => StatusSeverity.Warning
    };

    static string GetStatusIcon(OccurrenceStatus status) => status switch
    {
        OccurrenceStatus.Completed => "check_circle",
        OccurrenceStatus.InProgress => "play_circle",
        OccurrenceStatus.Overdue => "warning",
        OccurrenceStatus.Skipped => "skip_next",
        _ => "schedule"
    };

    static string GetPriorityColor(TaskPriority priority) => priority switch
    {
        TaskPriority.Critical => "var(--rz-danger)",
        TaskPriority.High => "var(--rz-warning)",
        TaskPriority.Medium => "var(--rz-info)",
        _ => "var(--rz-primary)"
    };

    static string GetCategoryIcon(TaskCategory category) => category switch
    {
        TaskCategory.Cleaning => "cleaning_services",
        TaskCategory.Maintenance => "build",
        TaskCategory.Cooking => "restaurant",
        TaskCategory.Gardening => "yard",
        TaskCategory.Laundry => "local_laundry_service",
        TaskCategory.Shopping => "shopping_cart",
        TaskCategory.PetCare => "pets",
        TaskCategory.ChildCare => "child_care",
        TaskCategory.Organization => "folder",
        _ => "task_alt"
    };
}
