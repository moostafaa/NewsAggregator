using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NewsAggregator.Domain.TextToSpeech.Repositories;
using NewsAggregator.Domain.TextToSpeech.Services;
using NewsAggregator.Infrastructure.TextToSpeech.Persistence;
using NewsAggregator.Infrastructure.TextToSpeech.Repositories;
using NewsAggregator.Infrastructure.TextToSpeech.Services;
using NewsAggregator.Domain.News.Repositories;
using NewsAggregator.Infrastructure.Data;
using NewsAggregator.Infrastructure.Data.Repositories;
using NewsAggregator.Infrastructure.Data.Seeders;
using NewsAggregator.Domain.News.Services;
using NewsAggregator.Infrastructure.News.Services;
using NewsAggregator.Infrastructure.News.Providers;
using NewsAggregator.Infrastructure.BackgroundServices;
using NewsAggregator.Infrastructure.Options;
using System;

namespace NewsAggregator.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Database Contexts
            services.AddDbContext<NewsAggregatorDbContext>(options =>
                options.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection"),
                    b => b.MigrationsAssembly(typeof(NewsAggregatorDbContext).Assembly.FullName)));
                    
            services.AddDbContext<TextToSpeechDbContext>(options =>
                options.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection"),
                    b => b.MigrationsAssembly(typeof(TextToSpeechDbContext).Assembly.FullName)));

            // Repositories
            services.AddScoped<INewsArticleRepository, NewsArticleRepository>();
            services.AddScoped<IRssSourceRepository, RssSourceRepository>();
            services.AddScoped<INewsCategoryRepository, NewsCategoryRepository>();
            services.AddScoped<ITextToSpeechRepository, TextToSpeechRepository>();
            
            // News services
            services.AddScoped<INewsService, NewsService>();
            services.AddScoped<INewsArticleService, NewsArticleService>();
            services.AddScoped<ICategoryClassificationService, DeepSeekCategoryClassificationService>();
            services.AddScoped<INewsCrawlerService, NewsCrawlerService>();

            // Register HTTP client
            services.AddHttpClient<NewsCrawlerService>(client => 
            {
                client.DefaultRequestHeaders.Add("User-Agent", "NewsAggregator/1.0");
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            // Register database seeder
            services.AddScoped<NewsCategorySeeder>();

            // Register options
            services.Configure<DeepSeekOptions>(configuration.GetSection(DeepSeekOptions.SectionName));
            services.Configure<CrawlerOptions>(configuration.GetSection(CrawlerOptions.SectionName));
            
            // Register background services
            services.AddHostedService<NewsCrawlerBackgroundService>();

            // Add news providers
            AddNewsProviders(services);
            
            // Add text-to-speech services
            AddTextToSpeechServices(services);

            return services;
        }
        
        private static void AddNewsProviders(IServiceCollection services)
        {
            // Register News Providers
            services.AddHttpClient<NewsApiProvider>();
            services.AddHttpClient<GuardianNewsProvider>();
            services.AddHttpClient<ReutersNewsProvider>();
            services.AddHttpClient<AssociatedPressProvider>();
            
            services.AddTransient<INewsProvider, NewsApiProvider>();
            services.AddTransient<INewsProvider, GuardianNewsProvider>();
            services.AddTransient<INewsProvider, ReutersNewsProvider>();
            services.AddTransient<INewsProvider, AssociatedPressProvider>();
        }
        
        private static void AddTextToSpeechServices(IServiceCollection services)
        {
            // Register Text-to-Speech Services
            services.AddTransient<AwsTextToSpeechService>();
            services.AddTransient<AzureTextToSpeechService>();
            services.AddTransient<GoogleTextToSpeechService>();
            services.AddTransient<ITextToSpeechFactory, TextToSpeechFactory>();
            
            // Register providers as a tuple collection for injection
            services.AddTransient<IEnumerable<(string ProviderName, ITextToSpeechProvider Provider)>>(sp => 
            {
                return new List<(string, ITextToSpeechProvider)>
                {
                    ("Azure", sp.GetRequiredService<AzureTextToSpeechService>()),
                    ("AWS", sp.GetRequiredService<AwsTextToSpeechService>()),
                    ("Google", sp.GetRequiredService<GoogleTextToSpeechService>())
                };
            });
        }
    }
} 