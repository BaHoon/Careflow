import api from '../utils/api';

/**
 * 检查类医嘱相关API
 */

/**
 * 批量创建检查医嘱（CT、MRI、X光等）
 * @param {Object} data - 检查医嘱数据
 * @param {string} data.patientId - 患者ID
 * @param {string} data.doctorId - 医生ID
 * @param {Array} data.orders - 检查医嘱列表
 * @param {string} data.orders[].inspectionType - 检查类型（CT、MRI、X-Ray等）
 * @param {string} data.orders[].targetOrgan - 检查部位（Head、Chest等）
 * @param {boolean} data.orders[].contrast - 是否使用造影剂
 * @param {string} data.orders[].scheduledTime - 预约时间
 * @param {string} [data.orders[].clinicalInfo] - 临床资料（可选）
 * @param {string} [data.orders[].remarks] - 备注（可选）
 */
export const batchCreateInspectionOrders = (data) => {
  return api.post('/orders/inspection/batch', data);
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
