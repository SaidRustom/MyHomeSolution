using BlazorUI.Models.Common;
using BlazorUI.Models.Portfolio;

namespace BlazorUI.Services.Contracts;

public interface IPortfolioService
{
    Task<ApiResult<PortfolioDto>> GetPortfolioAsync(CancellationToken cancellationToken = default);
}
