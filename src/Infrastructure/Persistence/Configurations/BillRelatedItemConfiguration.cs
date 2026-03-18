using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyHomeSolution.Domain.Entities;

namespace MyHomeSolution.Infrastructure.Persistence.Configurations;

public sealed class BillRelatedItemConfiguration : IEntityTypeConfiguration<BillRelatedItem>
{
    public void Configure(EntityTypeBuilder<BillRelatedItem> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.RelatedEntityType)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(r => r.RelatedEntityName)
            .HasMaxLength(512);

        builder.HasOne(r => r.Bill)
            .WithMany(b => b.RelatedItems)
            .HasForeignKey(r => r.BillId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => r.BillId);
        builder.HasIndex(r => new { r.RelatedEntityType, r.RelatedEntityId });
    }
}
