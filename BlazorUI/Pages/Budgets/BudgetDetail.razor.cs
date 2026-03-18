using BlazorUI.Components.Budgets;
using BlazorUI.Components.Common;
using BlazorUI.Models.Budgets;
using BlazorUI.Models.Common;
using BlazorUI.Services.Contracts;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace BlazorUI.Pages.Budgets;

public partial class BudgetDetail : IDisposable
{
    [Parameter]
    public Guid Id { get; set; }

    [Inject]
    IBudgetService BudgetService { get; set; } = default!;

    [Inject]
    NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    DialogService DialogService { get; set; } = default!;

    [Inject]
    NotificationService NotificationService { get; set; } = default!;

    BudgetDetailDto? Budget { get; set; }
    IReadOnlyList<BudgetOccurrenceDto> Occurrences { get; set; } = [];
    bool IsLoading { get; set; }
    ApiProblemDetails? Error { get; set; }

    CancellationTokenSource _cts = new();

    decimal OverallPercentUsed =>
        Budget is not null && Budget.TotalAllocated > 0
            ? Budget.TotalSpent / Budget.TotalAllocated * 100
            : 0;

    BudgetOccurrenceDto? CurrentOccurrence =>
        Occurrences.FirstOrDefault(o =>
            o.PeriodStart <= DateTimeOffset.UtcNow && o.PeriodEnd >= DateTimeOffset.UtcNow)
        ?? Occurrences.FirstOrDefault();

    IReadOnlyList<BudgetTransferDto> AllTransfers =>
        Occurrences.SelectMany(o => o.Transfers).OrderByDescending(t => t.CreatedAt).ToList();

    IReadOnlyList<BudgetBillDto> AllBills =>
        (Budget?.LinkedBills ?? [])
            .Concat(Budget?.LinkedTasks.SelectMany(t => t.Bills) ?? [])
            .Concat(Budget?.LinkedShoppingLists.SelectMany(sl => sl.Bills) ?? [])
            .GroupBy(b => b.BillId)
            .Select(g => g.First())
            .OrderByDescending(b => b.BillDate)
            .ToList();

    protected override async Task OnParametersSetAsync()
    {
        await LoadDataAsync();
    }

    async Task LoadDataAsync()
    {
        IsLoading = true;
        Error = null;

        var result = await BudgetService.GetBudgetByIdAsync(Id, _cts.Token);

        if (result.IsSuccess)
        {
            Budget = result.Value;
            var occResult = await BudgetService.GetOccurrencesAsync(Id, cancellationToken: _cts.Token);
            if (occResult.IsSuccess)
                Occurrences = occResult.Value;
        }
        else
        {
            Error = result.Problem;
        }

        IsLoading = false;
    }

    async Task EditBudgetAsync()
    {
        if (Budget is null) return;

        var model = BudgetFormModel.FromDetail(Budget);
        var result = await DialogService.OpenAsync<BudgetFormDialog>(
            "Edit Budget",
            new Dictionary<string, object>
            {
                { nameof(BudgetFormDialog.Model), model },
                { nameof(BudgetFormDialog.IsEdit), true }
            },
            new DialogOptions
            {
                Width = "700px", Height = "620px",
                Resizable = true, Draggable = true,
                CloseDialogOnOverlayClick = false, ShowClose = false
            });

        if (result is true)
            await LoadDataAsync();
    }

    async Task DeleteBudgetAsync()
    {
        if (Budget is null) return;

        var confirmed = await DialogService.OpenAsync<ConfirmDialog>(
            "Delete Budget",
            new Dictionary<string, object>
            {
                { nameof(ConfirmDialog.Message), $"Are you sure you want to delete '{Budget.Name}'? Child budgets will be detached." },
                { nameof(ConfirmDialog.ConfirmText), "Delete" },
                { nameof(ConfirmDialog.ConfirmStyle), ButtonStyle.Danger },
                { nameof(ConfirmDialog.ConfirmIcon), "delete" }
            },
            new DialogOptions { Width = "450px", CloseDialogOnOverlayClick = false });

        if (confirmed is true)
        {
            var deleteResult = await BudgetService.DeleteBudgetAsync(Budget.Id, _cts.Token);

            if (deleteResult.IsSuccess)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = "Budget Deleted",
                    Detail = $"'{Budget.Name}' has been deleted.",
                    Duration = 4000
                });
                NavigationManager.NavigateTo("/budgets");
            }
            else
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Error",
                    Detail = deleteResult.Problem.ToUserMessage(),
                    Duration = 6000
                });
            }
        }
    }

    async Task EditOccurrenceAmountAsync(BudgetOccurrenceDto occurrence)
    {
        var result = await DialogService.OpenAsync<EditOccurrenceAmountDialog>(
            "Edit Occurrence Amount",
            new Dictionary<string, object>
            {
                { nameof(EditOccurrenceAmountDialog.OccurrenceId), occurrence.Id },
                { nameof(EditOccurrenceAmountDialog.CurrentAmount), occurrence.AllocatedAmount },
                { nameof(EditOccurrenceAmountDialog.BudgetName), Budget?.Name ?? "" }
            },
            new DialogOptions
            {
                Width = "600px", Height = "650px",
                Resizable = true, Draggable = true,
                CloseDialogOnOverlayClick = false
            });

        if (result is true)
            await LoadDataAsync();
    }

    async Task OpenTransferDialogAsync()
    {
        var result = await DialogService.OpenAsync<TransferFundsDialog>(
            "Transfer Funds",
            null,
            new DialogOptions
            {
                Width = "600px", Height = "700px",
                Resizable = true, Draggable = true,
                CloseDialogOnOverlayClick = false
            });

        if (result is true)
            await LoadDataAsync();
    }

    async Task ShowHistoryAsync()
    {
        if (Budget is null) return;

        await DialogService.OpenAsync<EntityHistoryDialog>(
            "Budget History",
            new Dictionary<string, object>
            {
                { nameof(EntityHistoryDialog.EntityName), "Budget" },
                { nameof(EntityHistoryDialog.EntityId), Budget.Id.ToString() }
            },
            new DialogOptions { Width = "700px", Height = "500px", Resizable = true });
    }

    void NavigateToChild(Guid childId)
    {
        NavigationManager.NavigateTo($"/budgets/{childId}");
    }

    void NavigateToBill(Guid billId)
    {
        NavigationManager.NavigateTo($"/bills/{billId}");
    }

    void NavigateToParent()
    {
        if (Budget?.ParentBudgetId is not null)
            NavigationManager.NavigateTo($"/budgets/{Budget.ParentBudgetId}");
    }

    static string GetStatusColor(decimal percentUsed)
    {
        if (percentUsed >= 80) return "var(--rz-danger)";
        if (percentUsed >= 60) return "var(--rz-warning)";
        return "var(--rz-success)";
    }

    static string GetPercentColor(decimal pct)
    {
        if (pct >= 80) return "var(--rz-danger)";
        if (pct >= 60) return "var(--rz-warning)";
        return "var(--rz-success)";
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}
