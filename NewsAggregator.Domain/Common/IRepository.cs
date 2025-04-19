using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NewsAggregator.Domain.Common
{
    public interface IRepository<T> where T : AggregateRoot
    {
        Task<T> GetByIdAsync(Guid id);
        Task<IEnumerable<T>> GetAllAsync();
        Task AddAsync(T entity);
        Task UpdateAsync(T entity);
        Task DeleteAsync(T entity);
    }
} 