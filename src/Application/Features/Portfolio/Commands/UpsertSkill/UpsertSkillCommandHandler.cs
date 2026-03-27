using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Domain.Entities;

namespace MyHomeSolution.Application.Features.Portfolio.Commands.UpsertSkill;

public sealed class UpsertSkillCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<UpsertSkillCommand, Guid>
{
    public async Task<Guid> Handle(UpsertSkillCommand request, CancellationToken cancellationToken)
    {
        PortfolioSkill entity;

        if (request.Id.HasValue)
        {
            entity = await dbContext.PortfolioSkills
                .FirstOrDefaultAsync(s => s.Id == request.Id.Value, cancellationToken)
                ?? throw new KeyNotFoundException($"Skill {request.Id} not found.");
        }
        else
        {
            entity = new PortfolioSkill();
            dbContext.PortfolioSkills.Add(entity);
        }

        entity.Name = request.Name;
        entity.Category = request.Category;
        entity.ProficiencyLevel = request.ProficiencyLevel;
        entity.IconClass = request.IconClass;
        entity.SortOrder = request.SortOrder;
        entity.IsVisible = request.IsVisible;

        await dbContext.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}
