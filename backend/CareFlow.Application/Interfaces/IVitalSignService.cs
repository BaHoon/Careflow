using CareFlow.Application.DTOs.Nursing; // <--- 必须引用这个命名空间

namespace CareFlow.Application.Interfaces // 或者是 CareFlow.Application.Common.Interfaces
{
    public interface IVitalSignService
    {
        // 确保这里的参数类型是 NursingTaskSubmissionDto
        Task SubmitVitalSignsAsync(NursingTaskSubmissionDto input);
        
        // 取消护理任务
        Task CancelNursingTaskAsync(long taskId, string nurseId, string cancelReason);
    }
}