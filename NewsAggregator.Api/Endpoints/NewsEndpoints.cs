using Microsoft.AspNetCore.Http.HttpResults;
using System;
using System.Collections.Generic;
using NewsAggregator.Domain.News.Entities;
using NewsAggregator.Domain.News.Services;
using NewsAggregator.Domain.News.Enums;

namespace NewsAggregator.Api.Endpoints;

public static class NewsEndpoints
{
    public static IEndpointRouteBuilder MapNewsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/news")
            .WithTags("News")
            .WithOpenApi();

        // Get news with pagination and filters
        group.MapGet("/", async Task<Results<Ok<NewsResponse>, BadRequest<ErrorResponse>>> (
            INewsService newsService,
            string? provider = null,
            string? category = null,
            int page = 1,
            int pageSize = 10) =>
        {
            try
            {
                var articles = await newsService.GetNewsAsync(provider, category, page, pageSize);
                var totalCount = await newsService.GetTotalCountAsync(provider, category);

                var response = new NewsResponse(
                    Articles: articles,
                    TotalCount: totalCount,
                    CurrentPage: page,
                    PageSize: pageSize,
                    TotalPages: (int)Math.Ceiling(totalCount / (double)pageSize)
                );

                return TypedResults.Ok(response);
            }
            catch (Exception ex)
            {
                return TypedResults.BadRequest(new ErrorResponse(ex.Message));
            }
        })
        .WithName("GetNews")
        .WithDescription("Get paginated news articles with optional provider and category filters");

        // Get available providers
        group.MapGet("/providers", async Task<Results<Ok<IEnumerable<NewsProviderInfo>>, BadRequest<ErrorResponse>>> (
            INewsService newsService) =>
        {
            try
            {
                var providers = await newsService.GetAvailableProvidersAsync();
                return TypedResults.Ok(providers);
            }
            catch (Exception ex)
            {
                return TypedResults.BadRequest(new ErrorResponse(ex.Message));
            }
        })
        .WithName("GetProviders");

        // Get categories
        group.MapGet("/categories", async Task<Results<Ok<IEnumerable<NewsCategory>>, BadRequest<ErrorResponse>>> (
            INewsCategoryService categoryService,
            string? provider = null) =>
        {
            try
            {
                var categories = string.IsNullOrEmpty(provider)
                    ? await categoryService.GetAllAsync()
                    : Enum.TryParse<NewsProviderType>(provider, true, out var providerType)
                        ? await categoryService.GetActiveByProviderTypeAsync(providerType)
                        : throw new ArgumentException("Invalid provider type");

                return TypedResults.Ok(categories);
            }
            catch (Exception ex)
            {
                return TypedResults.BadRequest(new ErrorResponse(ex.Message));
            }
        })
        .WithName("GetCategories");

        // Get trending news
        group.MapGet("/trending", async Task<Results<Ok<IEnumerable<NewsArticle>>, BadRequest<ErrorResponse>>> (
            INewsService newsService,
            string? provider = null,
            int limit = 5) =>
        {
            try
            {
                var articles = await newsService.GetTrendingNewsAsync(provider, limit);
                return TypedResults.Ok(articles);
            }
            catch (Exception ex)
            {
                return TypedResults.BadRequest(new ErrorResponse(ex.Message));
            }
        })
        .WithName("GetTrendingNews");

        // Search news
        group.MapGet("/search", async Task<Results<Ok<NewsResponse>, BadRequest<ErrorResponse>>> (
            INewsService newsService,
            string query,
            string? provider = null,
            string? category = null,
            int page = 1,
            int pageSize = 10) =>
        {
            try
            {
                var articles = await newsService.SearchNewsAsync(query, provider, category, page, pageSize);
                var totalCount = await newsService.GetSearchTotalCountAsync(query, provider, category);

                var response = new NewsResponse(
                    Articles: articles,
                    TotalCount: totalCount,
                    CurrentPage: page,
                    PageSize: pageSize,
                    TotalPages: (int)Math.Ceiling(totalCount / (double)pageSize)
                );

                return TypedResults.Ok(response);
            }
            catch (Exception ex)
            {
                return TypedResults.BadRequest(new ErrorResponse(ex.Message));
            }
        })
        .WithName("SearchNews");

        return app;
    }
}

public record NewsResponse(
    IEnumerable<NewsArticle> Articles,
    int TotalCount,
    int CurrentPage,
    int PageSize,
    int TotalPages);

public record ErrorResponse(string Error); 