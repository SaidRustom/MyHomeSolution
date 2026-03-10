using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Domain.Entities;

namespace MyHomeSolution.Application.Features.Occurrences.Commands.UpdateOccurrenceNotes;

public sealed class UpdateOccurrenceNotesCommandHandler(
    IApplicationDbContext dbContext)
    : IRequestHandler<UpdateOccurrenceNotesCommand>
{
    public async Task Handle(UpdateOccurrenceNotesCommand request, CancellationToken cancellationToken)
    {
        var occurrence = await dbContext.TaskOccurrences
            .FirstOrDefaultAsync(o => o.Id == request.OccurrenceId && !o.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(TaskOccurrence), request.OccurrenceId);

        occurrence.Notes = request.Notes;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
