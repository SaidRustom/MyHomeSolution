using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyHomeSolution.Domain.Entities;

namespace MyHomeSolution.Infrastructure.Persistence.Configurations;

public sealed class ShoppingListConfiguration : IEntityTypeConfiguration<ShoppingList>
{
    public void Configure(EntityTypeBuilder<ShoppingList> builder)
    {
        builder.HasKey(sl => sl.Id);

        builder.Property(sl => sl.Title)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(sl => sl.Description)
            .HasMaxLength(2000);

        builder.Ignore(sl => sl.AuditLogs);
        builder.Ignore(sl => sl.Bills);

        builder.HasOne(sl => sl.DefaultBudget)
            .WithMany()
            .HasForeignKey(sl => sl.DefaultBudgetId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(sl => sl.Items)
            .WithOne(si => si.ShoppingList)
            .HasForeignKey(si => si.ShoppingListId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(sl => !sl.IsDeleted);

        builder.HasIndex(sl => sl.Category);
        builder.HasIndex(sl => sl.DueDate);
        builder.HasIndex(sl => sl.IsDeleted);
        builder.HasIndex(sl => sl.IsCompleted);
        builder.HasIndex(sl => sl.CreatedBy);
    }
}
