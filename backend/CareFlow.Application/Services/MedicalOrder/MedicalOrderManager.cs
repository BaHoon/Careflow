using CareFlow.Application.Interfaces;
using CareFlow.Core.Interfaces;
using CareFlow.Core.Models.Medical;

namespace CareFlow.Application.Services
{
    // 接口泛型：不管你传什么医嘱，只要是 MedicalOrder 的子类就行
    public interface IMedicalOrderManager
    {
        Task<T> CreateOrderAsync<T>(T order) where T : MedicalOrder;
    }

    public class MedicalOrderManager : IMedicalOrderManager
    {
        private readonly INurseAssignmentService _nurseAssignmentService;
        private readonly IRepository<SurgicalOrder, long> _surgicalRepo;
        private readonly IRepository<MedicationOrder, long> _medicationRepo;
        // 如果有检查医嘱，继续加...

        public MedicalOrderManager(
            INurseAssignmentService nurseAssignmentService,
            IRepository<SurgicalOrder, long> surgicalRepo,
            IRepository<MedicationOrder, long> medicationRepo)
        {
            _nurseAssignmentService = nurseAssignmentService;
            _surgicalRepo = surgicalRepo;
            _medicationRepo = medicationRepo;
        }

        // 实现泛型方法
        public async Task<T> CreateOrderAsync<T>(T order) where T : MedicalOrder
        {
            // 用时间+patientId来计算护士
            if (string.IsNullOrEmpty(order.NurseId))
            {
                // 确定我们要用哪个时间去查排班
                DateTime targetTime = DateTime.UtcNow; // 默认用当前时间

                // 如果是手术医嘱，用排期时间
                if (order is SurgicalOrder sOrder && sOrder.ScheduleTime != default)
                {
                    targetTime = sOrder.ScheduleTime;
                }
                // 如果是药品医嘱，且有开始时间，用开始时间
                else if (order is MedicationOrder mOrder && mOrder.StartTime.HasValue)
                {
                    targetTime = mOrder.StartTime.Value;
                }
        
                // 2. 使用确定的目标时间去计算
                order.NurseId = await _nurseAssignmentService
                    .CalculateResponsibleNurseAsync(order.PatientId, targetTime);
            }

            // B. 根据具体类型保存到不同表
            if (order is SurgicalOrder surgicalOrder)
            {
                // 存入手术表
                await _surgicalRepo.AddAsync(surgicalOrder);
            }
            else if (order is MedicationOrder medOrder)
            {
                // 存入药品表
                await _medicationRepo.AddAsync(medOrder);
            }
            else
            {
                // 如果以后加了新类型但忘了在这里写 if，报错提醒自己
                throw new NotSupportedException($"暂不支持保存类型为 {typeof(T).Name} 的医嘱，请在 Manager 中添加分支。");
            }

            return order;
        }
    }
}