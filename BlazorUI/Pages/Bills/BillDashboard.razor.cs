using BlazorUI.Models.Bills;
using BlazorUI.Models.Common;
using BlazorUI.Models.Enums;
using BlazorUI.Models.ShoppingLists;
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
    [Inject] IBillService BillService { get; set; } = default!;
    [Inject] IShoppingListService ShoppingListService { get; set; } = default!;
    [Inject] NavigationManager NavigationManager { get; set; } = default!;

    [CascadingParameter]
    private Task<AuthenticationState> AuthState { get; set; } = default!;

    // ── Raw server data ──
    SpendingSummaryDto? Summary { get; set; }
    IReadOnlyList<UserBalanceDto> Balances { get; set; } = [];
    PaginatedList<BillBriefDto> RecentBills { get; set; } = new();
    IReadOnlyList<ShoppingListBriefDto> ShoppingLists { get; set; } = [];

    // ── UI state ──
    bool IsLoading { get; set; }
    bool IsApplyingFilters { get; set; }
    ApiProblemDetails? Error { get; set; }
    int _selectedTab;
    int _previousTab;
    bool _showAdvanced;
    string? _currentUserId;
    CancellationTokenSource _cts = new();

    // ── Primary filters ──
    UserScopeFilter _userScope = UserScopeFilter.All;
    string? _selectedScopeUserId;
    DateTimeOffset? FromDate { get; set; }
    DateTimeOffset? ToDate { get; set; }

    // ── Tab-level sub-filters ──
    RecentBillsFilter _recentFilter = RecentBillsFilter.All;
    UpcomingPaidByFilter _upcomingPaidByFilter = UpcomingPaidByFilter.All;
    UpcomingWithinFilter _upcomingWithinFilter = UpcomingWithinFilter.Month;

    // ── Advanced filters ──
    IEnumerable<string> SelectedUserIds { get; set; } = [];
    IEnumerable<BillCategory> SelectedBillCategories { get; set; } = [];
    bool? AnalysisPaymentStatus { get; set; }
    bool? AnalysisHasLinkedTask { get; set; }
    Guid? AnalysisShoppingListId { get; set; }
    bool _sharedWithAny;

    // ══════════════════════════════════════════════════════════════
    // DERIVED DATA — every property below flows from FilteredBills
    // so that ALL filters apply to ALL components uniformly.
    // ══════════════════════════════════════════════════════════════

    IEnumerable<BillCategory> AllBillCategories => Enum.GetValues<BillCategory>();

    IReadOnlyList<UserSpendingDto> AvailableUsers => Summary?.ByUser ?? [];

    List<ScopeUserOption> ScopeUserOptions
    {
        get
        {
            if (Summary?.ByUser is null) return [];
            return Summary.ByUser
                .Select(u => new ScopeUserOption
                {
                    UserId = u.UserId,
                    DisplayName = u.UserFullName ?? u.UserId
                })
                .ToList();
        }
    }

    bool HasAdvancedFilters =>
        !string.IsNullOrEmpty(_selectedScopeUserId)
        || SelectedBillCategories.Any()
        || AnalysisPaymentStatus.HasValue
        || AnalysisHasLinkedTask.HasValue
        || AnalysisShoppingListId.HasValue
        || SelectedUserIds.Any()
        || _sharedWithAny;

    bool HasAnyFilter =>
        HasAdvancedFilters
        || _userScope == UserScopeFilter.MeOnly;

    // ── Central filtered list — single source of truth ──
    List<BillBriefDto> FilteredBills =>
        RecentBills.Items
            .Where(PassesAllFilters)
            .OrderByDescending(b => b.BillDate)
            .ToList();

    // ── Stat cards — computed from FilteredBills so filters apply ──
    decimal ComputedTotalSpent => FilteredBills.Sum(b => b.Amount);
    int ComputedBillCount => FilteredBills.Count;

    // ── Bill lists ──
    List<BillBriefDto> PastBills
    {
        get
        {
            var bills = FilteredBills.Where(b => b.IsFullyPaid);
            return _recentFilter switch
            {
                RecentBillsFilter.PaidByMe => bills.Where(b => IsCurrentUser(b.PaidByUserId)).ToList(),
                RecentBillsFilter.PaidByOthers => bills.Where(b => !IsCurrentUser(b.PaidByUserId)).ToList(),
                _ => bills.ToList()
            };
        }
    }

    List<BillBriefDto> UpcomingBills
    {
        get
        {
            var cutoff = _upcomingWithinFilter switch
            {
                UpcomingWithinFilter.Week => DateTimeOffset.Now.AddDays(7),
                UpcomingWithinFilter.TwoWeeks => DateTimeOffset.Now.AddDays(14),
                _ => DateTimeOffset.Now.AddMonths(1)
            };

            var bills = RecentBills.Items
                .Where(b => !b.IsFullyPaid && b.BillDate <= cutoff)
                .Where(PassesAllFilters)
                .OrderBy(b => b.BillDate);

            return _upcomingPaidByFilter switch
            {
                UpcomingPaidByFilter.Me => bills.Where(b => IsCurrentUser(b.PaidByUserId)).ToList(),
                UpcomingPaidByFilter.Others => bills.Where(b => !IsCurrentUser(b.PaidByUserId)).ToList(),
                _ => bills.ToList()
            };
        }
    }

    List<BillBriefDto> OverdueBills =>
        UpcomingBills.Where(b => b.BillDate.Date < DateTimeOffset.Now.Date).ToList();

    decimal UpcomingTotal => UpcomingBills.Sum(b => b.Amount);

    Dictionary<string, List<BillBriefDto>> UpcomingBillsGrouped
    {
        get
        {
            var today = DateTimeOffset.Now.Date;
            var groups = new Dictionary<string, List<BillBriefDto>>();

            foreach (var bill in UpcomingBills)
            {
                var days = (bill.BillDate.Date - today).Days;
                var key = days switch
                {
                    < 0 => "Overdue",
                    0 => "Today",
                    1 => "Tomorrow",
                    <= 7 => "This Week",
                    <= 14 => "Next Week",
                    _ => "Later"
                };

                if (!groups.ContainsKey(key))
                    groups[key] = [];
                groups[key].Add(bill);
            }

            return groups;
        }
    }

    // ── Balances — filtered by user scope ──
    IReadOnlyList<UserBalanceDto> FilteredBalances
    {
        get
        {
            if (_userScope == UserScopeFilter.MeOnly) return [];
            if (_sharedWithAny)
                return Balances; // shared-with implies all counterparties
            if (!string.IsNullOrEmpty(_selectedScopeUserId))
                return Balances
                    .Where(b => string.Equals(b.CounterpartyUserId, _selectedScopeUserId, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            return Balances;
        }
    }

    // ── Charts — derived from FilteredBills so all filters apply ──
    IReadOnlyList<CategorySpendingDto> FilteredByCategory
    {
        get
        {
            var bills = FilteredBills;
            if (bills.Count == 0) return [];

            return bills
                .GroupBy(b => b.Category)
                .Select(g => new CategorySpendingDto
                {
                    Category = g.Key,
                    TotalAmount = g.Sum(b => b.Amount),
                    BillCount = g.Count()
                })
                .OrderByDescending(c => c.TotalAmount)
                .ToList();
        }
    }

    IReadOnlyList<UserSpendingDto> FilteredByUser
    {
        get
        {
            if (Summary?.ByUser is null || Summary.ByUser.Count == 0) return [];

            var filtered = FilteredBills;
            if (filtered.Count == 0) return [];

            // Collect payer user IDs that appear in the filtered bills
            var activePayers = filtered
                .Select(b => b.PaidByUserId)
                .Where(id => id is not null)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var data = Summary.ByUser.AsEnumerable();

            // Only show users who appear in filtered bills
            data = data.Where(u => activePayers.Contains(u.UserId));

            // Apply spending users filter
            if (SelectedUserIds.Any())
            {
                var selected = SelectedUserIds.ToHashSet();
                data = data.Where(u => selected.Contains(u.UserId));
            }

            if (!string.IsNullOrEmpty(_selectedScopeUserId))
                data = data.Where(u => string.Equals(u.UserId, _selectedScopeUserId, StringComparison.OrdinalIgnoreCase));

            return data.ToList();
        }
    }

    // ══════════════════════════════════════════════════════════════
    // LIFECYCLE
    // ══════════════════════════════════════════════════════════════

    protected override async Task OnInitializedAsync()
    {
        var state = await AuthState;
        _currentUserId = state.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? state.User.FindFirst("sub")?.Value;

        var now = DateTimeOffset.Now;
        FromDate = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, now.Offset);
        ToDate = now;

        await LoadShoppingListsAsync();
        await LoadDashboardDataAsync();
    }

    // ══════════════════════════════════════════════════════════════
    // DATA LOADING
    // ══════════════════════════════════════════════════════════════

    async Task LoadShoppingListsAsync()
    {
        var result = await ShoppingListService.GetShoppingListsAsync(
            pageNumber: 1, pageSize: 100, cancellationToken: _cts.Token);
        if (result.IsSuccess)
            ShoppingLists = result.Value.Items.ToList();
    }

    async Task LoadDashboardDataAsync()
    {
        IsLoading = true;
        Error = null;

        var summaryTask = BillService.GetSpendingSummaryAsync(FromDate, ToDate, _cts.Token);
        var balancesTask = BillService.GetBalancesAsync(
            counterpartyUserId: _selectedScopeUserId, cancellationToken: _cts.Token);
        var recentTask = BillService.GetBillsAsync(
            pageNumber: 1, pageSize: 50,
            splitWithUserId: _selectedScopeUserId,
            shoppingListId: AnalysisShoppingListId,
            fromDate: FromDate,
            toDate: ToDate,
            cancellationToken: _cts.Token);

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

    // ══════════════════════════════════════════════════════════════
    // FILTER HANDLERS
    // ══════════════════════════════════════════════════════════════

    async Task OnFiltersChangedAsync()
    {
        if (_userScope == UserScopeFilter.MeOnly)
            _selectedScopeUserId = null;

        IsApplyingFilters = true;
        StateHasChanged();
        await Task.Yield();

        await LoadDashboardDataAsync();

        IsApplyingFilters = false;
    }

    async Task ClearAllFiltersAsync()
    {
        _userScope = UserScopeFilter.All;
        _selectedScopeUserId = null;
        SelectedUserIds = [];
        SelectedBillCategories = [];
        AnalysisPaymentStatus = null;
        AnalysisHasLinkedTask = null;
        AnalysisShoppingListId = null;
        _sharedWithAny = false;
        _recentFilter = RecentBillsFilter.All;
        _upcomingPaidByFilter = UpcomingPaidByFilter.All;
        _upcomingWithinFilter = UpcomingWithinFilter.Month;

        IsApplyingFilters = true;
        StateHasChanged();
        await Task.Yield();

        await LoadDashboardDataAsync();

        IsApplyingFilters = false;
    }

    async Task OnTabChangedAsync(int index)
    {
        if (index == _selectedTab) return;
        _previousTab = _selectedTab;
        _selectedTab = index;

        IsApplyingFilters = true;
        StateHasChanged();
        await Task.Yield();

        // Tab switch is client-side — brief yield lets the spinner render
        IsApplyingFilters = false;
    }

    // ══════════════════════════════════════════════════════════════
    // UNIFIED FILTER PREDICATE
    // ══════════════════════════════════════════════════════════════

    bool PassesAllFilters(BillBriefDto bill)
    {
        // User scope
        if (_userScope == UserScopeFilter.MeOnly)
        {
            if (bill.SplitCount > 1 || !IsCurrentUser(bill.PaidByUserId))
                return false;
        }

        if (!string.IsNullOrEmpty(_selectedScopeUserId) && bill.SplitCount <= 1)
            return false;

        // Shared-with filter
        if (_sharedWithAny && bill.SplitCount <= 1)
            return false;

        // Category
        if (SelectedBillCategories.Any())
        {
            if (!SelectedBillCategories.Contains(bill.Category))
                return false;
        }

        // Payment status
        if (AnalysisPaymentStatus.HasValue && bill.IsFullyPaid != AnalysisPaymentStatus.Value)
            return false;

        // Linked to task
        if (AnalysisHasLinkedTask.HasValue && bill.HasLinkedTask != AnalysisHasLinkedTask.Value)
            return false;

        return true;
    }

    // ══════════════════════════════════════════════════════════════
    // NAVIGATION
    // ══════════════════════════════════════════════════════════════

    void NavigateToBills() => NavigationManager.NavigateTo("/bills");

    void NavigateToBill(BillBriefDto bill) => NavigationManager.NavigateTo($"/bills/{bill.Id}");

    // ══════════════════════════════════════════════════════════════
    // HELPERS
    // ══════════════════════════════════════════════════════════════

    bool IsCurrentUser(string? userId) =>
        string.Equals(userId, _currentUserId, StringComparison.OrdinalIgnoreCase);

    string GetNetBalanceDescription()
    {
        var filtered = FilteredBills;
        if (filtered.Count == 0) return "No data";
        var paid = filtered.Where(b => b.IsFullyPaid).Sum(b => b.Amount);
        var unpaid = filtered.Where(b => !b.IsFullyPaid).Sum(b => b.Amount);
        var diff = paid - unpaid;
        return diff switch
        {
            > 0 => "You are owed overall",
            < 0 => "You owe overall",
            _ => "All settled up"
        };
    }

    /// <summary>Computes the net balance from the filtered bill set.</summary>
    decimal ComputedNetBalance
    {
        get
        {
            if (Summary is null) return 0;
            // When no client-side filters are active, use the server-provided balance
            if (!HasAnyFilter) return Summary.NetBalance;
            // Otherwise approximate from filtered bills
            var filtered = FilteredBills;
            var paidByMe = filtered.Where(b => IsCurrentUser(b.PaidByUserId)).Sum(b => b.Amount);
            var paidByOthers = filtered.Where(b => !IsCurrentUser(b.PaidByUserId)).Sum(b => b.Amount);
            return paidByMe - paidByOthers;
        }
    }

    static string GetInitials(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "?";
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2
            ? $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant()
            : name[..Math.Min(2, name.Length)].ToUpperInvariant();
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
