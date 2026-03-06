using BlazorUI.Models.Bills;
using BlazorUI.Models.Enums;
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
    public EventCallback OnSearch { get; set; }

    [Parameter]
    public EventCallback OnClear { get; set; }

    IEnumerable<BillCategory> Categories => Enum.GetValues<BillCategory>();

    bool HasActiveFilters =>
        !string.IsNullOrEmpty(SearchTerm) ||
        SelectedCategory.HasValue ||
        FromDate.HasValue ||
        ToDate.HasValue;

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

        await SearchTermChanged.InvokeAsync(null);
        await SelectedCategoryChanged.InvokeAsync(null);
        await FromDateChanged.InvokeAsync(null);
        await ToDateChanged.InvokeAsync(null);
        await OnClear.InvokeAsync();
    }
}
