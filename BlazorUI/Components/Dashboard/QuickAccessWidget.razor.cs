using BlazorUI.Models.Budgets;
using BlazorUI.Models.ShoppingLists;
using BlazorUI.Services.Contracts;
using Microsoft.AspNetCore.Components;

namespace BlazorUI.Components.Dashboard;

public partial class QuickAccessWidget
{
    [Inject] IShoppingListService ShoppingListService { get; set; } = default!;
    [Inject] IBudgetService BudgetService { get; set; } = default!;
    [Inject] NavigationManager NavigationManager { get; set; } = default!;

    List<ShoppingListBriefDto> _activeLists = [];
    List<BudgetBriefDto> _topBudgets = [];
    bool _isLoading;

    protected override async Task OnInitializedAsync()
    {
        _isLoading = true;

        var listsTask = ShoppingListService.GetShoppingListsAsync(
            pageNumber: 1, pageSize: 5, isCompleted: false);
        var budgetsTask = BudgetService.GetBudgetsAsync(
            pageNumber: 1, pageSize: 5, sortBy: "CurrentPeriodPercentUsed", sortDirection: "desc");

        await Task.WhenAll(listsTask, budgetsTask);

        if (listsTask.Result.IsSuccess)
        {
            _activeLists = listsTask.Result.Value.Items
                .Where(l => l.TotalItems - l.CheckedItems > 0)
                .OrderByDescending(l => l.CreatedAt)
                .ToList();
        }

        if (budgetsTask.Result.IsSuccess)
            _topBudgets = budgetsTask.Result.Value.Items.ToList();

        _isLoading = false;
    }

    static string GetBudgetColor(decimal pct) => pct switch
    {
        >= 100 => "var(--rz-danger)",
        >= 80 => "var(--rz-warning)",
        _ => "var(--rz-success)"
    };
}
