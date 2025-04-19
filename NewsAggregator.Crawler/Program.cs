using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NewsAggregator.Crawler.Coordination;
using NewsAggregator.Crawler.Data;
using NewsAggregator.Crawler.Options;
using NewsAggregator.Crawler.Protos;
using NewsAggregator.Crawler.Services;
using NewsAggregator.Crawler.Workers;
using NewsAggregator.Domain.News.Services;
using StackExchange.Redis;

namespace NewsAggregator.Crawler
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    var configuration = hostContext.Configuration;
                    
                    // Register options
                    services.Configure<DistributedCrawlerOptions>(
                        configuration.GetSection(DistributedCrawlerOptions.SectionName));
                    services.Configure<CrawlerOptions>(
                        configuration.GetSection("Crawler"));
                    
                    // Configure SQL Server database
                    services.AddDbContext<NewsCrawlerDbContext>(options =>
                        options.UseSqlServer(
                            configuration.GetConnectionString("DefaultConnection"),
                            b => b.MigrationsAssembly("NewsAggregator.Crawler")));
                    
                    // Configure gRPC client
                    var distributedCrawlerOptions = configuration
                        .GetSection(DistributedCrawlerOptions.SectionName)
                        .Get<DistributedCrawlerOptions>();
                    
                    services.AddGrpcClient<CategoryService.CategoryServiceClient>(options =>
                    {
                        options.Address = new Uri(distributedCrawlerOptions.ApiEndpoint);
                    }).ConfigurePrimaryHttpMessageHandler(() =>
                    {
                        var handler = new HttpClientHandler();
                        
                        // Allow untrusted certificates for development
                        if (hostContext.HostingEnvironment.IsDevelopment())
                        {
                            handler.ServerCertificateCustomValidationCallback = 
                                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                        }
                        
                        return handler;
                    });
                    
                    // Register HTTP clients
                    services.AddHttpClient<ISourceService, ApiSourceService>();
                    services.AddHttpClient<IArticlePublisher, ApiArticlePublisher>();
                    services.AddHttpClient();
                    
                    // Register services based on configuration
                    // Register the appropriate coordinator based on configuration
                    switch (distributedCrawlerOptions.CoordinationMode.ToLowerInvariant())
                    {
                        case "redis":
                            services.AddSingleton<IConnectionMultiplexer>(sp =>
                            {
                                var redisConnection = configuration.GetConnectionString("Redis");
                                return ConnectionMultiplexer.Connect(redisConnection);
                            });
                            services.AddScoped<IWorkCoordinator, RedisWorkCoordinator>();
                            break;
                            
                        case "local":
                        default:
                            services.AddSingleton<IWorkCoordinator, LocalWorkCoordinator>();
                            break;
                    }
                    
                    // Register category service
                    services.AddScoped<ICategoryService, GrpcCategoryService>();
                    
                    // Register domain services
                    services.AddScoped<ICategoryClassificationService, GrpcCategoryClassificationService>();
                    
                    // Register the background workers
                    services.AddHostedService<DistributedCrawlerWorker>();
                    services.AddHostedService<CategorySyncWorker>();
                    
                    services.AddLogging(builder =>
                    {
                        builder.AddConsole();
                        builder.AddDebug();
                    });
                });
    }
    
    /// <summary>
    /// Implementation of ICategoryClassificationService that uses the gRPC category service
    /// </summary>
    public class GrpcCategoryClassificationService : ICategoryClassificationService
    {
        private readonly ICategoryService _categoryService;
        private readonly ILogger<GrpcCategoryClassificationService> _logger;
        
        public GrpcCategoryClassificationService(
            ICategoryService categoryService,
            ILogger<GrpcCategoryClassificationService> logger)
        {
            _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        public async Task<string> ClassifyArticleAsync(
            string title, 
            string content, 
            string sourceName = null, 
            string sourceCategory = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var category = await _categoryService.ClassifyArticleAsync(
                    title, content, sourceName, sourceCategory, cancellationToken);
                
                return category?.Name ?? "Uncategorized";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error classifying article: {Title}", title);
                return "Uncategorized";
            }
        }
        
        public async Task<string> MapSourceCategoryAsync(
            string sourceName, 
            string sourceCategory,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var category = await _categoryService.ClassifyArticleAsync(
                    string.Empty, string.Empty, sourceName, sourceCategory, cancellationToken);
                
                return category?.Name ?? "Uncategorized";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error mapping source category: {SourceName}, {Category}", 
                    sourceName, sourceCategory);
                return "Uncategorized";
            }
        }
        
        public async Task<IEnumerable<string>> GetValidCategoriesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var categories = await _categoryService.GetCategoriesAsync(
                    includeInactive: false, cancellationToken: cancellationToken);
                
                return categories.Select(c => c.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting valid categories");
                return new List<string> { "Uncategorized" };
            }
        }
    }
} 