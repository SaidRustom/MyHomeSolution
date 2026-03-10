using BlazorUI.Models.Common;
using BlazorUI.Models.Users;

namespace BlazorUI.Services.Contracts;

public interface IUserService
{
    // Admin operations
    Task<ApiResult<PaginatedList<UserDto>>> GetUsersAsync(
        string? searchTerm = null,
        bool? isActive = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<ApiResult<UserDetailDto>> GetUserByIdAsync(
        string id, CancellationToken cancellationToken = default);

    Task<ApiResult<string>> CreateUserAsync(
        CreateUserRequest request, CancellationToken cancellationToken = default);

    Task<ApiResult> ActivateUserAsync(
        string id, CancellationToken cancellationToken = default);

    Task<ApiResult> DeactivateUserAsync(
        string id, CancellationToken cancellationToken = default);

    Task<ApiResult> AssignRoleAsync(
        string userId, string role, CancellationToken cancellationToken = default);

    Task<ApiResult> RemoveRoleAsync(
        string userId, string roleName, CancellationToken cancellationToken = default);

    // Self-service operations
    Task<ApiResult<UserDetailDto>> GetCurrentUserAsync(
        CancellationToken cancellationToken = default);

    Task<ApiResult> UpdateCurrentUserAsync(
        UpdateUserProfileRequest request, CancellationToken cancellationToken = default);

    Task<ApiResult> ChangePasswordAsync(
        ChangePasswordRequest request, CancellationToken cancellationToken = default);

    Task<ApiResult<AvatarUploadResponse>> UploadAvatarAsync(
        Stream fileStream, string fileName, string contentType,
        CancellationToken cancellationToken = default);

    Task<string?> GetAvatarDataUrlAsync(
        CancellationToken cancellationToken = default);

    Task<ApiResult> DeleteAccountAsync(
        CancellationToken cancellationToken = default);
}
