using BlazorUI.Components.Common;
using BlazorUI.Models.Bills;
using BlazorUI.Models.Enums;
using Microsoft.AspNetCore.Components;

namespace BlazorUI.Components.Bills;

public partial class BillSplitList
{
    [Parameter, EditorRequired]
    public IReadOnlyList<BillSplitDto> Splits { get; set; } = [];

    [Parameter]
    public bool AllowMarkAsPaid { get; set; }

    [Parameter]
    public EventCallback<BillSplitDto> OnMarkAsPaid { get; set; }

    [Parameter]
    public bool IsProcessing { get; set; }

    Guid? _processingSplitId;

    string GetStatusText(SplitStatus status) => status switch
    {
        SplitStatus.Unpaid => "Unpaid",
        SplitStatus.Paid => "Paid",
        SplitStatus.Settled => "Settled",
        _ => status.ToString()
    };

    StatusSeverity GetStatusSeverity(SplitStatus status) => status switch
    {
        SplitStatus.Unpaid => StatusSeverity.Warning,
        SplitStatus.Paid => StatusSeverity.Success,
        SplitStatus.Settled => StatusSeverity.Info,
        _ => StatusSeverity.Secondary
    };

    string GetStatusIcon(SplitStatus status) => status switch
    {
        SplitStatus.Unpaid => "schedule",
        SplitStatus.Paid => "check_circle",
        SplitStatus.Settled => "verified",
        _ => "help"
    };

    bool CanMarkAsPaid(BillSplitDto split) =>
        AllowMarkAsPaid && split.Status == SplitStatus.Unpaid;

    async Task MarkAsPaidAsync(BillSplitDto split)
    {
        _processingSplitId = split.Id;
        await OnMarkAsPaid.InvokeAsync(split);
        _processingSplitId = null;
    }
}
