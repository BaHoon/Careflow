import api from '../utils/api';

/**
 * 医院配置相关API
 */

/**
 * 获取医院时段配置
 */
export const getTimeSlots = () => {
  return api.get('/HospitalConfig/time-slots');
};

/**
 * 获取给药途径字典
 */
export const getUsageRoutes = () => {
  return api.get('/HospitalConfig/usage-routes');
};
