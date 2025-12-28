<template>
  <div class="department-management">
    <!-- 固定顶部导航栏 -->
    <header class="layout-header">
      <div class="header-logo">
        <el-icon :size="24" color="#f56c6c"><Setting /></el-icon>
        <span class="logo-text">CareFlow | 管理员工作台</span>
      </div>
      
      <!-- 导航菜单 -->
      <nav class="header-nav">
        <router-link 
          to="/admin/order-history" 
          class="nav-item"
        >
          <el-icon><DocumentCopy /></el-icon>
          <span>医嘱流转记录</span>
        </router-link>
        <router-link 
          to="/staff-management" 
          class="nav-item"
        >
          <el-icon><User /></el-icon>
          <span>人员管理</span>
        </router-link>
        <router-link 
          to="/admin/department" 
          class="nav-item"
          active-class="active"
        >
          <el-icon><OfficeBuilding /></el-icon>
          <span>科室管理</span>
        </router-link>
        <router-link 
          to="/admin/system-log" 
          class="nav-item"
        >
          <el-icon><List /></el-icon>
          <span>系统日志</span>
        </router-link>
      </nav>
      
      <!-- 用户信息 -->
      <div class="header-user">
        <el-dropdown trigger="click">
          <span class="user-info">
            <el-avatar :size="32" style="background-color: #f56c6c;">
              {{ userName }}
            </el-avatar>
            <span class="user-name">{{ fullName }}</span>
            <span class="user-role">(管理员)</span>
            <el-icon class="el-icon--right"><ArrowDown /></el-icon>
          </span>
          <template #dropdown>
            <el-dropdown-menu>
              <el-dropdown-item @click="handleLogout">
                <el-icon><SwitchButton /></el-icon>
                <span>退出登录</span>
              </el-dropdown-item>
            </el-dropdown-menu>
          </template>
        </el-dropdown>
      </div>
    </header>

    <!-- 内容区域 -->
    <div class="page-content">
      <el-card>
      <template #header>
        <div class="card-header">
          <h3>科室管理</h3>
          <el-button type="primary" @click="handleAdd">
            <el-icon><Plus /></el-icon>
            新增科室
          </el-button>
        </div>
      </template>

      <!-- 搜索栏 -->
      <el-form :inline="true" :model="searchForm" class="search-form">
        <el-form-item label="关键词">
          <el-input
            v-model="searchForm.keyword"
            placeholder="科室名称或位置"
            clearable
            @clear="loadDepartments"
          />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="loadDepartments">查询</el-button>
          <el-button @click="handleReset">重置</el-button>
        </el-form-item>
      </el-form>

      <!-- 表格 -->
      <el-table
        :data="tableData"
        v-loading="loading"
        border
        stripe
      >
        <el-table-column prop="departmentId" label="科室编号" min-width="120" />
        <el-table-column prop="departmentName" label="科室名称" min-width="150" />
        <el-table-column prop="location" label="物理位置" min-width="200" />
        <el-table-column prop="bedCount" label="病床数量" min-width="100" align="center" />
        <el-table-column label="操作" min-width="250" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" @click="handleViewBeds(row)">病床管理</el-button>
            <el-button link type="primary" @click="handleEdit(row)">编辑</el-button>
            <el-button 
              link 
              type="danger" 
              @click="handleDelete(row)"
              :disabled="row.bedCount > 0"
            >
              删除
            </el-button>
          </template>
        </el-table-column>
      </el-table>

      <!-- 分页 -->
      <el-pagination
        v-model:current-page="pagination.pageIndex"
        v-model:page-size="pagination.pageSize"
        :total="pagination.total"
        :page-sizes="[10, 20, 50, 100]"
        layout="total, sizes, prev, pager, next, jumper"
        @size-change="loadDepartments"
        @current-change="loadDepartments"
        class="pagination"
      />
    </el-card>

    <!-- 新增/编辑科室对话框 -->
    <el-dialog
      v-model="dialogVisible"
      :title="dialogTitle"
      width="600px"
      @close="handleDialogClose"
    >
      <el-form
        ref="formRef"
        :model="formData"
        :rules="formRules"
        label-width="100px"
      >
        <el-form-item label="科室编号" prop="departmentId" v-if="!isEdit">
          <el-input v-model="formData.departmentId" placeholder="请输入科室编号" />
        </el-form-item>
        <el-form-item label="科室名称" prop="departmentName">
          <el-input v-model="formData.departmentName" placeholder="请输入科室名称" />
        </el-form-item>
        <el-form-item label="物理位置" prop="location">
          <el-input v-model="formData.location" placeholder="请输入物理位置" />
        </el-form-item>
      </el-form>

      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSubmit" :loading="submitting">
          确定
        </el-button>
      </template>
    </el-dialog>

    <!-- 病床管理对话框 -->
    <el-dialog
      v-model="bedDialogVisible"
      :title="`病床管理 - ${currentDepartment.departmentName}`"
      width="800px"
    >
      <div style="margin-bottom: 15px;">
        <el-button type="primary" @click="handleAddBed" :disabled="!selectedWardId">
          <el-icon><Plus /></el-icon>
          新增病床
        </el-button>
        <el-select 
          v-model="selectedWardId" 
          placeholder="选择病区" 
          style="margin-left: 10px; width: 200px;"
          @change="loadBeds"
        >
          <el-option 
            v-for="ward in wards" 
            :key="ward.id" 
            :label="ward.id" 
            :value="ward.id"
          />
        </el-select>
        <el-button type="success" @click="handleAddWard" style="margin-left: 10px;">
          <el-icon><Plus /></el-icon>
          新增病区
        </el-button>
        <el-button type="danger" @click="handleDeleteWard" :disabled="!selectedWardId" style="margin-left: 10px;">
          <el-icon><Delete /></el-icon>
          删除病区
        </el-button>
      </div>

      <el-table
        :data="bedTableData"
        v-loading="bedLoading"
        border
        stripe
        max-height="400"
      >
        <el-table-column prop="bedId" label="病床编号" width="180" />
        <el-table-column prop="wardName" label="所属病区" width="150" />
        <el-table-column prop="status" label="状态" width="100" align="center">
          <template #default="{ row }">
            <el-tag :type="row.status === '空闲' ? 'success' : 'warning'">
              {{ row.status }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="120" fixed="right">
          <template #default="{ row }">
            <el-button 
              link 
              type="danger" 
              @click="handleDeleteBed(row)"
              :disabled="row.status === '占用'"
            >
              删除
            </el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-dialog>

    <!-- 新增病区对话框 -->
    <el-dialog
      v-model="wardDialogVisible"
      title="新增病区"
      width="500px"
    >
      <el-form
        ref="wardFormRef"
        :model="wardFormData"
        :rules="wardFormRules"
        label-width="100px"
      >
        <el-form-item label="病区编号" prop="wardId">
          <el-input v-model="wardFormData.wardId" placeholder="例如：IM-W03" />
        </el-form-item>
        <el-form-item label="所属科室">
          <el-input v-model="currentDepartment.departmentName" disabled />
        </el-form-item>
      </el-form>

      <template #footer>
        <el-button @click="wardDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSubmitWard" :loading="wardSubmitting">
          确定
        </el-button>
      </template>
    </el-dialog>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Plus, DocumentCopy, User, OfficeBuilding, List, Setting, ArrowDown, SwitchButton, Delete } from '@element-plus/icons-vue'
import { useRouter } from 'vue-router'
import { getDepartmentList, createDepartment, updateDepartment, deleteDepartment } from '@/api/department'
import { bedApi } from '@/api/bed'
import api from '@/utils/api'

const router = useRouter()

// 用户信息
const userName = computed(() => {
  const user = JSON.parse(localStorage.getItem('userInfo') || '{}')
  const name = user.fullName || user.name || '管理员'
  return name.substring(0, 2)
})

const fullName = computed(() => {
  const user = JSON.parse(localStorage.getItem('userInfo') || '{}')
  return user.fullName || user.name || '管理员'
})

// 退出登录
const handleLogout = async () => {
  try {
    await ElMessageBox.confirm('确定要退出登录吗？', '提示', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    })
    localStorage.removeItem('token')
    localStorage.removeItem('user')
    router.push('/login')
  } catch (error) {
    // 取消退出
  }
}

// 搜索表单
const searchForm = reactive({
  keyword: ''
})

// 分页
const pagination = reactive({
  pageIndex: 1,
  pageSize: 20,
  total: 0
})

// 表格数据
const tableData = ref([])
const loading = ref(false)

// 对话框
const dialogVisible = ref(false)
const dialogTitle = computed(() => isEdit.value ? '编辑科室' : '新增科室')
const isEdit = ref(false)
const submitting = ref(false)

// 表单
const formRef = ref(null)
const formData = reactive({
  departmentId: '',
  departmentName: '',
  location: ''
})

const formRules = {
  departmentId: [
    { required: true, message: '请输入科室编号', trigger: 'blur' }
  ],
  departmentName: [
    { required: true, message: '请输入科室名称', trigger: 'blur' }
  ],
  location: [
    { required: true, message: '请输入物理位置', trigger: 'blur' }
  ]
}

// 病床管理
const bedDialogVisible = ref(false)
const currentDepartment = reactive({
  departmentId: '',
  departmentName: ''
})
const bedTableData = ref([])
const bedLoading = ref(false)
const wards = ref([])
const selectedWardId = ref('')

// 病区管理
const wardDialogVisible = ref(false)
const wardFormRef = ref(null)
const wardSubmitting = ref(false)
const wardFormData = reactive({
  wardId: ''
})

const wardFormRules = {
  wardId: [
    { required: true, message: '请输入病区编号', trigger: 'blur' },
    { pattern: /^.+-W\d+$/, message: '格式：科室代码-W数字（如IM-W03或56-W02）', trigger: 'blur' }
  ]
}

// 加载科室列表
const loadDepartments = async () => {
  loading.value = true
  try {
    const params = {
      keyword: searchForm.keyword || undefined,
      pageIndex: pagination.pageIndex,
      pageSize: pagination.pageSize
    }
    const response = await getDepartmentList(params)
    tableData.value = response.items
    pagination.total = response.totalCount
  } catch (error) {
    ElMessage.error('加载科室列表失败')
    console.error(error)
  } finally {
    loading.value = false
  }
}

// 重置搜索
const handleReset = () => {
  searchForm.keyword = ''
  pagination.pageIndex = 1
  loadDepartments()
}

// 新增
const handleAdd = () => {
  isEdit.value = false
  resetForm()
  dialogVisible.value = true
}

// 编辑
const handleEdit = (row) => {
  isEdit.value = true
  Object.assign(formData, {
    departmentId: row.departmentId,
    departmentName: row.departmentName,
    location: row.location
  })
  dialogVisible.value = true
}

// 删除
const handleDelete = async (row) => {
  try {
    await ElMessageBox.confirm(
      `确定要删除科室 "${row.departmentName}" 吗？`,
      '确认删除',
      {
        confirmButtonText: '确定',
        cancelButtonText: '取消',
        type: 'warning'
      }
    )

    await deleteDepartment(row.departmentId)
    ElMessage.success('删除成功')
    loadDepartments()
  } catch (error) {
    if (error !== 'cancel') {
      ElMessage.error(error.response?.data?.message || '删除失败')
      console.error(error)
    }
  }
}

// 提交表单
const handleSubmit = async () => {
  if (!formRef.value) return

  await formRef.value.validate(async (valid) => {
    if (!valid) return

    submitting.value = true
    try {
      if (isEdit.value) {
        await updateDepartment(formData.departmentId, formData)
        ElMessage.success('更新成功')
      } else {
        await createDepartment(formData)
        ElMessage.success('创建成功')
      }
      dialogVisible.value = false
      loadDepartments()
    } catch (error) {
      ElMessage.error(error.response?.data?.message || '操作失败')
      console.error(error)
    } finally {
      submitting.value = false
    }
  })
}

// 关闭对话框
const handleDialogClose = () => {
  resetForm()
}

// 重置表单
const resetForm = () => {
  formData.departmentId = ''
  formData.departmentName = ''
  formData.location = ''
  formRef.value?.clearValidate()
}

// 查看病床
const handleViewBeds = async (row) => {
  currentDepartment.departmentId = row.departmentId
  currentDepartment.departmentName = row.departmentName
  bedDialogVisible.value = true
  
  // 加载该科室的病区
  try {
    const response = await api.get(`/Ward/department/${row.departmentId}`)
    wards.value = response
    if (wards.value.length > 0) {
      selectedWardId.value = wards.value[0].id
      await loadBeds()
    }
  } catch (error) {
    ElMessage.error('加载病区列表失败')
    console.error(error)
  }
}

// 加载病床列表
const loadBeds = async () => {
  if (!selectedWardId.value) return
  
  bedLoading.value = true
  try {
    const response = await bedApi.getBedsByDepartmentId(currentDepartment.departmentId)
    // 只显示选中病区的病床
    bedTableData.value = response.filter(bed => bed.wardId === selectedWardId.value)
  } catch (error) {
    ElMessage.error('加载病床列表失败')
    console.error(error)
  } finally {
    bedLoading.value = false
  }
}

// 新增病床
const handleAddBed = async () => {
  try {
    await ElMessageBox.confirm(
      `确定要在病区 "${selectedWardId.value}" 中新增病床吗？`,
      '确认新增',
      {
        confirmButtonText: '确定',
        cancelButtonText: '取消',
        type: 'info'
      }
    )

    await bedApi.createBed({
      departmentId: currentDepartment.departmentId,
      wardId: selectedWardId.value
    })
    
    ElMessage.success('病床创建成功')
    await loadBeds()
    await loadDepartments() // 刷新科室列表以更新病床数量
  } catch (error) {
    if (error !== 'cancel') {
      ElMessage.error(error.response?.data?.message || '创建病床失败')
      console.error(error)
    }
  }
}

// 新增病区
const handleAddWard = () => {
  wardFormData.wardId = ''
  wardDialogVisible.value = true
}

// 提交新增病区
const handleSubmitWard = async () => {
  if (!wardFormRef.value) return

  await wardFormRef.value.validate(async (valid) => {
    if (!valid) return

    wardSubmitting.value = true
    try {
      await api.post('/Ward', {
        id: wardFormData.wardId,
        departmentId: currentDepartment.departmentId
      })
      
      ElMessage.success('病区创建成功')
      wardDialogVisible.value = false
      
      // 重新加载病区列表
      const response = await api.get(`/Ward/department/${currentDepartment.departmentId}`)
      wards.value = response
      selectedWardId.value = wardFormData.wardId
      await loadBeds()
    } catch (error) {
      ElMessage.error(error.response?.data?.message || '创建病区失败')
      console.error(error)
    } finally {
      wardSubmitting.value = false
    }
  })
}

// 删除病区
const handleDeleteWard = async () => {
  if (!selectedWardId.value) return

  try {
    await ElMessageBox.confirm(
      `确定要删除病区 "${selectedWardId.value}" 吗？（仅当病区下没有病床时可删除）`,
      '确认删除',
      {
        confirmButtonText: '确定',
        cancelButtonText: '取消',
        type: 'warning'
      }
    )

    await api.delete(`/Ward/${selectedWardId.value}`)
    ElMessage.success('病区删除成功')
    
    // 重新加载病区列表
    const response = await api.get(`/Ward/department/${currentDepartment.departmentId}`)
    wards.value = response
    
    // 如果删除的是当前选中的病区，则选中第一个病区
    if (wards.value.length > 0) {
      selectedWardId.value = wards.value[0].id
      await loadBeds()
    } else {
      selectedWardId.value = ''
      bedTableData.value = []
    }
    
    // 刷新科室列表以更新病床数量
    await loadDepartments()
  } catch (error) {
    if (error !== 'cancel') {
      ElMessage.error(error.response?.data?.message || '删除病区失败')
      console.error(error)
    }
  }
}

// 删除病床
const handleDeleteBed = async (row) => {
  try {
    await ElMessageBox.confirm(
      `确定要删除病床 "${row.bedId}" 吗？`,
      '确认删除',
      {
        confirmButtonText: '确定',
        cancelButtonText: '取消',
        type: 'warning'
      }
    )

    await bedApi.deleteBed(row.bedId)
    ElMessage.success('删除成功')
    await loadBeds()
    await loadDepartments() // 刷新科室列表以更新病床数量
  } catch (error) {
    if (error !== 'cancel') {
      ElMessage.error(error.response?.data?.message || '删除失败')
      console.error(error)
    }
  }
}

// 格式化日期时间
const formatDateTime = (dateString) => {
  if (!dateString) return '-'
  const date = new Date(dateString)
  return date.toLocaleString('zh-CN', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit'
  })
}

onMounted(() => {
  loadDepartments()
})
</script>

<style scoped>
.department-management {
  display: flex;
  flex-direction: column;
  height: 100vh;
  overflow: hidden;
  background: #f5f7fa;
}

/* ==================== 固定顶部导航栏 ==================== */
.layout-header {
  display: flex;
  align-items: center;
  height: 60px;
  padding: 0 24px;
  background: #ffffff;
  border-bottom: 1px solid #e4e7ed;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.06);
  flex-shrink: 0;
  z-index: 1000;
}

.header-logo {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-right: 48px;
}

.logo-text {
  font-size: 18px;
  font-weight: 600;
  color: #303133;
  white-space: nowrap;
}

/* ==================== 导航菜单 ==================== */
.header-nav {
  display: flex;
  gap: 8px;
  flex: 1;
}

.nav-item {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 8px 20px;
  border-radius: 6px;
  color: #606266;
  text-decoration: none;
  font-size: 14px;
  font-weight: 500;
  transition: all 0.3s;
  cursor: pointer;
}

.nav-item:hover {
  background: #f5f7fa;
  color: #f56c6c;
}

.nav-item.active {
  background: #fef0f0;
  color: #f56c6c;
  font-weight: 600;
}

/* ==================== 用户信息 ==================== */
.header-user {
  margin-left: auto;
}

.user-info {
  display: flex;
  align-items: center;
  gap: 8px;
  cursor: pointer;
  padding: 6px 12px;
  border-radius: 6px;
  transition: all 0.3s;
}

.user-info:hover {
  background: #f5f7fa;
}

.user-name {
  font-size: 14px;
  font-weight: 500;
  color: #303133;
}

.user-role {
  font-size: 12px;
  color: #909399;
}

/* ==================== 页面内容区域 ==================== */
.page-content {
  flex: 1;
  overflow-y: auto;
  padding: 20px;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.card-header h3 {
  margin: 0;
}

.search-form {
  margin-bottom: 20px;
}

.pagination {
  margin-top: 20px;
  display: flex;
  justify-content: flex-end;
}
</style>
