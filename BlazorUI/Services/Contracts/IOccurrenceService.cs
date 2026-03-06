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

    Task<ApiResult> CompleteAsync(
        Guid id, CompleteOccurrenceRequest? request = null, CancellationToken cancellationToken = default);

    Task<ApiResult> SkipAsync(
        Guid id, SkipOccurrenceRequest? request = null, CancellationToken cancellationToken = default);
}
