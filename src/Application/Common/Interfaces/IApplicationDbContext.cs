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
    DbSet<ExceptionLog> ExceptionLogs { get; }
    DbSet<BackgroundServiceDefinition> BackgroundServiceDefinitions { get; }
    DbSet<BackgroundServiceLog> BackgroundServiceLogs { get; }
    DbSet<Budget> Budgets { get; }
    DbSet<BudgetOccurrence> BudgetOccurrences { get; }
    DbSet<BudgetTransfer> BudgetTransfers { get; }
    DbSet<BillBudgetLink> BillBudgetLinks { get; }
    DbSet<BillRelatedItem> BillRelatedItems { get; }
    DbSet<PortfolioProfile> PortfolioProfiles { get; }
    DbSet<PortfolioProject> PortfolioProjects { get; }
    DbSet<PortfolioExperience> PortfolioExperiences { get; }
    DbSet<PortfolioSkill> PortfolioSkills { get; }
    DbSet<DemoUser> DemoUsers { get; }
    DbSet<HomepageWidget> HomepageWidgets { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
