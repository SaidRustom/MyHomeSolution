using BlazorUI.Infrastructure.Navigation;
using BlazorUI.Models.Notifications;
using BlazorUI.Models.Realtime;
using BlazorUI.Services.Contracts;
using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;
using Radzen.Blazor.Rendering;

namespace BlazorUI.Components.Notifications;

public partial class NotificationBell : IAsyncDisposable
{
    [Inject]
    INotificationService NotificationService { get; set; } = default!;

    [Inject]
    INotificationHubClient HubClient { get; set; } = default!;

    [Inject]
    NotificationService RadzenNotifications { get; set; } = default!;

    [Inject]
    NavigationManager NavigationManager { get; set; } = default!;

    RadzenButton button;
    Popup popup;

    int UnreadCount { get; set; }

    IReadOnlyCollection<NotificationBriefDto> RecentNotifications { get; set; } = [];

    bool IsLoading { get; set; }

    bool _popoverOpen;

    protected override async Task OnInitializedAsync()
    {
        HubClient.OnUserNotification += HandlePushNotification;
        HubClient.OnReconnected += HandleReconnected;

        try
        {
            await HubClient.StartAsync();
        }
        catch
        {
            // Hub connection is best-effort; the component still works via polling
        }

        await LoadUnreadCountAsync();
    }

    async Task LoadUnreadCountAsync()
    {
        var result = await NotificationService.GetUnreadCountAsync();
        if (result.IsSuccess)
        {
            UnreadCount = result.Value;
        }
    }

    async Task LoadRecentNotificationsAsync()
    {
        if (IsLoading) return;

        IsLoading = true;

        var result = await NotificationService.GetNotificationsAsync(
            pageNumber: 1, pageSize: 10);

        if (result.IsSuccess)
        {
            RecentNotifications = result.Value.Items;
        }

        IsLoading = false;
    }

    async Task TogglePopoverAsync()
    {
        _popoverOpen = !_popoverOpen;

        if (_popoverOpen)
        {
            await LoadRecentNotificationsAsync();
        }
    }

    async Task OnNotificationClickedAsync(NotificationBriefDto notification)
    {
        _popoverOpen = false;

        if (!notification.IsRead)
        {
            await NotificationService.MarkAsReadAsync(notification.Id);
            UnreadCount = Math.Max(0, UnreadCount - 1);
        }

        var url = EntityNavigator.GetEntityUrl(
            notification.RelatedEntityType, notification.RelatedEntityId);

        if (url is not null)
        {
            NavigationManager.NavigateTo(url);
        }

        StateHasChanged();
    }

    async Task MarkAllAsReadAsync()
    {
        var result = await NotificationService.MarkAllAsReadAsync();
        if (result.IsSuccess)
        {
            UnreadCount = 0;
            await LoadRecentNotificationsAsync();
        }
    }

    void ClosePopover()
    {
        _popoverOpen = false;
    }

    void ViewAllNotifications()
    {
        _popoverOpen = false;
        NavigationManager.NavigateTo("/notifications");
    }

    void HandlePushNotification(UserPushNotification push)
    {
        InvokeAsync(() =>
        {
            UnreadCount++;

            var url = EntityNavigator.GetEntityUrl(
                push.RelatedEntityType, push.RelatedEntityId);

            RadzenNotifications.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Info,
                Summary = push.Title ?? "New Notification",
                Detail = push.Description,
                Duration = 6000,
                Click = url is not null
                    ? _ => NavigationManager.NavigateTo(url)
                    : null
            });

            StateHasChanged();
        });
    }

    void HandleReconnected()
    {
        InvokeAsync(async () =>
        {
            await LoadUnreadCountAsync();
            StateHasChanged();
        });
    }

    string GetNotificationIcon(NotificationBriefDto notification)
    {
        return notification.Type switch
        {
            Models.Enums.NotificationType.TaskAssigned or
            Models.Enums.NotificationType.TaskUpdated or
            Models.Enums.NotificationType.TaskDeleted or
            Models.Enums.NotificationType.TaskDueSoon => "task_alt",

            Models.Enums.NotificationType.BillCreated or
            Models.Enums.NotificationType.BillUpdated or
            Models.Enums.NotificationType.BillDeleted => "receipt_long",

            Models.Enums.NotificationType.BillSplitPaid => "payments",

            Models.Enums.NotificationType.BillReceiptAdded => "document_scanner",

            Models.Enums.NotificationType.ShareReceived or
            Models.Enums.NotificationType.ShareRevoked => "share",

            Models.Enums.NotificationType.OccurrenceCompleted or
            Models.Enums.NotificationType.OccurrenceSkipped => "event_repeat",

            Models.Enums.NotificationType.ShoppingListCreated or
            Models.Enums.NotificationType.ShoppingListUpdated or
            Models.Enums.NotificationType.ShoppingListDeleted or
            Models.Enums.NotificationType.ShoppingItemChecked => "shopping_cart",

            Models.Enums.NotificationType.Mention => "alternate_email",

            _ => "notifications"
        };
    }

    string GetTimeAgo(DateTimeOffset created)
    {
        var elapsed = DateTimeOffset.Now - created;

        return elapsed switch
        {
            { TotalMinutes: < 1 } => "just now",
            { TotalMinutes: < 60 } => $"{(int)elapsed.TotalMinutes}m ago",
            { TotalHours: < 24 } => $"{(int)elapsed.TotalHours}h ago",
            { TotalDays: < 7 } => $"{(int)elapsed.TotalDays}d ago",
            _ => created.ToString("MMM dd")
        };
    }

    public async ValueTask DisposeAsync()
    {
        HubClient.OnUserNotification -= HandlePushNotification;
        HubClient.OnReconnected -= HandleReconnected;

        await HubClient.DisposeAsync();
    }
}
