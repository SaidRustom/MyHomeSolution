using BlazorUI.Components.Common;
using BlazorUI.Models.Enums;
using BlazorUI.Models.Occurrences;
using BlazorUI.Services.Contracts;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace BlazorUI.Components.Scheduler;

public partial class UpcomingTasksPanel
{
    [Inject]
    IOccurrenceService OccurrenceService { get; set; } = default!;

    [Inject]
    NotificationService Notifications { get; set; } = default!;

    [Parameter]
    public EventCallback OnOccurrenceAction { get; set; }

    List<CalendarOccurrenceDto> _occurrences = [];
    bool IsLoading { get; set; }
    Guid? _busyId;

    protected override async Task OnInitializedAsync()
    {
        await LoadDataAsync();
    }

    async Task LoadDataAsync()
    {
        IsLoading = true;

        var result = await OccurrenceService.GetUpcomingAsync(pageNumber: 1, pageSize: 15);
        if (result.IsSuccess)
        {
            _occurrences = result.Value.Items.ToList();
        }

        IsLoading = false;
    }

    async Task StartOccurrenceAsync(Guid id)
    {
        _busyId = id;
        var result = await OccurrenceService.StartAsync(id, new StartOccurrenceRequest());

        if (result.IsSuccess)
        {
            Notifications.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Info,
                Summary = "Started",
                Duration = 3000
            });
            await LoadDataAsync();
            await OnOccurrenceAction.InvokeAsync();
        }

        _busyId = null;
    }

    async Task CompleteOccurrenceAsync(Guid id)
    {
        _busyId = id;
        var result = await OccurrenceService.CompleteAsync(id, new CompleteOccurrenceRequest());

        if (result.IsSuccess)
        {
            Notifications.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = "Completed",
                Duration = 3000
            });
            await LoadDataAsync();
            await OnOccurrenceAction.InvokeAsync();
        }

        _busyId = null;
    }

    async Task SkipOccurrenceAsync(Guid id)
    {
        _busyId = id;
        var result = await OccurrenceService.SkipAsync(id, new SkipOccurrenceRequest());

        if (result.IsSuccess)
        {
            Notifications.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Warning,
                Summary = "Skipped",
                Duration = 3000
            });
            await LoadDataAsync();
            await OnOccurrenceAction.InvokeAsync();
        }

        _busyId = null;
    }

    static string GetPriorityColor(TaskPriority priority) => priority switch
    {
        TaskPriority.Critical => "var(--rz-danger)",
        TaskPriority.High => "var(--rz-warning)",
        TaskPriority.Medium => "var(--rz-info)",
        _ => "var(--rz-primary)"
    };

    static StatusSeverity GetStatusSeverity(OccurrenceStatus status) => status switch
    {
        OccurrenceStatus.Completed => StatusSeverity.Success,
        OccurrenceStatus.InProgress => StatusSeverity.Info,
        OccurrenceStatus.Overdue => StatusSeverity.Danger,
        OccurrenceStatus.Skipped => StatusSeverity.Secondary,
        _ => StatusSeverity.Warning
    };
}
