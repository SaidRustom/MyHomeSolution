using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Constants;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Domain.Entities;

namespace MyHomeSolution.Application.Features.Shares.Commands.ShareEntity;

public sealed class ShareEntityCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IIdentityService identityService,
    IPublisher publisher)
    : IRequestHandler<ShareEntityCommand, Guid>
{
    public async Task<Guid> Handle(ShareEntityCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        await EnsureEntityExistsAndOwnedAsync(
            request.EntityType, request.EntityId, userId, cancellationToken);

        if (!await identityService.UserExistsAsync(request.SharedWithUserId, cancellationToken))
            throw new NotFoundException("User", request.SharedWithUserId);

        var existingShare = await dbContext.EntityShares
            .FirstOrDefaultAsync(s =>
                s.EntityType == request.EntityType &&
                s.EntityId == request.EntityId &&
                s.SharedWithUserId == request.SharedWithUserId &&
                !s.IsDeleted,
                cancellationToken);

        if (existingShare is not null)
        {
            existingShare.Permission = request.Permission;
            await dbContext.SaveChangesAsync(cancellationToken);
            return existingShare.Id;
        }

        var share = new EntityShare
        {
            EntityType = request.EntityType,
            EntityId = request.EntityId,
            SharedWithUserId = request.SharedWithUserId,
            Permission = request.Permission
        };

        dbContext.EntityShares.Add(share);
        await dbContext.SaveChangesAsync(cancellationToken);

        await publisher.Publish(
            new EntitySharedEvent(share.Id, share.EntityType, share.EntityId, share.SharedWithUserId, userId),
            cancellationToken);

        return share.Id;
    }

    private async Task EnsureEntityExistsAndOwnedAsync(
        string entityType, Guid entityId, string userId,
        CancellationToken cancellationToken)
    {
        var isOwner = entityType switch
        {
            EntityTypes.HouseholdTask =>
                await dbContext.HouseholdTasks.AnyAsync(
                    t => t.Id == entityId && !t.IsDeleted && t.CreatedBy == userId,
                    cancellationToken),
            EntityTypes.Bill =>
                await dbContext.Bills.AnyAsync(
                    b => b.Id == entityId && !b.IsDeleted && b.CreatedBy == userId,
                    cancellationToken),
            _ => false
        };

        if (!isOwner)
            throw new ForbiddenAccessException();
    }
}
