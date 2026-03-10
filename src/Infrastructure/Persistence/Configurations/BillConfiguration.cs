using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyHomeSolution.Domain.Entities;

namespace MyHomeSolution.Infrastructure.Persistence.Configurations;

public sealed class BillConfiguration : IEntityTypeConfiguration<Bill>
{
    public void Configure(EntityTypeBuilder<Bill> builder)
    {
        builder.HasKey(b => b.Id);

        builder.Property(b => b.RowVersion)
            .IsRowVersion();

        builder.Property(b => b.Title)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(b => b.Description)
            .HasMaxLength(2000);

        builder.Property(b => b.Amount)
            .HasPrecision(18, 2);

        builder.Property(b => b.Currency)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(b => b.PaidByUserId)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(b => b.ReceiptUrl)
            .HasMaxLength(2048);

        builder.Property(b => b.RelatedEntityType)
            .HasMaxLength(256);

        builder.Property(b => b.Notes)
            .HasMaxLength(2000);

        builder.Ignore(b => b.AuditLogs);

        builder.HasMany(b => b.Splits)
            .WithOne(s => s.Bill)
            .HasForeignKey(s => s.BillId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(b => b.Items)
            .WithOne(i => i.Bill)
            .HasForeignKey(i => i.BillId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(b => !b.IsDeleted);

        builder.HasIndex(b => b.PaidByUserId);
        builder.HasIndex(b => b.BillDate);
        builder.HasIndex(b => b.Category);
        builder.HasIndex(b => b.IsDeleted);
        builder.HasIndex(b => new { b.RelatedEntityType, b.RelatedEntityId })
            .HasFilter("RelatedEntityId IS NOT NULL");
    }
}
