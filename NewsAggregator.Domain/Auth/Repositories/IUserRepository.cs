using System.Threading.Tasks;
using NewsAggregator.Domain.Auth.Entities;
using NewsAggregator.Domain.Common;

namespace NewsAggregator.Domain.Auth.Repositories
{
    public interface IUserRepository : IRepository<User>
    {
        Task<User> GetByEmailAsync(string email);
        Task<User> GetByExternalProviderAsync(string providerName, string providerId);
        Task<bool> ExistsWithEmailAsync(string email);
    }
} 