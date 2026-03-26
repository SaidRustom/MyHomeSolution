using MediatR;
using MyHomeSolution.Application.Features.Portfolio.Common;

namespace MyHomeSolution.Application.Features.Portfolio.Queries.GetPortfolio;

public sealed record GetPortfolioQuery : IRequest<PortfolioDto>;
