using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.Shares.Common;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Features.Shares.Queries.GetEntityShares;

public sealed class GetEntitySharesQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IShareService shareService)
    : IRequestHandler<GetEntitySharesQuery, IReadOnlyList<ShareDto>>
{
    public async Task<IReadOnlyList<ShareDto>> Handle(
        GetEntitySharesQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        var hasAccess = await shareService.HasAccessAsync(
            request.EntityType, request.EntityId, userId,
            SharePermission.View, cancellationToken);

        if (!hasAccess)
            throw new ForbiddenAccessException();

        return await dbContext.EntityShares
            .AsNoTracking()
            .Where(s =>
                s.EntityType == request.EntityType &&
                s.EntityId == request.EntityId &&
                !s.IsDeleted)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new ShareDto
            {
                Id = s.Id,
                EntityId = s.EntityId,
                EntityType = s.EntityType,
                SharedWithUserId = s.SharedWithUserId,
                Permission = s.Permission,
                CreatedAt = s.CreatedAt,
                CreatedBy = s.CreatedBy
            })
            .ToListAsync(cancellationToken);
    }
}
