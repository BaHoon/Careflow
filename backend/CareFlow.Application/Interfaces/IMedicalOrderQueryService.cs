using CareFlow.Application.DTOs.Common;

namespace CareFlow.Application.Interfaces;

/// <summary>
/// 医嘱查询服务接口
/// 提供医生端查询医嘱列表、查看医嘱详情、停止医嘱等功能
/// </summary>
public interface IMedicalOrderQueryService
{
    /// <summary>
    /// 查询患者的医嘱列表（支持多条件筛选）
    /// </summary>
    /// <param name="request">查询请求，包含患者ID、状态、类型、时间范围等筛选条件</param>
    /// <returns>医嘱列表和统计信息</returns>
    /// <remarks>
    /// 筛选逻辑：
    /// 1. PatientId 必填，按患者筛选
    /// 2. Statuses 可选，如果提供则按状态筛选（支持多选）
    /// 3. OrderTypes 可选，如果提供则按类型筛选（支持多选）
    /// 4. CreateTimeFrom/CreateTimeTo 可选，按创建时间范围筛选
    /// 5. 默认按创建时间倒序排列（最新的在前）
    /// </remarks>
    Task<QueryOrdersResponseDto> GetOrdersByPatientAsync(QueryOrdersRequestDto request);
    
    /// <summary>
    /// 获取单个医嘱的详细信息（包含关联的执行任务列表）
    /// </summary>
    /// <param name="orderId">医嘱ID</param>
    /// <returns>医嘱详情，包含完整的医嘱信息和任务列表</returns>
    /// <remarks>
    /// 返回内容：
    /// 1. 医嘱基础信息（状态、类型、时间等）
    /// 2. 根据医嘱类型返回特定字段（药品、手术、检查、操作）
    /// 3. 关联的所有执行任务及其状态
    /// 4. 审计信息（签收、停嘱等操作记录）
    /// </remarks>
    Task<OrderDetailDto> GetOrderDetailAsync(long orderId);
    
    /// <summary>
    /// 医生停止医嘱（核心功能）
    /// </summary>
    /// <param name="request">停嘱请求，包含医嘱ID、医生ID、停嘱原因和停止节点</param>
    /// <returns>停嘱结果，包含被锁定的任务列表</returns>
    /// <remarks>
    /// 业务逻辑（参考状态流转图）：
    /// 
    /// 1. 验证医嘱状态：
    ///    - PendingReceive（未签收）：直接取消，状态改为 Cancelled，无需护士签收
    ///    - Accepted 或 InProgress（已签收）：改为 PendingStop，需要护士签收
    ///    - 其他状态（Draft、Completed、Stopped等）不允许停止
    /// 
    /// 2. 更新医嘱状态：
    ///    - 状态：InProgress → PendingStop
    ///    - 填充审计字段：StopOrderTime, StopDoctorId, StopReason
    /// 
    /// 3. 锁定任务（关键逻辑）：
    ///    a) 获取医嘱的所有任务，按 PlannedStartTime 排序
    ///    b) 找到停止节点任务（StopAfterTaskId）
    ///    c) 筛选该任务之后的所有未完成任务（排除 Completed 和 Stopped）
    ///    d) 对这些任务执行锁定操作：
    ///       - 保存当前状态到 StatusBeforeLocking
    ///       - 将状态改为 OrderStopping
    /// 
    /// 4. 示例：
    ///    医嘱有5个任务：[T1(Completed), T2(InProgress), T3(Pending), T4(Pending), T5(Applied)]
    ///    医生选择 StopAfterTaskId = T2
    ///    结果：
    ///    - T1(Completed) → 不变（已完成）
    ///    - T2(InProgress) → 不变（停止节点，让护士完成这个）
    ///    - T3(Pending) → OrderStopping（锁定，StatusBeforeLocking = Pending）
    ///    - T4(Pending) → OrderStopping（锁定，StatusBeforeLocking = Pending）
    ///    - T5(Applied) → OrderStopping（锁定，StatusBeforeLocking = Applied）
    /// 
    /// 5. 后续流程：
    ///    - 医嘱进入 PendingStop 状态，等待护士签收
    ///    - 护士签收停嘱 → 医嘱状态改为 Stopped，所有 OrderStopping 任务改为 Stopped
    ///    - 护士拒绝停嘱 → 医嘱状态恢复为 InProgress，任务恢复为 StatusBeforeLocking
    /// </remarks>
    Task<StopOrderResponseDto> StopOrderAsync(StopOrderRequestDto request);

    /// <summary>
    /// 重新提交已退回的医嘱
    /// </summary>
    /// <param name="orderId">医嘱ID</param>
    /// <param name="doctorId">医生ID</param>
    /// <returns>操作结果</returns>
    Task<bool> ResubmitRejectedOrderAsync(long orderId, string doctorId);

    /// <summary>
    /// 撤销已退回的医嘱
    /// </summary>
    /// <param name="orderId">医嘱ID</param>
    /// <param name="doctorId">医生ID</param>
    /// <param name="cancelReason">撤销原因</param>
    /// <returns>操作结果</returns>
    Task<bool> CancelRejectedOrderAsync(long orderId, string doctorId, string cancelReason);
}
