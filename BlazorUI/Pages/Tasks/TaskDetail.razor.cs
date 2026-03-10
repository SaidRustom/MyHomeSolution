using System.Security.Claims;
using BlazorUI.Components.Bills;
using BlazorUI.Components.Common;
using BlazorUI.Components.Scheduler;
using BlazorUI.Models.Common;
using BlazorUI.Models.Enums;
using BlazorUI.Models.Occurrences;
using BlazorUI.Models.Realtime;
using BlazorUI.Models.Tasks;
using BlazorUI.Models.Users;
using BlazorUI.Services.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Radzen;

using TaskOccurrenceDto = BlazorUI.Models.Tasks.OccurrenceDto;

namespace BlazorUI.Pages.Tasks;

public partial class TaskDetail : IDisposable
{
    [Parameter]
    public Guid Id { get; set; }

    [SupplyParameterFromQuery(Name = "occurrenceId")]
    public Guid? HighlightOccurrenceId { get; set; }

    [Inject]
    ITaskService TaskService { get; set; } = default!;

    [Inject]
    IOccurrenceService OccurrenceService { get; set; } = default!;

    [Inject]
    DialogService DialogService { get; set; } = default!;

    [Inject]
    NotificationService Notifications { get; set; } = default!;

    [Inject]
    NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    INotificationHubClient NotificationHubClient { get; set; } = default!;

    [Inject]
    IBillService BillService { get; set; } = default!;

    [CascadingParameter]
    private Task<AuthenticationState> AuthState { get; set; } = default!;

    TaskDetailDto? Task { get; set; }

    bool IsLoading { get; set; }

    ApiProblemDetails? Error { get; set; }

    Guid? _busyOccurrenceId;

    string? _currentUserId;
    UserDto? _currentUserDto;

    CancellationTokenSource _cts = new();

    protected override async Task OnParametersSetAsync()
    {
        NotificationHubClient.OnUserNotification -= HandleRealtimeNotification;
        NotificationHubClient.OnUserNotification += HandleRealtimeNotification;

        await ResolveCurrentUserAsync();
        await LoadTaskAsync();
    }

    async Task ResolveCurrentUserAsync()
    {
        if (_currentUserId is not null) return;

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

    async Task LoadTaskAsync()
    {
        IsLoading = true;
        Error = null;

        var result = await TaskService.GetTaskByIdAsync(Id, _cts.Token);

        if (result.IsSuccess)
        {
            Task = result.Value;
        }
        else
        {
            Error = result.Problem;
        }

        IsLoading = false;
    }

    async Task EditTaskAsync()
    {
        if (Task is null) return;

        var model = TaskFormModel.FromDetail(Task);

        var result = await DialogService.OpenAsync<TaskFormDialog>(
            "Edit Task",
            new Dictionary<string, object>
            {
                { nameof(TaskFormDialog.Model), model },
                { nameof(TaskFormDialog.IsEdit), true }
            },
            new DialogOptions
            {
                Width = "700px",
                Height = "700px",
                Resizable = true,
                Draggable = true,
                CloseDialogOnOverlayClick = false,
                ShowClose = false
            });

        if (result is true)
        {
            await LoadTaskAsync();
        }
    }

    async Task DeleteTaskAsync()
    {
        if (Task is null) return;

        var confirmed = await DialogService.OpenAsync<ConfirmDialog>(
            "Delete Task",
            new Dictionary<string, object>
            {
                { nameof(ConfirmDialog.Message), $"Are you sure you want to delete '{Task.Title}'? This will also remove all occurrences and scheduled bills." },
                { nameof(ConfirmDialog.ConfirmText), "Delete" },
                { nameof(ConfirmDialog.ConfirmStyle), ButtonStyle.Danger },
                { nameof(ConfirmDialog.ConfirmIcon), "delete" }
            },
            new DialogOptions
            {
                Width = "450px",
                CloseDialogOnOverlayClick = false
            });

        if (confirmed is true)
        {
            var deleteResult = await TaskService.DeleteTaskAsync(Task.Id, _cts.Token);

            if (deleteResult.IsSuccess)
            {
                Notifications.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = "Task Deleted",
                    Detail = $"'{Task.Title}' has been deleted.",
                    Duration = 4000
                });
                NavigationManager.NavigateTo("/tasks");
            }
            else
            {
                Notifications.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Error",
                    Detail = deleteResult.Problem.ToUserMessage(),
                    Duration = 6000
                });
            }
        }
    }

    async Task StartOccurrenceAsync(Guid occurrenceId)
    {
        _busyOccurrenceId = occurrenceId;
        var result = await OccurrenceService.StartAsync(occurrenceId, new StartOccurrenceRequest(), _cts.Token);

        if (result.IsSuccess)
        {
            Notifications.Notify(new NotificationMessage { Severity = NotificationSeverity.Info, Summary = "Started", Duration = 3000 });
            await LoadTaskAsync();
        }

        _busyOccurrenceId = null;
    }

    async Task CompleteOccurrenceAsync(Guid occurrenceId)
    {
        _busyOccurrenceId = occurrenceId;
        var result = await OccurrenceService.CompleteAsync(occurrenceId, new CompleteOccurrenceRequest(), _cts.Token);

        if (result.IsSuccess)
        {
            Notifications.Notify(new NotificationMessage { Severity = NotificationSeverity.Success, Summary = "Completed", Duration = 3000 });
            await LoadTaskAsync();

            // Show bill payment confirmation when a bill was auto-created
            if (Task?.AutoCreateBill == true)
            {
                await ShowBillPaymentConfirmAsync(occurrenceId);
            }
        }

        _busyOccurrenceId = null;
    }

    async Task ShowBillPaymentConfirmAsync(Guid occurrenceId)
    {
        var occurrence = Task?.Occurrences.FirstOrDefault(o => o.Id == occurrenceId);
        if (occurrence?.Bill is null) return;

        // Build eligible users from the bill splits + shared users
        var eligibleUsers = await BuildEligibleUsersAsync(occurrence.Bill.Id);

        var dialogResult = await DialogService.OpenAsync<BillPaymentConfirmDialog>(
            "Bill Payment",
            new Dictionary<string, object>
            {
                { nameof(BillPaymentConfirmDialog.BillTitle), occurrence.Bill.Title },
                { nameof(BillPaymentConfirmDialog.BillAmount), occurrence.Bill.Amount },
                { nameof(BillPaymentConfirmDialog.BillCurrency), occurrence.Bill.Currency },
                { nameof(BillPaymentConfirmDialog.BillCategory), occurrence.Bill.Category },
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
            await MarkBillAsPaidAsync(occurrence.Bill.Id, payResult.PaidByUserId);
        }
    }

    async Task<List<EligibleUserOption>> BuildEligibleUsersAsync(Guid billId)
    {
        var users = new List<EligibleUserOption>();

        // Always include the current user first
        if (_currentUserDto is not null)
        {
            users.Add(new EligibleUserOption
            {
                UserId = _currentUserDto.Id,
                DisplayName = _currentUserDto.FullName
            });
        }

        // Fetch bill detail to get split participants
        var billResult = await BillService.GetBillByIdAsync(billId, _cts.Token);
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
        // Fetch the bill to get its current details, then update with the new payer
        var billResult = await BillService.GetBillByIdAsync(billId, _cts.Token);
        if (!billResult.IsSuccess) return;

        var bill = billResult.Value;
        var updateRequest = new Models.Bills.UpdateBillRequest
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

        var updateResult = await BillService.UpdateBillAsync(billId, updateRequest, _cts.Token);
        if (updateResult.IsSuccess)
        {
            // Mark all splits as paid for the payer
            foreach (var split in bill.Splits.Where(s => string.Equals(s.UserId, paidByUserId, StringComparison.OrdinalIgnoreCase) && s.Status == SplitStatus.Unpaid))
            {
                await BillService.MarkSplitAsPaidAsync(billId, split.Id, _cts.Token);
            }

            Notifications.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = "Bill Marked as Paid",
                Detail = $"The bill has been marked as paid.",
                Duration = 4000
            });

            await LoadTaskAsync();
        }
    }

    async Task SkipOccurrenceAsync(Guid occurrenceId)
    {
        _busyOccurrenceId = occurrenceId;

        var zeroBalance = false;
        var occurrence = Task?.Occurrences.FirstOrDefault(o => o.Id == occurrenceId);

        // If occurrence has a linked bill, ask about zeroing balance
        if (occurrence?.BillId.HasValue == true)
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

        var result = await OccurrenceService.SkipAsync(occurrenceId, new SkipOccurrenceRequest
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
            await LoadTaskAsync();
        }

        _busyOccurrenceId = null;
    }

    void NavigateToBill(Guid billId)
    {
        NavigationManager.NavigateTo($"/bills/{billId}");
    }

    async Task OpenNotesEditorAsync(Guid occurrenceId, string? currentNotes)
    {
        var notes = await DialogService.OpenAsync<OccurrenceNotesDialog>(
            "Edit Notes",
            new Dictionary<string, object>
            {
                { nameof(OccurrenceNotesDialog.Notes), currentNotes ?? string.Empty }
            },
            new DialogOptions
            {
                Width = "450px",
                CloseDialogOnOverlayClick = false,
                ShowClose = true
            });

        if (notes is string updatedNotes)
        {
            var result = await OccurrenceService.UpdateNotesAsync(occurrenceId, updatedNotes, _cts.Token);

            if (result.IsSuccess)
            {
                Notifications.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = "Notes Updated",
                    Duration = 3000
                });
                await LoadTaskAsync();
            }
            else
            {
                Notifications.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Error",
                    Detail = result.Problem.ToUserMessage(),
                    Duration = 5000
                });
            }
        }
    }

    void OnOccurrenceRowRender(RowRenderEventArgs<BlazorUI.Models.Tasks.OccurrenceDto> args)
    {
        if (HighlightOccurrenceId.HasValue && args.Data.Id == HighlightOccurrenceId.Value)
        {
            args.Attributes.Add("style", "background-color: var(--rz-warning-lighter); transition: background-color 2s ease;");
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

    // Analytics computed properties
    double CompletionRate =>
        Task?.Occurrences.Count > 0
            ? Task.Occurrences.Count(o => o.Status == OccurrenceStatus.Completed) * 100.0 / Task.Occurrences.Count
            : 0;

    // Financial computed properties (for auto-bill tasks)
    IReadOnlyList<OccurrenceBillBriefDto> LinkedBills =>
        Task?.Occurrences
            .Where(o => o.Bill is not null)
            .Select(o => o.Bill!)
            .ToList()
        ?? [];

    decimal TotalBilled => LinkedBills.Sum(b => b.Amount);

    decimal AveragePerOccurrence =>
        LinkedBills.Count > 0 ? TotalBilled / LinkedBills.Count : 0;

    int FullyPaidBillCount =>
        LinkedBills.Count(b => b.PaidSplits == b.TotalSplits && b.TotalSplits > 0);

    int PendingBillCount =>
        LinkedBills.Count(b => b.PaidSplits < b.TotalSplits);

    double PaymentCompletionRate =>
        LinkedBills.Count > 0
            ? FullyPaidBillCount * 100.0 / LinkedBills.Count
            : 0;

    double OnTimeRate
    {
        get
        {
            var completed = Task?.Occurrences.Where(o => o.Status == OccurrenceStatus.Completed && o.CompletedAt.HasValue).ToList();
            if (completed is null || completed.Count == 0) return 0;
            var onTime = completed.Count(o => DateOnly.FromDateTime(o.CompletedAt!.Value.LocalDateTime) <= o.DueDate);
            return onTime * 100.0 / completed.Count;
        }
    }

    int SkippedCount => Task?.Occurrences.Count(o => o.Status == OccurrenceStatus.Skipped) ?? 0;

    int OverdueAnalyticsCount => Task?.Occurrences.Count(o => o.Status == OccurrenceStatus.Overdue) ?? 0;

    List<TaskOccurrenceDto> RecentCompletions =>
        Task?.Occurrences
            .Where(o => o.Status == OccurrenceStatus.Completed && o.CompletedAt.HasValue)
            .OrderByDescending(o => o.CompletedAt)
            .Take(5)
            .ToList()
        ?? [];

    List<UserCompletionEntry> CompletionsByUser
    {
        get
        {
            if (Task?.Occurrences is null) return [];

            var completed = Task.Occurrences
                .Where(o => o.Status == OccurrenceStatus.Completed && !string.IsNullOrEmpty(o.CompletedByUserId))
                .ToList();

            if (completed.Count == 0) return [];

            return completed
                .GroupBy(o => o.CompletedByUserId!)
                .Select(g =>
                {
                    var onTime = g.Count(o => o.CompletedAt.HasValue && DateOnly.FromDateTime(o.CompletedAt.Value.LocalDateTime) <= o.DueDate);
                    var sample = g.First();
                    return new UserCompletionEntry
                    {
                        UserId = g.Key,
                        UserName = sample.CompletedByUserFullName ?? g.Key,
                        AvatarUrl = sample.CompletedByUserAvatarUrl,
                        Completed = g.Count(),
                        OnTime = onTime,
                        Late = g.Count() - onTime,
                        Percentage = completed.Count > 0 ? g.Count() * 100.0 / completed.Count : 0
                    };
                })
                .OrderByDescending(e => e.Completed)
                .ToList();
        }
    }

    public void Dispose()
    {
        NotificationHubClient.OnUserNotification -= HandleRealtimeNotification;
        _cts.Cancel();
        _cts.Dispose();
    }

    void HandleRealtimeNotification(UserPushNotification push)
    {
        var entityType = push.RelatedEntityType?.ToLowerInvariant();
        if (entityType is "householdtask" or "task" or "taskoccurrence" or "occurrence")
        {
            InvokeAsync(async () =>
            {
                await LoadTaskAsync();
                StateHasChanged();
            });
        }
    }
}

public sealed class UserCompletionEntry
{
    public required string UserId { get; init; }
    public required string UserName { get; init; }
    public string? AvatarUrl { get; init; }
    public int Completed { get; init; }
    public int OnTime { get; init; }
    public int Late { get; init; }
    public double Percentage { get; init; }
}
