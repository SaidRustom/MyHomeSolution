using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyHomeSolution.Domain.Entities;

namespace MyHomeSolution.Infrastructure.Persistence.Configurations;

public sealed class BackgroundServiceDefinitionConfiguration
    : IEntityTypeConfiguration<BackgroundServiceDefinition>
{
    public void Configure(EntityTypeBuilder<BackgroundServiceDefinition> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasMaxLength(1024)
            .IsRequired();

        builder.Property(e => e.QualifiedTypeName)
            .HasMaxLength(1024)
            .IsRequired();

        builder.HasIndex(e => e.QualifiedTypeName)
            .IsUnique();

        builder.HasIndex(e => e.Name);

        builder.HasMany(e => e.Logs)
            .WithOne(l => l.BackgroundService)
            .HasForeignKey(l => l.BackgroundServiceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
