using MediatR;

namespace MyHomeSolution.Application.Features.UserConnections.Commands.SendConnectionRequest;

public sealed record SendConnectionRequestCommand : IRequest<Guid>
{
    public required string AddresseeId { get; init; }
}
