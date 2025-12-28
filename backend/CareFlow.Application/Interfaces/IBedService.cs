using CareFlow.Application.DTOs.Admin;

namespace CareFlow.Application.Interfaces
{
    public interface IBedService
    {
        /// <summary>
        /// 获取科室下的所有病床
        /// </summary>
        Task<List<BedDto>> GetBedsByDepartmentIdAsync(string departmentId);

        /// <summary>
        /// 创建新病床
        /// </summary>
        Task<BedDto> CreateBedAsync(CreateBedDto dto);

        /// <summary>
        /// 删除病床
        /// </summary>
        Task<bool> DeleteBedAsync(string bedId);

        /// <summary>
        /// 统计科室病床数量
        /// </summary>
        Task<int> CountBedsByDepartmentIdAsync(string departmentId);
    }
}
