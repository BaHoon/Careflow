using CareFlow.Application.DTOs.Admin;
using CareFlow.Application.DTOs.Common;

namespace CareFlow.Application.Interfaces
{
    /// <summary>
    /// 科室管理服务接口
    /// </summary>
    public interface IDepartmentManagementService
    {
        /// <summary>
        /// 获取科室列表
        /// </summary>
        Task<PagedResultDto<DepartmentDto>> GetDepartmentListAsync(QueryDepartmentRequestDto request);

        /// <summary>
        /// 根据ID获取科室详情
        /// </summary>
        Task<DepartmentDto?> GetDepartmentByIdAsync(string departmentId);

        /// <summary>
        /// 创建科室
        /// </summary>
        Task<DepartmentDto> CreateDepartmentAsync(SaveDepartmentDto dto, string? operatorId = null, string? operatorName = null, string? ipAddress = null);

        /// <summary>
        /// 更新科室
        /// </summary>
        Task<DepartmentDto> UpdateDepartmentAsync(string departmentId, SaveDepartmentDto dto, string? operatorId = null, string? operatorName = null, string? ipAddress = null);

        /// <summary>
        /// 删除科室
        /// </summary>
        Task<bool> DeleteDepartmentAsync(string departmentId, string? operatorId = null, string? operatorName = null, string? ipAddress = null);

        /// <summary>
        /// 获取所有启用的科室（用于下拉选择）
        /// </summary>
        Task<List<DepartmentDto>> GetActiveDepartmentsAsync();
    }
}
