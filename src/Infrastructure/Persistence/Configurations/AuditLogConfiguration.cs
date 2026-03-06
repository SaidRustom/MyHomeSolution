using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyHomeSolution.Domain.Entities;

namespace MyHomeSolution.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.EntityName)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(a => a.EntityId)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(a => a.UserId)
            .HasMaxLength(450);

        builder.HasMany(a => a.HistoryEntries)
            .WithOne(h => h.AuditLog)
            .HasForeignKey(h => h.AuditLogId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(a => new { a.EntityName, a.EntityId });
        builder.HasIndex(a => a.Timestamp);
    }
}
