<template>
  <div class="system-log-audit">
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
        >
          <el-icon><OfficeBuilding /></el-icon>
          <span>科室管理</span>
        </router-link>
        <router-link 
          to="/admin/system-log" 
          class="nav-item"
          active-class="active"
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
        <h3>系统操作日志审计</h3>
      </template>

      <!-- 筛选栏 -->
      <el-form :inline="true" :model="searchForm" class="search-form">
        <el-form-item label="操作类型">
          <el-select v-model="searchForm.operationType" placeholder="全部" clearable style="width: 180px">
            <el-option
              v-for="type in operationTypes"
              :key="type.value"
              :label="type.label"
              :value="type.value"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="操作人姓名">
          <el-input
            v-model="searchForm.operatorName"
            placeholder="输入姓名搜索"
            clearable
            style="width: 150px"
          />
        </el-form-item>
        <el-form-item label="操作时间">
          <el-date-picker
            v-model="searchForm.timeRange"
            type="datetimerange"
            range-separator="至"
            start-placeholder="开始时间"
            end-placeholder="结束时间"
            value-format="YYYY-MM-DD HH:mm:ss"
            style="width: 360px"
          />
        </el-form-item>
        <el-form-item label="操作结果">
          <el-select v-model="searchForm.result" placeholder="全部" clearable style="width: 120px">
            <el-option label="成功" value="Success" />
            <el-option label="失败" value="Failed" />
          </el-select>
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="loadLogs">查询</el-button>
          <el-button @click="handleReset">重置</el-button>
        </el-form-item>
      </el-form>

      <!-- 统计信息 -->
      <div class="stats-bar">
        <el-statistic title="总记录数" :value="pagination.total" />
      </div>

      <!-- 表格 -->
      <el-table
        :data="tableData"
        v-loading="loading"
        border
        stripe
        height="calc(100vh - 400px)"
      >
        <el-table-column prop="logId" label="日志ID" width="80" />
        <el-table-column prop="operationType" label="操作类型" width="150">
          <template #default="{ row }">
            <el-tag :type="getOperationTypeColor(row.operationType)">
              {{ getOperationTypeLabel(row.operationType) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="operatorName" label="操作人" width="120" />
        <el-table-column prop="operationTime" label="操作时间" width="160">
          <template #default="{ row }">
            {{ formatDateTime(row.operationTime) }}
          </template>
        </el-table-column>
        <el-table-column prop="operationDetails" label="操作详情" min-width="250" show-overflow-tooltip />
        <el-table-column prop="ipAddress" label="IP地址" width="140" />
        <el-table-column prop="result" label="操作结果" width="90" align="center">
          <template #default="{ row }">
            <el-tag :type="row.result === 'Success' ? 'success' : 'danger'">
              {{ row.result === 'Success' ? '成功' : '失败' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="errorMessage" label="错误信息" width="200" show-overflow-tooltip />
        <el-table-column label="操作" width="100" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" @click="handleViewDetail(row)">查看详情</el-button>
          </template>
        </el-table-column>
      </el-table>

      <!-- 分页 -->
      <el-pagination
        v-model:current-page="pagination.pageIndex"
        v-model:page-size="pagination.pageSize"
        :total="pagination.total"
        :page-sizes="[20, 50, 100, 200]"
        layout="total, sizes, prev, pager, next, jumper"
        @size-change="loadLogs"
        @current-change="loadLogs"
        class="pagination"
      />
    </el-card>

    <!-- 详情对话框 -->
    <el-dialog
      v-model="detailDialogVisible"
      title="日志详情"
      width="700px"
    >
      <el-descriptions :column="2" border v-if="currentLog">
        <el-descriptions-item label="日志ID">{{ currentLog.logId }}</el-descriptions-item>
        <el-descriptions-item label="操作类型">
          <el-tag :type="getOperationTypeColor(currentLog.operationType)">
            {{ getOperationTypeLabel(currentLog.operationType) }}
          </el-tag>
        </el-descriptions-item>

        <el-descriptions-item label="操作时间" :span="2">
          {{ formatDateTime(currentLog.operationTime) }}
        </el-descriptions-item>
        <el-descriptions-item label="IP地址" :span="2">
          {{ currentLog.ipAddress || '-' }}
        </el-descriptions-item>
        <el-descriptions-item label="操作结果">
          <el-tag :type="currentLog.result === 'Success' ? 'success' : 'danger'">
            {{ currentLog.result === 'Success' ? '成功' : '失败' }}
          </el-tag>
        </el-descriptions-item>
        <el-descriptions-item label="关联实体">
          {{ currentLog.entityType || '-' }} #{{ currentLog.entityId || '-' }}
        </el-descriptions-item>        <el-descriptions-item label="操作人ID">{{ currentLog.operatorId || '-' }}</el-descriptions-item>
        <el-descriptions-item label="操作人姓名">{{ currentLog.operatorName || '-' }}</el-descriptions-item>        <el-descriptions-item label="操作详情" :span="2">
          {{ currentLog.operationDetails || '-' }}
        </el-descriptions-item>
        <el-descriptions-item label="错误信息" :span="2" v-if="currentLog.errorMessage">
          <el-text type="danger">{{ currentLog.errorMessage }}</el-text>
        </el-descriptions-item>
      </el-descriptions>
    </el-dialog>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { DocumentCopy, User, OfficeBuilding, List, Setting, ArrowDown, SwitchButton } from '@element-plus/icons-vue'
import { useRouter } from 'vue-router'
import { getSystemLogs, getOperationTypes } from '@/api/systemLog'

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
  operationType: '',
  operatorName: '',
  timeRange: [],
  result: ''
})

// 分页
const pagination = reactive({
  pageIndex: 1,
  pageSize: 50,
  total: 0
})

// 表格数据
const tableData = ref([])
const loading = ref(false)

// 操作类型选项
const operationTypes = ref([])

// 详情对话框
const detailDialogVisible = ref(false)
const currentLog = ref(null)

// 加载操作类型列表
const loadOperationTypes = async () => {
  try {
    const response = await getOperationTypes()
    operationTypes.value = response
  } catch (error) {
    console.error('加载操作类型失败', error)
  }
}

// 加载日志列表
const loadLogs = async () => {
  loading.value = true
  try {
    const params = {
      operationType: searchForm.operationType || undefined,
      operatorName: searchForm.operatorName || undefined,
      startTime: searchForm.timeRange && searchForm.timeRange[0] ? searchForm.timeRange[0] : undefined,
      endTime: searchForm.timeRange && searchForm.timeRange[1] ? searchForm.timeRange[1] : undefined,
      result: searchForm.result || undefined,
      pageIndex: pagination.pageIndex,
      pageSize: pagination.pageSize
    }
    const response = await getSystemLogs(params)
    tableData.value = response.items
    pagination.total = response.totalCount
  } catch (error) {
    ElMessage.error('加载日志列表失败')
    console.error(error)
  } finally {
    loading.value = false
  }
}

// 重置搜索
const handleReset = () => {
  searchForm.operationType = ''
  searchForm.operatorName = ''
  searchForm.timeRange = []
  searchForm.result = ''
  pagination.pageIndex = 1
  loadLogs()
}

// 查看详情
const handleViewDetail = (row) => {
  currentLog.value = row
  detailDialogVisible.value = true
}

// 获取操作类型标签
const getOperationTypeLabel = (type) => {
  const found = operationTypes.value.find(t => t.value === type)
  return found ? found.label : type
}

// 获取操作类型颜色
const getOperationTypeColor = (type) => {
  const colorMap = {
    'Login': 'success',
    'Logout': 'info',
    'OrderStop': 'warning',
    'DrugVerificationFailed': 'danger',
    'AccountCreated': 'primary',
    'AccountModified': 'warning',
    'AccountDeleted': 'danger'
  }
  return colorMap[type] || 'info'
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
    minute: '2-digit',
    second: '2-digit'
  })
}

onMounted(() => {
  loadOperationTypes()
  loadLogs()
})
</script>

<style scoped>
.system-log-audit {
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

.search-form {
  margin-bottom: 20px;
}

.stats-bar {
  margin-bottom: 20px;
  padding: 15px;
  background-color: #f5f7fa;
  border-radius: 4px;
}

.pagination {
  margin-top: 20px;
  display: flex;
  justify-content: flex-end;
}
</style>
