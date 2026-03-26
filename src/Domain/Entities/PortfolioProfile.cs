using MyHomeSolution.Domain.Common;

namespace MyHomeSolution.Domain.Entities;

public class PortfolioProfile : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string Headline { get; set; } = string.Empty;
    public string SubHeadline { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Location { get; set; }
    public string? AvatarUrl { get; set; }
    public string? ResumeUrl { get; set; }
    public string? GitHubUrl { get; set; }
    public string? LinkedInUrl { get; set; }
    public string? TwitterUrl { get; set; }
    public string? WebsiteUrl { get; set; }
    public bool IsActive { get; set; } = true;
}
