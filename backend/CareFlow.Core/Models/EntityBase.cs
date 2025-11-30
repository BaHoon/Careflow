using System.ComponentModel.DataAnnotations;

namespace CareFlow.Core.Models;

// 使用泛型 TKey，因为你的表有的用 int (Department)，有的用 Guid (Staff)
public abstract class EntityBase<TKey>
{
    [Key]
    public TKey Id { get; set; } = default!;// 所有表的主键都叫 Id，方便统一管理

    public DateTime CreateTime { get; set; } = DateTime.UtcNow; // 默认创建时间
}