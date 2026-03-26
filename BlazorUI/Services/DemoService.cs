using BlazorUI.Models.Common;
using BlazorUI.Models.Demo;
using BlazorUI.Services.Contracts;
using BlazorUI.Services.Infrastructure;

namespace BlazorUI.Services;

public sealed class DemoService(HttpClient httpClient)
    : ApiServiceBase(httpClient), IDemoService
{
    private const string BasePath = "api/demo";

    public Task<ApiResult<DemoStatusDto>> GetDemoStatusAsync(
        CancellationToken cancellationToken = default)
    {
        return GetAsync<DemoStatusDto>($"{BasePath}/status", cancellationToken);
    }
}
