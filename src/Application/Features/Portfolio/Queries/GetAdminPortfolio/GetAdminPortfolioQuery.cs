using MediatR;
using MyHomeSolution.Application.Features.Portfolio.Common;

namespace MyHomeSolution.Application.Features.Portfolio.Queries.GetAdminPortfolio;

public sealed record GetAdminPortfolioQuery : IRequest<AdminPortfolioDto>;
