using MyHomeSolution.Domain.Common;

namespace MyHomeSolution.Domain.Entities;

public class PortfolioSkill : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int ProficiencyLevel { get; set; }
    public string? IconClass { get; set; }
    public int SortOrder { get; set; }
    public bool IsVisible { get; set; } = true;
}
