using System.ComponentModel.DataAnnotations;
using MyHomeSolution.Domain.Common;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Domain.Entities;

public sealed class TaskOccurrence : BaseAuditableEntity
{
    [Timestamp]
    public byte[] RowVersion { get; set; } = default!;

    public Guid HouseholdTaskId { get; set; }
    public DateOnly DueDate { get; set; }
    public OccurrenceStatus Status { get; set; } = OccurrenceStatus.Pending;
    public string? AssignedToUserId { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string? CompletedByUserId { get; set; }
    public string? Notes { get; set; }
    public Guid? BillId { get; set; }

    public HouseholdTask HouseholdTask { get; set; } = default!;
    public Bill? Bill { get; set; }
}
