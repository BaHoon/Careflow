namespace CareFlow.Core.Models;

public abstract class SoftDeleteEntity<TKey> : EntityBase<TKey>
{
    public bool IsDeleted { get; set; } = false; // true表示已删除
    public DateTime? DeleteTime { get; set; }     // 删除时间
}
