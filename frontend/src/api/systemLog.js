import api from '../utils/api'

/**
 * 查询系统日志
 */
export async function getSystemLogs(params) {
  return await api.get('/SystemLog', { params })
}

/**
 * 获取操作类型列表
 */
export async function getOperationTypes() {
  return await api.get('/SystemLog/operation-types')
}

/**
 * 记录登录日志
 */
export async function logLogin(data) {
  return await api.post('/SystemLog/login', data)
}

/**
 * 记录登出日志
 */
export async function logLogout(data) {
  return await api.post('/SystemLog/logout', data)
}
