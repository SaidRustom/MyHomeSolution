using BlazorUI.Models.Budgets;
using BlazorUI.Models.Common;
using BlazorUI.Services.Contracts;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace BlazorUI.Pages.Budgets;

public enum QuickPeriod { ThisMonth, LastMonth, Last3Months, Last6Months, ThisYear, Custom }
public enum BudgetStatusFilter { All, OverBudget, Warning, OnTrack }

public partial class BudgetDashboard : IDisposable
{
    [Inject] IBudgetService BudgetService { get; set; } = default!;
    [Inject] NavigationManager NavigationManager { get; set; } = default!;

    // ── Raw server data ──
    BudgetSummaryDto? Summary { get; set; }
    BudgetTrendsDto? Trends { get; set; }

    // ── UI state ──
    bool IsLoading { get; set; }
    bool IsApplyingFilters { get; set; }
    ApiProblemDetails? Error { get; set; }
    int _selectedTab;
    bool _showAdvanced;
    CancellationTokenSource _cts = new();

    // ── Primary filters ──
    QuickPeriod _quickPeriod = QuickPeriod.ThisMonth;
    DateTimeOffset? FromDate { get; set; }
    DateTimeOffset? ToDate { get; set; }
    BudgetCategory? SelectedCategory { get; set; }

    // ── Advanced filters ──
    BudgetPeriod? SelectedPeriod { get; set; }
    BudgetStatusFilter _statusFilter = BudgetStatusFilter.All;
    bool? _isRecurringFilter;
    string? _searchTerm;

    // ── Enums for dropdowns ──
    IEnumerable<BudgetCategory> AllCategories => Enum.GetValues<BudgetCategory>();
    IEnumerable<BudgetPeriod> AllPeriods => Enum.GetValues<BudgetPeriod>();

    // ══════════════════════════════════════════════════════════════
    // FILTER PREDICATES & DERIVED DATA
    // ══════════════════════════════════════════════════════════════

    bool HasAdvancedFilters =>
        SelectedPeriod.HasValue
        || _statusFilter != BudgetStatusFilter.All
        || _isRecurringFilter.HasValue
        || !string.IsNullOrWhiteSpace(_searchTerm);

    bool HasAnyFilter =>
        HasAdvancedFilters
        || SelectedCategory.HasValue
        || _quickPeriod != QuickPeriod.ThisMonth;

    /// <summary>All budget statuses after applying client-side filters.</summary>
    List<BudgetStatusDto> FilteredStatuses
    {
        get
        {
            if (Summary is null) return [];
            var items = Summary.BudgetStatuses.AsEnumerable();

            if (_statusFilter != BudgetStatusFilter.All)
            {
                items = _statusFilter switch
                {
                    BudgetStatusFilter.OverBudget => items.Where(s => s.Status == "over"),
                    BudgetStatusFilter.Warning => items.Where(s => s.Status == "warning"),
                    BudgetStatusFilter.OnTrack => items.Where(s => s.Status is "on-track" or "under"),
                    _ => items
                };
            }

            if (!string.IsNullOrWhiteSpace(_searchTerm))
            {
                var term = _searchTerm.Trim();
                items = items.Where(s => s.BudgetName.Contains(term, StringComparison.OrdinalIgnoreCase));
            }

            return items.OrderByDescending(s => s.PercentUsed).ToList();
        }
    }

    /// <summary>Budgets that are over budget or in warning state.</summary>
    List<BudgetStatusDto> AtRiskBudgets =>
        FilteredStatuses.Where(s => s.Status is "over" or "warning").ToList();

    /// <summary>Category spending derived from the filtered status list.</summary>
    List<BudgetCategorySpendingDto> FilteredByCategory
    {
        get
        {
            if (Summary is null) return [];
            var statuses = FilteredStatuses;
            if (statuses.Count == 0) return [];

            return statuses
                .GroupBy(s => s.Category)
                .Select(g => new BudgetCategorySpendingDto
                {
                    Category = g.Key,
                    Budgeted = g.Sum(s => s.Budgeted),
                    Spent = g.Sum(s => s.Spent),
                    Remaining = g.Sum(s => s.Remaining),
                    PercentUsed = g.Sum(s => s.Budgeted) > 0
                        ? Math.Round(g.Sum(s => s.Spent) / g.Sum(s => s.Budgeted) * 100, 2) : 0,
                    BudgetCount = g.Count()
                })
                .OrderByDescending(c => c.Spent)
                .ToList();
        }
    }

    /// <summary>Computed stats from filtered data.</summary>
    decimal ComputedTotalBudgeted => FilteredStatuses.Sum(s => s.Budgeted);
    decimal ComputedTotalSpent => FilteredStatuses.Sum(s => s.Spent);
    decimal ComputedTotalRemaining => ComputedTotalBudgeted - ComputedTotalSpent;
    decimal ComputedPercentUsed => ComputedTotalBudgeted > 0
        ? Math.Round(ComputedTotalSpent / ComputedTotalBudgeted * 100, 1) : 0;
    int ComputedBudgetCount => FilteredStatuses.Count;
    int ComputedOverBudgetCount => FilteredStatuses.Count(s => s.Status == "over");
    int ComputedOnTrackCount => FilteredStatuses.Count(s => s.Status is "on-track" or "under");

    // ── Trend helpers ──
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

    // ── SVG ring computation ──
    double RingCircumference => 2 * Math.PI * 54; // radius = 54
    double RingOffset
    {
        get
        {
            var pct = Math.Min((double)ComputedPercentUsed, 100) / 100.0;
            return RingCircumference * (1 - pct);
        }
    }

    string RingColor => ComputedPercentUsed switch
    {
        >= 100 => "var(--rz-danger)",
        >= 80 => "var(--rz-warning)",
        _ => "var(--rz-success)"
    };

    // ══════════════════════════════════════════════════════════════
    // LIFECYCLE
    // ══════════════════════════════════════════════════════════════

    protected override async Task OnInitializedAsync()
    {
        ApplyQuickPeriodDates();
        await LoadDashboardDataAsync();
    }

    // ══════════════════════════════════════════════════════════════
    // DATA LOADING
    // ══════════════════════════════════════════════════════════════

    async Task LoadDashboardDataAsync()
    {
        IsLoading = true;
        Error = null;

        var summaryTask = BudgetService.GetSummaryAsync(
            fromDate: FromDate, toDate: ToDate,
            category: SelectedCategory, period: SelectedPeriod,
            cancellationToken: _cts.Token);

        var trendsTask = BudgetService.GetTrendsAsync(
            periods: 6, asOfDate: ToDate,
            cancellationToken: _cts.Token);

        await Task.WhenAll(summaryTask, trendsTask);

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

    // ══════════════════════════════════════════════════════════════
    // FILTER HANDLERS
    // ══════════════════════════════════════════════════════════════

    async Task OnQuickPeriodChangedAsync(QuickPeriod period)
    {
        _quickPeriod = period;
        if (period != QuickPeriod.Custom)
            ApplyQuickPeriodDates();

        await OnFiltersChangedAsync();
    }

    void ApplyQuickPeriodDates()
    {
        var now = DateTimeOffset.Now;
        (FromDate, ToDate) = _quickPeriod switch
        {
            QuickPeriod.ThisMonth => (new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, now.Offset), now),
            QuickPeriod.LastMonth => (new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, now.Offset).AddMonths(-1),
                                     new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, now.Offset).AddTicks(-1)),
            QuickPeriod.Last3Months => (now.AddMonths(-3), now),
            QuickPeriod.Last6Months => (now.AddMonths(-6), now),
            QuickPeriod.ThisYear => (new DateTimeOffset(now.Year, 1, 1, 0, 0, 0, now.Offset), now),
            _ => (FromDate, ToDate)
        };
    }

    async Task OnFiltersChangedAsync()
    {
        IsApplyingFilters = true;
        StateHasChanged();
        await Task.Yield();

        await LoadDashboardDataAsync();

        IsApplyingFilters = false;
    }

    async Task ClearAllFiltersAsync()
    {
        _quickPeriod = QuickPeriod.ThisMonth;
        SelectedCategory = null;
        SelectedPeriod = null;
        _statusFilter = BudgetStatusFilter.All;
        _isRecurringFilter = null;
        _searchTerm = null;
        ApplyQuickPeriodDates();

        IsApplyingFilters = true;
        StateHasChanged();
        await Task.Yield();

        await LoadDashboardDataAsync();

        IsApplyingFilters = false;
    }

    async Task OnTabChangedAsync(int index)
    {
        if (index == _selectedTab) return;
        _selectedTab = index;

        IsApplyingFilters = true;
        StateHasChanged();
        await Task.Yield();

        IsApplyingFilters = false;
    }

    // ══════════════════════════════════════════════════════════════
    // NAVIGATION
    // ══════════════════════════════════════════════════════════════

    void NavigateToBudget(Guid budgetId) =>
        NavigationManager.NavigateTo($"/budgets/{budgetId}");

    void NavigateToBudgets() =>
        NavigationManager.NavigateTo("/budgets");

    // ══════════════════════════════════════════════════════════════
    // HELPERS
    // ══════════════════════════════════════════════════════════════

    static string GetStatusColor(decimal percentUsed)
    {
        if (percentUsed >= 100) return "var(--rz-danger)";
        if (percentUsed >= 80) return "var(--rz-warning)";
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

    static string GetStatusCssClass(string status) => status switch
    {
        "over" => "status-over",
        "warning" => "status-warning",
        "on-track" => "status-on-track",
        "under" => "status-under",
        _ => ""
    };

    static readonly string[] CategoryColors =
    [
        "#6366f1", "#ec4899", "#f59e0b", "#10b981", "#3b82f6",
        "#8b5cf6", "#ef4444", "#14b8a6", "#f97316", "#06b6d4",
        "#84cc16", "#e879f9", "#22d3ee", "#fb7185", "#a3e635",
        "#c084fc", "#fbbf24", "#34d399", "#60a5fa"
    ];

    static string GetCategoryColor(int index) =>
        CategoryColors[index % CategoryColors.Length];

    string GetQuickPeriodLabel() => _quickPeriod switch
    {
        QuickPeriod.ThisMonth => "This Month",
        QuickPeriod.LastMonth => "Last Month",
        QuickPeriod.Last3Months => "3 Months",
        QuickPeriod.Last6Months => "6 Months",
        QuickPeriod.ThisYear => "This Year",
        QuickPeriod.Custom => "Custom",
        _ => "This Month"
    };

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}
