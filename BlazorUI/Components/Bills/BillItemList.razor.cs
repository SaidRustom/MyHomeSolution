using BlazorUI.Models.Bills;
using Microsoft.AspNetCore.Components;

namespace BlazorUI.Components.Bills;

public partial class BillItemList
{
    [Parameter, EditorRequired]
    public IReadOnlyList<BillItemDto> Items { get; set; } = [];

    [Parameter]
    public bool GroupByShoppingList { get; set; }

    decimal TotalItemsValue => Items.Sum(i => i.Price);

    decimal TotalDiscount => Items.Sum(i => i.Discount);

    decimal TotalTax => Items.Sum(i => i.TaxAmount);

    bool HasItems => Items.Count > 0;
}
