using BlazorUI.Models.Budgets;
using BlazorUI.Services.Contracts;
using Microsoft.AspNetCore.Components;

namespace BlazorUI.Components.Dashboard;

public partial class BudgetOverviewWidget
{
    [Inject] IBudgetService BudgetService { get; set; } = default!;
    [Inject] NavigationManager NavigationManager { get; set; } = default!;

    BudgetSummaryDto? _summary;
    bool _isLoading;

    protected override async Task OnInitializedAsync()
    {
        _isLoading = true;
        var result = await BudgetService.GetSummaryAsync();
        if (result.IsSuccess && result.Value.TotalBudgets > 0)
            _summary = result.Value;
        _isLoading = false;
    }

    static string GetStatusColor(decimal percentUsed) => percentUsed switch
    {
        >= 100 => "var(--rz-danger)",
        >= 80 => "var(--rz-warning)",
        _ => "var(--rz-success)"
    };
}
