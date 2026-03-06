using MediatR;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Features.Bills.Commands.CreateBill;

public sealed record CreateBillCommand : IRequest<Guid>
{
    public required string Title { get; init; }
    public string? Description { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "$";
    public BillCategory Category { get; init; }
    public DateTimeOffset BillDate { get; init; }
    public string? Notes { get; init; }
    public Guid? RelatedEntityId { get; init; }
    public string? RelatedEntityType { get; init; }
    public List<BillSplitRequest> Splits { get; init; } = [];
}
