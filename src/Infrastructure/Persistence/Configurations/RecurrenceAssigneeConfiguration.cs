using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyHomeSolution.Domain.Entities;

namespace MyHomeSolution.Infrastructure.Persistence.Configurations;

public sealed class RecurrenceAssigneeConfiguration : IEntityTypeConfiguration<RecurrenceAssignee>
{
    public void Configure(EntityTypeBuilder<RecurrenceAssignee> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.UserId)
            .HasMaxLength(450)
            .IsRequired();

        builder.HasIndex(a => new { a.RecurrencePatternId, a.Order })
            .IsUnique();

        builder.HasIndex(a => new { a.RecurrencePatternId, a.UserId })
            .IsUnique();

        builder.HasQueryFilter(a => !a.RecurrencePattern.HouseholdTask.IsDeleted);
    }
}
