using BlazorUI.Models.Bills;
using Microsoft.AspNetCore.Components;

namespace BlazorUI.Components.Bills;

public partial class UserSpendingChart
{
    [Parameter, EditorRequired]
    public IReadOnlyList<UserSpendingDto> Data { get; set; } = [];

    [Parameter]
    public string Title { get; set; } = "Spending by User";

    bool HasData => Data.Count > 0;

    IEnumerable<UserSpendingChartItem> ChartItems => Data.Select(d => new UserSpendingChartItem
    {
        UserId = d.UserId,
        DisplayName = d.UserFullName ?? d.UserId,
        TotalPaid = d.TotalPaid,
        TotalOwed = d.TotalOwed,
        TotalOwing = d.TotalOwing,
        NetBalance = d.NetBalance
    });
}

public sealed record UserSpendingChartItem
{
    public required string UserId { get; init; }
    public required string DisplayName { get; init; }
    public decimal TotalPaid { get; init; }
    public decimal TotalOwed { get; init; }
    public decimal TotalOwing { get; init; }
    public decimal NetBalance { get; init; }
}
