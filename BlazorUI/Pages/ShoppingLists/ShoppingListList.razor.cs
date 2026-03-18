using BlazorUI.Components.ShoppingLists;
using BlazorUI.Models.Common;
using BlazorUI.Models.Enums;
using BlazorUI.Models.ShoppingLists;
using BlazorUI.Services.Contracts;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace BlazorUI.Pages.ShoppingLists;

public partial class ShoppingListList : IDisposable
{
    [Inject]
    IShoppingListService ShoppingListService { get; set; } = default!;

    [Inject]
    NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    DialogService DialogService { get; set; } = default!;

    [Inject]
    NotificationService NotificationService { get; set; } = default!;

    PaginatedList<ShoppingListBriefDto> ListData { get; set; } = new();

    bool IsLoading { get; set; }

    ApiProblemDetails? Error { get; set; }

    // Filter state
    string? SearchTerm { get; set; }
    ShoppingListCategory? SelectedCategory { get; set; }
    ShoppingListStatusFilter StatusFilter { get; set; } = ShoppingListStatusFilter.All;

    IEnumerable<ShoppingListCategory> AllCategories => Enum.GetValues<ShoppingListCategory>();

    int _currentPage = 1;
    const int PageSize = 20;

    bool HasActiveFilters =>
        !string.IsNullOrEmpty(SearchTerm)
        || SelectedCategory.HasValue
        || StatusFilter != ShoppingListStatusFilter.All;

    CancellationTokenSource _cts = new();

    protected override async Task OnInitializedAsync()
    {
        await LoadListsAsync();
    }

    async Task LoadListsAsync()
    {
        IsLoading = true;
        Error = null;

        bool? isCompleted = StatusFilter switch
        {
            ShoppingListStatusFilter.Active => false,
            ShoppingListStatusFilter.Completed => true,
            _ => null
        };

        var result = await ShoppingListService.GetShoppingListsAsync(
            pageNumber: _currentPage,
            pageSize: PageSize,
            category: SelectedCategory,
            isCompleted: isCompleted,
            searchTerm: SearchTerm,
            cancellationToken: _cts.Token);

        if (result.IsSuccess)
        {
            ListData = result.Value;
        }
        else
        {
            Error = result.Problem;
        }

        IsLoading = false;
    }

    async Task OnLoadDataAsync(LoadDataArgs args)
    {
        _currentPage = (args.Skip ?? 0) / PageSize + 1;
        await LoadListsAsync();
    }

    async Task OnSearchAsync()
    {
        _currentPage = 1;
        await LoadListsAsync();
    }

    async Task OnClearFiltersAsync()
    {
        _currentPage = 1;
        SearchTerm = null;
        SelectedCategory = null;
        StatusFilter = ShoppingListStatusFilter.All;
        await LoadListsAsync();
    }

    async Task OnStatusFilterChanged()
    {
        _currentPage = 1;
        await LoadListsAsync();
    }

    void ViewList(ShoppingListBriefDto list)
    {
        NavigationManager.NavigateTo($"/shopping-lists/{list.Id}");
    }

    void GoToInStoreView(ShoppingListBriefDto list)
    {
        NavigationManager.NavigateTo($"/shopping-lists/{list.Id}/in-store");
    }

    async Task CreateListAsync()
    {
        var model = new ShoppingListFormModel();

        var result = await DialogService.OpenAsync<ShoppingListFormDialog>(
            "Create Shopping List",
            new Dictionary<string, object>
            {
                { nameof(ShoppingListFormDialog.Model), model },
                { nameof(ShoppingListFormDialog.IsEdit), false }
            },
            new DialogOptions
            {
                Width = "550px",
                Resizable = true,
                Draggable = true,
                CloseDialogOnOverlayClick = false,
                ShowClose = false
            });

        if (result is true)
        {
            await LoadListsAsync();
        }
    }

    async Task DeleteListAsync(ShoppingListBriefDto list)
    {
        var confirmed = await DialogService.Confirm(
            $"Are you sure you want to delete \"{list.Title}\"?",
            "Delete Shopping List",
            new ConfirmOptions
            {
                OkButtonText = "Delete",
                CancelButtonText = "Cancel"
            });

        if (confirmed == true)
        {
            var deleteResult = await ShoppingListService.DeleteShoppingListAsync(list.Id, _cts.Token);

            if (deleteResult.IsSuccess)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = "List Deleted",
                    Detail = $"'{list.Title}' has been deleted.",
                    Duration = 4000
                });
                await LoadListsAsync();
            }
            else
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Error",
                    Detail = deleteResult.Problem.ToUserMessage(),
                    Duration = 6000
                });
            }
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}

public enum ShoppingListStatusFilter
{
    All,
    Active,
    Completed
}
