using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Domain.Common;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;
using MyHomeSolution.Infrastructure.Identity;

namespace MyHomeSolution.Infrastructure.Persistence;

public sealed class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider)
    : IdentityDbContext<ApplicationUser>(options), IApplicationDbContext
{
    public DbSet<HouseholdTask> HouseholdTasks => Set<HouseholdTask>();
    public DbSet<RecurrencePattern> RecurrencePatterns => Set<RecurrencePattern>();
    public DbSet<RecurrenceAssignee> RecurrenceAssignees => Set<RecurrenceAssignee>();
    public DbSet<TaskOccurrence> TaskOccurrences => Set<TaskOccurrence>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<AuditHistoryEntry> AuditHistoryEntries => Set<AuditHistoryEntry>();
    public DbSet<EntityShare> EntityShares => Set<EntityShare>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Bill> Bills => Set<Bill>();
    public DbSet<BillSplit> BillSplits => Set<BillSplit>();
    public DbSet<BillItem> BillItems => Set<BillItem>();
    public DbSet<ShoppingList> ShoppingLists => Set<ShoppingList>();
    public DbSet<ShoppingItem> ShoppingItems => Set<ShoppingItem>();
    public DbSet<UserConnection> UserConnections => Set<UserConnection>();
    public DbSet<ExceptionLog> ExceptionLogs => Set<ExceptionLog>();
    public DbSet<BackgroundServiceDefinition> BackgroundServiceDefinitions => Set<BackgroundServiceDefinition>();
    public DbSet<BackgroundServiceLog> BackgroundServiceLogs => Set<BackgroundServiceLog>();
    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<BudgetOccurrence> BudgetOccurrences => Set<BudgetOccurrence>();
    public DbSet<BudgetTransfer> BudgetTransfers => Set<BudgetTransfer>();
    public DbSet<BillBudgetLink> BillBudgetLinks => Set<BillBudgetLink>();
    public DbSet<BillRelatedItem> BillRelatedItems => Set<BillRelatedItem>();
    public DbSet<PortfolioProfile> PortfolioProfiles => Set<PortfolioProfile>();
    public DbSet<PortfolioProject> PortfolioProjects => Set<PortfolioProject>();
    public DbSet<PortfolioExperience> PortfolioExperiences => Set<PortfolioExperience>();
    public DbSet<PortfolioSkill> PortfolioSkills => Set<PortfolioSkill>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
        {
            // SQLite does not support SQL Server rowversion; treat RowVersion as a
            // concurrency token with a client-generated default instead.
            foreach (var entityType in builder.Model.GetEntityTypes())
            {
                var rowVersionProp = entityType.FindProperty("RowVersion");
                if (rowVersionProp is null)
                    continue;

                rowVersionProp.ValueGenerated = Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAddOrUpdate;
                rowVersionProp.SetDefaultValueSql("randomblob(8)");
            }
        }
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var userId = currentUserService.UserId;
        var now = dateTimeProvider.UtcNow;

        ProcessAuditableEntities(userId, now);

        return await base.SaveChangesAsync(cancellationToken);
    }

    private void ProcessAuditableEntities(string? userId, DateTimeOffset now)
    {
        var entries = ChangeTracker.Entries<IAuditableEntity>().ToList();

        foreach (var entry in entries)
        {
            var entityType = entry.Metadata.ClrType;
            var efEntityType = Model.FindEntityType(entityType);
            var tableName = efEntityType?.GetTableName() ?? entityType.Name;

            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy = userId;
                    AddCreatedAudit(entry.Entity, tableName, userId, now);
                    break;

                case EntityState.Modified:
                    entry.Entity.LastModifiedAt = now;
                    entry.Entity.LastModifiedBy = userId;
                    AddModifiedAudit(entry, tableName, userId, now);
                    break;

                case EntityState.Deleted:
                    if (entry.Entity is ISoftDeletable softDeletable)
                    {
                        entry.State = EntityState.Modified;
                        softDeletable.IsDeleted = true;
                        softDeletable.DeletedAt = now;
                        softDeletable.DeletedBy = userId;
                        entry.Entity.LastModifiedAt = now;
                        entry.Entity.LastModifiedBy = userId;
                    }
                    AddDeletedAudit(entry.Entity, tableName, userId, now);
                    break;
            }
        }
    }

    private void AddCreatedAudit(
        IAuditableEntity entity, string tableName, string? userId, DateTimeOffset now)
    {
        if (entity is not IEntity identifiable)
            return;

        var auditLog = new AuditLog(
            tableName, identifiable.Id.ToString(), userId, AuditActionType.Create, now);

        entity.AuditLogs ??= [];
        entity.AuditLogs.Add(auditLog);
        AuditLogs.Add(auditLog);
    }

    private void AddModifiedAudit(
        EntityEntry<IAuditableEntity> entry, string tableName, string? userId, DateTimeOffset now)
    {
        if (entry.Entity is not IEntity identifiable)
            return;

        var auditLog = new AuditLog(
            tableName, identifiable.Id.ToString(), userId, AuditActionType.Update, now);

        var changes = new List<AuditHistoryEntry>();

        foreach (var prop in entry.Properties)
        {
            if (IsAuditMetadataProperty(prop.Metadata.Name))
                continue;

            if (!prop.IsModified)
                continue;

            var originalValue = prop.OriginalValue;
            var currentValue = prop.CurrentValue;

            if (Equals(originalValue, currentValue))
                continue;

            if (prop.Metadata.ClrType.IsAssignableTo(typeof(System.Collections.IEnumerable))
                && prop.Metadata.ClrType != typeof(string))
                continue;

            changes.Add(new AuditHistoryEntry(auditLog)
            {
                PropertyName = prop.Metadata.Name,
                OldValue = originalValue?.ToString(),
                NewValue = currentValue?.ToString()
            });
        }

        if (changes.Count > 0)
        {
            entry.Entity.AuditLogs ??= [];
            entry.Entity.AuditLogs.Add(auditLog);

            AuditLogs.Add(auditLog);
            AuditHistoryEntries.AddRange(changes);
        }
    }

    private void AddDeletedAudit(
        IAuditableEntity entity, string tableName, string? userId, DateTimeOffset now)
    {
        if (entity is not IEntity identifiable)
            return;

        var auditLog = new AuditLog(
            tableName, identifiable.Id.ToString(), userId, AuditActionType.Delete, now);

        entity.AuditLogs ??= [];
        entity.AuditLogs.Add(auditLog);
        AuditLogs.Add(auditLog);
    }

    private static bool IsAuditMetadataProperty(string propertyName) =>
        propertyName is nameof(IAuditableEntity.CreatedAt)
            or nameof(IAuditableEntity.CreatedBy)
            or nameof(IAuditableEntity.LastModifiedAt)
            or nameof(IAuditableEntity.LastModifiedBy)
            or nameof(ISoftDeletable.IsDeleted)
            or nameof(ISoftDeletable.DeletedAt)
            or nameof(ISoftDeletable.DeletedBy);
}
