using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyHomeSolution.Domain.Entities;

namespace MyHomeSolution.Infrastructure.Persistence.Configurations;

public sealed class UserConnectionConfiguration : IEntityTypeConfiguration<UserConnection>
{
    public void Configure(EntityTypeBuilder<UserConnection> builder)
    {
        builder.HasKey(uc => uc.Id);

        builder.Property(uc => uc.RequesterId)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(uc => uc.AddresseeId)
            .HasMaxLength(450)
            .IsRequired();

        builder.Ignore(uc => uc.AuditLogs);

        builder.HasQueryFilter(uc => !uc.IsDeleted);

        builder.HasIndex(uc => uc.RequesterId);
        builder.HasIndex(uc => uc.AddresseeId);
        builder.HasIndex(uc => new { uc.RequesterId, uc.AddresseeId })
            .IsUnique()
            .HasFilter("IsDeleted = 0");
        builder.HasIndex(uc => uc.Status);
        builder.HasIndex(uc => uc.IsDeleted);
    }
}
