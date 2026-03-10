using BlazorUI.Components.Bills;
using BlazorUI.Components.Common;
using BlazorUI.Models.Enums;
using BlazorUI.Models.Occurrences;
using BlazorUI.Models.Users;
using BlazorUI.Services.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Radzen;
using System.Security.Claims;

namespace BlazorUI.Components.Scheduler;

public partial class UpcomingTasksPanel
{
    [Inject]
    IOccurrenceService OccurrenceService { get; set; } = default!;

    [Inject]
    ITaskService TaskService { get; set; } = default!;

    [Inject]
    IBillService BillService { get; set; } = default!;

    [Inject]
    DialogService DialogService { get; set; } = default!;

    [Inject]
    NotificationService Notifications { get; set; } = default!;

    [CascadingParameter]
    private Task<AuthenticationState>? AuthState { get; set; }

    [Parameter]
    public EventCallback OnOccurrenceAction { get; set; }

    [Parameter]
    public EventCallback OnCreateTask { get; set; }

    [Parameter]
    public DateOnly SelectedDate { get; set; }

    List<CalendarOccurrenceDto> _occurrences = [];
    bool IsLoading { get; set; }
    Guid? _busyId;

    DateOnly? _lastDate;

    string? _currentUserId;
    UserDto? _currentUserDto;

    // Summary counts
    int _pendingCount;
    int _inProgressCount;
    int _completedCount;
    int _overdueCount;

    string HeaderIcon => IsToday ? "today" : "calendar_today";

    string HeaderTitle => IsToday
        ? "Today's Tasks"
        : SelectedDate.ToString("dddd");

    string HeaderSubtitle => SelectedDate.ToString("MMMM dd, yyyy");

    bool IsToday => SelectedDate == DateOnly.FromDateTime(DateTime.Today);

    protected override async Task OnInitializedAsync()
    {
        await ResolveCurrentUserAsync();
        await LoadDataAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        if (_lastDate != SelectedDate)
        {
            _lastDate = SelectedDate;
            await LoadDataAsync();
        }
    }

    async Task ResolveCurrentUserAsync()
    {
        if (_currentUserId is not null || AuthState is null) return;

        var state = await AuthState;
        _currentUserId = state.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? state.User.FindFirst("sub")?.Value;

        if (!string.IsNullOrEmpty(_currentUserId))
        {
            var email = state.User.FindFirst(ClaimTypes.Email)?.Value
                ?? state.User.FindFirst("email")?.Value ?? string.Empty;
            var name = state.User.FindFirst(ClaimTypes.Name)?.Value
                ?? state.User.FindFirst("name")?.Value ?? email;
            var parts = name.Split(' ', 2);

            _currentUserDto = new UserDto
            {
                Id = _currentUserId,
                Email = email,
                FirstName = parts.Length > 0 ? parts[0] : name,
                LastName = parts.Length > 1 ? parts[1] : string.Empty,
                FullName = $"{name} (You)"
            };
        }
    }

    async Task LoadDataAsync()
    {
        IsLoading = true;

        var result = await OccurrenceService.GetByDateRangeAsync(
            SelectedDate, SelectedDate);

        if (result.IsSuccess)
        {
            _occurrences = result.Value
                .OrderBy(o => o.Status switch
                {
                    OccurrenceStatus.Overdue => 0,
                    OccurrenceStatus.InProgress => 1,
                    OccurrenceStatus.Pending => 2,
                    OccurrenceStatus.Completed => 3,
                    _ => 4
                })
                .ThenBy(o => o.TaskPriority)
                .ToList();

            _pendingCount = _occurrences.Count(o => o.Status == OccurrenceStatus.Pending);
            _inProgressCount = _occurrences.Count(o => o.Status == OccurrenceStatus.InProgress);
            _completedCount = _occurrences.Count(o => o.Status == OccurrenceStatus.Completed);
            _overdueCount = _occurrences.Count(o => o.Status == OccurrenceStatus.Overdue);
        }

        IsLoading = false;
    }

    async Task CreateTaskClicked()
    {
        await OnCreateTask.InvokeAsync();
    }

    async Task StartOccurrenceAsync(Guid id)
    {
        _busyId = id;
        var result = await OccurrenceService.StartAsync(id, new StartOccurrenceRequest());

        if (result.IsSuccess)
        {
            Notifications.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Info,
                Summary = "Started",
                Duration = 3000
            });
            await LoadDataAsync();
            await OnOccurrenceAction.InvokeAsync();
        }

        _busyId = null;
    }

    async Task CompleteOccurrenceAsync(Guid id)
    {
        _busyId = id;
        var result = await OccurrenceService.CompleteAsync(id, new CompleteOccurrenceRequest());

        if (result.IsSuccess)
        {
            Notifications.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = "Completed",
                Duration = 3000
            });

            // Check if the task has auto-bill and show payment confirm dialog
            var occ = _occurrences.FirstOrDefault(o => o.Id == id);
            if (occ?.HasBill == true)
            {
                await ShowBillPaymentConfirmAsync(occ.TaskId, id);
            }

            await LoadDataAsync();
            await OnOccurrenceAction.InvokeAsync();
        }

        _busyId = null;
    }

    async Task ShowBillPaymentConfirmAsync(Guid taskId, Guid occurrenceId)
    {
        var taskResult = await TaskService.GetTaskByIdAsync(taskId);
        if (!taskResult.IsSuccess) return;

        var freshOcc = taskResult.Value.Occurrences.FirstOrDefault(o => o.Id == occurrenceId);
        if (freshOcc?.Bill is null) return;

        var eligibleUsers = new List<EligibleUserOption>();

        if (_currentUserDto is not null)
        {
            eligibleUsers.Add(new EligibleUserOption
            {
                UserId = _currentUserDto.Id,
                DisplayName = _currentUserDto.FullName
            });
        }

        var billResult = await BillService.GetBillByIdAsync(freshOcc.Bill.Id);
        if (billResult.IsSuccess)
        {
            foreach (var split in billResult.Value.Splits)
            {
                if (eligibleUsers.Any(u => u.UserId == split.UserId)) continue;
                eligibleUsers.Add(new EligibleUserOption
                {
                    UserId = split.UserId,
                    DisplayName = split.UserFullName ?? split.UserId,
                    AvatarUrl = split.UserAvatarUrl
                });
            }
        }

        var dialogResult = await DialogService.OpenAsync<BillPaymentConfirmDialog>(
            "Bill Payment",
            new Dictionary<string, object>
            {
                { nameof(BillPaymentConfirmDialog.BillTitle), freshOcc.Bill.Title },
                { nameof(BillPaymentConfirmDialog.BillAmount), freshOcc.Bill.Amount },
                { nameof(BillPaymentConfirmDialog.BillCurrency), freshOcc.Bill.Currency },
                { nameof(BillPaymentConfirmDialog.BillCategory), freshOcc.Bill.Category },
                { nameof(BillPaymentConfirmDialog.CurrentUserId), _currentUserId ?? string.Empty },
                { nameof(BillPaymentConfirmDialog.CurrentUserDto), _currentUserDto! },
                { nameof(BillPaymentConfirmDialog.EligibleUsers), eligibleUsers }
            },
            new DialogOptions
            {
                Width = "500px",
                CloseDialogOnOverlayClick = false,
                ShowClose = false
            });

        if (dialogResult is BillPaymentConfirmResult payResult && payResult.MarkAsPaid
            && !string.IsNullOrEmpty(payResult.PaidByUserId))
        {
            await MarkBillAsPaidAsync(freshOcc.Bill.Id, payResult.PaidByUserId);
        }
    }

    async Task MarkBillAsPaidAsync(Guid billId, string paidByUserId)
    {
        var billResult = await BillService.GetBillByIdAsync(billId);
        if (!billResult.IsSuccess) return;

        var bill = billResult.Value;
        var updateRequest = new BlazorUI.Models.Bills.UpdateBillRequest
        {
            Id = bill.Id,
            Title = bill.Title,
            Description = bill.Description,
            Amount = bill.Amount,
            Currency = bill.Currency,
            Category = bill.Category,
            BillDate = bill.BillDate,
            Notes = bill.Notes,
            PaidByUserId = paidByUserId
        };

        var updateResult = await BillService.UpdateBillAsync(billId, updateRequest);
        if (updateResult.IsSuccess)
        {
            foreach (var split in bill.Splits.Where(s =>
                string.Equals(s.UserId, paidByUserId, StringComparison.OrdinalIgnoreCase)
                && s.Status == SplitStatus.Unpaid))
            {
                await BillService.MarkSplitAsPaidAsync(billId, split.Id);
            }

            Notifications.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = "Bill Marked as Paid",
                Duration = 4000
            });
        }
    }

    async Task SkipOccurrenceAsync(Guid id)
    {
        _busyId = id;

        var zeroBalance = false;
        var occ = _occurrences.FirstOrDefault(o => o.Id == id);

        // If occurrence has a linked bill, ask about zeroing balance
        if (occ?.BillId.HasValue == true)
        {
            var confirmed = await DialogService.OpenAsync<ConfirmDialog>(
                "Zero Bill Balance?",
                new Dictionary<string, object>
                {
                    { nameof(ConfirmDialog.Message), "This occurrence has a linked bill. Would you like to reduce the bill balance to $0?" },
                    { nameof(ConfirmDialog.ConfirmText), "Yes, zero balance" },
                    { nameof(ConfirmDialog.ConfirmStyle), ButtonStyle.Warning },
                    { nameof(ConfirmDialog.ConfirmIcon), "money_off" },
                    { nameof(ConfirmDialog.CancelText), "No, keep balance" }
                },
                new DialogOptions { Width = "450px", CloseDialogOnOverlayClick = false });

            zeroBalance = confirmed is true;
        }

        var result = await OccurrenceService.SkipAsync(id, new SkipOccurrenceRequest
        {
            ZeroLinkedBillBalance = zeroBalance
        });

        if (result.IsSuccess)
        {
            Notifications.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Warning,
                Summary = "Skipped",
                Detail = zeroBalance ? "Bill balance zeroed." : null,
                Duration = 3000
            });
            await LoadDataAsync();
            await OnOccurrenceAction.InvokeAsync();
        }

        _busyId = null;
    }

    static string GetPriorityColor(TaskPriority priority) => priority switch
    {
        TaskPriority.Critical => "var(--rz-danger)",
        TaskPriority.High => "var(--rz-warning)",
        TaskPriority.Medium => "var(--rz-info)",
        _ => "var(--rz-primary)"
    };

    static StatusSeverity GetStatusSeverity(OccurrenceStatus status) => status switch
    {
        OccurrenceStatus.Completed => StatusSeverity.Success,
        OccurrenceStatus.InProgress => StatusSeverity.Info,
        OccurrenceStatus.Overdue => StatusSeverity.Danger,
        OccurrenceStatus.Skipped => StatusSeverity.Secondary,
        _ => StatusSeverity.Warning
    };
}
