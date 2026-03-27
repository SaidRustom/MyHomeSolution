using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Domain.Entities;

namespace MyHomeSolution.Application.Features.Portfolio.Commands.UpsertProject;

public sealed class UpsertProjectCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<UpsertProjectCommand, Guid>
{
    public async Task<Guid> Handle(UpsertProjectCommand request, CancellationToken cancellationToken)
    {
        PortfolioProject entity;

        if (request.Id.HasValue)
        {
            entity = await dbContext.PortfolioProjects
                .FirstOrDefaultAsync(p => p.Id == request.Id.Value, cancellationToken)
                ?? throw new KeyNotFoundException($"Project {request.Id} not found.");
        }
        else
        {
            entity = new PortfolioProject();
            dbContext.PortfolioProjects.Add(entity);
        }

        entity.Title = request.Title;
        entity.ShortDescription = request.ShortDescription;
        entity.LongDescription = request.LongDescription;
        entity.ImageUrl = request.ImageUrl;
        entity.LiveUrl = request.LiveUrl;
        entity.GitHubUrl = request.GitHubUrl;
        entity.Technologies = request.Technologies;
        entity.Category = request.Category;
        entity.SortOrder = request.SortOrder;
        entity.IsFeatured = request.IsFeatured;
        entity.IsVisible = request.IsVisible;

        await dbContext.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}
