namespace MyHomeSolution.Application.Features.Dashboard.Common;

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
