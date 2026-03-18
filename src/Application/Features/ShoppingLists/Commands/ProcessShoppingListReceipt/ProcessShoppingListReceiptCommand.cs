using MediatR;
using MyHomeSolution.Application.Common.Authorization;
using MyHomeSolution.Application.Common.Constants;
using MyHomeSolution.Application.Features.ShoppingLists.Common;

namespace MyHomeSolution.Application.Features.ShoppingLists.Commands.ProcessShoppingListReceipt;

public sealed record ProcessShoppingListReceiptCommand : IRequest<ProcessReceiptResultDto>, IRequireViewAccess
{
    public Guid ShoppingListId { get; init; }
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public required Stream Content { get; init; }
    public List<ReceiptSplitRequest>? Splits { get; init; }

    public string ResourceType => EntityTypes.ShoppingList;
    public Guid ResourceId => ShoppingListId;
}

public sealed record ReceiptSplitRequest
{
    public required string UserId { get; init; }
    public decimal? Percentage { get; init; }
}
