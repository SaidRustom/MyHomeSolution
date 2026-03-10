using BlazorUI.Models.Common;
using BlazorUI.Models.Exceptions;

namespace BlazorUI.Services.Contracts;

public interface IExceptionLogService
{
    Task<ApiResult<PaginatedList<ExceptionLogBriefDto>>> GetExceptionsAsync(
        int pageNumber = 1,
        int pageSize = 20,
        int? severity = null,
        bool? isHandled = null,
        string? exceptionType = null,
        string? searchTerm = null,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        CancellationToken cancellationToken = default);

    Task<ApiResult<ExceptionLogDetailDto>> GetExceptionByIdAsync(
        Guid id, CancellationToken cancellationToken = default);

    Task<ApiResult<ExceptionSummaryDto>> GetSummaryAsync(
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        CancellationToken cancellationToken = default);

    Task<ApiResult> DeleteExceptionAsync(
        Guid id, CancellationToken cancellationToken = default);

    Task<ApiResult<ExceptionLogDetailDto>> AnalyseExceptionAsync(
        Guid id, CancellationToken cancellationToken = default);
}
