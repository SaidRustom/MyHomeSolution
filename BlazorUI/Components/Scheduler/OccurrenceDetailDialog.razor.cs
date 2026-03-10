using BlazorUI.Components.Bills;
using BlazorUI.Components.Common;
using BlazorUI.Models.Common;
using BlazorUI.Models.Enums;
using BlazorUI.Models.Occurrences;
using BlazorUI.Models.Tasks;
using BlazorUI.Models.Users;
using BlazorUI.Services.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Radzen;
using System.Security.Claims;

using TaskOccurrenceDto = BlazorUI.Models.Tasks.OccurrenceDto;

namespace BlazorUI.Components.Scheduler;

public partial class OccurrenceDetailDialog
{
    [Inject]
    DialogService DialogService { get; set; } = default!;

    [Inject]
    ITaskService TaskService { get; set; } = default!;

    [Inject]
    IOccurrenceService OccurrenceService { get; set; } = default!;

    [Inject]
    IBillService BillService { get; set; } = default!;

    [Inject]
    NotificationService Notifications { get; set; } = default!;

    [Inject]
    NavigationManager NavigationManager { get; set; } = default!;

    [CascadingParameter]
    private Task<AuthenticationState>? AuthState { get; set; }

    [Parameter]
    public Guid OccurrenceId { get; set; }

    [Parameter]
    public Guid TaskId { get; set; }

    TaskDetailDto? Task { get; set; }
    TaskOccurrenceDto? Occurrence { get; set; }

    bool IsLoading { get; set; }
    bool IsBusy { get; set; }
    string? Error { get; set; }

    bool _showReschedule;
    DateOnly? _rescheduleDate;
    string? _rescheduleNotes;

    bool _showNotesInput;
    string? _actionNotes;

    string? _currentUserId;
    UserDto? _currentUserDto;

    protected override async Task OnInitializedAsync()
    {
        await ResolveCurrentUserAsync();
        await LoadDataAsync();
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
        Error = null;

        var taskResult = await TaskService.GetTaskByIdAsync(TaskId);
        if (taskResult.IsSuccess)
        {
            Task = taskResult.Value;
            Occurrence = Task.Occurrences.FirstOrDefault(o => o.Id == OccurrenceId);

            if (Occurrence is null)
                Error = "Occurrence not found.";
        }
        else
        {
            Error = taskResult.Problem.ToUserMessage();
        }

        IsLoading = false;
    }

    async Task StartAsync()
    {
        if (Occurrence is null) return;
        IsBusy = true;

        var request = new StartOccurrenceRequest { Notes = _actionNotes };
        var result = await OccurrenceService.StartAsync(Occurrence.Id, request);

        if (result.IsSuccess)
        {
            Notifications.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Info,
                Summary = "Task Started",
                Detail = $"'{Task?.Title}' is now in progress.",
                Duration = 4000
            });
            DialogService.Close(true);
        }
        else
        {
            Error = result.Problem.ToUserMessage();
        }

        IsBusy = false;
    }

    async Task CompleteAsync()
    {
        if (Occurrence is null) return;
        IsBusy = true;

        var request = new CompleteOccurrenceRequest { Notes = _actionNotes };
        var result = await OccurrenceService.CompleteAsync(Occurrence.Id, request);

        if (result.IsSuccess)
        {
            Notifications.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = "Task Completed",
                Detail = $"'{Task?.Title}' has been marked as complete.",
                Duration = 4000
            });

            // Show bill payment confirmation when auto-bill is enabled
            if (Task?.AutoCreateBill == true)
            {
                await ShowBillPaymentConfirmAsync();
            }

            DialogService.Close(true);
        }
        else
        {
            Error = result.Problem.ToUserMessage();
        }

        IsBusy = false;
    }

    async Task ShowBillPaymentConfirmAsync()
    {
        // Reload the task to get the freshly created bill
        var taskResult = await TaskService.GetTaskByIdAsync(TaskId);
        if (!taskResult.IsSuccess) return;

        var freshOccurrence = taskResult.Value.Occurrences.FirstOrDefault(o => o.Id == OccurrenceId);
        if (freshOccurrence?.Bill is null) return;

        var eligibleUsers = await BuildEligibleUsersAsync(freshOccurrence.Bill.Id);

        var dialogResult = await DialogService.OpenAsync<BillPaymentConfirmDialog>(
            "Bill Payment",
            new Dictionary<string, object>
            {
                { nameof(BillPaymentConfirmDialog.BillTitle), freshOccurrence.Bill.Title },
                { nameof(BillPaymentConfirmDialog.BillAmount), freshOccurrence.Bill.Amount },
                { nameof(BillPaymentConfirmDialog.BillCurrency), freshOccurrence.Bill.Currency },
                { nameof(BillPaymentConfirmDialog.BillCategory), freshOccurrence.Bill.Category },
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
            await MarkBillAsPaidAsync(freshOccurrence.Bill.Id, payResult.PaidByUserId);
        }
    }

    async Task<List<EligibleUserOption>> BuildEligibleUsersAsync(Guid billId)
    {
        var users = new List<EligibleUserOption>();

        if (_currentUserDto is not null)
        {
            users.Add(new EligibleUserOption
            {
                UserId = _currentUserDto.Id,
                DisplayName = _currentUserDto.FullName
            });
        }

        var billResult = await BillService.GetBillByIdAsync(billId);
        if (billResult.IsSuccess)
        {
            foreach (var split in billResult.Value.Splits)
            {
                if (users.Any(u => u.UserId == split.UserId)) continue;
                users.Add(new EligibleUserOption
                {
                    UserId = split.UserId,
                    DisplayName = split.UserFullName ?? split.UserId,
                    AvatarUrl = split.UserAvatarUrl
                });
            }
        }

        return users;
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
                Detail = "The bill has been marked as paid.",
                Duration = 4000
            });
        }
    }

    async Task SkipAsync()
    {
        if (Occurrence is null) return;
        IsBusy = true;

        var zeroBalance = false;

        // If there's a linked bill, ask if balance should be zeroed
        if (Occurrence.BillId.HasValue)
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

        var request = new SkipOccurrenceRequest
        {
            Notes = _actionNotes,
            ZeroLinkedBillBalance = zeroBalance
        };
        var result = await OccurrenceService.SkipAsync(Occurrence.Id, request);

        if (result.IsSuccess)
        {
            Notifications.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Warning,
                Summary = "Task Skipped",
                Detail = $"'{Task?.Title}' occurrence has been skipped.{(zeroBalance ? " Bill balance zeroed." : "")}",
                Duration = 4000
            });
            DialogService.Close(true);
        }
        else
        {
            Error = result.Problem.ToUserMessage();
        }

        IsBusy = false;
    }

    async Task RescheduleAsync()
    {
        if (Occurrence is null || _rescheduleDate is null) return;
        IsBusy = true;

        var request = new RescheduleOccurrenceRequest
        {
            NewDueDate = _rescheduleDate.Value,
            Notes = _rescheduleNotes
        };
        var result = await OccurrenceService.RescheduleAsync(Occurrence.Id, request);

        if (result.IsSuccess)
        {
            Notifications.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Info,
                Summary = "Task Rescheduled",
                Detail = $"'{Task?.Title}' moved to {_rescheduleDate.Value:MMM dd, yyyy}.",
                Duration = 4000
            });
            DialogService.Close(true);
        }
        else
        {
            Error = result.Problem.ToUserMessage();
        }

        IsBusy = false;
    }

    void NavigateToTask()
    {
        DialogService.Close(true);
        NavigationManager.NavigateTo($"/tasks/{TaskId}");
    }

    void NavigateToBill()
    {
        if (Occurrence?.BillId is not null)
        {
            DialogService.Close(true);
            NavigationManager.NavigateTo($"/bills/{Occurrence.BillId}");
        }
    }

    static StatusSeverity GetStatusSeverity(OccurrenceStatus status) => status switch
    {
        OccurrenceStatus.Completed => StatusSeverity.Success,
        OccurrenceStatus.InProgress => StatusSeverity.Info,
        OccurrenceStatus.Overdue => StatusSeverity.Danger,
        OccurrenceStatus.Skipped => StatusSeverity.Secondary,
        _ => StatusSeverity.Warning
    };

    static string GetStatusIcon(OccurrenceStatus status) => status switch
    {
        OccurrenceStatus.Completed => "check_circle",
        OccurrenceStatus.InProgress => "play_circle",
        OccurrenceStatus.Overdue => "warning",
        OccurrenceStatus.Skipped => "skip_next",
        _ => "schedule"
    };

    static string GetPriorityColor(TaskPriority priority) => priority switch
    {
        TaskPriority.Critical => "var(--rz-danger)",
        TaskPriority.High => "var(--rz-warning)",
        TaskPriority.Medium => "var(--rz-info)",
        _ => "var(--rz-primary)"
    };

    static string GetCategoryIcon(TaskCategory category) => category switch
    {
        TaskCategory.Cleaning => "cleaning_services",
        TaskCategory.Maintenance => "build",
        TaskCategory.Cooking => "restaurant",
        TaskCategory.Gardening => "yard",
        TaskCategory.Laundry => "local_laundry_service",
        TaskCategory.Shopping => "shopping_cart",
        TaskCategory.PetCare => "pets",
        TaskCategory.ChildCare => "child_care",
        TaskCategory.Organization => "folder",
        _ => "task_alt"
    };
}
