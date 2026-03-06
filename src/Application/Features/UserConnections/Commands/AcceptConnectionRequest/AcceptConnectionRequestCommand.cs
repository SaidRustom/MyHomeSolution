using MediatR;

namespace MyHomeSolution.Application.Features.UserConnections.Commands.AcceptConnectionRequest;

public sealed record AcceptConnectionRequestCommand(Guid ConnectionId) : IRequest;
