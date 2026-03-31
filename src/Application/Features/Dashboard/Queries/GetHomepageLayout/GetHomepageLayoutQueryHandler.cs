using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.Dashboard.Common;

namespace MyHomeSolution.Application.Features.Dashboard.Queries.GetHomepageLayout;

public sealed class GetHomepageLayoutQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : IRequestHandler<GetHomepageLayoutQuery, HomepageLayoutDto>
{
    private static readonly IReadOnlyList<HomepageWidgetDto> DefaultWidgets =
    [
        new() { Id = Guid.Empty, WidgetType = "RequiresAttention", Position = 0, ColumnSpan = 2, IsVisible = true },
        new() { Id = Guid.Empty, WidgetType = "TodayTasks", Position = 1, ColumnSpan = 1, IsVisible = true },
        new() { Id = Guid.Empty, WidgetType = "BudgetOverview", Position = 2, ColumnSpan = 1, IsVisible = true },
        new() { Id = Guid.Empty, WidgetType = "RecentBills", Position = 3, ColumnSpan = 1, IsVisible = true },
        new() { Id = Guid.Empty, WidgetType = "QuickAccess", Position = 4, ColumnSpan = 1, IsVisible = true },
    ];

    public async Task<HomepageLayoutDto> Handle(
        GetHomepageLayoutQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        var widgets = await dbContext.HomepageWidgets
            .AsNoTracking()
            .Where(w => w.UserId == userId)
            .OrderBy(w => w.Position)
            .Select(w => new HomepageWidgetDto
            {
                Id = w.Id,
                WidgetType = w.WidgetType,
                Position = w.Position,
                ColumnSpan = w.ColumnSpan,
                IsVisible = w.IsVisible,
                Settings = w.Settings
            })
            .ToListAsync(cancellationToken);

        return new HomepageLayoutDto
        {
            Widgets = widgets.Count > 0 ? widgets : DefaultWidgets
        };
    }
}
