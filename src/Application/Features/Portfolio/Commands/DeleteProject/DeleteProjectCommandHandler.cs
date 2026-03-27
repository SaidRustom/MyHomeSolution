using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Interfaces;

namespace MyHomeSolution.Application.Features.Portfolio.Commands.DeleteProject;

public sealed class DeleteProjectCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<DeleteProjectCommand>
{
    public async Task Handle(DeleteProjectCommand request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.PortfolioProjects
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Project {request.Id} not found.");

        dbContext.PortfolioProjects.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
