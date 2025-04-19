using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NewsAggregator.Domain.News.Entities;

namespace NewsAggregator.Infrastructure.Data.Configurations
{
    public class NewsCategoryConfiguration : IEntityTypeConfiguration<NewsCategory>
    {
        public void Configure(EntityTypeBuilder<NewsCategory> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.Slug)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.Description)
                .HasMaxLength(500);

            builder.Property(x => x.ProviderType)
                .IsRequired();

            builder.Property(x => x.ProviderSpecificKey)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(x => x.IsActive)
                .IsRequired();

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.Property(x => x.UpdatedAt);

            builder.HasIndex(x => x.Slug)
                .IsUnique();

            builder.HasIndex(x => new { x.ProviderType, x.ProviderSpecificKey })
                .IsUnique();
        }
    }
} 