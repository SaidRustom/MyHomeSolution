using BlazorUI.Models.Bills;
using Microsoft.AspNetCore.Components;

namespace BlazorUI.Components.Bills;

public partial class BillCard
{
    [Parameter, EditorRequired]
    public BillBriefDto Bill { get; set; } = default!;

    [Parameter]
    public EventCallback<BillBriefDto> OnView { get; set; }

    [Parameter]
    public EventCallback<BillBriefDto> OnEdit { get; set; }

    [Parameter]
    public EventCallback<BillBriefDto> OnDelete { get; set; }

    string CategoryName => Bill.Category.ToString();

    async Task ViewBillAsync() => await OnView.InvokeAsync(Bill);

    async Task EditBillAsync() => await OnEdit.InvokeAsync(Bill);

    async Task DeleteBillAsync() => await OnDelete.InvokeAsync(Bill);
}
