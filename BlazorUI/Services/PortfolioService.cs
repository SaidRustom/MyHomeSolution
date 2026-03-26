using BlazorUI.Models.Common;
using BlazorUI.Models.Portfolio;
using BlazorUI.Services.Contracts;
using BlazorUI.Services.Infrastructure;

namespace BlazorUI.Services;

public sealed class PortfolioService(HttpClient httpClient)
    : ApiServiceBase(httpClient), IPortfolioService
{
    private const string BasePath = "api/portfolio";

    public Task<ApiResult<PortfolioDto>> GetPortfolioAsync(CancellationToken cancellationToken = default)
    {
        return GetAsync<PortfolioDto>(BasePath, cancellationToken);
    }
}
