using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NewsAggregator.Domain.News.Entities;
using NewsAggregator.Domain.News.Repositories;
using NewsAggregator.Domain.News.Services;
using NewsAggregator.Domain.News.ValueObjects;

namespace NewsAggregator.Api.Controllers
{
    [ApiController]
    [Route("api/articles")]
    public class ArticlesController : ControllerBase
    {
        private readonly INewsArticleRepository _articleRepository;
        private readonly ICategoryClassificationService _categoryClassificationService;
        private readonly ILogger<ArticlesController> _logger;

        public ArticlesController(
            INewsArticleRepository articleRepository,
            ICategoryClassificationService categoryClassificationService,
            ILogger<ArticlesController> logger)
        {
            _articleRepository = articleRepository ?? throw new ArgumentNullException(nameof(articleRepository));
            _categoryClassificationService = categoryClassificationService ?? throw new ArgumentNullException(nameof(categoryClassificationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public class ArticleSubmissionDto
        {
            public string Title { get; set; }
            public string Content { get; set; }
            public string Url { get; set; }
            public string Author { get; set; }
            public DateTime? PublishedAt { get; set; }
            public string SourceName { get; set; }
            public string SourceUrl { get; set; }
            public List<string> Categories { get; set; } = new List<string>();
            public string ImageUrl { get; set; }
            public string Summary { get; set; }
        }

        public class ArticleBatchSubmissionDto
        {
            public List<ArticleSubmissionDto> Articles { get; set; } = new List<ArticleSubmissionDto>();
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateArticle(
            [FromBody] ArticleSubmissionDto dto,
            CancellationToken cancellationToken)
        {
            if (dto == null)
            {
                return BadRequest("Article data is required");
            }

            try
            {
                var articleId = await ProcessAndSaveArticle(dto, cancellationToken);
                return CreatedAtAction(nameof(GetArticle), new { id = articleId }, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating article");
                return StatusCode(500, "An error occurred while creating the article");
            }
        }

        [HttpPost("batch")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateArticleBatch(
            [FromBody] ArticleBatchSubmissionDto dto,
            CancellationToken cancellationToken)
        {
            if (dto == null || dto.Articles.Count == 0)
            {
                return BadRequest("Article data is required");
            }

            try
            {
                int successCount = 0;
                int failureCount = 0;
                
                foreach (var article in dto.Articles)
                {
                    try
                    {
                        await ProcessAndSaveArticle(article, cancellationToken);
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing article in batch: {Title}", article.Title);
                        failureCount++;
                    }
                }

                return Ok(new { 
                    message = $"Processed {dto.Articles.Count} articles. Success: {successCount}, Failed: {failureCount}",
                    successCount,
                    failureCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing article batch");
                return StatusCode(500, "An error occurred while processing the article batch");
            }
        }

        private async Task<Guid> ProcessAndSaveArticle(ArticleSubmissionDto dto, CancellationToken cancellationToken)
        {
            // Check if article already exists
            var existingArticle = await _articleRepository.GetByUrlAsync(dto.Url, cancellationToken);
            if (existingArticle != null)
            {
                _logger.LogInformation("Article already exists: {Url}", dto.Url);
                return existingArticle.Id;
            }

            // Create article
            var article = NewsArticle.Create(
                dto.Title,
                dto.Summary ?? string.Empty,
                dto.Content,
                NewsSource.Create(
                    dto.SourceName,
                    dto.SourceUrl,
                    dto.Categories
                ),
                dto.PublishedAt ?? DateTime.UtcNow,
                "general",
                dto.Url,
                null
            );

            // Classify categories if not provided
            if (dto.Categories == null || dto.Categories.Count == 0)
            {
                var category = await _categoryClassificationService.ClassifyArticleAsync(
                    article.Title,
                    article.Body,
                    dto.SourceName,
                    null
                );
                
                article.UpdateCategory(category);
            }

            // Save article
            await _articleRepository.AddAsync(article, cancellationToken);
            
            return article.Id;
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(NewsArticle), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetArticle(Guid id, CancellationToken cancellationToken)
        {
            var article = await _articleRepository.GetByIdAsync(id, cancellationToken);
            if (article == null)
            {
                return NotFound();
            }

            return Ok(article);
        }
    }
} 