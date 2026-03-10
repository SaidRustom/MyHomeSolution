using MyHomeSolution.Application.Common.Models;
using MyHomeSolution.Application.Features.Users.Common;

namespace MyHomeSolution.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<(IdentityResultDto Result, string UserId)> CreateUserAsync(
        string email, string password, string firstName, string lastName,
        CancellationToken cancellationToken = default);

    Task<IdentityResultDto> UpdateUserAsync(
        string userId, string firstName, string lastName, string email, string? avatarUrl,
        CancellationToken cancellationToken = default);

    Task<IdentityResultDto> ChangePasswordAsync(
        string userId, string currentPassword, string newPassword,
        CancellationToken cancellationToken = default);

    Task<IdentityResultDto> SetActiveStatusAsync(
        string userId, bool isActive,
        CancellationToken cancellationToken = default);

    Task<IdentityResultDto> AssignToRoleAsync(
        string userId, string role,
        CancellationToken cancellationToken = default);

    Task<IdentityResultDto> RemoveFromRoleAsync(
        string userId, string role,
        CancellationToken cancellationToken = default);

    Task<UserDetailDto?> GetUserByIdAsync(
        string userId, CancellationToken cancellationToken = default);

    Task<PaginatedList<UserDto>> GetUsersAsync(
        string? searchTerm, bool? isActive, int pageNumber, int pageSize,
        CancellationToken cancellationToken = default);

    Task<bool> UserExistsAsync(
        string userId, CancellationToken cancellationToken = default);

    Task<string> GenerateEmailConfirmationTokenAsync(
        string userId, CancellationToken cancellationToken = default);

    Task<IdentityResultDto> ConfirmEmailAsync(
        string userId, string token, CancellationToken cancellationToken = default);

    Task<(string Token, string UserId, string UserName)?> GeneratePasswordResetTokenAsync(
        string email, CancellationToken cancellationToken = default);

    Task<IdentityResultDto> ResetPasswordAsync(
        string userId, string token, string newPassword,
        CancellationToken cancellationToken = default);

    Task<string?> GetUserNameByIdAsync(
        string userId, CancellationToken cancellationToken = default);

    Task<Dictionary<string, string>> GetUserFullNamesByIdsAsync(
        IEnumerable<string> userIds, CancellationToken cancellationToken = default);

    Task<bool> IsEmailConfirmedAsync(
        string userId, CancellationToken cancellationToken = default);

    Task<(IdentityResultDto Result, string? Email, string? UserName)> DeleteUserAsync(
        string userId, CancellationToken cancellationToken = default);
}
