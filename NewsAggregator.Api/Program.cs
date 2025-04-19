using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using NewsAggregator.Api.Endpoints;
using NewsAggregator.Application;
using NewsAggregator.Infrastructure;
using NewsAggregator.Domain.News.Entities;
using NewsAggregator.Domain.News.Enums;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add authorization services
builder.Services.AddAuthorization();

// Add gRPC services
builder.Services.AddGrpc(options =>
{
    options.EnableDetailedErrors = true;
    options.MaxReceiveMessageSize = 16 * 1024 * 1024; // 16 MB
});

// Configure authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var jwtSettings = builder.Configuration.GetSection("Authentication:JwtBearer");
    var key = Encoding.ASCII.GetBytes(jwtSettings["SecurityKey"]!);

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
})
.AddGoogle(options =>
{
    var googleAuth = builder.Configuration.GetSection("Authentication:Google");
    options.ClientId = googleAuth["ClientId"]!;
    options.ClientSecret = googleAuth["ClientSecret"]!;
})
.AddFacebook(options =>
{
    var facebookAuth = builder.Configuration.GetSection("Authentication:Facebook");
    options.AppId = facebookAuth["AppId"]!;
    options.AppSecret = facebookAuth["AppSecret"]!;
})
.AddGitHub(options =>
{
    var githubAuth = builder.Configuration.GetSection("Authentication:GitHub");
    options.ClientId = githubAuth["ClientId"]!;
    options.ClientSecret = githubAuth["ClientSecret"]!;
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader());
});

// Add application services
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

// Map endpoints
app.MapNewsEndpoints();
app.MapManagementEndpoints();
app.MapAuthEndpoints();

// Map gRPC services
app.MapGrpcService<NewsAggregator.Api.Services.GrpcCategoryService>();

// Initialize the application
// Commented out until seeder is properly implemented
/*
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var categorySeeder = services.GetRequiredService<NewsCategorySeeder>();
    await categorySeeder.SeedAsync();
}
*/

// Initialize default news sources
// Commented out until classes are properly implemented
/*
using (var scope = app.Services.CreateScope())
{
    var crawlerService = scope.ServiceProvider.GetRequiredService<INewsCrawlerService>();
    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        var defaultSources = configuration.GetSection("DefaultSources")
            .Get<List<DefaultSourceConfig>>() ?? new List<DefaultSourceConfig>();

        logger.LogInformation("Initializing {Count} default news sources", defaultSources.Count);

        foreach (var sourceConfig in defaultSources)
        {
            if (Uri.TryCreate(sourceConfig.Url, UriKind.Absolute, out var uri))
            {
                var source = NewsSource.Create(
                    sourceConfig.Name,
                    sourceConfig.Url,
                    sourceConfig.Categories);

                await crawlerService.AddSourceAsync(source);
                logger.LogInformation("Added source: {Name}", sourceConfig.Name);
            }
            else
            {
                logger.LogWarning("Invalid source URL: {Url}", sourceConfig.Url);
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error initializing default news sources");
    }
}
*/

app.Run();

public class DefaultSourceConfig
{
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public List<string> Categories { get; set; } = new List<string>();
} 