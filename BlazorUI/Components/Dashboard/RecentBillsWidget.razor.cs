using System.Security.Claims;
using BlazorUI.Models.Bills;
using BlazorUI.Models.Enums;
using BlazorUI.Services.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace BlazorUI.Components.Dashboard;

public partial class RecentBillsWidget
{
    [Inject] IBillService BillService { get; set; } = default!;
    [Inject] NavigationManager NavigationManager { get; set; } = default!;

    [CascadingParameter]
    private Task<AuthenticationState> AuthState { get; set; } = default!;

    enum BillFilter { All, Mine, Shared }

    List<BillBriefDto> _bills = [];
    bool _isLoading;
    BillFilter _filter = BillFilter.All;
    string? _currentUserId;
    decimal _totalSpent => _bills.Sum(b => b.Amount);

    protected override async Task OnInitializedAsync()
    {
        var state = await AuthState;
        _currentUserId = state.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        await LoadBillsAsync();
    }

    async Task LoadBillsAsync()
    {
        _isLoading = true;

        string? paidByUserId = _filter == BillFilter.Mine ? _currentUserId : null;
        string? splitWithUserId = _filter == BillFilter.Shared ? _currentUserId : null;

        var fromDate = DateTimeOffset.Now.AddDays(-30);
        var result = await BillService.GetBillsAsync(
            pageNumber: 1,
            pageSize: 5,
            fromDate: fromDate,
            paidByUserId: paidByUserId,
            splitWithUserId: splitWithUserId,
            sortBy: "BillDate",
            sortDirection: "desc");

        if (result.IsSuccess)
            _bills = result.Value.Items.ToList();

        _isLoading = false;
    }

    async Task OnFilterChangedAsync(BillFilter newFilter)
    {
        _filter = newFilter;
        await LoadBillsAsync();
    }
}
