import api from '../utils/api';

/**
 * ==============================
 * 【患者管理相关API】
 * 提供通用的患者数据获取接口
 * 可在多个模块中复用
 * ==============================
 */

// ==================== 基础患者信息 ====================

/**
 * 获取患者列表（基础版）
 * @param {string} departmentId - 科室ID（可选）
 * @param {string} wardId - 病区ID（可选）
 * @returns {Promise<Array>} 患者列表
 */
export const getPatientList = (departmentId = null, wardId = null) => {
  const params = {};
  if (departmentId) params.departmentId = departmentId;
  if (wardId) params.wardId = wardId;
  
  return api.get('/Patient/list', { params });
};

/**
 * 获取科室患者列表（含待处理统计）
 * 用于医嘱签收、护理任务等需要显示待处理数量的场景
 * @param {string} deptCode - 科室代码
 * @returns {Promise<Array>} 患者列表及统计信息
 * @example
 * [{
 *   patientId: "P001",
 *   patientName: "张三",
 *   bedId: "101",
 *   gender: "男",
 *   age: 45,
 *   weight: 70,
 *   nursingGrade: 2,
 *   wardId: "W01",
 *   wardName: "内科一病区",
 *   unacknowledgedCount: 3  // 待签收医嘱数量
 * }]
 */
export const getPatientsWithPendingCount = (deptCode) => {
  return api.get('/orders/acknowledgement/pending-summary', { 
    params: { deptCode } 
  });
};

/**
 * 获取患者详情
 * @param {string} patientId - 患者ID
 * @returns {Promise<Object>} 患者详细信息
 */
export const getPatientDetail = (patientId) => {
  return api.get(`/Patient/${patientId}`);
};

// ==================== 护士排班相关 ====================

/**
 * 获取护士当前排班的病区
 * @param {string} nurseId - 护士ID
 * @returns {Promise<Object>} 当前排班的病区信息
 * @example
 * {
 *   wardId: "IM-W01",
 *   wardName: "内科一病区",
 *   shiftType: "day",
 *   scheduleDate: "2024-01-20"
 * }
 */
export const getCurrentWard = (nurseId) => {
  return api.get(`/nurse/schedule/current-ward/${nurseId}`);
};

/**
 * 获取护士负责的患者列表
 * @param {string} nurseId - 护士ID
 * @param {string} shiftDate - 班次日期（可选，默认今天）
 * @returns {Promise<Array>} 负责的患者列表
 */
export const getAssignedPatients = (nurseId, shiftDate = null) => {
  const params = { nurseId };
  if (shiftDate) params.shiftDate = shiftDate;
  
  return api.get('/nurse/assigned-patients', { params });
};

// ==================== 患者筛选辅助 ====================

/**
 * 获取科室下所有病区列表
 * @param {string} deptCode - 科室代码
 * @returns {Promise<Array>} 病区列表
 * @example
 * [{
 *   wardId: "IM-W01",
 *   wardName: "内科一病区",
 *   bedCount: 30,
 *   patientCount: 25
 * }]
 */
export const getWardsByDepartment = (deptCode) => {
  return api.get('/hospital-config/wards', { 
    params: { deptCode } 
  });
};
