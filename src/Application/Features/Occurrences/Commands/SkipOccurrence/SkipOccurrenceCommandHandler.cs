using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Features.Occurrences.Commands.SkipOccurrence;

public sealed class SkipOccurrenceCommandHandler(
    IApplicationDbContext dbContext,
    IPublisher publisher)
    : IRequestHandler<SkipOccurrenceCommand>
{
    public async Task Handle(SkipOccurrenceCommand request, CancellationToken cancellationToken)
    {
        var occurrence = await dbContext.TaskOccurrences
            .FirstOrDefaultAsync(o => o.Id == request.OccurrenceId && !o.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(TaskOccurrence), request.OccurrenceId);

        occurrence.Status = OccurrenceStatus.Skipped;
        occurrence.Notes = request.Notes;

        await dbContext.SaveChangesAsync(cancellationToken);

        await publisher.Publish(
            new OccurrenceSkippedEvent(occurrence.Id, occurrence.HouseholdTaskId),
            cancellationToken);
    }
}
