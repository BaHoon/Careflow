<template>
  <div class="order-detail-panel">
    <!-- 使用风琴式折叠面板 -->
    <el-collapse v-model="activeNames">
      <!-- 基础信息 -->
      <el-collapse-item name="basic" class="detail-collapse-item">
        <template #title>
          <div class="collapse-title">
            <span class="title-icon">📋</span>
            <span class="title-text">基础信息</span>
          </div>
        </template>
        
        <div class="info-grid">
          <div class="info-item">
            <span class="label">医嘱ID:</span>
            <span class="value">{{ detail.id }}</span>
          </div>
          <div class="info-item">
            <span class="label">医嘱类型:</span>
            <el-tag :type="getOrderTypeColor(detail.orderType)" size="small">
              {{ getOrderTypeName(detail.orderType) }}
            </el-tag>
          </div>
          <div class="info-item">
            <span class="label">状态:</span>
            <el-tag :type="getStatusColor(detail.status)" size="small">
              {{ getStatusText(detail.status) }}
            </el-tag>
          </div>
          <div class="info-item">
            <span class="label">医嘱类别:</span>
            <el-tag :type="detail.isLongTerm ? 'primary' : 'warning'" size="small">
              {{ detail.isLongTerm ? '长期医嘱' : '临时医嘱' }}
            </el-tag>
          </div>
          
          <div class="info-item full-width">
            <span class="label">患者信息:</span>
            <span class="value">{{ detail.patientName }} (ID: {{ detail.patientId }})</span>
          </div>
          
          <div class="info-item">
            <span class="label">开单医生:</span>
            <span class="value">{{ detail.doctorName }}</span>
          </div>
          <div class="info-item">
            <span class="label">负责护士:</span>
            <span class="value">{{ detail.nurseName || '未分配' }}</span>
          </div>
          
          <div class="info-item">
            <span class="label">创建时间:</span>
            <span class="value">{{ formatDateTime(detail.createTime) }}</span>
          </div>
          <div class="info-item">
            <span class="label">计划结束:</span>
            <span class="value">{{ formatDateTime(detail.plantEndTime) }}</span>
          </div>
          
          <div v-if="detail.signedAt" class="info-item">
            <span class="label">签收时间:</span>
            <span class="value">{{ formatDateTime(detail.signedAt) }}</span>
          </div>
          <div v-if="detail.signedByNurseName" class="info-item">
            <span class="label">签收护士:</span>
            <span class="value">{{ detail.signedByNurseName }}</span>
          </div>
          
          <div v-if="detail.stopReason" class="info-item full-width stop-info">
            <span class="label">停嘱原因:</span>
            <span class="value danger">{{ detail.stopReason }}</span>
          </div>
          <div v-if="detail.stopOrderTime" class="info-item">
            <span class="label">停嘱时间:</span>
            <span class="value">{{ formatDateTime(detail.stopOrderTime) }}</span>
          </div>
          <div v-if="detail.stopDoctorName" class="info-item">
            <span class="label">停嘱医生:</span>
            <span class="value">{{ detail.stopDoctorName }}</span>
          </div>
          
          <div v-if="detail.remarks" class="info-item full-width">
            <span class="label">备注:</span>
            <span class="value">{{ detail.remarks }}</span>
          </div>
        </div>
      </el-collapse-item>

      <!-- 药品医嘱详情 -->
      <el-collapse-item 
        v-if="detail.orderType === 'MedicationOrder'"
        name="medication" 
        class="detail-collapse-item"
      >
        <template #title>
          <div class="collapse-title">
            <span class="title-icon">💊</span>
            <span class="title-text">药品信息</span>
          </div>
        </template>
        
        <div class="medication-info">
          <div class="info-grid">
            <div class="info-item">
              <span class="label">用药途径:</span>
              <span class="value">{{ getUsageRouteName(detail.usageRoute) }}</span>
            </div>
            <div class="info-item">
              <span class="label">时间策略:</span>
              <span class="value">{{ getTimingStrategyName(detail.timingStrategy) }}</span>
            </div>
            <div v-if="detail.startTime" class="info-item">
              <span class="label">开始时间:</span>
              <span class="value">{{ formatDateTime(detail.startTime) }}</span>
            </div>
            <div v-if="detail.intervalHours" class="info-item">
              <span class="label">执行间隔:</span>
              <span class="value">每{{ detail.intervalHours }}小时</span>
            </div>
            <div v-if="detail.intervalDays" class="info-item">
              <span class="label">间隔天数:</span>
              <span class="value">{{ detail.intervalDays }}天</span>
            </div>
            <div v-if="detail.timingStrategy === 'SLOTS' && detail.smartSlotsMask" class="info-item full-width">
              <span class="label">执行时间点:</span>
              <span class="value">{{ getSlotNamesFromMask(detail.smartSlotsMask) }}</span>
            </div>
          </div>
          
          <div v-if="detail.medicationItems && detail.medicationItems.length > 0" class="drug-list">
            <div class="drug-list-header">药品列表</div>
            <div v-for="item in detail.medicationItems" :key="item.id" class="drug-item">
              <span class="drug-name">{{ item.drugName }}</span>
              <span class="drug-dosage">{{ item.dosage }}</span>
              <span v-if="item.note" class="drug-note">({{ item.note }})</span>
            </div>
          </div>
        </div>
      </el-collapse-item>

      <!-- 手术医嘱详情 -->
      <el-collapse-item 
        v-if="detail.orderType === 'SurgicalOrder'"
        name="surgical"
        class="detail-collapse-item"
      >
        <template #title>
          <div class="collapse-title">
            <span class="title-icon">🏥</span>
            <span class="title-text">手术信息</span>
          </div>
        </template>
        
        <div class="info-grid">
          <div class="info-item full-width">
            <span class="label">手术名称:</span>
            <span class="value highlight">{{ detail.surgeryName }}</span>
          </div>
          <div class="info-item">
            <span class="label">手术时间:</span>
            <span class="value">{{ formatDateTime(detail.scheduleTime) }}</span>
          </div>
          <div class="info-item">
            <span class="label">麻醉方式:</span>
            <span class="value">{{ detail.anesthesiaType }}</span>
          </div>
          <div class="info-item">
            <span class="label">切口部位:</span>
            <span class="value">{{ detail.incisionSite }}</span>
          </div>
          <div class="info-item">
            <span class="label">主刀医生:</span>
            <span class="value">{{ detail.surgeonName }}</span>
          </div>
          
          <div v-if="detail.requiredTalk && detail.requiredTalk.length > 0" class="info-item full-width">
            <span class="label">术前宣讲:</span>
            <div class="requirement-list">
              <div v-for="(item, index) in detail.requiredTalk" :key="index" class="requirement-item">
                • {{ item }}
              </div>
            </div>
          </div>
          
          <div v-if="detail.requiredOperation && detail.requiredOperation.length > 0" class="info-item full-width">
            <span class="label">术前操作:</span>
            <div class="requirement-list">
              <div v-for="(item, index) in detail.requiredOperation" :key="index" class="requirement-item">
                • {{ item }}
              </div>
            </div>
          </div>
          
          <div v-if="detail.surgicalItems && detail.surgicalItems.length > 0" class="info-item full-width">
            <span class="label">手术药品:</span>
            <div class="drug-list">
              <div v-for="item in detail.surgicalItems" :key="item.id" class="drug-item">
                <span class="drug-name">{{ item.drugName }}</span>
                <span class="drug-dosage">{{ item.dosage }}</span>
                <span v-if="item.note" class="drug-note">({{ item.note }})</span>
              </div>
            </div>
          </div>
        </div>
      </el-collapse-item>

      <!-- 检查医嘱详情 -->
      <el-collapse-item 
        v-if="detail.orderType === 'InspectionOrder'"
        name="inspection"
        class="detail-collapse-item"
      >
        <template #title>
          <div class="collapse-title">
            <span class="title-icon">🔬</span>
            <span class="title-text">检查信息</span>
          </div>
        </template>
        
        <div class="info-grid">
          <div class="info-item">
            <span class="label">检查项目:</span>
            <span class="value highlight">{{ detail.itemName }}</span>
          </div>
          <div class="info-item">
            <span class="label">项目代码:</span>
            <span class="value">{{ detail.itemCode }}</span>
          </div>
        </div>
      </el-collapse-item>

      <!-- 操作医嘱详情 -->
      <el-collapse-item 
        v-if="detail.orderType === 'OperationOrder'"
        name="operation"
        class="detail-collapse-item"
      >
        <template #title>
          <div class="collapse-title">
            <span class="title-icon">⚕️</span>
            <span class="title-text">操作信息</span>
          </div>
        </template>
        
        <div class="info-grid">
          <div class="info-item">
            <span class="label">操作名称:</span>
            <span class="value highlight">{{ detail.operationName }}</span>
          </div>
          <div class="info-item">
            <span class="label">操作代码:</span>
            <span class="value">{{ detail.operationCode }}</span>
          </div>
          <div v-if="detail.targetSite" class="info-item">
            <span class="label">操作部位:</span>
            <span class="value">{{ detail.targetSite }}</span>
          </div>
        </div>
      </el-collapse-item>

      <!-- 关联任务列表 -->
      <el-collapse-item name="tasks" class="detail-collapse-item">
        <template #title>
          <div class="collapse-title">
            <span class="title-icon">📋</span>
            <span class="title-text">关联任务 ({{ detail.tasks.length }})</span>
            <el-button 
              v-if="!expandAllTasks" 
              text 
              @click.stop="toggleExpandAllTasks"
              size="small"
              class="expand-btn"
            >
              全部展开
            </el-button>
            <el-button 
              v-else 
              text 
              @click.stop="toggleExpandAllTasks"
              size="small"
              class="expand-btn"
            >
              全部收起
            </el-button>
          </div>
        </template>
        
        <el-collapse v-model="activeTaskIds" class="task-collapse">
          <el-collapse-item 
            v-for="(task, index) in detail.tasks" 
            :key="task.id"
            :name="task.id"
            class="task-collapse-item"
          >
            <template #title>
              <div class="task-header">
                <span class="task-number">{{ index + 1 }}</span>
                <el-tag :type="getTaskStatusColor(task.status)" size="small">
                  {{ getTaskStatusText(task.status) }}
                </el-tag>
                <el-tag 
                  size="small" 
                  :type="getTaskCategoryStyle(task.category).type"
                  :style="{ borderColor: getTaskCategoryStyle(task.category).color, color: getTaskCategoryStyle(task.category).color }"
                >
                  {{ getTaskCategoryStyle(task.category).name }}
                </el-tag>
                <span v-if="getTaskTimingStatus(task).text" class="timing-status" :class="getTaskTimingStatus(task).class">
                  {{ getTaskTimingStatus(task).text }}
                </span>
                <span class="task-time-separator">|</span>
                <span class="task-time">计划: {{ formatTime(task.plannedStartTime) }}</span>
                <span v-if="task.statusBeforeLocking !== null" class="lock-indicator" title="此任务已被停嘱锁定">
                  🔒 锁前: {{ getTaskStatusText(task.statusBeforeLocking) }}
                </span>
              </div>
            </template>
            
            <div class="task-detail">
              <!-- 时间线 -->
              <div class="task-section">
                <div class="section-title">⏰ 时间线</div>
                <div class="timeline-item">
                  <span class="timeline-label">计划:</span>
                  <span class="timeline-value">{{ formatDateTime(task.plannedStartTime) }}</span>
                </div>
                <div v-if="task.actualStartTime" class="timeline-item">
                  <span class="timeline-label">开始:</span>
                  <span class="timeline-value">{{ formatDateTime(task.actualStartTime) }}</span>
                  <span v-if="getDelayMinutes(task.plannedStartTime, task.actualStartTime) !== null" class="timeline-badge" :class="getDelayClass(getDelayMinutes(task.plannedStartTime, task.actualStartTime))">
                    [{{ formatDelayText(getDelayMinutes(task.plannedStartTime, task.actualStartTime)) }}]
                  </span>
                </div>
                <div v-if="task.actualEndTime" class="timeline-item">
                  <span class="timeline-label">结束:</span>
                  <span class="timeline-value">{{ formatDateTime(task.actualEndTime) }}</span>
                  <span v-if="getDurationMinutes(task.actualStartTime, task.actualEndTime)" class="timeline-badge duration">
                    [耗时{{ getDurationMinutes(task.actualStartTime, task.actualEndTime) }}分钟]
                  </span>
                </div>
              </div>
              
              <!-- 执行信息 -->
              <div v-if="task.executorName || task.exceptionReason" class="task-section">
                <div class="section-title">👤 执行信息</div>
                <div v-if="task.executorName" class="timeline-item">
                  <span class="timeline-label">负责护士:</span>
                  <span class="timeline-value">{{ task.executorName }}</span>
                </div>
                <div v-if="task.exceptionReason" class="timeline-item">
                  <span class="timeline-label">异常原因:</span>
                  <span class="timeline-value danger">{{ task.exceptionReason }}</span>
                </div>
              </div>

              <!-- 护士模式：任务操作按钮 -->
              <div v-if="nurseMode" class="nurse-actions">
                <el-button 
                  type="primary" 
                  size="small"
                  @click.stop="emit('update-task-execution', task.id)"
                  :icon="EditPen"
                >
                  修改执行情况
                </el-button>
                <el-button 
                  type="success" 
                  size="small"
                  @click.stop="emit('print-task-sheet', task.id)"
                  :icon="Printer"
                >
                  打印执行单
                </el-button>
              </div>
            </div>
          </el-collapse-item>
        </el-collapse>
        
        <div v-if="detail.tasks.length === 0" class="no-tasks">
          暂无关联任务
        </div>
      </el-collapse-item>
    </el-collapse>
  </div>
</template>

<script setup>
import { ref, computed, watch } from 'vue';
import { EditPen, Printer } from '@element-plus/icons-vue';

// ==================== Props ====================
const props = defineProps({
  detail: {
    type: Object,
    required: true
  },
  // 护士模式：显示任务操作按钮
  nurseMode: {
    type: Boolean,
    default: false
  }
});

// ==================== Emits ====================
const emit = defineEmits([
  'update-task-execution',  // 修改任务执行情况
  'print-task-sheet'        // 打印任务执行单
]);

// ==================== 风琴控制 ====================
// 主风琴面板控制（基础信息、药品信息等）
const activeNames = ref(['basic', 'tasks']); // 默认展开基础信息和任务列表

// 任务风琴控制
const activeTaskIds = ref([]);
const expandAllTasks = ref(false);

// 全部展开/收起任务
const toggleExpandAllTasks = () => {
  if (expandAllTasks.value) {
    activeTaskIds.value = [];
    expandAllTasks.value = false;
  } else {
    activeTaskIds.value = props.detail.tasks.map(t => t.id);
    expandAllTasks.value = true;
  }
};

// 监听detail变化，重置展开状态
watch(() => props.detail, (newDetail) => {
  // 根据医嘱类型自动展开对应的信息面板
  activeNames.value = ['basic', 'tasks'];
  if (newDetail.orderType === 'MedicationOrder') {
    activeNames.value.push('medication');
  } else if (newDetail.orderType === 'SurgicalOrder') {
    activeNames.value.push('surgical');
  } else if (newDetail.orderType === 'InspectionOrder') {
    activeNames.value.push('inspection');
  } else if (newDetail.orderType === 'OperationOrder') {
    activeNames.value.push('operation');
  }
  
  activeTaskIds.value = [];
  expandAllTasks.value = false;
}, { immediate: true });

// ==================== 格式化方法 ====================
const formatDateTime = (dateString) => {
  if (!dateString) return '-';
  try {
    const date = new Date(dateString);
    return date.toLocaleString('zh-CN', { 
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit'
    });
  } catch {
    return dateString;
  }
};

// ==================== 状态映射 ====================
const getStatusText = (status) => {
  const statusMap = {
    0: '草稿', 1: '未签收', 2: '已签收', 3: '进行中',
    4: '已完成', 5: '已拒绝', 6: '已取消', 7: '等待停嘱', 8: '已停止'
  };
  return statusMap[status] || `状态${status}`;
};

const getStatusColor = (status) => {
  const colorMap = {
    0: 'info', 1: 'warning', 2: 'primary', 3: 'success',
    4: 'success', 5: 'danger', 6: 'info', 7: 'warning', 8: 'info'
  };
  return colorMap[status] || 'info';
};

const getOrderTypeName = (orderType) => {
  const nameMap = {
    MedicationOrder: '药品医嘱',
    InspectionOrder: '检查医嘱',
    OperationOrder: '操作医嘱',
    SurgicalOrder: '手术医嘱'
  };
  return nameMap[orderType] || orderType;
};

const getOrderTypeColor = (orderType) => {
  const colorMap = {
    MedicationOrder: 'success',
    InspectionOrder: 'info',
    OperationOrder: 'warning',
    SurgicalOrder: 'danger'
  };
  return colorMap[orderType] || 'info';
};

// 修正后的用药途径映射 - 匹配后端UsageRoute枚举值
const getUsageRouteName = (route) => {
  if (route === null || route === undefined) return '未指定';
  
  const routeMap = {
    1: '口服 (PO)',
    2: '外用/涂抹',
    10: '肌内注射 (IM)',
    11: '皮下注射 (SC)',
    12: '静脉推注 (IVP)',
    20: '静脉滴注 (IVGTT)',
    21: '吸氧'
  };
  return routeMap[route] || `未知途径(${route})`;
};

// 时间策略名称映射
const getTimingStrategyName = (strategy) => {
  if (!strategy) return '未指定';
  
  const strategyMap = {
    'IMMEDIATE': '立即执行',
    'SPECIFIC': '指定时间',
    'CYCLIC': '周期执行',
    'SLOTS': '时段执行',
    'OnceDaily': '每日一次',
    'TwiceDaily': '每日两次',
    'ThreeTimesDaily': '每日三次',
    'FourTimesDaily': '每日四次',
    'EveryOtherDay': '隔日一次',
    'StatDose': '立即执行',
    'CustomSchedule': '自定义时间',
    'Hourly': '按小时'
  };
  return strategyMap[strategy] || strategy;
};

// 根据时间槽掩码获取中文时间点名称
const getSlotNamesFromMask = (mask) => {
  if (!mask) return '未指定';
  
  const slotMap = {
    1: '早餐前',
    2: '早餐后',
    4: '午餐前',
    8: '午餐后',
    16: '晚餐前',
    32: '晚餐后',
    64: '睡前'
  };
  
  const selectedSlots = [];
  for (let bit = 1; bit <= 64; bit *= 2) {
    if (mask & bit) {
      selectedSlots.push(slotMap[bit]);
    }
  }
  
  return selectedSlots.length > 0 ? selectedSlots.join('、') : '未指定';
};

const getTaskStatusText = (status) => {
  const statusMap = {
    0: '待申请', 1: '已申请', 2: '已确认', 3: '待执行',
    4: '进行中', 5: '已完成', 6: '未完成', 7: '停嘱中'
  };
  return statusMap[status] || `状态${status}`;
};

const getTaskStatusColor = (status) => {
  const colorMap = {
    0: 'info', 1: 'warning', 2: 'primary', 3: 'primary',
    4: 'success', 5: 'success', 6: 'danger', 7: 'warning'
  };
  return colorMap[status] || 'info';
};

// 获取任务类型样式和名称（使用正确的TaskCategory枚举：1-6）
const getTaskCategoryStyle = (category) => {
  const categoryMap = {
    1: { name: '操作', color: '#67c23a', type: 'success' },      // Immediate 即刻执行
    2: { name: '操作', color: '#409eff', type: 'primary' },      // Duration 持续执行
    3: { name: '操作', color: '#e6a23c', type: 'warning' },      // ResultPending 结果等待
    4: { name: '操作', color: '#9b59b6', type: 'info' },         // DataCollection 护理记录
    5: { name: '取药核对', color: '#909399', type: '' },          // Verification 核对类
    6: { name: '检查申请', color: '#17a2b8', type: 'info' }       // ApplicationWithPrint 申请打印
  };
  return categoryMap[category] || { name: '未知', color: '#909399', type: 'info' };
};

// 格式化只显示时间（HH:mm）
const formatTime = (dateString) => {
  if (!dateString) return '--:--';
  try {
    const date = new Date(dateString);
    return date.toLocaleTimeString('zh-CN', { hour: '2-digit', minute: '2-digit' });
  } catch {
    return '--:--';
  }
};

// 计算延迟分钟数
const getDelayMinutes = (plannedTime, actualTime) => {
  if (!plannedTime || !actualTime) return null;
  try {
    const planned = new Date(plannedTime);
    const actual = new Date(actualTime);
    return Math.round((actual - planned) / 60000);
  } catch {
    return null;
  }
};

// 计算持续时间（分钟）
const getDurationMinutes = (startTime, endTime) => {
  if (!startTime || !endTime) return null;
  try {
    const start = new Date(startTime);
    const end = new Date(endTime);
    return Math.round((end - start) / 60000);
  } catch {
    return null;
  }
};

// 格式化延迟文本
const formatDelayText = (minutes) => {
  if (minutes > 15) return `延迟${minutes}分钟`;
  if (minutes > 5) return `延迟${minutes}分钟`;
  if (minutes < -5) return `提前${-minutes}分钟`;
  return '按时';
};

// 获取延迟样式类
const getDelayClass = (minutes) => {
  if (minutes > 15) return 'delay-serious';
  if (minutes > 5) return 'delay-minor';
  if (minutes < -5) return 'early';
  return 'ontime';
};

// 获取任务时效状态（显示在标题栏）
const getTaskTimingStatus = (task) => {
  // 未完成且有异常
  if (task.status === 8 && task.exceptionReason) {
    return { text: '❌异常', class: 'status-exception' };
  }
  
  // 已完成或执行中，计算时效
  if (task.actualStartTime) {
    const delay = getDelayMinutes(task.plannedStartTime, task.actualStartTime);
    if (delay === null) return { text: '', class: '' };
    
    if (task.status === 5) { // 已完成
      if (delay > 15) return { text: `⏱️延迟${delay}分`, class: 'status-delay-serious' };
      if (delay > 5) return { text: `⏱️延迟${delay}分`, class: 'status-delay-minor' };
      if (delay < -5) return { text: `⚡提前${-delay}分`, class: 'status-early' };
      return { text: '✓按时', class: 'status-ontime' };
    }
    
    if (task.status === 4) { // 执行中
      return { text: '进行中...', class: 'status-progress' };
    }
  }
  
  // 停嘱锁定
  if (task.status === 6) {
    return { text: '🔒锁定', class: 'status-locked' };
  }
  
  return { text: '', class: '' };
};
</script>

<style scoped>
.order-detail-panel {
  display: flex;
  flex-direction: column;
  /* 移除 max-height 和 overflow-y，让整个面板可滚动 */
}

/* 风琴面板样式 */
.detail-collapse-item {
  margin-bottom: 12px;
  border: 1px solid #e4e7ed;
  border-radius: 8px;
  overflow: hidden;
  background: #fff;
}

/* 为风琴面板标题添加内边距 */
.detail-collapse-item :deep(.el-collapse-item__header) {
  padding-left: 20px;
  padding-right: 20px;
}

/* 展开按钮（箭头）样式 - 确保显示 */
.detail-collapse-item :deep(.el-collapse-item__arrow) {
  display: inline-block !important;
  margin-right: 12px;
  margin-left: 0 !important;
  color: #409eff !important;
  font-size: 14px !important;
  font-weight: bold;
  order: -1;
}

/* 确保箭头在header内正确定位 */
.detail-collapse-item :deep(.el-collapse-item__header) {
  display: flex !important;
  align-items: center;
}

.collapse-title {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 1rem;
  font-weight: 600;
  color: #303133;
  flex: 1;
  padding-left: 0;
}

.title-icon {
  font-size: 1.2rem;
}

.title-text {
  flex: 1;
}

.expand-btn {
  margin-left: auto;
  margin-right: 48px;
}

.info-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 16px;
  padding: 16px 32px;
}

.info-item {
  display: flex;
  gap: 8px;
  font-size: 0.9rem;
  align-items: flex-start;
}

.info-item.full-width {
  grid-column: 1 / -1;
}

.label {
  color: #909399;
  font-weight: 500;
  min-width: 90px;
  flex-shrink: 0;
}

.value {
  color: #606266;
  font-weight: 600;
  flex: 1;
}

.value.highlight {
  color: #409eff;
  font-size: 1rem;
}

.value.danger {
  color: #f56c6c;
}

.stop-info {
  background: #fef0f0;
  padding: 12px;
  border-radius: 6px;
  border-left: 4px solid #f56c6c;
}

.medication-info {
  display: flex;
  flex-direction: column;
  gap: 16px;
  padding: 16px 32px;
}

.drug-list-header {
  font-weight: 600;
  color: #606266;
  margin-bottom: 8px;
  font-size: 0.95rem;
}

.drug-list {
  display: flex;
  flex-direction: column;
  gap: 8px;
  margin-top: 12px;
}

.drug-item {
  display: flex;
  gap: 12px;
  align-items: center;
  padding: 10px 14px;
  background: #f0f9ff;
  border-radius: 6px;
  border-left: 3px solid #409eff;
}

.drug-name {
  font-weight: 600;
  color: #409eff;
  font-size: 0.9rem;
}

.drug-dosage {
  font-weight: 600;
  color: #67c23a;
  font-size: 0.9rem;
}

.drug-note {
  color: #e6a23c;
  font-size: 0.85rem;
  font-style: italic;
}

.requirement-list {
  display: flex;
  flex-direction: column;
  gap: 6px;
  margin-top: 4px;
}

.requirement-item {
  color: #606266;
  font-size: 0.9rem;
  padding-left: 8px;
}

/* 任务风琴样式 */
.task-collapse {
  border: none;
  padding: 8px 32px;
}

.task-collapse-item {
  margin-bottom: 8px;
  border: 1px solid #e4e7ed;
  border-radius: 6px;
  overflow: hidden;
  background: #fafafa;
}

/* 任务子项的箭头样式 */
.task-collapse-item :deep(.el-collapse-item__arrow) {
  display: inline-block !important;
  margin-right: 8px;
  margin-left: 0 !important;
  color: #409eff !important;
  font-size: 12px !important;
  font-weight: bold;
  order: -1;
}

.task-header {
  display: flex;
  align-items: center;
  gap: 10px;
  flex: 1;
  padding-right: 20px;
}

.task-number {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 24px;
  height: 24px;
  background: #409eff;
  color: #fff;
  font-size: 0.8rem;
  font-weight: bold;
  border-radius: 50%;
  flex-shrink: 0;
}

.task-time-separator {
  color: #dcdfe6;
  margin: 0 8px;
  font-weight: normal;
}

.task-time {
  font-size: 0.85rem;
  color: #909399;
  font-weight: normal;
}

.timing-status {
  font-size: 0.8rem;
  padding: 2px 8px;
  border-radius: 3px;
  font-weight: 500;
  margin-left: 8px;
}

.timing-status.status-ontime {
  background: #f0f9ff;
  color: #67c23a;
}

.timing-status.status-early {
  background: #f0f9ff;
  color: #409eff;
}

.timing-status.status-delay-minor {
  background: #fdf6ec;
  color: #e6a23c;
}

.timing-status.status-delay-serious {
  background: #fef0f0;
  color: #f56c6c;
}

.timing-status.status-exception {
  background: #fef0f0;
  color: #f56c6c;
  font-weight: 600;
}

.timing-status.status-progress {
  background: #f0f9ff;
  color: #409eff;
}

.timing-status.status-locked {
  background: #fef0f0;
  color: #f56c6c;
}

.lock-indicator {
  background: #fef0f0;
  color: #f56c6c;
  padding: 4px 8px;
  border-radius: 4px;
  font-size: 0.75rem;
  font-weight: 600;
  border: 1px solid #fbc4c4;
}

.task-detail {
  padding: 16px;
  background: #fff;
  border-top: 1px solid #e4e7ed;
}

.task-section {
  margin-bottom: 16px;
}

.task-section:last-child {
  margin-bottom: 0;
}

.section-title {
  font-size: 0.85rem;
  font-weight: 600;
  color: #606266;
  margin-bottom: 12px;
  padding-bottom: 6px;
  border-bottom: 1px solid #f0f0f0;
}

.timeline-item {
  display: flex;
  align-items: baseline;
  margin-bottom: 8px;
  font-size: 0.9rem;
  line-height: 1.8;
}

.timeline-item:last-child {
  margin-bottom: 0;
}

.timeline-label {
  color: #909399;
  font-weight: 500;
  min-width: 80px;
  flex-shrink: 0;
}

.timeline-value {
  color: #606266;
  font-weight: 500;
  margin-right: 8px;
}

.timeline-value.danger {
  color: #f56c6c;
  font-weight: 600;
}

.timeline-badge {
  font-size: 0.8rem;
  font-weight: 500;
  margin-left: 4px;
}

.timeline-badge.ontime {
  color: #67c23a;
}

.timeline-badge.early {
  color: #409eff;
}

.timeline-badge.delay-minor {
  color: #e6a23c;
}

.timeline-badge.delay-serious {
  color: #f56c6c;
}

.timeline-badge.duration {
  color: #909399;
}

.task-detail-row {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 10px;
  font-size: 0.9rem;
}

.task-detail-row:last-child {
  margin-bottom: 0;
}

.timing-badge {
  font-size: 0.75rem;
  padding: 2px 8px;
  border-radius: 3px;
  font-weight: 500;
  margin-left: 8px;
}

.timing-badge.ontime {
  background: #f0f9ff;
  color: #67c23a;
}

.timing-badge.early {
  background: #f0f9ff;
  color: #409eff;
}

.timing-badge.delay-minor {
  background: #fdf6ec;
  color: #e6a23c;
}

.timing-badge.delay-serious {
  background: #fef0f0;
  color: #f56c6c;
}

.timing-badge.duration {
  background: #f4f4f5;
  color: #909399;
}

.task-label {
  color: #909399;
  font-weight: 500;
  min-width: 90px;
  flex-shrink: 0;
}

.task-value {
  color: #606266;
  font-weight: 600;
  flex: 1;
}

.task-value.danger {
  color: #f56c6c;
}

.no-tasks {
  text-align: center;
  color: #c0c4cc;
  padding: 40px 16px;
  font-size: 0.9rem;
}

/* ==================== 护士操作按钮 ==================== */
.nurse-actions {
  display: flex;
  gap: 10px;
  justify-content: flex-end;
  margin-top: 16px;
  padding-top: 16px;
  border-top: 1px dashed #e4e7ed;
}

.nurse-actions .el-button {
  flex: 0 0 auto;
}
</style>
