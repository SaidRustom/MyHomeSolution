using MediatR;

namespace MyHomeSolution.Application.Features.Shares.Commands.RevokeShare;

public sealed record RevokeShareCommand(Guid ShareId) : IRequest;
