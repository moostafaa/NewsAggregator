namespace NewsAggregator.Domain.Auth.Models
{
    public class AuthResult
    {
        public string AccessToken { get; set; }
        public string TokenType { get; set; }
        public int ExpiresIn { get; set; }
        public UserInfo User { get; set; }
    }
} 