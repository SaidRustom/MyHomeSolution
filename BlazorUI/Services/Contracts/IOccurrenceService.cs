using BlazorUI.Models.Common;
using BlazorUI.Models.Enums;
using BlazorUI.Models.Occurrences;

namespace BlazorUI.Services.Contracts;

public interface IOccurrenceService
{
    Task<ApiResult<PaginatedList<OccurrenceDto>>> GetByTaskAsync(
        Guid taskId,
        OccurrenceStatus? status = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<ApiResult<IReadOnlyCollection<CalendarOccurrenceDto>>> GetByDateRangeAsync(
        DateOnly startDate,
        DateOnly endDate,
        string? assignedToUserId = null,
        OccurrenceStatus? status = null,
        bool? assignedByMe = null,
        bool? myTasks = null,
        bool? @private = null,
        bool? shared = null,
        bool? isRecurring = null,
        bool? hasBill = null,
        TaskCategory? category = null,
        TaskPriority? priority = null,
        CancellationToken cancellationToken = default);

    Task<ApiResult<PaginatedList<CalendarOccurrenceDto>>> GetUpcomingAsync(
        int pageNumber = 1,
        int pageSize = 20,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        CancellationToken cancellationToken = default);

    Task<ApiResult> StartAsync(
        Guid id, StartOccurrenceRequest? request = null, CancellationToken cancellationToken = default);

    Task<ApiResult> CompleteAsync(
        Guid id, CompleteOccurrenceRequest? request = null, CancellationToken cancellationToken = default);

    Task<ApiResult> SkipAsync(
        Guid id, SkipOccurrenceRequest? request = null, CancellationToken cancellationToken = default);

    Task<ApiResult> RescheduleAsync(
        Guid id, RescheduleOccurrenceRequest request, CancellationToken cancellationToken = default);

    Task<ApiResult> UpdateNotesAsync(
        Guid id, string? notes, CancellationToken cancellationToken = default);

    Task<ApiResult> RevertAsync(
        Guid id, string? notes = null, CancellationToken cancellationToken = default);
}
