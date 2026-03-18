using BlazorUI.Models.Budgets;
using BlazorUI.Models.Common;
using BlazorUI.Services.Contracts;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace BlazorUI.Components.Budgets;

public partial class TransferFundsDialog
{
    [Inject]
    DialogService DialogService { get; set; } = default!;

    [Inject]
    IBudgetService BudgetService { get; set; } = default!;

    [Inject]
    NotificationService Notifications { get; set; } = default!;

    [Parameter]
    public Guid? PreselectedSourceOccurrenceId { get; set; }

    bool IsBusy { get; set; }
    string? ErrorMessage { get; set; }

    decimal Amount { get; set; }
    string? Reason { get; set; }

    PaginatedList<BudgetBriefDto> AllBudgets { get; set; } = new();

    Guid? SourceBudgetId { get; set; }
    IReadOnlyList<BudgetOccurrenceDto> SourceOccurrences { get; set; } = [];
    Guid? SourceOccurrenceId { get; set; }

    Guid? DestBudgetId { get; set; }
    IReadOnlyList<BudgetOccurrenceDto> DestOccurrences { get; set; } = [];
    Guid? DestOccurrenceId { get; set; }

    BudgetOccurrenceDto? SelectedSourceOccurrence =>
        SourceOccurrences.FirstOrDefault(o => o.Id == SourceOccurrenceId);

    BudgetOccurrenceDto? SelectedDestOccurrence =>
        DestOccurrences.FirstOrDefault(o => o.Id == DestOccurrenceId);

    protected override async Task OnInitializedAsync()
    {
        var result = await BudgetService.GetBudgetsAsync(pageNumber: 1, pageSize: 200);
        if (result.IsSuccess)
            AllBudgets = result.Value;

        if (PreselectedSourceOccurrenceId.HasValue)
            SourceOccurrenceId = PreselectedSourceOccurrenceId;
    }

    async Task OnSourceBudgetChanged(object? value)
    {
        SourceOccurrenceId = null;
        SourceOccurrences = [];

        if (value is Guid budgetId && budgetId != Guid.Empty)
        {
            SourceBudgetId = budgetId;
            var result = await BudgetService.GetOccurrencesAsync(budgetId);
            if (result.IsSuccess)
                SourceOccurrences = result.Value;
        }
        else
        {
            SourceBudgetId = null;
        }
    }

    async Task OnDestBudgetChanged(object? value)
    {
        DestOccurrenceId = null;
        DestOccurrences = [];

        if (value is Guid budgetId && budgetId != Guid.Empty)
        {
            DestBudgetId = budgetId;
            var result = await BudgetService.GetOccurrencesAsync(budgetId);
            if (result.IsSuccess)
                DestOccurrences = result.Value;
        }
        else
        {
            DestBudgetId = null;
        }
    }

    async Task OnSubmitAsync()
    {
        if (!SourceOccurrenceId.HasValue || !DestOccurrenceId.HasValue)
        {
            ErrorMessage = "Please select both source and destination occurrences.";
            return;
        }

        if (Amount <= 0)
        {
            ErrorMessage = "Amount must be greater than zero.";
            return;
        }

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var request = new TransferFundsRequest
            {
                SourceOccurrenceId = SourceOccurrenceId.Value,
                DestinationOccurrenceId = DestOccurrenceId.Value,
                Amount = Amount,
                Reason = Reason
            };

            var result = await BudgetService.TransferFundsAsync(request);

            if (result.IsSuccess)
            {
                Notifications.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = "Funds Transferred",
                    Detail = $"${Amount:N2} has been transferred successfully.",
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
        $"{occ.PeriodStart:MMM dd} – {occ.PeriodEnd:MMM dd, yyyy} (${occ.RemainingAmount:N2} avail.)";
}
