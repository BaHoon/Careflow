import api from '../utils/api';

/**
 * 患者管理相关API
 */

/**
 * 获取患者列表
 * @param {string} departmentId - 科室ID（可选）
 * @param {string} wardId - 病区ID（可选）
 */
export const getPatientList = (departmentId = null, wardId = null) => {
  const params = {};
  if (departmentId) params.departmentId = departmentId;
  if (wardId) params.wardId = wardId;
  
  return api.get('/Patient/list', { params });
};

/**
 * 获取患者详情
 * @param {string} patientId - 患者ID
 */
export const getPatientDetail = (patientId) => {
  return api.get(`/Patient/${patientId}`);
};
