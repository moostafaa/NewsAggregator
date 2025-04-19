using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using NewsAggregator.Domain.News.Enums;
using NewsAggregator.Domain.News.Services;

namespace NewsAggregator.Infrastructure.News.Providers
{
    public class NewsProviderFactory : INewsProviderFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public NewsProviderFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public INewsProvider CreateProvider(NewsProviderType providerType)
        {
            // Get all registered news providers from the service provider
            var providers = _serviceProvider.GetServices<INewsProvider>();
            
            // Find the provider that matches the requested type
            var provider = providers.FirstOrDefault(p => p.ProviderType == providerType);
            
            if (provider == null)
            {
                throw new ArgumentException($"No provider registered for type {providerType}", nameof(providerType));
            }
            
            return provider;
        }
    }
} 