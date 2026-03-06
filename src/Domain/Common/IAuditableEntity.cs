using MyHomeSolution.Domain.Entities;

namespace MyHomeSolution.Domain.Common;

public interface IAuditableEntity
{
    DateTimeOffset CreatedAt { get; set; }
    string? CreatedBy { get; set; }
    DateTimeOffset? LastModifiedAt { get; set; }
    string? LastModifiedBy { get; set; }
    ICollection<AuditLog> AuditLogs { get; set; }
}
