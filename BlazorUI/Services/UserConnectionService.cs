using BlazorUI.Models.Common;
using BlazorUI.Models.Enums;
using BlazorUI.Models.UserConnections;
using BlazorUI.Models.Users;
using BlazorUI.Services.Contracts;
using BlazorUI.Services.Infrastructure;

namespace BlazorUI.Services;

public sealed class UserConnectionService(HttpClient httpClient)
    : ApiServiceBase(httpClient), IUserConnectionService
{
    private const string BasePath = "api/user-connections";

    public Task<ApiResult<PaginatedList<UserConnectionDto>>> GetConnectionsAsync(
        int pageNumber = 1,
        int pageSize = 20,
        ConnectionStatus? status = null,
        string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQueryString(
            ("pageNumber", pageNumber.ToString()),
            ("pageSize", pageSize.ToString()),
            ("status", status?.ToString()),
            ("searchTerm", searchTerm));

        return GetAsync<PaginatedList<UserConnectionDto>>($"{BasePath}{query}", cancellationToken);
    }

    public Task<ApiResult<IReadOnlyList<UserConnectionDto>>> GetPendingRequestsAsync(
        bool sent = false,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQueryString(
            ("sent", sent.ToString().ToLowerInvariant()));

        return GetAsync<IReadOnlyList<UserConnectionDto>>($"{BasePath}/pending{query}", cancellationToken);
    }

    public Task<ApiResult<IReadOnlyList<UserDto>>> SearchConnectedUsersAsync(
        string? searchTerm = null,
        int maxResults = 20,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQueryString(
            ("searchTerm", searchTerm),
            ("maxResults", maxResults.ToString()));

        return GetAsync<IReadOnlyList<UserDto>>($"{BasePath}/friends/search{query}", cancellationToken);
    }

    public Task<ApiResult<Guid>> SendConnectionRequestAsync(
        SendConnectionRequestModel request,
        CancellationToken cancellationToken = default)
    {
        return PostAsync<Guid>(BasePath, request, cancellationToken);
    }

    public Task<ApiResult> AcceptRequestAsync(
        Guid connectionId,
        CancellationToken cancellationToken = default)
    {
        return PutAsync($"{BasePath}/{connectionId}/accept", cancellationToken: cancellationToken);
    }

    public Task<ApiResult> DeclineRequestAsync(
        Guid connectionId,
        CancellationToken cancellationToken = default)
    {
        return PutAsync($"{BasePath}/{connectionId}/decline", cancellationToken: cancellationToken);
    }

    public Task<ApiResult> CancelRequestAsync(
        Guid connectionId,
        CancellationToken cancellationToken = default)
    {
        return PutAsync($"{BasePath}/{connectionId}/cancel", cancellationToken: cancellationToken);
    }

    public Task<ApiResult> RemoveConnectionAsync(
        Guid connectionId,
        CancellationToken cancellationToken = default)
    {
        return DeleteAsync($"{BasePath}/{connectionId}", cancellationToken);
    }
}
