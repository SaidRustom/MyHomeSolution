using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.BackgroundServices.Common;

namespace MyHomeSolution.Application.Features.BackgroundServices.Queries.GetBackgroundServices;

public sealed class GetBackgroundServicesQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetBackgroundServicesQuery, IReadOnlyList<BackgroundServiceDto>>
{
    public async Task<IReadOnlyList<BackgroundServiceDto>> Handle(
        GetBackgroundServicesQuery request, CancellationToken cancellationToken)
    {
        var services = await dbContext.BackgroundServiceDefinitions
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .Select(s => new BackgroundServiceDto
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                QualifiedTypeName = s.QualifiedTypeName,
                IsEnabled = s.IsEnabled,
                RegisteredAt = s.RegisteredAt,
                LatestLog = s.Logs
                    .OrderByDescending(l => l.StartedAt)
                    .Select(l => new BackgroundServiceLogBriefDto
                    {
                        Id = l.Id,
                        BackgroundServiceId = l.BackgroundServiceId,
                        StartedAt = l.StartedAt,
                        CompletedAt = l.CompletedAt,
                        Status = l.Status.ToString(),
                        ResultMessage = l.ResultMessage,
                        ExceptionLogId = l.ExceptionLogId
                    })
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        return services;
    }
}
