namespace NewsAggregator.Infrastructure.Options
{
    public class DeepSeekOptions
    {
        public const string SectionName = "DeepSeek";
        
        public string ApiEndpoint { get; set; }
        public string ApiKey { get; set; }
        public string ModelName { get; set; } = "deepseek-coder";
    }
} 