using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Domain.Entities;

namespace MyHomeSolution.Application.Features.Shares.Commands.RevokeShare;

public sealed class RevokeShareCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IPublisher publisher)
    : IRequestHandler<RevokeShareCommand>
{
    public async Task Handle(RevokeShareCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        var share = await dbContext.EntityShares
            .FirstOrDefaultAsync(s => s.Id == request.ShareId && !s.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(EntityShare), request.ShareId);

        if (share.CreatedBy != userId)
            throw new ForbiddenAccessException();

        var sharedWithUserId = share.SharedWithUserId;
        var entityType = share.EntityType;
        var entityId = share.EntityId;

        dbContext.EntityShares.Remove(share);
        await dbContext.SaveChangesAsync(cancellationToken);

        await publisher.Publish(
            new ShareRevokedEvent(entityType, entityId, sharedWithUserId, userId),
            cancellationToken);
    }
}
