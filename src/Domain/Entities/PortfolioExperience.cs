using MyHomeSolution.Domain.Common;

namespace MyHomeSolution.Domain.Entities;

public class PortfolioExperience : BaseEntity
{
    public string Company { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string? CompanyUrl { get; set; }
    public string? Technologies { get; set; }
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
    public bool IsCurrent { get; set; }
    public int SortOrder { get; set; }
    public bool IsVisible { get; set; } = true;
}
