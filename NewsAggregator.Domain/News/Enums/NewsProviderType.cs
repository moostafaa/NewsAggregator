using System;

namespace NewsAggregator.Domain.News.Enums
{
    public enum NewsProviderType
    {
        RSS,
        API,
        Scraper,
        Custom,
        NewsAPI,
        TheGuardian,
        NewYorkTimes,
        Reuters,
        AssociatedPress,
        Bloomberg,
        BBC,
        CNN,
        AlJazeera,
        WSJ,
        FinancialTimes,
        AWS,
        Azure
    }

    public static class NewsProviderTypeExtensions
    {
        public static bool TryParse(string? value, IFormatProvider? provider, out NewsProviderType result)
        {
            if (Enum.TryParse<NewsProviderType>(value, true, out result))
            {
                return true;
            }
            result = default;
            return false;
        }

        public static NewsProviderType Parse(string s, IFormatProvider? provider)
        {
            if (TryParse(s, provider, out var result))
            {
                return result;
            }
            throw new FormatException($"Could not parse NewsProviderType from '{s}'");
        }
    }
}