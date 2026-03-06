using BlazorUI.Models.Bills;
using Microsoft.AspNetCore.Components;

namespace BlazorUI.Components.Bills;

public partial class BalanceCard
{
    [Parameter, EditorRequired]
    public UserBalanceDto Balance { get; set; } = default!;

    [Parameter]
    public EventCallback<UserBalanceDto> OnViewDetails { get; set; }

    bool IsPositive => Balance.NetBalance > 0;

    bool IsNegative => Balance.NetBalance < 0;

    bool IsSettled => Balance.NetBalance == 0;

    string StatusText => IsPositive ? "owes you" : IsNegative ? "you owe" : "settled";

    string StatusIcon => IsPositive ? "arrow_downward" : IsNegative ? "arrow_upward" : "check_circle";

    string StatusColor => IsPositive ? "var(--rz-success)" : IsNegative ? "var(--rz-danger)" : "var(--rz-secondary)";

    async Task ViewDetailsAsync() => await OnViewDetails.InvokeAsync(Balance);
}
