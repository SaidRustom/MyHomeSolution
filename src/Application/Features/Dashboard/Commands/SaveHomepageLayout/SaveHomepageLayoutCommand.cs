using MediatR;
using MyHomeSolution.Application.Features.Dashboard.Common;

namespace MyHomeSolution.Application.Features.Dashboard.Commands.SaveHomepageLayout;

public sealed record SaveHomepageLayoutCommand : IRequest<HomepageLayoutDto>
{
    public IReadOnlyList<SaveWidgetDto> Widgets { get; init; } = [];
}

public sealed record SaveWidgetDto
{
    public required string WidgetType { get; init; }
    public int Position { get; init; }
    public int ColumnSpan { get; init; }
    public bool IsVisible { get; init; }
    public string? Settings { get; init; }
}
