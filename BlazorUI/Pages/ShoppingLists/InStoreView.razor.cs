using BlazorUI.Components.ShoppingLists;
using BlazorUI.Models.Common;
using BlazorUI.Models.ShoppingLists;
using BlazorUI.Services.Contracts;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace BlazorUI.Pages.ShoppingLists;

public sealed record InStoreGroupViewModel
{
    public required string Category { get; init; }
    public required string Icon { get; init; }
    public int SortOrder { get; init; }
    public List<ShoppingItemDto> Items { get; init; } = [];
}

public partial class InStoreView : IDisposable
{
    [Parameter]
    public Guid Id { get; set; }

    [Inject]
    IShoppingListService ShoppingListService { get; set; } = default!;

    [Inject]
    NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    DialogService DialogService { get; set; } = default!;

    [Inject]
    NotificationService NotificationService { get; set; } = default!;

    ShoppingListDetailDto? _detail;
    ApiProblemDetails? _error;
    bool _isLoading;
    bool _isGrouping;
    bool _isGrouped;
    bool _showChecked;
    List<InStoreGroupViewModel>? _groups;

    CancellationTokenSource _cts = new();

    int CheckedCount => _detail?.Items.Count(i => i.IsChecked) ?? 0;
    int RemainingCount => (_detail?.Items.Count ?? 0) - CheckedCount;

    decimal PredictedCost => _detail?.Items
        .Where(i => !i.IsChecked && i.AveragePrice.HasValue)
        .Sum(i => i.AveragePrice!.Value * i.Quantity) ?? 0m;

    double ProgressPercentage => _detail is not null && _detail.Items.Count > 0
        ? (double)CheckedCount / _detail.Items.Count * 100
        : 0;

    List<ShoppingItemDto> UncheckedItems => _detail?.Items
        .Where(i => !i.IsChecked)
        .OrderBy(i => i.SortOrder)
        .ThenBy(i => i.Name)
        .ToList() ?? [];

    List<ShoppingItemDto> RecentlyCheckedItems => _detail?.Items
        .Where(i => i.IsChecked)
        .OrderByDescending(i => i.CheckedAt)
        .ToList() ?? [];

    protected override async Task OnInitializedAsync()
    {
        await LoadDetailAsync();
    }

    async Task LoadDetailAsync()
    {
        _isLoading = true;
        _error = null;

        var result = await ShoppingListService.GetShoppingListByIdAsync(Id, _cts.Token);

        if (result.IsSuccess)
        {
            _detail = result.Value;

            // If we had groups, rebuild them with the updated items
            if (_isGrouped && _groups is not null)
            {
                RebuildGroupsFromCache();
            }
        }
        else
        {
            _error = result.Problem;
        }

        _isLoading = false;
    }

    async Task ToggleGroupingAsync()
    {
        if (_isGrouped)
        {
            _isGrouped = false;
            _groups = null;
            return;
        }

        await LoadGroupsAsync();
    }

    async Task LoadGroupsAsync()
    {
        if (_detail is null) return;

        _isGrouping = true;
        StateHasChanged();

        var result = await ShoppingListService.GroupItemsAsync(_detail.Id, _cts.Token);

        if (result.IsSuccess)
        {
            MapGroupsToViewModels(result.Value);
            _isGrouped = true;
        }
        else
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Grouping Failed",
                Detail = result.Problem.ToUserMessage(),
                Duration = 6000
            });
        }

        _isGrouping = false;
        StateHasChanged();
    }

    void MapGroupsToViewModels(ShoppingItemGroupResultDto groupResult)
    {
        if (_detail is null) return;

        var uncheckedLookup = _detail.Items
            .Where(i => !i.IsChecked)
            .ToDictionary(i => i.Name, i => i, StringComparer.OrdinalIgnoreCase);

        _groups = groupResult.Groups
            .Select(g => new InStoreGroupViewModel
            {
                Category = g.Category,
                Icon = g.Icon,
                SortOrder = g.SortOrder,
                Items = g.ItemNames
                    .Where(n => uncheckedLookup.ContainsKey(n))
                    .Select(n => uncheckedLookup[n])
                    .ToList()
            })
            .Where(g => g.Items.Count > 0)
            .OrderBy(g => g.SortOrder)
            .ToList();
    }

    void RebuildGroupsFromCache()
    {
        if (_groups is null || _detail is null) return;

        var uncheckedLookup = _detail.Items
            .Where(i => !i.IsChecked)
            .ToDictionary(i => i.Name, i => i, StringComparer.OrdinalIgnoreCase);

        foreach (var group in _groups)
        {
            var updated = group.Items
                .Select(old => uncheckedLookup.GetValueOrDefault(old.Name))
                .Where(i => i is not null)
                .Cast<ShoppingItemDto>()
                .ToList();

            group.Items.Clear();
            group.Items.AddRange(updated);
        }

        _groups = _groups.Where(g => g.Items.Count > 0).ToList();

        if (_groups.Count == 0)
        {
            _isGrouped = false;
            _groups = null;
        }
    }

    async Task ToggleItemAsync(ShoppingItemDto item)
    {
        if (_detail is null) return;

        var result = await ShoppingListService.ToggleItemAsync(_detail.Id, item.Id, _cts.Token);

        if (result.IsSuccess)
        {
            await LoadDetailAsync();
        }
        else
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Error",
                Detail = result.Problem.ToUserMessage(),
                Duration = 5000
            });
        }
    }

    async Task OpenReceiptScanAsync()
    {
        if (_detail is null) return;

        var result = await DialogService.OpenAsync<ShoppingListReceiptScanDialog>(
            "Process Receipt",
            new Dictionary<string, object>
            {
                { nameof(ShoppingListReceiptScanDialog.ShoppingListId), _detail.Id }
            },
            new DialogOptions
            {
                Width = "700px",
                Resizable = true,
                Draggable = true,
                CloseDialogOnOverlayClick = false,
                ShowClose = true
            });

        if (result is true)
        {
            await LoadDetailAsync();
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}
