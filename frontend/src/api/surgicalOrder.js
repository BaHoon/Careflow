import api from '../utils/api';

/**
 * 手术类医嘱相关API
 */

/**
 * 批量创建手术医嘱
 * @param {Object} data - 手术医嘱数据
 * @param {string} data.patientId - 患者ID
 * @param {string} data.doctorId - 医生ID
 * @param {Array} data.orders - 手术医嘱列表
 * @param {string} data.orders[].surgeryName - 手术名称
 * @param {string} data.orders[].surgeryType - 手术类型（Elective、Emergency）
 * @param {string} data.orders[].anesthesiaMethod - 麻醉方式
 * @param {string} data.orders[].surgeonId - 主刀医生ID
 * @param {Array<string>} data.orders[].assistantIds - 助手医生ID列表
 * @param {string} data.orders[].scheduledTime - 计划手术时间
 * @param {number} [data.orders[].estimatedDuration] - 预计时长（分钟）
 * @param {string} [data.orders[].operatingRoom] - 手术室
 */
export const batchCreateSurgicalOrders = (data) => {
  return api.post('/orders/surgical/batch', data);
};

/**
 * 获取患者手术医嘱列表
 * @param {string} patientId - 患者ID
 */
export const getPatientSurgicalOrders = (patientId) => {
  return api.get(`/SurgicalOrder/patient/${patientId}`);
};

/**
 * 获取可用手术室
 */
export const getAvailableOperatingRooms = () => {
  return api.get('/SurgicalOrder/operating-rooms');
};

/**
 * 获取外科医生列表
 */
export const getSurgeons = () => {
  return api.get('/SurgicalOrder/surgeons');
};
