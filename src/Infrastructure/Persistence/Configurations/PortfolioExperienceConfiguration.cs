using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyHomeSolution.Domain.Entities;

namespace MyHomeSolution.Infrastructure.Persistence.Configurations;

public sealed class PortfolioExperienceConfiguration : IEntityTypeConfiguration<PortfolioExperience>
{
    public void Configure(EntityTypeBuilder<PortfolioExperience> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Company)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(e => e.Role)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(e => e.LogoUrl)
            .HasMaxLength(1000);

        builder.Property(e => e.CompanyUrl)
            .HasMaxLength(1000);

        builder.Property(e => e.Technologies)
            .HasMaxLength(1000);

        builder.Property(e => e.StartDate)
            .IsRequired();
    }
}
