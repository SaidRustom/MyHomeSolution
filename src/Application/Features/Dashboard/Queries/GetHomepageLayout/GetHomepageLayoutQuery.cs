using MediatR;
using MyHomeSolution.Application.Features.Dashboard.Common;

namespace MyHomeSolution.Application.Features.Dashboard.Queries.GetHomepageLayout;

public sealed record GetHomepageLayoutQuery : IRequest<HomepageLayoutDto>;
