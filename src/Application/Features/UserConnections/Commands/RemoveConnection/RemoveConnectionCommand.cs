using MediatR;

namespace MyHomeSolution.Application.Features.UserConnections.Commands.RemoveConnection;

public sealed record RemoveConnectionCommand(Guid ConnectionId) : IRequest;
