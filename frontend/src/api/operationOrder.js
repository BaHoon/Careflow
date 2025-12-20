import api from '../utils/api';

/**
 * 操作类医嘱相关API
 */

/**
 * 批量创建操作医嘱（换药、导尿等）
 * @param {Object} data - 操作医嘱数据
 * @param {string} data.patientId - 患者ID
 * @param {string} data.doctorId - 医生ID
 * @param {Array} data.orders - 操作医嘱列表
 * @param {string} data.orders[].operationCode - 操作编码
 * @param {string} data.orders[].operationName - 操作名称
 * @param {string} [data.orders[].targetSite] - 操作部位（可选）
 * @param {string} [data.orders[].scheduledTime] - 计划执行时间（可选）
 * @param {string} [data.orders[].remarks] - 备注（可选）
 */
export const batchCreateOperationOrders = (data) => {
  return api.post('/orders/operation/batch', data);
};

/**
 * 获取患者操作医嘱列表
 * @param {string} patientId - 患者ID
 */
export const getPatientOperationOrders = (patientId) => {
  return api.get(`/OperationOrder/patient/${patientId}`);
};

/**
 * 获取常用操作项目列表
 */
export const getCommonOperations = () => {
  return api.get('/OperationOrder/common-operations');
};
