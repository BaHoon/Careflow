import api from '../utils/api';

/**
 * 护士排班相关API
 */

/**
 * 获取排班列表（支持筛选）
 * @param {Object} params - 查询参数
 * @param {string} params.startDate - 开始日期 (YYYY-MM-DD)
 * @param {string} params.endDate - 结束日期 (YYYY-MM-DD)
 * @param {string} params.wardId - 病区ID（可选）
 * @param {string} params.nurseId - 护士ID（可选）
 * @returns {Promise<Object>} 排班列表
 */
export const getScheduleList = (params = {}) => {
  return api.get('/nurse/schedule/list', { params });
};

/**
 * 获取所有病区列表
 * @returns {Promise<Array>} 病区列表
 */
export const getWards = () => {
  return api.get('/nurse/schedule/wards');
};

/**
 * 获取所有班次类型列表
 * @returns {Promise<Array>} 班次类型列表
 */
export const getShiftTypes = () => {
  return api.get('/nurse/schedule/shift-types');
};

/**
 * 获取护士当前排班的病区
 * @param {string} nurseId - 护士ID
 * @returns {Promise<Object>} 当前排班病区信息
 */
export const getCurrentWard = (nurseId) => {
  return api.get(`/nurse/schedule/current-ward/${nurseId}`);
};

