using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Features.UserConnections.Commands.CancelConnectionRequest;

public sealed class CancelConnectionRequestCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : IRequestHandler<CancelConnectionRequestCommand>
{
    public async Task Handle(CancelConnectionRequestCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        var connection = await dbContext.UserConnections
            .FirstOrDefaultAsync(uc => uc.Id == request.ConnectionId && !uc.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("UserConnection", request.ConnectionId);

        if (connection.RequesterId != userId)
            throw new ForbiddenAccessException();

        if (connection.Status != ConnectionStatus.Pending)
            throw new ConflictException("This connection request is no longer pending.");

        connection.Status = ConnectionStatus.Cancelled;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
