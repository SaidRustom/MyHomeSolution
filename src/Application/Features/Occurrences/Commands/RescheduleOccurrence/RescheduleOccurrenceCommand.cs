using MediatR;

namespace MyHomeSolution.Application.Features.Occurrences.Commands.RescheduleOccurrence;

public sealed record RescheduleOccurrenceCommand : IRequest
{
    public Guid OccurrenceId { get; init; }
    public DateOnly NewDueDate { get; init; }
    public string? Notes { get; init; }
}
