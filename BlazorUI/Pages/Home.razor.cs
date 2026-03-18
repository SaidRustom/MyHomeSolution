using BlazorUI.Models.ShoppingLists;
using BlazorUI.Services.Contracts;
using Microsoft.AspNetCore.Components;

namespace BlazorUI.Pages;

public partial class Home : IDisposable
{
    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    [Inject]
    private IShoppingListService ShoppingListService { get; set; } = default!;

    List<ShoppingListBriefDto> _activeLists = [];
    Guid? _selectedListId;
    bool _isLoadingLists;
    CancellationTokenSource _cts = new();

    protected override async Task OnInitializedAsync()
    {
        await LoadActiveListsAsync();
    }

    async Task LoadActiveListsAsync()
    {
        _isLoadingLists = true;

        var result = await ShoppingListService.GetShoppingListsAsync(
            pageNumber: 1,
            pageSize: 50,
            isCompleted: false,
            cancellationToken: _cts.Token);

        if (result.IsSuccess)
        {
            _activeLists = result.Value.Items
                .Where(l => l.TotalItems - l.CheckedItems > 0)
                .OrderByDescending(l => l.CreatedAt)
                .ToList();
        }

        _isLoadingLists = false;
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}
