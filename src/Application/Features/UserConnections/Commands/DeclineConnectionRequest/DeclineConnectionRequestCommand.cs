using MediatR;

namespace MyHomeSolution.Application.Features.UserConnections.Commands.DeclineConnectionRequest;

public sealed record DeclineConnectionRequestCommand(Guid ConnectionId) : IRequest;
