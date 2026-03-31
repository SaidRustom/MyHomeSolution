using BlazorUI.Models.Enums;
using BlazorUI.Models.Tasks;
using BlazorUI.Services.Contracts;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace BlazorUI.Components.Dashboard;

public partial class TodayTasksWidget
{
    [Inject] ITaskService TaskService { get; set; } = default!;
    [Inject] IOccurrenceService OccurrenceService { get; set; } = default!;
    [Inject] NavigationManager NavigationManager { get; set; } = default!;
    [Inject] NotificationService Notifications { get; set; } = default!;

    List<TodayTaskDto> _tasks = [];
    bool _isLoading;
    bool _isBusy;

    int _totalOccurrences => _tasks.Sum(t => t.Occurrences.Count);
    int _completedCount => _tasks.Sum(t => t.Occurrences.Count(o => o.Status == OccurrenceStatus.Completed));
    double _progressPercent => _totalOccurrences > 0 ? (double)_completedCount / _totalOccurrences * 100 : 0;

    protected override async Task OnInitializedAsync()
    {
        _isLoading = true;
        var result = await TaskService.GetTodayTasksAsync();
        if (result.IsSuccess)
            _tasks = result.Value.ToList();
        _isLoading = false;
    }

    async Task StartOccurrenceAsync(Guid occurrenceId)
    {
        _isBusy = true;
        var result = await OccurrenceService.StartAsync(occurrenceId);
        if (result.IsSuccess)
        {
            UpdateOccurrenceStatus(occurrenceId, OccurrenceStatus.InProgress);
            Notifications.Notify(NotificationSeverity.Success, "Started", duration: 2000);
        }
        _isBusy = false;
    }

    async Task CompleteOccurrenceAsync(Guid occurrenceId)
    {
        _isBusy = true;
        var result = await OccurrenceService.CompleteAsync(occurrenceId);
        if (result.IsSuccess)
        {
            UpdateOccurrenceStatus(occurrenceId, OccurrenceStatus.Completed);
            Notifications.Notify(NotificationSeverity.Success, "Completed!", duration: 2000);
        }
        _isBusy = false;
    }

    async Task SkipOccurrenceAsync(Guid occurrenceId)
    {
        _isBusy = true;
        var result = await OccurrenceService.SkipAsync(occurrenceId);
        if (result.IsSuccess)
        {
            UpdateOccurrenceStatus(occurrenceId, OccurrenceStatus.Skipped);
            Notifications.Notify(NotificationSeverity.Info, "Skipped", duration: 2000);
        }
        _isBusy = false;
    }

    void UpdateOccurrenceStatus(Guid occurrenceId, OccurrenceStatus newStatus)
    {
        foreach (var task in _tasks)
        {
            var occ = task.Occurrences.FirstOrDefault(o => o.Id == occurrenceId);
            if (occ is not null)
            {
                // TodayTaskDto uses IReadOnlyCollection — rebuild list with updated status
                var updated = task.Occurrences.Select(o => o.Id == occurrenceId
                    ? new OccurrenceDto
                    {
                        Id = o.Id,
                        DueDate = o.DueDate,
                        Status = newStatus,
                        AssignedToUserId = o.AssignedToUserId,
                        AssignedToUserFullName = o.AssignedToUserFullName,
                        AssignedToUserAvatarUrl = o.AssignedToUserAvatarUrl,
                        CompletedAt = o.CompletedAt,
                        CompletedByUserId = o.CompletedByUserId,
                        CompletedByUserFullName = o.CompletedByUserFullName,
                        CompletedByUserAvatarUrl = o.CompletedByUserAvatarUrl,
                        Notes = o.Notes,
                        BillId = o.BillId,
                        Bill = o.Bill
                    }
                    : o).ToList();

                // Replace the task in the list
                var idx = _tasks.IndexOf(task);
                _tasks[idx] = task with { Occurrences = updated };
                break;
            }
        }
    }

    static string GetPriorityColor(TaskPriority priority) => priority switch
    {
        TaskPriority.Critical => "var(--rz-danger)",
        TaskPriority.High => "var(--rz-warning)",
        TaskPriority.Medium => "var(--rz-info)",
        _ => "var(--rz-base-400)"
    };
}
