using System.Web;
using BlazorUI.Models.BackgroundServices;
using BlazorUI.Models.Common;
using BlazorUI.Services.Contracts;
using BlazorUI.Services.Infrastructure;

namespace BlazorUI.Services;

public sealed class BackgroundServiceMonitorService(HttpClient httpClient)
    : ApiServiceBase(httpClient), IBackgroundServiceMonitorService
{
    public async Task<ApiResult<IReadOnlyList<BackgroundServiceDto>>> GetServicesAsync(
        CancellationToken cancellationToken = default)
    {
        return await GetAsync<IReadOnlyList<BackgroundServiceDto>>(
            "api/background-services", cancellationToken);
    }

    public async Task<ApiResult<PaginatedList<BackgroundServiceLogBriefDto>>> GetLogsAsync(
        Guid serviceId,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var qs = HttpUtility.ParseQueryString(string.Empty);
        qs["pageNumber"] = pageNumber.ToString();
        qs["pageSize"] = pageSize.ToString();

        return await GetAsync<PaginatedList<BackgroundServiceLogBriefDto>>(
            $"api/background-services/{serviceId}/logs?{qs}", cancellationToken);
    }
}
