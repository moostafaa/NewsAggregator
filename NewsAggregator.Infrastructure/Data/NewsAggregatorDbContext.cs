using Microsoft.EntityFrameworkCore;
using NewsAggregator.Domain.Auth.Entities;
using NewsAggregator.Domain.Auth.ValueObjects;
using NewsAggregator.Domain.Management.Entities;
using NewsAggregator.Domain.News.Entities;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NewsAggregator.Infrastructure.Data
{
    public class NewsAggregatorDbContext : DbContext
    {
        public DbSet<NewsCategory> NewsCategories { get; set; }
        public DbSet<NewsArticle> NewsArticles { get; set; }
        public DbSet<RssSource> RssSources { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<NewsProvider> NewsProviders { get; set; }
        public DbSet<CloudConfig> CloudConfigs { get; set; }

        public NewsAggregatorDbContext(DbContextOptions<NewsAggregatorDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // News Domain
            modelBuilder.Entity<NewsCategory>(entity =>
            {
                entity.ToTable("NewsCategories");
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Slug)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Description)
                    .HasMaxLength(500);

                entity.Property(e => e.ProviderSpecificKey)
                    .HasMaxLength(100);
                
                entity.HasIndex(e => e.Slug)
                    .IsUnique();
                
                entity.HasIndex(e => new { e.ProviderType, e.IsActive });
            });

            modelBuilder.Entity<NewsArticle>(entity =>
            {
                entity.ToTable("NewsArticles");
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(200);
                
                entity.Property(e => e.Summary)
                    .HasMaxLength(500);
                
                entity.Property(e => e.Body)
                    .IsRequired();
                
                entity.Property(e => e.Category)
                    .HasMaxLength(50);
                
                entity.Property(e => e.Url)
                    .IsRequired();
                
                entity.HasIndex(e => e.PublishedDate);
                entity.HasIndex(e => e.Category);
            });

            modelBuilder.Entity<RssSource>(entity =>
            {
                entity.ToTable("RssSources");
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(100);
                
                entity.Property(e => e.Url)
                    .IsRequired()
                    .HasMaxLength(500);
                
                entity.Property(e => e.Description)
                    .HasMaxLength(500);
                
                entity.Property(e => e.DefaultCategory)
                    .HasMaxLength(50);
                
                entity.HasIndex(e => e.Url)
                    .IsUnique();
                
                entity.HasIndex(e => new { e.ProviderType, e.IsActive });
            });

            // Auth Domain
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.Email)
                    .IsRequired()
                    .HasMaxLength(100);
                
                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(100);
                
                entity.Property(e => e.Picture)
                    .HasMaxLength(1000);
                
                entity.Property(e => e.ExternalProviderId)
                    .HasMaxLength(100);
                
                entity.Property(e => e.ExternalProviderName)
                    .HasMaxLength(50);
                
                entity.HasIndex(e => e.Email)
                    .IsUnique();
                
                entity.HasIndex(e => new { e.ExternalProviderId, e.ExternalProviderName })
                    .IsUnique()
                    .HasFilter("[ExternalProviderId] IS NOT NULL AND [ExternalProviderName] IS NOT NULL");
                
                // Configure owned types
                entity.OwnsMany(e => e.Roles, role =>
                {
                    role.WithOwner().HasForeignKey("UserId");
                    role.Property(r => r.Name).IsRequired().HasMaxLength(50);
                    role.HasKey("UserId", "Name");
                });
            });

            // Management Domain
            modelBuilder.Entity<NewsProvider>(entity =>
            {
                entity.ToTable("NewsProviders");
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(100);
                
                entity.Property(e => e.Description)
                    .HasMaxLength(500);
                
                entity.Property(e => e.ApiKey)
                    .HasMaxLength(500);
                
                entity.Property(e => e.BaseUrl)
                    .HasMaxLength(1000);
                
                entity.HasIndex(e => e.Name)
                    .IsUnique();
                
                entity.HasIndex(e => new { e.ProviderType, e.IsActive });
                
                entity.Property(e => e.AdditionalSettings)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, new JsonSerializerOptions()),
                        v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, new JsonSerializerOptions()));
            });

            modelBuilder.Entity<CloudConfig>(entity =>
            {
                entity.ToTable("CloudConfigs");
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.Region)
                    .IsRequired()
                    .HasMaxLength(50);
                
                entity.Property(e => e.Settings)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, new JsonSerializerOptions()),
                        v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, new JsonSerializerOptions()));
                
                // Configure owned value objects
                entity.OwnsOne(e => e.Credentials, credentials =>
                {
                    credentials.Property(c => c.AccessKeyId)
                        .HasMaxLength(100);
                    
                    credentials.Property(c => c.SecretAccessKey)
                        .HasMaxLength(100);
                    
                    credentials.Property(c => c.TenantId)
                        .HasMaxLength(100);
                    
                    credentials.Property(c => c.ClientId)
                        .HasMaxLength(100);
                    
                    credentials.Property(c => c.ClientSecret)
                        .HasMaxLength(100);
                });
            });
        }
    }
} 