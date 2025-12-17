using Microsoft.AspNetCore.Mvc;
using CareFlow.Application.Services;
using CareFlow.Application.DTOs.MedicalOrder;
using CareFlow.Core.Models.Medical;
using CareFlow.Core.Enums;

namespace CareFlow.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MedicalOrderController : ControllerBase
    {
        private readonly IMedicalOrderManager _orderManager;

        public MedicalOrderController(IMedicalOrderManager orderManager)
        {
            _orderManager = orderManager;
        }

        /// <summary>
        /// 统一医嘱创建入口 (自动匹配负责护士)
        /// </summary>
        [HttpPost("create")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequestDto request)
        {
            // 1. 根据 OrderType 决定创建哪个实体
            MedicalOrder? orderEntity = null;

            switch (request.OrderType.ToUpper())
            {
                case "SURGICAL":
                case "SURGICALORDER":
                    orderEntity = new SurgicalOrder
                    {
                        // 基础字段
                        PatientId = request.PatientId,
                        DoctorId = request.DoctorId,
                        OrderType = "SurgicalOrder",
                        IsLongTerm = request.IsLongTerm,
                        Status = "Pending", // 初始状态
                        CreateTime = DateTime.UtcNow,
                        
                        // 手术特有字段
                        SurgeryName = request.SurgeryName ?? "未命名手术",
                        ScheduleTime = request.ScheduleTime ?? DateTime.UtcNow.AddDays(1),
                        AnesthesiaType = request.AnesthesiaType ?? "局部麻醉",
                        IncisionSite = request.IncisionSite ?? "待定",
                        RequiredMeds = request.RequiredMeds,
                        RequiredTalk = request.RequiredTalk,
                        RequiredOperation = request.RequiredOperation,
                        PrepStatus = "未开始",
                        PrepProgress = 0f
                    };
                    break;

                case "MEDICATION":
                case "MEDICATIONORDER":
                    orderEntity = new MedicationOrder
                    {
                        PatientId = request.PatientId,
                        DoctorId = request.DoctorId,
                        OrderType = "MedicationOrder",
                        IsLongTerm = request.IsLongTerm,
                        Status = "Active",
                        CreateTime = DateTime.UtcNow,

                        // 药品特有字段
                        UsageRoute = Enum.Parse<UsageRoute>(request.UsageRoute!),
                        FreqCode = request.FreqCode!,
                        TimingStrategy = request.TimingStrategy ?? "IMMEDIATE",
                        SmartSlotsMask = request.SmartSlotsMask ?? 0,
                        IntervalDays = request.IntervalDays ?? 1
                    };
                    break;

                default:
                    return BadRequest($"不支持的医嘱类型: {request.OrderType}");
            }

            // 2. [关键] 将实体交给 Manager
            // Manager 会自动：计算护士 -> 填充NurseId -> 保存到对应表
            // 因为 CreateOrderAsync 是泛型方法，我们可以利用 C# 的 dynamic 或反射，
            // 但最简单稳妥的方法是显式转换调用。
            
            if (orderEntity is SurgicalOrder sOrder)
            {
                await _orderManager.CreateOrderAsync(sOrder);
            }
            else if (orderEntity is MedicationOrder mOrder)
            {
                await _orderManager.CreateOrderAsync(mOrder);
            }

            // 3. 返回结果 (此时 orderEntity.NurseId 应该已经被自动填上了)
            return Ok(new 
            { 
                Success = true, 
                Message = "医嘱创建成功，已自动分配护士", 
                OrderId = orderEntity.Id, 
                AssignedNurseId = orderEntity.NurseId // 让前端看到分配给了谁
            });
        }
    }
}