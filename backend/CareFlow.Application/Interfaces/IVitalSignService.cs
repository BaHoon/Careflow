using CareFlow.Application.DTOs.Nursing; // <--- 必须引用这个命名空间

namespace CareFlow.Application.Interfaces // 或者是 CareFlow.Application.Common.Interfaces
{
    public interface IVitalSignService
    {
        // 确保这里的参数类型是 NursingTaskSubmissionDto
        Task SubmitVitalSignsAsync(NursingTaskSubmissionDto input);
        
        // 取消护理任务
        Task CancelNursingTaskAsync(long taskId, string nurseId, string cancelReason);
        
        // 添加护理记录补充说明
        Task<SupplementDto> AddSupplementAsync(AddSupplementDto dto);
        
        // 获取护理记录的补充说明列表
        Task<List<SupplementDto>> GetSupplementsAsync(long nursingTaskId);
    }
}