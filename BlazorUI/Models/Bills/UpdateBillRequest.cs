using BlazorUI.Models.Enums;

namespace BlazorUI.Models.Bills;

public sealed record UpdateBillRequest
{
    public Guid Id { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "CAD";
    public BillCategory Category { get; init; }
    public DateTimeOffset BillDate { get; init; }
    public string? Notes { get; init; }
}
