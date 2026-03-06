using BlazorUI.Models.Common;
using BlazorUI.Models.Enums;
using BlazorUI.Models.Occurrences;
using BlazorUI.Services.Contracts;
using BlazorUI.Services.Infrastructure;

namespace BlazorUI.Services;

public sealed class OccurrenceService(HttpClient httpClient)
    : ApiServiceBase(httpClient), IOccurrenceService
{
    private const string BasePath = "api/occurrences";

    public Task<ApiResult<PaginatedList<OccurrenceDto>>> GetByTaskAsync(
        Guid taskId,
        OccurrenceStatus? status = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQueryString(
            ("status", status?.ToString()),
            ("pageNumber", pageNumber.ToString()),
            ("pageSize", pageSize.ToString()));

        return GetAsync<PaginatedList<OccurrenceDto>>(
            $"{BasePath}/by-task/{taskId}{query}", cancellationToken);
    }

    public Task<ApiResult<IReadOnlyCollection<CalendarOccurrenceDto>>> GetByDateRangeAsync(
        DateOnly startDate,
        DateOnly endDate,
        string? assignedToUserId = null,
        OccurrenceStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQueryString(
            ("startDate", startDate.ToString("O")),
            ("endDate", endDate.ToString("O")),
            ("assignedToUserId", assignedToUserId),
            ("status", status?.ToString()));

        return GetAsync<IReadOnlyCollection<CalendarOccurrenceDto>>(
            $"{BasePath}/by-date-range{query}", cancellationToken);
    }

    public Task<ApiResult<PaginatedList<CalendarOccurrenceDto>>> GetUpcomingAsync(
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQueryString(
            ("pageNumber", pageNumber.ToString()),
            ("pageSize", pageSize.ToString()));

        return GetAsync<PaginatedList<CalendarOccurrenceDto>>(
            $"{BasePath}/upcoming{query}", cancellationToken);
    }

    public Task<ApiResult> StartAsync(
        Guid id, StartOccurrenceRequest? request = null, CancellationToken cancellationToken = default)
    {
        return PostAsync($"{BasePath}/{id}/start", request, cancellationToken);
    }

    public Task<ApiResult> CompleteAsync(
        Guid id, CompleteOccurrenceRequest? request = null, CancellationToken cancellationToken = default)
    {
        return PostAsync($"{BasePath}/{id}/complete", request, cancellationToken);
    }

    public Task<ApiResult> SkipAsync(
        Guid id, SkipOccurrenceRequest? request = null, CancellationToken cancellationToken = default)
    {
        return PostAsync($"{BasePath}/{id}/skip", request, cancellationToken);
    }

    public Task<ApiResult> RescheduleAsync(
        Guid id, RescheduleOccurrenceRequest request, CancellationToken cancellationToken = default)
    {
        return PostAsync($"{BasePath}/{id}/reschedule", request, cancellationToken);
    }
}
