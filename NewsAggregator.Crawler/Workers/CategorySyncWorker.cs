using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NewsAggregator.Crawler.Options;
using NewsAggregator.Crawler.Services;

namespace NewsAggregator.Crawler.Workers
{
    /// <summary>
    /// Background worker that periodically synchronizes categories from the main application
    /// </summary>
    public class CategorySyncWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CategorySyncWorker> _logger;
        private readonly DistributedCrawlerOptions _options;
        private readonly TimeSpan _syncInterval = TimeSpan.FromMinutes(30); // Sync categories every 30 minutes

        public CategorySyncWorker(
            IServiceProvider serviceProvider,
            IOptions<DistributedCrawlerOptions> options,
            ILogger<CategorySyncWorker> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Category sync worker starting");

            // Initial sync on startup
            await SyncCategoriesAsync(stoppingToken);

            // Periodically sync categories
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_syncInterval, stoppingToken);
                    await SyncCategoriesAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // Graceful shutdown
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in category sync worker");
                }
            }

            _logger.LogInformation("Category sync worker stopping");
        }

        private async Task SyncCategoriesAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Syncing categories from the main application");

                // Create a scope for the service provider
                using var scope = _serviceProvider.CreateScope();
                var categoryService = scope.ServiceProvider.GetRequiredService<ICategoryService>();

                // Refresh categories
                int updateCount = await categoryService.RefreshCategoriesAsync(cancellationToken);
                _logger.LogInformation("Synchronized {Count} categories", updateCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing categories");
            }
        }
    }
} 