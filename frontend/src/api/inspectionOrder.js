import api from '../utils/api';

/**
 * 检查类医嘱相关API - 完整工作流
 * 
 * 流程说明：
 * 步骤1: 医生开立检查医嘱 (状态: Pending)
 * 步骤2: 病房护士签收医嘱 (状态: Pending)
 * 步骤3: 护士提交检查申请，等待预约确认 (状态: Pending)
 * 步骤4: 预约确认后，护士打印导引单交给患者 (状态: Pending)
 * 步骤5: 患者自行前往检查站进行检查
 * 步骤6: 检查时间+30分钟后，系统自动获取报告 (状态: Pending → ReportPending/ReportCompleted)
 * 步骤7: 报告到达后，自动生成"查看报告"任务给护士 (状态: ReportCompleted)
 * 步骤8: 病房护士查看报告并告知患者
 */

// ========== 步骤1：医生开立检查医嘱 ==========

/**
 * 批量创建检查医嘱
 * @param {Object} data - 检查医嘱数据
 * @param {string} data.patientId - 患者ID
 * @param {string} data.doctorId - 医生ID
 * @param {Array} data.orders - 检查医嘱列表
 * @param {string} data.orders[].itemCode - 检查项目代码（如 CT_HEAD、MRI_CHEST）
 * @param {string} [data.orders[].remarks] - 备注（可选）
 */
export const batchCreateInspectionOrders = (data) => {
  return api.post('/orders/inspection/batch', data);
};

// ========== 扫码处理 ==========

/**
 * 统一扫码接口 - 根据任务类型自动处理
 * 
 * 步骤4: 打印导引单 → 确认打印完成
 * 
 * @param {Object} data
 * @param {number} data.taskId - 任务ID
 * @param {string} data.patientId - 患者ID
 * @param {string} data.nurseId - 护士ID
 */
export const processScan = (data) => {
  return api.post('/Inspection/scan', data);
};

// ========== 步骤7：系统自动获取报告（⚡自动生成查看任务）==========

/**
 * 创建检查报告（通常由系统自动调用或检查站推送）
 * ⚡自动操作:
 * - 更新状态为 ReportCompleted
 * - 记录报告时间
 * - 自动生成"查看报告"任务给病房护士
 * 
 * @param {Object} data
 * @param {number} data.orderId - 检查医嘱ID
 * @param {string} data.risLisId - RIS/LIS申请单号
 * @param {string} data.findings - 检查所见
 * @param {string} data.impression - 检查印象/结论
 * @param {string} data.attachmentUrl - 报告附件URL
 * @param {string} data.reviewerId - 审核医生ID
 * @param {string} data.reportSource - 报告来源
 */
export const createInspectionReport = (data) => {
  return api.post('/Inspection/reports', data);
};

// ========== 步骤8：查看报告 ==========

/**
 * 获取检查报告详情
 * @param {number} reportId - 报告ID
 */
export const getInspectionReport = (reportId) => {
  return api.get(`/Inspection/reports/${reportId}`);
};

// ========== 辅助功能 ==========

/**
 * 获取检查医嘱列表（支持筛选和分页）
 * @param {Object} params
 * @param {number} [params.pageIndex=1] - 页码
 * @param {number} [params.pageSize=20] - 每页数量
 * @param {string} [params.inspectionStatus] - 检查状态（Pending, ReportPending, ReportCompleted）
 * @param {string} [params.ward] - 病区
 * @param {string} [params.patientName] - 患者姓名
 */
export const getInspectionOrderList = (params) => {
  return api.get('/Inspection/list', { params });
};

/**
 * 获取检查医嘱详情
 * @param {number} orderId - 医嘱ID
 */
export const getInspectionOrderDetail = (orderId) => {
  return api.get(`/Inspection/detail/${orderId}`);
};

/**
 * 获取检查医嘱关联的任务列表
 * @param {number} orderId - 医嘱ID
 */
export const getInspectionOrderTasks = (orderId) => {
  return api.get(`/Inspection/${orderId}/tasks`);
};

/**
 * 获取患者检查医嘱列表
 * @param {string} patientId - 患者ID
 */
export const getPatientInspectionOrders = (patientId) => {
  return api.get(`/InspectionOrder/patient/${patientId}`);
};

/**
 * 获取可用检查设备
 */
export const getAvailableInspectionDevices = () => {
  return api.get('/InspectionOrder/devices');
};

// ========== 状态常量 ==========

export const InspectionOrderStatus = {
  Pending: 'Pending',              // 待前往
  ReportPending: 'ReportPending',  // 报告待出
  ReportCompleted: 'ReportCompleted', // 报告已出
  Cancelled: 'Cancelled'           // 已取消
};

export const StatusDisplayText = {
  'Pending': '待前往',
  'ReportPending': '报告待出',
  'ReportCompleted': '报告已出',
  'Cancelled': '已取消'
};

// ========== 任务类型常量 ==========

export const InspectionTaskType = {
  PrintGuide: 'INSP_PRINT_GUIDE',      // 打印导引单
  ReviewReport: 'INSP_REVIEW_REPORT'   // 查看报告
};
