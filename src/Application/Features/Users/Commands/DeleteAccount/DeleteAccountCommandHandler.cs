using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyHomeSolution.Application.Common.Interfaces;

namespace MyHomeSolution.Application.Features.Users.Commands.DeleteAccount;

public sealed class DeleteAccountCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IIdentityService identityService,
    IDateTimeProvider dateTimeProvider,
    ILogger<DeleteAccountCommandHandler> logger)
    : IRequestHandler<DeleteAccountCommand, DeleteAccountResult>
{
    public async Task<DeleteAccountResult> Handle(
        DeleteAccountCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new UnauthorizedAccessException("User is not authenticated.");

        var now = dateTimeProvider.UtcNow;

        logger.LogInformation("Starting account deletion for user {UserId}", userId);

        // 1. Soft-delete all tasks created by user
        var tasks = await dbContext.HouseholdTasks
            .IgnoreQueryFilters()
            .Where(t => t.CreatedBy == userId && !t.IsDeleted)
            .Include(t => t.RecurrencePattern!)
                .ThenInclude(rp => rp.Assignees)
            .Include(t => t.Occurrences.Where(o => !o.IsDeleted))
            .ToListAsync(cancellationToken);

        foreach (var task in tasks)
        {
            foreach (var occurrence in task.Occurrences)
            {
                occurrence.IsDeleted = true;
                occurrence.DeletedAt = now;
                occurrence.DeletedBy = userId;
            }

            if (task.RecurrencePattern is not null)
            {
                dbContext.RecurrenceAssignees.RemoveRange(task.RecurrencePattern.Assignees);
                dbContext.RecurrencePatterns.Remove(task.RecurrencePattern);
            }

            task.IsDeleted = true;
            task.DeletedAt = now;
            task.DeletedBy = userId;
        }

        // 2. Unassign user from occurrences assigned to them (created by others)
        var assignedOccurrences = await dbContext.TaskOccurrences
            .IgnoreQueryFilters()
            .Where(o => o.AssignedToUserId == userId && !o.IsDeleted)
            .ToListAsync(cancellationToken);

        foreach (var occ in assignedOccurrences)
        {
            occ.AssignedToUserId = null;
        }

        // 3. Remove user from rotation assignees on other users' tasks
        var rotationAssignees = await dbContext.RecurrenceAssignees
            .Where(ra => ra.UserId == userId)
            .ToListAsync(cancellationToken);

        dbContext.RecurrenceAssignees.RemoveRange(rotationAssignees);

        // 4. Soft-delete all bills created by user
        var bills = await dbContext.Bills
            .IgnoreQueryFilters()
            .Where(b => b.CreatedBy == userId && !b.IsDeleted)
            .Include(b => b.Splits)
            .Include(b => b.Items)
            .ToListAsync(cancellationToken);

        foreach (var bill in bills)
        {
            dbContext.BillSplits.RemoveRange(bill.Splits);
            dbContext.BillItems.RemoveRange(bill.Items);
            bill.IsDeleted = true;
            bill.DeletedAt = now;
            bill.DeletedBy = userId;
        }

        // 5. Remove user's bill splits from other users' bills
        var userSplits = await dbContext.BillSplits
            .Where(bs => bs.UserId == userId)
            .ToListAsync(cancellationToken);

        dbContext.BillSplits.RemoveRange(userSplits);

        // 6. Soft-delete shopping lists created by user
        var shoppingLists = await dbContext.ShoppingLists
            .IgnoreQueryFilters()
            .Where(sl => sl.CreatedBy == userId && !sl.IsDeleted)
            .Include(sl => sl.Items)
            .ToListAsync(cancellationToken);

        foreach (var list in shoppingLists)
        {
            dbContext.ShoppingItems.RemoveRange(list.Items);
            list.IsDeleted = true;
            list.DeletedAt = now;
            list.DeletedBy = userId;
        }

        // 7. Soft-delete all entity shares involving the user
        var shares = await dbContext.EntityShares
            .IgnoreQueryFilters()
            .Where(es => es.CreatedBy == userId || es.SharedWithUserId == userId)
            .ToListAsync(cancellationToken);

        foreach (var share in shares)
        {
            share.IsDeleted = true;
            share.DeletedAt = now;
            share.DeletedBy = userId;
        }

        // 8. Soft-delete all notifications to/from user
        var notifications = await dbContext.Notifications
            .IgnoreQueryFilters()
            .Where(n => (n.ToUserId == userId || n.FromUserId == userId) && !n.IsDeleted)
            .ToListAsync(cancellationToken);

        foreach (var notification in notifications)
        {
            notification.IsDeleted = true;
            notification.DeletedAt = now;
            notification.DeletedBy = userId;
        }

        // 9. Soft-delete all user connections
        var connections = await dbContext.UserConnections
            .IgnoreQueryFilters()
            .Where(uc => (uc.RequesterId == userId || uc.AddresseeId == userId) && !uc.IsDeleted)
            .ToListAsync(cancellationToken);

        foreach (var connection in connections)
        {
            connection.IsDeleted = true;
            connection.DeletedAt = now;
            connection.DeletedBy = userId;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        // 10. Hard-delete the Identity user
        var (result, email, userName) = await identityService.DeleteUserAsync(userId, cancellationToken);

        if (!result.Succeeded)
        {
            logger.LogError(
                "Failed to delete identity for user {UserId}: {Errors}",
                userId, string.Join(", ", result.Errors));
            throw new InvalidOperationException(
                $"Failed to delete user account: {string.Join(", ", result.Errors)}");
        }

        logger.LogInformation("Account deletion completed for user {UserId}", userId);

        return new DeleteAccountResult(email, userName);
    }
}
