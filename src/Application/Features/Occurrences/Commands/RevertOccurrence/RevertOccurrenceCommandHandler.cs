using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Features.Occurrences.Commands.RevertOccurrence;

public sealed class RevertOccurrenceCommandHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<RevertOccurrenceCommand>
{
    public async Task Handle(RevertOccurrenceCommand request, CancellationToken cancellationToken)
    {
        var occurrence = await dbContext.TaskOccurrences
            .FirstOrDefaultAsync(o => o.Id == request.OccurrenceId && !o.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(TaskOccurrence), request.OccurrenceId);

        // Determine the appropriate target status
        occurrence.Status = occurrence.DueDate < DateOnly.FromDateTime(dateTimeProvider.UtcNow.DateTime)
            ? OccurrenceStatus.Overdue
            : OccurrenceStatus.Pending;

        occurrence.CompletedAt = null;
        occurrence.CompletedByUserId = null;

        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            occurrence.Notes = request.Notes;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
