using CareFlow.Core.Interfaces;
using CareFlow.Core.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace CareFlow.Infrastructure;

// 这是一个泛型仓储，T 必须是继承自 EntityBase 的类
public class EfRepository<T, TKey> : IRepository<T, TKey> where T : EntityBase<TKey>
{
    private readonly ApplicationDbContext _context;
    private readonly DbSet<T> _dbSet;

    public EfRepository(ApplicationDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public async Task<T?> GetByIdAsync(TKey id)
    {
        return await _dbSet.FindAsync(id);
    }

    public async Task<T?> GetAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.FirstOrDefaultAsync(predicate);
    }

    public async Task<List<T>> ListAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public async Task<List<T>> ListAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.Where(predicate).ToListAsync();
    }

    public async Task<T> AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task UpdateAsync(T entity)
    {
        _context.Entry(entity).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(T entity)
    {
        // 如果是软删除实体，只标记，不真删
        if (entity is SoftDeleteEntity<TKey> softDeleteEntity)
        {
            softDeleteEntity.IsDeleted = true;
            softDeleteEntity.DeleteTime = DateTime.UtcNow;
            await UpdateAsync(entity);
        }
        else
        {
            _dbSet.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}