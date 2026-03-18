using MediatR;
using MyHomeSolution.Application.Common.Models;
using MyHomeSolution.Application.Features.Bills.Common;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Features.Bills.Queries.GetBills;

public sealed record GetBillsQuery : IRequest<PaginatedList<BillBriefDto>>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public BillCategory? Category { get; init; }
    public string? PaidByUserId { get; init; }
    public string? SearchTerm { get; init; }
    public DateTimeOffset? FromDate { get; init; }
    public DateTimeOffset? ToDate { get; init; }
    public string? SortBy { get; init; }
    public string? SortDirection { get; init; }
    public bool? IsFullyPaid { get; init; }
    public string? SplitWithUserId { get; init; }
    public bool? HasLinkedTask { get; init; }
    public Guid? ShoppingListId { get; init; }
    public Guid? BudgetId { get; init; }
}
