using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Domain.Common;
using MyHomeSolution.Domain.Entities;

namespace MyHomeSolution.Application.Tests.Testing;

public sealed class TestDbContext(DbContextOptions<TestDbContext> options)
    : DbContext(options), IApplicationDbContext
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // SQLite does not natively support DateTimeOffset ordering.
        // Convert all DateTimeOffset properties to sortable strings.
        var dateTimeOffsetConverter = new DateTimeOffsetToStringConverter();
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTimeOffset))
                    property.SetValueConverter(dateTimeOffsetConverter);
                else if (property.ClrType == typeof(DateTimeOffset?))
                    property.SetValueConverter(new ValueConverter<DateTimeOffset?, string?>(
                        v => v.HasValue ? v.Value.ToString("O") : null,
                        v => v != null ? DateTimeOffset.Parse(v) : null));
            }
        }

        modelBuilder.Entity<HouseholdTask>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Title).HasMaxLength(200).IsRequired();
            entity.Property(t => t.Description).HasMaxLength(2000);
            entity.Property(t => t.AssignedToUserId).HasMaxLength(450);
            entity.Ignore(t => t.AuditLogs);
            entity.Ignore(t => t.Bills);

            entity.HasOne(t => t.RecurrencePattern)
                .WithOne(rp => rp.HouseholdTask)
                .HasForeignKey<RecurrencePattern>(rp => rp.HouseholdTaskId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(t => t.Occurrences)
                .WithOne(o => o.HouseholdTask)
                .HasForeignKey(o => o.HouseholdTaskId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RecurrencePattern>(entity =>
        {
            entity.HasKey(rp => rp.Id);
            entity.Property(rp => rp.Interval).IsRequired();
            entity.Property(rp => rp.StartDate).IsRequired();

            entity.HasMany(rp => rp.Assignees)
                .WithOne(a => a.RecurrencePattern)
                .HasForeignKey(a => a.RecurrencePatternId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RecurrenceAssignee>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.UserId).HasMaxLength(450).IsRequired();
        });

        modelBuilder.Entity<TaskOccurrence>(entity =>
        {
            entity.HasKey(o => o.Id);
            entity.Property(o => o.AssignedToUserId).HasMaxLength(450);
            entity.Property(o => o.CompletedByUserId).HasMaxLength(450);
            entity.Property(o => o.Notes).HasMaxLength(1000);
            entity.Ignore(o => o.AuditLogs);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.EntityName).HasMaxLength(256).IsRequired();
            entity.Property(a => a.EntityId).HasMaxLength(450).IsRequired();
            entity.Property(a => a.UserId).HasMaxLength(450);

            entity.HasMany(a => a.HistoryEntries)
                .WithOne(h => h.AuditLog)
                .HasForeignKey(h => h.AuditLogId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AuditHistoryEntry>(entity =>
        {
            entity.HasKey(h => h.Id);
            entity.Property(h => h.PropertyName).HasMaxLength(256).IsRequired();
            entity.Property(h => h.OldValue).HasMaxLength(2000);
            entity.Property(h => h.NewValue).HasMaxLength(2000);
        });

        modelBuilder.Entity<EntityShare>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.EntityType).HasMaxLength(256).IsRequired();
            entity.Property(s => s.SharedWithUserId).HasMaxLength(450).IsRequired();
            entity.Ignore(s => s.AuditLogs);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(n => n.Id);
            entity.Property(n => n.Title).HasMaxLength(256).IsRequired();
            entity.Property(n => n.Description).HasMaxLength(2000);
            entity.Property(n => n.FromUserId).HasMaxLength(450).IsRequired();
            entity.Property(n => n.ToUserId).HasMaxLength(450).IsRequired();
            entity.Property(n => n.RelatedEntityType).HasMaxLength(256);
            entity.Ignore(n => n.AuditLogs);
        });

        modelBuilder.Entity<Bill>(entity =>
        {
            entity.HasKey(b => b.Id);
            entity.Property(b => b.Title).HasMaxLength(256).IsRequired();
            entity.Property(b => b.Description).HasMaxLength(2000);
            entity.Property(b => b.Amount).HasColumnType("TEXT");
            entity.Property(b => b.Currency).HasMaxLength(3).IsRequired();
            entity.Property(b => b.PaidByUserId).HasMaxLength(450).IsRequired();
            entity.Property(b => b.ReceiptUrl).HasMaxLength(2048);
            entity.Property(b => b.RelatedEntityType).HasMaxLength(256);
            entity.Property(b => b.Notes).HasMaxLength(2000);
            entity.Ignore(b => b.AuditLogs);

            entity.HasMany(b => b.Splits)
                .WithOne(s => s.Bill)
                .HasForeignKey(s => s.BillId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(b => b.Items)
                .WithOne(i => i.Bill)
                .HasForeignKey(i => i.BillId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BillSplit>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.UserId).HasMaxLength(450).IsRequired();
            entity.Property(s => s.Percentage).HasColumnType("TEXT");
            entity.Property(s => s.Amount).HasColumnType("TEXT");
        });

        modelBuilder.Entity<BillItem>(entity =>
        {
            entity.HasKey(i => i.Id);
            entity.Property(i => i.Name).HasMaxLength(500).IsRequired();
            entity.Property(i => i.UnitPrice).HasColumnType("TEXT");
            entity.Property(i => i.Price).HasColumnType("TEXT");
            entity.Property(i => i.Discount).HasColumnType("TEXT");
        });

        modelBuilder.Entity<ShoppingList>(entity =>
        {
            entity.HasKey(sl => sl.Id);
            entity.Property(sl => sl.Title).HasMaxLength(256).IsRequired();
            entity.Property(sl => sl.Description).HasMaxLength(2000);
            entity.Ignore(sl => sl.AuditLogs);
            entity.Ignore(sl => sl.Bills);

            entity.HasMany(sl => sl.Items)
                .WithOne(si => si.ShoppingList)
                .HasForeignKey(si => si.ShoppingListId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ShoppingItem>(entity =>
        {
            entity.HasKey(si => si.Id);
            entity.Property(si => si.Name).HasMaxLength(500).IsRequired();
            entity.Property(si => si.Unit).HasMaxLength(50);
            entity.Property(si => si.Notes).HasMaxLength(1000);
            entity.Property(si => si.CheckedByUserId).HasMaxLength(450);
        });

        modelBuilder.Entity<UserConnection>(entity =>
        {
            entity.HasKey(uc => uc.Id);
            entity.Property(uc => uc.RequesterId).HasMaxLength(450).IsRequired();
            entity.Property(uc => uc.AddresseeId).HasMaxLength(450).IsRequired();
            entity.Ignore(uc => uc.AuditLogs);
        });
    }
}
