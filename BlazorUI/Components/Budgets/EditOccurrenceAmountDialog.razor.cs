using BlazorUI.Models.Budgets;
using BlazorUI.Models.Common;
using BlazorUI.Services.Contracts;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace BlazorUI.Components.Budgets;

public partial class EditOccurrenceAmountDialog
{
    [Inject]
    DialogService DialogService { get; set; } = default!;

    [Inject]
    IBudgetService BudgetService { get; set; } = default!;

    [Inject]
    NotificationService Notifications { get; set; } = default!;

    [Parameter, EditorRequired]
    public Guid OccurrenceId { get; set; }

    [Parameter, EditorRequired]
    public decimal CurrentAmount { get; set; }

    [Parameter]
    public string? BudgetName { get; set; }

    bool IsBusy { get; set; }
    string? ErrorMessage { get; set; }
    decimal NewAmount { get; set; }
    string? Notes { get; set; }
    bool EnableTransfer { get; set; }
    Guid? TransferOccurrenceId { get; set; }
    string? TransferReason { get; set; }

    PaginatedList<BudgetBriefDto> AvailableBudgets { get; set; } = new();
    IReadOnlyList<BudgetOccurrenceDto> TransferOccurrences { get; set; } = [];
    Guid? _selectedTransferBudgetId;

    decimal Difference => NewAmount - CurrentAmount;

    string DifferenceDisplay => Difference >= 0
        ? $"+${Difference:N2}"
        : $"-${Math.Abs(Difference):N2}";

    string DifferenceColor => Difference >= 0
        ? "var(--rz-success)"
        : "var(--rz-danger)";

    protected override async Task OnInitializedAsync()
    {
        NewAmount = CurrentAmount;
        await LoadBudgetsAsync();
    }

    async Task LoadBudgetsAsync()
    {
        var result = await BudgetService.GetBudgetsAsync(pageNumber: 1, pageSize: 100);
        if (result.IsSuccess)
            AvailableBudgets = result.Value;
    }

    async Task OnTransferBudgetChanged(object? value)
    {
        TransferOccurrenceId = null;
        TransferOccurrences = [];

        if (value is Guid budgetId && budgetId != Guid.Empty)
        {
            _selectedTransferBudgetId = budgetId;
            var result = await BudgetService.GetOccurrencesAsync(budgetId);
            if (result.IsSuccess)
                TransferOccurrences = result.Value;
        }
        else
        {
            _selectedTransferBudgetId = null;
        }
    }

    async Task OnSubmitAsync()
    {
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var request = new EditOccurrenceAmountRequest
            {
                OccurrenceId = OccurrenceId,
                NewAmount = NewAmount,
                Notes = Notes,
                TransferOccurrenceId = EnableTransfer ? TransferOccurrenceId : null,
                TransferReason = EnableTransfer ? TransferReason : null
            };

            var result = await BudgetService.EditOccurrenceAmountAsync(OccurrenceId, request);

            if (result.IsSuccess)
            {
                Notifications.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = "Amount Updated",
                    Detail = $"Occurrence amount updated to ${NewAmount:N2}.",
                    Duration = 4000
                });
                DialogService.Close(true);
            }
            else
            {
                ErrorMessage = result.Problem.ToUserMessage();
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

    static string FormatOccurrenceLabel(BudgetOccurrenceDto occ) =>
        $"{occ.PeriodStart:MMM dd} – {occ.PeriodEnd:MMM dd, yyyy} (${occ.RemainingAmount:N2} remaining)";
}
