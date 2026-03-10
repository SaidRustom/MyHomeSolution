using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyHomeSolution.Domain.Entities;

namespace MyHomeSolution.Infrastructure.Persistence.Configurations;

public sealed class BillSplitConfiguration : IEntityTypeConfiguration<BillSplit>
{
    public void Configure(EntityTypeBuilder<BillSplit> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.UserId)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(s => s.Percentage)
            .HasPrecision(5, 2);

        builder.Property(s => s.Amount)
            .HasPrecision(18, 2);

        builder.HasIndex(s => s.UserId);
        builder.HasIndex(s => new { s.BillId, s.UserId }).IsUnique();

        builder.HasQueryFilter(s => !s.Bill.IsDeleted);
    }
}
