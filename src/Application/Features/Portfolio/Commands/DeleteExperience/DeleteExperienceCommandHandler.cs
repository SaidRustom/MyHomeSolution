using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Interfaces;

namespace MyHomeSolution.Application.Features.Portfolio.Commands.DeleteExperience;

public sealed class DeleteExperienceCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<DeleteExperienceCommand>
{
    public async Task Handle(DeleteExperienceCommand request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.PortfolioExperiences
            .FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Experience {request.Id} not found.");

        dbContext.PortfolioExperiences.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
