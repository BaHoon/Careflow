import api from '../utils/api';

/**
 * 操作类医嘱相关API
 */

/**
 * 批量创建操作医嘱
 * @param {Object} data - 操作医嘱数据
 * @param {string} data.patientId - 患者ID
 * @param {string} data.doctorId - 医生ID
 * @param {Array} data.orders - 操作医嘱列表
 * @param {boolean} data.orders[].isLongTerm - 是否长期医嘱
 * @param {string} data.orders[].plantEndTime - 计划结束时间（ISO格式）
 * @param {string} data.orders[].opId - 操作代码（如 "OP001"）
 * @param {string} data.orders[].operationName - 操作名称
 * @param {string} [data.orders[].operationSite] - 操作部位（可选）
 * @param {boolean} data.orders[].normal - 正常/异常标识
 * @param {string} data.orders[].timingStrategy - 时间策略（IMMEDIATE/SPECIFIC/CYCLIC/SLOTS）
 * @param {string} [data.orders[].startTime] - 开始时间（ISO格式，可选）
 * @param {number} [data.orders[].intervalHours] - 间隔小时数（仅CYCLIC）
 * @param {number} data.orders[].intervalDays - 间隔天数（默认1）
 * @param {number} data.orders[].smartSlotsMask - 时段掩码（仅SLOTS）
 * @param {boolean} data.orders[].requiresPreparation - 是否需要准备物品
 * @param {Array<string>} [data.orders[].preparationItems] - 准备物品列表（可选）
 * @param {number} [data.orders[].expectedDurationMinutes] - 预期执行时长（分钟，可选）
 * @param {boolean} data.orders[].requiresResult - 是否需要记录结果
 * @param {Object} [data.orders[].resultTemplate] - 结果模板（可选）
 * @param {string} [data.orders[].remarks] - 备注（可选）
 */
/**
 * 批量创建操作医嘱（支持单个或多个医嘱创建）
 * 单个医嘱时，orders 数组包含一个元素即可
 */
export const batchCreateOperationOrders = (data) => {
  return api.post('/orders/operation/batch', data);
};

/**
 * 验证操作医嘱数据
 * @param {Object} orderData - 单个操作医嘱数据
 */
export const validateOperationOrder = (orderData) => {
  return api.post('/orders/operation/validate', orderData);
};

/**
 * 获取患者操作医嘱列表
 * @param {string} patientId - 患者ID
 */
export const getPatientOperationOrders = (patientId) => {
  return api.get(`/orders/operation/patient/${patientId}`);
};

/**
 * 获取常用操作项目列表
 */
export const getCommonOperations = () => {
  return api.get('/orders/operation/common-operations');
};
