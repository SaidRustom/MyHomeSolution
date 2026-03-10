using MediatR;
using MyHomeSolution.Application.Features.Dashboard.Common;

namespace MyHomeSolution.Application.Features.Dashboard.Queries.GetRequiresAttention;

public sealed record GetRequiresAttentionQuery : IRequest<RequiresAttentionDto>;
