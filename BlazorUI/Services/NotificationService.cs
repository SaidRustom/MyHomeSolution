using BlazorUI.Models.Common;
using BlazorUI.Models.Enums;
using BlazorUI.Models.Notifications;
using BlazorUI.Services.Contracts;
using BlazorUI.Services.Infrastructure;

namespace BlazorUI.Services;

public sealed class NotificationService(HttpClient httpClient)
    : ApiServiceBase(httpClient), INotificationService
{
    private const string BasePath = "api/notifications";

    public Task<ApiResult<PaginatedList<NotificationBriefDto>>> GetNotificationsAsync(
        int pageNumber = 1,
        int pageSize = 20,
        bool? isRead = null,
        NotificationType? type = null,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQueryString(
            ("pageNumber", pageNumber.ToString()),
            ("pageSize", pageSize.ToString()),
            ("isRead", isRead?.ToString()),
            ("type", type?.ToString()));

        return GetAsync<PaginatedList<NotificationBriefDto>>($"{BasePath}{query}", cancellationToken);
    }

    public Task<ApiResult<NotificationDetailDto>> GetNotificationByIdAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        return GetAsync<NotificationDetailDto>($"{BasePath}/{id}", cancellationToken);
    }

    public Task<ApiResult<int>> GetUnreadCountAsync(
        CancellationToken cancellationToken = default)
    {
        return GetAsync<int>($"{BasePath}/unread-count", cancellationToken);
    }

    public Task<ApiResult<Guid>> CreateNotificationAsync(
        CreateNotificationRequest request, CancellationToken cancellationToken = default)
    {
        return PostAsync<Guid>(BasePath, request, cancellationToken);
    }

    public Task<ApiResult> MarkAsReadAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        return PutAsync($"{BasePath}/{id}/read", cancellationToken: cancellationToken);
    }

    public Task<ApiResult<int>> MarkAllAsReadAsync(
        CancellationToken cancellationToken = default)
    {
        return PutWithResponseAsync<int>($"{BasePath}/read-all", cancellationToken: cancellationToken);
    }

    public Task<ApiResult> DeleteNotificationAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        return DeleteAsync($"{BasePath}/{id}", cancellationToken);
    }
}
