using System.Collections.Generic;

namespace NewsAggregator.Domain.Auth.Models
{
    public class UserInfo
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public string Picture { get; set; }
        public IEnumerable<string> Roles { get; set; }
    }
} 