using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyHomeSolution.Application.Common.Constants;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Common.Models;
using MyHomeSolution.Application.Features.Users.Commands.AssignRole;
using MyHomeSolution.Application.Features.Users.Commands.ChangePassword;
using MyHomeSolution.Application.Features.Users.Commands.CreateUser;
using MyHomeSolution.Application.Features.Users.Commands.RemoveRole;
using MyHomeSolution.Application.Features.Users.Commands.ToggleUserActivation;
using MyHomeSolution.Application.Features.Users.Commands.UpdateUser;
using MyHomeSolution.Application.Features.Users.Common;
using MyHomeSolution.Application.Features.Users.Queries.GetUserById;
using MyHomeSolution.Application.Features.Users.Queries.GetUsers;

namespace MyHomeSolution.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class UsersController(ISender sender) : ControllerBase
{
    // ── Admin: list users ───────────────────────────────────────────────────

    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<UserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] string? searchTerm = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetUsersQuery
        {
            SearchTerm = searchTerm,
            IsActive = isActive,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var result = await sender.Send(query, cancellationToken);
        return Ok(result);
    }

    // ── Admin: get user by id ───────────────────────────────────────────────

    [HttpGet("{id}")]
    [Authorize(Roles = Roles.Administrator)]
    [ProducesResponseType(typeof(UserDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUser(string id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetUserByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    // ── Admin: create user ──────────────────────────────────────────────────

    [HttpPost]
    [Authorize(Roles = Roles.Administrator)]
    [ProducesResponseType(typeof(string), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateUser(
        CreateUserCommand command, CancellationToken cancellationToken)
    {
        var userId = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetUser), new { id = userId }, userId);
    }

    // ── Admin: update any user ──────────────────────────────────────────────

    [HttpPut("{id}")]
    [Authorize(Roles = Roles.Administrator)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateUser(
        string id, UpdateUserCommand command, CancellationToken cancellationToken)
    {
        if (id != command.UserId)
            return BadRequest("Route id does not match command user id.");

        await sender.Send(command, cancellationToken);
        return NoContent();
    }

    // ── Admin: activate user ────────────────────────────────────────────────

    [HttpPost("{id}/activate")]
    [Authorize(Roles = Roles.Administrator)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActivateUser(
        string id, CancellationToken cancellationToken)
    {
        await sender.Send(
            new ToggleUserActivationCommand { UserId = id, IsActive = true },
            cancellationToken);

        return NoContent();
    }

    // ── Admin: deactivate user ──────────────────────────────────────────────

    [HttpPost("{id}/deactivate")]
    [Authorize(Roles = Roles.Administrator)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateUser(
        string id, CancellationToken cancellationToken)
    {
        await sender.Send(
            new ToggleUserActivationCommand { UserId = id, IsActive = false },
            cancellationToken);

        return NoContent();
    }

    // ── Admin: assign role ──────────────────────────────────────────────────

    [HttpPost("{id}/roles")]
    [Authorize(Roles = Roles.Administrator)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AssignRole(
        string id, AssignRoleCommand command, CancellationToken cancellationToken)
    {
        if (id != command.UserId)
            return BadRequest("Route id does not match command user id.");

        await sender.Send(command, cancellationToken);
        return NoContent();
    }

    // ── Admin: remove role ──────────────────────────────────────────────────

    [HttpDelete("{id}/roles/{roleName}")]
    [Authorize(Roles = Roles.Administrator)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RemoveRole(
        string id, string roleName, CancellationToken cancellationToken)
    {
        await sender.Send(
            new RemoveRoleCommand { UserId = id, Role = roleName },
            cancellationToken);

        return NoContent();
    }

    // ── Self-service: get own profile ───────────────────────────────────────

    [HttpGet("me")]
    [ProducesResponseType(typeof(UserDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await sender.Send(new GetUserByIdQuery(userId), cancellationToken);
        return Ok(result);
    }

    // ── Self-service: update own profile ────────────────────────────────────

    [HttpPut("me")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateCurrentUser(
        UpdateUserProfileRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        var command = new UpdateUserCommand
        {
            UserId = userId,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            AvatarUrl = request.AvatarUrl
        };

        await sender.Send(command, cancellationToken);
        return NoContent();
    }

    // ── Self-service: change own password ───────────────────────────────────

    [HttpPost("me/change-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword(
        ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        var command = new ChangePasswordCommand
        {
            UserId = userId,
            CurrentPassword = request.CurrentPassword,
            NewPassword = request.NewPassword
        };

        await sender.Send(command, cancellationToken);
        return NoContent();
    }

    // ── Self-service: upload avatar ─────────────────────────────────────────

    [HttpPost("me/avatar")]
    [ProducesResponseType(typeof(AvatarUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(5 * 1024 * 1024)] // 5 MB
    public async Task<IActionResult> UploadAvatar(
        IFormFile file,
        [FromServices] IFileStorageService fileStorage,
        CancellationToken cancellationToken)
    {
        if (file.Length == 0)
            return BadRequest("No file uploaded.");

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp", "image/gif" };
        if (!allowedTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
            return BadRequest("Only JPEG, PNG, WebP, and GIF images are allowed.");

        var userId = GetCurrentUserId();
        var extension = Path.GetExtension(file.FileName);
        var fileName = $"{userId}{extension}";

        await using var stream = file.OpenReadStream();

        var url = await fileStorage.UploadAsync(
            "avatars", fileName, stream, file.ContentType, cancellationToken);

        // Update user's avatar URL
        var user = await sender.Send(new GetUserByIdQuery(userId), cancellationToken);
        if (user is not null)
        {
            await sender.Send(new UpdateUserCommand
            {
                UserId = userId,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                AvatarUrl = url
            }, cancellationToken);
        }

        return Ok(new AvatarUploadResponse(url));
    }

    // ── Self-service: get avatar ────────────────────────────────────────────

    [HttpGet("me/avatar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAvatar(
        [FromServices] IFileStorageService fileStorage,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var user = await sender.Send(new GetUserByIdQuery(userId), cancellationToken);

        if (user?.AvatarUrl is null)
            return NotFound();

        var fileName = Path.GetFileName(user.AvatarUrl);
        var result = await fileStorage.DownloadAsync("avatars", fileName, cancellationToken);

        if (result is null)
            return NotFound();

        return File(result.Value.Content, result.Value.ContentType);
    }

    // ── Get avatar by user id ───────────────────────────────────────────────

    [HttpGet("{id}/avatar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserAvatar(
        string id,
        [FromServices] IFileStorageService fileStorage,
        CancellationToken cancellationToken)
    {
        var user = await sender.Send(new GetUserByIdQuery(id), cancellationToken);

        if (user?.AvatarUrl is null)
            return NotFound();

        var fileName = Path.GetFileName(user.AvatarUrl);
        var result = await fileStorage.DownloadAsync("avatars", fileName, cancellationToken);

        if (result is null)
            return NotFound();

        return File(result.Value.Content, result.Value.ContentType);
    }

    private string GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException();
    }

    // ── Self-service: delete own account ────────────────────────────────────

    [HttpDelete("me")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteAccount(
        [FromServices] IEmailBackgroundQueue emailQueue,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new MyHomeSolution.Application.Features.Users.Commands.DeleteAccount.DeleteAccountCommand(),
            cancellationToken);

        if (!string.IsNullOrEmpty(result.Email))
        {
            var html = MyHomeSolution.Infrastructure.Services.EmailTemplates.AccountDeleted(
                result.UserName ?? "there");

            await emailQueue.EnqueueAsync(
                new MyHomeSolution.Application.Common.Interfaces.EmailMessage(
                    result.Email, result.UserName, "Your MyHome Account Has Been Deleted", html),
                cancellationToken);
        }

        return NoContent();
    }
}

public sealed record UpdateUserProfileRequest(
    string FirstName,
    string LastName,
    string Email,
    string? AvatarUrl);

public sealed record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword);

public sealed record AvatarUploadResponse(string AvatarUrl);
