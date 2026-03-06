using MediatR;
using MyHomeSolution.Application.Features.UserConnections.Common;

namespace MyHomeSolution.Application.Features.UserConnections.Queries.GetPendingRequests;

public sealed record GetPendingRequestsQuery : IRequest<IReadOnlyList<UserConnectionDto>>
{
    /// <summary>
    /// When true, returns requests sent by the current user. 
    /// When false (default), returns requests received by the current user.
    /// </summary>
    public bool Sent { get; init; }
}
