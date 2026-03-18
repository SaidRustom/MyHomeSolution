using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyHomeSolution.Domain.Entities;

namespace MyHomeSolution.Infrastructure.Persistence.Configurations;

public sealed class BudgetConfiguration : IEntityTypeConfiguration<Budget>
{
    public void Configure(EntityTypeBuilder<Budget> builder)
    {
        builder.HasKey(b => b.Id);

        builder.Property(b => b.RowVersion)
            .IsRowVersion();

        builder.Property(b => b.Name)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(b => b.Description)
            .HasMaxLength(2000);

        builder.Property(b => b.Amount)
            .HasPrecision(18, 2);

        builder.Property(b => b.Currency)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(b => b.StartDate)
            .IsRequired();

        builder.Ignore(b => b.AuditLogs);

        builder.HasOne(b => b.ParentBudget)
            .WithMany(b => b.ChildBudgets)
            .HasForeignKey(b => b.ParentBudgetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(b => b.Occurrences)
            .WithOne(o => o.Budget)
            .HasForeignKey(o => o.BudgetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(b => b.BillLinks)
            .WithOne(l => l.Budget)
            .HasForeignKey(l => l.BudgetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(b => !b.IsDeleted);

        builder.HasIndex(b => b.Category);
        builder.HasIndex(b => b.Period);
        builder.HasIndex(b => b.StartDate);
        builder.HasIndex(b => b.IsDeleted);
        builder.HasIndex(b => b.ParentBudgetId);
    }
}
