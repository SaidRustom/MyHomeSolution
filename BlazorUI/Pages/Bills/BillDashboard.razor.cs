using BlazorUI.Models.Bills;
using BlazorUI.Models.Common;
using BlazorUI.Models.Enums;
using BlazorUI.Services.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace BlazorUI.Pages.Bills;

public enum RecentBillsFilter { All, PaidByMe, PaidByOthers }
public enum UpcomingPaidByFilter { All, Me, Others }
public enum UpcomingWithinFilter { Week, TwoWeeks, Month }
public enum UserScopeFilter { All, MeOnly }

public partial class BillDashboard : IDisposable
{
    [Inject]
    IBillService BillService { get; set; } = default!;

    [Inject]
    NavigationManager NavigationManager { get; set; } = default!;

    [CascadingParameter]
    private Task<AuthenticationState> AuthState { get; set; } = default!;

    SpendingSummaryDto? Summary { get; set; }

    IReadOnlyList<UserBalanceDto> Balances { get; set; } = [];

    PaginatedList<BillBriefDto> RecentBills { get; set; } = new();

    // Tab filter state
    RecentBillsFilter _recentFilter = RecentBillsFilter.All;
    UpcomingPaidByFilter _upcomingPaidByFilter = UpcomingPaidByFilter.All;
    UpcomingWithinFilter _upcomingWithinFilter = UpcomingWithinFilter.Month;

    // User scope filter
    UserScopeFilter _userScope = UserScopeFilter.All;
    string? _selectedScopeUserId;

    /// <summary>Recently paid bills — sorted by date descending.</summary>
    List<BillBriefDto> PastBills
    {
        get
        {
            var bills = RecentBills.Items
                .Where(b => b.IsFullyPaid)
                .Where(b => PassesUserScopeFilter(b))
                .OrderByDescending(b => b.BillDate);

            return _recentFilter switch
            {
                RecentBillsFilter.PaidByMe => bills.Where(b => string.Equals(b.PaidByUserId, _currentUserId, StringComparison.OrdinalIgnoreCase)).ToList(),
                RecentBillsFilter.PaidByOthers => bills.Where(b => !string.Equals(b.PaidByUserId, _currentUserId, StringComparison.OrdinalIgnoreCase)).ToList(),
                _ => bills.ToList()
            };
        }
    }

    /// <summary>Upcoming unpaid bills within the selected timeframe.</summary>
    List<BillBriefDto> UpcomingBills
    {
        get
        {
            var cutoffDate = _upcomingWithinFilter switch
            {
                UpcomingWithinFilter.Week => DateTimeOffset.Now.AddDays(7),
                UpcomingWithinFilter.TwoWeeks => DateTimeOffset.Now.AddDays(14),
                _ => DateTimeOffset.Now.AddMonths(1)
            };

            var bills = RecentBills.Items
                .Where(b => !b.IsFullyPaid && b.BillDate <= cutoffDate)
                .Where(b => PassesUserScopeFilter(b))
                .OrderBy(b => b.BillDate);

            return _upcomingPaidByFilter switch
            {
                UpcomingPaidByFilter.Me => bills.Where(b => string.Equals(b.PaidByUserId, _currentUserId, StringComparison.OrdinalIgnoreCase)).ToList(),
                UpcomingPaidByFilter.Others => bills.Where(b => !string.Equals(b.PaidByUserId, _currentUserId, StringComparison.OrdinalIgnoreCase)).ToList(),
                _ => bills.ToList()
            };
        }
    }

    /// <summary>Balances filtered by selected scope user.</summary>
    IReadOnlyList<UserBalanceDto> FilteredBalances
    {
        get
        {
            if (_userScope == UserScopeFilter.MeOnly)
                return [];

            if (!string.IsNullOrEmpty(_selectedScopeUserId))
                return Balances.Where(b =>
                    string.Equals(b.CounterpartyUserId, _selectedScopeUserId, StringComparison.OrdinalIgnoreCase))
                    .ToList();

            return Balances;
        }
    }

    string? _currentUserId;

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

    /// <summary>Scope user options for the top-level filter.</summary>
    List<ScopeUserOption> ScopeUserOptions
    {
        get
        {
            var options = new List<ScopeUserOption>();
            if (Summary?.ByUser is not null)
            {
                foreach (var u in Summary.ByUser)
                {
                    options.Add(new ScopeUserOption
                    {
                        UserId = u.UserId,
                        DisplayName = u.UserFullName ?? u.UserId
                    });
                }
            }
            return options;
        }
    }

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

            // Also apply scope filter
            if (!string.IsNullOrEmpty(_selectedScopeUserId))
            {
                data = data.Where(u => string.Equals(u.UserId, _selectedScopeUserId, StringComparison.OrdinalIgnoreCase));
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
        var state = await AuthState;
        _currentUserId = state.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? state.User.FindFirst("sub")?.Value;

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
        var balancesTask = BillService.GetBalancesAsync(
            counterpartyUserId: _selectedScopeUserId,
            cancellationToken: _cts.Token);
        var recentTask = BillService.GetBillsAsync(pageNumber: 1, pageSize: 50, cancellationToken: _cts.Token);

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

    async Task OnUserScopeChangedAsync()
    {
        // When scope changes, clear the specific user if switching to All or Me Only
        if (_userScope != UserScopeFilter.All || string.IsNullOrEmpty(_selectedScopeUserId))
        {
            _selectedScopeUserId = null;
        }
        await LoadDashboardDataAsync();
    }

    async Task OnScopeUserChangedAsync()
    {
        await LoadDashboardDataAsync();
    }

    /// <summary>Returns true if a bill passes the user scope filter.</summary>
    bool PassesUserScopeFilter(BillBriefDto bill)
    {
        if (_userScope == UserScopeFilter.MeOnly)
        {
            return string.Equals(bill.PaidByUserId, _currentUserId, StringComparison.OrdinalIgnoreCase);
        }

        // When a specific user is selected, show only bills shared between current user and selected user
        // This means the bill was paid by either the current user or the selected user
        if (!string.IsNullOrEmpty(_selectedScopeUserId))
        {
            return string.Equals(bill.PaidByUserId, _currentUserId, StringComparison.OrdinalIgnoreCase)
                || string.Equals(bill.PaidByUserId, _selectedScopeUserId, StringComparison.OrdinalIgnoreCase);
        }

        return true;
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

public sealed class ScopeUserOption
{
    public required string UserId { get; init; }
    public required string DisplayName { get; init; }
}
