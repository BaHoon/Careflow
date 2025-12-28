namespace CareFlow.Application.DTOs.Admin
{
    /// <summary>
    /// 病床响应DTO
    /// </summary>
    public class BedDto
    {
        public string BedId { get; set; } = string.Empty;
        public string WardId { get; set; } = string.Empty;
        public string WardName { get; set; } = string.Empty;
        public string DepartmentId { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    /// <summary>
    /// 创建病床DTO
    /// </summary>
    public class CreateBedDto
    {
        public string DepartmentId { get; set; } = string.Empty;
        public string WardId { get; set; } = string.Empty;
    }
}
