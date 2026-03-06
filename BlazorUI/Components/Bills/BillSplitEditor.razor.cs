using BlazorUI.Components.Bills;
using Microsoft.AspNetCore.Components;

namespace BlazorUI.Components.Bills;

public partial class BillSplitEditor
{
    [Parameter, EditorRequired]
    public List<BillSplitFormModel> Splits { get; set; } = [];

    [Parameter]
    public decimal BillAmount { get; set; }

    [Parameter]
    public string? CurrentUserId { get; set; }

    [Parameter]
    public EventCallback OnChanged { get; set; }

    decimal AllocatedPercentage => Splits.Sum(s => s.Percentage ?? 0m);

    decimal RemainingPercentage => 100m - AllocatedPercentage;

    bool IsOverAllocated => AllocatedPercentage > 100m;

    bool IsFullyAllocated => Math.Abs(AllocatedPercentage - 100m) < 0.01m;

    /// <summary>
    /// Returns the user IDs that are already assigned to a split,
    /// so the FriendPicker can exclude them.
    /// </summary>
    IEnumerable<string> SelectedUserIds =>
        Splits.Where(s => !string.IsNullOrEmpty(s.UserId)).Select(s => s.UserId);

    void AddSplit()
    {
        var wasEqual = IsSplitEqual();

        if (Splits.Count == 0 && !string.IsNullOrEmpty(CurrentUserId))
        {
            // First split: auto-add current user + empty friend, split 50/50
            Splits.Add(new BillSplitFormModel { UserId = CurrentUserId, Percentage = 50m });
            Splits.Add(new BillSplitFormModel { Percentage = 50m });
        }
        else
        {
            Splits.Add(new BillSplitFormModel());

            if (wasEqual)
            {
                DistributeEvenly();
            }
        }

        OnChanged.InvokeAsync();
    }

    void RemoveSplit(BillSplitFormModel split)
    {
        var wasEqual = IsSplitEqual();

        Splits.Remove(split);

        if (wasEqual && Splits.Count > 0)
        {
            DistributeEvenly();
        }

        OnChanged.InvokeAsync();
    }

    void SplitEvenly()
    {
        if (Splits.Count == 0) return;

        DistributeEvenly();
        OnChanged.InvokeAsync();
    }

    /// <summary>
    /// Checks whether all splits that have a percentage are evenly distributed.
    /// </summary>
    bool IsSplitEqual()
    {
        var withPercentage = Splits.Where(s => s.Percentage.HasValue).ToList();
        if (withPercentage.Count < 2) return true;

        var expected = Math.Round(100m / withPercentage.Count, 2);
        return withPercentage.All(s => Math.Abs(s.Percentage!.Value - expected) < 0.02m);
    }

    void DistributeEvenly()
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
    }

    decimal GetEstimatedAmount(BillSplitFormModel split)
    {
        if (!split.Percentage.HasValue || BillAmount <= 0) return 0;
        return Math.Round(BillAmount * split.Percentage.Value / 100m, 2);
    }
}
