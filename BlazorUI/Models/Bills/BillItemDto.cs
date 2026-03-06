namespace BlazorUI.Models.Bills;

public sealed record BillItemDto
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal Price { get; init; }
    public decimal Discount { get; init; }
}
