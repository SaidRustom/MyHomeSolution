using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.Dashboard.Common;
using MyHomeSolution.Domain.Entities;

namespace MyHomeSolution.Application.Features.Dashboard.Commands.SaveHomepageLayout;

public sealed class SaveHomepageLayoutCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : IRequestHandler<SaveHomepageLayoutCommand, HomepageLayoutDto>
{
    public async Task<HomepageLayoutDto> Handle(
        SaveHomepageLayoutCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        var existing = await dbContext.HomepageWidgets
            .Where(w => w.UserId == userId)
            .ToListAsync(cancellationToken);

        dbContext.HomepageWidgets.RemoveRange(existing);

        var widgets = request.Widgets
            .Select((w, i) => new HomepageWidget
            {
                UserId = userId,
                WidgetType = w.WidgetType,
                Position = w.Position,
                ColumnSpan = w.ColumnSpan,
                IsVisible = w.IsVisible,
                Settings = w.Settings
            })
            .ToList();

        dbContext.HomepageWidgets.AddRange(widgets);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new HomepageLayoutDto
        {
            Widgets = widgets
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
                .ToList()
        };
    }
}
