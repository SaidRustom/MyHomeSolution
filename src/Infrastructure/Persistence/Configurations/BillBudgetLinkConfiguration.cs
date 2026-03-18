using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyHomeSolution.Domain.Entities;

namespace MyHomeSolution.Infrastructure.Persistence.Configurations;

public sealed class BillBudgetLinkConfiguration : IEntityTypeConfiguration<BillBudgetLink>
{
    public void Configure(EntityTypeBuilder<BillBudgetLink> builder)
    {
        builder.HasKey(l => l.Id);

        builder.HasOne(l => l.Bill)
            .WithOne(b => b.BudgetLink)
            .HasForeignKey<BillBudgetLink>(l => l.BillId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.Budget)
            .WithMany(b => b.BillLinks)
            .HasForeignKey(l => l.BudgetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.BudgetOccurrence)
            .WithMany(o => o.BillLinks)
            .HasForeignKey(l => l.BudgetOccurrenceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(l => l.BillId).IsUnique();
        builder.HasIndex(l => l.BudgetId);
        builder.HasIndex(l => l.BudgetOccurrenceId);
    }
}
