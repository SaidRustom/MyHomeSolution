using System.Net.Http.Json;
using BlazorUI.Models.Common;
using BlazorUI.Models.Users;
using BlazorUI.Services.Contracts;
using BlazorUI.Services.Infrastructure;

namespace BlazorUI.Services;

public sealed class UserService(HttpClient httpClient)
    : ApiServiceBase(httpClient), IUserService
{
    private const string BasePath = "api/users";

    public Task<ApiResult<PaginatedList<UserDto>>> GetUsersAsync(
        string? searchTerm = null,
        bool? isActive = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQueryString(
            ("searchTerm", searchTerm),
            ("isActive", isActive?.ToString()),
            ("pageNumber", pageNumber.ToString()),
            ("pageSize", pageSize.ToString()));

        return GetAsync<PaginatedList<UserDto>>($"{BasePath}{query}", cancellationToken);
    }

    public Task<ApiResult<UserDetailDto>> GetUserByIdAsync(
        string id, CancellationToken cancellationToken = default)
    {
        return GetAsync<UserDetailDto>($"{BasePath}/{Uri.EscapeDataString(id)}", cancellationToken);
    }

    public Task<ApiResult<string>> CreateUserAsync(
        CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        return PostAsync<string>(BasePath, request, cancellationToken);
    }

    public Task<ApiResult> ActivateUserAsync(
        string id, CancellationToken cancellationToken = default)
    {
        return PostAsync($"{BasePath}/{Uri.EscapeDataString(id)}/activate", cancellationToken: cancellationToken);
    }

    public Task<ApiResult> DeactivateUserAsync(
        string id, CancellationToken cancellationToken = default)
    {
        return PostAsync($"{BasePath}/{Uri.EscapeDataString(id)}/deactivate", cancellationToken: cancellationToken);
    }

    public Task<ApiResult> AssignRoleAsync(
        string userId, string role, CancellationToken cancellationToken = default)
    {
        var body = new { UserId = userId, Role = role };
        return PostAsync($"{BasePath}/{Uri.EscapeDataString(userId)}/roles", body, cancellationToken);
    }

    public Task<ApiResult> RemoveRoleAsync(
        string userId, string roleName, CancellationToken cancellationToken = default)
    {
        return DeleteAsync(
            $"{BasePath}/{Uri.EscapeDataString(userId)}/roles/{Uri.EscapeDataString(roleName)}",
            cancellationToken);
    }

    public Task<ApiResult<UserDetailDto>> GetCurrentUserAsync(
        CancellationToken cancellationToken = default)
    {
        return GetAsync<UserDetailDto>($"{BasePath}/me", cancellationToken);
    }

    public Task<ApiResult> UpdateCurrentUserAsync(
        UpdateUserProfileRequest request, CancellationToken cancellationToken = default)
    {
        return PutAsync($"{BasePath}/me", request, cancellationToken);
    }

    public Task<ApiResult> ChangePasswordAsync(
        ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        return PostAsync($"{BasePath}/me/change-password", request, cancellationToken);
    }

    public async Task<ApiResult<AvatarUploadResponse>> UploadAvatarAsync(
        Stream fileStream, string fileName, string contentType,
        CancellationToken cancellationToken = default)
    {
        using var content = new MultipartFormDataContent();
        using var streamContent = new StreamContent(fileStream);
        streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        content.Add(streamContent, "file", fileName);

        using var response = await Http.PostAsync($"{BasePath}/me/avatar", content, cancellationToken);

        var statusCode = (int)response.StatusCode;

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<AvatarUploadResponse>(cancellationToken: cancellationToken);
            return ApiResult<AvatarUploadResponse>.Success(result!, statusCode);
        }

        return ApiResult<AvatarUploadResponse>.Failure(
            new Models.Common.ApiProblemDetails { Title = "Upload failed", Detail = "Failed to upload avatar." },
            statusCode);
    }

    public async Task<string?> GetAvatarDataUrlAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await Http.GetAsync($"{BasePath}/me/avatar", cancellationToken);

            if (!response.IsSuccessStatusCode)
                return null;

            var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/png";
            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            return $"data:{contentType};base64,{Convert.ToBase64String(bytes)}";
        }
        catch
        {
            return null;
        }
    }
}
