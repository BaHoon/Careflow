import api from '../utils/api';

/**
 * ==============================
 * 【医嘱申请相关API】
 * 用于护士提交药品取药申请和检查申请
 * ==============================
 */

// ==================== 查询申请列表 ====================

/**
 * 获取药品申请列表
 * @param {Object} data - 查询参数
 * @param {string} data.applicationType - 申请类型 "Medication"
 * @param {Array<string>} data.patientIds - 患者ID列表
 * @param {Array<string>} data.statusFilter - 状态筛选 ["Applying", "Applied", "AppliedConfirmed"]
 * @param {string} data.startTime - 开始时间（ISO格式）
 * @param {string} data.endTime - 结束时间（ISO格式）
 * @returns {Promise<Array>} 申请项列表
 */
export const getMedicationApplications = (data) => {
  return api.post('/orders/application/medication/list', data);
};

/**
 * 获取检查申请列表
 * @param {Object} data - 查询参数
 * @param {string} data.applicationType - 申请类型 "Inspection"
 * @param {Array<string>} data.patientIds - 患者ID列表
 * @returns {Promise<Array>} 申请项列表
 */
export const getInspectionApplications = (data) => {
  return api.post('/orders/application/inspection/list', data);
};

// ==================== 提交申请 ====================

/**
 * 提交药品申请
 * @param {Object} data - 申请数据
 * @param {string} data.nurseId - 护士ID
 * @param {Array<number>} data.taskIds - 任务ID列表
 * @param {boolean} data.isUrgent - 是否加急
 * @param {string} data.remarks - 备注
 * @returns {Promise<Object>} 申请结果
 */
export const submitMedicationApplication = (data) => {
  return api.post('/orders/application/medication/submit', data);
};

/**
 * 提交检查申请
 * @param {Object} data - 申请数据
 * @param {string} data.nurseId - 护士ID
 * @param {Array<number>} data.taskIds - 申请任务ID列表
 * @param {boolean} data.isUrgent - 是否加急
 * @param {string} data.remarks - 备注
 * @returns {Promise<Object>} 申请结果
 */
export const submitInspectionApplication = (data) => {
  return api.post('/orders/application/inspection/submit', data);
};

// ==================== 撤销申请 ====================

/**
 * 撤销药品申请
 * @param {Object} data - 撤销数据
 * @param {string} data.nurseId - 护士ID
 * @param {Array<number>} data.ids - 任务ID列表
 * @param {string} data.reason - 撤销原因
 * @returns {Promise<Object>} 撤销结果
 */
export const cancelMedicationApplication = (data) => {
  return api.post('/orders/application/medication/cancel', data);
};

/**
 * 撤销检查申请
 * @param {Object} data - 撤销数据
 * @param {string} data.nurseId - 护士ID
 * @param {Array<number>} data.ids - 医嘱ID列表
 * @param {string} data.reason - 撤销原因
 * @returns {Promise<Object>} 撤销结果
 */
export const cancelInspectionApplication = (data) => {
  return api.post('/orders/application/inspection/cancel', data);
};
