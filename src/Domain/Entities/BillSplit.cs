using MyHomeSolution.Domain.Common;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Domain.Entities;

public sealed class BillSplit : BaseEntity
{
    public Guid BillId { get; set; }
    public string UserId { get; set; } = default!;
    public decimal Percentage { get; set; }
    public decimal Amount { get; set; }
    public SplitStatus Status { get; set; }
    public DateTimeOffset? PaidAt { get; set; }

    /// <summary>
    /// When the bill is paid by a user, other split participants owe their share to this user.
    /// Null when unpaid or when this split belongs to the user who paid.
    /// </summary>
    public string? OwedToUserId { get; set; }

    public Bill Bill { get; set; } = default!;
}
