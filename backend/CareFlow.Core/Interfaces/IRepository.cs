using CareFlow.Core.Models;
using System.Linq.Expressions;

namespace CareFlow.Core.Interfaces;

// T 是实体类型（比如 Doctor），TKey 是主键类型（比如 Guid 或 int）
// where T : EntityBase<TKey> 限制了 T 必须是我们定义的实体，不能是随便一个类
public interface IRepository<T, TKey> where T : EntityBase<TKey>
{
    // 根据ID查找
    Task<T?> GetByIdAsync(TKey id);

    // 根据条件查找单个实体
    Task<T?> GetAsync(Expression<Func<T, bool>> predicate);

    // 获取列表 (最基础版，后面我们会教你分页)
    Task<List<T>> ListAsync();

    // 获取列表，根据条件
    Task<List<T>> ListAsync(Expression<Func<T, bool>> predicate);

    // 添加实体
    Task<T> AddAsync(T entity);

    // 更新实体
    Task UpdateAsync(T entity);

    // 删除实体
    Task DeleteAsync(T entity);
}