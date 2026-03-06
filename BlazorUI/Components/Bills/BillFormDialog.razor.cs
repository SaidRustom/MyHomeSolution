using BlazorUI.Models.Bills;
using BlazorUI.Models.Enums;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace BlazorUI.Components.Bills;

public partial class BillFormDialog
{
    [Inject]
    DialogService DialogService { get; set; } = default!;

    [Parameter]
    public BillFormModel Model { get; set; } = new();

    [Parameter]
    public bool IsEdit { get; set; }

    [Parameter]
    public bool IsBusy { get; set; }

    string DialogTitle => IsEdit ? "Edit Bill" : "Create Bill";

    string SubmitText => IsEdit ? "Save Changes" : "Create Bill";

    IEnumerable<BillCategory> Categories => Enum.GetValues<BillCategory>();

    void OnSubmit() => DialogService.Close(Model);

    void Cancel() => DialogService.Close(null);

    void AddSplit()
    {
        Model.Splits.Add(new BillSplitFormModel());
    }

    void RemoveSplit(BillSplitFormModel split)
    {
        Model.Splits.Remove(split);
    }

    decimal RemainingPercentage
    {
        get
        {
            var allocated = Model.Splits.Sum(s => s.Percentage ?? 0m);
            return 100m - allocated;
        }
    }
}

public sealed class BillFormModel
{
    public Guid? Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "$";
    public BillCategory Category { get; set; }
    public DateTime BillDate { get; set; } = DateTime.Today;
    public string? Notes { get; set; }
    public Guid? RelatedEntityId { get; set; }
    public string? RelatedEntityType { get; set; }
    public List<BillSplitFormModel> Splits { get; set; } = [];

    public CreateBillRequest ToCreateRequest() => new()
    {
        Title = Title,
        Description = Description,
        Amount = Amount,
        Currency = Currency,
        Category = Category,
        BillDate = new DateTimeOffset(BillDate, TimeSpan.Zero),
        Notes = Notes,
        RelatedEntityId = RelatedEntityId,
        RelatedEntityType = RelatedEntityType,
        Splits = Splits.Select(s => new BillSplitRequest
        {
            UserId = s.UserId,
            Percentage = s.Percentage
        }).ToList()
    };

    public UpdateBillRequest ToUpdateRequest() => new()
    {
        Id = Id ?? Guid.Empty,
        Title = Title,
        Description = Description,
        Amount = Amount,
        Currency = Currency,
        Category = Category,
        BillDate = new DateTimeOffset(BillDate, TimeSpan.Zero),
        Notes = Notes
    };

    public static BillFormModel FromDetail(BillDetailDto detail) => new()
    {
        Id = detail.Id,
        Title = detail.Title,
        Description = detail.Description,
        Amount = detail.Amount,
        Currency = detail.Currency,
        Category = detail.Category,
        BillDate = detail.BillDate.LocalDateTime.Date,
        Notes = detail.Notes,
        RelatedEntityId = detail.RelatedEntityId,
        RelatedEntityType = detail.RelatedEntityType,
        Splits = detail.Splits.Select(s => new BillSplitFormModel
        {
            UserId = s.UserId,
            Percentage = s.Percentage
        }).ToList()
    };
}

public sealed class BillSplitFormModel
{
    public string UserId { get; set; } = string.Empty;
    public decimal? Percentage { get; set; }
}
