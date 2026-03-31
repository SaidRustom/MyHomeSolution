using BlazorUI.Infrastructure.Navigation;
using BlazorUI.Models.Common;
using BlazorUI.Models.Enums;
using BlazorUI.Models.Notifications;
using BlazorUI.Services.Contracts;
using Microsoft.AspNetCore.Components;

namespace BlazorUI.Pages.Notifications;

public enum ReadFilter { All, Unread, Read }

public enum NotificationCategory
{
    Tasks,
    Bills,
    Shopping,
    Budgets,
    Connections,
    Occurrences,
    Other
}

public partial class Notifications : IDisposable
{
    [Inject] INotificationService NotificationService { get; set; } = default!;
    [Inject] NavigationManager NavigationManager { get; set; } = default!;

    // ── Filter state ──
    ReadFilter _readFilter = ReadFilter.All;
    NotificationCategory? _categoryFilter;
    DateTimeOffset? _dateFrom;
    DateTimeOffset? _dateTo;

    // ── Pagination ──
    int _pageNumber = 1;
    const int PageSize = 25;

    // ── Data ──
    PaginatedList<NotificationBriefDto>? _paginatedResult;
    int _totalCount;
    int _unreadCount;

    // ── UI state ──
    bool _isLoading;
    bool _isFiltering;
    bool _isBusy;
    ApiProblemDetails? _error;
    CancellationTokenSource _cts = new();

    // ── Category filter options for dropdown ──
    readonly List<CategoryOption> _categoryOptions =
    [
        new() { Text = "Tasks", Value = NotificationCategory.Tasks },
        new() { Text = "Bills", Value = NotificationCategory.Bills },
        new() { Text = "Shopping", Value = NotificationCategory.Shopping },
        new() { Text = "Budgets", Value = NotificationCategory.Budgets },
        new() { Text = "Connections", Value = NotificationCategory.Connections },
        new() { Text = "Schedules", Value = NotificationCategory.Occurrences },
        new() { Text = "Other", Value = NotificationCategory.Other },
    ];

    sealed record CategoryOption
    {
        public required string Text { get; init; }
        public required NotificationCategory Value { get; init; }
    }

    // ──────────────────────────────────────────────────────
    // Computed
    // ──────────────────────────────────────────────────────

    bool HasAnyFilter =>
        _readFilter != ReadFilter.All
        || _categoryFilter.HasValue
        || _dateFrom.HasValue
        || _dateTo.HasValue;

    IReadOnlyList<NotificationBriefDto> FilteredNotifications
    {
        get
        {
            if (_paginatedResult is null) return [];

            IEnumerable<NotificationBriefDto> items = _paginatedResult.Items;

            // Client-side category filter (API only supports single type)
            if (_categoryFilter.HasValue)
            {
                var types = GetTypesForCategory(_categoryFilter.Value);
                items = items.Where(n => types.Contains(n.Type));
            }

            // Client-side date filter
            if (_dateFrom.HasValue)
                items = items.Where(n => n.CreatedAt >= _dateFrom.Value.Date);

            if (_dateTo.HasValue)
                items = items.Where(n => n.CreatedAt < _dateTo.Value.Date.AddDays(1));

            return items.ToList();
        }
    }

    IEnumerable<IGrouping<string, NotificationBriefDto>> GroupedNotifications =>
        FilteredNotifications
            .GroupBy(n => GetDateGroupLabel(n.CreatedAt))
            .OrderByDescending(g => g.First().CreatedAt);

    // ──────────────────────────────────────────────────────
    // Lifecycle
    // ──────────────────────────────────────────────────────

    protected override async Task OnInitializedAsync()
    {
        _isLoading = true;
        await Task.WhenAll(LoadNotificationsAsync(), LoadUnreadCountAsync());
        _isLoading = false;
    }

    // ──────────────────────────────────────────────────────
    // Data loading
    // ──────────────────────────────────────────────────────

    async Task LoadNotificationsAsync()
    {
        _error = null;

        bool? isRead = _readFilter switch
        {
            ReadFilter.Read => true,
            ReadFilter.Unread => false,
            _ => null
        };

        var result = await NotificationService.GetNotificationsAsync(
            pageNumber: _pageNumber,
            pageSize: PageSize,
            isRead: isRead,
            cancellationToken: _cts.Token);

        if (result.IsSuccess)
        {
            _paginatedResult = result.Value;
            _totalCount = result.Value.TotalCount;
        }
        else
        {
            _error = result.Problem;
        }
    }

    async Task LoadUnreadCountAsync()
    {
        var result = await NotificationService.GetUnreadCountAsync(_cts.Token);
        if (result.IsSuccess)
            _unreadCount = result.Value;
    }

    // ──────────────────────────────────────────────────────
    // Filter actions
    // ──────────────────────────────────────────────────────

    async Task ApplyFiltersAsync()
    {
        _isFiltering = true;
        _pageNumber = 1;
        await LoadNotificationsAsync();
        _isFiltering = false;
    }

    async Task ClearFiltersAsync()
    {
        _readFilter = ReadFilter.All;
        _categoryFilter = null;
        _dateFrom = null;
        _dateTo = null;
        _pageNumber = 1;

        _isFiltering = true;
        await LoadNotificationsAsync();
        _isFiltering = false;
    }

    async Task GoToPageAsync(int page)
    {
        _pageNumber = page;
        _isFiltering = true;
        await LoadNotificationsAsync();
        _isFiltering = false;
    }

    // ──────────────────────────────────────────────────────
    // Row actions
    // ──────────────────────────────────────────────────────

    async Task OnNotificationClickedAsync(NotificationBriefDto notification)
    {
        if (!notification.IsRead)
        {
            await NotificationService.MarkAsReadAsync(notification.Id);
            _unreadCount = Math.Max(0, _unreadCount - 1);
        }

        var url = EntityNavigator.GetEntityUrl(
            notification.RelatedEntityType, notification.RelatedEntityId);

        if (url is not null)
            NavigationManager.NavigateTo(url);
    }

    async Task MarkAsReadAsync(NotificationBriefDto notification)
    {
        var result = await NotificationService.MarkAsReadAsync(notification.Id);
        if (result.IsSuccess)
        {
            _unreadCount = Math.Max(0, _unreadCount - 1);
            await LoadNotificationsAsync();
        }
    }

    async Task MarkAllAsReadAsync()
    {
        _isBusy = true;
        var result = await NotificationService.MarkAllAsReadAsync();
        if (result.IsSuccess)
        {
            _unreadCount = 0;
            await LoadNotificationsAsync();
        }
        _isBusy = false;
    }

    async Task DeleteNotificationAsync(NotificationBriefDto notification)
    {
        var result = await NotificationService.DeleteNotificationAsync(notification.Id);
        if (result.IsSuccess)
        {
            if (!notification.IsRead)
                _unreadCount = Math.Max(0, _unreadCount - 1);

            _totalCount = Math.Max(0, _totalCount - 1);
            await LoadNotificationsAsync();
        }
    }

    // ──────────────────────────────────────────────────────
    // Category / type helpers
    // ──────────────────────────────────────────────────────

    static HashSet<NotificationType> GetTypesForCategory(NotificationCategory category) => category switch
    {
        NotificationCategory.Tasks => [
            NotificationType.TaskAssigned, NotificationType.TaskUpdated,
            NotificationType.TaskDeleted, NotificationType.TaskDueSoon],
        NotificationCategory.Bills => [
            NotificationType.BillCreated, NotificationType.BillUpdated,
            NotificationType.BillDeleted, NotificationType.BillSplitPaid,
            NotificationType.BillReceiptAdded, NotificationType.BillRequiresPayment],
        NotificationCategory.Shopping => [
            NotificationType.ShoppingListCreated, NotificationType.ShoppingListUpdated,
            NotificationType.ShoppingListDeleted, NotificationType.ShoppingItemChecked],
        NotificationCategory.Budgets => [
            NotificationType.BudgetCreated, NotificationType.BudgetUpdated,
            NotificationType.BudgetDeleted, NotificationType.BudgetThresholdReached,
            NotificationType.BudgetExceeded, NotificationType.BudgetTransfer,
            NotificationType.BudgetPeriodExpired],
        NotificationCategory.Connections => [
            NotificationType.ConnectionRequestReceived,
            NotificationType.ConnectionRequestAccepted],
        NotificationCategory.Occurrences => [
            NotificationType.OccurrenceCompleted, NotificationType.OccurrenceSkipped,
            NotificationType.OccurrenceOverdue, NotificationType.OccurrenceStarted,
            NotificationType.OccurrenceRescheduled, NotificationType.OccurrenceCompletedByOther],
        _ => [NotificationType.General, NotificationType.ShareReceived,
              NotificationType.ShareRevoked, NotificationType.Mention],
    };

    static string GetCategoryLabel(NotificationType type) => type switch
    {
        NotificationType.TaskAssigned or NotificationType.TaskUpdated or
        NotificationType.TaskDeleted or NotificationType.TaskDueSoon => "Task",

        NotificationType.BillCreated or NotificationType.BillUpdated or
        NotificationType.BillDeleted or NotificationType.BillSplitPaid or
        NotificationType.BillReceiptAdded or NotificationType.BillRequiresPayment => "Bill",

        NotificationType.ShoppingListCreated or NotificationType.ShoppingListUpdated or
        NotificationType.ShoppingListDeleted or NotificationType.ShoppingItemChecked => "Shopping",

        NotificationType.BudgetCreated or NotificationType.BudgetUpdated or
        NotificationType.BudgetDeleted or NotificationType.BudgetThresholdReached or
        NotificationType.BudgetExceeded or NotificationType.BudgetTransfer or
        NotificationType.BudgetPeriodExpired => "Budget",

        NotificationType.ConnectionRequestReceived or
        NotificationType.ConnectionRequestAccepted => "Connection",

        NotificationType.OccurrenceCompleted or NotificationType.OccurrenceSkipped or
        NotificationType.OccurrenceOverdue or NotificationType.OccurrenceStarted or
        NotificationType.OccurrenceRescheduled or NotificationType.OccurrenceCompletedByOther => "Schedule",

        NotificationType.ShareReceived or NotificationType.ShareRevoked => "Share",
        NotificationType.Mention => "Mention",
        _ => "General"
    };

    // ──────────────────────────────────────────────────────
    // Icon / color helpers
    // ──────────────────────────────────────────────────────

    static string GetNotificationIcon(NotificationBriefDto n) => n.Type switch
    {
        NotificationType.TaskAssigned or NotificationType.TaskUpdated or
        NotificationType.TaskDeleted or NotificationType.TaskDueSoon => "task_alt",

        NotificationType.BillCreated or NotificationType.BillUpdated or
        NotificationType.BillDeleted => "receipt_long",
        NotificationType.BillSplitPaid => "payments",
        NotificationType.BillReceiptAdded => "document_scanner",
        NotificationType.BillRequiresPayment => "payment",

        NotificationType.ShoppingListCreated or NotificationType.ShoppingListUpdated or
        NotificationType.ShoppingListDeleted or NotificationType.ShoppingItemChecked => "shopping_cart",

        NotificationType.BudgetCreated or NotificationType.BudgetUpdated or
        NotificationType.BudgetDeleted => "account_balance",
        NotificationType.BudgetThresholdReached or NotificationType.BudgetExceeded => "warning",
        NotificationType.BudgetTransfer => "swap_horiz",
        NotificationType.BudgetPeriodExpired => "schedule",

        NotificationType.ConnectionRequestReceived or
        NotificationType.ConnectionRequestAccepted => "person_add",

        NotificationType.OccurrenceCompleted or NotificationType.OccurrenceSkipped or
        NotificationType.OccurrenceOverdue or NotificationType.OccurrenceStarted or
        NotificationType.OccurrenceRescheduled or NotificationType.OccurrenceCompletedByOther => "event_repeat",

        NotificationType.ShareReceived or NotificationType.ShareRevoked => "share",
        NotificationType.Mention => "alternate_email",
        _ => "notifications"
    };

    static string GetIconColor(NotificationBriefDto n) => n.Type switch
    {
        NotificationType.TaskAssigned or NotificationType.TaskUpdated or
        NotificationType.TaskDeleted or NotificationType.TaskDueSoon => "var(--rz-primary)",

        NotificationType.BillCreated or NotificationType.BillUpdated or
        NotificationType.BillDeleted or NotificationType.BillSplitPaid or
        NotificationType.BillReceiptAdded or NotificationType.BillRequiresPayment => "var(--rz-info)",

        NotificationType.ShoppingListCreated or NotificationType.ShoppingListUpdated or
        NotificationType.ShoppingListDeleted or NotificationType.ShoppingItemChecked => "var(--rz-warning-darker)",

        NotificationType.BudgetCreated or NotificationType.BudgetUpdated or
        NotificationType.BudgetDeleted or NotificationType.BudgetTransfer or
        NotificationType.BudgetPeriodExpired => "var(--rz-success)",
        NotificationType.BudgetThresholdReached or NotificationType.BudgetExceeded => "var(--rz-danger)",

        NotificationType.ConnectionRequestReceived or
        NotificationType.ConnectionRequestAccepted => "#9C27B0",

        NotificationType.OccurrenceCompleted or NotificationType.OccurrenceSkipped or
        NotificationType.OccurrenceOverdue or NotificationType.OccurrenceStarted or
        NotificationType.OccurrenceRescheduled or NotificationType.OccurrenceCompletedByOther => "var(--rz-secondary)",

        _ => "var(--rz-text-secondary-color)"
    };

    static string GetIconBackground(NotificationBriefDto n)
    {
        var color = GetIconColor(n);
        return $"color-mix(in srgb, {color} 12%, transparent)";
    }

    // ──────────────────────────────────────────────────────
    // Date grouping
    // ──────────────────────────────────────────────────────

    static string GetDateGroupLabel(DateTimeOffset created)
    {
        var today = DateTimeOffset.Now.Date;
        var date = created.Date;

        if (date == today) return "Today";
        if (date == today.AddDays(-1)) return "Yesterday";
        if (date >= today.AddDays(-7)) return "This Week";
        if (date >= today.AddDays(-30)) return "This Month";
        return created.ToString("MMMM yyyy");
    }

    static string GetTimeAgo(DateTimeOffset created)
    {
        var elapsed = DateTimeOffset.Now - created;

        return elapsed switch
        {
            { TotalMinutes: < 1 } => "just now",
            { TotalMinutes: < 60 } => $"{(int)elapsed.TotalMinutes}m ago",
            { TotalHours: < 24 } => $"{(int)elapsed.TotalHours}h ago",
            { TotalDays: < 7 } => $"{(int)elapsed.TotalDays}d ago",
            _ => created.ToString("MMM dd, yyyy")
        };
    }

    // ──────────────────────────────────────────────────────
    // Cleanup
    // ──────────────────────────────────────────────────────

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}
