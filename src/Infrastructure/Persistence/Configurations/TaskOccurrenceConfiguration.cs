using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyHomeSolution.Domain.Entities;

namespace MyHomeSolution.Infrastructure.Persistence.Configurations;

public sealed class TaskOccurrenceConfiguration : IEntityTypeConfiguration<TaskOccurrence>
{
    public void Configure(EntityTypeBuilder<TaskOccurrence> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.RowVersion)
            .IsRowVersion();

        builder.Property(o => o.AssignedToUserId)
            .HasMaxLength(450);

        builder.Property(o => o.CompletedByUserId)
            .HasMaxLength(450);

        builder.Property(o => o.Notes)
            .HasMaxLength(1000);

        builder.Ignore(o => o.AuditLogs);

        builder.HasQueryFilter(o => !o.IsDeleted);

        builder.HasIndex(o => o.DueDate);
        builder.HasIndex(o => o.Status);
        builder.HasIndex(o => new { o.HouseholdTaskId, o.DueDate });
    }
}
