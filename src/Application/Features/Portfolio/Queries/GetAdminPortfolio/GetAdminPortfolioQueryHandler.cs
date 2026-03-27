using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.Portfolio.Common;

namespace MyHomeSolution.Application.Features.Portfolio.Queries.GetAdminPortfolio;

public sealed class GetAdminPortfolioQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetAdminPortfolioQuery, AdminPortfolioDto>
{
    public async Task<AdminPortfolioDto> Handle(GetAdminPortfolioQuery request, CancellationToken cancellationToken)
    {
        var profile = await dbContext.PortfolioProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        var projects = await dbContext.PortfolioProjects
            .AsNoTracking()
            .OrderBy(p => p.SortOrder)
            .ToListAsync(cancellationToken);

        var experiences = await dbContext.PortfolioExperiences
            .AsNoTracking()
            .OrderBy(e => e.SortOrder)
            .ToListAsync(cancellationToken);

        var skills = await dbContext.PortfolioSkills
            .AsNoTracking()
            .OrderBy(s => s.Category)
            .ThenBy(s => s.SortOrder)
            .ToListAsync(cancellationToken);

        return new AdminPortfolioDto
        {
            Profile = profile is null ? null : new AdminPortfolioProfileDto
            {
                Id = profile.Id,
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
                WebsiteUrl = profile.WebsiteUrl,
                IsActive = profile.IsActive
            },
            Projects = projects.Select(p => new AdminPortfolioProjectDto
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
                SortOrder = p.SortOrder,
                IsFeatured = p.IsFeatured,
                IsVisible = p.IsVisible
            }).ToList(),
            Experiences = experiences.Select(e => new AdminPortfolioExperienceDto
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
                IsCurrent = e.IsCurrent,
                SortOrder = e.SortOrder,
                IsVisible = e.IsVisible
            }).ToList(),
            Skills = skills.Select(s => new AdminPortfolioSkillDto
            {
                Id = s.Id,
                Name = s.Name,
                Category = s.Category,
                ProficiencyLevel = s.ProficiencyLevel,
                IconClass = s.IconClass,
                SortOrder = s.SortOrder,
                IsVisible = s.IsVisible
            }).ToList()
        };
    }
}
