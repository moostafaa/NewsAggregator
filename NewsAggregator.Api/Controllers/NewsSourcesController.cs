using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NewsAggregator.Domain.News.Services;
using NewsAggregator.Domain.News.ValueObjects;

namespace NewsAggregator.Api.Controllers
{
    [ApiController]
    [Route("api/sources")]
    public class NewsSourcesController : ControllerBase
    {
        private readonly INewsCrawlerService _crawlerService;
        private readonly ILogger<NewsSourcesController> _logger;

        public NewsSourcesController(
            INewsCrawlerService crawlerService,
            ILogger<NewsSourcesController> logger)
        {
            _crawlerService = crawlerService ?? throw new ArgumentNullException(nameof(crawlerService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<NewsSource>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSources(CancellationToken cancellationToken)
        {
            try
            {
                // Use reflection to access the private _sources field from the NewsCrawlerService
                // This is a bit of a hack but allows us to get the sources without modifying the domain interface
                var sourcesProperty = _crawlerService.GetType().GetField("_sources", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (sourcesProperty != null)
                {
                    var sources = sourcesProperty.GetValue(_crawlerService) as List<NewsSource>;
                    return Ok(sources);
                }
                
                // Fallback if we can't get the sources through reflection
                _logger.LogWarning("Could not access sources through reflection, returning empty list");
                return Ok(Enumerable.Empty<NewsSource>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sources");
                return StatusCode(500, "An error occurred while retrieving sources");
            }
        }

        [HttpGet("filter")]
        [ProducesResponseType(typeof(IEnumerable<NewsSource>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSourcesByFilter(
            [FromQuery] string category = null,
            [FromQuery] string providerType = null,
            [FromQuery] int limit = 100,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Same approach as above, but with filtering
                var sourcesProperty = _crawlerService.GetType().GetField("_sources", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (sourcesProperty != null)
                {
                    var allSources = sourcesProperty.GetValue(_crawlerService) as List<NewsSource>;
                    var filteredSources = allSources;
                    
                    // Apply category filter if provided
                    if (!string.IsNullOrEmpty(category))
                    {
                        filteredSources = filteredSources.Where(s => 
                            s.Categories.Contains(category, StringComparer.OrdinalIgnoreCase)).ToList();
                    }
                    
                    // Apply provider type filter if provided
                    if (!string.IsNullOrEmpty(providerType))
                    {
                        // This is a best-effort approach since we don't have direct access to provider type
                        filteredSources = filteredSources.Where(s => 
                            s.Name.Contains(providerType, StringComparison.OrdinalIgnoreCase)).ToList();
                    }
                    
                    // Apply limit
                    filteredSources = filteredSources.Take(limit).ToList();
                    
                    return Ok(filteredSources);
                }
                
                _logger.LogWarning("Could not access sources through reflection, returning empty list");
                return Ok(Enumerable.Empty<NewsSource>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting filtered sources");
                return StatusCode(500, "An error occurred while retrieving sources");
            }
        }
    }
} 