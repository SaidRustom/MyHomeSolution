using BlazorUI.Models.Budgets;
using BlazorUI.Models.Common;

namespace BlazorUI.Services.Contracts;

public interface IBudgetService
{
    Task<ApiResult<PaginatedList<BudgetBriefDto>>> GetBudgetsAsync(
        int pageNumber = 1,
        int pageSize = 20,
        BudgetCategory? category = null,
        BudgetPeriod? period = null,
        string? searchTerm = null,
        bool? isRecurring = null,
        bool? isOverBudget = null,
        Guid? parentBudgetId = null,
        bool? rootOnly = null,
        string? sortBy = null,
        string? sortDirection = null,
        CancellationToken cancellationToken = default);

    Task<ApiResult<BudgetDetailDto>> GetBudgetByIdAsync(
        Guid id, CancellationToken cancellationToken = default);

    Task<ApiResult<IReadOnlyList<BudgetOccurrenceDto>>> GetOccurrencesAsync(
        Guid budgetId,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null,
        CancellationToken cancellationToken = default);

    Task<ApiResult<BudgetSummaryDto>> GetSummaryAsync(
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null,
        BudgetCategory? category = null,
        BudgetPeriod? period = null,
        CancellationToken cancellationToken = default);

    Task<ApiResult<IReadOnlyList<BudgetTreeNodeDto>>> GetTreeAsync(
        CancellationToken cancellationToken = default);

    Task<ApiResult<BudgetTrendsDto>> GetTrendsAsync(
        Guid? budgetId = null,
        int periods = 6,
        DateTimeOffset? asOfDate = null,
        CancellationToken cancellationToken = default);

    Task<ApiResult<Guid>> CreateBudgetAsync(
        CreateBudgetRequest request, CancellationToken cancellationToken = default);

    Task<ApiResult> UpdateBudgetAsync(
        Guid id, UpdateBudgetRequest request, CancellationToken cancellationToken = default);

    Task<ApiResult> DeleteBudgetAsync(
        Guid id, CancellationToken cancellationToken = default);

    Task<ApiResult> EditOccurrenceAmountAsync(
        Guid occurrenceId, EditOccurrenceAmountRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResult<Guid>> TransferFundsAsync(
        TransferFundsRequest request, CancellationToken cancellationToken = default);
}
