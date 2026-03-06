using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.UserConnections.Common;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Features.UserConnections.Queries.GetPendingRequests;

public sealed class GetPendingRequestsQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IIdentityService identityService)
    : IRequestHandler<GetPendingRequestsQuery, IReadOnlyList<UserConnectionDto>>
{
    public async Task<IReadOnlyList<UserConnectionDto>> Handle(
        GetPendingRequestsQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        var query = dbContext.UserConnections
            .AsNoTracking()
            .Where(uc => uc.Status == ConnectionStatus.Pending && !uc.IsDeleted);

        query = request.Sent
            ? query.Where(uc => uc.RequesterId == userId)
            : query.Where(uc => uc.AddresseeId == userId);

        var connections = await query
            .OrderByDescending(uc => uc.CreatedAt)
            .Select(uc => new UserConnectionDto
            {
                Id = uc.Id,
                RequesterId = uc.RequesterId,
                AddresseeId = uc.AddresseeId,
                Status = uc.Status,
                CreatedAt = uc.CreatedAt,
                RespondedAt = uc.RespondedAt,
                ConnectedUserId = request.Sent ? uc.AddresseeId : uc.RequesterId
            })
            .ToListAsync(cancellationToken);

        // Enrich with user details
        for (var i = 0; i < connections.Count; i++)
        {
            var user = await identityService.GetUserByIdAsync(connections[i].ConnectedUserId!, cancellationToken);
            if (user is not null)
            {
                connections[i] = connections[i] with
                {
                    ConnectedUserName = user.FullName,
                    ConnectedUserEmail = user.Email
                };
            }
        }

        return connections;
    }
}
