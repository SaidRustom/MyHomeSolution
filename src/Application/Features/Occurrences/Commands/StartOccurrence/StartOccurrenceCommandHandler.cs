using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Features.Occurrences.Commands.StartOccurrence;

public sealed class StartOccurrenceCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IPublisher publisher)
    : IRequestHandler<StartOccurrenceCommand>
{
    public async Task Handle(StartOccurrenceCommand request, CancellationToken cancellationToken)
    {
        var occurrence = await dbContext.TaskOccurrences
            .Include(o => o.HouseholdTask)
            .FirstOrDefaultAsync(o => o.Id == request.OccurrenceId && !o.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(TaskOccurrence), request.OccurrenceId);

        if (occurrence.Status is not (OccurrenceStatus.Pending or OccurrenceStatus.Overdue))
        {
            var failures = new List<ValidationFailure>
            {
                new(nameof(occurrence.Status),
                    $"Occurrence can only be started from Pending or Overdue status. Current: {occurrence.Status}")
            };
            throw new ValidationException(failures);
        }

        occurrence.Status = OccurrenceStatus.InProgress;
        occurrence.Notes = request.Notes;

        await dbContext.SaveChangesAsync(cancellationToken);

        await publisher.Publish(
            new OccurrenceStartedEvent(
                occurrence.Id,
                occurrence.HouseholdTaskId,
                occurrence.HouseholdTask.Title,
                currentUserService.UserId),
            cancellationToken);
    }
}
