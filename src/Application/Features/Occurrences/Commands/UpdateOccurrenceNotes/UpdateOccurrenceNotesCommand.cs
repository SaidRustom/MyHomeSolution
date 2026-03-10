using MediatR;

namespace MyHomeSolution.Application.Features.Occurrences.Commands.UpdateOccurrenceNotes;

public sealed record UpdateOccurrenceNotesCommand : IRequest
{
    public Guid OccurrenceId { get; init; }
    public string? Notes { get; init; }
}
