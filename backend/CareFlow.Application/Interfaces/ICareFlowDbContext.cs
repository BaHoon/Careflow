using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace CareFlow.Application.Interfaces
{
    public interface ICareFlowDbContext
    {
        // 这是一个万能方法，让你可以用 _context.Set<Patient>()
        DbSet<TEntity> Set<TEntity>() where TEntity : class; 
        
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        
        // 开始数据库事务
        Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
    }
}