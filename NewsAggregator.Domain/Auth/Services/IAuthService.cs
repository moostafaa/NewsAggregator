using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Claims;
using NewsAggregator.Domain.Auth.Models;

namespace NewsAggregator.Domain.Auth.Services
{
    public interface IAuthService
    {
        Dictionary<string, string> ConfigureExternalAuthenticationProperties(string provider, string redirectUrl);
        Task<AuthResult> HandleExternalLoginCallback();
        Task<UserInfo> GetCurrentUserInfo();
        Task AssignRoleToUser(string userId, string role);
        Task<string> GenerateJwtToken(UserInfo user);
    }

    // Simplified model for AuthenticationProperties
    public class AuthProperties
    {
        public Dictionary<string, string> Items { get; }
        public Dictionary<string, object> Parameters { get; }

        public AuthProperties()
        {
            Items = new Dictionary<string, string>();
            Parameters = new Dictionary<string, object>();
        }
    }
} 