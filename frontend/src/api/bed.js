import api from '../utils/api'

export const bedApi = {
  // 获取科室下的所有病床
  getBedsByDepartmentId: (departmentId) => 
    api.get(`/Bed/department/${departmentId}`),

  // 创建新病床
  createBed: (data) => 
    api.post('/Bed', data),

  // 删除病床
  deleteBed: (bedId) => 
    api.delete(`/Bed/${bedId}`)
}
