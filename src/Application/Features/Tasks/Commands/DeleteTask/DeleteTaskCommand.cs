using MediatR;
using MyHomeSolution.Application.Common.Authorization;
using MyHomeSolution.Application.Common.Constants;

namespace MyHomeSolution.Application.Features.Tasks.Commands.DeleteTask;

public sealed record DeleteTaskCommand(Guid Id) : IRequest, IRequireEditAccess
{
    public string ResourceType => EntityTypes.HouseholdTask;
    public Guid ResourceId => Id;
}
