using BlazorUI.Components.ShoppingLists;
using BlazorUI.Models.Bills;
using BlazorUI.Models.Common;
using BlazorUI.Models.ShoppingLists;
using BlazorUI.Services.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Radzen;

namespace BlazorUI.Pages.ShoppingLists;

public enum ShoppingItemViewFilter { All, Unchecked, Checked }

public sealed record UserSpendingSummary
{
    public required string UserFullName { get; init; }
    public decimal TotalAmount { get; init; }
    public int TripCount { get; init; }
}

public partial class ShoppingListDetail : IDisposable
{
    [Parameter]
    public Guid Id { get; set; }

    [Inject]
    IShoppingListService ShoppingListService { get; set; } = default!;

    [Inject]
    IBillService BillService { get; set; } = default!;

    [Inject]
    NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    DialogService DialogService { get; set; } = default!;

    [Inject]
    NotificationService NotificationService { get; set; } = default!;

    ShoppingListDetailDto? Detail { get; set; }

    bool IsLoading { get; set; }

    bool IsAddingItem { get; set; }

    bool _isTogglingAll;

    ApiProblemDetails? Error { get; set; }

    // Add-item form state
    string? _newItemName;
    int _newItemQuantity = 1;
    string? _newItemUnit;
    string? _newItemNotes;

    // Item filter state
    ShoppingItemViewFilter _itemViewFilter = ShoppingItemViewFilter.All;
    string? _itemSearchTerm;

    // Tab state
    int _selectedTabIndex;

    // Purchase history state
    bool _isLoadingHistory;
    List<BillBriefDto> _purchaseHistory = [];
    DateTimeOffset? _historyFromDate;
    DateTimeOffset? _historyToDate;
    string? _historySearchTerm;

    // Financial summary state
    bool _isLoadingFinancials;
    decimal _totalSpent;
    decimal _averagePerTrip;
    decimal _totalTax;
    List<UserSpendingSummary> _spendingByUser = [];

    int CheckedCount => Detail?.Items.Count(i => i.IsChecked) ?? 0;

    int RemainingCount => (Detail?.Items.Count ?? 0) - CheckedCount;

    decimal PredictedCost => Detail?.Items
        .Where(i => !i.IsChecked && i.AveragePrice.HasValue)
        .Sum(i => i.AveragePrice!.Value * i.Quantity) ?? 0m;

    double ProgressPercentage => Detail is not null && Detail.Items.Count > 0
        ? (double)CheckedCount / Detail.Items.Count * 100
        : 0;

    bool IsPastDue => Detail?.DueDate.HasValue == true
        && !Detail.IsCompleted
        && Detail.DueDate.Value < DateOnly.FromDateTime(DateTime.Today);

    List<ShoppingItemDto> FilteredItems
    {
        get
        {
            if (Detail is null) return [];

            var items = _itemViewFilter switch
            {
                ShoppingItemViewFilter.Unchecked => Detail.Items.Where(i => !i.IsChecked),
                ShoppingItemViewFilter.Checked => Detail.Items.Where(i => i.IsChecked),
                _ => Detail.Items.AsEnumerable()
            };

            if (!string.IsNullOrWhiteSpace(_itemSearchTerm))
            {
                items = items.Where(i =>
                    i.Name.Contains(_itemSearchTerm, StringComparison.OrdinalIgnoreCase)
                    || (i.Notes?.Contains(_itemSearchTerm, StringComparison.OrdinalIgnoreCase) == true));
            }

            return items.OrderBy(i => i.IsChecked).ThenBy(i => i.SortOrder).ThenBy(i => i.Name).ToList();
        }
    }

    CancellationTokenSource _cts = new();

    protected override async Task OnInitializedAsync()
    {
        await LoadDetailAsync();
        // Fire-and-forget loading of purchase history and financials
        _ = LoadPurchaseHistoryAsync();
    }

    async Task LoadDetailAsync()
    {
        IsLoading = true;
        Error = null;

        var result = await ShoppingListService.GetShoppingListByIdAsync(Id, _cts.Token);

        if (result.IsSuccess)
        {
            Detail = result.Value;
        }
        else
        {
            Error = result.Problem;
        }

        IsLoading = false;
    }

    async Task LoadPurchaseHistoryAsync()
    {
        _isLoadingHistory = true;
        _isLoadingFinancials = true;
        StateHasChanged();

        var result = await BillService.GetBillsAsync(
            pageNumber: 1,
            pageSize: 100,
            shoppingListId: Id,
            fromDate: _historyFromDate,
            toDate: _historyToDate,
            searchTerm: _historySearchTerm,
            sortBy: "billDate",
            sortDirection: "desc",
            cancellationToken: _cts.Token);

        if (result.IsSuccess)
        {
            _purchaseHistory = result.Value.Items.ToList();
            CalculateFinancials();
        }
        else
        {
            _purchaseHistory = [];
        }

        _isLoadingHistory = false;
        _isLoadingFinancials = false;
        StateHasChanged();
    }

    void CalculateFinancials()
    {
        _totalSpent = _purchaseHistory.Sum(b => b.Amount);
        _averagePerTrip = _purchaseHistory.Count > 0
            ? _totalSpent / _purchaseHistory.Count
            : 0;

        // Estimate tax (13% HST on total - this is a rough estimate from bill totals)
        // Actual per-item tax is tracked in bill items
        _totalTax = _purchaseHistory.Sum(b =>
        {
            // Approximate: if the bill total includes tax, estimate it
            // The real per-item tax is in BillItem.TaxAmount but we don't have it here
            return 0m;
        });

        _spendingByUser = _purchaseHistory
            .GroupBy(b => b.PaidByUserFullName ?? b.PaidByUserId)
            .Select(g => new UserSpendingSummary
            {
                UserFullName = g.Key,
                TotalAmount = g.Sum(b => b.Amount),
                TripCount = g.Count()
            })
            .OrderByDescending(s => s.TotalAmount)
            .ToList();
    }

    void ClearHistoryFilters()
    {
        _historyFromDate = null;
        _historyToDate = null;
        _historySearchTerm = null;
        _ = LoadPurchaseHistoryAsync();
    }

    async Task AddItemAsync()
    {
        if (string.IsNullOrWhiteSpace(_newItemName) || Detail is null)
            return;

        IsAddingItem = true;

        var request = new AddShoppingItemRequest
        {
            ShoppingListId = Detail.Id,
            Name = _newItemName.Trim(),
            Quantity = _newItemQuantity,
            Unit = string.IsNullOrWhiteSpace(_newItemUnit) ? null : _newItemUnit.Trim(),
            Notes = string.IsNullOrWhiteSpace(_newItemNotes) ? null : _newItemNotes.Trim()
        };

        var result = await ShoppingListService.AddItemAsync(Detail.Id, request, _cts.Token);

        if (result.IsSuccess)
        {
            _newItemName = null;
            _newItemQuantity = 1;
            _newItemUnit = null;
            _newItemNotes = null;
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

        IsAddingItem = false;
    }

    async Task HandleAddItemKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !string.IsNullOrWhiteSpace(_newItemName))
        {
            await AddItemAsync();
        }
    }

    async Task ToggleItemAsync(ShoppingItemDto item)
    {
        if (Detail is null) return;

        var result = await ShoppingListService.ToggleItemAsync(Detail.Id, item.Id, _cts.Token);

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

    async Task ToggleAllAsync()
    {
        if (Detail is null) return;

        _isTogglingAll = true;
        var result = await ShoppingListService.ToggleAllItemsAsync(Detail.Id, true, _cts.Token);
        _isTogglingAll = false;

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

    async Task UntoggleAllAsync()
    {
        if (Detail is null) return;

        _isTogglingAll = true;
        var result = await ShoppingListService.ToggleAllItemsAsync(Detail.Id, false, _cts.Token);
        _isTogglingAll = false;

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

    async Task EditItemAsync(ShoppingItemDto item)
    {
        if (Detail is null) return;

        var newName = await DialogService.OpenAsync<ShoppingItemEditDialog>(
            "Edit Item",
            new Dictionary<string, object>
            {
                { nameof(ShoppingItemEditDialog.Item), item },
                { nameof(ShoppingItemEditDialog.ListId), Detail.Id }
            },
            new DialogOptions
            {
                Width = "450px",
                CloseDialogOnOverlayClick = false,
                ShowClose = false
            });

        if (newName is true)
        {
            await LoadDetailAsync();
        }
    }

    async Task RemoveItemAsync(ShoppingItemDto item)
    {
        if (Detail is null) return;

        var confirmed = await DialogService.Confirm(
            $"Remove \"{item.Name}\" from the list?",
            "Remove Item",
            new ConfirmOptions
            {
                OkButtonText = "Remove",
                CancelButtonText = "Cancel"
            });

        if (confirmed == true)
        {
            var result = await ShoppingListService.RemoveItemAsync(Detail.Id, item.Id, _cts.Token);

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
    }

    async Task EditListAsync()
    {
        if (Detail is null) return;

        var model = new ShoppingListFormModel
        {
            Id = Detail.Id,
            Title = Detail.Title,
            Description = Detail.Description,
            Category = Detail.Category,
            DueDate = Detail.DueDate,
            DefaultBudgetId = Detail.DefaultBudgetId
        };

        var result = await DialogService.OpenAsync<ShoppingListFormDialog>(
            "Edit Shopping List",
            new Dictionary<string, object>
            {
                { nameof(ShoppingListFormDialog.Model), model },
                { nameof(ShoppingListFormDialog.IsEdit), true }
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
            await LoadDetailAsync();
        }
    }

    async Task DeleteListAsync()
    {
        if (Detail is null) return;

        var confirmed = await DialogService.Confirm(
            $"Are you sure you want to delete \"{Detail.Title}\"? This action cannot be undone.",
            "Delete Shopping List",
            new ConfirmOptions
            {
                OkButtonText = "Delete",
                CancelButtonText = "Cancel"
            });

        if (confirmed == true)
        {
            var result = await ShoppingListService.DeleteShoppingListAsync(Detail.Id, _cts.Token);

            if (result.IsSuccess)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = "List Deleted",
                    Detail = $"'{Detail.Title}' has been deleted.",
                    Duration = 4000
                });
                NavigationManager.NavigateTo("/shopping-lists");
            }
            else
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Error",
                    Detail = result.Problem.ToUserMessage(),
                    Duration = 6000
                });
            }
        }
    }

    async Task OpenReceiptScanAsync()
    {
        if (Detail is null) return;

        var result = await DialogService.OpenAsync<ShoppingListReceiptScanDialog>(
            "Process Receipt",
            new Dictionary<string, object>
            {
                { nameof(ShoppingListReceiptScanDialog.ShoppingListId), Detail.Id }
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
            _ = LoadPurchaseHistoryAsync();
        }
    }

    async Task ShowItemHistoryAsync(ShoppingItemDto item)
    {
        if (Detail is null) return;

        await DialogService.OpenAsync<ShoppingItemHistoryDialog>(
            $"Purchase History: {item.Name}",
            new Dictionary<string, object>
            {
                { nameof(ShoppingItemHistoryDialog.ItemName), item.Name },
                { nameof(ShoppingItemHistoryDialog.ShoppingListId), Detail.Id }
            },
            new DialogOptions
            {
                Width = "600px",
                Resizable = true,
                CloseDialogOnOverlayClick = true,
                ShowClose = true
            });
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}
