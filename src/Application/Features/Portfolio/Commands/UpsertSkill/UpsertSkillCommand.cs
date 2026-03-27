using MediatR;

namespace MyHomeSolution.Application.Features.Portfolio.Commands.UpsertSkill;

public sealed record UpsertSkillCommand : IRequest<Guid>
{
    public Guid? Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public int ProficiencyLevel { get; init; }
    public string? IconClass { get; init; }
    public int SortOrder { get; init; }
    public bool IsVisible { get; init; } = true;
}
