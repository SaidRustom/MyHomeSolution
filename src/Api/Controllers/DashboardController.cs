using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyHomeSolution.Application.Features.Dashboard.Commands.SaveHomepageLayout;
using MyHomeSolution.Application.Features.Dashboard.Common;
using MyHomeSolution.Application.Features.Dashboard.Queries.GetHomepageLayout;
using MyHomeSolution.Application.Features.Dashboard.Queries.GetRequiresAttention;

namespace MyHomeSolution.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class DashboardController(ISender sender) : ControllerBase
{
    [HttpGet("requires-attention")]
    [ProducesResponseType(typeof(RequiresAttentionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRequiresAttention(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetRequiresAttentionQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("homepage-layout")]
    [ProducesResponseType(typeof(HomepageLayoutDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHomepageLayout(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetHomepageLayoutQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPut("homepage-layout")]
    [ProducesResponseType(typeof(HomepageLayoutDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> SaveHomepageLayout(
        [FromBody] SaveHomepageLayoutCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return Ok(result);
    }
}
