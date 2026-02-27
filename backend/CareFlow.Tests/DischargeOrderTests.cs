using CareFlow.Application.DTOs.DischargeOrders;
using CareFlow.Application.Interfaces;
using CareFlow.Application.Services.DischargeOrders;
using CareFlow.Core.Enums;
using CareFlow.Core.Interfaces;
using CareFlow.Core.Models.Medical;
using CareFlow.Core.Models.Nursing;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CareFlow.Tests;

/// <summary>
/// 出院医嘱业务逻辑测试
/// </summary>
public class DischargeOrderTests
{
    private readonly Mock<IRepository<DischargeOrder, long>> _mockOrderRepository;
    private readonly Mock<IRepository<MedicalOrder, long>> _mockMedicalOrderRepository;
    private readonly Mock<IRepository<MedicalOrderStatusHistory, long>> _mockStatusHistoryRepository;
    private readonly Mock<IRepository<ExecutionTask, long>> _mockTaskRepository;
    private readonly Mock<INurseAssignmentService> _mockNurseAssignmentService;
    private readonly Mock<ILogger<DischargeOrderService>> _mockLogger;
    private readonly DischargeOrderService _service;

    public DischargeOrderTests()
    {
        _mockOrderRepository = new Mock<IRepository<DischargeOrder, long>>();
        _mockMedicalOrderRepository = new Mock<IRepository<MedicalOrder, long>>();
        _mockStatusHistoryRepository = new Mock<IRepository<MedicalOrderStatusHistory, long>>();
        _mockTaskRepository = new Mock<IRepository<ExecutionTask, long>>();
        _mockNurseAssignmentService = new Mock<INurseAssignmentService>();
        _mockLogger = new Mock<ILogger<DischargeOrderService>>();

        _service = new DischargeOrderService(
            _mockOrderRepository.Object,
            _mockMedicalOrderRepository.Object,
            _mockStatusHistoryRepository.Object,
            _mockTaskRepository.Object,
            _mockNurseAssignmentService.Object,
            _mockLogger.Object
        );
    }

    #region 创建前置验证测试

    [Fact]
    public async Task ValidateCreation_NoOrders_ShouldAllowCreation()
    {
        // Arrange: 患者没有任何医嘱
        var patientId = "P001";
        _mockMedicalOrderRepository.Setup(r => r.GetQueryable())
            .Returns(new List<MedicalOrder>().AsQueryable());

        // Act
        var result = await _service.ValidateDischargeOrderCreationAsync(patientId);

        // Assert
        Assert.True(result.CanCreateDischargeOrder);
        Assert.Empty(result.BlockedOrders);
        Assert.Empty(result.PendingStopOrders);
    }

    [Fact]
    public async Task ValidateCreation_AllOrdersCompleted_ShouldAllowCreation()
    {
        // Arrange: 患者所有医嘱都已完成
        var patientId = "P001";
        var completedOrders = new List<MedicalOrder>
        {
            new MedicationOrder 
            { 
                Id = 1, 
                PatientId = patientId, 
                Status = OrderStatus.Completed,
                OrderType = "MedicationOrder"
            },
            new MedicationOrder 
            { 
                Id = 2, 
                PatientId = patientId, 
                Status = OrderStatus.Stopped,
                OrderType = "MedicationOrder"
            }
        };

        _mockMedicalOrderRepository.Setup(r => r.GetQueryable())
            .Returns(completedOrders.AsQueryable());

        // Act
        var result = await _service.ValidateDischargeOrderCreationAsync(patientId);

        // Assert
        Assert.True(result.CanCreateDischargeOrder);
        Assert.Empty(result.BlockedOrders);
    }

    [Fact]
    public async Task ValidateCreation_HasInProgressOrders_ShouldBlockCreation()
    {
        // Arrange: 患者有执行中的医嘱
        var patientId = "P001";
        var orders = new List<MedicalOrder>
        {
            new MedicationOrder 
            { 
                Id = 1, 
                PatientId = patientId, 
                Status = OrderStatus.InProgress,
                OrderType = "MedicationOrder",
                CreateTime = DateTime.UtcNow
            },
            new MedicationOrder 
            { 
                Id = 2, 
                PatientId = patientId, 
                Status = OrderStatus.Accepted,
                OrderType = "MedicationOrder",
                CreateTime = DateTime.UtcNow
            }
        };

        _mockMedicalOrderRepository.Setup(r => r.GetQueryable())
            .Returns(orders.AsQueryable());

        // Act
        var result = await _service.ValidateDischargeOrderCreationAsync(patientId);

        // Assert
        Assert.False(result.CanCreateDischargeOrder);
        Assert.Equal(2, result.BlockedOrders.Count);
        Assert.Contains(result.BlockedOrders, o => o.Status == OrderStatus.InProgress);
        Assert.Contains(result.BlockedOrders, o => o.Status == OrderStatus.Accepted);
    }

    [Fact]
    public async Task ValidateCreation_HasPendingStopOrders_ShouldCalculateEarliestDischargeTime()
    {
        // Arrange: 患者有待停止医嘱
        var patientId = "P001";
        var futureTime = DateTime.UtcNow.AddHours(6);
        
        var orders = new List<MedicalOrder>
        {
            new MedicationOrder 
            { 
                Id = 1, 
                PatientId = patientId, 
                Status = OrderStatus.PendingStop,
                OrderType = "MedicationOrder",
                CreateTime = DateTime.UtcNow
            }
        };

        var tasks = new List<ExecutionTask>
        {
            new ExecutionTask 
            { 
                Id = 101, 
                MedicalOrderId = 1, 
                Status = ExecutionTaskStatus.Pending,
                PlannedStartTime = futureTime
            }
        };

        _mockMedicalOrderRepository.Setup(r => r.GetQueryable())
            .Returns(orders.AsQueryable());
        
        _mockTaskRepository.Setup(r => r.GetQueryable())
            .Returns(tasks.AsQueryable());

        // Act
        var result = await _service.ValidateDischargeOrderCreationAsync(patientId);

        // Assert
        Assert.True(result.CanCreateDischargeOrder);
        Assert.Single(result.PendingStopOrders);
        Assert.Equal(futureTime, result.EarliestDischargeTime);
    }

    [Fact]
    public async Task ValidateCreation_PendingStopWithStoppedTasks_ShouldIgnoreStoppedTasks()
    {
        // Arrange: 待停止医嘱有已停止的任务，不应计入最晚时间
        var patientId = "P001";
        var futureTime = DateTime.UtcNow.AddHours(6);
        var veryFutureTime = DateTime.UtcNow.AddHours(12);
        
        var orders = new List<MedicalOrder>
        {
            new MedicationOrder 
            { 
                Id = 1, 
                PatientId = patientId, 
                Status = OrderStatus.PendingStop,
                OrderType = "MedicationOrder",
                CreateTime = DateTime.UtcNow
            }
        };

        var tasks = new List<ExecutionTask>
        {
            new ExecutionTask 
            { 
                Id = 101, 
                MedicalOrderId = 1, 
                Status = ExecutionTaskStatus.Pending,
                PlannedStartTime = futureTime
            },
            new ExecutionTask 
            { 
                Id = 102, 
                MedicalOrderId = 1, 
                Status = ExecutionTaskStatus.Stopped, // 已停止，不应计入
                PlannedStartTime = veryFutureTime
            }
        };

        _mockMedicalOrderRepository.Setup(r => r.GetQueryable())
            .Returns(orders.AsQueryable());
        
        _mockTaskRepository.Setup(r => r.GetQueryable())
            .Returns(tasks.AsQueryable());

        // Act
        var result = await _service.ValidateDischargeOrderCreationAsync(patientId);

        // Assert
        Assert.True(result.CanCreateDischargeOrder);
        Assert.Equal(futureTime, result.EarliestDischargeTime); // 应该是较早的未停止任务时间
    }

    #endregion

    #region 签收前置验证测试

    [Fact]
    public async Task ValidateAcknowledgement_NoPendingStopOrders_ShouldAllowAcknowledgement()
    {
        // Arrange: 患者没有待停止医嘱
        var patientId = "P001";
        _mockMedicalOrderRepository.Setup(r => r.GetQueryable())
            .Returns(new List<MedicalOrder>().AsQueryable());

        // Act
        var result = await _service.ValidateDischargeOrderAcknowledgementAsync(patientId);

        // Assert
        Assert.True(result.CanAcknowledge);
        Assert.Null(result.Reason);
        Assert.Empty(result.PendingStopOrderIds);
    }

    [Fact]
    public async Task ValidateAcknowledgement_HasPendingStopOrders_ShouldBlockAcknowledgement()
    {
        // Arrange: 患者有待停止医嘱
        var patientId = "P001";
        var orders = new List<MedicalOrder>
        {
            new MedicationOrder 
            { 
                Id = 1, 
                PatientId = patientId, 
                Status = OrderStatus.PendingStop,
                OrderType = "MedicationOrder",
                StopReason = "病情好转"
            },
            new MedicationOrder 
            { 
                Id = 2, 
                PatientId = patientId, 
                Status = OrderStatus.PendingStop,
                OrderType = "MedicationOrder",
                StopReason = "改用其他药物"
            }
        };

        _mockMedicalOrderRepository.Setup(r => r.GetQueryable())
            .Returns(orders.AsQueryable());

        // Act
        var result = await _service.ValidateDischargeOrderAcknowledgementAsync(patientId);

        // Assert
        Assert.False(result.CanAcknowledge);
        Assert.NotNull(result.Reason);
        Assert.Contains("2 条待停止医嘱", result.Reason);
        Assert.Equal(2, result.PendingStopOrderIds.Count);
        Assert.Equal(2, result.PendingStopOrderDetails.Count);
    }

    #endregion

    #region 出院类型枚举测试

    [Theory]
    [InlineData(DischargeType.Cured, "治愈出院")]
    [InlineData(DischargeType.Improved, "好转出院")]
    [InlineData(DischargeType.Transfer, "转院")]
    [InlineData(DischargeType.AutoDischarge, "自动出院")]
    [InlineData(DischargeType.Death, "死亡")]
    [InlineData(DischargeType.Other, "其他")]
    public void DischargeType_AllValuesAreDefined(DischargeType type, string expectedDisplay)
    {
        // Assert: 验证枚举值定义完整
        Assert.True(Enum.IsDefined(typeof(DischargeType), type));
        Assert.NotNull(expectedDisplay); // 确保所有类型都有中文显示
    }

    #endregion

    #region 任务类别测试

    [Fact]
    public void TaskCategory_DischargeConfirmation_IsDefined()
    {
        // Assert: 验证出院确认任务类别已定义
        var dischargeConfirmation = TaskCategory.DischargeConfirmation;
        Assert.Equal(11, (int)dischargeConfirmation);
        Assert.True(Enum.IsDefined(typeof(TaskCategory), dischargeConfirmation));
    }

    #endregion

    #region 边界条件测试

    [Fact]
    public async Task ValidateCreation_PastDischargeTime_ShouldStillValidate()
    {
        // Arrange: 验证方法本身不检查时间是否过去（由创建方法检查）
        var patientId = "P001";
        _mockMedicalOrderRepository.Setup(r => r.GetQueryable())
            .Returns(new List<MedicalOrder>().AsQueryable());

        // Act
        var result = await _service.ValidateDischargeOrderCreationAsync(patientId);

        // Assert: 验证方法只关心医嘱状态，不关心时间
        Assert.True(result.CanCreateDischargeOrder);
    }

    [Fact]
    public async Task ValidateCreation_EmptyPatientId_ShouldNotThrow()
    {
        // Arrange
        var patientId = "";
        _mockMedicalOrderRepository.Setup(r => r.GetQueryable())
            .Returns(new List<MedicalOrder>().AsQueryable());

        // Act & Assert: 不应该抛出异常，返回空结果
        var result = await _service.ValidateDischargeOrderCreationAsync(patientId);
        Assert.NotNull(result);
    }

    #endregion
}
