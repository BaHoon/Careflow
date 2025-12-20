using CareFlow.Application.Interfaces;
using CareFlow.Core.Interfaces;
using CareFlow.Core.Models.Medical;
using Microsoft.Extensions.Logging;

namespace CareFlow.Application.Services
{
    /// <summary>
    /// 操作医嘱管理服务（独立入口，不修改 MedicalOrderManager）
    /// </summary>
    public interface IOperationOrderManager
    {
        Task<OperationOrder> CreateOperationOrderAsync(OperationOrder order);
    }

    public class OperationOrderManager : IOperationOrderManager
    {
        private readonly INurseAssignmentService _nurseAssignmentService;
        private readonly IRepository<OperationOrder, long> _operationRepo;
        private readonly ILogger<OperationOrderManager> _logger;

        public OperationOrderManager(
            INurseAssignmentService nurseAssignmentService,
            IRepository<OperationOrder, long> operationRepo,
            ILogger<OperationOrderManager> logger)
        {
            _nurseAssignmentService = nurseAssignmentService;
            _operationRepo = operationRepo;
            _logger = logger;
        }

        public async Task<OperationOrder> CreateOperationOrderAsync(OperationOrder order)
        {
            // 1. 自动分配护士（如果未指定）
            if (string.IsNullOrEmpty(order.NurseId))
            {
                // 操作医嘱使用当前时间查询排班
                var targetTime = DateTime.UtcNow;
                
                order.NurseId = await _nurseAssignmentService
                    .CalculateResponsibleNurseAsync(order.PatientId, targetTime);
                
                if (string.IsNullOrEmpty(order.NurseId))
                {
                    _logger.LogWarning("无法为患者 {PatientId} 分配负责护士，医嘱将不设置 NurseId", order.PatientId);
                }
                else
                {
                    _logger.LogInformation("为操作医嘱自动分配护士: {NurseId}", order.NurseId);
                }
            }

            // 2. 设置默认值
            if (string.IsNullOrEmpty(order.Status))
            {
                order.Status = "Pending"; // 初始状态
            }

            if (order.CreateTime == default)
            {
                order.CreateTime = DateTime.UtcNow;
            }

            // 3. 保存到数据库
            await _operationRepo.AddAsync(order);

            _logger.LogInformation("操作医嘱创建成功: OrderId={OrderId}, OpId={OpId}, PatientId={PatientId}", 
                order.Id, order.OpId, order.PatientId);

            return order;
        }
    }
}

