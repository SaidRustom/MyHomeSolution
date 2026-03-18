using BlazorUI.Models.Budgets;
using BlazorUI.Models.Common;
using BlazorUI.Services.Contracts;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace BlazorUI.Components.Budgets;

public partial class BudgetFormDialog
{
    [Inject]
    DialogService DialogService { get; set; } = default!;

    [Inject]
    IBudgetService BudgetService { get; set; } = default!;

    [Inject]
    NotificationService Notifications { get; set; } = default!;

    [Parameter]
    public BudgetFormModel Model { get; set; } = new();

    [Parameter]
    public bool IsEdit { get; set; }

    bool IsBusy { get; set; }

    string? ErrorMessage { get; set; }

    string SubmitText => IsEdit ? "Save Changes" : "Create Budget";

    IEnumerable<BudgetCategory> Categories => Enum.GetValues<BudgetCategory>();

    IEnumerable<BudgetPeriod> Periods => Enum.GetValues<BudgetPeriod>();

    PaginatedList<BudgetBriefDto> ParentBudgets { get; set; } = new();

    string? _parentSearchTerm;

    protected override async Task OnInitializedAsync()
    {
        await LoadParentBudgetsAsync();
    }

    async Task LoadParentBudgetsAsync()
    {
        var result = await BudgetService.GetBudgetsAsync(
            pageNumber: 1, pageSize: 100, rootOnly: false);

        if (result.IsSuccess)
            ParentBudgets = result.Value;
    }

    IEnumerable<BudgetBriefDto> FilteredParentBudgets
    {
        get
        {
            var items = ParentBudgets.Items.AsEnumerable();

            if (IsEdit && Model.Id.HasValue)
                items = items.Where(b => b.Id != Model.Id.Value);

            if (!string.IsNullOrWhiteSpace(_parentSearchTerm))
                items = items.Where(b =>
                    b.Name.Contains(_parentSearchTerm, StringComparison.OrdinalIgnoreCase));

            return items;
        }
    }

    async Task OnSubmitAsync()
    {
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            if (IsEdit)
            {
                var request = new UpdateBudgetRequest
                {
                    Id = Model.Id ?? Guid.Empty,
                    Name = Model.Name,
                    Description = Model.Description,
                    Amount = Model.Amount,
                    Currency = Model.Currency,
                    Category = Model.Category,
                    Period = Model.Period,
                    StartDate = Model.StartDate,
                    EndDate = Model.EndDate,
                    IsRecurring = Model.IsRecurring,
                    ParentBudgetId = Model.ParentBudgetId
                };

                var result = await BudgetService.UpdateBudgetAsync(Model.Id ?? Guid.Empty, request);

                if (result.IsSuccess)
                {
                    Notifications.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Success,
                        Summary = "Budget Updated",
                        Detail = $"'{Model.Name}' has been updated successfully.",
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
                var request = new CreateBudgetRequest
                {
                    Name = Model.Name,
                    Description = Model.Description,
                    Amount = Model.Amount,
                    Currency = Model.Currency,
                    Category = Model.Category,
                    Period = Model.Period,
                    StartDate = Model.StartDate,
                    EndDate = Model.EndDate,
                    IsRecurring = Model.IsRecurring,
                    ParentBudgetId = Model.ParentBudgetId
                };

                var result = await BudgetService.CreateBudgetAsync(request);

                if (result.IsSuccess)
                {
                    Notifications.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Success,
                        Summary = "Budget Created",
                        Detail = $"'{Model.Name}' has been created successfully.",
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
}

public sealed class BudgetFormModel
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "$";
    public BudgetCategory Category { get; set; }
    public BudgetPeriod Period { get; set; } = BudgetPeriod.Monthly;
    public DateTimeOffset StartDate { get; set; } = DateTimeOffset.Now;
    public DateTimeOffset? EndDate { get; set; }
    public bool IsRecurring { get; set; } = true;
    public Guid? ParentBudgetId { get; set; }

    public static BudgetFormModel FromDetail(BudgetDetailDto detail) => new()
    {
        Id = detail.Id,
        Name = detail.Name,
        Description = detail.Description,
        Amount = detail.Amount,
        Currency = detail.Currency,
        Category = detail.Category,
        Period = detail.Period,
        StartDate = detail.StartDate,
        EndDate = detail.EndDate,
        IsRecurring = detail.IsRecurring,
        ParentBudgetId = detail.ParentBudgetId
    };
}
