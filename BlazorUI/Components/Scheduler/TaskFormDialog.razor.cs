using BlazorUI.Models.Common;
using BlazorUI.Models.Enums;
using BlazorUI.Models.Tasks;
using BlazorUI.Services.Contracts;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace BlazorUI.Components.Scheduler;

public partial class TaskFormDialog
{
    [Inject]
    DialogService DialogService { get; set; } = default!;

    [Inject]
    ITaskService TaskService { get; set; } = default!;

    [Inject]
    NotificationService Notifications { get; set; } = default!;

    [Parameter]
    public TaskFormModel Model { get; set; } = new();

    [Parameter]
    public bool IsEdit { get; set; }

    bool IsBusy { get; set; }

    string? ErrorMessage { get; set; }

    string SubmitText => IsEdit ? "Save Changes" : "Create Task";

    IEnumerable<TaskCategory> Categories => Enum.GetValues<TaskCategory>();
    IEnumerable<TaskPriority> Priorities => Enum.GetValues<TaskPriority>();
    IEnumerable<RecurrenceType> RecurrenceTypes => Enum.GetValues<RecurrenceType>();
    IEnumerable<BillCategory> BillCategories => Enum.GetValues<BillCategory>();

    async Task OnSubmitAsync()
    {
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            if (IsEdit)
            {
                var request = Model.ToUpdateRequest();
                var result = await TaskService.UpdateTaskAsync(Model.Id ?? Guid.Empty, request);

                if (result.IsSuccess)
                {
                    Notifications.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Success,
                        Summary = "Task Updated",
                        Detail = $"'{Model.Title}' has been updated successfully.",
                        Duration = 4000
                    });
                    DialogService.Close(true);
                }
                else
                {
                    ErrorMessage = result.Problem.ToUserMessage();
                }
            }
            else
            {
                var request = Model.ToCreateRequest();
                var result = await TaskService.CreateTaskAsync(request);

                if (result.IsSuccess)
                {
                    Notifications.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Success,
                        Summary = "Task Created",
                        Detail = $"'{Model.Title}' has been created successfully.",
                        Duration = 4000
                    });
                    DialogService.Close(true);
                }
                else
                {
                    ErrorMessage = result.Problem.ToUserMessage();
                }
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"An unexpected error occurred: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    void Cancel() => DialogService.Close(null);
}

public sealed class TaskFormModel
{
    public Guid? Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskCategory Category { get; set; }
    public TaskPriority Priority { get; set; }
    public int? EstimatedDurationMinutes { get; set; }
    public bool IsRecurring { get; set; }
    public bool IsActive { get; set; } = true;
    public DateOnly? DueDate { get; set; }
    public string? AssignedToUserId { get; set; }
    public RecurrenceType? RecurrenceType { get; set; }
    public int? Interval { get; set; }
    public DateOnly? RecurrenceStartDate { get; set; }
    public DateOnly? RecurrenceEndDate { get; set; }
    public IEnumerable<string>? AssigneeUserIds { get; set; }
    public bool AutoCreateBill { get; set; }
    public decimal? DefaultBillAmount { get; set; }
    public string? DefaultBillCurrency { get; set; } = "$";
    public BillCategory? DefaultBillCategory { get; set; }
    public string? DefaultBillTitle { get; set; }

    public CreateTaskRequest ToCreateRequest() => new()
    {
        Title = Title,
        Description = Description,
        Priority = Priority,
        Category = Category,
        EstimatedDurationMinutes = EstimatedDurationMinutes,
        IsRecurring = IsRecurring,
        DueDate = DueDate,
        AssignedToUserId = AssignedToUserId,
        RecurrenceType = IsRecurring ? RecurrenceType : null,
        Interval = IsRecurring ? Interval : null,
        RecurrenceStartDate = IsRecurring ? RecurrenceStartDate : null,
        RecurrenceEndDate = IsRecurring ? RecurrenceEndDate : null,
        AssigneeUserIds = IsRecurring ? AssigneeUserIds?.ToList() : null,
        AutoCreateBill = AutoCreateBill,
        DefaultBillAmount = AutoCreateBill ? DefaultBillAmount : null,
        DefaultBillCurrency = AutoCreateBill ? DefaultBillCurrency : null,
        DefaultBillCategory = AutoCreateBill ? DefaultBillCategory : null,
        DefaultBillTitle = AutoCreateBill ? DefaultBillTitle : null
    };

    public UpdateTaskRequest ToUpdateRequest() => new()
    {
        Id = Id ?? Guid.Empty,
        Title = Title,
        Description = Description,
        Priority = Priority,
        Category = Category,
        EstimatedDurationMinutes = EstimatedDurationMinutes,
        IsRecurring = IsRecurring,
        IsActive = IsActive,
        DueDate = DueDate,
        AssignedToUserId = AssignedToUserId,
        RecurrenceType = IsRecurring ? RecurrenceType : null,
        Interval = IsRecurring ? Interval : null,
        RecurrenceStartDate = IsRecurring ? RecurrenceStartDate : null,
        RecurrenceEndDate = IsRecurring ? RecurrenceEndDate : null,
        AssigneeUserIds = IsRecurring ? AssigneeUserIds?.ToList() : null,
        AutoCreateBill = AutoCreateBill,
        DefaultBillAmount = AutoCreateBill ? DefaultBillAmount : null,
        DefaultBillCurrency = AutoCreateBill ? DefaultBillCurrency : null,
        DefaultBillCategory = AutoCreateBill ? DefaultBillCategory : null,
        DefaultBillTitle = AutoCreateBill ? DefaultBillTitle : null
    };

    public static TaskFormModel FromDetail(TaskDetailDto detail) => new()
    {
        Id = detail.Id,
        Title = detail.Title,
        Description = detail.Description,
        Priority = detail.Priority,
        Category = detail.Category,
        EstimatedDurationMinutes = detail.EstimatedDurationMinutes,
        IsRecurring = detail.IsRecurring,
        IsActive = detail.IsActive,
        DueDate = detail.DueDate,
        AssignedToUserId = detail.AssignedToUserId,
        RecurrenceType = detail.RecurrencePattern?.Type,
        Interval = detail.RecurrencePattern?.Interval,
        RecurrenceStartDate = detail.RecurrencePattern?.StartDate,
        RecurrenceEndDate = detail.RecurrencePattern?.EndDate,
        AssigneeUserIds = detail.RecurrencePattern?.AssigneeUserIds,
        AutoCreateBill = detail.AutoCreateBill,
        DefaultBillAmount = detail.DefaultBillAmount,
        DefaultBillCurrency = detail.DefaultBillCurrency,
        DefaultBillCategory = detail.DefaultBillCategory,
        DefaultBillTitle = detail.DefaultBillTitle
    };
}
