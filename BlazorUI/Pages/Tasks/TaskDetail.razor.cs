using BlazorUI.Components.Common;
using BlazorUI.Components.Scheduler;
using BlazorUI.Models.Common;
using BlazorUI.Models.Enums;
using BlazorUI.Models.Occurrences;
using BlazorUI.Models.Tasks;
using BlazorUI.Services.Contracts;
using Microsoft.AspNetCore.Components;
using Radzen;

using TaskOccurrenceDto = BlazorUI.Models.Tasks.OccurrenceDto;

namespace BlazorUI.Pages.Tasks;

public partial class TaskDetail : IDisposable
{
    [Parameter]
    public Guid Id { get; set; }

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

    TaskDetailDto? Task { get; set; }

    bool IsLoading { get; set; }

    ApiProblemDetails? Error { get; set; }

    Guid? _busyOccurrenceId;

    CancellationTokenSource _cts = new();

    protected override async Task OnParametersSetAsync()
    {
        await LoadTaskAsync();
    }

    async Task LoadTaskAsync()
    {
        IsLoading = true;
        Error = null;

        var result = await TaskService.GetTaskByIdAsync(Id, _cts.Token);

        if (result.IsSuccess)
        {
            Task = result.Value;
        }
        else
        {
            Error = result.Problem;
        }

        IsLoading = false;
    }

    async Task EditTaskAsync()
    {
        if (Task is null) return;

        var model = TaskFormModel.FromDetail(Task);

        var result = await DialogService.OpenAsync<TaskFormDialog>(
            "Edit Task",
            new Dictionary<string, object>
            {
                { nameof(TaskFormDialog.Model), model },
                { nameof(TaskFormDialog.IsEdit), true }
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
            await LoadTaskAsync();
        }
    }

    async Task DeleteTaskAsync()
    {
        if (Task is null) return;

        var confirmed = await DialogService.OpenAsync<ConfirmDialog>(
            "Delete Task",
            new Dictionary<string, object>
            {
                { nameof(ConfirmDialog.Message), $"Are you sure you want to delete '{Task.Title}'? This will also remove all occurrences and scheduled bills." },
                { nameof(ConfirmDialog.ConfirmText), "Delete" },
                { nameof(ConfirmDialog.ConfirmStyle), ButtonStyle.Danger },
                { nameof(ConfirmDialog.ConfirmIcon), "delete" }
            },
            new DialogOptions
            {
                Width = "450px",
                CloseDialogOnOverlayClick = false
            });

        if (confirmed is true)
        {
            var deleteResult = await TaskService.DeleteTaskAsync(Task.Id, _cts.Token);

            if (deleteResult.IsSuccess)
            {
                Notifications.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = "Task Deleted",
                    Detail = $"'{Task.Title}' has been deleted.",
                    Duration = 4000
                });
                NavigationManager.NavigateTo("/tasks");
            }
            else
            {
                Notifications.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Error",
                    Detail = deleteResult.Problem.ToUserMessage(),
                    Duration = 6000
                });
            }
        }
    }

    async Task StartOccurrenceAsync(Guid occurrenceId)
    {
        _busyOccurrenceId = occurrenceId;
        var result = await OccurrenceService.StartAsync(occurrenceId, new StartOccurrenceRequest(), _cts.Token);

        if (result.IsSuccess)
        {
            Notifications.Notify(new NotificationMessage { Severity = NotificationSeverity.Info, Summary = "Started", Duration = 3000 });
            await LoadTaskAsync();
        }

        _busyOccurrenceId = null;
    }

    async Task CompleteOccurrenceAsync(Guid occurrenceId)
    {
        _busyOccurrenceId = occurrenceId;
        var result = await OccurrenceService.CompleteAsync(occurrenceId, new CompleteOccurrenceRequest(), _cts.Token);

        if (result.IsSuccess)
        {
            Notifications.Notify(new NotificationMessage { Severity = NotificationSeverity.Success, Summary = "Completed", Duration = 3000 });
            await LoadTaskAsync();
        }

        _busyOccurrenceId = null;
    }

    async Task SkipOccurrenceAsync(Guid occurrenceId)
    {
        _busyOccurrenceId = occurrenceId;
        var result = await OccurrenceService.SkipAsync(occurrenceId, new SkipOccurrenceRequest(), _cts.Token);

        if (result.IsSuccess)
        {
            Notifications.Notify(new NotificationMessage { Severity = NotificationSeverity.Warning, Summary = "Skipped", Duration = 3000 });
            await LoadTaskAsync();
        }

        _busyOccurrenceId = null;
    }

    void NavigateToBill(Guid billId)
    {
        NavigationManager.NavigateTo($"/bills/{billId}");
    }

    static StatusSeverity GetStatusSeverity(OccurrenceStatus status) => status switch
    {
        OccurrenceStatus.Completed => StatusSeverity.Success,
        OccurrenceStatus.InProgress => StatusSeverity.Info,
        OccurrenceStatus.Overdue => StatusSeverity.Danger,
        OccurrenceStatus.Skipped => StatusSeverity.Secondary,
        _ => StatusSeverity.Warning
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

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}
