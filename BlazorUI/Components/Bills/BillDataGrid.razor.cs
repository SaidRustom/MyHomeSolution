using BlazorUI.Models.Bills;
using BlazorUI.Models.Common;
using BlazorUI.Models.Enums;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace BlazorUI.Components.Bills;

public partial class BillDataGrid
{
    [Parameter, EditorRequired]
    public PaginatedList<BillBriefDto> Data { get; set; } = new();

    [Parameter]
    public bool IsLoading { get; set; }

    [Parameter]
    public int PageSize { get; set; } = 20;

    [Parameter]
    public EventCallback<LoadDataArgs> OnLoadData { get; set; }

    [Parameter]
    public EventCallback<BillBriefDto> OnRowSelect { get; set; }

    [Parameter]
    public EventCallback<BillBriefDto> OnEdit { get; set; }

    [Parameter]
    public EventCallback<BillBriefDto> OnDelete { get; set; }

    int Count => Data.TotalCount;

    IEnumerable<BillBriefDto> Items => Data.Items;

    async Task RowSelectAsync(BillBriefDto bill) => await OnRowSelect.InvokeAsync(bill);

    string GetSplitStatusSummary(BillBriefDto bill) =>
        bill.SplitCount > 0 ? $"{bill.SplitCount} split(s)" : "No splits";

    string GetCategoryDisplay(BillCategory category) => category.ToString();
}
