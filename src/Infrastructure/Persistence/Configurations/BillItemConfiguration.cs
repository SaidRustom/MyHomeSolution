using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyHomeSolution.Domain.Entities;

namespace MyHomeSolution.Infrastructure.Persistence.Configurations;

public sealed class BillItemConfiguration : IEntityTypeConfiguration<BillItem>
{
    public void Configure(EntityTypeBuilder<BillItem> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Name)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(i => i.UnitPrice)
            .HasPrecision(18, 2);

        builder.Property(i => i.Price)
            .HasPrecision(18, 2);

        builder.Property(i => i.Discount)
            .HasPrecision(18, 2);

        builder.Property(i => i.TaxAmount)
            .HasPrecision(18, 2);

        builder.HasIndex(i => i.BillId);
        builder.HasIndex(i => i.ShoppingListId)
            .HasFilter("ShoppingListId IS NOT NULL");

        builder.HasQueryFilter(i => !i.Bill.IsDeleted);
    }
}
