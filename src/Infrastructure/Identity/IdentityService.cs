using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Common.Models;
using MyHomeSolution.Application.Features.Users.Common;

namespace MyHomeSolution.Infrastructure.Identity;

public sealed class IdentityService(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IDateTimeProvider dateTimeProvider)
    : IIdentityService
{
    public async Task<(IdentityResultDto Result, string UserId)> CreateUserAsync(
        string email, string password, string firstName, string lastName,
        CancellationToken cancellationToken = default)
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            IsActive = true,
            CreatedAt = dateTimeProvider.UtcNow
        };

        var result = await userManager.CreateAsync(user, password);

        return (result.ToResultDto(), user.Id);
    }

    public async Task<IdentityResultDto> UpdateUserAsync(
        string userId, string firstName, string lastName, string email, string? avatarUrl,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return IdentityResultDto.Failure(["User not found."]);

        user.FirstName = firstName;
        user.LastName = lastName;
        user.AvatarUrl = avatarUrl;

        if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
        {
            var setEmailResult = await userManager.SetEmailAsync(user, email);
            if (!setEmailResult.Succeeded)
                return setEmailResult.ToResultDto();

            var setUserNameResult = await userManager.SetUserNameAsync(user, email);
            if (!setUserNameResult.Succeeded)
                return setUserNameResult.ToResultDto();
        }

        var result = await userManager.UpdateAsync(user);
        return result.ToResultDto();
    }

    public async Task<IdentityResultDto> ChangePasswordAsync(
        string userId, string currentPassword, string newPassword,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return IdentityResultDto.Failure(["User not found."]);

        var result = await userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        return result.ToResultDto();
    }

    public async Task<IdentityResultDto> SetActiveStatusAsync(
        string userId, bool isActive,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return IdentityResultDto.Failure(["User not found."]);

        user.IsActive = isActive;

        var result = await userManager.UpdateAsync(user);
        return result.ToResultDto();
    }

    public async Task<IdentityResultDto> AssignToRoleAsync(
        string userId, string role,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return IdentityResultDto.Failure(["User not found."]);

        if (!await roleManager.RoleExistsAsync(role))
            return IdentityResultDto.Failure([$"Role '{role}' does not exist."]);

        if (await userManager.IsInRoleAsync(user, role))
            return IdentityResultDto.Success();

        var result = await userManager.AddToRoleAsync(user, role);
        return result.ToResultDto();
    }

    public async Task<IdentityResultDto> RemoveFromRoleAsync(
        string userId, string role,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return IdentityResultDto.Failure(["User not found."]);

        if (!await userManager.IsInRoleAsync(user, role))
            return IdentityResultDto.Success();

        var result = await userManager.RemoveFromRoleAsync(user, role);
        return result.ToResultDto();
    }

    public async Task<UserDetailDto?> GetUserByIdAsync(
        string userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return null;

        var roles = await userManager.GetRolesAsync(user);

        return new UserDetailDto
        {
            Id = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = user.FullName,
            AvatarUrl = user.AvatarUrl,
            IsActive = user.IsActive,
            EmailConfirmed = user.EmailConfirmed,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            Roles = roles.ToList()
        };
    }

    public async Task<PaginatedList<UserDto>> GetUsersAsync(
        string? searchTerm, bool? isActive, int pageNumber, int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = userManager.Users.AsNoTracking();

        if (isActive.HasValue)
            query = query.Where(u => u.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(u =>
                u.FirstName.ToLower().Contains(term) ||
                u.LastName.ToLower().Contains(term) ||
                u.Email!.ToLower().Contains(term));
        }

        query = query.OrderBy(u => u.LastName).ThenBy(u => u.FirstName);

        var totalCount = await query.CountAsync(cancellationToken);

        var users = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Email = u.Email!,
                FirstName = u.FirstName,
                LastName = u.LastName,
                FullName = u.FirstName + " " + u.LastName,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new PaginatedList<UserDto>(users, totalCount, pageNumber, pageSize);
    }

    public async Task<bool> UserExistsAsync(
        string userId, CancellationToken cancellationToken = default)
    {
        return await userManager.Users
            .AnyAsync(u => u.Id == userId, cancellationToken);
    }

    public async Task<string> GenerateEmailConfirmationTokenAsync(
        string userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId)
            ?? throw new InvalidOperationException($"User '{userId}' not found.");

        return await userManager.GenerateEmailConfirmationTokenAsync(user);
    }

    public async Task<IdentityResultDto> ConfirmEmailAsync(
        string userId, string token, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return IdentityResultDto.Failure(["User not found."]);

        var result = await userManager.ConfirmEmailAsync(user, token);
        return result.ToResultDto();
    }

    public async Task<(string Token, string UserId, string UserName)?> GeneratePasswordResetTokenAsync(
        string email, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null || !user.IsActive)
            return null;

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        return (token, user.Id, user.FirstName);
    }

    public async Task<IdentityResultDto> ResetPasswordAsync(
        string userId, string token, string newPassword,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return IdentityResultDto.Failure(["User not found."]);

        var result = await userManager.ResetPasswordAsync(user, token, newPassword);
        return result.ToResultDto();
    }

    public async Task<string?> GetUserNameByIdAsync(
        string userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        return user?.FullName;
    }

    public async Task<Dictionary<string, string>> GetUserFullNamesByIdsAsync(
        IEnumerable<string> userIds, CancellationToken cancellationToken = default)
    {
        var distinctIds = userIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList();
        if (distinctIds.Count == 0)
            return [];

        return await userManager.Users
            .AsNoTracking()
            .Where(u => distinctIds.Contains(u.Id))
            .ToDictionaryAsync(
                u => u.Id,
                u => u.FirstName + " " + u.LastName,
                cancellationToken);
    }

    public async Task<bool> IsEmailConfirmedAsync(
        string userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        return user?.EmailConfirmed ?? false;
    }

    public async Task<(IdentityResultDto Result, string? Email, string? UserName)> DeleteUserAsync(
        string userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return (IdentityResultDto.Failure(["User not found."]), null, null);

        var email = user.Email;
        var userName = user.FirstName;

        var result = await userManager.DeleteAsync(user);
        return (result.ToResultDto(), email, userName);
    }
}
