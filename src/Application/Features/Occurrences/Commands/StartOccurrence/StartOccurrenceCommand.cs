using MediatR;

namespace MyHomeSolution.Application.Features.Occurrences.Commands.StartOccurrence;

public sealed record StartOccurrenceCommand : IRequest
{
    public Guid OccurrenceId { get; init; }
    public string? Notes { get; init; }
}
