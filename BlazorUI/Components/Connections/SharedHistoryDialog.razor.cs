using BlazorUI.Models.SharedHistory;
using BlazorUI.Services.Contracts;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace BlazorUI.Components.Connections;

public partial class SharedHistoryDialog : IDisposable
{
    [Inject] IUserConnectionService ConnectionService { get; set; } = default!;
    [Inject] NavigationManager NavigationManager { get; set; } = default!;
    [Inject] DialogService DialogService { get; set; } = default!;

    [Parameter, EditorRequired]
    public required string UserId { get; set; }

    SharedHistoryDto? _data;
    bool _isLoading;
    string? _error;
    int _selectedTab;
    CancellationTokenSource _cts = new();

    int TotalSharedEntities => (_data?.SharedBillCount ?? 0)
        + (_data?.SharedBudgetCount ?? 0)
        + (_data?.SharedTaskCount ?? 0)
        + (_data?.SharedShoppingListCount ?? 0);

    protected override async Task OnInitializedAsync()
    {
        _isLoading = true;
        var result = await ConnectionService.GetSharedHistoryAsync(UserId, _cts.Token);

        if (result.IsSuccess)
        {
            _data = result.Value;
        }
        else
        {
            _error = "Failed to load shared history.";
        }

        _isLoading = false;
    }

    string GetConnectionDuration()
    {
        if (_data?.ConnectedSince is null) return "—";

        var elapsed = DateTimeOffset.Now - _data.ConnectedSince.Value;

        if (elapsed.TotalDays >= 365)
        {
            var years = (int)(elapsed.TotalDays / 365);
            return years == 1 ? "1 year" : $"{years} years";
        }

        if (elapsed.TotalDays >= 30)
        {
            var months = (int)(elapsed.TotalDays / 30);
            return months == 1 ? "1 month" : $"{months} months";
        }

        if (elapsed.TotalDays >= 1)
        {
            var days = (int)elapsed.TotalDays;
            return days == 1 ? "1 day" : $"{days} days";
        }

        return "less than a day";
    }

    static string GetBudgetColor(decimal pct) => pct switch
    {
        >= 100 => "var(--rz-danger)",
        >= 80 => "var(--rz-warning)",
        _ => "var(--rz-success)"
    };

    void NavigateToEntity(string url)
    {
        DialogService.Close();
        NavigationManager.NavigateTo(url);
    }

    void Close() => DialogService.Close();

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}
