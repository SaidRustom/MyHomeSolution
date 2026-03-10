using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Features.Tasks.Commands.DeleteTask;

public sealed class DeleteTaskCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider,
    IPublisher publisher)
    : IRequestHandler<DeleteTaskCommand>
{
    public async Task Handle(DeleteTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await dbContext.HouseholdTasks
            .Include(t => t.Occurrences.Where(o => !o.IsDeleted))
            .FirstOrDefaultAsync(t => t.Id == request.Id && !t.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(HouseholdTask), request.Id);

        var now = dateTimeProvider.UtcNow;
        var today = dateTimeProvider.Today;

        // Cascade-delete future incomplete occurrences and their linked unpaid bills
        var futureOccurrences = task.Occurrences
            .Where(o => o.DueDate >= today
                        && o.Status is OccurrenceStatus.Pending or OccurrenceStatus.InProgress)
            .ToList();

        var billIdsToDelete = futureOccurrences
            .Where(o => o.BillId.HasValue)
            .Select(o => o.BillId!.Value)
            .ToList();

        var deletedBillCount = 0;
        var affectedUserIds = new HashSet<string>();

        if (billIdsToDelete.Count > 0)
        {
            var bills = await dbContext.Bills
                .Include(b => b.Splits)
                .Where(b => billIdsToDelete.Contains(b.Id) && !b.IsDeleted)
                .ToListAsync(cancellationToken);

            foreach (var bill in bills)
            {
                // Only delete bills that have no fully-paid splits
                var hasCompletedSplits = bill.Splits.Any(s => s.Status == SplitStatus.Paid);
                if (hasCompletedSplits)
                    continue;

                bill.IsDeleted = true;
                bill.DeletedAt = now;
                deletedBillCount++;

                foreach (var split in bill.Splits)
                {
                    affectedUserIds.Add(split.UserId);
                }
            }
        }

        foreach (var occurrence in futureOccurrences)
        {
            occurrence.IsDeleted = true;
            occurrence.DeletedAt = now;
        }

        task.IsDeleted = true;
        task.IsActive = false;

        // Remove current user from affected list (they initiated the delete)
        if (currentUserService.UserId is not null)
            affectedUserIds.Remove(currentUserService.UserId);

        await dbContext.SaveChangesAsync(cancellationToken);

        await publisher.Publish(
            new TaskDeletedEvent(
                task.Id,
                task.Title,
                futureOccurrences.Count,
                deletedBillCount,
                affectedUserIds.ToList()),
            cancellationToken);
    }
}
