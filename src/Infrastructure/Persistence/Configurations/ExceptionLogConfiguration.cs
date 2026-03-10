using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyHomeSolution.Domain.Entities;

namespace MyHomeSolution.Infrastructure.Persistence.Configurations;

public sealed class ExceptionLogConfiguration : IEntityTypeConfiguration<ExceptionLog>
{
    public void Configure(EntityTypeBuilder<ExceptionLog> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.ExceptionType)
            .HasMaxLength(1024)
            .IsRequired();

        builder.Property(e => e.Message)
            .IsRequired();

        builder.Property(e => e.ThrownByService)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(e => e.ClassName)
            .HasMaxLength(512);

        builder.Property(e => e.MethodName)
            .HasMaxLength(512);

        builder.Property(e => e.RequestPath)
            .HasMaxLength(2048);

        builder.Property(e => e.HttpMethod)
            .HasMaxLength(10);

        builder.Property(e => e.UserId)
            .HasMaxLength(450);

        builder.Property(e => e.TraceId)
            .HasMaxLength(256);

        builder.Property(e => e.Environment)
            .HasMaxLength(50);

        builder.HasIndex(e => e.OccurredAt)
            .IsDescending();

        builder.HasIndex(e => e.ExceptionType);
        builder.HasIndex(e => e.Severity);
        builder.HasIndex(e => e.IsHandled);
        builder.HasIndex(e => e.ThrownByService);
    }
}
