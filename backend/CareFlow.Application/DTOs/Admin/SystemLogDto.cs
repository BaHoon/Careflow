namespace CareFlow.Application.DTOs.Admin
{
    /// <summary>
    /// 系统日志查询请求DTO
    /// </summary>
    public class QuerySystemLogRequestDto
    {
        /// <summary>
        /// 操作类型
        /// </summary>
        public string? OperationType { get; set; }

        /// <summary>
        /// 操作人姓名
        /// </summary>
        public string? OperatorName { get; set; }

        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// 结束时间
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// 操作结果（Success/Failed）
        /// </summary>
        public string? Result { get; set; }

        /// <summary>
        /// 页码
        /// </summary>
        public int PageIndex { get; set; } = 1;

        /// <summary>
        /// 每页大小
        /// </summary>
        public int PageSize { get; set; } = 50;
    }

    /// <summary>
    /// 系统日志响应DTO
    /// </summary>
    public class SystemLogDto
    {
        public int LogId { get; set; }
        public string OperationType { get; set; } = string.Empty;
        public int? OperatorId { get; set; }
        public string? OperatorName { get; set; }
        public DateTime OperationTime { get; set; }
        public string? OperationDetails { get; set; }
        public string? IpAddress { get; set; }
        public string Result { get; set; } = "Success";
        public string? ErrorMessage { get; set; }
        public string? EntityType { get; set; }
        public int? EntityId { get; set; }
    }

    /// <summary>
    /// 创建系统日志DTO
    /// </summary>
    public class CreateSystemLogDto
    {
        public string OperationType { get; set; } = string.Empty;
        public int? OperatorId { get; set; }
        public string? OperatorName { get; set; }
        public string? OperationDetails { get; set; }
        public string? IpAddress { get; set; }
        public string Result { get; set; } = "Success";
        public string? ErrorMessage { get; set; }
        public string? EntityType { get; set; }
        public int? EntityId { get; set; }
    }
}
