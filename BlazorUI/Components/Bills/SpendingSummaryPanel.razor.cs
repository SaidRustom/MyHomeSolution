using BlazorUI.Models.Bills;
using BlazorUI.Models.Common;
using BlazorUI.Services.Contracts;
using Microsoft.AspNetCore.Components;

namespace BlazorUI.Components.Bills;

public partial class SpendingSummaryPanel : IDisposable
{
    [Inject]
    IBillService BillService { get; set; } = default!;

    [Parameter]
    public DateTimeOffset? FromDate { get; set; }

    [Parameter]
    public DateTimeOffset? ToDate { get; set; }

    SpendingSummaryDto? Summary { get; set; }

    bool IsLoading { get; set; }

    ApiProblemDetails? Error { get; set; }

    CancellationTokenSource _cts = new();

    protected override async Task OnInitializedAsync()
    {
        await LoadSummaryAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        await LoadSummaryAsync();
    }

    async Task LoadSummaryAsync()
    {
        IsLoading = true;
        Error = null;

        var result = await BillService.GetSpendingSummaryAsync(FromDate, ToDate, _cts.Token);

        if (result.IsSuccess)
        {
            Summary = result.Value;
        }
        else
        {
            Error = result.Problem;
        }

        IsLoading = false;
    }

    public async Task RefreshAsync()
    {
        await LoadSummaryAsync();
        StateHasChanged();
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}
