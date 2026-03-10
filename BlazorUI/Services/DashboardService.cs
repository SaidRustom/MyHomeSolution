using BlazorUI.Models.Common;
using BlazorUI.Models.Dashboard;
using BlazorUI.Services.Contracts;
using BlazorUI.Services.Infrastructure;

namespace BlazorUI.Services;

public sealed class DashboardService(HttpClient httpClient)
    : ApiServiceBase(httpClient), IDashboardService
{
    private const string BasePath = "api/dashboard";

    public Task<ApiResult<RequiresAttentionDto>> GetRequiresAttentionAsync(
        CancellationToken cancellationToken = default)
    {
        return GetAsync<RequiresAttentionDto>($"{BasePath}/requires-attention", cancellationToken);
    }
}
