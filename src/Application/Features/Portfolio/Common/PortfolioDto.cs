namespace MyHomeSolution.Application.Features.Portfolio.Common;

public sealed record PortfolioDto
{
    public PortfolioProfileDto? Profile { get; init; }
    public IReadOnlyList<PortfolioProjectDto> Projects { get; init; } = [];
    public IReadOnlyList<PortfolioExperienceDto> Experiences { get; init; } = [];
    public IReadOnlyList<PortfolioSkillDto> Skills { get; init; } = [];
}

public sealed record PortfolioProfileDto
{
    public string FullName { get; init; } = string.Empty;
    public string Headline { get; init; } = string.Empty;
    public string SubHeadline { get; init; } = string.Empty;
    public string Bio { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public string? Location { get; init; }
    public string? AvatarUrl { get; init; }
    public string? ResumeUrl { get; init; }
    public string? GitHubUrl { get; init; }
    public string? LinkedInUrl { get; init; }
    public string? TwitterUrl { get; init; }
    public string? WebsiteUrl { get; init; }
}

public sealed record PortfolioProjectDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string ShortDescription { get; init; } = string.Empty;
    public string? LongDescription { get; init; }
    public string? ImageUrl { get; init; }
    public string? LiveUrl { get; init; }
    public string? GitHubUrl { get; init; }
    public string Technologies { get; init; } = string.Empty;
    public string? Category { get; init; }
    public bool IsFeatured { get; init; }
}

public sealed record PortfolioExperienceDto
{
    public Guid Id { get; init; }
    public string Company { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? LogoUrl { get; init; }
    public string? CompanyUrl { get; init; }
    public string? Technologies { get; init; }
    public DateTimeOffset StartDate { get; init; }
    public DateTimeOffset? EndDate { get; init; }
    public bool IsCurrent { get; init; }
}

public sealed record PortfolioSkillDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public int ProficiencyLevel { get; init; }
    public string? IconClass { get; init; }
}
