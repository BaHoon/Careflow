using CareFlow.Core.Models.Medical;

namespace CareFlow.Application.Interfaces;

/// <summary>
/// 操作医嘱管理服务接口（兼容旧接口）
/// 用于单个创建操作医嘱
/// </summary>
public interface IOperationOrderManager
{
    Task<OperationOrder> CreateOperationOrderAsync(OperationOrder order);
}

