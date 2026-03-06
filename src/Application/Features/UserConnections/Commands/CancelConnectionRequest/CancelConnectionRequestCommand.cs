using MediatR;

namespace MyHomeSolution.Application.Features.UserConnections.Commands.CancelConnectionRequest;

public sealed record CancelConnectionRequestCommand(Guid ConnectionId) : IRequest;
