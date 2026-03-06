using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.Users.Common;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Features.UserConnections.Queries.SearchConnectedUsers;

public sealed class SearchConnectedUsersQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IIdentityService identityService)
    : IRequestHandler<SearchConnectedUsersQuery, IReadOnlyList<UserDto>>
{
    public async Task<IReadOnlyList<UserDto>> Handle(
        SearchConnectedUsersQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        // Get accepted connection user ids
        var connectedUserIds = await dbContext.UserConnections
            .AsNoTracking()
            .Where(uc =>
                (uc.RequesterId == userId || uc.AddresseeId == userId) &&
                uc.Status == ConnectionStatus.Accepted &&
                !uc.IsDeleted)
            .Select(uc => uc.RequesterId == userId ? uc.AddresseeId : uc.RequesterId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (connectedUserIds.Count == 0)
            return [];

        // Fetch user details and filter by search term
        var results = new List<UserDto>();

        foreach (var connectedUserId in connectedUserIds)
        {
            var user = await identityService.GetUserByIdAsync(connectedUserId, cancellationToken);
            if (user is null || !user.IsActive)
                continue;

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.Trim();
                if (!user.FullName.Contains(term, StringComparison.OrdinalIgnoreCase) &&
                    !user.Email.Contains(term, StringComparison.OrdinalIgnoreCase))
                    continue;
            }

            results.Add(new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = user.FullName,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt
            });

            if (results.Count >= request.MaxResults)
                break;
        }

        return results;
    }
}
