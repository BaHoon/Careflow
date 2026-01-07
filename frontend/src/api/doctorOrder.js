import api from '../utils/api';

/**
 * 医生端医嘱查询API
 * 对应后端 DoctorOrderController
 */

/**
 * 查询患者医嘱列表（支持多条件筛选）
 * @param {Object} data - 查询条件
 * @param {string} data.patientId - 患者ID（必填）
 * @param {Array<number>} data.statuses - 医嘱状态列表（可选）
 * @param {Array<string>} data.orderTypes - 医嘱类型列表（可选）
 * @param {string} data.createTimeFrom - 创建时间起始（可选）
 * @param {string} data.createTimeTo - 创建时间结束（可选）
 * @param {string} data.sortBy - 排序字段（可选）
 * @param {boolean} data.sortDescending - 是否降序（可选）
 * @returns {Promise} 医嘱列表和统计信息
 */
export function queryOrders(data) {
  return api.post('/doctor/orders/query', data);
}

/**
 * 获取医嘱详细信息（包含关联任务列表）
 * @param {number} orderId - 医嘱ID
 * @returns {Promise} 医嘱详情
 */
export function getOrderDetail(orderId) {
  return api.get(`/doctor/orders/${orderId}/detail`);
}

/**
 * 医生停止医嘱
 * @param {Object} data - 停嘱请求
 * @param {number} data.orderId - 医嘱ID
 * @param {string} data.doctorId - 医生ID
 * @param {string} data.stopReason - 停嘱原因
 * @param {number} data.stopAfterTaskId - 停止节点任务ID
 * @returns {Promise} 停嘱结果
 */
export function stopOrder(data) {
  return api.post('/doctor/orders/stop', data);
}

/**
 * 重新提交已退回的医嘱
 * @param {number} orderId - 医嘱ID
 * @param {string} doctorId - 医生ID
 * @returns {Promise} 操作结果
 */
export function resubmitRejectedOrder(orderId, doctorId) {
  return api.post(`/doctor/orders/${orderId}/resubmit`, { doctorId });
}

/**
 * 撤销已退回的医嘱
 * @param {number} orderId - 医嘱ID
 * @param {string} doctorId - 医生ID
 * @param {string} cancelReason - 撤销原因
 * @returns {Promise} 操作结果
 */
export function cancelRejectedOrder(orderId, doctorId, cancelReason) {
  return api.post(`/doctor/orders/${orderId}/cancel`, { doctorId, cancelReason });
}

/**
 * 医生撤回停嘱申请
 * @param {Object} data - 撤回请求
 * @param {number} data.orderId - 医嘱ID
 * @param {string} data.doctorId - 医生ID
 * @param {string} data.withdrawReason - 撤回原因
 * @returns {Promise} 撤回结果
 */
export function withdrawStopOrder(data) {
  return api.post('/doctor/orders/withdraw-stop', data);
}

/**
 * 处理异常态医嘱
 * @param {Object} data - 处理请求
 * @param {number} data.orderId - 医嘱ID
 * @param {string} data.doctorId - 医生ID
 * @param {string} data.handleNote - 处理说明
 * @returns {Promise} 处理结果
 */
export function handleAbnormalTask(data) {
  return api.post('/doctor/orders/handle-abnormal', data);
}
