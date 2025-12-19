using Microsoft.EntityFrameworkCore;

namespace CareFlow.Application.Interfaces
{
    public interface ICareFlowDbContext
    {
        // 这是一个万能方法，让你可以用 _context.Set<Patient>()
        DbSet<TEntity> Set<TEntity>() where TEntity : class; 
        
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}