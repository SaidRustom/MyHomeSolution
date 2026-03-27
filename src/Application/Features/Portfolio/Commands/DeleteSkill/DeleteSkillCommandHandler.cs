using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Interfaces;

namespace MyHomeSolution.Application.Features.Portfolio.Commands.DeleteSkill;

public sealed class DeleteSkillCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<DeleteSkillCommand>
{
    public async Task Handle(DeleteSkillCommand request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.PortfolioSkills
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Skill {request.Id} not found.");

        dbContext.PortfolioSkills.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
