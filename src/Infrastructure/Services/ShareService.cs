using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Constants;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Infrastructure.Services;

public sealed class ShareService(IApplicationDbContext dbContext) : IShareService
{
    public async Task<bool> HasAccessAsync(
        string entityType, Guid entityId, string userId, SharePermission requiredPermission,
        CancellationToken cancellationToken = default)
    {
        if (await IsOwnerAsync(entityType, entityId, userId, cancellationToken))
            return true;

        return await HasSharePermissionAsync(
            entityType, entityId, userId, requiredPermission, cancellationToken);
    }

    private async Task<bool> IsOwnerAsync(
        string entityType, Guid entityId, string userId,
        CancellationToken cancellationToken)
    {
        return entityType switch
        {
            EntityTypes.HouseholdTask =>
                await dbContext.HouseholdTasks.AnyAsync(
                    t => t.Id == entityId && !t.IsDeleted && t.CreatedBy == userId,
                    cancellationToken),
            EntityTypes.Bill =>
                await dbContext.Bills.AnyAsync(
                    b => b.Id == entityId && !b.IsDeleted && b.CreatedBy == userId,
                    cancellationToken),
            EntityTypes.ShoppingList =>
                await dbContext.ShoppingLists.AnyAsync(
                    sl => sl.Id == entityId && !sl.IsDeleted && sl.CreatedBy == userId,
                    cancellationToken),
            _ => false
        };
    }

    private async Task<bool> HasSharePermissionAsync(
        string entityType, Guid entityId, string userId, SharePermission requiredPermission,
        CancellationToken cancellationToken)
    {
        return await dbContext.EntityShares.AnyAsync(s =>
            s.EntityType == entityType &&
            s.EntityId == entityId &&
            s.SharedWithUserId == userId &&
            !s.IsDeleted &&
            s.Permission >= requiredPermission,
            cancellationToken);
    }
}
