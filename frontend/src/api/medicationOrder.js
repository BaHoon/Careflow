import api from '../utils/api';

/**
 * 药物医嘱相关API
 */

/**
 * 批量创建医嘱
 * @param {Object} data - 医嘱数据
 * @param {string} data.patientId - 患者ID
 * @param {string} data.doctorId - 医生ID
 * @param {Array} data.orders - 医嘱列表
 */
export const batchCreateOrders = (data) => {
  return api.post('/MedicationOrder/batch-create', data);
};

/**
 * 验证医嘱数据
 * @param {Object} orderData - 单个医嘱数据
 */
export const validateOrder = (orderData) => {
  return api.post('/MedicationOrder/validate', orderData);
};

/**
 * 获取患者有效医嘱
 * @param {string} patientId - 患者ID
 */
export const getPatientActiveOrders = (patientId) => {
  return api.get(`/MedicationOrder/patient/${patientId}/active`);
};
