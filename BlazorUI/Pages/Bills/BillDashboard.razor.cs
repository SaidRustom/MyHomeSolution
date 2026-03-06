using BlazorUI.Models.Bills;
using BlazorUI.Models.Common;
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

    bool IsLoading { get; set; }

    ApiProblemDetails? Error { get; set; }

    // Date range for summary
    DateTimeOffset? FromDate { get; set; }
    DateTimeOffset? ToDate { get; set; }

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
        var recentTask = BillService.GetBillsAsync(pageNumber: 1, pageSize: 5, cancellationToken: _cts.Token);

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
