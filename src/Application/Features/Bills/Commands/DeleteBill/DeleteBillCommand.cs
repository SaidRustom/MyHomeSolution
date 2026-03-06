using MediatR;
using MyHomeSolution.Application.Common.Authorization;
using MyHomeSolution.Application.Common.Constants;

namespace MyHomeSolution.Application.Features.Bills.Commands.DeleteBill;

public sealed record DeleteBillCommand(Guid Id) : IRequest, IRequireEditAccess
{
    public string ResourceType => EntityTypes.Bill;
    public Guid ResourceId => Id;
}
