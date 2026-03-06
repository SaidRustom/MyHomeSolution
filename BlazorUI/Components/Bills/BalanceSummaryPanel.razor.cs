using BlazorUI.Models.Bills;
using BlazorUI.Models.Common;
using BlazorUI.Services.Contracts;
using Microsoft.AspNetCore.Components;

namespace BlazorUI.Components.Bills;

public partial class BalanceSummaryPanel : IDisposable
{
    [Inject]
    IBillService BillService { get; set; } = default!;

    IReadOnlyList<UserBalanceDto> Balances { get; set; } = [];

    bool IsLoading { get; set; }

    ApiProblemDetails? Error { get; set; }

    decimal TotalOwed => Balances.Sum(b => b.TotalOwed);

    decimal TotalOwing => Balances.Sum(b => b.TotalOwing);

    decimal NetBalance => Balances.Sum(b => b.NetBalance);

    CancellationTokenSource _cts = new();

    protected override async Task OnInitializedAsync()
    {
        await LoadBalancesAsync();
    }

    async Task LoadBalancesAsync()
    {
        IsLoading = true;
        Error = null;

        var result = await BillService.GetBalancesAsync(cancellationToken: _cts.Token);

        if (result.IsSuccess)
        {
            Balances = result.Value;
        }
        else
        {
            Error = result.Problem;
        }

        IsLoading = false;
    }

    public async Task RefreshAsync()
    {
        await LoadBalancesAsync();
        StateHasChanged();
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}
