using MediatR;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Features.Shares.Commands.UpdateSharePermission;

public sealed record UpdateSharePermissionCommand : IRequest
{
    public Guid ShareId { get; init; }
    public SharePermission Permission { get; init; }
}
