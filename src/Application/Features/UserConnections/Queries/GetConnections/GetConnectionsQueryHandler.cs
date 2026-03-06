using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Common.Models;
using MyHomeSolution.Application.Features.UserConnections.Common;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Features.UserConnections.Queries.GetConnections;

public sealed class GetConnectionsQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IIdentityService identityService)
    : IRequestHandler<GetConnectionsQuery, PaginatedList<UserConnectionDto>>
{
    public async Task<PaginatedList<UserConnectionDto>> Handle(
        GetConnectionsQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        var query = dbContext.UserConnections
            .AsNoTracking()
            .Where(uc =>
                (uc.RequesterId == userId || uc.AddresseeId == userId) &&
                !uc.IsDeleted);

        if (request.Status.HasValue)
        {
            query = query.Where(uc => uc.Status == request.Status.Value);
        }
        else
        {
            // Default: only show accepted connections
            query = query.Where(uc => uc.Status == ConnectionStatus.Accepted);
        }

        var ordered = query.OrderByDescending(uc => uc.CreatedAt);

        var paginatedConnections = await PaginatedList<UserConnectionDto>.CreateAsync(
            ordered.Select(uc => new UserConnectionDto
            {
                Id = uc.Id,
                RequesterId = uc.RequesterId,
                AddresseeId = uc.AddresseeId,
                Status = uc.Status,
                CreatedAt = uc.CreatedAt,
                RespondedAt = uc.RespondedAt,
                ConnectedUserId = uc.RequesterId == userId ? uc.AddresseeId : uc.RequesterId
            }),
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        // Enrich with user details from Identity
        var userIds = paginatedConnections.Items
            .Select(c => c.ConnectedUserId!)
            .Distinct()
            .ToList();

        var users = new Dictionary<string, (string Name, string Email)>();
        foreach (var uid in userIds)
        {
            var user = await identityService.GetUserByIdAsync(uid, cancellationToken);
            if (user is not null)
            {
                users[uid] = (user.FullName, user.Email);
            }
        }

        var enrichedItems = paginatedConnections.Items.Select(c => c with
        {
            ConnectedUserName = users.TryGetValue(c.ConnectedUserId!, out var u) ? u.Name : null,
            ConnectedUserEmail = users.TryGetValue(c.ConnectedUserId!, out var e) ? e.Email : null
        }).ToList();

        return new PaginatedList<UserConnectionDto>(
            enrichedItems,
            paginatedConnections.TotalCount,
            paginatedConnections.PageNumber,
            request.PageSize);
    }
}
