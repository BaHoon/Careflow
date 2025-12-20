<template>
  <div class="inspection-container">
    <!-- 筛选条件区域 -->
    <el-card class="filter-card" shadow="never">
      <div class="card-header-title" style="display:none">查询条件</div> <!-- 可选：如果需要标题可以放开 -->
      <el-form :inline="true" :model="filterForm" class="filter-form">
        <el-form-item label="开单日期">
          <el-date-picker
            v-model="filterForm.dateRange"
            type="daterange"
            range-separator="至"
            start-placeholder="开始日期"
            end-placeholder="结束日期"
            format="YYYY-MM-DD"
            value-format="YYYY-MM-DD"
            style="width: 240px"
          />
        </el-form-item>
        <el-form-item label="预约类型">
          <el-select v-model="filterForm.appointmentType" placeholder="全部" clearable style="width: 120px">
            <el-option label="全部" value="" />
            <el-option label="需要预约" value="required" />
            <el-option label="无需预约" value="not_required" />
          </el-select>
        </el-form-item>
        <el-form-item label="医技确认">
          <el-select v-model="filterForm.confirmStatus" placeholder="全部" clearable style="width: 120px">
            <el-option label="全部" value="" />
            <el-option label="未确认" value="pending" />
            <el-option label="已确认" value="confirmed" />
          </el-select>
        </el-form-item>
        <el-form-item label="检查状态">
          <el-select v-model="filterForm.inspectionStatus" placeholder="全部" clearable style="width: 120px">
            <el-option label="全部" value="" />
            <el-option label="待前往" value="Pending" />
            <el-option label="检查中" value="InProgress" />
            <el-option label="已回病房" value="BackToWard" />
            <el-option label="报告已出" value="ReportCompleted" />
          </el-select>
        </el-form-item>
        <el-form-item label="病区">
          <el-input v-model="filterForm.ward" placeholder="请输入病区" clearable style="width: 140px" />
        </el-form-item>
        <el-form-item label="患者姓名">
          <el-input v-model="filterForm.patientName" placeholder="请输入姓名" clearable style="width: 140px" />
        </el-form-item>
     <el-form-item class="search-actions">
  <el-button type="primary" @click="handleSearch">查询</el-button>
  <el-button @click="handleReset">重置</el-button>
</el-form-item>

      </el-form>
    </el-card>

    <!-- 数据展示区域 (包含工具栏和表格) -->
    <div class="data-section">
      <!-- 操作按钮区域 -->
      <div class="toolbar">
        <div class="toolbar-left">
          <span class="record-count">共 <span class="highlight">{{ total }}</span> 条记录</span>
        </div>
        <div class="toolbar-right">
          <el-button type="primary" plain :icon="Printer" @click="handleBatchPrint">列表打印</el-button>
          <el-button type="success" plain :icon="DocumentCopy" @click="handlePrintSelected">检查单打印</el-button>
        </div>
      </div>

      <!-- 数据表格 -->
      <el-card class="table-card" shadow="never" :body-style="{ padding: '0' }">
        <el-table
          v-loading="loading"
          :data="tableData"
          stripe
          border
          style="width: 100%"
          :header-cell-style="{ background: '#F5F7FA', color: '#606266', fontWeight: '600', height: '50px' }"
          @selection-change="handleSelectionChange"
        >
          <el-table-column type="selection" width="50" align="center" />
          
          <el-table-column prop="appointmentType" label="预约分类" width="100" align="center">
            <template #default="{ row }">
              <el-tag :type="row.requiresAppointment ? 'warning' : 'info'" effect="plain" size="small" round>
                {{ row.requiresAppointment ? '需预约' : '无需预约' }}
              </el-tag>
            </template>
          </el-table-column>
          <el-table-column prop="printStatus" label="打印状态" width="100" align="center">
            <template #default="{ row }">
              <el-tag :type="row.isPrinted ? 'success' : 'info'" effect="light" size="small">
                {{ row.isPrinted ? '已打印' : '未打印' }}
              </el-tag>
            </template>
          </el-table-column>
          <el-table-column prop="inspectionStatus" label="检查状态" width="110" align="center">
            <template #default="{ row }">
              <el-tag :type="getStatusType(row.inspectionStatus)" effect="dark" size="small">
                {{ getStatusText(row.inspectionStatus) }}
              </el-tag>
            </template>
          </el-table-column>
          <el-table-column prop="ward" label="病区" width="100" show-overflow-tooltip />
          <el-table-column prop="bedNumber" label="床号" width="80" align="center" />
          <el-table-column prop="patientId" label="住院号" width="120" />
          <el-table-column prop="patientName" label="姓名" width="100" />
          
          <el-table-column prop="gender" label="性别" width="60" align="center">
            <template #default="{ row }">
              {{ row.gender === 'Male' ? '男' : '女' }}
            </template>
          </el-table-column>
          <el-table-column prop="age" label="年龄" width="70" align="center" />
          <el-table-column prop="itemCode" label="检查项目" min-width="160" show-overflow-tooltip />
          
          <el-table-column prop="appointmentTime" label="预约检查时间" width="170">
            <template #default="{ row }">
              <span :class="{ 'text-secondary': !row.appointmentTime }">
                {{ row.appointmentTime ? formatDateTime(row.appointmentTime) : '无需预约' }}
              </span>
            </template>
          </el-table-column>
          <el-table-column prop="location" label="检查地址" width="120" show-overflow-tooltip />
          
          <el-table-column prop="createTime" label="开单时间" width="170">
            <template #default="{ row }">
              {{ formatDateTime(row.createTime) }}
            </template>
          </el-table-column>
          <el-table-column label="操作" width="220" fixed="right" align="center">
            <template #default="{ row }">
              <el-button link type="primary" size="small" @click="handleViewDetail(row)">
                详情
              </el-button>
              <el-divider direction="vertical" />
              <el-button link type="primary" size="small" @click="handlePrintSingle(row)">
                打印
              </el-button>
              <el-divider direction="vertical" v-if="!row.appointmentTime && row.inspectionStatus === 'Pending'" />
              <el-button 
                v-if="!row.appointmentTime && row.inspectionStatus === 'Pending'" 
                link 
                type="warning" 
                size="small" 
                @click="handleCreateAppointment(row)"
              >
                预约
              </el-button>
            </template>
          </el-table-column>
        </el-table>

        <!-- 分页 -->
        <div class="pagination-container">
          <el-pagination
            v-model:current-page="pagination.currentPage"
            v-model:page-size="pagination.pageSize"
            :page-sizes="[10, 20, 50, 100]"
            :total="total"
            layout="total, sizes, prev, pager, next, jumper"
            background
            @size-change="handleSizeChange"
            @current-change="handleCurrentChange"
          />
        </div>
      </el-card>
    </div>

    <!-- 创建预约对话框 -->
    <el-dialog v-model="appointmentDialogVisible" title="创建预约" width="500px" destroy-on-close align-center>
      <el-form :model="appointmentForm" label-width="100px" style="padding: 20px 20px 0 0">
        <el-form-item label="预约时间">
          <el-date-picker
            v-model="appointmentForm.appointmentTime"
            type="datetime"
            placeholder="选择日期时间"
            format="YYYY-MM-DD HH:mm"
            value-format="YYYY-MM-DD HH:mm:ss"
            style="width: 100%"
          />
        </el-form-item>
        <el-form-item label="预约地点">
          <el-input v-model="appointmentForm.appointmentPlace" placeholder="请输入预约地点" />
        </el-form-item>
        <el-form-item label="注意事项">
          <el-input
            v-model="appointmentForm.precautions"
            type="textarea"
            :rows="3"
            placeholder="请输入注意事项（如：检查前禁食8小时）"
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <span class="dialog-footer">
          <el-button @click="appointmentDialogVisible = false">取消</el-button>
          <el-button type="primary" @click="submitAppointment">确定</el-button>
        </span>
      </template>
    </el-dialog>

    <!-- 详情对话框 -->
    <el-dialog v-model="detailDialogVisible" title="检查医嘱详情" width="800px" align-center>
      <el-descriptions :column="2" border class="detail-descriptions">
        <el-descriptions-item label="医嘱ID">{{ currentOrder.id }}</el-descriptions-item>
        <el-descriptions-item label="患者姓名">{{ currentOrder.patientName }}</el-descriptions-item>
        <el-descriptions-item label="住院号">{{ currentOrder.patientId }}</el-descriptions-item>
        <el-descriptions-item label="床号">{{ currentOrder.bedNumber }}</el-descriptions-item>
        <el-descriptions-item label="检查项目">{{ currentOrder.itemCode }}</el-descriptions-item>
        <el-descriptions-item label="RIS/LIS号">{{ currentOrder.risLisId }}</el-descriptions-item>
        <el-descriptions-item label="检查科室">{{ currentOrder.location }}</el-descriptions-item>
        <el-descriptions-item label="检查来源">
          {{ currentOrder.source === 'RIS' ? '影像检查' : '检验检查' }}
        </el-descriptions-item>
        <el-descriptions-item label="预约时间">
          {{ currentOrder.appointmentTime ? formatDateTime(currentOrder.appointmentTime) : '无需预约' }}
        </el-descriptions-item>
        <el-descriptions-item label="预约地点">{{ currentOrder.appointmentPlace || '-' }}</el-descriptions-item>
        <el-descriptions-item label="注意事项" :span="2">
          {{ currentOrder.precautions || '无' }}
        </el-descriptions-item>
        <el-descriptions-item label="检查状态">
          <el-tag :type="getStatusType(currentOrder.inspectionStatus)" size="small">
            {{ getStatusText(currentOrder.inspectionStatus) }}
          </el-tag>
        </el-descriptions-item>
        <el-descriptions-item label="开单时间">
          {{ formatDateTime(currentOrder.createTime) }}
        </el-descriptions-item>
        <el-descriptions-item label="检查开始时间">
          {{ currentOrder.checkStartTime ? formatDateTime(currentOrder.checkStartTime) : '-' }}
        </el-descriptions-item>
        <el-descriptions-item label="检查结束时间">
          {{ currentOrder.checkEndTime ? formatDateTime(currentOrder.checkEndTime) : '-' }}
        </el-descriptions-item>
        <el-descriptions-item label="报告完成时间">
          {{ currentOrder.reportTime ? formatDateTime(currentOrder.reportTime) : '-' }}
        </el-descriptions-item>
      </el-descriptions>

      <!-- 执行任务列表 -->
      <el-divider content-position="left">执行任务</el-divider>
      <el-table v-if="currentOrderTasks.length > 0" :data="currentOrderTasks" border style="margin-bottom: 20px">
        <el-table-column prop="id" label="任务ID" width="80" />
        <el-table-column label="任务类型" width="150">
          <template #default="{ row }">
            {{ getTaskTypeText(row) }}
          </template>
        </el-table-column>
        <el-table-column prop="status" label="状态" width="100" align="center">
          <template #default="{ row }">
            <el-tag :type="row.status === 'Completed' ? 'success' : 'info'" size="small">
              {{ row.status === 'Completed' ? '已完成' : '待执行' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="计划执行时间" width="170">
          <template #default="{ row }">
            {{ formatDateTime(row.plannedStartTime) }}
          </template>
        </el-table-column>
        <el-table-column label="条形码" width="150" align="center">
          <template #default="{ row }">
            <img 
              :src="getBarcodeUrl(row.id)" 
              alt="任务条形码" 
              style="max-width: 120px; height: 40px; cursor: pointer"
              @click="viewBarcodeFullScreen(row.id)"
            />
          </template>
        </el-table-column>
      </el-table>
      <el-empty v-else description="暂无执行任务" :image-size="80" />

      <template #footer>
        <el-button type="primary" @click="detailDialogVisible = false">关闭</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Printer, DocumentCopy, Search, Refresh } from '@element-plus/icons-vue' // 引入图标
import api from '@/utils/api'

// 筛选条件
const filterForm = reactive({
  dateRange: [],
  appointmentType: '',
  confirmStatus: '',
  inspectionStatus: '',
  ward: '',
  patientName: ''
})

// 分页信息
const pagination = reactive({
  currentPage: 1,
  pageSize: 20
})

// 表格数据
const tableData = ref([])
const total = ref(0)
const loading = ref(false)
const selectedRows = ref([])

// 预约对话框
const appointmentDialogVisible = ref(false)
const appointmentForm = reactive({
  orderId: null,
  appointmentTime: '',
  appointmentPlace: '',
  precautions: ''
})

// 详情对话框
const detailDialogVisible = ref(false)
const currentOrder = ref({})
const currentOrderTasks = ref([])

// 查询数据
const fetchData = async () => {
  loading.value = true
  try {
    // 构建查询参数
    const params = {
      pageIndex: pagination.currentPage,
      pageSize: pagination.pageSize,
      ...filterForm
    }
    // 调用后端 API（根据你的实际接口调整）
    const response = await api.get('/inspection/list', { params })
    
    tableData.value = response.data || []
    total.value = response.total || 0
  } catch (error) {
    ElMessage.error('获取数据失败')
  } finally {
    loading.value = false
  }
}

// 查询按钮
const handleSearch = () => {
  pagination.currentPage = 1
  fetchData()
}

// 重置按钮
const handleReset = () => {
  Object.assign(filterForm, {
    dateRange: [],
    appointmentType: '',
    confirmStatus: '',
    inspectionStatus: '',
    ward: '',
    patientName: ''
  })
  handleSearch()
}

// 分页改变
const handleSizeChange = (val) => {
  pagination.pageSize = val
  fetchData()
}

const handleCurrentChange = (val) => {
  pagination.currentPage = val
  fetchData()
}

// 表格选择
const handleSelectionChange = (val) => {
  selectedRows.value = val
}

// 查看详情
const handleViewDetail = async (row) => {
  try {
    const response = await api.get(`/inspection/detail/${row.id}`)
    currentOrder.value = response
    
    // 获取关联的执行任务
    try {
      const tasksResponse = await api.get(`/inspection/${row.id}/tasks`)
      currentOrderTasks.value = tasksResponse.data || []
    } catch (taskError) {
      console.warn('获取任务列表失败', taskError)
      currentOrderTasks.value = []
    }
    
    detailDialogVisible.value = true
  } catch (error) {
    ElMessage.error('获取详情失败')
  }
}

// 创建预约
const handleCreateAppointment = (row) => {
  appointmentForm.orderId = row.id
  appointmentForm.appointmentTime = ''
  appointmentForm.appointmentPlace = row.location
  appointmentForm.precautions = ''
  appointmentDialogVisible.value = true
}

// 提交预约
const submitAppointment = async () => {
  try {
    await api.post('/inspection/create-appointment', appointmentForm)
    ElMessage.success('预约创建成功')
    appointmentDialogVisible.value = false
    fetchData()
  } catch (error) {
    ElMessage.error('预约创建失败')
  }
}

// 打印单条检查单
const handlePrintSingle = (row) => {
  window.open(`/print/inspection/${row.id}`, '_blank')
  ElMessage.success('正在打开打印页面...')
}

// 批量打印检查单
const handlePrintSelected = () => {
  if (selectedRows.value.length === 0) {
    ElMessage.warning('请先选择要打印的检查单')
    return
  }
  const ids = selectedRows.value.map(row => row.id).join(',')
  window.open(`/print/inspection-batch?ids=${ids}`, '_blank')
  ElMessage.success('正在打开打印页面...')
}

// 列表打印
const handleBatchPrint = () => {
  window.print()
}

// 状态显示
const getStatusText = (status) => {
  const statusMap = {
    'Pending': '待前往',
    'InProgress': '检查中',
    'BackToWard': '已回病房',
    'ReportCompleted': '报告已出',
    'Cancelled': '已取消'
  }
  return statusMap[status] || status
}

const getStatusType = (status) => {
  const typeMap = {
    'Pending': 'info',
    'InProgress': 'warning',
    'BackToWard': 'success',
    'ReportCompleted': 'success',
    'Cancelled': 'danger'
  }
  return typeMap[status] || 'info'
}

// 时间格式化
const formatDateTime = (dateTime) => {
  if (!dateTime) return '-'
  const date = new Date(dateTime)
  return date.toLocaleString('zh-CN', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit'
  })
}

// 获取条形码图片URL
const getBarcodeUrl = (taskId) => {
  return `/api/Inspection/task/${taskId}/barcode`
}

// 全屏查看条形码
const viewBarcodeFullScreen = (taskId) => {
  window.open(getBarcodeUrl(taskId), '_blank')
}

// 获取任务类型文本
const getTaskTypeText = (task) => {
  try {
    const payload = JSON.parse(task.dataPayload || '{}')
    return payload.Title || payload.TaskType || '未知任务'
  } catch {
    return '未知任务'
  }
}

onMounted(() => {
  fetchData()
})
</script>

<style scoped>
/* 容器背景色优化，营造后台管理系统感 */
.inspection-container {
  padding: 20px;
  background-color: #f0f2f5;
  min-height: 100vh;
}

/* 筛选卡片美化 */
.filter-card {
  margin-bottom: 16px;
  border-radius: 8px;
  border: none;
}

.filter-form {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
}

/* 调整form-item间距，使其更紧凑整齐 */
.filter-form .el-form-item {
  margin-bottom: 10px;
  margin-right: 0;
}

/* 将按钮组推到右侧或保持在流中，视需求而定，这里保持紧凑 */
.search-actions {
  margin-left: auto;
}

/* 工具栏样式优化 */
.toolbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 16px;
  padding: 0 4px;
}

.record-count {
  font-size: 14px;
  color: #606266;
}

.record-count .highlight {
  color: #409EFF;
  font-weight: bold;
  margin: 0 4px;
}

.toolbar-right {
  display: flex;
  gap: 12px;
}

/* 表格卡片美化 */
.table-card {
  border-radius: 8px;
  border: none;
  overflow: hidden; /* 配合border-radius */
}

/* 文本颜色微调 */
.text-secondary {
  color: #909399;
}

/* 分页居右并增加留白 */
.pagination-container {
  padding: 20px;
  display: flex;
  justify-content: flex-end;
  background: #fff;
}

/* 打印样式保持不变 */
@media print {
  .inspection-container {
    background-color: white;
    padding: 0;
  }
  .filter-card,
  .toolbar,
  .pagination-container,
  .el-table__column--selection,
  .el-table__fixed-right {
    display: none !important;
  }
}
</style>
