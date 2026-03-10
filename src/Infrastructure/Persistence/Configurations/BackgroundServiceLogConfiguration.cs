using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyHomeSolution.Domain.Entities;

namespace MyHomeSolution.Infrastructure.Persistence.Configurations;

public sealed class BackgroundServiceLogConfiguration
    : IEntityTypeConfiguration<BackgroundServiceLog>
{
    public void Configure(EntityTypeBuilder<BackgroundServiceLog> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.ResultMessage)
            .HasMaxLength(4000);

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasIndex(e => e.BackgroundServiceId);

        builder.HasIndex(e => e.StartedAt)
            .IsDescending();

        builder.HasIndex(e => e.Status);

        builder.HasOne(e => e.ExceptionLog)
            .WithMany()
            .HasForeignKey(e => e.ExceptionLogId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
