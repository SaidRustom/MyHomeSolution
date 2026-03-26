using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyHomeSolution.Domain.Entities;

namespace MyHomeSolution.Infrastructure.Persistence.Configurations;

public sealed class PortfolioProjectConfiguration : IEntityTypeConfiguration<PortfolioProject>
{
    public void Configure(EntityTypeBuilder<PortfolioProject> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Title)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(p => p.ShortDescription)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(p => p.LongDescription)
            .HasMaxLength(4000);

        builder.Property(p => p.ImageUrl)
            .HasMaxLength(1000);

        builder.Property(p => p.LiveUrl)
            .HasMaxLength(1000);

        builder.Property(p => p.GitHubUrl)
            .HasMaxLength(1000);

        builder.Property(p => p.Technologies)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(p => p.Category)
            .HasMaxLength(200);
    }
}
