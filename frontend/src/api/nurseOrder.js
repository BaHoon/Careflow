import api from '../utils/api';
import { queryOrders, getOrderDetail } from './doctorOrder';

/**
 * ==============================
 * 【护士端医嘱查询API】
 * 复用医生端接口，添加护士端特有的业务逻辑封装
 * 对应后端 DoctorOrderController (复用)
 * ==============================
 */

/**
 * 护士端医嘱查询 - 多患者批量查询
 * 并发查询多个患者的医嘱，合并结果并添加患者信息
 * 
 * @param {Array<Object>} patients - 患者列表
 * @param {string} patients[].patientId - 患者ID
 * @param {string} patients[].patientName - 患者姓名
 * @param {string} patients[].bedId - 床位号
 * @param {Object} filters - 筛选条件
 * @param {Array<number>} filters.statuses - 医嘱状态列表（可选）
 * @param {Array<string>} filters.orderTypes - 医嘱类型列表（可选）
 * @param {string} filters.createTimeFrom - 创建时间起始（可选）
 * @param {string} filters.createTimeTo - 创建时间结束（可选）
 * @param {string} filters.sortBy - 排序字段（可选）
 * @param {boolean} filters.sortDescending - 是否降序（可选）
 * @returns {Promise<Object>} 包含 orders 和 totalCount 的结果对象
 * 
 * @example
 * const result = await queryMultiPatientOrders(
 *   [{ patientId: 'P001', patientName: '张三', bedId: '01' }],
 *   { statuses: [1, 2, 3], orderTypes: ['MedicationOrder'] }
 * );
 */
export async function queryMultiPatientOrders(patients, filters = {}) {
  if (!patients || patients.length === 0) {
    return { orders: [], totalCount: 0 };
  }

  try {
    // 并发查询所有患者的医嘱
    const promises = patients.map(patient => 
      queryOrders({
        patientId: patient.patientId,
        ...filters
      }).catch(error => {
        // 单个患者查询失败不影响其他患者
        console.error(`查询患者 ${patient.patientId} 的医嘱失败:`, error);
        return { orders: [], totalCount: 0 };
      })
    );
    
    const results = await Promise.all(promises);
    
    // 合并所有医嘱，并添加患者信息
    let allOrders = [];
    results.forEach((result, index) => {
      const patient = patients[index];
      if (result.orders && result.orders.length > 0) {
        result.orders.forEach(order => {
          // 为每个医嘱添加患者信息（用于多患者视图）
          allOrders.push({
            ...order,
            patientName: patient.patientName,
            bedId: patient.bedId,
            wardId: patient.wardId,
            wardName: patient.wardName
          });
        });
      }
    });
    
    return {
      orders: allOrders,
      totalCount: allOrders.length
    };
  } catch (error) {
    console.error('批量查询医嘱失败:', error);
    throw error;
  }
}

/**
 * 判断是否为"新开医嘱"
 * 定义：创建时间在最近指定小时内的医嘱
 * 
 * @param {Object} order - 医嘱对象
 * @param {string} order.createTime - 创建时间（ISO 8601格式）
 * @param {number} hoursThreshold - 时间阈值（小时），默认24小时
 * @returns {boolean} 是否为新开医嘱
 */
export function isNewlyCreatedOrder(order, hoursThreshold = 24) {
  if (!order.createTime) return false;
  
  const now = new Date();
  const createTime = new Date(order.createTime);
  const diffMs = now - createTime;
  const diffHours = diffMs / (1000 * 60 * 60);
  
  return diffHours <= hoursThreshold && diffHours >= 0;
}

/**
 * 判断是否为"新停医嘱"
 * 定义：停嘱时间在最近指定小时内且状态为 Stopped(5) 的医嘱
 * 
 * @param {Object} order - 医嘱对象
 * @param {string} order.stopOrderTime - 停嘱时间（ISO 8601格式）
 * @param {number} order.status - 医嘱状态（5=Stopped）
 * @param {number} hoursThreshold - 时间阈值（小时），默认24小时
 * @returns {boolean} 是否为新停医嘱
 */
export function isNewlyStoppedOrder(order, hoursThreshold = 24) {
  // 必须有停嘱时间，且状态为已停止(5)
  if (!order.stopOrderTime || order.status !== 5) {
    return false;
  }
  
  const now = new Date();
  const stopTime = new Date(order.stopOrderTime);
  const diffMs = now - stopTime;
  const diffHours = diffMs / (1000 * 60 * 60);
  
  return diffHours <= hoursThreshold && diffHours >= 0;
}

/**
 * 应用"新开/新停"筛选
 * 
 * @param {Array<Object>} orders - 医嘱列表
 * @param {Object} options - 筛选选项
 * @param {boolean} options.showNewCreated - 是否显示新开医嘱
 * @param {boolean} options.showNewStopped - 是否显示新停医嘱
 * @param {number} options.hoursThreshold - 时间阈值（小时）
 * @returns {Array<Object>} 筛选后的医嘱列表
 */
export function applyNewOrderFilter(orders, options = {}) {
  const {
    showNewCreated = false,
    showNewStopped = false,
    hoursThreshold = 24
  } = options;

  // 如果两个选项都未勾选，返回全部
  if (!showNewCreated && !showNewStopped) {
    return orders;
  }

  return orders.filter(order => {
    const isNew = isNewlyCreatedOrder(order, hoursThreshold);
    const isNewStopped = isNewlyStoppedOrder(order, hoursThreshold);

    // 两个选项都勾选：满足任一条件即可
    if (showNewCreated && showNewStopped) {
      return isNew || isNewStopped;
    }
    // 只勾选新开
    else if (showNewCreated) {
      return isNew;
    }
    // 只勾选新停
    else {
      return isNewStopped;
    }
  });
}

/**
 * 应用内容搜索筛选
 * 根据医嘱摘要（summary）字段进行模糊匹配
 * 
 * @param {Array<Object>} orders - 医嘱列表
 * @param {string} keyword - 搜索关键词
 * @returns {Array<Object>} 筛选后的医嘱列表
 * 
 * @remarks
 * summary 字段由后端生成，包含：
 * - 药品医嘱：药品名称、规格、剂量
 * - 检查医嘱：检查项目名称（itemName）
 * - 手术医嘱：手术名称
 * - 操作医嘱：操作名称（TODO: 待确认字段结构）
 */
export function applyContentSearch(orders, keyword) {
  if (!keyword || keyword.trim() === '') {
    return orders;
  }

  const searchTerm = keyword.trim().toLowerCase();
  
  return orders.filter(order => {
    // 主要搜索 summary 字段
    if (order.summary && order.summary.toLowerCase().includes(searchTerm)) {
      return true;
    }
    
    // 辅助搜索：患者姓名和床号（多患者模式下有用）
    if (order.patientName && order.patientName.toLowerCase().includes(searchTerm)) {
      return true;
    }
    
    if (order.bedId && order.bedId.toLowerCase().includes(searchTerm)) {
      return true;
    }
    
    return false;
  });
}

/**
 * 按患者分组医嘱
 * 用于"按患者排序"模式下的医嘱展示
 * 
 * @param {Array<Object>} orders - 医嘱列表
 * @returns {Object} 分组后的医嘱对象 { patientId: [orders] }
 */
export function groupOrdersByPatient(orders) {
  const grouped = {};
  
  orders.forEach(order => {
    const patientId = order.patientId;
    if (!grouped[patientId]) {
      grouped[patientId] = [];
    }
    grouped[patientId].push(order);
  });
  
  return grouped;
}

// ==================== 复用医生端接口 ====================

/**
 * 查询单个患者医嘱列表
 * 直接复用医生端接口
 */
export { queryOrders };

/**
 * 获取医嘱详细信息
 * 直接复用医生端接口
 */
export { getOrderDetail };

// ==================== 护士端特有功能（TODO：等待后端接口） ====================

/**
 * 修改任务执行情况
 * 
 * @param {number} taskId - 任务ID
 * @param {Object} data - 执行情况数据
 * @param {string} data.actualStartTime - 实际开始时间（可选）
 * @param {string} data.actualEndTime - 实际结束时间（可选）
 * @param {string} data.executionNote - 执行备注（可选）
 * @param {number} data.status - 任务状态（可选）
 * @returns {Promise<Object>} 操作结果
 * 
 * @todo 等待后端提供 /api/nurse/tasks/{taskId}/update-execution 接口
 */
export function updateTaskExecution(taskId, data) {
  // TODO: 后端接口开发完成后启用
  console.warn('TODO: updateTaskExecution 接口尚未实现');
  return Promise.reject(new Error('此功能接口尚未实现，请等待后端开发'));
  
  // 预期实现：
  // return api.post(`/nurse/tasks/${taskId}/update-execution`, data);
}

/**
 * 打印任务执行单
 * 
 * @param {number} taskId - 任务ID
 * @returns {Promise<Blob>} PDF文件的Blob对象
 * 
 * @todo 等待后端提供 /api/nurse/tasks/{taskId}/print 接口
 */
export function printTaskExecutionSheet(taskId) {
  // TODO: 后端接口开发完成后启用
  console.warn('TODO: printTaskExecutionSheet 接口尚未实现');
  return Promise.reject(new Error('此功能接口尚未实现，请等待后端开发'));
  
  // 预期实现：
  // return api.get(`/nurse/tasks/${taskId}/print`, { 
  //   responseType: 'blob' 
  // });
}

/**
 * 批量打印执行单
 * 
 * @param {Array<number>} taskIds - 任务ID列表
 * @returns {Promise<Blob>} PDF文件的Blob对象
 * 
 * @todo 等待后端提供批量打印接口
 */
export function batchPrintTaskExecutionSheets(taskIds) {
  // TODO: 后端接口开发完成后启用
  console.warn('TODO: batchPrintTaskExecutionSheets 接口尚未实现');
  return Promise.reject(new Error('此功能接口尚未实现，请等待后端开发'));
  
  // 预期实现：
  // return api.post('/nurse/tasks/batch-print', { taskIds }, {
  //   responseType: 'blob'
  // });
}
