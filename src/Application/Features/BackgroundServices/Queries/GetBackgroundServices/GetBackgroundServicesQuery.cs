using MediatR;
using MyHomeSolution.Application.Features.BackgroundServices.Common;

namespace MyHomeSolution.Application.Features.BackgroundServices.Queries.GetBackgroundServices;

public sealed record GetBackgroundServicesQuery : IRequest<IReadOnlyList<BackgroundServiceDto>>;
