import api from '../utils/api';

/**
 * ==============================
 * 【医嘱签收相关API】
 * 
 * 注意：患者列表和排班相关API已移至 api/patient.js
 * 请使用以下导入：
 * import { getPatientsWithPendingCount, getCurrentWard } from '@/api/patient'
 * ==============================
 */

// ==================== 待签收医嘱查询 ====================

/**
 * 获取指定患者的待签收医嘱（包括新开和停止）
 * @param {string} patientId - 患者ID
 * @returns {Promise<Object>} 新开医嘱和停止医嘱列表
 * @example
 * {
 *   newOrders: [...],
 *   stoppedOrders: [...]
 * }
 */
export const getPatientPendingOrders = (patientId) => {
  return api.get(`/orders/acknowledgement/patient/${patientId}`);
};

// ==================== 医嘱签收操作 ====================

/**
 * 批量签收医嘱
 * @param {Object} data - 签收数据
 * @param {string} data.nurseId - 护士ID
 * @param {Array<number>} data.orderIds - 医嘱ID列表
 * @returns {Promise} 签收结果
 */
export const acknowledgeOrders = (data) => {
  return api.post('/orders/acknowledgement/acknowledge', data);
};

/**
 * 批量退回医嘱
 * @param {Object} data - 退回数据
 * @param {string} data.nurseId - 护士ID
 * @param {Array<number>} data.orderIds - 医嘱ID列表
 * @param {string} data.rejectReason - 退回原因
 * @returns {Promise} 退回结果
 */
export const rejectOrders = (data) => {
  return api.post('/orders/acknowledgement/reject', data);
};

// ==================== TODO: 阶段三实现以下API ====================

/**
 * TODO: 阶段三 - 立即申请药品
 * @param {Object} data - 申请数据
 * @param {number} data.orderId - 医嘱ID
 * @returns {Promise}
 */
export const requestMedicationImmediately = (data) => {
  return api.post('/orders/acknowledgement/request-medication-immediately', data);
};

/**
 * TODO: 阶段三 - 申请检查
 * @param {Object} data - 申请数据
 * @param {number} data.orderId - 医嘱ID
 * @returns {Promise}
 */
export const requestInspection = (data) => {
  return api.post('/orders/acknowledgement/request-inspection', data);
};

/**
 * TODO: 阶段三 - 取消药品申请
 * @param {Object} data - 取消申请数据
 * @param {number} data.orderId - 医嘱ID
 * @param {Array<number>} data.requestIds - 申请ID列表
 * @returns {Promise}
 */
export const cancelMedicationRequest = (data) => {
  return api.post('/orders/acknowledgement/cancel-medication-request', data);
};
