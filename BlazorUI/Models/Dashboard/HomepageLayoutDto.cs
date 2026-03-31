namespace BlazorUI.Models.Dashboard;

public sealed record HomepageLayoutDto
{
    public IReadOnlyList<HomepageWidgetDto> Widgets { get; init; } = [];
}

public sealed record HomepageWidgetDto
{
    public Guid Id { get; init; }
    public required string WidgetType { get; init; }
    public int Position { get; init; }
    public int ColumnSpan { get; init; }
    public bool IsVisible { get; init; }
    public string? Settings { get; init; }
}

public sealed record SaveHomepageLayoutRequest
{
    public IReadOnlyList<SaveWidgetRequest> Widgets { get; init; } = [];
}

public sealed record SaveWidgetRequest
{
    public required string WidgetType { get; init; }
    public int Position { get; init; }
    public int ColumnSpan { get; init; }
    public bool IsVisible { get; init; }
    public string? Settings { get; init; }
}

/// <summary>
/// Registry of all available homepage widget types.
/// Add new entries here to extend the component pool.
/// </summary>
public static class WidgetTypes
{
    public const string RequiresAttention = "RequiresAttention";
    public const string TodayTasks = "TodayTasks";
    public const string BudgetOverview = "BudgetOverview";
    public const string RecentBills = "RecentBills";
    public const string QuickAccess = "QuickAccess";

    public static readonly IReadOnlyList<WidgetDefinition> All =
    [
        new(RequiresAttention, "Requires Attention", "notifications_active", "Items needing your action", 2),
        new(TodayTasks, "Today's Tasks", "task_alt", "Your tasks and occurrences for today", 1),
        new(BudgetOverview, "Budget Overview", "account_balance", "Budget status at a glance", 1),
        new(RecentBills, "Recent Bills", "receipt_long", "Bills from the last 30 days", 1),
        new(QuickAccess, "Quick Access", "apps", "Fast navigation to your entities", 1),
    ];

    public static WidgetDefinition? Get(string widgetType)
        => All.FirstOrDefault(w => w.Type == widgetType);
}

public sealed record WidgetDefinition(
    string Type,
    string DisplayName,
    string Icon,
    string Description,
    int DefaultColumnSpan);
