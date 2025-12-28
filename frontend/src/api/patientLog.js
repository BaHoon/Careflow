import api from '../utils/api';

/**
 * 患者日志相关API
 */

/**
 * 获取患者日志数据
 * @param {Object} params
 * @param {string} params.patientId - 患者ID (必填)
 * @param {string} params.startDate - 开始日期 (可选，格式: YYYY-MM-DD)
 * @param {string} params.endDate - 结束日期 (可选，格式: YYYY-MM-DD)
 * @param {string} params.contentTypes - 内容类型 (可选，逗号分隔: MedicalOrders,NursingRecords,ExamReports)
 * @returns {Promise<Object>} 患者日志数据
 */
export const getPatientLog = (params) => {
  return api.get('/PatientLog', { params });
};
