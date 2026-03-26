using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Infrastructure.Persistence;

namespace MyHomeSolution.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class DemoController(
    ApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider) : ControllerBase
{
    /// <summary>
    /// Returns the demo status for the current user.
    /// Non-demo users receive IsDemoUser = false.
    /// </summary>
    [HttpGet("status")]
    [ProducesResponseType(typeof(DemoStatusResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatus(CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
            return Ok(new DemoStatusResponse(false, null, null));

        var demoUser = await dbContext.DemoUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.UserId == userId && d.IsActive, cancellationToken);

        if (demoUser is null)
            return Ok(new DemoStatusResponse(false, null, null));

        var now = dateTimeProvider.UtcNow;
        var remaining = demoUser.ExpiresAt - now;
        if (remaining < TimeSpan.Zero)
            remaining = TimeSpan.Zero;

        return Ok(new DemoStatusResponse(true, demoUser.ExpiresAt, remaining));
    }
}

public sealed record DemoStatusResponse(
    bool IsDemoUser,
    DateTimeOffset? ExpiresAt,
    TimeSpan? TimeRemaining);
