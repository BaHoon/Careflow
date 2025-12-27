import api from '../utils/api';

/**
 * ==============================
 * 【执行任务扫码相关API】
 * ==============================
 */

/**
 * 识别任务条形码图片
 * @param {File} taskBarcodeImage - 任务条形码图片文件
 * @returns {Promise<Object>} 识别结果，包含taskId
 */
export const recognizeTaskBarcode = (taskBarcodeImage) => {
  const formData = new FormData();
  formData.append('taskBarcodeImage', taskBarcodeImage);
  
  return api.post('/Nursing/barcode/recognize-task', formData, {
    headers: {
      'Content-Type': 'multipart/form-data'
    }
  });
};

/**
 * 通过任务条形码获取任务详情
 * @param {string} taskBarcode - 任务条形码内容
 * @returns {Promise<Object>} 任务详情
 */
export const getExecutionTaskByBarcode = (taskBarcode) => {
  return api.post('/ExecutionTask/by-barcode', { barcode: taskBarcode });
};

/**
 * 验证执行任务和患者条形码匹配
 * @param {File} executionTaskBarcode - 执行任务条形码图片
 * @param {File} patientBarcode - 患者条形码图片
 * @returns {Promise<Object>} 匹配结果 { isMatched, executionTaskId, patientId, ... }
 */
export const validateBarcodeMatch = (executionTaskBarcode, patientBarcode) => {
  const formData = new FormData();
  formData.append('executionTaskBarcode', executionTaskBarcode);
  formData.append('patientBarcode', patientBarcode);
  
  return api.post('/BarcodeMatching/validate', formData, {
    headers: {
      'Content-Type': 'multipart/form-data'
    }
  });
};

/**
 * 验证患者条形码（新图片上传API）
 * @param {number} taskId - 任务ID
 * @param {File} taskBarcodeImage - 任务条形码图片
 * @param {File} patientBarcodeImage - 患者条形码图片
 * @returns {Promise<Object>} 验证结果
 */
export const validatePatientBarcodeImage = (taskId, taskBarcodeImage, patientBarcodeImage) => {
  const formData = new FormData();
  formData.append('taskBarcodeImage', taskBarcodeImage);
  formData.append('patientBarcodeImage', patientBarcodeImage);
  
  return api.post(`/Nursing/barcode/validate-patient?taskId=${taskId}`, formData, {
    headers: {
      'Content-Type': 'multipart/form-data'
    }
  });
};

/**
 * 验证执行任务和药品条形码匹配
 * @param {File} executionTaskBarcode - 执行任务条形码图片
 * @param {File} drugBarcode - 药品条形码图片
 * @returns {Promise<Object>} 匹配结果 { isMatched, taskId, scannedDrugId, totalDrugs, confirmedDrugs, isFullyCompleted, ... }
 */
export const validateTaskDrugMatch = (executionTaskBarcode, drugBarcode) => {
  const formData = new FormData();
  formData.append('executionTaskBarcode', executionTaskBarcode);
  formData.append('drugBarcode', drugBarcode);
  
  return api.post('/BarcodeMatching/validate-drug', formData, {
    headers: {
      'Content-Type': 'multipart/form-data'
    }
  });
};

/**
 * 验证药品条形码（新图片上传API）
 * @param {number} taskId - 任务ID
 * @param {File} taskBarcodeImage - 任务条形码图片
 * @param {File} drugBarcodeImage - 药品条形码图片
 * @returns {Promise<Object>} 验证结果
 */
export const validateDrugBarcodeImage = (taskId, taskBarcodeImage, drugBarcodeImage) => {
  const formData = new FormData();
  formData.append('taskBarcodeImage', taskBarcodeImage);
  formData.append('drugBarcodeImage', drugBarcodeImage);
  
  return api.post(`/Nursing/barcode/validate-drug?taskId=${taskId}`, formData, {
    headers: {
      'Content-Type': 'multipart/form-data'
    }
  });
};

/**
 * 开始执行任务
 * @param {number} taskId - 任务ID
 * @param {string} nurseId - 护士ID
 * @returns {Promise} 结果
 */
export const startExecutionTask = (taskId, nurseId) => {
  return api.post(`/Nursing/execution-tasks/${taskId}/start`, { nurseId });
};

/**
 * 完成执行任务
 * @param {number} taskId - 任务ID
 * @param {string} nurseId - 护士ID
 * @param {string} resultPayload - 执行结果（JSON字符串，可选）
 * @returns {Promise} 结果
 */
export const completeExecutionTask = (taskId, nurseId, resultPayload = null) => {
  return api.post(`/Nursing/execution-tasks/${taskId}/complete`, {
    nurseId,
    resultPayload
  });
};

/**
 * 更新执行任务状态
 * @param {number} taskId - 任务ID
 * @param {string} status - 新状态 (Pending, InProgress, Completed)
 * @param {string} nurseId - 护士ID
 * @param {string} resultPayload - 执行结果（可选）
 * @returns {Promise} 结果
 */
export const updateExecutionTaskStatus = (taskId, status, nurseId, resultPayload = null) => {
  return api.post(`/Nursing/execution-tasks/${taskId}/update-status`, {
    status,
    nurseId,
    resultPayload
  });
};

/**
 * 获取执行任务详情
 * @param {number} taskId - 任务ID
 * @returns {Promise<Object>} 任务详情
 */
export const getExecutionTaskDetail = (taskId) => {
  return api.get(`/Nursing/execution-tasks/${taskId}`);
};
