using BlazorUI.Models.Budgets;
using BlazorUI.Models.Common;
using BlazorUI.Services.Contracts;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace BlazorUI.Pages.Budgets;

public partial class BudgetDashboard : IDisposable
{
    [Inject]
    IBudgetService BudgetService { get; set; } = default!;

    [Inject]
    NavigationManager NavigationManager { get; set; } = default!;

    bool IsLoading { get; set; }
    ApiProblemDetails? Error { get; set; }

    BudgetSummaryDto? Summary { get; set; }
    BudgetTrendsDto? Trends { get; set; }

    DateTimeOffset? FromDate { get; set; } = DateTimeOffset.UtcNow.AddMonths(-6);
    DateTimeOffset? ToDate { get; set; } = DateTimeOffset.UtcNow;
    BudgetCategory? SelectedCategory { get; set; }
    BudgetPeriod? SelectedPeriod { get; set; }

    IEnumerable<BudgetCategory> Categories => Enum.GetValues<BudgetCategory>();
    IEnumerable<BudgetPeriod> Periods => Enum.GetValues<BudgetPeriod>();

    CancellationTokenSource _cts = new();

    IReadOnlyList<BudgetStatusDto> SortedStatuses =>
        Summary?.BudgetStatuses.OrderByDescending(s => s.PercentUsed).ToList()
        ?? (IReadOnlyList<BudgetStatusDto>)[];

    string TrendIcon => Trends?.TrendDirection switch
    {
        "increasing" => "trending_up",
        "decreasing" => "trending_down",
        _ => "trending_flat"
    };

    string TrendColor => Trends?.TrendDirection switch
    {
        "increasing" => "var(--rz-danger)",
        "decreasing" => "var(--rz-success)",
        _ => "var(--rz-secondary)"
    };

    string TrendLabel => Trends?.TrendDirection switch
    {
        "increasing" => "Spending increasing",
        "decreasing" => "Spending decreasing",
        _ => "Spending stable"
    };

    protected override async Task OnInitializedAsync()
    {
        await LoadDashboardDataAsync();
    }

    async Task LoadDashboardDataAsync()
    {
        IsLoading = true;
        Error = null;

        var summaryTask = BudgetService.GetSummaryAsync(
            fromDate: FromDate, toDate: ToDate,
            category: SelectedCategory, period: SelectedPeriod,
            cancellationToken: _cts.Token);

        var trendsTask = BudgetService.GetTrendsAsync(
            periods: 6, cancellationToken: _cts.Token);

        var summaryResult = await summaryTask;
        var trendsResult = await trendsTask;

        if (summaryResult.IsSuccess)
            Summary = summaryResult.Value;
        else
            Error = summaryResult.Problem;

        if (trendsResult.IsSuccess)
            Trends = trendsResult.Value;

        IsLoading = false;
    }

    async Task OnApplyFiltersAsync()
    {
        await LoadDashboardDataAsync();
    }

    void NavigateToBudget(Guid budgetId)
    {
        NavigationManager.NavigateTo($"/budgets/{budgetId}");
    }

    void NavigateToBudgets()
    {
        NavigationManager.NavigateTo("/budgets");
    }

    static string GetStatusColor(decimal percentUsed)
    {
        if (percentUsed >= 80) return "var(--rz-danger)";
        if (percentUsed >= 60) return "var(--rz-warning)";
        return "var(--rz-success)";
    }

    static BadgeStyle GetStatusBadgeStyle(string status) => status switch
    {
        "over" => BadgeStyle.Danger,
        "warning" => BadgeStyle.Warning,
        "on-track" => BadgeStyle.Success,
        _ => BadgeStyle.Info
    };

    static string GetStatusDisplayLabel(string status) => status switch
    {
        "over" => "Over Budget",
        "warning" => "Warning",
        "on-track" => "On Track",
        "under" => "Under Budget",
        _ => status
    };

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}
