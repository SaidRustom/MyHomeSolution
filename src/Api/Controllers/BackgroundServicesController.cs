using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyHomeSolution.Application.Features.BackgroundServices.Queries.GetBackgroundServiceLogs;
using MyHomeSolution.Application.Features.BackgroundServices.Queries.GetBackgroundServices;

namespace MyHomeSolution.Api.Controllers;

[ApiController]
[Route("api/background-services")]
[Authorize(Roles = "Administrator")]
public sealed class BackgroundServicesController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetBackgroundServicesQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}/logs")]
    public async Task<IActionResult> GetLogs(
        Guid id,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetBackgroundServiceLogsQuery
        {
            BackgroundServiceId = id,
            PageNumber = pageNumber,
            PageSize = pageSize
        }, cancellationToken);

        return Ok(result);
    }
}
