using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Domain.Entities;

namespace MyHomeSolution.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<HouseholdTask> HouseholdTasks { get; }
    DbSet<RecurrencePattern> RecurrencePatterns { get; }
    DbSet<RecurrenceAssignee> RecurrenceAssignees { get; }
    DbSet<TaskOccurrence> TaskOccurrences { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<AuditHistoryEntry> AuditHistoryEntries { get; }
    DbSet<EntityShare> EntityShares { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<Bill> Bills { get; }
    DbSet<BillSplit> BillSplits { get; }
    DbSet<BillItem> BillItems { get; }
    DbSet<ShoppingList> ShoppingLists { get; }
    DbSet<ShoppingItem> ShoppingItems { get; }
    DbSet<UserConnection> UserConnections { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
