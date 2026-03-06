using BlazorUI.Models.Bills;

namespace BlazorUI.Models.ShoppingLists;

public sealed record ProcessReceiptResultDto
{
    public Guid BillId { get; init; }
    public required BillDetailDto Bill { get; init; }
    public IReadOnlyList<ShoppingItemDto> CheckedItems { get; init; } = [];
    public IReadOnlyList<ShoppingItemDto> AddedItems { get; init; } = [];
}
