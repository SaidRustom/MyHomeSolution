using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.Portfolio.Common;

namespace MyHomeSolution.Application.Features.Portfolio.Queries.GetPortfolio;

public sealed class GetPortfolioQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetPortfolioQuery, PortfolioDto>
{
    public async Task<PortfolioDto> Handle(GetPortfolioQuery request, CancellationToken cancellationToken)
    {
        var profile = await dbContext.PortfolioProfiles
            .AsNoTracking()
            .Where(p => p.IsActive)
            .FirstOrDefaultAsync(cancellationToken);

        var projects = await dbContext.PortfolioProjects
            .AsNoTracking()
            .Where(p => p.IsVisible)
            .OrderBy(p => p.SortOrder)
            .ToListAsync(cancellationToken);

        var experiences = await dbContext.PortfolioExperiences
            .AsNoTracking()
            .Where(e => e.IsVisible)
            .OrderBy(e => e.SortOrder)
            .ToListAsync(cancellationToken);

        var skills = await dbContext.PortfolioSkills
            .AsNoTracking()
            .Where(s => s.IsVisible)
            .OrderBy(s => s.Category)
            .ThenBy(s => s.SortOrder)
            .ToListAsync(cancellationToken);

        return new PortfolioDto
        {
            Profile = profile is null ? null : new PortfolioProfileDto
            {
                FullName = profile.FullName,
                Headline = profile.Headline,
                SubHeadline = profile.SubHeadline,
                Bio = profile.Bio,
                Email = profile.Email,
                Phone = profile.Phone,
                Location = profile.Location,
                AvatarUrl = profile.AvatarUrl,
                ResumeUrl = profile.ResumeUrl,
                GitHubUrl = profile.GitHubUrl,
                LinkedInUrl = profile.LinkedInUrl,
                TwitterUrl = profile.TwitterUrl,
                WebsiteUrl = profile.WebsiteUrl
            },
            Projects = projects.Select(p => new PortfolioProjectDto
            {
                Id = p.Id,
                Title = p.Title,
                ShortDescription = p.ShortDescription,
                LongDescription = p.LongDescription,
                ImageUrl = p.ImageUrl,
                LiveUrl = p.LiveUrl,
                GitHubUrl = p.GitHubUrl,
                Technologies = p.Technologies,
                Category = p.Category,
                IsFeatured = p.IsFeatured
            }).ToList(),
            Experiences = experiences.Select(e => new PortfolioExperienceDto
            {
                Id = e.Id,
                Company = e.Company,
                Role = e.Role,
                Description = e.Description,
                LogoUrl = e.LogoUrl,
                CompanyUrl = e.CompanyUrl,
                Technologies = e.Technologies,
                StartDate = e.StartDate,
                EndDate = e.EndDate,
                IsCurrent = e.IsCurrent
            }).ToList(),
            Skills = skills.Select(s => new PortfolioSkillDto
            {
                Id = s.Id,
                Name = s.Name,
                Category = s.Category,
                ProficiencyLevel = s.ProficiencyLevel,
                IconClass = s.IconClass
            }).ToList()
        };
    }
}
