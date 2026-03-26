using MyHomeSolution.Domain.Common;

namespace MyHomeSolution.Domain.Entities;

public class PortfolioProject : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string? LongDescription { get; set; }
    public string? ImageUrl { get; set; }
    public string? LiveUrl { get; set; }
    public string? GitHubUrl { get; set; }
    public string Technologies { get; set; } = string.Empty;
    public string? Category { get; set; }
    public int SortOrder { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsVisible { get; set; } = true;
}
