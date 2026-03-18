using BlazorUI.Components.Budgets;
using BlazorUI.Components.Common;
using BlazorUI.Components.Connections;
using BlazorUI.Models.Budgets;
using BlazorUI.Models.Common;
using BlazorUI.Services.Contracts;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace BlazorUI.Pages.Budgets;

public partial class BudgetList : IDisposable
{
    [Inject]
    IBudgetService BudgetService { get; set; } = default!;

    [Inject]
    NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    DialogService DialogService { get; set; } = default!;

    [Inject]
    NotificationService NotificationService { get; set; } = default!;

    static readonly DialogOptions FormDialogOptions = new()
    {
        Width = "700px",
        Height = "620px",
        Resizable = true,
        Draggable = true,
        CloseDialogOnOverlayClick = false,
        ShowClose = false
    };

    PaginatedList<BudgetBriefDto> BudgetData { get; set; } = new();
    bool IsLoading { get; set; }
    ApiProblemDetails? Error { get; set; }

    // Filter state
    string? SearchTerm { get; set; }
    BudgetCategory? SelectedCategory { get; set; }
    BudgetPeriod? SelectedPeriod { get; set; }
    bool? IsRecurring { get; set; }
    bool? IsOverBudget { get; set; }
    bool? RootOnly { get; set; }
    string? SortBy { get; set; }
    string? SortDirection { get; set; }

    int _currentPage = 1;
    const int PageSize = 20;
    string? _sortBy;
    string? _sortDirection;

    CancellationTokenSource _cts = new();

    IEnumerable<BudgetCategory> Categories => Enum.GetValues<BudgetCategory>();
    IEnumerable<BudgetPeriod> Periods => Enum.GetValues<BudgetPeriod>();

    protected override async Task OnInitializedAsync()
    {
        await LoadBudgetsAsync();
    }

    async Task LoadBudgetsAsync()
    {
        IsLoading = true;
        Error = null;

        var result = await BudgetService.GetBudgetsAsync(
            pageNumber: _currentPage,
            pageSize: PageSize,
            category: SelectedCategory,
            period: SelectedPeriod,
            searchTerm: SearchTerm,
            isRecurring: IsRecurring,
            isOverBudget: IsOverBudget,
            rootOnly: RootOnly,
            sortBy: _sortBy,
            sortDirection: _sortDirection,
            cancellationToken: _cts.Token);

        if (result.IsSuccess)
        {
            BudgetData = result.Value;
        }
        else
        {
            Error = result.Problem;
        }

        IsLoading = false;
    }

    async Task OnLoadDataAsync(LoadDataArgs args)
    {
        _currentPage = (args.Skip ?? 0) / PageSize + 1;

        if (args.Sorts?.Any() == true)
        {
            var sort = args.Sorts.First();
            _sortBy = sort.Property;
            _sortDirection = sort.SortOrder == SortOrder.Descending ? "desc" : "asc";
        }

        await LoadBudgetsAsync();
    }

    async Task OnSearchAsync()
    {
        _currentPage = 1;
        await LoadBudgetsAsync();
    }

    async Task OnClearFiltersAsync()
    {
        _currentPage = 1;
        SearchTerm = null;
        SelectedCategory = null;
        SelectedPeriod = null;
        IsRecurring = null;
        IsOverBudget = null;
        RootOnly = null;
        _sortBy = null;
        _sortDirection = null;
        await LoadBudgetsAsync();
    }

    void ViewBudget(BudgetBriefDto budget)
    {
        NavigationManager.NavigateTo($"/budgets/{budget.Id}");
    }

    async Task CreateBudgetAsync()
    {
        var model = new BudgetFormModel();
        var result = await DialogService.OpenAsync<BudgetFormDialog>(
            "Create Budget",
            new Dictionary<string, object>
            {
                { nameof(BudgetFormDialog.Model), model },
                { nameof(BudgetFormDialog.IsEdit), false }
            },
            FormDialogOptions);

        if (result is true)
            await LoadBudgetsAsync();
    }

    async Task EditBudgetAsync(BudgetBriefDto budget)
    {
        var detailResult = await BudgetService.GetBudgetByIdAsync(budget.Id, _cts.Token);
        if (!detailResult.IsSuccess)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Error",
                Detail = detailResult.Problem.ToUserMessage(),
                Duration = 5000
            });
            return;
        }

        var model = BudgetFormModel.FromDetail(detailResult.Value);
        var result = await DialogService.OpenAsync<BudgetFormDialog>(
            "Edit Budget",
            new Dictionary<string, object>
            {
                { nameof(BudgetFormDialog.Model), model },
                { nameof(BudgetFormDialog.IsEdit), true }
            },
            FormDialogOptions);

        if (result is true)
            await LoadBudgetsAsync();
    }

    async Task DeleteBudgetAsync(BudgetBriefDto budget)
    {
        var confirmed = await DialogService.OpenAsync<ConfirmDialog>(
            "Delete Budget",
            new Dictionary<string, object>
            {
                { nameof(ConfirmDialog.Message), $"Are you sure you want to delete '{budget.Name}'? Child budgets will be detached." },
                { nameof(ConfirmDialog.ConfirmText), "Delete" },
                { nameof(ConfirmDialog.ConfirmStyle), ButtonStyle.Danger },
                { nameof(ConfirmDialog.ConfirmIcon), "delete" }
            },
            new DialogOptions { Width = "450px", CloseDialogOnOverlayClick = false });

        if (confirmed is true)
        {
            var deleteResult = await BudgetService.DeleteBudgetAsync(budget.Id, _cts.Token);

            if (deleteResult.IsSuccess)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = "Budget Deleted",
                    Detail = $"'{budget.Name}' has been deleted.",
                    Duration = 4000
                });
                await LoadBudgetsAsync();
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

    async Task ShareBudgetAsync(BudgetBriefDto budget)
    {
        await DialogService.OpenAsync<ShareDialog>(
            "Share Budget",
            new Dictionary<string, object>
            {
                { nameof(ShareDialog.EntityId), budget.Id },
                { nameof(ShareDialog.EntityType), "Budget" }
            },
            new DialogOptions { Width = "500px", CloseDialogOnOverlayClick = false });
    }

    static string GetStatusColor(decimal percentUsed)
    {
        if (percentUsed >= 80) return "var(--rz-danger)";
        if (percentUsed >= 60) return "var(--rz-warning)";
        return "var(--rz-success)";
    }

    static string GetStatusLabel(decimal percentUsed)
    {
        if (percentUsed > 100) return "Over Budget";
        if (percentUsed >= 80) return "Warning";
        return "On Track";
    }

    static BadgeStyle GetStatusBadgeStyle(decimal percentUsed)
    {
        if (percentUsed >= 80) return BadgeStyle.Danger;
        if (percentUsed >= 60) return BadgeStyle.Warning;
        return BadgeStyle.Success;
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}
