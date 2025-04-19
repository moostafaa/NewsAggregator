using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NewsAggregator.Domain.News.Entities;
using NewsAggregator.Domain.News.Enums;

namespace NewsAggregator.Infrastructure.Data.Seeders
{
    public class NewsCategorySeeder
    {
        private readonly NewsAggregatorDbContext _context;

        public NewsCategorySeeder(NewsAggregatorDbContext context)
        {
            _context = context;
        }

        public async Task SeedAsync()
        {
            if (await _context.NewsCategories.AnyAsync())
                return;

            var categories = new List<NewsCategory>();

            //// NewsAPI Categories
            //categories.AddRange(new[]
            //{
            //    NewsCategory.Create("Business", "business", "Business news and updates", NewsProviderType.NewsAPI, "business"),
            //    NewsCategory.Create("Entertainment", "entertainment", "Entertainment news and updates", NewsProviderType.NewsAPI, "entertainment"),
            //    NewsCategory.Create("General", "general", "General news", NewsProviderType.NewsAPI, "general"),
            //    NewsCategory.Create("Health", "health", "Health news and updates", NewsProviderType.NewsAPI, "health"),
            //    NewsCategory.Create("Science", "science", "Science news and discoveries", NewsProviderType.NewsAPI, "science"),
            //    NewsCategory.Create("Sports", "sports", "Sports news and updates", NewsProviderType.NewsAPI, "sports"),
            //    NewsCategory.Create("Technology", "technology", "Technology news and updates", NewsProviderType.NewsAPI, "technology")
            //});

            //// The Guardian Categories
            //categories.AddRange(new[]
            //{
            //    NewsCategory.Create("World News", "world", "World news and updates", NewsProviderType.TheGuardian, "world"),
            //    NewsCategory.Create("Politics", "politics", "Political news and updates", NewsProviderType.TheGuardian, "politics"),
            //    NewsCategory.Create("Business", "business", "Business news and updates", NewsProviderType.TheGuardian, "business"),
            //    NewsCategory.Create("Technology", "technology", "Technology news and updates", NewsProviderType.TheGuardian, "technology"),
            //    NewsCategory.Create("Sports", "sports", "Sports news and updates", NewsProviderType.TheGuardian, "sport"),
            //    NewsCategory.Create("Culture", "culture", "Culture news and updates", NewsProviderType.TheGuardian, "culture"),
            //    NewsCategory.Create("Lifestyle", "lifestyle", "Lifestyle news and updates", NewsProviderType.TheGuardian, "lifestyle"),
            //    NewsCategory.Create("Environment", "environment", "Environment news and updates", NewsProviderType.TheGuardian, "environment")
            //});

            //// Reuters Categories
            //categories.AddRange(new[]
            //{
            //    NewsCategory.Create("Business", "business", "Business news and updates", NewsProviderType.Reuters, "business"),
            //    NewsCategory.Create("Markets", "markets", "Markets news and updates", NewsProviderType.Reuters, "markets"),
            //    NewsCategory.Create("World", "world", "World news and updates", NewsProviderType.Reuters, "world"),
            //    NewsCategory.Create("Politics", "politics", "Political news and updates", NewsProviderType.Reuters, "politics"),
            //    NewsCategory.Create("Technology", "technology", "Technology news and updates", NewsProviderType.Reuters, "technology"),
            //    NewsCategory.Create("Sports", "sports", "Sports news and updates", NewsProviderType.Reuters, "sports"),
            //    NewsCategory.Create("Lifestyle", "lifestyle", "Lifestyle news and updates", NewsProviderType.Reuters, "lifestyle"),
            //    NewsCategory.Create("Entertainment", "entertainment", "Entertainment news and updates", NewsProviderType.Reuters, "entertainment"),
            //    NewsCategory.Create("Science", "science", "Science news and updates", NewsProviderType.Reuters, "science"),
            //    NewsCategory.Create("Health", "health", "Health news and updates", NewsProviderType.Reuters, "health")
            //});

            //// Associated Press Categories
            //categories.AddRange(new[]
            //{
            //    NewsCategory.Create("Politics", "politics", "Political news and updates", NewsProviderType.AssociatedPress, "politics"),
            //    NewsCategory.Create("Domestic", "domestic", "Domestic news and updates", NewsProviderType.AssociatedPress, "domestic"),
            //    NewsCategory.Create("International", "international", "International news and updates", NewsProviderType.AssociatedPress, "international"),
            //    NewsCategory.Create("Business", "business", "Business news and updates", NewsProviderType.AssociatedPress, "business"),
            //    NewsCategory.Create("Technology", "technology", "Technology news and updates", NewsProviderType.AssociatedPress, "technology"),
            //    NewsCategory.Create("Sports", "sports", "Sports news and updates", NewsProviderType.AssociatedPress, "sports"),
            //    NewsCategory.Create("Entertainment", "entertainment", "Entertainment news and updates", NewsProviderType.AssociatedPress, "entertainment"),
            //    NewsCategory.Create("Science", "science", "Science news and updates", NewsProviderType.AssociatedPress, "science"),
            //    NewsCategory.Create("Health", "health", "Health news and updates", NewsProviderType.AssociatedPress, "health"),
            //    NewsCategory.Create("Oddities", "oddities", "Unusual and odd news", NewsProviderType.AssociatedPress, "oddities")
            //});

            await _context.NewsCategories.AddRangeAsync(categories);
            await _context.SaveChangesAsync();
        }
    }
} 