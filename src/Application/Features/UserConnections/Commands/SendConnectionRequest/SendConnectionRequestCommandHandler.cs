using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Features.UserConnections.Commands.SendConnectionRequest;

public sealed class SendConnectionRequestCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IIdentityService identityService,
    IPublisher publisher)
    : IRequestHandler<SendConnectionRequestCommand, Guid>
{
    public async Task<Guid> Handle(SendConnectionRequestCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        if (userId == request.AddresseeId)
            throw new ConflictException("You cannot send a connection request to yourself.");

        if (!await identityService.UserExistsAsync(request.AddresseeId, cancellationToken))
            throw new NotFoundException("User", request.AddresseeId);

        // Check for existing connection in either direction
        var existing = await dbContext.UserConnections
            .FirstOrDefaultAsync(uc =>
                ((uc.RequesterId == userId && uc.AddresseeId == request.AddresseeId) ||
                 (uc.RequesterId == request.AddresseeId && uc.AddresseeId == userId)) &&
                !uc.IsDeleted,
                cancellationToken);

        if (existing is not null)
        {
            if (existing.Status == ConnectionStatus.Accepted)
                throw new ConflictException("You are already connected with this user.");

            if (existing.Status == ConnectionStatus.Pending)
                throw new ConflictException("A pending connection request already exists.");

            // Re-send if previously declined/cancelled/removed
            existing.RequesterId = userId;
            existing.AddresseeId = request.AddresseeId;
            existing.Status = ConnectionStatus.Pending;
            existing.RespondedAt = null;

            await dbContext.SaveChangesAsync(cancellationToken);

            await publisher.Publish(
                new ConnectionRequestSentEvent(existing.Id, userId, request.AddresseeId),
                cancellationToken);

            return existing.Id;
        }

        var connection = new UserConnection
        {
            RequesterId = userId,
            AddresseeId = request.AddresseeId,
            Status = ConnectionStatus.Pending
        };

        dbContext.UserConnections.Add(connection);
        await dbContext.SaveChangesAsync(cancellationToken);

        await publisher.Publish(
            new ConnectionRequestSentEvent(connection.Id, userId, request.AddresseeId),
            cancellationToken);

        return connection.Id;
    }
}
