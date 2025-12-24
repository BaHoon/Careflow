<template>
  <div class="stop-order-panel">
    <!-- 医嘱信息摘要 -->
    <el-alert
      type="warning"
      :closable="false"
      class="alert-info"
    >
      <template #title>
        <div class="alert-content">
          <span>⚠️ 确认停止医嘱</span>
          <el-tag type="primary" size="small">{{ getOrderTypeName(order.orderType) }}</el-tag>
          <span class="order-summary">{{ order.summary }}</span>
        </div>
      </template>
    </el-alert>

    <!-- 说明文本 -->
    <div class="instruction-box">
      <div class="instruction-title">操作说明：</div>
      <ol v-if="order.status === 1" class="instruction-list">
        <li>该医嘱<strong>尚未签收</strong>，停止后将<strong>直接取消</strong></li>
        <li>无需选择停止节点，填写停嘱原因即可</li>
        <li>停嘱后医嘱状态将变为<strong>已取消</strong>，无需护士签收</li>
      </ol>
      <ol v-else class="instruction-list">
        <li>请选择<strong>停止节点</strong>（从该任务开始的所有任务将被锁定）</li>
        <li>停止节点任务本身<strong>也会</strong>被锁定，不会继续执行</li>
        <li>停止节点之前的任务<strong>保持不变</strong></li>
        <li>填写停嘱原因后，医嘱将进入<strong>等待护士签收</strong>状态</li>
      </ol>
    </div>

    <!-- 任务列表（仅已签收医嘱显示） -->
    <div v-if="order.status !== 1" class="task-selection">
      <div class="task-selection-header">
        <span class="header-title">选择停止节点</span>
        <span class="header-hint">（点击任务卡片选择）</span>
      </div>

      <el-scrollbar max-height="400px">
        <div class="task-list">
          <div 
            v-for="(task, index) in tasks" 
            :key="task.id"
            :class="[
              'task-card',
              { 
                'selected': selectedTaskId === task.id,
                'before-stop': isBeforeStopNode(task.id),
                'after-stop': isAfterStopNode(task.id),
                'disabled': !canSelectTask(task)
              }
            ]"
            @click="selectTask(task)"
          >
            <!-- 任务头部 -->
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
            </div>

            <!-- 选中标识 -->
            <div v-if="selectedTaskId === task.id" class="selected-indicator">
              <el-icon><CircleCheck /></el-icon>
              <span>停止节点</span>
            </div>

            <!-- 锁定标识 -->
            <div v-if="isAfterStopNode(task.id) && selectedTaskId" class="lock-indicator">
              <el-icon><Lock /></el-icon>
              <span>将被锁定</span>
            </div>
          </div>

          <div v-if="tasks.length === 0" class="no-tasks">
            该医嘱暂无可操作的任务
          </div>
        </div>
      </el-scrollbar>
    </div>

    <!-- 停嘱原因输入 -->
    <div class="stop-reason-section">
      <div class="section-title">停嘱原因 <span class="required">*</span></div>
      <el-input
        v-model="stopReason"
        type="textarea"
        :rows="4"
        placeholder="请详细说明停嘱原因（例如：患者病情好转，无需继续用药）"
        maxlength="500"
        show-word-limit
      />
    </div>

    <!-- 确认信息 -->
    <el-alert
      v-if="selectedTaskId && stopReason"
      type="info"
      :closable="false"
      class="confirm-info"
    >
      <template #title>
        <div class="confirm-content">
          <div class="confirm-item">
            <span class="confirm-label">停止节点:</span>
            <span class="confirm-value">任务 {{ selectedTaskIndex + 1 }}</span>
          </div>
          <div class="confirm-item">
            <span class="confirm-label">将锁定任务:</span>
            <span class="confirm-value">{{ lockedTaskCount }} 个</span>
          </div>
          <div class="confirm-item full-width">
            <span class="confirm-label">停嘱原因:</span>
            <span class="confirm-value">{{ stopReason }}</span>
          </div>
        </div>
      </template>
    </el-alert>

    <!-- 操作按钮 -->
    <div class="action-buttons">
      <el-button @click="handleCancel" size="large">
        取消
      </el-button>
      <el-button 
        type="danger" 
        @click="handleConfirm"
        :disabled="!canConfirm"
        :loading="submitting"
        size="large"
      >
        确认停嘱
      </el-button>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, watch } from 'vue';
import { ElMessage } from 'element-plus';
import { CircleCheck, Lock } from '@element-plus/icons-vue';

// ==================== Props ====================
const props = defineProps({
  order: {
    type: Object,
    required: true
  },
  tasks: {
    type: Array,
    default: () => []
  }
});

// ==================== Emits ====================
const emit = defineEmits(['confirm', 'cancel']);

// ==================== 状态管理 ====================
const selectedTaskId = ref(null);
const stopReason = ref('');
const submitting = ref(false);

// ==================== 计算属性 ====================
// 是否有任务
const hasTasks = computed(() => {
  return props.tasks && props.tasks.length > 0;
});

// 选中任务的索引
const selectedTaskIndex = computed(() => {
  return props.tasks.findIndex(t => t.id === selectedTaskId.value);
});

// 将被锁定的任务数量（包含选定节点本身）
const lockedTaskCount = computed(() => {
  if (!selectedTaskId.value) return 0;
  const index = selectedTaskIndex.value;
  if (index === -1) return 0;
  
  // 统计从停止节点开始（包含该节点）的未完成任务数量
  return props.tasks
    .slice(index) // 从选定节点开始，而不是 index + 1
    .filter(t => t.status !== 5 && t.status !== 8) // 排除已完成(5)和已停止(8)
    .length;
});

// 是否可以确认
const canConfirm = computed(() => {
  // 未签收医嘱（无任务）：只需填写原因
  // 已签收医嘱（有任务）：需要选择任务和填写原因
  const hasReason = stopReason.value.trim().length > 0;
  if (!hasTasks.value) {
    return hasReason && !submitting.value;
  }
  return selectedTaskId.value && hasReason && !submitting.value;
});

// ==================== 方法 ====================
// 判断任务是否在停止节点之前
const isBeforeStopNode = (taskId) => {
  if (!selectedTaskId.value) return false;
  const selectedIndex = selectedTaskIndex.value;
  const currentIndex = props.tasks.findIndex(t => t.id === taskId);
  return currentIndex < selectedIndex;
};

// 判断任务是否在停止节点及之后（包含选定节点本身）
const isAfterStopNode = (taskId) => {
  if (!selectedTaskId.value) return false;
  const selectedIndex = selectedTaskIndex.value;
  const currentIndex = props.tasks.findIndex(t => t.id === taskId);
  // 修改为 >= 而不是 >，包含选定节点本身
  return currentIndex >= selectedIndex && (props.tasks[currentIndex].status !== 5 && props.tasks[currentIndex].status !== 8);
};

// 判断任务是否可以被选择为停止节点
const canSelectTask = (task) => {
  // 只有未完成的任务才能作为停止节点
  // 已完成(5)和已停止(8)的任务不能选择
  return task.status !== 5 && task.status !== 8;
};

// 选择任务
const selectTask = (task) => {
  if (!canSelectTask(task)) {
    ElMessage.warning('已完成或已停止的任务不能作为停止节点');
    return;
  }
  
  if (selectedTaskId.value === task.id) {
    selectedTaskId.value = null; // 取消选择
  } else {
    selectedTaskId.value = task.id;
  }
};

// 取消
const handleCancel = () => {
  emit('cancel');
};

// 确认停嘱
const handleConfirm = async () => {
  if (!canConfirm.value) {
    const msg = hasTasks.value ? '请选择停止节点并填写停嘱原因' : '请填写停嘱原因';
    ElMessage.warning(msg);
    return;
  }

  submitting.value = true;
  try {
    emit('confirm', {
      orderId: props.order.id,
      stopAfterTaskId: hasTasks.value ? selectedTaskId.value : 0, // 未签收医嘱传0
      stopReason: stopReason.value.trim()
    });
  } finally {
    submitting.value = false;
  }
};

// ==================== 格式化方法 ====================
const formatDateTime = (dateString) => {
  if (!dateString) return '-';
  try {
    const date = new Date(dateString);
    return date.toLocaleString('zh-CN', { 
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit'
    });
  } catch {
    return dateString;
  }
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

// 获取任务时效状态（显示在标题栏）
const getTaskTimingStatus = (task) => {
  // 未完成且有异常
  if (task.status === 8 && task.exceptionReason) {
    return { text: '\u274c\u5f02\u5e38', class: 'status-exception' };
  }
  
  // 已完成或执行中，计算时效
  if (task.actualStartTime) {
    const delay = getDelayMinutes(task.plannedStartTime, task.actualStartTime);
    if (delay === null) return { text: '', class: '' };
    
    if (task.status === 5) { // 已完成
      if (delay > 15) return { text: `\u23f1\ufe0f\u5ef6\u8fdf${delay}\u5206`, class: 'status-delay-serious' };
      if (delay > 5) return { text: `\u23f1\ufe0f\u5ef6\u8fdf${delay}\u5206`, class: 'status-delay-minor' };
      if (delay < -5) return { text: `\u26a1\u63d0\u524d${-delay}\u5206`, class: 'status-early' };
      return { text: '\u2713\u6309\u65f6', class: 'status-ontime' };
    }
    
    if (task.status === 4) { // 执行中
      return { text: '\u8fdb\u884c\u4e2d...', class: 'status-progress' };
    }
  }
  
  // 停嘱锁定
  if (task.status === 6) {
    return { text: '\ud83d\udd12\u9501\u5b9a', class: 'status-locked' };
  }
  
  return { text: '', class: '' };
};

const getTaskStatusText = (status) => {
  const statusMap = {
    0: '待申请', 1: '已申请', 2: '已确认', 3: '已分配',
    4: '进行中', 5: '已完成', 6: '未完成', 7: '停嘱中', 8: '已停止'
  };
  return statusMap[status] || `状态${status}`;
};

const getTaskStatusColor = (status) => {
  const colorMap = {
    0: 'info', 1: 'warning', 2: 'primary', 3: 'primary',
    4: 'success', 5: 'success', 6: 'danger', 7: 'warning', 8: 'info'
  };
  return colorMap[status] || 'info';
};

// 获取任务类型样式和名称（使用正确的TaskCategory枚举：1-6）
const getTaskCategoryStyle = (category) => {
  const categoryMap = {
    1: { name: '\u64cd\u4f5c', color: '#67c23a', type: 'success' },      // Immediate 即刻执行
    2: { name: '\u64cd\u4f5c', color: '#409eff', type: 'primary' },      // Duration 持续执行
    3: { name: '\u64cd\u4f5c', color: '#e6a23c', type: 'warning' },      // ResultPending 结果等待
    4: { name: '\u64cd\u4f5c', color: '#9b59b6', type: 'info' },         // DataCollection 护理记录
    5: { name: '\u53d6\u836f\u6838\u5bf9', color: '#909399', type: '' },          // Verification 核对类
    6: { name: '\u68c0\u67e5\u7533\u8bf7', color: '#17a2b8', type: 'info' }       // ApplicationWithPrint 申请打印
  };
  return categoryMap[category] || { name: '\u672a\u77e5', color: '#909399', type: 'info' };
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
</script>

<style scoped>
.stop-order-panel {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.alert-info {
  margin-bottom: 0;
}

.alert-content {
  display: flex;
  align-items: center;
  gap: 12px;
  font-size: 1rem;
  font-weight: 600;
}

.order-summary {
  color: #303133;
}

.instruction-box {
  background: #f0f9ff;
  border: 1px solid #b3d8ff;
  border-radius: 6px;
  padding: 16px;
}

.instruction-title {
  font-weight: 600;
  color: #409eff;
  margin-bottom: 8px;
  font-size: 0.95rem;
}

.instruction-list {
  margin: 0;
  padding-left: 20px;
  color: #606266;
  font-size: 0.9rem;
  line-height: 1.8;
}

.instruction-list li {
  margin-bottom: 4px;
}

.instruction-list strong {
  color: #f56c6c;
  font-weight: 600;
}

.task-selection {
  border: 1px solid #dcdfe6;
  border-radius: 6px;
  overflow: hidden;
}

.task-selection-header {
  background: #f5f7fa;
  padding: 12px 16px;
  border-bottom: 1px solid #dcdfe6;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.header-title {
  font-weight: 600;
  color: #303133;
  font-size: 0.95rem;
}

.header-hint {
  color: #909399;
  font-size: 0.85rem;
}

.task-list {
  padding: 12px;
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.task-card {
  display: flex;
  flex-direction: column;
  padding: 16px;
  background: #fff;
  border: 2px solid #e4e7ed;
  border-radius: 8px;
  margin-bottom: 12px;
  cursor: pointer;
  transition: all 0.3s;
  position: relative;
  gap: 12px;
}

.task-card:hover:not(.disabled) {
  border-color: #409eff;
  box-shadow: 0 2px 12px rgba(64, 158, 255, 0.2);
  transform: translateY(-2px);
}

.task-card.selected {
  border-color: #409eff;
  background: #ecf5ff;
  box-shadow: 0 2px 12px rgba(64, 158, 255, 0.3);
}

.task-card.before-stop {
  opacity: 0.6;
}

.task-card.after-stop {
  border-color: #f56c6c;
  background: #fef0f0;
}

.task-card.disabled {
  opacity: 0.4;
  cursor: not-allowed;
  background: #f5f7fa;
}

.task-header {
  display: flex;
  align-items: center;
  gap: 10px;
  flex-wrap: wrap;
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
  margin: 0 4px;
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

.selected-indicator,
.lock-indicator {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 6px 12px;
  border-radius: 20px;
  font-size: 0.85rem;
  font-weight: 600;
}

.selected-indicator {
  background: #409eff;
  color: white;
}

.lock-indicator {
  background: #f56c6c;
  color: white;
}

.no-tasks {
  text-align: center;
  padding: 40px;
  color: #909399;
  font-size: 0.9rem;
}

.stop-reason-section {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.section-title {
  font-weight: 600;
  color: #303133;
  font-size: 0.95rem;
}

.required {
  color: #f56c6c;
  margin-left: 4px;
}

.confirm-info {
  margin-top: 0;
}

.confirm-content {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 12px;
  font-size: 0.9rem;
}

.confirm-item {
  display: flex;
  gap: 8px;
}

.confirm-item.full-width {
  grid-column: 1 / -1;
}

.confirm-label {
  color: #909399;
  font-weight: 500;
  min-width: 90px;
}

.confirm-value {
  color: #303133;
  font-weight: 600;
}

.action-buttons {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
  padding-top: 8px;
}
</style>
