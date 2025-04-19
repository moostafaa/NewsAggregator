namespace NewsAggregator.Domain.Auth.Models
{
    public class ExternalLoginInfo
    {
        public string Provider { get; set; }
        public string ProviderId { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public string Picture { get; set; }
    }
} 