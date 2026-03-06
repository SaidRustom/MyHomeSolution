using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyHomeSolution.Domain.Entities;

namespace MyHomeSolution.Infrastructure.Persistence.Configurations;

public sealed class RecurrencePatternConfiguration : IEntityTypeConfiguration<RecurrencePattern>
{
    public void Configure(EntityTypeBuilder<RecurrencePattern> builder)
    {
        builder.HasKey(rp => rp.Id);

        builder.Property(rp => rp.Interval)
            .IsRequired();

        builder.Property(rp => rp.StartDate)
            .IsRequired();

        builder.HasMany(rp => rp.Assignees)
            .WithOne(a => a.RecurrencePattern)
            .HasForeignKey(a => a.RecurrencePatternId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(rp => rp.HouseholdTaskId)
            .IsUnique();
    }
}
