using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyHomeSolution.Domain.Entities;

namespace MyHomeSolution.Infrastructure.Persistence.Configurations;

public sealed class BudgetOccurrenceConfiguration : IEntityTypeConfiguration<BudgetOccurrence>
{
    public void Configure(EntityTypeBuilder<BudgetOccurrence> builder)
    {
        builder.ToTable(tb => tb.UseSqlOutputClause(false));

        builder.HasKey(o => o.Id);

        builder.Property(o => o.AllocatedAmount)
            .HasPrecision(18, 2);

        // SpentAmount: computed from linked non-deleted bills
        builder.Property(o => o.SpentAmount)
            .HasPrecision(18, 2)
            .HasComputedColumnSql(
                "ISNULL((SELECT SUM(b.[Amount]) FROM [Bills] b INNER JOIN [BillBudgetLinks] bbl ON bbl.[BillId] = b.[Id] WHERE bbl.[BudgetOccurrenceId] = [Id] AND b.[IsDeleted] = 0), 0)",
                stored: false);

        builder.Property(o => o.CarryoverAmount)
            .HasPrecision(18, 2);

        // Balance: computed from AllocatedAmount + CarryoverAmount - SpentAmount
        builder.Property(o => o.Balance)
            .HasPrecision(18, 2)
            .HasComputedColumnSql(
                "[AllocatedAmount] + [CarryoverAmount] - ISNULL((SELECT SUM(b.[Amount]) FROM [Bills] b INNER JOIN [BillBudgetLinks] bbl ON bbl.[BillId] = b.[Id] WHERE bbl.[BudgetOccurrenceId] = [Id] AND b.[IsDeleted] = 0), 0)",
                stored: false);

        // IsActive: computed based on current UTC time
        builder.Property(o => o.IsActive)
            .HasComputedColumnSql(
                "CAST(CASE WHEN [PeriodStart] <= SYSUTCDATETIME() AND [PeriodEnd] >= SYSUTCDATETIME() THEN 1 ELSE 0 END AS bit)",
                stored: false);

        builder.Property(o => o.Notes)
            .HasMaxLength(2000);

        builder.Property(o => o.PeriodStart)
            .IsRequired();

        builder.Property(o => o.PeriodEnd)
            .IsRequired();

        builder.HasMany(o => o.BillLinks)
            .WithOne(l => l.BudgetOccurrence)
            .HasForeignKey(l => l.BudgetOccurrenceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(o => o.OutgoingTransfers)
            .WithOne(t => t.SourceOccurrence)
            .HasForeignKey(t => t.SourceOccurrenceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(o => o.IncomingTransfers)
            .WithOne(t => t.DestinationOccurrence)
            .HasForeignKey(t => t.DestinationOccurrenceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(o => o.BudgetId);
        builder.HasIndex(o => new { o.PeriodStart, o.PeriodEnd });
    }
}
