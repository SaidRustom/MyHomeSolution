using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Domain.Entities;

namespace MyHomeSolution.Application.Features.Shares.Commands.UpdateSharePermission;

public sealed class UpdateSharePermissionCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : IRequestHandler<UpdateSharePermissionCommand>
{
    public async Task Handle(
        UpdateSharePermissionCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        var share = await dbContext.EntityShares
            .FirstOrDefaultAsync(s => s.Id == request.ShareId && !s.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(EntityShare), request.ShareId);

        if (share.CreatedBy != userId)
            throw new ForbiddenAccessException();

        share.Permission = request.Permission;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
