<template>
  <div class="patient-log-view">
    <!-- ============================== 
      【患者日志界面】
      左侧：患者列表（单选模式）
      右侧：患者日志工作区
    ============================== -->

    <!-- 左侧患者列表面板 -->
    <PatientListPanel 
      v-loading="patientListLoading"
      element-loading-text="正在加载患者列表..."
      :patient-list="patientList"
      :selected-patients="selectedPatients"
      :my-ward-id="currentScheduledWardId"
      :multi-select="false"
      :enable-multi-select-mode="false"
      :show-pending-filter="false"
      :show-badge="false"
      title="患者列表"
      @patient-select="handlePatientSelect"
    />

    <!-- 右侧工作区 -->
    <div class="work-area">
      <!-- 患者信息栏 -->
      <PatientInfoBar 
        :patients="selectedPatients"
        :is-multi-select="false"
        :show-sort-control="false"
      />

      <!-- 未选择患者提示 -->
      <div v-if="selectedPatients.length === 0" class="no-patient-hint">
        <el-icon :size="48"><InfoFilled /></el-icon>
        <span>请从左侧患者列表中选择患者查看日志</span>
      </div>

      <!-- 日志内容区 -->
      <div v-if="selectedPatients.length > 0" class="log-container">
        <!-- 筛选工具栏 -->
        <div class="filter-bar">
          <!-- 时间范围筛选 -->
          <div class="filter-item">
            <span class="filter-label">时间范围:</span>
            <el-date-picker
              v-model="dateRange"
              type="daterange"
              range-separator="至"
              start-placeholder="开始日期"
              end-placeholder="结束日期"
              value-format="YYYY-MM-DD"
              @change="loadPatientLog"
              size="small"
              class="date-picker"
            />
          </div>

          <!-- 内容类型筛选 -->
          <div class="filter-item">
            <span class="filter-label">内容类型:</span>
            <el-checkbox-group v-model="contentTypes" @change="loadPatientLog" size="small">
              <el-checkbox label="MedicalOrders">医嘱执行</el-checkbox>
              <el-checkbox label="NursingRecords">护理记录</el-checkbox>
              <el-checkbox label="ExamReports">检查报告</el-checkbox>
            </el-checkbox-group>
          </div>
        </div>

        <!-- 时间线内容区 -->
        <div class="timeline-content" v-loading="loading">
          <!-- 空状态 -->
          <div v-if="dailyLogs.length === 0 && !loading" class="empty-state">
            <div class="empty-icon">📝</div>
            <p>该时间段内暂无日志数据</p>
          </div>

          <!-- 按日期分组的卡片流 -->
          <div v-else class="daily-logs">
            <div 
              v-for="dayLog in dailyLogs" 
              :key="dayLog.date"
              class="day-section"
            >
              <!-- 日期分割线 -->
              <div class="date-divider">
                <span class="date-text">{{ formatDate(dayLog.date) }}</span>
              </div>

              <!-- 医嘱执行汇总卡片 -->
              <MedicalOrdersSummaryCard
                v-if="dayLog.medicalOrdersSummary"
                :summary="dayLog.medicalOrdersSummary"
                :date="dayLog.date"
                @order-click="handleOrderClick"
              />

              <!-- 护理记录汇总卡片 -->
              <NursingRecordsSummaryCard
                v-if="dayLog.nursingRecordsSummary"
                :summary="dayLog.nursingRecordsSummary"
                :date="dayLog.date"
                @record-click="handleRecordClick"
              />

              <!-- 检查报告汇总卡片 -->
              <ExamReportsSummaryCard
                v-if="dayLog.examReportsSummary"
                :summary="dayLog.examReportsSummary"
                :date="dayLog.date"
                @report-click="handleReportClick"
              />
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- ==================== 医嘱详情弹窗 ==================== -->
    <el-dialog
      v-model="orderDetailDialogVisible"
      :title="`医嘱详情 - ${currentOrderDetail?.summary || ''}`"
      width="900px"
      class="order-detail-dialog"
    >
      <div class="order-detail-dialog-body">
        <OrderDetailPanel 
          v-if="currentOrderDetail"
          :detail="currentOrderDetail"
          :filter-date="currentFilterDate"
          :nurse-mode="false"
        />
      </div>
      <template #footer>
        <el-button @click="orderDetailDialogVisible = false">关闭</el-button>
      </template>
    </el-dialog>

    <!-- ==================== 护理记录详情弹窗 ==================== -->
    <el-dialog
      v-model="recordDetailDialogVisible"
      title="护理记录详情"
      width="700px"
      class="record-detail-dialog"
    >
      <div v-if="currentRecordDetail" class="record-detail-content">
        <el-descriptions :column="2" border>
          <el-descriptions-item label="记录时间">
            {{ formatDateTime(currentRecordDetail.recordTime) }}
          </el-descriptions-item>
          <el-descriptions-item label="记录护士">
            {{ currentRecordDetail.recorderNurseName }}
          </el-descriptions-item>
          <el-descriptions-item label="体温">
            <span :class="{ 'abnormal-value': isVitalAbnormal(currentRecordDetail, 'temperature') }">
              {{ currentRecordDetail.temperature || '--' }} °C
            </span>
          </el-descriptions-item>
          <el-descriptions-item label="脉搏">
            <span :class="{ 'abnormal-value': isVitalAbnormal(currentRecordDetail, 'pulse') }">
              {{ currentRecordDetail.pulse || '--' }} 次/分
            </span>
          </el-descriptions-item>
          <el-descriptions-item label="血压">
            <span :class="{ 'abnormal-value': isVitalAbnormal(currentRecordDetail, 'bloodPressure') }">
              {{ currentRecordDetail.sysBp || '--' }}/{{ currentRecordDetail.diaBp || '--' }} mmHg
            </span>
          </el-descriptions-item>
          <el-descriptions-item label="血氧饱和度">
            <span :class="{ 'abnormal-value': isVitalAbnormal(currentRecordDetail, 'spo2') }">
              {{ currentRecordDetail.spo2 || '--' }} %
            </span>
          </el-descriptions-item>
        </el-descriptions>
      </div>
      <template #footer>
        <el-button @click="recordDetailDialogVisible = false">关闭</el-button>
      </template>
    </el-dialog>

    <!-- ==================== 检查报告详情弹窗 ==================== -->
    <el-dialog
      v-model="reportDetailDialogVisible"
      title="检查报告详情"
      width="800px"
      class="report-detail-dialog"
    >
      <div v-if="currentReportDetail" class="report-detail-content">
        <el-descriptions :column="2" border>
          <el-descriptions-item label="检查项目" :span="2">
            <strong>{{ currentReportDetail.itemName }}</strong>
          </el-descriptions-item>
          <el-descriptions-item label="报告时间">
            {{ formatDateTime(currentReportDetail.reportTime) }}
          </el-descriptions-item>
          <el-descriptions-item label="报告状态">
            <el-tag :type="getReportStatusColor(currentReportDetail.reportStatus)">
              {{ getReportStatusText(currentReportDetail.reportStatus) }}
            </el-tag>
          </el-descriptions-item>
          <el-descriptions-item v-if="currentReportDetail.reviewerName" label="审核医生" :span="2">
            {{ currentReportDetail.reviewerName }}
          </el-descriptions-item>
          <el-descriptions-item v-if="currentReportDetail.findings" label="检查所见" :span="2">
            <div class="report-text">{{ currentReportDetail.findings }}</div>
          </el-descriptions-item>
          <el-descriptions-item v-if="currentReportDetail.impression" label="诊断结论" :span="2">
            <div class="report-text impression">{{ currentReportDetail.impression }}</div>
          </el-descriptions-item>
        </el-descriptions>
      </div>
      <template #footer>
        <el-button @click="reportDetailDialogVisible = false">关闭</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, watch } from 'vue';
import { ElMessage } from 'element-plus';
import { InfoFilled } from '@element-plus/icons-vue';
import { getPatientLog } from '@/api/patientLog';
import { getOrderDetail } from '@/api/doctorOrder';
import PatientListPanel from '@/components/PatientListPanel.vue';
import PatientInfoBar from '@/components/PatientInfoBar.vue';
import MedicalOrdersSummaryCard from '@/components/PatientLog/MedicalOrdersSummaryCard.vue';
import NursingRecordsSummaryCard from '@/components/PatientLog/NursingRecordsSummaryCard.vue';
import ExamReportsSummaryCard from '@/components/PatientLog/ExamReportsSummaryCard.vue';
import OrderDetailPanel from '@/components/OrderDetailPanel.vue';
import { usePatientData } from '@/composables/usePatientData';

// ==================== 患者数据管理 ====================
const { 
  patientList,
  selectedPatient, 
  selectedPatients,
  currentScheduledWardId,
  selectPatient,
  clearSelection,
  initializePatientData
} = usePatientData();

const loading = ref(false);
const patientListLoading = ref(false); // 患者列表加载状态

// 筛选条件
const dateRange = ref([]);
const contentTypes = ref(['MedicalOrders', 'NursingRecords', 'ExamReports']);

// 日志数据
const dailyLogs = ref([]);

// ==================== 详情弹窗状态 ====================
const orderDetailDialogVisible = ref(false);
const currentOrderDetail = ref(null);
const currentFilterDate = ref(null);

const recordDetailDialogVisible = ref(false);
const currentRecordDetail = ref(null);

const reportDetailDialogVisible = ref(false);
const currentReportDetail = ref(null);

// ==================== 初始化 ====================
onMounted(async () => {
  console.log('🚀 患者日志界面初始化...');
  
  // 设置默认日期范围：前天~今天
  const today = new Date();
  const twoDaysAgo = new Date(today);
  twoDaysAgo.setDate(today.getDate() - 2);
  
  dateRange.value = [
    twoDaysAgo.toISOString().split('T')[0],
    today.toISOString().split('T')[0]
  ];
  
  // 🚀 性能优化：延迟加载患者列表，让页面框架先渲染
  // 使用 setTimeout 将患者列表加载推迟到下一个事件循环
  // 这样用户可以立即看到页面框架，而不是等待数据加载完成
  setTimeout(async () => {
    patientListLoading.value = true;
    try {
      // 初始化患者数据（获取排班病区 + 加载患者列表）
      await initializePatientData();
      console.log(`✅ 初始化完成，当前排班病区: ${currentScheduledWardId.value}`);
      console.log(`📊 患者列表加载完成，共 ${patientList.value.length} 位患者`);
    } catch (error) {
      console.error('❌ 患者列表加载失败:', error);
      ElMessage.error('患者列表加载失败');
    } finally {
      patientListLoading.value = false;
    }
  }, 100); // 延迟100ms，让页面先渲染
});

// ==================== 监听患者选择变化 ====================
watch(selectedPatients, (newPatients) => {
  if (newPatients.length > 0) {
    console.log(`📋 患者选择变化: ${newPatients[0].patientName}`);
    loadPatientLog();
  } else {
    dailyLogs.value = [];
  }
}, { deep: true });

// ==================== 方法 ====================

/**
 * 患者选择处理
 */
const handlePatientSelect = ({ patient }) => {
  selectPatient(patient, false); // 单选模式
  console.log(`✅ 选择患者: ${patient.patientName}`);
};

/**
 * 加载患者日志
 */
const loadPatientLog = async () => {
  if (selectedPatients.value.length === 0) return;
  
  // 如果没有选择任何内容类型，清空日志并返回
  if (contentTypes.value.length === 0) {
    dailyLogs.value = [];
    return;
  }
  
  loading.value = true;
  try {
    // 🔧 将日期字符串转换为UTC时间范围
    // 前端选择的是日期（如 "2025-12-26"），需要转换为当天的开始和结束时间（UTC）
    // startDate: "2025-12-26" → "2025-12-26T00:00:00.000Z"
    // endDate: "2025-12-27" → "2025-12-27T23:59:59.999Z"
    const startDate = new Date(dateRange.value[0] + 'T00:00:00');
    const endDate = new Date(dateRange.value[1] + 'T23:59:59.999');
    
    console.log('📋 加载患者日志，参数:', {
      patientId: selectedPatients.value[0].patientId,
      startDate: startDate.toISOString(),
      endDate: endDate.toISOString(),
      contentTypes: contentTypes.value.join(',')
    });
    
    const response = await getPatientLog({
      patientId: selectedPatients.value[0].patientId,
      startDate: startDate.toISOString(), // 转换为UTC: "2025-12-26T00:00:00.000Z"
      endDate: endDate.toISOString(),     // 转换为UTC: "2025-12-27T23:59:59.999Z"
      contentTypes: contentTypes.value.join(',')
    });
    
    console.log('✅ 患者日志数据返回:', response);
    
    // 响应拦截器已经解包了 response.data，所以直接访问 dailyLogs
    dailyLogs.value = response.dailyLogs || [];
    
    console.log('📊 设置 dailyLogs:', dailyLogs.value);
  } catch (error) {
    console.error('❌ 加载患者日志失败:', error);
    ElMessage.error('加载失败，请重试');
    dailyLogs.value = [];
  } finally {
    loading.value = false;
  }
};

/**
 * 日期格式化 (显示"今天"、"昨天"等)
 */
const formatDate = (dateStr) => {
  const date = new Date(dateStr + 'T00:00:00');
  const today = new Date();
  today.setHours(0, 0, 0, 0);
  const yesterday = new Date(today);
  yesterday.setDate(today.getDate() - 1);
  
  const targetDate = new Date(dateStr + 'T00:00:00');
  targetDate.setHours(0, 0, 0, 0);
  
  if (targetDate.getTime() === today.getTime()) {
    return `今天 (${dateStr})`;
  } else if (targetDate.getTime() === yesterday.getTime()) {
    return `昨天 (${dateStr})`;
  } else {
    const weekDays = ['周日', '周一', '周二', '周三', '周四', '周五', '周六'];
    return `${dateStr} ${weekDays[date.getDay()]}`;
  }
};

/**
 * 日期时间格式化 (UTC → 北京时间)
 * @param {string} dateTimeString - UTC时间字符串
 * @returns {string} 格式化后的北京时间字符串
 */
const formatDateTime = (dateTimeString) => {
  if (!dateTimeString) return '--';
  
  try {
    // 🔧 确保UTC时间字符串带有Z标识
    let utcString = dateTimeString;
    if (!dateTimeString.endsWith('Z') && !dateTimeString.includes('+')) {
      utcString = dateTimeString + 'Z';
    }
    
    const date = new Date(utcString);
    
    // JavaScript的toLocaleString会自动转换为本地时区（北京时间UTC+8）
    return date.toLocaleString('zh-CN', { 
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit',
      hour12: false,
      timeZone: 'Asia/Shanghai'
    });
  } catch (error) {
    console.error('时间格式化失败:', error, dateTimeString);
    return dateTimeString;
  }
};

/**
 * 医嘱点击处理
 */
const handleOrderClick = async (orderId, date) => {
  try {
    loading.value = true;
    
    // 调用医嘱详情API
    const response = await getOrderDetail(orderId);
    
    // 响应拦截器已经解包，response 就是后端返回的 OrderDetailDto
    currentOrderDetail.value = response;
    currentFilterDate.value = date; // 设置日期过滤
    orderDetailDialogVisible.value = true;
  } catch (error) {
    console.error('获取医嘱详情失败:', error);
    ElMessage.error('获取医嘱详情失败');
  } finally {
    loading.value = false;
  }
};

/**
 * 护理记录点击处理
 */
const handleRecordClick = (recordId, date) => {
  // 从dailyLogs中查找对应的护理记录
  for (const dayLog of dailyLogs.value) {
    if (dayLog.nursingRecordsSummary && dayLog.nursingRecordsSummary.records) {
      const record = dayLog.nursingRecordsSummary.records.find(r => r.id === recordId);
      if (record) {
        currentRecordDetail.value = record;
        recordDetailDialogVisible.value = true;
        return;
      }
    }
  }
  
  ElMessage.warning('未找到该护理记录');
};

/**
 * 检查报告点击处理
 */
const handleReportClick = (reportId, date) => {
  // 从dailyLogs中查找对应的检查报告
  for (const dayLog of dailyLogs.value) {
    if (dayLog.examReportsSummary && dayLog.examReportsSummary.reports) {
      const report = dayLog.examReportsSummary.reports.find(r => r.id === reportId);
      if (report) {
        currentReportDetail.value = report;
        reportDetailDialogVisible.value = true;
        return;
      }
    }
  }
  
  ElMessage.warning('未找到该检查报告');
};

/**
 * 判断某个生命体征是否异常（用于护理记录详情）
 */
const isVitalAbnormal = (record, vitalType) => {
  if (!record.isAbnormal || !record.abnormalItems?.length) {
    return false;
  }
  
  const abnormalMap = {
    'temperature': 'Temperature',
    'pulse': 'Pulse',
    'bloodPressure': 'BloodPressure',
    'spo2': 'SpO2'
  };
  
  return record.abnormalItems.includes(abnormalMap[vitalType]);
};

/**
 * 获取报告状态颜色
 */
const getReportStatusColor = (status) => {
  const statusMap = {
    'Pending': 'warning',
    'Completed': 'success',
    'Reviewed': 'primary',
    'Cancelled': 'info'
  };
  return statusMap[status] || 'info';
};

/**
 * 获取报告状态文本
 */
const getReportStatusText = (status) => {
  const textMap = {
    'Pending': '待报告',
    'Completed': '已完成',
    'Reviewed': '已审核',
    'Cancelled': '已取消'
  };
  return textMap[status] || status;
};
</script>

<style scoped lang="scss">
/* ============================== 
  【患者日志界面样式】
  复用现有界面的设计风格和颜色变量
============================== */

.patient-log-view {
  --primary-color: #409eff;
  --success-color: #67c23a;
  --warning-color: #e6a23c;
  --danger-color: #f56c6c;
  --info-color: #909399;
  --bg-page: #f5f7fa;
  --bg-card: #ffffff;
  --text-primary: #303133;
  --text-regular: #606266;
  --text-secondary: #909399;
  --border-base: #dcdfe6;
  --border-light: #e4e7ed;
  --radius-base: 8px;
  --shadow-light: 0 2px 12px rgba(0, 0, 0, 0.1);

  display: flex;
  height: calc(100vh - 60px);
  background: var(--bg-page);
  overflow: hidden;
  
  .work-area {
    flex: 1;
    display: flex;
    flex-direction: column;
    overflow: hidden;
    
    /* 未选择患者提示 */
    .no-patient-hint {
      flex: 1;
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      gap: 16px;
      color: var(--text-secondary);
      font-size: 16px;
    }
    
    /* 日志容器 */
    .log-container {
      flex: 1;
      display: flex;
      flex-direction: column;
      overflow: hidden;
      
      /* 筛选工具栏 */
      .filter-bar {
        padding: 16px 20px;
        background: var(--bg-card);
        border-bottom: 1px solid var(--border-light);
        display: flex;
        gap: 24px;
        flex-wrap: wrap;
        align-items: center;
        flex-shrink: 0;
        
        .filter-item {
          display: flex;
          align-items: center;
          gap: 10px;
          
          .filter-label {
            font-size: 14px;
            color: var(--text-regular);
            font-weight: 500;
            white-space: nowrap;
          }
          
          .date-picker {
            width: 300px;
          }
        }
      }
      
      /* 时间线内容区 */
      .timeline-content {
        flex: 1;
        overflow-y: auto;
        padding: 20px;
        background: var(--bg-page);
        
        /* 空状态 */
        .empty-state {
          text-align: center;
          padding: 80px 20px;
          color: var(--text-secondary);
          
          .empty-icon {
            font-size: 72px;
            margin-bottom: 20px;
            opacity: 0.5;
          }
          
          p {
            font-size: 16px;
            margin: 0;
          }
        }
        
        /* 每日日志列表 */
        .daily-logs {
          max-width: 1200px;
          margin: 0 auto;
          
          .day-section {
            margin-bottom: 40px;
            
            /* 日期分割线 */
            .date-divider {
              position: relative;
              text-align: center;
              margin-bottom: 20px;
              
              &::before {
                content: '';
                position: absolute;
                top: 50%;
                left: 0;
                right: 0;
                height: 1px;
                background: linear-gradient(to right, transparent, var(--border-base), transparent);
                z-index: 0;
              }
              
              .date-text {
                position: relative;
                display: inline-block;
                padding: 0 20px;
                background: var(--bg-page);
                color: var(--text-regular);
                font-size: 15px;
                font-weight: 600;
                z-index: 1;
              }
            }
          }
        }
      }
    }
  }
}

/* 自定义滚动条样式 */
.timeline-content::-webkit-scrollbar {
  width: 8px;
}

.timeline-content::-webkit-scrollbar-track {
  background: #f1f1f1;
  border-radius: 4px;
}

.timeline-content::-webkit-scrollbar-thumb {
  background: #c0c4cc;
  border-radius: 4px;
}

.timeline-content::-webkit-scrollbar-thumb:hover {
  background: #909399;
}

/* ==================== 详情弹窗样式 ==================== */
.order-detail-dialog,
.record-detail-dialog,
.report-detail-dialog {
  :deep(.el-dialog__header) {
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    padding: 20px 24px;
    
    .el-dialog__title {
      color: #ffffff;
      font-size: 18px;
      font-weight: 600;
    }
    
    .el-dialog__headerbtn .el-dialog__close {
      color: #ffffff;
      font-size: 20px;
    }
  }
  
  :deep(.el-dialog__body) {
    padding: 24px;
    max-height: 70vh;
    overflow-y: auto;
  }
  
  :deep(.el-dialog__footer) {
    padding: 16px 24px;
    border-top: 1px solid var(--border-light);
  }
}

.order-detail-dialog-body {
  .no-tasks {
    padding: 40px 20px;
    text-align: center;
    color: var(--text-secondary);
    font-size: 14px;
  }
}

.record-detail-content,
.report-detail-content {
  .abnormal-value {
    color: var(--danger-color);
    font-weight: 700;
  }
  
  .report-text {
    line-height: 1.8;
    white-space: pre-wrap;
    word-wrap: break-word;
    
    &.impression {
      color: var(--success-color);
      font-weight: 600;
    }
  }
}
</style>

