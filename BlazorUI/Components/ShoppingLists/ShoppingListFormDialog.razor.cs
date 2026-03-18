using BlazorUI.Models.Budgets;
using BlazorUI.Models.Common;
using BlazorUI.Models.Enums;
using BlazorUI.Models.ShoppingLists;
using BlazorUI.Services.Contracts;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace BlazorUI.Components.ShoppingLists;

public partial class ShoppingListFormDialog
{
    [Inject]
    DialogService DialogService { get; set; } = default!;

    [Inject]
    IShoppingListService ShoppingListService { get; set; } = default!;

    [Inject]
    IBudgetService BudgetService { get; set; } = default!;

    [Inject]
    NotificationService Notifications { get; set; } = default!;

    [Parameter]
    public ShoppingListFormModel Model { get; set; } = new();

    [Parameter]
    public bool IsEdit { get; set; }

    bool IsBusy { get; set; }

    string? ErrorMessage { get; set; }

    string SubmitText => IsEdit ? "Save Changes" : "Create List";

    IEnumerable<ShoppingListCategory> Categories => Enum.GetValues<ShoppingListCategory>();

    PaginatedList<BudgetBriefDto> AvailableBudgets { get; set; } = new();

    protected override async Task OnInitializedAsync()
    {
        var result = await BudgetService.GetBudgetsAsync(pageNumber: 1, pageSize: 100);
        if (result.IsSuccess)
            AvailableBudgets = result.Value;
    }

    async Task OnSubmitAsync()
    {
        IsBusy = true;
        ErrorMessage = null;

        if (IsEdit)
        {
            var request = new UpdateShoppingListRequest
            {
                Id = Model.Id,
                Title = Model.Title,
                Description = Model.Description,
                Category = Model.Category,
                DueDate = Model.DueDate,
                DefaultBudgetId = Model.DefaultBudgetId
            };

            var result = await ShoppingListService.UpdateShoppingListAsync(Model.Id, request);

            if (result.IsSuccess)
            {
                Notifications.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = "List Updated",
                    Detail = $"'{Model.Title}' has been updated.",
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
            var request = new CreateShoppingListRequest
            {
                Title = Model.Title,
                Description = Model.Description,
                Category = Model.Category,
                DueDate = Model.DueDate,
                DefaultBudgetId = Model.DefaultBudgetId
            };

            var result = await ShoppingListService.CreateShoppingListAsync(request);

            if (result.IsSuccess)
            {
                Notifications.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = "List Created",
                    Detail = $"'{Model.Title}' has been created.",
                    Duration = 4000
                });
                DialogService.Close(true);
            }
            else
            {
                ErrorMessage = result.Problem.ToUserMessage();
            }
        }

        IsBusy = false;
    }

    void CancelAsync()
    {
        DialogService.Close(false);
    }
}

public sealed class ShoppingListFormModel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ShoppingListCategory Category { get; set; }
    public DateOnly? DueDate { get; set; }
    public Guid? DefaultBudgetId { get; set; }
}
