using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyHomeSolution.Domain.Entities;

namespace MyHomeSolution.Infrastructure.Persistence.Configurations;

public sealed class PortfolioSkillConfiguration : IEntityTypeConfiguration<PortfolioSkill>
{
    public void Configure(EntityTypeBuilder<PortfolioSkill> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(s => s.Category)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(s => s.IconClass)
            .HasMaxLength(200);
    }
}
