using BlazorUI.Models.Bills;
using BlazorUI.Services.Contracts;
using Microsoft.AspNetCore.Components;

namespace BlazorUI.Components.Bills;

public partial class BillDetailPanel
{
    [Inject]
    private IBillService BillService { get; set; } = default!;

    [Parameter, EditorRequired]
    public BillDetailDto Bill { get; set; } = default!;

    [Parameter]
    public EventCallback OnEdit { get; set; }

    [Parameter]
    public EventCallback OnDelete { get; set; }

    [Parameter]
    public EventCallback<BillSplitDto> OnMarkSplitAsPaid { get; set; }

    [Parameter]
    public bool IsProcessing { get; set; }

    [Parameter]
    public EventCallback OnBack { get; set; }

    private string? ReceiptDataUrl { get; set; }
    private bool IsLoadingReceipt { get; set; }
    private bool ShowReceiptPreview { get; set; }

    string CategoryName => Bill.Category.ToString();

    bool HasReceipt => !string.IsNullOrEmpty(Bill.ReceiptUrl);

    bool HasNotes => !string.IsNullOrEmpty(Bill.Notes);

    bool HasDescription => !string.IsNullOrEmpty(Bill.Description);

    bool HasRelatedEntity => Bill.RelatedEntityId.HasValue;

    bool HasSplits => Bill.Splits.Count > 0;

    bool HasItems => Bill.Items.Count > 0;

    decimal PaidAmount => Bill.Splits
        .Where(s => s.Status != Models.Enums.SplitStatus.Unpaid)
        .Sum(s => s.Amount);

    decimal UnpaidAmount => Bill.Amount - PaidAmount;

    double PaidPercentage => Bill.Amount > 0
        ? (double)(PaidAmount / Bill.Amount * 100)
        : 0;

    async Task EditAsync() => await OnEdit.InvokeAsync();

    async Task DeleteAsync() => await OnDelete.InvokeAsync();

    async Task GoBackAsync() => await OnBack.InvokeAsync();

    async Task MarkSplitAsPaidAsync(BillSplitDto split) =>
        await OnMarkSplitAsPaid.InvokeAsync(split);

    async Task ViewReceiptAsync()
    {
        if (ReceiptDataUrl is not null)
        {
            ShowReceiptPreview = !ShowReceiptPreview;
            return;
        }

        IsLoadingReceipt = true;
        StateHasChanged();

        try
        {
            ReceiptDataUrl = await BillService.GetReceiptDataUrlAsync(Bill.Id);
            ShowReceiptPreview = ReceiptDataUrl is not null;
        }
        finally
        {
            IsLoadingReceipt = false;
            StateHasChanged();
        }
    }
}
