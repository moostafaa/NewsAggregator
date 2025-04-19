using System;
using Microsoft.EntityFrameworkCore;
using NewsAggregator.Crawler.Models;

namespace NewsAggregator.Crawler.Data
{
    public class NewsCrawlerDbContext : DbContext
    {
        public NewsCrawlerDbContext(DbContextOptions<NewsCrawlerDbContext> options)
            : base(options)
        {
        }

        public DbSet<Category> Categories { get; set; }
        public DbSet<CrawledSource> CrawledSources { get; set; }
        public DbSet<CrawlerState> CrawlerStates { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Category entity
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Slug).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.ProviderType).HasMaxLength(50);
                entity.HasIndex(e => e.Slug).IsUnique();
                
                // Set default values
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            });

            // Configure CrawledSource entity
            modelBuilder.Entity<CrawledSource>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Url).IsRequired().HasMaxLength(500);
                entity.HasIndex(e => e.Url).IsUnique();
                
                // Set default values
                entity.Property(e => e.LastCrawledAt).HasDefaultValue(null);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            });

            // Configure CrawlerState entity
            modelBuilder.Entity<CrawlerState>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CrawlerId).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
                
                // Set default values
                entity.Property(e => e.StartedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.CompletedAt).HasDefaultValue(null);
                entity.Property(e => e.SourcesProcessed).HasDefaultValue(0);
                entity.Property(e => e.ArticlesProcessed).HasDefaultValue(0);
            });
        }
    }
} 