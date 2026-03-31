using BlazorUI.Models.Bills;
using BlazorUI.Models.Budgets;
using BlazorUI.Models.ShoppingLists;

namespace BlazorUI.Models.SharedHistory;

public sealed record SharedHistoryDto
{
    public required string UserId { get; init; }
    public required string UserFullName { get; init; }
    public string? UserAvatarUrl { get; init; }

    public DateTimeOffset? ConnectedSince { get; init; }

    public int SharedBillCount { get; init; }
    public int SharedBudgetCount { get; init; }
    public int SharedTaskCount { get; init; }
    public int SharedTaskOccurrenceCount { get; init; }
    public int SharedShoppingListCount { get; init; }

    public IReadOnlyCollection<BillBriefDto> SharedBills { get; init; } = [];
    public IReadOnlyCollection<BudgetBriefDto> SharedBudgets { get; init; } = [];
    public IReadOnlyCollection<SharedTaskBriefDto> SharedTasks { get; init; } = [];
    public IReadOnlyCollection<ShoppingListBriefDto> SharedShoppingLists { get; init; } = [];
}

public sealed record SharedTaskBriefDto
{
    public Guid Id { get; init; }
    public required string Title { get; init; }
    public string? Category { get; init; }
    public string? Priority { get; init; }
    public bool IsRecurring { get; init; }
    public bool IsActive { get; init; }
    public DateOnly? NextDueDate { get; init; }
    public int OccurrenceCount { get; init; }
}
