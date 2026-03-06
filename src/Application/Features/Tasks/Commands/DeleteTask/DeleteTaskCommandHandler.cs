using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Domain.Entities;

namespace MyHomeSolution.Application.Features.Tasks.Commands.DeleteTask;

public sealed class DeleteTaskCommandHandler(
    IApplicationDbContext dbContext,
    IPublisher publisher)
    : IRequestHandler<DeleteTaskCommand>
{
    public async Task Handle(DeleteTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await dbContext.HouseholdTasks
            .FirstOrDefaultAsync(t => t.Id == request.Id && !t.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(HouseholdTask), request.Id);

        task.IsDeleted = true;
        task.IsActive = false;

        await dbContext.SaveChangesAsync(cancellationToken);

        await publisher.Publish(new TaskDeletedEvent(task.Id), cancellationToken);
    }
}
