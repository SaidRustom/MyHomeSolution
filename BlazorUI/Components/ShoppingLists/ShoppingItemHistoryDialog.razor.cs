using BlazorUI.Services.Contracts;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace BlazorUI.Components.ShoppingLists;

public sealed record ItemPurchaseRecord
{
    public Guid BillId { get; init; }
    public required string StoreName { get; init; }
    public DateTimeOffset PurchaseDate { get; init; }
    public decimal Price { get; init; }
    public int Quantity { get; init; }
    public required string PurchasedByFullName { get; init; }
}

public partial class ShoppingItemHistoryDialog
{
    [Inject]
    private IBillService BillService { get; set; } = default!;

    [Inject]
    private DialogService DialogService { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Parameter, EditorRequired]
    public required string ItemName { get; set; }

    [Parameter, EditorRequired]
    public Guid ShoppingListId { get; set; }

    private bool _isLoading;
    private List<ItemPurchaseRecord> _history = [];

    protected override async Task OnInitializedAsync()
    {
        await LoadHistoryAsync();
    }

    private async Task LoadHistoryAsync()
    {
        _isLoading = true;

        // Load all bills linked to this shopping list
        var result = await BillService.GetBillsAsync(
            pageNumber: 1,
            pageSize: 100,
            shoppingListId: ShoppingListId);

        if (result.IsSuccess)
        {
            _history = [];

            foreach (var bill in result.Value.Items)
            {
                // Load full bill detail to access items
                var detailResult = await BillService.GetBillByIdAsync(bill.Id);

                if (detailResult.IsSuccess)
                {
                    var matchingItems = detailResult.Value.Items
                        .Where(i => i.Name.Contains(ItemName, StringComparison.OrdinalIgnoreCase)
                                 || ItemName.Contains(i.Name, StringComparison.OrdinalIgnoreCase));

                    foreach (var item in matchingItems)
                    {
                        _history.Add(new ItemPurchaseRecord
                        {
                            BillId = bill.Id,
                            StoreName = bill.Title,
                            PurchaseDate = bill.BillDate,
                            Price = item.Price,
                            Quantity = item.Quantity,
                            PurchasedByFullName = detailResult.Value.PaidByUserFullName ?? "Unknown"
                        });
                    }
                }
            }

            _history = _history.OrderByDescending(h => h.PurchaseDate).ToList();
        }

        _isLoading = false;
    }

    private void ViewBill(Guid billId)
    {
        DialogService.Close(null);
        NavigationManager.NavigateTo($"/bills/{billId}");
    }
}
