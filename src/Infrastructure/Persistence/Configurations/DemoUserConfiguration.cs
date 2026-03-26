using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyHomeSolution.Domain.Entities;

namespace MyHomeSolution.Infrastructure.Persistence.Configurations;

public sealed class DemoUserConfiguration : IEntityTypeConfiguration<DemoUser>
{
    public void Configure(EntityTypeBuilder<DemoUser> builder)
    {
        builder.ToTable("DemoUsers");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.UserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(d => d.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(d => d.FullName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(d => d.CreatedAt)
            .IsRequired();

        builder.Property(d => d.ExpiresAt)
            .IsRequired();

        builder.Property(d => d.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(d => d.ActionCount)
            .IsRequired()
            .HasDefaultValue(0L);

        builder.HasIndex(d => d.Email);
        builder.HasIndex(d => d.IsActive);
    }
}
