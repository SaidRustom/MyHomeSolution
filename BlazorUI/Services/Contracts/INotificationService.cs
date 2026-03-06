using BlazorUI.Models.Common;
using BlazorUI.Models.Enums;
using BlazorUI.Models.Notifications;

namespace BlazorUI.Services.Contracts;

public interface INotificationService
{
    Task<ApiResult<PaginatedList<NotificationBriefDto>>> GetNotificationsAsync(
        int pageNumber = 1,
        int pageSize = 20,
        bool? isRead = null,
        NotificationType? type = null,
        CancellationToken cancellationToken = default);

    Task<ApiResult<NotificationDetailDto>> GetNotificationByIdAsync(
        Guid id, CancellationToken cancellationToken = default);

    Task<ApiResult<int>> GetUnreadCountAsync(
        CancellationToken cancellationToken = default);

    Task<ApiResult<Guid>> CreateNotificationAsync(
        CreateNotificationRequest request, CancellationToken cancellationToken = default);

    Task<ApiResult> MarkAsReadAsync(
        Guid id, CancellationToken cancellationToken = default);

    Task<ApiResult<int>> MarkAllAsReadAsync(
        CancellationToken cancellationToken = default);

    Task<ApiResult> DeleteNotificationAsync(
        Guid id, CancellationToken cancellationToken = default);
}
