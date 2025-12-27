<template>
  <div class="patient-records-view">
    <!-- ============================== 
      【患者综合记录界面】
      左侧：患者列表（复用PatientListPanel）
      右侧：综合记录展示区（医嘱+护理记录+检查报告）
    ============================== -->

    <!-- 左侧患者列表面板 -->
    <PatientListPanel 
      :patient-list="patientList"
      :selected-patients="selectedPatient ? [selectedPatient] : []"
      :my-ward-id="currentScheduledWardId"
      :multi-select="false"
      :enable-multi-select-mode="false"
      title="患者列表"
      :show-pending-filter="false"
      :collapsed="false"
      @patient-select="handlePatientSelect"
    />

    <!-- 右侧综合记录工作区 -->
    <div class="work-area">
      <!-- 患者信息栏 -->
      <div v-if="selectedPatient" class="patient-info-bar">
        <div class="patient-badge">{{ selectedPatient.bedId }}</div>
        <div class="patient-details">
          <span class="name">{{ selectedPatient.patientName }}</span>
          <span class="meta">
            {{ selectedPatient.gender }} | {{ selectedPatient.age }}岁 | {{ selectedPatient.weight }}kg
          </span>
          <span class="tag">护理{{ selectedPatient.nursingGrade }}级</span>
        </div>
      </div>

      <!-- 未选择患者提示 -->
      <div v-if="!selectedPatient" class="no-patient-hint">
        <el-icon><InfoFilled /></el-icon>
        <span>请从左侧患者列表中选择患者查看综合记录</span>
      </div>

      <!-- 工作区内容容器 -->
      <div v-if="selectedPatient" class="content-container">
        <!-- 筛选工具栏 -->
        <div class="filter-toolbar">
          <!-- 记录类型筛选 -->
          <div class="filter-group">
            <span class="filter-label">记录类型:</span>
            <el-checkbox-group v-model="typeFilter" @change="applyFilters" size="small">
              <el-checkbox label="Order">医嘱</el-checkbox>
              <el-checkbox label="NursingRecord">护理记录</el-checkbox>
              <el-checkbox label="InspectionReport">检查报告</el-checkbox>
            </el-checkbox-group>
          </div>

          <!-- 时间范围筛选 -->
          <div class="filter-group">
            <span class="filter-label">时间范围:</span>
            <el-date-picker
              v-model="dateRange"
              type="datetimerange"
              range-separator="至"
              start-placeholder="开始时间"
              end-placeholder="结束时间"
              format="YYYY-MM-DD HH:mm"
              value-format="YYYY-MM-DDTHH:mm:ssZ"
              size="small"
              @change="applyFilters"
              style="width: 400px;"
            />
            <el-button 
              type="text" 
              size="small" 
              @click="clearDateRange"
              style="margin-left: 10px;"
            >
              清除
            </el-button>
          </div>

          <!-- 内容搜索 -->
          <div class="filter-group search-group">
            <el-input
              v-model="searchKeyword"
              placeholder="搜索记录内容（标题/内容）"
              clearable
              @input="applyFilters"
              size="small"
              class="search-input"
            >
              <template #prefix>
                <el-icon><Search /></el-icon>
              </template>
            </el-input>
          </div>
        </div>

        <!-- ==================== 记录列表 ==================== -->
        <div class="records-list-container">
          <!-- 加载状态 -->
          <div v-if="loading" class="loading-state">
            <el-icon class="is-loading"><Loading /></el-icon>
            <p>加载中...</p>
          </div>

          <!-- 空状态 -->
          <div v-else-if="displayRecords.length === 0" class="empty-state">
            <div class="empty-icon">📋</div>
            <p v-if="recordsList.length === 0">暂无记录</p>
            <p v-else>没有符合筛选条件的记录</p>
            <p v-if="recordsList.length === 0" class="empty-hint">
              请确认该患者是否有医嘱、护理记录或检查报告
            </p>
          </div>

          <!-- 记录列表 -->
          <div v-else class="records-list">
            <div 
              v-for="record in displayRecords" 
              :key="`${record.recordType}-${record.recordId}`"
              :class="['record-item', `record-type-${record.recordType.toLowerCase()}`]"
            >
              <!-- 记录类型标签 -->
              <div class="record-header">
                <el-tag 
                  :type="getRecordTypeColor(record.recordType)" 
                  size="small"
                  class="record-type-tag"
                >
                  {{ getRecordTypeName(record.recordType) }}
                </el-tag>

                <span class="record-time">{{ formatDateTime(record.recordTime) }}</span>
              </div>

              <!-- 记录标题 -->
              <div class="record-title">
                {{ record.title }}
              </div>

              <!-- 记录内容 -->
              <div v-if="record.content" class="record-content">
                {{ record.content }}
              </div>

              <!-- 记录详情 -->
              <div class="record-details">
                <!-- 医嘱特有信息 -->
                <template v-if="record.recordType === 'Order'">
                  <div class="detail-row">
                    <span class="detail-label">医嘱类型:</span>
                    <span class="detail-value">{{ record.orderTypeDisplay }}</span>
                  </div>
                  <div class="detail-row">
                    <span class="detail-label">状态:</span>
                    <el-tag :type="getStatusColor(record.status)" size="small">
                      {{ record.statusDisplay }}
                    </el-tag>
                  </div>
                  <div v-if="record.extras?.isLongTerm !== undefined" class="detail-row">
                    <span class="detail-label">类型:</span>
                    <span class="detail-value">{{ record.extras.isLongTerm ? '长期' : '临时' }}</span>
                  </div>
                </template>

                <!-- 检查报告特有信息 -->
                <template v-if="record.recordType === 'InspectionReport'">
                  <div class="detail-row">
                    <span class="detail-label">状态:</span>
                    <el-tag :type="getStatusColor(record.status)" size="small">
                      {{ record.statusDisplay }}
                    </el-tag>
                  </div>
                  <div v-if="record.extras?.attachmentUrl" class="detail-row">
                    <span class="detail-label">附件:</span>
                    <el-link 
                      :href="record.extras.attachmentUrl" 
                      target="_blank"
                      type="primary"
                      size="small"
                    >
                      查看报告
                    </el-link>
                  </div>
                </template>

                <!-- 创建人信息 -->
                <div class="detail-row">
                  <span class="detail-label">记录人:</span>
                  <span class="detail-value">{{ record.creatorName || '未知' }}</span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, watch } from 'vue';
import { ElMessage } from 'element-plus';
import { InfoFilled, Search, Loading } from '@element-plus/icons-vue';
import PatientListPanel from '../components/PatientListPanel.vue';
import { getPatientRecords } from '../api/patientRecords';
import { getPatientsWithPendingCount, getCurrentWard } from '../api/patient';

// ==================== 状态管理 ====================

// 患者相关
const patientList = ref([]);
const selectedPatient = ref(null);
const currentScheduledWardId = ref(null);

// 记录数据
const recordsList = ref([]); // 当前选中患者的记录列表
const loading = ref(false);

// 筛选条件
const typeFilter = ref(['Order', 'NursingRecord', 'InspectionReport']); // 默认显示所有类型
const dateRange = ref(null);
const searchKeyword = ref('');

// ==================== 计算属性 ====================

/**
 * 显示的记录列表（应用前端筛选条件后）
 * 注意：类型筛选和时间范围筛选已在后端完成，这里只做内容搜索
 */
const displayRecords = computed(() => {
  let filtered = [...recordsList.value];

  // 应用内容搜索（只在前端做，实现实时搜索）
  if (searchKeyword.value) {
    const keyword = searchKeyword.value.toLowerCase();
    filtered = filtered.filter(r => {
      return (r.title && r.title.toLowerCase().includes(keyword)) ||
             (r.content && r.content.toLowerCase().includes(keyword));
    });
  }

  // 按时间倒序排序（后端已经排序，这里再次确保）
  filtered.sort((a, b) => new Date(b.recordTime) - new Date(a.recordTime));

  return filtered;
});

// ==================== 患者选择处理 ====================

/**
 * 处理患者选择事件
 */
const handlePatientSelect = async (eventData) => {
  const { patient } = eventData;
  
  // 如果选择的是同一个患者，不重复加载
  if (selectedPatient.value?.patientId === patient.patientId) {
    return;
  }
  
  selectedPatient.value = patient;
  
  // 加载选中患者的记录
  await loadPatientRecords();
};

// ==================== 数据加载 ====================

/**
 * 加载选中患者的综合记录
 */
const loadPatientRecords = async () => {
  if (!selectedPatient.value) {
    recordsList.value = [];
    return;
  }

  loading.value = true;
  try {
    console.log(`🔄 开始加载患者 ${selectedPatient.value.patientName} 的综合记录...`);

    // 构建请求参数
    const requestParams = {
      patientIds: [selectedPatient.value.patientId],
      // 如果 typeFilter 为空数组，传递 null 让后端返回所有类型
      recordTypes: typeFilter.value.length > 0 ? typeFilter.value : null,
      sortDescending: true
    };

    // 添加时间范围筛选（如果设置了）
    if (dateRange.value && dateRange.value.length === 2) {
      // 将本地时间转换为 UTC 时间字符串（ISO 8601 格式）
      // PostgreSQL 要求 timestamp with time zone 必须是 UTC 格式
      const startDate = new Date(dateRange.value[0]);
      const endDate = new Date(dateRange.value[1]);
      
      requestParams.startTime = startDate.toISOString(); // 转换为 UTC: "2025-12-25T02:30:00.000Z"
      requestParams.endTime = endDate.toISOString();     // 转换为 UTC: "2025-12-25T14:30:00.000Z"
      
      console.log(`🕐 时间范围筛选: ${dateRange.value[0]} ~ ${dateRange.value[1]}`);
      console.log(`🌍 转换为UTC: ${requestParams.startTime} ~ ${requestParams.endTime}`);
    }

    console.log('📤 请求参数:', JSON.stringify(requestParams, null, 2));
    
    const result = await getPatientRecords(requestParams);
    
    console.log('📥 API返回结果:', result);
    console.log('📥 API返回结果类型:', Array.isArray(result) ? 'Array' : typeof result);

    // 处理返回结果（单个患者返回对象，多个患者返回数组）
    // 后端配置了camelCase序列化，所以属性名是 records（小写）
    if (!result) {
      console.warn('⚠️ API返回结果为空');
      recordsList.value = [];
      ElMessage.warning('未获取到任何记录');
      return;
    }

    let extractedRecords = [];
    
    if (Array.isArray(result)) {
      // 如果是数组，取第一个（应该只有一个）
      console.log('📋 返回结果是数组，长度:', result.length);
      const firstResult = result[0];
      if (firstResult) {
        console.log('📋 第一个结果对象:', firstResult);
        // 兼容两种属性名（Records 或 records）
        extractedRecords = firstResult.records || firstResult.Records || [];
        console.log('📋 提取的记录数:', extractedRecords.length);
      } else {
        console.warn('⚠️ 数组为空或第一个元素为空');
        extractedRecords = [];
      }
    } else {
      // 单个患者返回对象，属性名是 records（camelCase）或 Records（PascalCase）
      console.log('📋 返回结果是对象，keys:', Object.keys(result));
      extractedRecords = result.records || result.Records || [];
      console.log('📋 提取的记录数:', extractedRecords.length);
      if (extractedRecords.length > 0) {
        console.log('📋 第一条记录示例:', extractedRecords[0]);
      }
    }

    recordsList.value = extractedRecords;
    console.log(`✅ 加载完成，共 ${recordsList.value.length} 条记录`);
    
    if (recordsList.value.length > 0) {
      ElMessage.success(`加载了 ${recordsList.value.length} 条记录`);
    }
  } catch (error) {
    console.error('❌ 加载患者综合记录失败:', error);
    
    // 提取错误信息
    let errorMessage = '加载记录失败';
    if (error.response?.data?.message) {
      errorMessage = error.response.data.message;
    } else if (error.message) {
      errorMessage = error.message;
    }
    
    ElMessage.error(errorMessage);
    recordsList.value = [];
  } finally {
    loading.value = false;
  }
};

/**
 * 初始化患者数据
 */
const initializePatientData = async () => {
  try {
    // 获取当前护士信息
    const userInfoStr = localStorage.getItem('userInfo');
    if (!userInfoStr) {
      ElMessage.error('未找到用户信息');
      return;
    }

    const userInfo = JSON.parse(userInfoStr);
    const nurseId = userInfo.staffId;

    // 获取当前排班病区
    try {
      const result = await getCurrentWard(nurseId);
      currentScheduledWardId.value = result.wardId || userInfo.wardId;
    } catch (error) {
      console.error('获取当前排班病区失败:', error);
      currentScheduledWardId.value = userInfo.wardId;
    }

    // 加载患者列表
    const deptCode = userInfo.deptCode;
    if (!deptCode) {
      ElMessage.error('未找到护士所属科室信息');
      return;
    }

    const summary = await getPatientsWithPendingCount(deptCode);
    patientList.value = summary;
    
    console.log(`✅ 患者列表加载完成，共 ${summary.length} 位患者`);
  } catch (error) {
    console.error('初始化患者数据失败:', error);
    ElMessage.error(error.message || '初始化失败');
  }
};

// ==================== 筛选处理 ====================

/**
 * 应用筛选条件（重新从后端加载数据）
 */
const applyFilters = () => {
  // 如果已选择患者，重新加载数据
  if (selectedPatient.value) {
    loadPatientRecords();
  }
};

/**
 * 清除时间范围
 */
const clearDateRange = () => {
  dateRange.value = null;
  applyFilters();
};

// ==================== 辅助方法 ====================

/**
 * 获取记录类型名称
 */
const getRecordTypeName = (recordType) => {
  const map = {
    'Order': '医嘱',
    'NursingRecord': '护理记录',
    'InspectionReport': '检查报告'
  };
  return map[recordType] || recordType;
};

/**
 * 获取记录类型颜色
 */
const getRecordTypeColor = (recordType) => {
  const map = {
    'Order': 'primary',
    'NursingRecord': 'success',
    'InspectionReport': 'info'
  };
  return map[recordType] || '';
};

/**
 * 获取状态颜色
 */
const getStatusColor = (status) => {
  const statusStr = status?.toLowerCase() || '';
  if (statusStr.includes('pending') || statusStr.includes('待')) {
    return 'warning';
  } else if (statusStr.includes('completed') || statusStr.includes('已完成') || statusStr.includes('已出')) {
    return 'success';
  } else if (statusStr.includes('stopped') || statusStr.includes('已停止')) {
    return 'danger';
  }
  return 'info';
};


/**
 * 格式化日期时间
 */
const formatDateTime = (dateTime) => {
  if (!dateTime) return '-';
  try {
    const date = new Date(dateTime);
    return date.toLocaleString('zh-CN', { 
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit',
      timeZone: 'Asia/Shanghai'
    });
  } catch {
    return dateTime;
  }
};

// ==================== 监听器 ====================

// 监听类型筛选和时间范围变化，重新从后端加载数据
// 注意：searchKeyword 的变化不需要重新加载，因为搜索是在前端做的
watch([typeFilter, dateRange], () => {
  // 如果已选择患者，重新加载数据
  if (selectedPatient.value) {
    loadPatientRecords();
  }
});

// ==================== 组件挂载 ====================

onMounted(async () => {
  console.log('🚀 患者综合记录界面初始化...');
  
  // 初始化患者数据
  await initializePatientData();
  
  console.log(`✅ 初始化完成，当前排班病区: ${currentScheduledWardId.value}`);
});
</script>

<style scoped>
/* ============================== 
  【患者综合记录界面样式】
  复用医嘱查询界面的设计系统
============================== */

/* ==================== 设计系统变量 ==================== */
.patient-records-view {
  /* 主题色 */
  --primary-color: #409eff;
  --success-color: #67c23a;
  --warning-color: #e6a23c;
  --danger-color: #f56c6c;
  --info-color: #909399;
  
  /* 背景色 */
  --bg-page: #f4f7f9;
  --bg-card: #ffffff;
  --bg-secondary: #f9fafc;
  
  /* 边框和文本 */
  --border-color: #dcdfe6;
  --text-primary: #303133;
  --text-regular: #606266;
  --text-secondary: #909399;
  
  /* 圆角 */
  --radius-large: 8px;
  --radius-medium: 6px;
  --radius-small: 4px;

  /* 布局：网格布局，左侧患者列表250px，右侧自适应 */
  display: grid;
  grid-template-columns: 250px 1fr;
  height: calc(100vh - 60px);
  background: var(--bg-page);
  gap: 20px;
  padding: 20px;
}

/* ==================== 右侧工作区 ==================== */
.work-area {
  background: var(--bg-card);
  border-radius: var(--radius-large);
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.08);
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

/* ==================== 未选择患者提示 ==================== */
.no-patient-hint {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 10px;
  padding: 20px;
  background: #f0f9ff;
  border-bottom: 1px solid #b3e0ff;
  color: var(--primary-color);
  font-size: 0.95rem;
}

.no-patient-hint .el-icon {
  font-size: 1.2rem;
}

/* ==================== 内容容器 ==================== */
.content-container {
  flex: 1;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

/* ==================== 筛选工具栏 ==================== */
.filter-toolbar {
  padding: 15px 25px;
  border-bottom: 1px solid var(--border-color);
  background: var(--bg-secondary);
  display: flex;
  flex-direction: column;
  gap: 12px;
  flex-shrink: 0;
}

.filter-group {
  display: flex;
  align-items: center;
  gap: 10px;
}

.filter-label {
  font-size: 0.9rem;
  color: var(--text-regular);
  font-weight: 500;
  min-width: 80px;
}

.search-group {
  margin-top: 8px;
}

.search-input {
  width: 100%;
  max-width: 400px;
}

/* ==================== 记录列表容器 ==================== */
.records-list-container {
  flex: 1;
  overflow-y: auto;
  padding: 20px 25px;
}

/* ==================== 加载状态 ==================== */
.loading-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 60px 20px;
  color: var(--text-secondary);
}

.loading-state .el-icon {
  font-size: 2rem;
  margin-bottom: 12px;
}

/* ==================== 空状态 ==================== */
.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 60px 20px;
  color: var(--text-secondary);
}

.empty-icon {
  font-size: 64px;
  margin-bottom: 16px;
  opacity: 0.5;
}

.empty-state p {
  font-size: 1rem;
  color: var(--text-secondary);
}

/* ==================== 记录列表 ==================== */
.records-list {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

/* ==================== 记录项 ==================== */
.record-item {
  background: var(--bg-card);
  border: 1.5px solid var(--border-color);
  border-radius: var(--radius-medium);
  padding: 16px;
  transition: all 0.3s;
}

.record-item:hover {
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.08);
  border-color: var(--primary-color);
  transform: translateY(-2px);
}

/* 记录类型特定样式 */
.record-item.record-type-order {
  border-left: 4px solid var(--primary-color);
}

.record-item.record-type-nursingrecord {
  border-left: 4px solid var(--success-color);
}

.record-item.record-type-inspectionreport {
  border-left: 4px solid var(--info-color);
}

/* ==================== 患者信息栏 ==================== */
.patient-info-bar {
  display: flex;
  align-items: center;
  gap: 16px;
  padding: 15px 25px;
  background: var(--bg-card);
  border-bottom: 2px solid #f0f0f0;
  border-left: 5px solid var(--primary-color);
}

.patient-info-bar .patient-badge {
  background: var(--primary-color);
  color: white;
  padding: 8px 16px;
  border-radius: var(--radius-small);
  font-weight: bold;
  font-size: 1.1rem;
}

.patient-details {
  flex: 1;
  display: flex;
  align-items: center;
  gap: 15px;
}

.patient-details .name {
  font-size: 1.2rem;
  font-weight: bold;
  color: var(--text-primary);
}

.patient-details .meta {
  font-size: 0.95rem;
  color: var(--text-secondary);
}

.patient-details .tag {
  background: #e8f4ff;
  color: var(--primary-color);
  padding: 4px 12px;
  border-radius: 20px;
  font-size: 0.85rem;
}

/* ==================== 记录头部 ==================== */
.record-header {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 12px;
  justify-content: space-between;
}

.record-type-tag {
  font-weight: 600;
}

.record-time {
  font-size: 0.85rem;
  color: var(--text-secondary);
}

/* ==================== 记录标题 ==================== */
.record-title {
  font-size: 1rem;
  font-weight: 600;
  color: var(--text-primary);
  margin-bottom: 8px;
}

/* ==================== 记录内容 ==================== */
.record-content {
  font-size: 0.9rem;
  color: var(--text-regular);
  line-height: 1.6;
  margin-bottom: 12px;
  padding: 12px;
  background: var(--bg-secondary);
  border-radius: var(--radius-small);
}

/* ==================== 记录详情 ==================== */
.record-details {
  display: flex;
  flex-direction: column;
  gap: 8px;
  padding-top: 12px;
  border-top: 1px dashed var(--border-color);
}

.detail-row {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 0.85rem;
}

.detail-label {
  color: var(--text-secondary);
  min-width: 70px;
  font-weight: 500;
}

.detail-value {
  color: var(--text-regular);
  flex: 1;
}

/* ==================== 响应式 ==================== */
@media (max-width: 768px) {
  .patient-records-view {
    grid-template-columns: 1fr;
  }
}
</style>

