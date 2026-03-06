using BlazorUI.Models.Bills;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace BlazorUI.Components.Bills;

public partial class BillDeleteConfirm
{
    [Inject]
    DialogService DialogService { get; set; } = default!;

    [Parameter]
    public string BillTitle { get; set; } = string.Empty;

    [Parameter]
    public decimal Amount { get; set; }

    [Parameter]
    public string Currency { get; set; } = "$";

    void Cancel() => DialogService.Close(false);

    void Confirm() => DialogService.Close(true);
}
