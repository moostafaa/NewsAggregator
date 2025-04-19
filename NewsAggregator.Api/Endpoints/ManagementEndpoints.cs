using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Authorization;
using NewsAggregator.Domain.News.Entities;
using NewsAggregator.Domain.News.Services;
using NewsAggregator.Domain.News.Enums;
using NewsAggregator.Domain.Management.Models;
using NewsAggregator.Domain.Management.Services;
using NewsAggregator.Domain.Management.Entities;

namespace NewsAggregator.Api.Endpoints;

public static class ManagementEndpoints
{
    public static IEndpointRouteBuilder MapManagementEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/management")
            .WithTags("Management")
            .WithOpenApi()
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });

        // Category Management
        var categories = group.MapGroup("/categories");

        categories.MapGet("/", async Task<Results<Ok<IEnumerable<NewsCategory>>, BadRequest<ErrorResponse>>> (
            INewsCategoryService categoryService) =>
        {
            try
            {
                var result = await categoryService.GetAllAsync();
                return TypedResults.Ok(result);
            }
            catch (Exception ex)
            {
                return TypedResults.BadRequest(new ErrorResponse(ex.Message));
            }
        })
        .WithName("GetAllCategories");

        categories.MapGet("/{id}", async Task<Results<Ok<NewsCategory>, NotFound, BadRequest<ErrorResponse>>> (
            INewsCategoryService categoryService,
            Guid id) =>
        {
            try
            {
                var category = await categoryService.GetByIdAsync(id);
                return category is null ? TypedResults.NotFound() : TypedResults.Ok(category);
            }
            catch (Exception ex)
            {
                return TypedResults.BadRequest(new ErrorResponse(ex.Message));
            }
        })
        .WithName("GetCategory");

        categories.MapPost("/", async Task<Results<Created<NewsCategory>, BadRequest<ErrorResponse>>> (
            INewsCategoryService categoryService,
            CategoryCreateModel model) =>
        {
            try
            {
                var category = await categoryService.CreateAsync(
                    model.Name,
                    model.Description,
                    model.ProviderType,
                    model.ProviderSpecificKey);

                return TypedResults.Created($"/api/management/categories/{category.Id}", category);
            }
            catch (Exception ex)
            {
                return TypedResults.BadRequest(new ErrorResponse(ex.Message));
            }
        })
        .WithName("CreateCategory");

        categories.MapPut("/{id}", async Task<Results<Ok<NewsCategory>, NotFound, BadRequest<ErrorResponse>>> (
            INewsCategoryService categoryService,
            Guid id,
            CategoryUpdateModel model) =>
        {
            try
            {
                var category = await categoryService.UpdateAsync(
                    id,
                    model.Name,
                    model.Description,
                    model.ProviderSpecificKey,
                    model.IsActive);

                return TypedResults.Ok(category);
            }
            catch (Exception ex)
            {
                return TypedResults.BadRequest(new ErrorResponse(ex.Message));
            }
        })
        .WithName("UpdateCategory");

        categories.MapDelete("/{id}", async Task<Results<NoContent, BadRequest<ErrorResponse>>> (
            INewsCategoryService categoryService,
            Guid id) =>
        {
            try
            {
                await categoryService.DeleteAsync(id);
                return TypedResults.NoContent();
            }
            catch (Exception ex)
            {
                return TypedResults.BadRequest(new ErrorResponse(ex.Message));
            }
        })
        .WithName("DeleteCategory");

        categories.MapPost("/{id}/activate", async Task<Results<Ok<NewsCategory>, BadRequest<ErrorResponse>>> (
            INewsCategoryService categoryService,
            Guid id) =>
        {
            try
            {
                var category = await categoryService.ActivateAsync(id);
                return TypedResults.Ok(category);
            }
            catch (Exception ex)
            {
                return TypedResults.BadRequest(new ErrorResponse(ex.Message));
            }
        })
        .WithName("ActivateCategory");

        categories.MapPost("/{id}/deactivate", async Task<Results<Ok<NewsCategory>, BadRequest<ErrorResponse>>> (
            INewsCategoryService categoryService,
            Guid id) =>
        {
            try
            {
                var category = await categoryService.DeactivateAsync(id);
                return TypedResults.Ok(category);
            }
            catch (Exception ex)
            {
                return TypedResults.BadRequest(new ErrorResponse(ex.Message));
            }
        })
        .WithName("DeactivateCategory");

        // Provider Configuration
        var providers = group.MapGroup("/providers/config");

        providers.MapGet("/", async Task<Results<Ok<IEnumerable<ProviderConfig>>, BadRequest<ErrorResponse>>> (
            INewsProviderConfigService providerConfigService) =>
        {
            try
            {
                var configs = await providerConfigService.GetAllConfigsAsync();
                return TypedResults.Ok(configs);
            }
            catch (Exception ex)
            {
                return TypedResults.BadRequest(new ErrorResponse(ex.Message));
            }
        })
        .WithName("GetProvidersConfig");

        providers.MapPut("/{providerType}", async Task<Results<Ok<ProviderConfig>, BadRequest<ErrorResponse>>> (
            INewsProviderConfigService providerConfigService,
            NewsProviderType providerType,
            ProviderConfigUpdateModel model) =>
        {
            try
            {
                var config = await providerConfigService.UpdateConfigAsync(
                    providerType,
                    model.ApiKey,
                    model.BaseUrl,
                    model.AdditionalSettings);

                return TypedResults.Ok(config);
            }
            catch (Exception ex)
            {
                return TypedResults.BadRequest(new ErrorResponse(ex.Message));
            }
        })
        .WithName("UpdateProviderConfig");

        // Cloud Configuration
        var cloud = group.MapGroup("/cloud/config");

        cloud.MapGet("/", async Task<Results<Ok<CloudConfiguration>, BadRequest<ErrorResponse>>> (
            ICloudConfigurationService cloudConfigService) =>
        {
            try
            {
                var config = await cloudConfigService.GetConfigurationAsync();
                return TypedResults.Ok(config);
            }
            catch (Exception ex)
            {
                return TypedResults.BadRequest(new ErrorResponse(ex.Message));
            }
        })
        .WithName("GetCloudConfig");

        cloud.MapPut("/", async Task<Results<Ok<CloudConfiguration>, BadRequest<ErrorResponse>>> (
            ICloudConfigurationService cloudConfigService,
            CloudConfigurationUpdateModel model) =>
        {
            try
            {
                var config = await cloudConfigService.UpdateConfigurationAsync(
                    model.Provider,
                    model.Credentials,
                    model.Region,
                    model.Settings);

                return TypedResults.Ok(config);
            }
            catch (Exception ex)
            {
                return TypedResults.BadRequest(new ErrorResponse(ex.Message));
            }
        })
        .WithName("UpdateCloudConfig");

        return app;
    }
} 