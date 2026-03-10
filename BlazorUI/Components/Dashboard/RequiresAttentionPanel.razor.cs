using BlazorUI.Models.Common;
using BlazorUI.Models.Dashboard;
using BlazorUI.Services.Contracts;
using Microsoft.AspNetCore.Components;

namespace BlazorUI.Components.Dashboard;

public partial class RequiresAttentionPanel
{
    [Inject]
    IDashboardService DashboardService { get; set; } = default!;

    [Inject]
    NavigationManager NavigationManager { get; set; } = default!;

    RequiresAttentionDto? Data { get; set; }

    bool IsLoading { get; set; }

    ApiProblemDetails? Error { get; set; }

    bool HasItems => Data is not null
        && (Data.UnpaidBills.Count > 0 || Data.UrgentTasks.Count > 0);

    int TotalCount => (Data?.UnpaidBills.Count ?? 0) + (Data?.UrgentTasks.Count ?? 0);

    bool IsExpanded { get; set; }

    const int CollapsedMaxItems = 4;

    int RemainingBillSlots => IsExpanded ? int.MaxValue : CollapsedMaxItems;

    int RemainingTaskSlots => IsExpanded
        ? int.MaxValue
        : Math.Max(0, CollapsedMaxItems - Math.Min(Data?.UnpaidBills.Count ?? 0, CollapsedMaxItems));

    bool HasMoreItems => TotalCount > CollapsedMaxItems;

    void ToggleExpanded() => IsExpanded = !IsExpanded;

    protected override async Task OnInitializedAsync()
    {
        await LoadAsync();
    }

    async Task LoadAsync()
    {
        IsLoading = true;
        Error = null;

        var result = await DashboardService.GetRequiresAttentionAsync();

        if (result.IsSuccess)
        {
            Data = result.Value;
        }
        else
        {
            Error = result.Problem;
        }

        IsLoading = false;
    }

    void NavigateToBill(Guid billId) =>
        NavigationManager.NavigateTo($"/bills/{billId}");

    void NavigateToTask(Guid taskId, Guid occurrenceId) =>
        NavigationManager.NavigateTo($"/tasks/{taskId}?occurrenceId={occurrenceId}");

    static string GetPriorityColor(Models.Enums.TaskPriority priority) => priority switch
    {
        Models.Enums.TaskPriority.Critical => "var(--rz-danger)",
        Models.Enums.TaskPriority.High => "var(--rz-warning)",
        Models.Enums.TaskPriority.Medium => "var(--rz-info)",
        _ => "var(--rz-primary)"
    };

    static string GetDueDateLabel(DateOnly dueDate)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        if (dueDate == today) return "Today";
        if (dueDate == today.AddDays(1)) return "Tomorrow";
        if (dueDate == today.AddDays(2)) return "Day after tomorrow";
        return dueDate.ToString("MMM dd");
    }
}
