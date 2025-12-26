import api from '../utils/api';

/**
 * 出院医嘱相关API
 */

/**
 * 批量创建出院医嘱
 * @param {Object} data - 出院医嘱数据
 * @param {string} data.patientId - 患者ID
 * @param {string} data.doctorId - 医生ID
 * @param {Array} data.orders - 出院医嘱列表
 * @param {number} data.orders[].dischargeType - 出院类型（1-治愈 2-好转 3-转院 4-自动出院 5-死亡 99-其他）
 * @param {string} data.orders[].dischargeTime - 出院时间
 * @param {string} data.orders[].dischargeDiagnosis - 出院诊断
 * @param {string} [data.orders[].dischargeInstructions] - 出院医嘱
 * @param {string} [data.orders[].medicationInstructions] - 用药指导
 * @param {boolean} data.orders[].requiresFollowUp - 是否需要随访
 * @param {string} [data.orders[].followUpDate] - 随访日期
 * @param {Array} [data.orders[].items] - 出院带回药品
 */
export const batchCreateDischargeOrders = (data) => {
  return api.post('/orders/discharge/batch', data);
};

/**
 * 验证出院医嘱创建前置条件
 * @param {string} patientId - 患者ID
 */
export const validateDischargeOrderCreation = (patientId) => {
  return api.get(`/orders/discharge/validate-creation/${patientId}`);
};

/**
 * 验证出院医嘱签收前置条件
 * @param {string} patientId - 患者ID
 */
export const validateDischargeOrderAcknowledgement = (patientId) => {
  return api.get(`/orders/discharge/validate-acknowledgement/${patientId}`);
};

/**
 * 获取患者出院医嘱列表
 * @param {string} patientId - 患者ID
 */
export const getPatientDischargeOrders = (patientId) => {
  return api.get(`/orders/discharge/patient/${patientId}`);
};
