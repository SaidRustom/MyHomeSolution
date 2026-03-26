using MediatR;
using Microsoft.AspNetCore.Mvc;
using MyHomeSolution.Application.Features.Portfolio.Common;
using MyHomeSolution.Application.Features.Portfolio.Queries.GetPortfolio;

namespace MyHomeSolution.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class PortfolioController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PortfolioDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPortfolio(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetPortfolioQuery(), cancellationToken);
        return Ok(result);
    }
}
