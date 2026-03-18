using System.ComponentModel.DataAnnotations;
using MyHomeSolution.Domain.Common;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Domain.Entities;

public sealed class Bill : BaseAuditableEntity
{
    [Timestamp]
    public byte[] RowVersion { get; set; } = default!;

    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "CAD";
    public BillCategory Category { get; set; }
    public DateTimeOffset BillDate { get; set; }
    public string PaidByUserId { get; set; } = default!;
    public string? ReceiptUrl { get; set; }
    public Guid? RelatedEntityId { get; set; }
    public string? RelatedEntityType { get; set; }
    public string? Notes { get; set; }

    public ICollection<BillSplit> Splits { get; set; } = [];
    public ICollection<BillItem> Items { get; set; } = [];
    public ICollection<BillRelatedItem> RelatedItems { get; set; } = [];

    /// <summary>
    /// Single budget link. A bill can only belong to one budget.
    /// </summary>
    public BillBudgetLink? BudgetLink { get; set; }
}
