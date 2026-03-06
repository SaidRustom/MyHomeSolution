using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyHomeSolution.Domain.Entities;

namespace MyHomeSolution.Infrastructure.Persistence.Configurations;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasKey(n => n.Id);

        builder.Property(n => n.Title)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(n => n.Description)
            .HasMaxLength(2000);

        builder.Property(n => n.FromUserId)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(n => n.ToUserId)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(n => n.RelatedEntityType)
            .HasMaxLength(256);

        builder.Ignore(n => n.AuditLogs);

        builder.HasQueryFilter(n => !n.IsDeleted);

        builder.HasIndex(n => n.ToUserId);
        builder.HasIndex(n => new { n.ToUserId, n.IsRead })
            .HasFilter("IsDeleted = 0");
        builder.HasIndex(n => n.CreatedAt);
        builder.HasIndex(n => n.IsDeleted);
    }
}
