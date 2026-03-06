using MediatR;
using MyHomeSolution.Application.Common.Authorization;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Common.Behaviors;

public sealed class AuthorizationBehavior<TRequest, TResponse>(
    ICurrentUserService currentUserService,
    IShareService shareService)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not IRequireAuthorization authorizedRequest)
            return await next(cancellationToken);

        var userId = currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        var requiredPermission = request is IRequireEditAccess
            ? SharePermission.Edit
            : SharePermission.View;

        var hasAccess = await shareService.HasAccessAsync(
            authorizedRequest.ResourceType,
            authorizedRequest.ResourceId,
            userId,
            requiredPermission,
            cancellationToken);

        if (!hasAccess)
            throw new ForbiddenAccessException();

        return await next(cancellationToken);
    }
}
