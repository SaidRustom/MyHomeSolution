using BlazorUI.Models.BackgroundServices;
using BlazorUI.Models.Common;

namespace BlazorUI.Services.Contracts;

public interface IBackgroundServiceMonitorService
{
    Task<ApiResult<IReadOnlyList<BackgroundServiceDto>>> GetServicesAsync(
        CancellationToken cancellationToken = default);

    Task<ApiResult<PaginatedList<BackgroundServiceLogBriefDto>>> GetLogsAsync(
        Guid serviceId,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);
}
