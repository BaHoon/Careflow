using CareFlow.Core.Models;
using System;
using System.Threading.Tasks;

namespace CareFlow.Core.Interfaces
{
    public interface IRepository<TEntity> where TEntity : EntityBase
    {
        Task<TEntity?> GetByIdAsync(Guid id);
        Task AddAsync(TEntity entity);
        Task UpdateAsync(TEntity entity);
        Task DeleteAsync(Guid id);
    }
}
