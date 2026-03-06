using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Features.UserConnections.Commands.RemoveConnection;

public sealed class RemoveConnectionCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<RemoveConnectionCommand>
{
    public async Task Handle(RemoveConnectionCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        var connection = await dbContext.UserConnections
            .FirstOrDefaultAsync(uc => uc.Id == request.ConnectionId && !uc.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("UserConnection", request.ConnectionId);

        if (connection.RequesterId != userId && connection.AddresseeId != userId)
            throw new ForbiddenAccessException();

        if (connection.Status != ConnectionStatus.Accepted)
            throw new ConflictException("Only accepted connections can be removed.");

        connection.Status = ConnectionStatus.Removed;
        connection.RespondedAt = dateTimeProvider.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
