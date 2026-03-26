using BlazorUI.Models.Common;
using BlazorUI.Models.Demo;

namespace BlazorUI.Services.Contracts;

public interface IDemoService
{
    Task<ApiResult<DemoStatusDto>> GetDemoStatusAsync(CancellationToken cancellationToken = default);
}
