using BlazorUI.Models.Common;
using BlazorUI.Models.Enums;
using BlazorUI.Models.UserConnections;
using BlazorUI.Models.Users;

namespace BlazorUI.Services.Contracts;

public interface IUserConnectionService
{
    Task<ApiResult<PaginatedList<UserConnectionDto>>> GetConnectionsAsync(
        int pageNumber = 1,
        int pageSize = 20,
        ConnectionStatus? status = null,
        string? searchTerm = null,
        CancellationToken cancellationToken = default);

    Task<ApiResult<IReadOnlyList<UserConnectionDto>>> GetPendingRequestsAsync(
        bool sent = false,
        CancellationToken cancellationToken = default);

    Task<ApiResult<IReadOnlyList<UserDto>>> SearchConnectedUsersAsync(
        string? searchTerm = null,
        int maxResults = 20,
        CancellationToken cancellationToken = default);

    Task<ApiResult<Guid>> SendConnectionRequestAsync(
        SendConnectionRequestModel request,
        CancellationToken cancellationToken = default);

    Task<ApiResult> AcceptRequestAsync(
        Guid connectionId,
        CancellationToken cancellationToken = default);

    Task<ApiResult> DeclineRequestAsync(
        Guid connectionId,
        CancellationToken cancellationToken = default);

    Task<ApiResult> CancelRequestAsync(
        Guid connectionId,
        CancellationToken cancellationToken = default);

    Task<ApiResult> RemoveConnectionAsync(
        Guid connectionId,
        CancellationToken cancellationToken = default);
}
