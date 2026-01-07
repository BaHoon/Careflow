using CareFlow.Application.Interfaces;
using CareFlow.Core.Interfaces;
using CareFlow.Core.Models.Medical;

namespace CareFlow.Application.Services
{
    // 接口泛型：不管你传什么医嘱，只要是 MedicalOrder 的子类就行
    public interface IMedicalOrderManager
    {
        Task<T> CreateOrderAsync<T>(T order) where T : CareFlow.Core.Models.Medical.MedicalOrder;
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
        public async Task<T> CreateOrderAsync<T>(T order) where T : CareFlow.Core.Models.Medical.MedicalOrder
        {
            // 用时间+patientId来计算护士
            if (string.IsNullOrEmpty(order.NurseId))
            {
                // 使用医嘱创建时间（当前时间）来分配负责护士
                // 负责护士是指接收和签收医嘱的护士，应该基于医嘱开具时间而不是执行时间
                DateTime targetTime = DateTime.UtcNow;
        
                // 注意：所有类型的医嘱都使用创建时间来计算负责护士
                // 不再使用StartTime或ScheduleTime，因为那是执行时间，不是签收时间
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