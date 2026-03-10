using BlazorUI.Models.Bills;
using BlazorUI.Models.Common;
using BlazorUI.Models.Enums;
using BlazorUI.Services.Contracts;
using Microsoft.AspNetCore.Components;

namespace BlazorUI.Pages.Bills;

public partial class BillDashboard : IDisposable
{
    [Inject]
    IBillService BillService { get; set; } = default!;

    [Inject]
    NavigationManager NavigationManager { get; set; } = default!;

    SpendingSummaryDto? Summary { get; set; }

    IReadOnlyList<UserBalanceDto> Balances { get; set; } = [];

    PaginatedList<BillBriefDto> RecentBills { get; set; } = new();

    /// <summary>Bills with a BillDate in the past or today that are fully paid.</summary>
    List<BillBriefDto> PastBills => RecentBills.Items
        .Where(b => b.BillDate <= DateTimeOffset.Now && b.IsFullyPaid)
        .ToList();

    /// <summary>Future bills that are fully paid (BillDate > today).</summary>
    List<BillBriefDto> UpcomingBills => RecentBills.Items
        .Where(b => b.BillDate > DateTimeOffset.Now && b.IsFullyPaid)
        .ToList();

    bool IsLoading { get; set; }

    ApiProblemDetails? Error { get; set; }

    // Date range for summary
    DateTimeOffset? FromDate { get; set; }
    DateTimeOffset? ToDate { get; set; }

    // Analysis filters
    IEnumerable<string>? SelectedUserIds { get; set; }
    IEnumerable<BillCategory>? SelectedBillCategories { get; set; }

    IEnumerable<BillCategory> AllBillCategories => Enum.GetValues<BillCategory>();

    /// <summary>Users available in the summary data for filtering.</summary>
    IReadOnlyList<UserSpendingDto> AvailableUsers =>
        Summary?.ByUser ?? [];

    bool HasAnalysisFilters =>
        (SelectedUserIds?.Any() == true)
        || (SelectedBillCategories?.Any() == true);

    /// <summary>Category spending data filtered by selected bill categories.</summary>
    IReadOnlyList<CategorySpendingDto> FilteredByCategory
    {
        get
        {
            if (Summary is null) return [];

            var data = Summary.ByCategory.AsEnumerable();

            if (SelectedBillCategories?.Any() == true)
            {
                var selected = SelectedBillCategories.ToHashSet();
                data = data.Where(c => selected.Contains(c.Category));
            }

            return data.ToList();
        }
    }

    /// <summary>User spending data filtered by selected users.</summary>
    IReadOnlyList<UserSpendingDto> FilteredByUser
    {
        get
        {
            if (Summary is null) return [];

            var data = Summary.ByUser.AsEnumerable();

            if (SelectedUserIds?.Any() == true)
            {
                var selected = SelectedUserIds.ToHashSet();
                data = data.Where(u => selected.Contains(u.UserId));
            }

            return data.ToList();
        }
    }

    void ClearAnalysisFilters()
    {
        SelectedUserIds = null;
        SelectedBillCategories = null;
    }

    CancellationTokenSource _cts = new();

    protected override async Task OnInitializedAsync()
    {
        // Default to current month
        var now = DateTimeOffset.Now;
        FromDate = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, now.Offset);
        ToDate = now;

        await LoadDashboardDataAsync();
    }

    async Task LoadDashboardDataAsync()
    {
        IsLoading = true;
        Error = null;

        var summaryTask = BillService.GetSpendingSummaryAsync(FromDate, ToDate, _cts.Token);
        var balancesTask = BillService.GetBalancesAsync(cancellationToken: _cts.Token);
        var recentTask = BillService.GetBillsAsync(pageNumber: 1, pageSize: 12, cancellationToken: _cts.Token);

        await Task.WhenAll(summaryTask, balancesTask, recentTask);

        var summaryResult = await summaryTask;
        var balancesResult = await balancesTask;
        var recentResult = await recentTask;

        if (summaryResult.IsSuccess)
            Summary = summaryResult.Value;
        else
            Error = summaryResult.Problem;

        if (balancesResult.IsSuccess)
            Balances = balancesResult.Value;

        if (recentResult.IsSuccess)
            RecentBills = recentResult.Value;

        IsLoading = false;
    }

    async Task OnDateRangeChangedAsync()
    {
        await LoadDashboardDataAsync();
    }

    void NavigateToBills()
    {
        NavigationManager.NavigateTo("/bills");
    }

    void NavigateToBill(BillBriefDto bill)
    {
        NavigationManager.NavigateTo($"/bills/{bill.Id}");
    }

    string GetNetBalanceDescription()
    {
        if (Summary is null) return string.Empty;

        return Summary.NetBalance switch
        {
            > 0 => "You are owed overall",
            < 0 => "You owe overall",
            _ => "All settled up"
        };
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}
