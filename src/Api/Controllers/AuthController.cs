using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Constants;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Infrastructure.Identity;
using MyHomeSolution.Infrastructure.Persistence;
using MyHomeSolution.Infrastructure.Services;

namespace MyHomeSolution.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IJwtTokenService jwtTokenService,
    IIdentityService identityService,
    IDateTimeProvider dateTimeProvider,
    IEmailBackgroundQueue emailQueue,
    IConfiguration configuration,
    ApplicationDbContext dbContext,
    DemoDataSeederService demoSeeder) : ControllerBase
{
    // ── Login ───────────────────────────────────────────────────────────────

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email);

        if (user is null)
            return Unauthorized(CreateProblem("Invalid credentials", "The email or password is incorrect."));

        if (!user.IsActive)
            return Unauthorized(CreateProblem("Account disabled", "Your account has been deactivated. Please contact an administrator."));

        if (!user.EmailConfirmed)
            return Unauthorized(CreateProblem("Email not verified", "Please verify your email address before signing in. Check your inbox for a verification link."));

        var result = await signInManager.CheckPasswordSignInAsync(
            user, request.Password, lockoutOnFailure: true);

        if (result.IsLockedOut)
            return Unauthorized(CreateProblem("Account locked", "Your account has been locked due to too many failed attempts. Please try again later."));

        if (!result.Succeeded)
            return Unauthorized(CreateProblem("Invalid credentials", "The email or password is incorrect."));

        user.LastLoginAt = dateTimeProvider.UtcNow;
        await userManager.UpdateAsync(user);

        var tokens = await jwtTokenService.GenerateTokensAsync(user.Id, cancellationToken);

        return Ok(new TokenResponse
        {
            TokenType = "Bearer",
            AccessToken = tokens.AccessToken,
            ExpiresIn = tokens.ExpiresIn,
            RefreshToken = tokens.RefreshToken
        });
    }

    // ── Register ────────────────────────────────────────────────────────────

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register(
        RegisterRequest request, CancellationToken cancellationToken)
    {
        // Demo eligibility check
        if (request.IsDemoUser)
        {
            var canDemo = await CanRegisterAsDemoAsync(request.Email, cancellationToken);
            if (!canDemo)
            {
                return BadRequest(new
                {
                    Type = "https://httpstatuses.com/400",
                    Title = "Demo registration unavailable",
                    Status = 400,
                    Detail = "This email is currently in use as an active account or has an active demo session. Please wait for the current session to expire or use a different email."
                });
            }
        }

        var (result, userId) = await identityService.CreateUserAsync(
            request.Email, request.Password, request.FirstName, request.LastName,
            cancellationToken);

        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                Type = "https://httpstatuses.com/400",
                Title = "Registration failed",
                Status = 400,
                Detail = "One or more validation errors occurred.",
                Errors = new Dictionary<string, string[]>
                {
                    ["Registration"] = result.Errors.ToArray()
                }
            });
        }

        if (request.IsDemoUser)
        {
            var now = dateTimeProvider.UtcNow;

            // Record in DemoUsers table
            dbContext.DemoUsers.Add(new DemoUser
            {
                UserId = userId,
                Email = request.Email,
                FullName = $"{request.FirstName} {request.LastName}",
                CreatedAt = now,
                ExpiresAt = now.AddHours(24),
                IsActive = true,
                ActionCount = 0
            });
            await dbContext.SaveChangesAsync(cancellationToken);

            // Auto-confirm email for demo users so they can log in immediately
            var confirmToken = await identityService.GenerateEmailConfirmationTokenAsync(userId, cancellationToken);
            await identityService.ConfirmEmailAsync(userId, confirmToken, cancellationToken);

            // Seed demo data in the background
            _ = Task.Run(async () =>
            {
                try
                {
                    await demoSeeder.SeedAsync(userId, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    // Logged inside the seeder; swallow here so registration still succeeds
                    _ = ex;
                }
            }, CancellationToken.None);

            // Send demo-specific welcome email
            await SendDemoEmailConfirmationAsync(request.Email, request.FirstName, cancellationToken);

            return Ok(new { IsDemo = true, Message = "Demo account created! You can log in immediately." });
        }

        await SendEmailConfirmationAsync(userId, request.Email, request.FirstName, cancellationToken);

        return Ok();
    }

    private async Task<bool> CanRegisterAsDemoAsync(string email, CancellationToken cancellationToken)
    {
        // Check if email is used by a non-demo active account
        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser is not null)
        {
            // Check if this user is from an active demo session
            var isActiveDemo = await dbContext.DemoUsers
                .AnyAsync(d => d.UserId == existingUser.Id && d.IsActive, cancellationToken);
            if (!isActiveDemo)
                return false; // It's a real account
            return false; // Active demo session exists
        }

        // Check if there's an active demo session for this email
        var hasActiveDemo = await dbContext.DemoUsers
            .AnyAsync(d => d.Email == email && d.IsActive, cancellationToken);

        return !hasActiveDemo;
    }

    // ── Refresh ─────────────────────────────────────────────────────────────

    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(
        RefreshRequest request, CancellationToken cancellationToken)
    {
        var tokens = await jwtTokenService.RefreshTokensAsync(
            request.RefreshToken, cancellationToken);

        if (tokens is null)
            return Unauthorized(CreateProblem("Invalid token", "The refresh token is invalid or has expired."));

        return Ok(new TokenResponse
        {
            TokenType = "Bearer",
            AccessToken = tokens.AccessToken,
            ExpiresIn = tokens.ExpiresIn,
            RefreshToken = tokens.RefreshToken
        });
    }

    // ── DTOs ────────────────────────────────────────────────────────────────

    private static object CreateProblem(string title, string detail) => new
    {
        Type = "https://httpstatuses.com/401",
        Title = title,
        Status = 401,
        Detail = detail
    };

    private static object CreateBadRequest(string title, string detail) => new
    {
        Type = "https://httpstatuses.com/400",
        Title = title,
        Status = 400,
        Detail = detail
    };

    private async Task SendEmailConfirmationAsync(
        string userId, string email, string firstName, CancellationToken cancellationToken)
    {
        var token = await identityService.GenerateEmailConfirmationTokenAsync(userId, cancellationToken);

        var blazorBaseUrl = "https://www.saidrustom.ca";
        var encodedToken = Uri.EscapeDataString(token);
        var encodedUserId = Uri.EscapeDataString(userId);
        var confirmUrl = $"{blazorBaseUrl}/confirm-email?userId={encodedUserId}&token={encodedToken}";

        var html = EmailTemplates.EmailConfirmation(firstName, confirmUrl);

        await emailQueue.EnqueueAsync(
            new EmailMessage(email, firstName, "Verify Your Email — MyHome", html),
            cancellationToken);
    }

    private async Task SendDemoEmailConfirmationAsync(
        string email, string firstName, CancellationToken cancellationToken)
    {
        var blazorBaseUrl = "https://www.saidrustom.ca";
        var loginUrl = $"{blazorBaseUrl}/login";

        var html = EmailTemplates.DemoEmailConfirmation(firstName, loginUrl);

        await emailQueue.EnqueueAsync(
            new EmailMessage(email, firstName, "Welcome to Your MyHome Demo!", html),
            cancellationToken);
    }
}

// ── Confirm Email ──────────────────────────────────────────────────────

[ApiController]
[Route("api/auth")]
public sealed class AuthEmailController(
    IIdentityService identityService,
    IEmailBackgroundQueue emailQueue,
    IConfiguration configuration) : ControllerBase
{
    [HttpPost("confirm-email")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmEmail(
        ConfirmEmailRequest request, CancellationToken cancellationToken)
    {
        var result = await identityService.ConfirmEmailAsync(
            request.UserId, request.Token, cancellationToken);

        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                Type = "https://httpstatuses.com/400",
                Title = "Confirmation failed",
                Status = 400,
                Detail = "The confirmation link is invalid or has expired."
            });
        }

        return Ok();
    }

    [HttpPost("resend-confirmation")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ResendConfirmation(
        ResendConfirmationRequest request, CancellationToken cancellationToken)
    {
        // Always return OK to prevent email enumeration
        var tokenResult = await identityService.GeneratePasswordResetTokenAsync(
            request.Email, cancellationToken);

        if (tokenResult is null)
            return Ok();

        var user = await identityService.GetUserByIdAsync(tokenResult.Value.UserId, cancellationToken);
        if (user is null || user.EmailConfirmed)
            return Ok();

        var token = await identityService.GenerateEmailConfirmationTokenAsync(
            user.Id, cancellationToken);

        var blazorBaseUrl = "https://www.saidrustom.ca";
        var encodedToken = Uri.EscapeDataString(token);
        var encodedUserId = Uri.EscapeDataString(user.Id);
        var confirmUrl = $"{blazorBaseUrl}/confirm-email?userId={encodedUserId}&token={encodedToken}";

        var html = EmailTemplates.EmailConfirmation(user.FirstName, confirmUrl);

        await emailQueue.EnqueueAsync(
            new EmailMessage(request.Email, user.FirstName, "Verify Your Email — MyHome", html),
            cancellationToken);

        return Ok();
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword(
        ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        // Always return OK to prevent email enumeration
        var tokenResult = await identityService.GeneratePasswordResetTokenAsync(
            request.Email, cancellationToken);

        if (tokenResult is not null)
        {
            var (token, userId, userName) = tokenResult.Value;

            var blazorBaseUrl = "https://www.saidrustom.ca";
            var encodedToken = Uri.EscapeDataString(token);
            var encodedUserId = Uri.EscapeDataString(userId);
            var resetUrl = $"{blazorBaseUrl}/reset-password?userId={encodedUserId}&token={encodedToken}";

            var html = EmailTemplates.PasswordReset(userName, resetUrl);

            await emailQueue.EnqueueAsync(
                new EmailMessage(request.Email, userName, "Reset Your Password — MyHome", html),
                cancellationToken);
        }

        return Ok();
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword(
        ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await identityService.ResetPasswordAsync(
            request.UserId, request.Token, request.NewPassword, cancellationToken);

        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                Type = "https://httpstatuses.com/400",
                Title = "Reset failed",
                Status = 400,
                Detail = string.Join(" ", result.Errors)
            });
        }

        // Send password changed notification
        var userName = await identityService.GetUserNameByIdAsync(request.UserId, cancellationToken);
        var user = await identityService.GetUserByIdAsync(request.UserId, cancellationToken);

        if (user is not null)
        {
            var html = EmailTemplates.PasswordChanged(userName ?? "User");
            await emailQueue.EnqueueAsync(
                new EmailMessage(user.Email, userName, "Password Changed — MyHome", html),
                cancellationToken);
        }

        return Ok();
    }
}

public sealed record LoginRequest(string Email, string Password);
public sealed record RegisterRequest(string Email, string Password, string FirstName, string LastName, bool IsDemoUser = false);
public sealed record RefreshRequest(string RefreshToken);
public sealed record ConfirmEmailRequest(string UserId, string Token);
public sealed record ResendConfirmationRequest(string Email);
public sealed record ForgotPasswordRequest(string Email);
public sealed record ResetPasswordRequest(string UserId, string Token, string NewPassword);

public sealed record TokenResponse
{
    public string TokenType { get; init; } = "Bearer";
    public string AccessToken { get; init; } = default!;
    public int ExpiresIn { get; init; }
    public string RefreshToken { get; init; } = default!;
}
