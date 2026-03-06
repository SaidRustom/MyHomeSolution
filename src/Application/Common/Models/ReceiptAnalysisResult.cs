namespace MyHomeSolution.Application.Common.Models;

public sealed record ReceiptAnalysisResult
{
    public required string StoreName { get; init; }
    public string? StoreAddress { get; init; }
    public DateTimeOffset TransactionDate { get; init; }
    public required string Currency { get; init; }
    public decimal Subtotal { get; init; }
    public decimal Discount { get; init; }
    public decimal Total { get; init; }
    public IReadOnlyList<ReceiptLineItem> Items { get; init; } = [];
}

public sealed record ReceiptLineItem
{
    public required string Name { get; init; }
    public decimal Price { get; init; }
    public int Quantity { get; init; } = 1;
}
