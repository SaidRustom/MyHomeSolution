using MediatR;

namespace MyHomeSolution.Application.Features.UserConnections.Queries.GetSharedHistory;

public sealed record GetSharedHistoryQuery(string UserId) : IRequest<SharedHistoryDto>;
