using MediatR;
using MyHomeSolution.Application.Common.Authorization;
using MyHomeSolution.Application.Common.Constants;

namespace MyHomeSolution.Application.Features.Budgets.Commands.DeleteBudget;

public sealed record DeleteBudgetCommand(Guid Id) : IRequest, IRequireEditAccess
{
    public string ResourceType => EntityTypes.Budget;
    public Guid ResourceId => Id;
}
