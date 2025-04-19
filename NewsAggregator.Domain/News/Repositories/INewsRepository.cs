using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NewsAggregator.Domain.Common;
using NewsAggregator.Domain.News.Aggregates;
using NewsAggregator.Domain.News.Entities;

namespace NewsAggregator.Domain.News.Repositories
{
    public interface INewsRepository : IRepository<NewsAggregate>
    {
        Task<IEnumerable<NewsArticle>> GetArticlesByCategoryAsync(string category);
        Task<IEnumerable<NewsArticle>> GetLatestArticlesAsync(int count);
        Task<IEnumerable<NewsArticle>> SearchArticlesAsync(string query);
        Task<bool> ArticleExistsAsync(Uri url);
    }
} 