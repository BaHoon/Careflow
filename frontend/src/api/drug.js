import api from '../utils/api';

/**
 * 药品管理相关API
 */

/**
 * 获取药品列表
 * @param {Object} params - 查询参数
 * @param {string} params.keyword - 搜索关键词
 * @param {number} params.page - 页码
 * @param {number} params.pageSize - 每页数量
 * @param {string} params.category - 药品分类
 */
export const getDrugList = (params = {}) => {
  return api.get('/Drug/list', { 
    params: {
      keyword: params.keyword || null,
      page: params.page || 1,
      pageSize: params.pageSize || 100,
      category: params.category || null
    }
  });
};

/**
 * 获取药品详情
 * @param {string} drugId - 药品ID
 */
export const getDrugDetail = (drugId) => {
  return api.get(`/Drug/${drugId}`);
};
