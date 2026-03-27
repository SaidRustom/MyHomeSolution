using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyHomeSolution.Application.Features.Portfolio.Commands.DeleteExperience;
using MyHomeSolution.Application.Features.Portfolio.Commands.DeleteProject;
using MyHomeSolution.Application.Features.Portfolio.Commands.DeleteSkill;
using MyHomeSolution.Application.Features.Portfolio.Commands.UpdateProfile;
using MyHomeSolution.Application.Features.Portfolio.Commands.UpsertExperience;
using MyHomeSolution.Application.Features.Portfolio.Commands.UpsertProject;
using MyHomeSolution.Application.Features.Portfolio.Commands.UpsertSkill;
using MyHomeSolution.Application.Features.Portfolio.Common;
using MyHomeSolution.Application.Features.Portfolio.Queries.GetAdminPortfolio;

namespace MyHomeSolution.Api.Controllers;

[ApiController]
[Route("api/admin/portfolio")]
[Authorize(Roles = "Administrator")]
public sealed class PortfolioAdminController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(AdminPortfolioDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAdminPortfolio(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetAdminPortfolioQuery(), cancellationToken);
        return Ok(result);
    }

    // ── Profile ──────────────────────────────────────────────────────────

    [HttpPut("profile")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateProfileCommand command, CancellationToken cancellationToken)
    {
        await sender.Send(command, cancellationToken);
        return NoContent();
    }

    // ── Experiences ──────────────────────────────────────────────────────

    [HttpPost("experiences")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpsertExperience(
        [FromBody] UpsertExperienceCommand command, CancellationToken cancellationToken)
    {
        var id = await sender.Send(command, cancellationToken);
        return Ok(id);
    }

    [HttpDelete("experiences/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteExperience(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteExperienceCommand(id), cancellationToken);
        return NoContent();
    }

    // ── Projects ─────────────────────────────────────────────────────────

    [HttpPost("projects")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpsertProject(
        [FromBody] UpsertProjectCommand command, CancellationToken cancellationToken)
    {
        var id = await sender.Send(command, cancellationToken);
        return Ok(id);
    }

    [HttpDelete("projects/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteProject(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteProjectCommand(id), cancellationToken);
        return NoContent();
    }

    // ── Skills ───────────────────────────────────────────────────────────

    [HttpPost("skills")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpsertSkill(
        [FromBody] UpsertSkillCommand command, CancellationToken cancellationToken)
    {
        var id = await sender.Send(command, cancellationToken);
        return Ok(id);
    }

    [HttpDelete("skills/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteSkill(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteSkillCommand(id), cancellationToken);
        return NoContent();
    }
}
