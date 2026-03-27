using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Domain.Entities;

namespace MyHomeSolution.Application.Features.Portfolio.Commands.UpsertExperience;

public sealed class UpsertExperienceCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<UpsertExperienceCommand, Guid>
{
    public async Task<Guid> Handle(UpsertExperienceCommand request, CancellationToken cancellationToken)
    {
        PortfolioExperience entity;

        if (request.Id.HasValue)
        {
            entity = await dbContext.PortfolioExperiences
                .FirstOrDefaultAsync(e => e.Id == request.Id.Value, cancellationToken)
                ?? throw new KeyNotFoundException($"Experience {request.Id} not found.");
        }
        else
        {
            entity = new PortfolioExperience();
            dbContext.PortfolioExperiences.Add(entity);
        }

        entity.Company = request.Company;
        entity.Role = request.Role;
        entity.Description = request.Description;
        entity.LogoUrl = request.LogoUrl;
        entity.CompanyUrl = request.CompanyUrl;
        entity.Technologies = request.Technologies;
        entity.StartDate = request.StartDate;
        entity.EndDate = request.EndDate;
        entity.IsCurrent = request.IsCurrent;
        entity.SortOrder = request.SortOrder;
        entity.IsVisible = request.IsVisible;

        await dbContext.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}
