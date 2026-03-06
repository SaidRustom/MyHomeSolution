using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyHomeSolution.Domain.Entities;

namespace MyHomeSolution.Infrastructure.Persistence.Configurations;

public sealed class EntityShareConfiguration : IEntityTypeConfiguration<EntityShare>
{
    public void Configure(EntityTypeBuilder<EntityShare> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.EntityType)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(s => s.SharedWithUserId)
            .HasMaxLength(450)
            .IsRequired();

        builder.Ignore(s => s.AuditLogs);

        builder.HasQueryFilter(s => !s.IsDeleted);

        builder.HasIndex(s => new { s.EntityType, s.EntityId, s.SharedWithUserId })
            .IsUnique()
            .HasFilter("IsDeleted = 0");

        builder.HasIndex(s => s.SharedWithUserId);
        builder.HasIndex(s => new { s.EntityType, s.EntityId });
    }
}
