using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Features.Occurrences.Commands.RescheduleOccurrence;

public sealed class RescheduleOccurrenceCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IPublisher publisher)
    : IRequestHandler<RescheduleOccurrenceCommand>
{
    public async Task Handle(RescheduleOccurrenceCommand request, CancellationToken cancellationToken)
    {
        var occurrence = await dbContext.TaskOccurrences
            .Include(o => o.HouseholdTask)
            .FirstOrDefaultAsync(o => o.Id == request.OccurrenceId && !o.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(TaskOccurrence), request.OccurrenceId);

        if (occurrence.Status is OccurrenceStatus.Completed or OccurrenceStatus.Skipped)
        {
            var failures = new List<ValidationFailure>
            {
                new(nameof(occurrence.Status),
                    $"Cannot reschedule a {occurrence.Status} occurrence.")
            };
            throw new ValidationException(failures);
        }
        var previousDate = occurrence.DueDate;
        occurrence.DueDate = request.NewDueDate;
        occurrence.Notes = request.Notes ?? occurrence.Notes;

        if (occurrence.Status == OccurrenceStatus.Overdue && request.NewDueDate >= DateOnly.FromDateTime(DateTime.UtcNow))
            occurrence.Status = OccurrenceStatus.Pending;

        await dbContext.SaveChangesAsync(cancellationToken);

        await publisher.Publish(
            new OccurrenceRescheduledEvent(
                occurrence.Id,
                occurrence.HouseholdTaskId,
                occurrence.HouseholdTask.Title,
                previousDate,
                request.NewDueDate,
                currentUserService.UserId),
            cancellationToken);
    }
}
