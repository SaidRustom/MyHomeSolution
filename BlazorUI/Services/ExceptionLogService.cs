using System.Web;
using BlazorUI.Models.Common;
using BlazorUI.Models.Exceptions;
using BlazorUI.Services.Contracts;
using BlazorUI.Services.Infrastructure;

namespace BlazorUI.Services;

public sealed class ExceptionLogService(HttpClient httpClient)
    : ApiServiceBase(httpClient), IExceptionLogService
{
    public async Task<ApiResult<PaginatedList<ExceptionLogBriefDto>>> GetExceptionsAsync(
        int pageNumber = 1,
        int pageSize = 20,
        int? severity = null,
        bool? isHandled = null,
        string? exceptionType = null,
        string? searchTerm = null,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        CancellationToken cancellationToken = default)
    {
        var qs = HttpUtility.ParseQueryString(string.Empty);
        qs["pageNumber"] = pageNumber.ToString();
        qs["pageSize"] = pageSize.ToString();

        if (severity.HasValue) qs["severity"] = severity.Value.ToString();
        if (isHandled.HasValue) qs["isHandled"] = isHandled.Value.ToString();
        if (!string.IsNullOrWhiteSpace(exceptionType)) qs["exceptionType"] = exceptionType;
        if (!string.IsNullOrWhiteSpace(searchTerm)) qs["searchTerm"] = searchTerm;
        if (from.HasValue) qs["from"] = from.Value.ToString("O");
        if (to.HasValue) qs["to"] = to.Value.ToString("O");

        return await GetAsync<PaginatedList<ExceptionLogBriefDto>>(
            $"api/exceptions?{qs}", cancellationToken);
    }

    public async Task<ApiResult<ExceptionLogDetailDto>> GetExceptionByIdAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        return await GetAsync<ExceptionLogDetailDto>(
            $"api/exceptions/{id}", cancellationToken);
    }

    public async Task<ApiResult<ExceptionSummaryDto>> GetSummaryAsync(
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        CancellationToken cancellationToken = default)
    {
        var qs = HttpUtility.ParseQueryString(string.Empty);
        if (from.HasValue) qs["from"] = from.Value.ToString("O");
        if (to.HasValue) qs["to"] = to.Value.ToString("O");

        var queryString = qs.ToString();
        var uri = string.IsNullOrEmpty(queryString)
            ? "api/exceptions/summary"
            : $"api/exceptions/summary?{queryString}";

        return await GetAsync<ExceptionSummaryDto>(uri, cancellationToken);
    }

    public async Task<ApiResult> DeleteExceptionAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        return await DeleteAsync($"api/exceptions/{id}", cancellationToken);
    }

    public async Task<ApiResult<ExceptionLogDetailDto>> AnalyseExceptionAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        return await PostAsync<ExceptionLogDetailDto>(
            $"api/exceptions/{id}/analyse", null, cancellationToken);
    }
}
