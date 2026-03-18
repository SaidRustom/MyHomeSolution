using BlazorUI.Models.Bills;
using BlazorUI.Models.Enums;
using BlazorUI.Models.ShoppingLists;
using Microsoft.AspNetCore.Components;

namespace BlazorUI.Components.Bills;

public partial class BillFilterBar
{
    [Parameter]
    public string? SearchTerm { get; set; }

    [Parameter]
    public EventCallback<string?> SearchTermChanged { get; set; }

    [Parameter]
    public BillCategory? SelectedCategory { get; set; }

    [Parameter]
    public EventCallback<BillCategory?> SelectedCategoryChanged { get; set; }

    [Parameter]
    public DateTimeOffset? FromDate { get; set; }

    [Parameter]
    public EventCallback<DateTimeOffset?> FromDateChanged { get; set; }

    [Parameter]
    public DateTimeOffset? ToDate { get; set; }

    [Parameter]
    public EventCallback<DateTimeOffset?> ToDateChanged { get; set; }

    [Parameter]
    public string? SplitWithUserId { get; set; }

    [Parameter]
    public EventCallback<string?> SplitWithUserIdChanged { get; set; }

    [Parameter]
    public string? PaidByUserId { get; set; }

    [Parameter]
    public EventCallback<string?> PaidByUserIdChanged { get; set; }

    [Parameter]
    public bool? HasLinkedTask { get; set; }

    [Parameter]
    public EventCallback<bool?> HasLinkedTaskChanged { get; set; }

    [Parameter]
    public Guid? ShoppingListId { get; set; }

    [Parameter]
    public EventCallback<Guid?> ShoppingListIdChanged { get; set; }

    [Parameter]
    public IReadOnlyList<ShoppingListBriefDto> ShoppingLists { get; set; } = [];

    [Parameter]
    public EventCallback OnSearch { get; set; }

    [Parameter]
    public EventCallback OnClear { get; set; }

    IEnumerable<BillCategory> Categories => Enum.GetValues<BillCategory>();

    bool _expandAdvanced;

    bool HasActiveFilters =>
        !string.IsNullOrEmpty(SearchTerm) ||
        SelectedCategory.HasValue ||
        FromDate.HasValue ||
        ToDate.HasValue ||
        !string.IsNullOrEmpty(SplitWithUserId) ||
        !string.IsNullOrEmpty(PaidByUserId) ||
        HasLinkedTask.HasValue ||
        ShoppingListId.HasValue;

    bool HasAdvancedFilters =>
        !string.IsNullOrEmpty(SplitWithUserId) ||
        !string.IsNullOrEmpty(PaidByUserId) ||
        HasLinkedTask.HasValue ||
        ShoppingListId.HasValue;

    async Task OnSearchTermChanged(string? value)
    {
        SearchTerm = value;
        await SearchTermChanged.InvokeAsync(value);
    }

    async Task OnCategoryChanged(object? value)
    {
        var category = value is BillCategory bc ? bc : (BillCategory?)null;
        SelectedCategory = category;
        await SelectedCategoryChanged.InvokeAsync(category);
    }

    async Task OnFromDateChanged(DateTimeOffset? value)
    {
        FromDate = value;
        await FromDateChanged.InvokeAsync(value);
    }

    async Task OnToDateChanged(DateTimeOffset? value)
    {
        ToDate = value;
        await ToDateChanged.InvokeAsync(value);
    }

    async Task OnSplitWithUserChanged(string? value)
    {
        SplitWithUserId = value;
        await SplitWithUserIdChanged.InvokeAsync(value);
    }

    async Task OnPaidByUserChanged(string? value)
    {
        PaidByUserId = value;
        await PaidByUserIdChanged.InvokeAsync(value);
    }

    async Task OnHasLinkedTaskChanged(object? value)
    {
        var linked = value is bool b ? b : (bool?)null;
        HasLinkedTask = linked;
        await HasLinkedTaskChanged.InvokeAsync(linked);
    }

    async Task OnShoppingListChanged(object? value)
    {
        var id = value is Guid g ? g : (Guid?)null;
        ShoppingListId = id;
        await ShoppingListIdChanged.InvokeAsync(id);
    }

    async Task ApplyFiltersAsync()
    {
        await OnSearch.InvokeAsync();
    }

    async Task ClearFiltersAsync()
    {
        SearchTerm = null;
        SelectedCategory = null;
        FromDate = null;
        ToDate = null;
        SplitWithUserId = null;
        PaidByUserId = null;
        HasLinkedTask = null;
        ShoppingListId = null;

        await SearchTermChanged.InvokeAsync(null);
        await SelectedCategoryChanged.InvokeAsync(null);
        await FromDateChanged.InvokeAsync(null);
        await ToDateChanged.InvokeAsync(null);
        await SplitWithUserIdChanged.InvokeAsync(null);
        await PaidByUserIdChanged.InvokeAsync(null);
        await HasLinkedTaskChanged.InvokeAsync(null);
        await ShoppingListIdChanged.InvokeAsync(null);
        await OnClear.InvokeAsync();
    }
}
