using MyHomeSolution.Domain.Common;

namespace MyHomeSolution.Domain.Entities;

public sealed class HomepageWidget : BaseEntity
{
    public string UserId { get; set; } = default!;
    public string WidgetType { get; set; } = default!;
    public int Position { get; set; }
    public int ColumnSpan { get; set; } = 1;
    public bool IsVisible { get; set; } = true;
    public string? Settings { get; set; }
}
