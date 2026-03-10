using System.Security.Claims;
using BlazorUI.Models.Bills;
using BlazorUI.Models.Common;
using BlazorUI.Models.Enums;
using BlazorUI.Models.Users;
using BlazorUI.Services.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Radzen;

namespace BlazorUI.Components.Bills;

public partial class BillFormDialog
{
    [Inject]
    DialogService DialogService { get; set; } = default!;

    [Inject]
    IBillService BillService { get; set; } = default!;

    [Inject]
    NotificationService Notifications { get; set; } = default!;

    [Inject]
    IUserConnectionService UserConnectionService { get; set; } = default!;

    [CascadingParameter]
    private Task<AuthenticationState> AuthState { get; set; } = default!;

    [Parameter]
    public BillFormModel Model { get; set; } = new();

    [Parameter]
    public bool IsEdit { get; set; }

    bool IsBusy { get; set; }

    string? ErrorMessage { get; set; }

    string? CurrentUserId { get; set; }

    string? CurrentUserFullName { get; set; }

    string DialogTitle => IsEdit ? "Edit Bill" : "Create Bill";

    string SubmitText => IsEdit ? "Save Changes" : "Create Bill";

    IEnumerable<BillCategory> Categories => Enum.GetValues<BillCategory>();

    protected override async Task OnInitializedAsync()
    {
        var state = await AuthState;
        CurrentUserId = state.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? state.User.FindFirst("sub")?.Value;

        var name = state.User.FindFirst(ClaimTypes.Name)?.Value
            ?? state.User.FindFirst("name")?.Value;
        var email = state.User.FindFirst(ClaimTypes.Email)?.Value
            ?? state.User.FindFirst("email")?.Value;
        CurrentUserFullName = name ?? email ?? CurrentUserId;
    }

    /// <summary>
    /// Users eligible to be selected as "Paid By" — split participants + current user.
    /// </summary>
    List<EligibleUserOption> PaidByEligibleUsers
    {
        get
        {
            var users = new List<EligibleUserOption>();

            // Always include current user first
            if (!string.IsNullOrEmpty(CurrentUserId))
            {
                users.Add(new EligibleUserOption
                {
                    UserId = CurrentUserId,
                    DisplayName = $"{CurrentUserFullName} (You)"
                });
            }

            // Add split participants
            foreach (var split in Model.Splits)
            {
                if (string.IsNullOrEmpty(split.UserId)) continue;
                if (users.Any(u => u.UserId == split.UserId)) continue;
                users.Add(new EligibleUserOption
                {
                    UserId = split.UserId,
                    DisplayName = split.UserId // Display name resolved by split editor
                });
            }

            return users;
        }
    }

    async Task OnSubmit()
    {
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            if (IsEdit)
            {
                var request = Model.ToUpdateRequest();
                var result = await BillService.UpdateBillAsync(Model.Id ?? Guid.Empty, request);

                if (result.IsSuccess)
                {
                    Notifications.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Success,
                        Summary = "Bill Updated",
                        Detail = $"'{Model.Title}' has been updated successfully.",
                        Duration = 4000
                    });
                    DialogService.Close(true);
                }
                else
                {
                    ErrorMessage = result.Problem.ToUserMessage();
                }
            }
            else
            {
                var request = Model.ToCreateRequest();
                var result = await BillService.CreateBillAsync(request);

                if (result.IsSuccess)
                {
                    Notifications.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Success,
                        Summary = "Bill Created",
                        Detail = $"'{Model.Title}' has been created successfully.",
                        Duration = 4000
                    });
                    DialogService.Close(true);
                }
                else
                {
                    ErrorMessage = result.Problem.ToUserMessage();
                }
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"An unexpected error occurred: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    void Cancel() => DialogService.Close(null);

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
    public DateTimeOffset BillDate { get; set; } = DateTimeOffset.Now;
    public string? Notes { get; set; }
    public Guid? RelatedEntityId { get; set; }
    public string? RelatedEntityType { get; set; }
    public string? PaidByUserId { get; set; }
    public List<BillSplitFormModel> Splits { get; set; } = [];

    public CreateBillRequest ToCreateRequest() => new()
    {
        Title = Title,
        Description = Description,
        Amount = Amount,
        Currency = Currency,
        Category = Category,
        BillDate = BillDate,
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
        BillDate = BillDate,
        Notes = Notes,
        PaidByUserId = PaidByUserId,
        Splits = Splits.Count > 0
            ? Splits.Select(s => new BillSplitRequest
            {
                UserId = s.UserId,
                Percentage = s.Percentage
            }).ToList()
            : null
    };

    public static BillFormModel FromDetail(BillDetailDto detail) => new()
    {
        Id = detail.Id,
        Title = detail.Title,
        Description = detail.Description,
        Amount = detail.Amount,
        Currency = detail.Currency,
        Category = detail.Category,
        BillDate = detail.BillDate,
        Notes = detail.Notes,
        RelatedEntityId = detail.RelatedEntityId,
        RelatedEntityType = detail.RelatedEntityType,
        PaidByUserId = detail.PaidByUserId,
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
