import api from '../utils/api'

/**
 * 获取科室列表
 */
export async function getDepartmentList(params) {
  return await api.get('/Department/list', { params })
}

/**
 * 获取科室详情
 */
export async function getDepartmentById(departmentId) {
  return await api.get(`/Department/${departmentId}`)
}

/**
 * 创建科室
 */
export async function createDepartment(data) {
  return await api.post('/Department', data)
}

/**
 * 更新科室
 */
export async function updateDepartment(departmentId, data) {
  return await api.put(`/Department/${departmentId}`, data)
}

/**
 * 删除科室
 */
export async function deleteDepartment(departmentId) {
  return await api.delete(`/Department/${departmentId}`)
}

/**
 * 获取所有启用的科室
 */
export async function getActiveDepartments() {
  return await api.get('/Department/active')
}
