using BlazorUI.Models.Bills;
using Microsoft.AspNetCore.Components;

namespace BlazorUI.Components.Bills;

public partial class CategoryBreakdownChart
{
    [Parameter, EditorRequired]
    public IReadOnlyList<CategorySpendingDto> Data { get; set; } = [];

    [Parameter]
    public string Title { get; set; } = "Spending by Category";

    bool HasData => Data.Count > 0;

    IEnumerable<CategoryChartItem> ChartItems => Data.Select(d => new CategoryChartItem
    {
        Category = d.Category.ToString(),
        Amount = d.TotalAmount,
        Count = d.BillCount
    });
}

public sealed record CategoryChartItem
{
    public required string Category { get; init; }
    public decimal Amount { get; init; }
    public int Count { get; init; }
}
