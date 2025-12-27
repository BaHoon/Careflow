import api from '../utils/api';

/**
 * 查询医嘱状态变更历史
 * @param {Object} filters - 筛选条件
 * @returns {Promise}
 */
export async function queryOrderStatusHistory(filters) {
  return await api.post('/Admin/order-status-history', filters);
}

/**
 * 查询人员列表
 * @param {Object} params - 查询参数
 * @returns {Promise}
 */
export async function queryStaffList(params) {
  return await api.post('/Admin/staff-list', params);
}

/**
 * 添加新人员
 * @param {Object} data - 人员信息
 * @returns {Promise}
 */
export async function addStaff(data) {
  return await api.post('/Admin/add-staff', data);
}

/**
 * 重置人员密码
 * @param {Object} data - 包含staffId和newPassword
 * @returns {Promise}
 */
export async function resetPassword(data) {
  return await api.post('/Admin/reset-password', data);
}

/**
 * 更新员工信息
 * @param {Object} data - 包含staffId和deptCode
 * @returns {Promise}
 */
export async function updateStaff(data) {
  return await api.post('/Admin/update-staff', data);
}

/**
 * 删除员工
 * @param {string} staffId - 员工ID
 * @returns {Promise}
 */
export async function deleteStaff(staffId) {
  return await api.delete(`/Admin/delete-staff/${staffId}`);
}
