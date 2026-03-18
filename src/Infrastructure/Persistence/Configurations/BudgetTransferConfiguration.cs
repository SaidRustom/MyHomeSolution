using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyHomeSolution.Domain.Entities;

namespace MyHomeSolution.Infrastructure.Persistence.Configurations;

public sealed class BudgetTransferConfiguration : IEntityTypeConfiguration<BudgetTransfer>
{
    public void Configure(EntityTypeBuilder<BudgetTransfer> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Amount)
            .HasPrecision(18, 2);

        builder.Property(t => t.Reason)
            .HasMaxLength(1000);

        builder.Ignore(t => t.AuditLogs);

        builder.HasIndex(t => t.SourceOccurrenceId);
        builder.HasIndex(t => t.DestinationOccurrenceId);
    }
}
