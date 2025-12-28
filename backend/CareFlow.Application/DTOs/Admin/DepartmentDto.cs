namespace CareFlow.Application.DTOs.Admin
{
    /// <summary>
    /// 科室查询请求DTO
    /// </summary>
    public class QueryDepartmentRequestDto
    {
        /// <summary>
        /// 关键词搜索（科室名称或位置）
        /// </summary>
        public string? Keyword { get; set; }

        /// <summary>
        /// 页码
        /// </summary>
        public int PageIndex { get; set; } = 1;

        /// <summary>
        /// 每页大小
        /// </summary>
        public int PageSize { get; set; } = 20;
    }

    /// <summary>
    /// 科室响应DTO
    /// </summary>
    public class DepartmentDto
    {
        public string DepartmentId { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string? Location { get; set; }
        public int BedCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>
    /// 创建/更新科室DTO
    /// </summary>
    public class SaveDepartmentDto
    {
        public string? DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public string? Location { get; set; }
    }
}
