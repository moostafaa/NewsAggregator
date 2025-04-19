using Microsoft.Extensions.DependencyInjection;
using NewsAggregator.Application.TextToSpeech;
using NewsAggregator.Domain.TextToSpeech.Services;

namespace NewsAggregator.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Register application services
            services.AddScoped<ITextToSpeechService, TextToSpeechService>();
            
            return services;
        }
    }
} 