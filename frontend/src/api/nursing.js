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
 * @param {string} resultPayload - 执行结果（仅ResultPending任务使用，可选）
 * @param {string} executionRemarks - 执行备注（可选）
 */
export const completeExecutionTask = (taskId, nurseId, resultPayload = null, executionRemarks = null) => {
  return api.post(`/Nursing/execution-tasks/${taskId}/complete`, { 
    nurseId, 
    resultPayload,
    executionRemarks
  });
};

/**
 * 取消执行任务
 * @param {number} taskId - 任务ID
 * @param {string} nurseId - 护士ID
 * @param {string} cancelReason - 取消理由
 * @param {boolean} needReturn - 是否需要直接退药（仅对AppliedConfirmed状态有效）
 */
export const cancelExecutionTask = (taskId, nurseId, cancelReason, needReturn = false) => {
  const payload = { 
    nurseId, 
    cancelReason,
    needReturn
  };
  const url = `/Nursing/execution-tasks/${taskId}/cancel`;
  
  console.log('====================================');
  console.log('[cancelExecutionTask API] 准备发送请求');
  console.log('完整URL:', `/api${url}`);
  console.log('请求方法: POST');
  console.log('请求头:', {
    'Content-Type': 'application/json',
    'Authorization': localStorage.getItem('token') ? 'Bearer ***' : '无'
  });
  console.log('请求体 payload:', JSON.stringify(payload, null, 2));
  console.log('====================================');
  
  return api.post(url, payload)
    .then(response => {
      console.log('====================================');
      console.log('[cancelExecutionTask API] 收到响应');
      console.log('响应状态:', response.status);
      console.log('响应数据:', response.data);
      console.log('====================================');
      return response.data;
    })
    .catch(error => {
      console.error('====================================');
      console.error('[cancelExecutionTask API] 请求失败');
      console.error('错误对象:', error);
      console.error('错误消息:', error.message);
      if (error.response) {
        console.error('响应状态:', error.response.status);
        console.error('响应数据:', error.response.data);
        console.error('响应头:', error.response.headers);
      } else if (error.request) {
        console.error('请求已发送但没有收到响应');
        console.error('请求详情:', error.request);
      }
      console.error('====================================');
      throw error;
    });
};

/**
 * 获取护士待签收医嘱统计
 * @param {string} nurseId - 护士ID
 * @returns {Promise<number>} 待签收医嘱总数（包括新开和停止医嘱）
 */
export const getPendingOrdersCount = (nurseId) => {
  return api.get('/Nursing/pending-orders-count', {
    params: { nurseId }
  });
};

/**
 * 获取护士负责患者的待退药申请统计
 * @param {string} nurseId - 护士ID
 * @param {string} departmentId - 科室ID
 * @returns {Promise<number>} 待退药申请总数（包括待退药和异常取消待退药）
 */
export const getPendingReturnsCount = (nurseId, departmentId) => {
  return api.get('/Nursing/pending-returns-count', {
    params: { nurseId, departmentId }
  });
};
/**
 * 创建补充护理任务
 * @param {object} data - 补充任务数据 { patientId, assignedNurseId, description }
 */
export const createSupplementNursingTask = (data) => {
  return api.post('/Nursing/tasks/create-supplement', data);
};