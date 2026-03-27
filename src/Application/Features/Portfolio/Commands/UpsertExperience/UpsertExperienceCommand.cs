using MediatR;

namespace MyHomeSolution.Application.Features.Portfolio.Commands.UpsertExperience;

public sealed record UpsertExperienceCommand : IRequest<Guid>
{
    public Guid? Id { get; init; }
    public string Company { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? LogoUrl { get; init; }
    public string? CompanyUrl { get; init; }
    public string? Technologies { get; init; }
    public DateTimeOffset StartDate { get; init; }
    public DateTimeOffset? EndDate { get; init; }
    public bool IsCurrent { get; init; }
    public int SortOrder { get; init; }
    public bool IsVisible { get; init; } = true;
}
