using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyHomeSolution.Domain.Entities;

namespace MyHomeSolution.Infrastructure.Persistence.Configurations;

public sealed class PortfolioProfileConfiguration : IEntityTypeConfiguration<PortfolioProfile>
{
    public void Configure(EntityTypeBuilder<PortfolioProfile> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.FullName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.Headline)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(p => p.SubHeadline)
            .HasMaxLength(500);

        builder.Property(p => p.Bio)
            .HasMaxLength(4000);

        builder.Property(p => p.Email)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(p => p.Phone)
            .HasMaxLength(50);

        builder.Property(p => p.Location)
            .HasMaxLength(200);

        builder.Property(p => p.AvatarUrl)
            .HasMaxLength(1000);

        builder.Property(p => p.ResumeUrl)
            .HasMaxLength(1000);

        builder.Property(p => p.GitHubUrl)
            .HasMaxLength(500);

        builder.Property(p => p.LinkedInUrl)
            .HasMaxLength(500);

        builder.Property(p => p.TwitterUrl)
            .HasMaxLength(500);

        builder.Property(p => p.WebsiteUrl)
            .HasMaxLength(500);
    }
}
