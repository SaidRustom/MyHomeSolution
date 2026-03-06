using MediatR;

namespace MyHomeSolution.Application.Features.Occurrences.Commands.SkipOccurrence;

public sealed record SkipOccurrenceCommand : IRequest
{
    public Guid OccurrenceId { get; init; }
    public string? Notes { get; init; }
}
