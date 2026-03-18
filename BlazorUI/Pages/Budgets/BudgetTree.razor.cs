using BlazorUI.Models.Budgets;
using BlazorUI.Models.Common;
using BlazorUI.Services.Contracts;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace BlazorUI.Pages.Budgets;

public partial class BudgetTree : IDisposable
{
    [Inject]
    IBudgetService BudgetService { get; set; } = default!;

    [Inject]
    NavigationManager NavigationManager { get; set; } = default!;

    bool IsLoading { get; set; }
    ApiProblemDetails? Error { get; set; }

    IReadOnlyList<BudgetTreeNodeDto> TreeData { get; set; } = [];

    CancellationTokenSource _cts = new();

    protected override async Task OnInitializedAsync()
    {
        await LoadTreeAsync();
    }

    async Task LoadTreeAsync()
    {
        IsLoading = true;
        Error = null;

        var result = await BudgetService.GetTreeAsync(_cts.Token);

        if (result.IsSuccess)
        {
            TreeData = result.Value;
        }
        else
        {
            Error = result.Problem;
        }

        IsLoading = false;
    }

    void NavigateToBudget(Guid id)
    {
        NavigationManager.NavigateTo($"/budgets/{id}");
    }

    static string GetStatusColor(decimal percentUsed)
    {
        if (percentUsed >= 80) return "var(--rz-danger)";
        if (percentUsed >= 60) return "var(--rz-warning)";
        return "var(--rz-success)";
    }

    static string GetStatusLabel(decimal percentUsed)
    {
        if (percentUsed > 100) return "Over Budget";
        if (percentUsed >= 80) return "Warning";
        return "On Track";
    }

    static BadgeStyle GetStatusBadgeStyle(decimal percentUsed)
    {
        if (percentUsed >= 80) return BadgeStyle.Danger;
        if (percentUsed >= 60) return BadgeStyle.Warning;
        return BadgeStyle.Success;
    }

    static string GetCategoryIcon(BudgetCategory category) => category switch
    {
        BudgetCategory.Groceries => "shopping_cart",
        BudgetCategory.Utilities => "bolt",
        BudgetCategory.Rent => "home",
        BudgetCategory.Transportation => "directions_car",
        BudgetCategory.Entertainment => "movie",
        BudgetCategory.DiningOut => "restaurant",
        BudgetCategory.Healthcare => "local_hospital",
        BudgetCategory.Insurance => "shield",
        BudgetCategory.Savings => "savings",
        BudgetCategory.Subscriptions => "subscriptions",
        BudgetCategory.Clothing => "checkroom",
        BudgetCategory.Education => "school",
        BudgetCategory.PersonalCare => "spa",
        BudgetCategory.HomeImprovement => "build",
        BudgetCategory.Gifts => "card_giftcard",
        BudgetCategory.Travel => "flight",
        BudgetCategory.Pets => "pets",
        _ => "account_balance_wallet"
    };

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}
