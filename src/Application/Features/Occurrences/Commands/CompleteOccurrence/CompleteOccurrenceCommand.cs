using MediatR;

namespace MyHomeSolution.Application.Features.Occurrences.Commands.CompleteOccurrence;

public sealed record CompleteOccurrenceCommand : IRequest
{
    public Guid OccurrenceId { get; init; }
    public string? Notes { get; init; }
}
