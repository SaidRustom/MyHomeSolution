using BlazorUI.Models.Budgets;
using BlazorUI.Models.Common;
using BlazorUI.Services.Contracts;
using BlazorUI.Services.Infrastructure;

namespace BlazorUI.Services;

public sealed class BudgetService(HttpClient httpClient)
    : ApiServiceBase(httpClient), IBudgetService
{
    private const string BasePath = "api/budgets";

    public Task<ApiResult<PaginatedList<BudgetBriefDto>>> GetBudgetsAsync(
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
        CancellationToken cancellationToken = default)
    {
        var query = BuildQueryString(
            ("pageNumber", pageNumber.ToString()),
            ("pageSize", pageSize.ToString()),
            ("category", category?.ToString()),
            ("period", period?.ToString()),
            ("searchTerm", searchTerm),
            ("isRecurring", isRecurring?.ToString()),
            ("isOverBudget", isOverBudget?.ToString()),
            ("parentBudgetId", parentBudgetId?.ToString()),
            ("rootOnly", rootOnly?.ToString()),
            ("sortBy", sortBy),
            ("sortDirection", sortDirection));

        return GetAsync<PaginatedList<BudgetBriefDto>>($"{BasePath}{query}", cancellationToken);
    }

    public Task<ApiResult<BudgetDetailDto>> GetBudgetByIdAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        return GetAsync<BudgetDetailDto>($"{BasePath}/{id}", cancellationToken);
    }

    public Task<ApiResult<IReadOnlyList<BudgetOccurrenceDto>>> GetOccurrencesAsync(
        Guid budgetId,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQueryString(
            ("fromDate", fromDate?.ToString("O")),
            ("toDate", toDate?.ToString("O")));

        return GetAsync<IReadOnlyList<BudgetOccurrenceDto>>(
            $"{BasePath}/{budgetId}/occurrences{query}", cancellationToken);
    }

    public Task<ApiResult<BudgetSummaryDto>> GetSummaryAsync(
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null,
        BudgetCategory? category = null,
        BudgetPeriod? period = null,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQueryString(
            ("fromDate", fromDate?.ToString("O")),
            ("toDate", toDate?.ToString("O")),
            ("category", category?.ToString()),
            ("period", period?.ToString()));

        return GetAsync<BudgetSummaryDto>($"{BasePath}/summary{query}", cancellationToken);
    }

    public Task<ApiResult<IReadOnlyList<BudgetTreeNodeDto>>> GetTreeAsync(
        CancellationToken cancellationToken = default)
    {
        return GetAsync<IReadOnlyList<BudgetTreeNodeDto>>($"{BasePath}/tree", cancellationToken);
    }

    public Task<ApiResult<BudgetTrendsDto>> GetTrendsAsync(
        Guid? budgetId = null,
        int periods = 6,
        DateTimeOffset? asOfDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQueryString(
            ("budgetId", budgetId?.ToString()),
            ("periods", periods.ToString()),
            ("asOfDate", asOfDate?.ToString("O")));

        return GetAsync<BudgetTrendsDto>($"{BasePath}/trends{query}", cancellationToken);
    }

    public Task<ApiResult<Guid>> CreateBudgetAsync(
        CreateBudgetRequest request, CancellationToken cancellationToken = default)
    {
        return PostAsync<Guid>(BasePath, request, cancellationToken);
    }

    public Task<ApiResult> UpdateBudgetAsync(
        Guid id, UpdateBudgetRequest request, CancellationToken cancellationToken = default)
    {
        return PutAsync($"{BasePath}/{id}", request, cancellationToken);
    }

    public Task<ApiResult> DeleteBudgetAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        return DeleteAsync($"{BasePath}/{id}", cancellationToken);
    }

    public Task<ApiResult> EditOccurrenceAmountAsync(
        Guid occurrenceId, EditOccurrenceAmountRequest request,
        CancellationToken cancellationToken = default)
    {
        return PutAsync($"{BasePath}/occurrences/{occurrenceId}/amount", request, cancellationToken);
    }

    public Task<ApiResult<Guid>> TransferFundsAsync(
        TransferFundsRequest request, CancellationToken cancellationToken = default)
    {
        return PostAsync<Guid>($"{BasePath}/transfers", request, cancellationToken);
    }
}
