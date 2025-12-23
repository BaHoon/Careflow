import api from '../utils/api';

/**
 * 护理相关API
 */

/**
 * 获取病区床位概览
 * @param {string} wardId - 病区ID（可选）
 * @param {string} departmentId - 科室ID（可选）
 */
export const getWardOverview = (wardId = null, departmentId = null) => {
  const params = {};
  if (wardId) params.wardId = wardId;
  if (departmentId) params.departmentId = departmentId;
  
  return api.get('/Nursing/ward-overview', { params });
};

/**
 * 获取护士的待办任务列表
 * @param {string} nurseId - 护士ID
 * @param {string} date - 查询日期（可选，格式：YYYY-MM-DD）
 * @param {string} status - 任务状态（可选）
 */
export const getMyTasks = (nurseId, date = null, status = null) => {
  const params = { nurseId };
  if (date) params.date = date;
  if (status) params.status = status;
  
  return api.get('/Nursing/my-tasks', { params });
};

/**
 * 生成今日护理任务
 * @param {string} deptId - 科室ID
 */
export const generateDailyTasks = (deptId) => {
  return api.post('/Nursing/tasks/generate', null, {
    params: { deptId }
  });
};

/**
 * 提交体征数据
 * @param {object} data - 提交的数据包
 */
export const submitVitalSigns = (data) => {
  return api.post('/Nursing/tasks/submit', data);
};

/**
 * 获取指定患者的护理任务（护理记录功能使用）
 * @param {string} patientId - 患者ID
 * @param {string} date - 查询日期（可选，格式：YYYY-MM-DD）
 */
export const getPatientNursingTasks = (patientId, date = null) => {
  const params = { patientId };
  if (date) params.date = date;
  
  return api.get('/Nursing/patient-nursing-tasks', { params });
};

/**
 * 获取护理任务详情
 * @param {number} taskId - 任务ID
 */
export const getNursingTaskDetail = (taskId) => {
  return api.get(`/Nursing/tasks/${taskId}`);
};

/**
 * 获取体征记录历史
 * @param {string} patientId - 患者ID
 * @param {string} startDate - 开始日期 (可选)
 * @param {string} endDate - 结束日期 (可选)
 */
export const getVitalSignsHistory = (patientId, startDate = null, endDate = null) => {
  const params = { patientId };
  if (startDate) params.startDate = startDate;
  if (endDate) params.endDate = endDate;
  
  return api.get('/Nursing/vitalsigns/history', { params });
};
