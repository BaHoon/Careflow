import api from '../utils/api';

/**
 * 医嘱签收相关API
 */

/**
 * 获取科室患者未签收医嘱统计（用于红点标注）
 * @param {string} deptCode - 科室代码
 * @returns {Promise} 患者列表及未签收医嘱数量
 */
export const getPendingOrdersSummary = (deptCode) => {
  return api.get('/orders/acknowledgement/pending-summary', { 
    params: { deptCode } 
  });
};

/**
 * 获取指定患者的待签收医嘱（包括新开和停止）
 * @param {string} patientId - 患者ID
 * @returns {Promise} 新开医嘱和停止医嘱列表
 */
export const getPatientPendingOrders = (patientId) => {
  return api.get(`/orders/acknowledgement/patient/${patientId}`);
};

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

/**
 * 获取护士当前排班的病区
 * @param {string} nurseId - 护士ID
 * @returns {Promise} 当前排班的病区信息
 */
export const getCurrentWard = (nurseId) => {
  return api.get(`/nurse/schedule/current-ward/${nurseId}`);
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
