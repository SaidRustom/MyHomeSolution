using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyHomeSolution.Domain.Entities;

namespace MyHomeSolution.Infrastructure.Persistence.Configurations;

public sealed class HouseholdTaskConfiguration : IEntityTypeConfiguration<HouseholdTask>
{
    public void Configure(EntityTypeBuilder<HouseholdTask> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(t => t.Description)
            .HasMaxLength(2000);

        builder.Property(t => t.AssignedToUserId)
            .HasMaxLength(450);

        builder.Ignore(t => t.AuditLogs);
        builder.Ignore(t => t.Bills);

        builder.HasOne(t => t.RecurrencePattern)
            .WithOne(rp => rp.HouseholdTask)
            .HasForeignKey<RecurrencePattern>(rp => rp.HouseholdTaskId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.Occurrences)
            .WithOne(o => o.HouseholdTask)
            .HasForeignKey(o => o.HouseholdTaskId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(t => !t.IsDeleted);

        builder.HasIndex(t => t.IsActive);
        builder.HasIndex(t => t.Category);
        builder.HasIndex(t => t.Priority);
        builder.HasIndex(t => t.IsDeleted);
    }
}
