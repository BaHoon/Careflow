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

/**
 * 取消护理任务
 * @param {number} taskId - 任务ID
 * @param {string} nurseId - 护士ID
 * @param {string} cancelReason - 取消理由
 */
export const cancelNursingTask = (taskId, nurseId, cancelReason = '') => {
  console.log('调用cancelNursingTask API - taskId:', taskId, 'nurseId:', nurseId, 'reason:', cancelReason);
  
  if (!taskId) {
    console.error('taskId is invalid:', taskId);
    throw new Error('任务ID无效');
  }
  
  if (!nurseId) {
    console.error('nurseId is invalid:', nurseId);
    throw new Error('护士ID无效');
  }
  
  const url = `/Nursing/tasks/${taskId}/cancel`;
  console.log('请求URL:', url, 'params:', { nurseId, cancelReason });
  
  return api.post(url, null, {
    params: { nurseId, cancelReason }
  });
};

/**
 * 添加护理记录补充说明
 * @param {object} data - 补充说明数据 { nursingTaskId, supplementNurseId, content, supplementType }
 */
export const addSupplement = (data) => {
  return api.post('/Nursing/tasks/supplement', data);
};

/**
 * 获取护理记录的补充说明列表
 * @param {number} taskId - 护理任务ID
 */
export const getSupplements = (taskId) => {
  return api.get(`/Nursing/tasks/${taskId}/supplements`);
};

// ==================== ExecutionTask 操作接口 ====================

/**
 * 开始执行任务
 * @param {number} taskId - 任务ID
 * @param {string} nurseId - 护士ID
 */
export const startExecutionTask = (taskId, nurseId) => {
  return api.post(`/Nursing/execution-tasks/${taskId}/start`, { nurseId });
};

/**
 * 完成执行任务
 * @param {number} taskId - 任务ID
 * @param {string} nurseId - 护士ID
 * @param {string} resultPayload - 执行结果（JSON字符串，可选）
 */
export const completeExecutionTask = (taskId, nurseId, resultPayload = null) => {
  return api.post(`/Nursing/execution-tasks/${taskId}/complete`, { 
    nurseId, 
    resultPayload 
  });
};

/**
 * 取消执行任务
 * @param {number} taskId - 任务ID
 * @param {string} nurseId - 护士ID
 * @param {string} cancelReason - 取消理由
 */
export const cancelExecutionTask = (taskId, nurseId, cancelReason) => {
  return api.post(`/Nursing/execution-tasks/${taskId}/cancel`, { 
    nurseId, 
    cancelReason 
  });
};
/**
 * 创建补充护理任务
 * @param {object} data - 补充任务数据 { patientId, assignedNurseId, description }
 */
export const createSupplementNursingTask = (data) => {
  return api.post('/Nursing/tasks/create-supplement', data);
};