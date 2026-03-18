using BlazorUI.Models.Common;
using BlazorUI.Models.Enums;
using BlazorUI.Models.ShoppingLists;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace BlazorUI.Components.ShoppingLists;

public partial class ShoppingListDataGrid
{
    [Parameter, EditorRequired]
    public PaginatedList<ShoppingListBriefDto> Data { get; set; } = new();

    [Parameter]
    public bool IsLoading { get; set; }

    [Parameter]
    public int PageSize { get; set; } = 20;

    [Parameter]
    public EventCallback<LoadDataArgs> OnLoadData { get; set; }

    [Parameter]
    public EventCallback<ShoppingListBriefDto> OnRowSelect { get; set; }

    [Parameter]
    public EventCallback<ShoppingListBriefDto> OnDelete { get; set; }

    [Parameter]
    public EventCallback<ShoppingListBriefDto> OnInStoreView { get; set; }

    int Count => Data.TotalCount;

    IEnumerable<ShoppingListBriefDto> Items => Data.Items;

    async Task RowSelectAsync(ShoppingListBriefDto list) => await OnRowSelect.InvokeAsync(list);

    static double GetProgress(ShoppingListBriefDto list) =>
        list.TotalItems > 0 ? (double)list.CheckedItems / list.TotalItems * 100 : 0;

    static bool IsPastDue(ShoppingListBriefDto list) =>
        list.DueDate.HasValue && !list.IsCompleted && list.DueDate.Value < DateOnly.FromDateTime(DateTime.Today);

    static string GetCategoryIcon(ShoppingListCategory category) => category switch
    {
        ShoppingListCategory.Groceries => "local_grocery_store",
        ShoppingListCategory.Household => "house",
        ShoppingListCategory.Personal => "person",
        ShoppingListCategory.Health => "health_and_safety",
        ShoppingListCategory.Electronics => "devices",
        ShoppingListCategory.Clothing => "checkroom",
        ShoppingListCategory.Other => "more_horiz",
        _ => "shopping_cart"
    };
}
