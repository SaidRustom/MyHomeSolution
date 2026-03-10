using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyHomeSolution.Domain.Entities;

namespace MyHomeSolution.Infrastructure.Persistence.Configurations;

public sealed class ShoppingItemConfiguration : IEntityTypeConfiguration<ShoppingItem>
{
    public void Configure(EntityTypeBuilder<ShoppingItem> builder)
    {
        builder.HasKey(si => si.Id);

        builder.Property(si => si.Name)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(si => si.Unit)
            .HasMaxLength(50);

        builder.Property(si => si.Notes)
            .HasMaxLength(1000);

        builder.Property(si => si.CheckedByUserId)
            .HasMaxLength(450);

        builder.HasIndex(si => si.ShoppingListId);
        builder.HasIndex(si => new { si.ShoppingListId, si.SortOrder });

        builder.HasQueryFilter(si => !si.ShoppingList.IsDeleted);
    }
}
