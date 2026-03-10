using BlazorUI.Models.Enums;
using BlazorUI.Models.Users;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace BlazorUI.Components.Bills;

public partial class BillPaymentConfirmDialog
{
    [Inject]
    DialogService DialogService { get; set; } = default!;

    /// <summary>Title of the auto-created bill.</summary>
    [Parameter]
    public string? BillTitle { get; set; }

    /// <summary>Amount of the auto-created bill.</summary>
    [Parameter]
    public decimal? BillAmount { get; set; }

    /// <summary>Currency symbol.</summary>
    [Parameter]
    public string BillCurrency { get; set; } = "$";

    /// <summary>Bill category.</summary>
    [Parameter]
    public BillCategory? BillCategory { get; set; }

    /// <summary>Current user ID — pre-selected as the default payer.</summary>
    [Parameter]
    public string? CurrentUserId { get; set; }

    /// <summary>Current user DTO for the FriendPicker fallback.</summary>
    [Parameter]
    public UserDto? CurrentUserDto { get; set; }

    /// <summary>
    /// Users eligible to pay this bill (split participants + shared users).
    /// When populated, a local dropdown is rendered instead of FriendPicker.
    /// </summary>
    [Parameter]
    public List<EligibleUserOption> EligibleUsers { get; set; } = [];

    bool MarkAsPaid { get; set; } = true;

    string? SelectedPaidByUserId { get; set; }

    protected override void OnInitialized()
    {
        SelectedPaidByUserId = CurrentUserId;
    }

    void SetMarkAsPaid(bool value)
    {
        MarkAsPaid = value;
        if (value && string.IsNullOrEmpty(SelectedPaidByUserId))
        {
            SelectedPaidByUserId = CurrentUserId;
        }
    }

    void Confirm()
    {
        var result = new BillPaymentConfirmResult
        {
            MarkAsPaid = MarkAsPaid,
            PaidByUserId = MarkAsPaid ? SelectedPaidByUserId : null
        };
        DialogService.Close(result);
    }
}

/// <summary>Result returned by the BillPaymentConfirmDialog.</summary>
public sealed class BillPaymentConfirmResult
{
    public bool MarkAsPaid { get; init; }
    public string? PaidByUserId { get; init; }
}

/// <summary>Represents a user eligible to pay a bill.</summary>
public sealed class EligibleUserOption
{
    public required string UserId { get; init; }
    public required string DisplayName { get; init; }
    public string? AvatarUrl { get; init; }
}
