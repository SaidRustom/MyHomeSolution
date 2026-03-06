using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Domain.Entities;

namespace MyHomeSolution.Application.Features.Tasks.Commands.UpdateTask;

public sealed class UpdateTaskCommandHandler(
    IApplicationDbContext dbContext,
    IPublisher publisher)
    : IRequestHandler<UpdateTaskCommand>
{
    public async Task Handle(UpdateTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await dbContext.HouseholdTasks
            .FirstOrDefaultAsync(t => t.Id == request.Id && !t.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(HouseholdTask), request.Id);

        task.Title = request.Title;
        task.Description = request.Description;
        task.Priority = request.Priority;
        task.Category = request.Category;
        task.EstimatedDurationMinutes = request.EstimatedDurationMinutes;
        task.IsActive = request.IsActive;
        task.DueDate = request.DueDate;
        task.AssignedToUserId = request.AssignedToUserId;

        await dbContext.SaveChangesAsync(cancellationToken);

        await publisher.Publish(new TaskUpdatedEvent(task.Id, task.Title), cancellationToken);
    }
}
