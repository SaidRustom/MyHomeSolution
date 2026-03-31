using System.Security.Claims;
using BlazorUI.Components.Dashboard;
using BlazorUI.Models.Dashboard;
using BlazorUI.Services.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Radzen;

namespace BlazorUI.Pages;

public partial class Home : IDisposable
{
    [Inject] NavigationManager Navigation { get; set; } = default!;
    [Inject] IDashboardService DashboardService { get; set; } = default!;
    [Inject] DialogService DialogService { get; set; } = default!;

    [CascadingParameter]
    private Task<AuthenticationState> AuthState { get; set; } = default!;

    HomepageLayoutDto? _layout;
    bool _isLoading;
    CancellationTokenSource _cts = new();

    List<HomepageWidgetDto> VisibleWidgets =>
        _layout?.Widgets
            .Where(w => w.IsVisible)
            .OrderBy(w => w.Position)
            .ToList() ?? [];

    string Greeting
    {
        get
        {
            var hour = DateTime.Now.Hour;
            return hour switch
            {
                < 12 => "Good morning",
                < 17 => "Good afternoon",
                _ => "Good evening"
            };
        }
    }

    protected override async Task OnInitializedAsync()
    {
        _isLoading = true;

        var result = await DashboardService.GetHomepageLayoutAsync(_cts.Token);
        if (result.IsSuccess)
            _layout = result.Value;
        else
            _layout = DefaultLayout();

        _isLoading = false;
    }

    async Task OpenCustomizeDialogAsync()
    {
        var currentWidgets = (_layout?.Widgets ?? DefaultLayout().Widgets)
            .Select(w => new EditableWidget
            {
                WidgetType = w.WidgetType,
                ColumnSpan = w.ColumnSpan,
                IsVisible = w.IsVisible,
                Settings = w.Settings
            })
            .ToList();

        var result = await DialogService.OpenAsync<HomepageCustomizeDialog>(
            "Customize Homepage",
            new Dictionary<string, object>
            {
                ["Widgets"] = currentWidgets
            },
            new DialogOptions
            {
                Width = "min(95vw, 520px)",
                CloseDialogOnOverlayClick = true,
                ShowClose = true
            });

        if (result is HomepageLayoutDto newLayout)
        {
            _layout = newLayout;
            StateHasChanged();
        }
    }

    static HomepageLayoutDto DefaultLayout() => new()
    {
        Widgets =
        [
            new() { Id = Guid.Empty, WidgetType = WidgetTypes.RequiresAttention, Position = 0, ColumnSpan = 2, IsVisible = true },
            new() { Id = Guid.Empty, WidgetType = WidgetTypes.TodayTasks, Position = 1, ColumnSpan = 1, IsVisible = true },
            new() { Id = Guid.Empty, WidgetType = WidgetTypes.BudgetOverview, Position = 2, ColumnSpan = 1, IsVisible = true },
            new() { Id = Guid.Empty, WidgetType = WidgetTypes.RecentBills, Position = 3, ColumnSpan = 1, IsVisible = true },
            new() { Id = Guid.Empty, WidgetType = WidgetTypes.QuickAccess, Position = 4, ColumnSpan = 1, IsVisible = true },
        ]
    };

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}
