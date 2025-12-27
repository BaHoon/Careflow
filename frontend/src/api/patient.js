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
  }).then(response => {
    // 确保每个患者都有 unacknowledgedCount 字段（徽章显示）
    return response.map(patient => ({
      ...patient,
      unacknowledgedCount: patient.unacknowledgedCount || 0,
      // 暂时用 unacknowledgedCount 作为待申请数量的占位符
      // 未来可以从后端获取真实的待申请数量
      pendingApplicationCount: patient.unacknowledgedCount || 0
    }));
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

// ==================== 患者管理接口 ====================

/**
 * 【患者管理】获取患者管理列表（支持筛选和搜索）
 * @param {Object} params - 查询参数
 * @param {number} params.status - 患者状态（0=待入院, 1=在院, 2=待出院, 3=已出院）可选
 * @param {string} params.keyword - 搜索关键词（患者ID/身份证号/姓名）可选
 * @param {string} params.departmentId - 科室ID（可选）
 * @param {string} params.wardId - 病区ID（可选）
 * @returns {Promise<Array>} 患者卡片列表
 * @example
 * // 获取所有在院患者
 * const patients = await getPatientManagementList({ status: 1 });
 * 
 * // 搜索患者
 * const patients = await getPatientManagementList({ keyword: '张三' });
 * 
 * // 返回数据格式
 * [{
 *   id: "P001",
 *   name: "张三",
 *   gender: "男",
 *   age: 34,
 *   bedId: "IM-W01-001",
 *   nursingGrade: 2,
 *   status: 1,
 *   statusDisplay: "在院",
 *   department: "内科",
 *   ward: "IM-W01"
 * }]
 */
export const getPatientManagementList = (params = {}) => {
  return api.get('/Patient/management/list', { params });
};

/**
 * 【患者管理】获取患者完整信息
 * @param {string} patientId - 患者ID
 * @returns {Promise<Object>} 患者完整信息
 * @example
 * const patient = await getPatientFullInfo('P001');
 * 
 * // 返回数据格式
 * {
 *   // 基本信息（不可修改）
 *   id: "P001",
 *   name: "张三",
 *   idCard: "110100199001010001",
 *   
 *   // 可修改字段
 *   gender: "男",
 *   dateOfBirth: "1990-01-01T00:00:00Z",
 *   age: 34,
 *   height: 175.0,
 *   weight: 70.5,
 *   phoneNumber: "13800138001",
 *   outpatientDiagnosis: "高血压2级，糖尿病",
 *   scheduledAdmissionTime: "2024-12-01T08:00:00Z",
 *   actualAdmissionTime: "2024-12-01T09:30:00Z",
 *   nursingGrade: 2,
 *   
 *   // 关联信息（只读）
 *   bedId: "IM-W01-001",
 *   department: "内科",
 *   ward: "IM-W01",
 *   attendingDoctorName: "张医生",
 *   status: 1
 * }
 */
export const getPatientFullInfo = (patientId) => {
  return api.get(`/Patient/management/${patientId}/full`);
};

/**
 * 【患者管理】更新患者信息
 * @param {string} patientId - 患者ID
 * @param {Object} data - 更新数据（只传修改的字段）
 * @param {number} data.height - 身高（cm）可选
 * @param {number} data.weight - 体重（kg）可选
 * @param {string} data.phoneNumber - 电话号码 可选
 * @param {string} data.outpatientDiagnosis - 门诊诊断 可选
 * @param {string} data.scheduledAdmissionTime - 预约入院时间 可选
 * @param {string} data.actualAdmissionTime - 实际入院时间 可选
 * @param {number} data.nursingGrade - 护理级别（0=特级, 1=一级, 2=二级, 3=三级）可选
 * @param {string} data.operatorId - 操作人ID（必填）
 * @param {string} data.operatorType - 操作人类型（Doctor/Nurse/Admin）（必填）
 * @returns {Promise<Object>} 更新结果
 * @example
 * // 更新身高和体重
 * await updatePatientInfo('P001', {
 *   patientId: 'P001',
 *   height: 175,
 *   weight: 70.5,
 *   operatorId: 'D001',
 *   operatorType: 'Doctor'
 * });
 * 
 * // 返回数据格式
 * {
 *   message: "患者信息更新成功",
 *   data: {
 *     patientId: "P001",
 *     changesCount: 2,
 *     changes: ["身高: 170cm → 175cm", "体重: 68kg → 70.5kg"]
 *   }
 * }
 */
export const updatePatientInfo = (patientId, data) => {
  // 确保 patientId 在请求体中
  const requestData = {
    ...data,
    patientId: patientId
  };
  
  return api.put(`/Patient/management/${patientId}`, requestData);
};

/**
 * 【患者管理】出院前检查
 * @param {string} patientId - 患者ID
 * @returns {Promise<Object>} 出院检查结果
 * @example
 * const result = await checkPatientDischarge('P002');
 * 
 * // 返回数据格式
 * {
 *   patientId: "P002",
 *   canDischarge: false,
 *   message: "患者有 2 条未完成的医嘱，请先处理后再办理出院",
 *   unfinishedTasks: [
 *     {
 *       orderId: 123,
 *       orderType: "MedicationOrder",
 *       orderSummary: "阿司匹林口服",
 *       status: "InProgress",
 *       statusDisplay: "进行中（有2个未完成任务）",
 *       unfinishedTaskCount: 2,
 *       latestTaskTime: "2024-12-26T18:00:00Z"
 *     }
 *   ]
 * }
 */
export const checkPatientDischarge = (patientId) => {
  return api.get(`/Patient/management/${patientId}/discharge-check`);
};

/**
 * 【患者管理】办理出院
 * @param {string} patientId - 患者ID
 * @param {Object} data - 出院数据
 * @param {string} data.patientId - 患者ID（必填，会自动设置）
 * @param {string} data.operatorId - 操作人ID（必填）
 * @param {string} data.operatorType - 操作人类型：'Nurse' 或 'Doctor'（必填）
 * @param {string} [data.remarks] - 出院备注（可选）
 * @returns {Promise<Object>} 出院处理结果
 * @example
 * await processPatientDischarge('P002', {
 *   operatorId: 'N001',
 *   operatorType: 'Nurse',
 *   remarks: '患者康复良好，符合出院标准'
 * });
 */
export const processPatientDischarge = (patientId, data) => {
  const requestData = {
    patientId: patientId,
    operatorId: data.operatorId,
    operatorType: data.operatorType,
    remarks: data.remarks || null
  };
  
  return api.post(`/Patient/management/${patientId}/discharge`, requestData);
};

// ==================== 患者状态枚举 ====================

/**
 * 患者状态枚举
 */
export const PatientStatus = {
  PendingAdmission: 0,  // 待入院
  Hospitalized: 1,      // 在院
  PendingDischarge: 2,  // 待出院
  Discharged: 3         // 已出院
};

/**
 * 护理级别枚举
 */
export const NursingGrade = {
  Special: 0,  // 特级护理
  Grade1: 1,   // 一级护理
  Grade2: 2,   // 二级护理
  Grade3: 3    // 三级护理
};

/**
 * 获取患者状态显示名称
 * @param {number} status - 状态值
 * @returns {string} 状态显示名称
 */
export const getPatientStatusText = (status) => {
  const statusMap = {
    0: '待入院',
    1: '在院',
    2: '待出院',
    3: '已出院'
  };
  return statusMap[status] || `未知状态(${status})`;
};

/**
 * 获取护理级别显示名称
 * @param {number} grade - 护理级别值
 * @returns {string} 护理级别显示名称
 */
export const getNursingGradeText = (grade) => {
  const gradeMap = {
    0: '特级护理',
    1: '一级护理',
    2: '二级护理',
    3: '三级护理'
  };
  return gradeMap[grade] || `未知级别(${grade})`;
};

/**
 * 获取患者状态对应的标签颜色
 * @param {number} status - 状态值
 * @returns {string} Element Plus 标签类型
 */
export const getPatientStatusColor = (status) => {
  const colorMap = {
    0: 'warning',   // 待入院 - 橙色
    1: 'primary',   // 在院 - 蓝色
    2: 'success',   // 待出院 - 绿色
    3: 'info'       // 已出院 - 灰色
  };
  return colorMap[status] || 'info';
};
