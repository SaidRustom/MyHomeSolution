using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyHomeSolution.Domain.Entities;

namespace MyHomeSolution.Infrastructure.Persistence.Configurations;

public sealed class AuditHistoryEntryConfiguration : IEntityTypeConfiguration<AuditHistoryEntry>
{
    public void Configure(EntityTypeBuilder<AuditHistoryEntry> builder)
    {
        builder.HasKey(h => h.Id);

        builder.Property(h => h.PropertyName)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(h => h.OldValue)
            .HasMaxLength(2000);

        builder.Property(h => h.NewValue)
            .HasMaxLength(2000);

        builder.HasIndex(h => h.AuditLogId);
    }
}
