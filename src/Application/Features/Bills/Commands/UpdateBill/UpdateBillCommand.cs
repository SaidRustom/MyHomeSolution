using MediatR;
using MyHomeSolution.Application.Common.Authorization;
using MyHomeSolution.Application.Common.Constants;
using MyHomeSolution.Application.Features.Bills.Commands.CreateBill;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Features.Bills.Commands.UpdateBill;

public sealed record UpdateBillCommand : IRequest, IRequireEditAccess
{
    public Guid Id { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "USD";
    public BillCategory Category { get; init; }
    public DateTimeOffset BillDate { get; init; }
    public string? Notes { get; init; }
    public string? PaidByUserId { get; init; }
    public List<BillSplitRequest>? Splits { get; init; }

    public string ResourceType => EntityTypes.Bill;
    public Guid ResourceId => Id;
}
