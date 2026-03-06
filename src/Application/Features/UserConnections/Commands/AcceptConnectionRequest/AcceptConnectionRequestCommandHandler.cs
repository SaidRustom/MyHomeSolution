using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Features.UserConnections.Commands.AcceptConnectionRequest;

public sealed class AcceptConnectionRequestCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider,
    IPublisher publisher)
    : IRequestHandler<AcceptConnectionRequestCommand>
{
    public async Task Handle(AcceptConnectionRequestCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        var connection = await dbContext.UserConnections
            .FirstOrDefaultAsync(uc => uc.Id == request.ConnectionId && !uc.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("UserConnection", request.ConnectionId);

        if (connection.AddresseeId != userId)
            throw new ForbiddenAccessException();

        if (connection.Status != ConnectionStatus.Pending)
            throw new ConflictException("This connection request is no longer pending.");

        connection.Status = ConnectionStatus.Accepted;
        connection.RespondedAt = dateTimeProvider.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        await publisher.Publish(
            new ConnectionRequestAcceptedEvent(connection.Id, connection.RequesterId, userId),
            cancellationToken);
    }
}
