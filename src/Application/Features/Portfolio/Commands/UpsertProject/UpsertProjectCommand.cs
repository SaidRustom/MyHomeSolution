using MediatR;

namespace MyHomeSolution.Application.Features.Portfolio.Commands.UpsertProject;

public sealed record UpsertProjectCommand : IRequest<Guid>
{
    public Guid? Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string ShortDescription { get; init; } = string.Empty;
    public string? LongDescription { get; init; }
    public string? ImageUrl { get; init; }
    public string? LiveUrl { get; init; }
    public string? GitHubUrl { get; init; }
    public string Technologies { get; init; } = string.Empty;
    public string? Category { get; init; }
    public int SortOrder { get; init; }
    public bool IsFeatured { get; init; }
    public bool IsVisible { get; init; } = true;
}
