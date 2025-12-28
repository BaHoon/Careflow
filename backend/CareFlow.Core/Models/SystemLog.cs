using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CareFlow.Core.Models
{
    /// <summary>
    /// 系统操作日志实体
    /// </summary>
    [Table("system_logs")]
    public class SystemLog
    {
        /// <summary>
        /// 日志ID
        /// </summary>
        [Key]
        [Column("log_id")]
        public int LogId { get; set; }

        /// <summary>
        /// 操作类型
        /// </summary>
        [Required]
        [MaxLength(50)]
        [Column("operation_type")]
        public string OperationType { get; set; } = string.Empty;

        /// <summary>
        /// 操作人ID
        /// </summary>
        [Column("operator_id")]
        public int? OperatorId { get; set; }

        /// <summary>
        /// 操作人姓名
        /// </summary>
        [MaxLength(100)]
        [Column("operator_name")]
        public string? OperatorName { get; set; }

        /// <summary>
        /// 操作时间
        /// </summary>
        [Column("operation_time")]
        public DateTime OperationTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 操作详情
        /// </summary>
        [Column("operation_details")]
        public string? OperationDetails { get; set; }

        /// <summary>
        /// IP地址
        /// </summary>
        [MaxLength(50)]
        [Column("ip_address")]
        public string? IpAddress { get; set; }

        /// <summary>
        /// 操作结果 (Success/Failed)
        /// </summary>
        [MaxLength(20)]
        [Column("result")]
        public string Result { get; set; } = "Success";

        /// <summary>
        /// 错误信息（如果操作失败）
        /// </summary>
        [Column("error_message")]
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 关联实体类型
        /// </summary>
        [MaxLength(50)]
        [Column("entity_type")]
        public string? EntityType { get; set; }

        /// <summary>
        /// 关联实体ID
        /// </summary>
        [Column("entity_id")]
        public int? EntityId { get; set; }
    }
}
