using BlazorUI.Models.Common;
using BlazorUI.Models.Dashboard;

namespace BlazorUI.Services.Contracts;

public interface IDashboardService
{
    Task<ApiResult<RequiresAttentionDto>> GetRequiresAttentionAsync(
        CancellationToken cancellationToken = default);

    Task<ApiResult<HomepageLayoutDto>> GetHomepageLayoutAsync(
        CancellationToken cancellationToken = default);

    Task<ApiResult<HomepageLayoutDto>> SaveHomepageLayoutAsync(
        SaveHomepageLayoutRequest request, CancellationToken cancellationToken = default);
}
