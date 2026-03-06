using BlazorUI.Components.Common;
using BlazorUI.Components.Scheduler;
using BlazorUI.Models.Common;
using BlazorUI.Models.Enums;
using BlazorUI.Models.Tasks;
using BlazorUI.Services.Contracts;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace BlazorUI.Pages.Tasks;

public partial class TaskList : IDisposable
{
    [Inject]
    ITaskService TaskService { get; set; } = default!;

    [Inject]
    DialogService DialogService { get; set; } = default!;

    [Inject]
    NotificationService Notifications { get; set; } = default!;

    [Inject]
    NavigationManager NavigationManager { get; set; } = default!;

    PaginatedList<TaskBriefDto>? Tasks { get; set; }

    bool IsLoading { get; set; }

    ApiProblemDetails? Error { get; set; }

    // Filter state
    string? _searchTerm;
    TaskCategory? _categoryFilter;
    TaskPriority? _priorityFilter;
    bool _activeOnly;

    int _pageNumber = 1;
    int _pageSize = 20;

    CancellationTokenSource _cts = new();

    IEnumerable<TaskCategory> CategoriesList => Enum.GetValues<TaskCategory>();
    IEnumerable<TaskPriority> PrioritiesList => Enum.GetValues<TaskPriority>();

    protected override async Task OnInitializedAsync()
    {
        await LoadTasksAsync();
    }

    async Task LoadTasksAsync()
    {
        IsLoading = true;
        Error = null;

        var result = await TaskService.GetTasksAsync(
            pageNumber: _pageNumber,
            pageSize: _pageSize,
            category: _categoryFilter,
            priority: _priorityFilter,
            searchTerm: _searchTerm,
            notCompletedOnly: _activeOnly ? true : null,
            cancellationToken: _cts.Token);

        if (result.IsSuccess)
        {
            Tasks = result.Value;
        }
        else
        {
            Error = result.Problem;
        }

        IsLoading = false;
    }

    async Task OnGridLoadData(LoadDataArgs args)
    {
        _pageNumber = (args.Skip ?? 0) / _pageSize + 1;
        await LoadTasksAsync();
    }

    async Task OnRowSelectAsync(TaskBriefDto task)
    {
        NavigationManager.NavigateTo($"/tasks/{task.Id}");
    }

    async Task OpenCreateDialogAsync()
    {
        var model = new TaskFormModel();

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
            await LoadTasksAsync();
        }
    }

    async Task EditTaskAsync(Guid taskId)
    {
        var taskResult = await TaskService.GetTaskByIdAsync(taskId, _cts.Token);
        if (!taskResult.IsSuccess) return;

        var model = TaskFormModel.FromDetail(taskResult.Value);

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
            await LoadTasksAsync();
        }
    }

    async Task DeleteTaskAsync(TaskBriefDto task)
    {
        var confirmed = await DialogService.OpenAsync<ConfirmDialog>(
            "Delete Task",
            new Dictionary<string, object>
            {
                { nameof(ConfirmDialog.Message), $"Are you sure you want to delete '{task.Title}'? This will also remove all occurrences." },
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
            var result = await TaskService.DeleteTaskAsync(task.Id, _cts.Token);

            if (result.IsSuccess)
            {
                Notifications.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = "Task Deleted",
                    Detail = $"'{task.Title}' has been deleted.",
                    Duration = 4000
                });
                await LoadTasksAsync();
            }
            else
            {
                Notifications.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Error",
                    Detail = result.Problem.ToUserMessage(),
                    Duration = 6000
                });
            }
        }
    }

    void NavigateToScheduler()
    {
        NavigationManager.NavigateTo("/scheduler");
    }

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
