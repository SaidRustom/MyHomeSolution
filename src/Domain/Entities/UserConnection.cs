using MyHomeSolution.Domain.Common;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Domain.Entities;

public sealed class UserConnection : BaseAuditableEntity
{
    public string RequesterId { get; set; } = default!;
    public string AddresseeId { get; set; } = default!;
    public ConnectionStatus Status { get; set; }
    public DateTimeOffset? RespondedAt { get; set; }
}
