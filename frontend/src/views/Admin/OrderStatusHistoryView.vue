<template>
  <div class="admin-history-view">
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
          active-class="active"
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
    
    <!-- 筛选面板 -->
    <el-card class="filter-card" shadow="never">
      <div class="filter-row">
        <!-- 患者ID筛选 -->
        <div class="filter-item">
          <label>患者ID</label>
          <el-input 
            v-model="filters.patientId" 
            placeholder="输入患者ID" 
            clearable
            size="small"
          />
        </div>

        <!-- 患者姓名筛选 -->
        <div class="filter-item">
          <label>患者姓名</label>
          <el-input 
            v-model="filters.patientName" 
            placeholder="输入患者姓名" 
            clearable
            size="small"
          />
        </div>

        <!-- 操作人筛选 -->
        <div class="filter-item">
          <label>操作人ID</label>
          <el-input 
            v-model="filters.changedById" 
            placeholder="输入医护人员ID" 
            clearable
            size="small"
          />
        </div>

        <!-- 操作人类型 -->
        <div class="filter-item">
          <label>操作人类型</label>
          <el-select 
            v-model="filters.changedByType" 
            placeholder="选择类型" 
            clearable
            size="small"
          >
            <el-option label="医生" value="Doctor" />
            <el-option label="护士" value="Nurse" />
            <el-option label="系统" value="System" />
          </el-select>
        </div>

        <!-- 医嘱类型 -->
        <div class="filter-item">
          <label>医嘱类型</label>
          <el-select 
            v-model="filters.orderType" 
            placeholder="选择类型" 
            clearable
            size="small"
          >
            <el-option label="药品医嘱" value="MedicationOrder" />
            <el-option label="检查医嘱" value="InspectionOrder" />
            <el-option label="操作医嘱" value="OperationOrder" />
            <el-option label="手术医嘱" value="SurgicalOrder" />
            <el-option label="出院医嘱" value="DischargeOrder" />
          </el-select>
        </div>
      </div>

      <div class="filter-row">
        <!-- 时间范围 -->
        <div class="filter-item time-range">
          <label>变更时间</label>
          <el-date-picker
            v-model="timeRange"
            type="datetimerange"
            range-separator="至"
            start-placeholder="开始时间"
            end-placeholder="结束时间"
            value-format="YYYY-MM-DDTHH:mm:ss"
            size="small"
          />
        </div>

        <!-- 操作按钮 -->
        <div class="filter-actions">
          <el-button type="primary" @click="handleSearch" size="small" :loading="loading">
            🔍 查询
          </el-button>
          <el-button @click="handleReset" size="small">
            🔄 重置
          </el-button>
        </div>
      </div>
    </el-card>

    <!-- 统计信息 -->
    <div class="stats-bar" v-if="totalCount > 0">
      <span>共找到 <strong>{{ totalCount }}</strong> 条记录</span>
    </div>

    <!-- 历史记录表格 -->
    <el-card class="table-card" shadow="never">
      <el-table 
        :data="histories" 
        v-loading="loading"
        stripe
        border
        height="calc(100vh - 400px)"
      >
        <el-table-column prop="id" label="记录ID" width="80" />
        <el-table-column prop="changedAt" label="变更时间" width="160">
          <template #default="{ row }">
            {{ formatDateTime(row.changedAt) }}
          </template>
        </el-table-column>
        <el-table-column prop="patientId" label="患者ID" width="100" />
        <el-table-column prop="patientName" label="患者姓名" width="100" />
        <el-table-column prop="bedId" label="床位" width="80" />
        <el-table-column prop="orderType" label="医嘱类型" width="110">
          <template #default="{ row }">
            <el-tag :type="getOrderTypeColor(row.orderType)" size="small">
              {{ getOrderTypeName(row.orderType) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="状态变更" width="200">
          <template #default="{ row }">
            <div class="status-change">
              <el-tag :type="getStatusColor(row.fromStatus)" size="small">
                {{ row.fromStatusName }}
              </el-tag>
              <el-icon class="arrow"><Right /></el-icon>
              <el-tag :type="getStatusColor(row.toStatus)" size="small">
                {{ row.toStatusName }}
              </el-tag>
            </div>
          </template>
        </el-table-column>
        <el-table-column prop="changedByName" label="操作人" width="150">
          <template #default="{ row }">
            <div>{{ row.changedByName }}</div>
            <el-tag :type="getOperatorTypeColor(row.changedByType)" size="small">
              {{ row.changedByType }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="reason" label="变更原因" min-width="150" />
        <el-table-column prop="notes" label="备注" min-width="150" />
        <el-table-column label="操作" width="100" fixed="right">
          <template #default="{ row }">
            <el-button 
              type="primary" 
              link 
              size="small"
              @click="viewOrderDetail(row.medicalOrderId)"
            >
              查看医嘱
            </el-button>
          </template>
        </el-table-column>
      </el-table>

      <!-- 分页 -->
      <div class="pagination-container">
        <el-pagination
          v-model:current-page="currentPage"
          v-model:page-size="pageSize"
          :page-sizes="[20, 50, 100, 200]"
          :total="totalCount"
          layout="total, sizes, prev, pager, next, jumper"
          @size-change="handleSearch"
          @current-change="handleSearch"
        />
      </div>
    </el-card>

    <!-- ==================== 医嘱详情弹窗 ==================== -->
    <el-dialog
      v-model="detailDialogVisible"
      :title="`医嘱详情 - ID: ${currentOrderId || ''}`"
      width="900px"
      class="order-detail-dialog"
      :close-on-click-modal="false"
    >
      <div class="order-detail-dialog-body" v-loading="loadingDetail">
        <OrderDetailPanel 
          v-if="currentOrderDetail"
          :detail="currentOrderDetail"
          :nurse-mode="false"
        />
      </div>
    </el-dialog>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted, computed } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { Right, Setting, DocumentCopy, User, ArrowDown, SwitchButton, OfficeBuilding, List } from '@element-plus/icons-vue';
import { useRouter } from 'vue-router';
import { queryOrderStatusHistory } from '@/api/admin';
import { logLogout } from '@/api/systemLog';
import { getOrderDetail } from '@/api/nurseOrder';
import OrderDetailPanel from '@/components/OrderDetailPanel.vue';

const router = useRouter();

const userName = computed(() => {
  const stored = localStorage.getItem('userInfo') || localStorage.getItem('user')
  if (!stored) return '管理'
  try {
    const user = JSON.parse(stored)
    const name = user.fullName || user.name || '管理员'
    return name.substring(0, 2)
  } catch {
    return '管理'
  }
});

const fullName = computed(() => {
  const stored = localStorage.getItem('userInfo') || localStorage.getItem('user')
  if (!stored) return '管理员'
  try {
    const user = JSON.parse(stored)
    return user.fullName || user.name || '管理员'
  } catch {
    return '管理员'
  }
});

// ==================== 数据状态 ====================
const loading = ref(false);
const histories = ref([]);
const totalCount = ref(0);
const currentPage = ref(1);
const pageSize = ref(50);
const timeRange = ref([]);

const filters = reactive({
  patientId: '',
  patientName: '',
  changedById: '',
  changedByType: '',
  orderType: '',
  startTime: null,
  endTime: null
});

// ==================== 医嘱详情弹窗 ====================
const detailDialogVisible = ref(false);
const currentOrderDetail = ref(null);
const currentOrderId = ref(null);
const loadingDetail = ref(false);

// ==================== 生命周期 ====================
onMounted(() => {
  handleSearch();
});

const handleLogout = async () => {
  try {
    await ElMessageBox.confirm(
      '确定要退出登录吗？',
      '提示',
      {
        confirmButtonText: '确定',
        cancelButtonText: '取消',
        type: 'warning'
      }
    );
    
    // 记录登出日志
    try {
      const user = JSON.parse(localStorage.getItem('userInfo') || '{}')
      await logLogout({
        operatorId: user.id || null,
        operatorName: user.fullName || user.name || '未知用户'
      })
    } catch (logError) {
      console.error('记录登出日志失败:', logError)
    }
    
    localStorage.removeItem('token');
    localStorage.removeItem('userInfo');
    ElMessage.success('已退出登录');
    router.push('/login');
  } catch (error) {
    // 用户取消
  }
};

// ==================== 查询操作 ====================
const handleSearch = async () => {
  loading.value = true;
  try {
    // 处理时间范围
    if (timeRange.value && timeRange.value.length === 2) {
      filters.startTime = timeRange.value[0];
      filters.endTime = timeRange.value[1];
    } else {
      filters.startTime = null;
      filters.endTime = null;
    }

    const response = await queryOrderStatusHistory({
      ...filters,
      pageNumber: currentPage.value,
      pageSize: pageSize.value
    });

    histories.value = response.histories || [];
    totalCount.value = response.totalCount || 0;
  } catch (error) {
    console.error('查询失败:', error);
    ElMessage.error('查询失败，请稍后重试');
  } finally {
    loading.value = false;
  }
};

const handleReset = () => {
  filters.patientId = '';
  filters.patientName = '';
  filters.changedById = '';
  filters.changedByType = '';
  filters.orderType = '';
  filters.startTime = null;
  filters.endTime = null;
  timeRange.value = [];
  currentPage.value = 1;
  handleSearch();
};

// ==================== 格式化方法 ====================
const formatDateTime = (dateString) => {
  if (!dateString) return '-';
  const date = new Date(dateString);
  return date.toLocaleString('zh-CN', { 
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
    timeZone: 'Asia/Shanghai'
  });
};

const getOrderTypeName = (type) => {
  const map = {
    'MedicationOrder': '药品',
    'InspectionOrder': '检查',
    'OperationOrder': '操作',
    'SurgicalOrder': '手术',
    'DischargeOrder': '出院'
  };
  return map[type] || type;
};

const getOrderTypeColor = (type) => {
  const map = {
    'MedicationOrder': 'primary',
    'InspectionOrder': 'success',
    'OperationOrder': 'warning',
    'SurgicalOrder': 'danger',
    'DischargeOrder': 'info'
  };
  return map[type] || '';
};

const getStatusColor = (status) => {
  const map = {
    0: 'info',      // 草稿
    1: 'warning',   // 未签收
    2: 'primary',   // 已签收
    3: 'success',   // 进行中
    4: 'success',   // 已完成
    5: 'danger',    // 已拒绝
    6: 'info',      // 已取消
    7: 'warning',   // 等待停嘱
    8: 'info'       // 已停止
  };
  return map[status] || 'info';
};

const getOperatorTypeColor = (type) => {
  const map = {
    'Doctor': 'primary',
    'Nurse': 'success',
    'System': 'info'
  };
  return map[type] || 'info';
};

/**
 * 查看医嘱详情
 */
const viewOrderDetail = async (orderId) => {
  try {
    console.log('📖 查看医嘱详情:', orderId);
    currentOrderId.value = orderId;
    detailDialogVisible.value = true;
    loadingDetail.value = true;
    
    // 获取完整的医嘱详情（包含任务列表）
    const detail = await getOrderDetail(orderId);
    currentOrderDetail.value = detail;
    
    console.log('✅ 医嘱详情加载成功');
  } catch (error) {
    console.error('❌ 获取医嘱详情失败:', error);
    ElMessage.error('获取医嘱详情失败');
    detailDialogVisible.value = false;
  } finally {
    loadingDetail.value = false;
  }
};
</script>

<style scoped>
.admin-history-view {
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

.page-header {
  margin-bottom: 20px;
}

.page-header h2 {
  margin: 0 0 8px 0;
  color: #303133;
  font-size: 24px;
}

.subtitle {
  margin: 0;
  color: #909399;
  font-size: 14px;
}

.filter-card {
  margin-bottom: 20px;
}

.filter-row {
  display: flex;
  gap: 15px;
  margin-bottom: 15px;
  flex-wrap: wrap;
}

.filter-row:last-child {
  margin-bottom: 0;
}

.filter-item {
  display: flex;
  flex-direction: column;
  gap: 5px;
  min-width: 180px;
}

.filter-item.time-range {
  min-width: 360px;
}

.filter-item label {
  font-size: 14px;
  color: #606266;
  font-weight: 500;
}

.filter-actions {
  display: flex;
  gap: 10px;
  align-items: flex-end;
}

.stats-bar {
  padding: 12px 20px;
  background: white;
  border-radius: 4px;
  margin-bottom: 20px;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

.stats-bar strong {
  color: #409eff;
  font-size: 18px;
}

.table-card {
  margin-bottom: 20px;
}

.status-change {

/* 医嘱详情弹窗样式 */
.order-detail-dialog-body {
  min-height: 300px;
}

:deep(.order-detail-dialog) {
  border-radius: 8px;
}

:deep(.order-detail-dialog .el-dialog__header) {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  padding: 20px;
  border-radius: 8px 8px 0 0;
}

:deep(.order-detail-dialog .el-dialog__title) {
  color: white;
  font-size: 18px;
  font-weight: 600;
}

:deep(.order-detail-dialog .el-dialog__headerbtn .el-dialog__close) {
  color: white;
  font-size: 20px;
}

:deep(.order-detail-dialog .el-dialog__body) {
  padding: 0;
  max-height: 70vh;
  overflow-y: auto;
}
  display: flex;
  align-items: center;
  gap: 8px;
}

.status-change .arrow {
  color: #909399;
}

.sub-text {
  font-size: 12px;
  color: #909399;
  margin-top: 2px;
}

.pagination-container {
  display: flex;
  justify-content: flex-end;
  padding: 20px 0 10px 0;
}
</style>
