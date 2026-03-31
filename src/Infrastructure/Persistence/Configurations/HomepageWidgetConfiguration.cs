using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyHomeSolution.Domain.Entities;

namespace MyHomeSolution.Infrastructure.Persistence.Configurations;

public sealed class HomepageWidgetConfiguration : IEntityTypeConfiguration<HomepageWidget>
{
    public void Configure(EntityTypeBuilder<HomepageWidget> builder)
    {
        builder.HasKey(w => w.Id);

        builder.Property(w => w.UserId)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(w => w.WidgetType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(w => w.Settings)
            .HasMaxLength(4000);

        builder.HasIndex(w => w.UserId);
    }
}
