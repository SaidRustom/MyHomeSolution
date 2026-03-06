using Microsoft.AspNetCore.Components;

namespace BlazorUI.Components.Bills;

public partial class BillSplitEditor
{
    [Parameter, EditorRequired]
    public List<BillSplitFormModel> Splits { get; set; } = [];

    [Parameter]
    public decimal BillAmount { get; set; }

    [Parameter]
    public EventCallback OnChanged { get; set; }

    decimal AllocatedPercentage => Splits.Sum(s => s.Percentage ?? 0m);

    decimal RemainingPercentage => 100m - AllocatedPercentage;

    bool IsOverAllocated => AllocatedPercentage > 100m;

    bool IsFullyAllocated => Math.Abs(AllocatedPercentage - 100m) < 0.01m;

    void AddSplit()
    {
        Splits.Add(new BillSplitFormModel());
        OnChanged.InvokeAsync();
    }

    void RemoveSplit(BillSplitFormModel split)
    {
        Splits.Remove(split);
        OnChanged.InvokeAsync();
    }

    void SplitEvenly()
    {
        if (Splits.Count == 0) return;

        var evenPercentage = Math.Round(100m / Splits.Count, 2);
        foreach (var split in Splits)
        {
            split.Percentage = evenPercentage;
        }

        // Handle rounding remainder
        var diff = 100m - Splits.Sum(s => s.Percentage ?? 0m);
        if (Splits.Count > 0 && diff != 0)
        {
            Splits[0].Percentage += diff;
        }

        OnChanged.InvokeAsync();
    }

    decimal GetEstimatedAmount(BillSplitFormModel split)
    {
        if (!split.Percentage.HasValue || BillAmount <= 0) return 0;
        return Math.Round(BillAmount * split.Percentage.Value / 100m, 2);
    }
}
