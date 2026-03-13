using MediatR;

namespace MyHomeSolution.Application.Features.Occurrences.Commands.RevertOccurrence;

public sealed record RevertOccurrenceCommand : IRequest
{
    public Guid OccurrenceId { get; init; }
    public string? Notes { get; init; }
}
