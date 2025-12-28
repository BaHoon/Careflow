<template>
  <div>
    <!-- NursingTask 详情对话框 -->
    <el-dialog
      :model-value="modelValue && currentTask?.taskSource === 'NursingTask'"
      title="护理任务详情"
      width="700px"
      @update:model-value="handleDialogClose"
      class="task-detail-dialog"
    >
      <div v-if="currentTask" class="task-detail-content">
        <el-descriptions :column="2" border>
          <!-- 任务基本信息 -->
          <el-descriptions-item label="任务ID">
            {{ currentTask.id }}
          </el-descriptions-item>
          <el-descriptions-item label="任务来源">
            <el-tag type="info">护理任务</el-tag>
          </el-descriptions-item>
          
          <!-- 患者信息 -->
          <el-descriptions-item label="患者姓名">
            {{ currentTask.patientName }}
          </el-descriptions-item>
          <el-descriptions-item label="床号">
            {{ currentTask.bedId }}
          </el-descriptions-item>
          
          <!-- 任务类别和状态 -->
          <el-descriptions-item label="任务类别">
            <el-tag :type="getCategoryTagType(currentTask.category)">
              {{ getCategoryText(currentTask.category) }}
            </el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="任务状态">
            <el-tag :type="getStatusTagType(currentTask.status)">
              {{ getStatusText(currentTask.status) }}
            </el-tag>
          </el-descriptions-item>
          
          <!-- 时间信息 -->
          <el-descriptions-item 
            v-if="currentTask.category !== 'ApplicationWithPrint'"
            label="计划开始时间" 
            :span="2"
          >
            {{ formatDateTime(currentTask.plannedStartTime) }}
          </el-descriptions-item>
          <el-descriptions-item
            v-if="currentTask.actualStartTime"
            label="实际开始时间"
            :span="2"
          >
            {{ formatDateTime(currentTask.actualStartTime) }}
          </el-descriptions-item>
          <el-descriptions-item
            v-if="currentTask.actualEndTime"
            label="完成时间"
            :span="2"
          >
            {{ formatDateTime(currentTask.actualEndTime) }}
          </el-descriptions-item>
          
          <!-- 护士信息 -->
          <el-descriptions-item label="负责护士">
            {{ currentTask.assignedNurseName || '-' }}
          </el-descriptions-item>
          <el-descriptions-item v-if="currentTask.executorNurseName && currentTask.status === 5" label="执行护士">
            {{ currentTask.executorNurseName }}
          </el-descriptions-item>
          
          <!-- 延迟信息 -->
          <el-descriptions-item label="延迟信息">
            {{ currentTask.delayMinutes !== undefined && currentTask.delayMinutes !== null ? formatDelayMinutes(currentTask.delayMinutes) : '-' }}
          </el-descriptions-item>
          <el-descriptions-item label="允许延迟">
            {{ currentTask.allowedDelayMinutes !== undefined && currentTask.allowedDelayMinutes !== null ? formatAllowedDelayMinutes(currentTask.allowedDelayMinutes) : '-' }}
          </el-descriptions-item>
          
          <!-- 异常原因 -->
          <el-descriptions-item
            v-if="currentTask.exceptionReason"
            label="异常原因"
            :span="2"
          >
            <span style="color: #f56c6c;">{{ currentTask.exceptionReason }}</span>
          </el-descriptions-item>
        </el-descriptions>
      </div>
      <template #footer>
        <el-button @click="handleDialogClose">关闭</el-button>
      </template>
    </el-dialog>

    <!-- ExecutionTask 详情对话框 -->
    <el-dialog
      :model-value="modelValue && currentTask?.taskSource === 'ExecutionTask'"
      title="医嘱任务详情"
      width="700px"
      @update:model-value="handleDialogClose"
      class="task-detail-dialog"
    >
      <div v-if="currentTask" class="task-detail-content">
        <el-descriptions :column="2" border>
          <!-- 任务基本信息 -->
          <el-descriptions-item label="任务ID">
            {{ currentTask.id }}
          </el-descriptions-item>
          <el-descriptions-item label="任务来源">
            <el-tag type="success">医嘱任务</el-tag>
          </el-descriptions-item>
          
          <!-- 操作信息 -->
          <el-descriptions-item label="操作名称" :span="2">
            {{ getDisplayTitle(currentTask) }}
          </el-descriptions-item>
          <el-descriptions-item label="操作代码">
            {{ getOperationCode(currentTask) }}
          </el-descriptions-item>
          <el-descriptions-item label="操作部位">
            {{ getOperationSite(currentTask) || '-' }}
          </el-descriptions-item>
          
          <!-- 患者信息 -->
          <el-descriptions-item label="患者姓名">
            {{ currentTask.patientName }}
          </el-descriptions-item>
          <el-descriptions-item label="床号">
            {{ currentTask.bedId }}
          </el-descriptions-item>
          
          <!-- 任务类别和状态 -->
          <el-descriptions-item label="任务类别">
            <el-tag :type="getCategoryTagType(currentTask.category)">
              {{ getCategoryText(currentTask.category) }}
            </el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="任务状态">
            <el-tag :type="getStatusTagType(currentTask.status)">
              {{ getStatusText(currentTask.status) }}
            </el-tag>
          </el-descriptions-item>
          
          <!-- 时间信息 -->
          <el-descriptions-item 
            v-if="currentTask.category !== 'ApplicationWithPrint'"
            label="计划开始时间" 
            :span="2"
          >
            {{ formatDateTime(currentTask.plannedStartTime) }}
          </el-descriptions-item>
          <el-descriptions-item
            v-if="currentTask.actualStartTime"
            label="实际开始时间"
            :span="2"
          >
            {{ formatDateTime(currentTask.actualStartTime) }}
          </el-descriptions-item>
          <el-descriptions-item
            v-if="currentTask.actualEndTime"
            label="完成时间"
            :span="2"
          >
            {{ formatDateTime(currentTask.actualEndTime) }}
          </el-descriptions-item>
          
          <!-- 护士信息 -->
          <el-descriptions-item label="负责护士">
            {{ currentTask.assignedNurseName || '-' }}
          </el-descriptions-item>
          <el-descriptions-item v-if="currentTask.executorNurseName && currentTask.status === 5" label="执行护士">
            {{ currentTask.executorNurseName }}
          </el-descriptions-item>
          
          <!-- 延迟信息 -->
          <el-descriptions-item label="延迟信息">
            {{ currentTask.delayMinutes !== undefined && currentTask.delayMinutes !== null ? formatDelayMinutes(currentTask.delayMinutes) : '-' }}
          </el-descriptions-item>
          <el-descriptions-item label="允许延迟">
            {{ currentTask.allowedDelayMinutes !== undefined && currentTask.allowedDelayMinutes !== null ? formatAllowedDelayMinutes(currentTask.allowedDelayMinutes) : '-' }}
          </el-descriptions-item>
          
          <!-- 准备物品 -->
          <el-descriptions-item
            v-if="getPreparationItems(currentTask).length > 0"
            label="准备物品"
            :span="2"
          >
            <div class="preparation-items">
              <el-tag
                v-for="(item, index) in getPreparationItems(currentTask)"
                :key="index"
                type="info"
                closable
                :disable-transitions="false"
              >
                {{ item }}
              </el-tag>
            </div>
          </el-descriptions-item>
          
          <!-- 操作说明 -->
          <el-descriptions-item
            v-if="getTaskDescription(currentTask)"
            label="操作说明"
            :span="2"
          >
            <p class="task-description">{{ getTaskDescription(currentTask) }}</p>
          </el-descriptions-item>
          
          <!-- 执行结果 -->
          <el-descriptions-item
            v-if="currentTask.resultPayload"
            label="执行结果"
            :span="2"
          >
            <pre class="json-display">{{ formatJson(currentTask.resultPayload) }}</pre>
          </el-descriptions-item>
          
          <!-- 异常原因 -->
          <el-descriptions-item
            v-if="currentTask.exceptionReason"
            label="异常原因"
            :span="2"
          >
            <span style="color: #f56c6c;">{{ currentTask.exceptionReason }}</span>
          </el-descriptions-item>
        </el-descriptions>
      </div>
      <template #footer>
        <el-button @click="handleDialogClose">关闭</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { computed } from 'vue';

const props = defineProps({
  modelValue: {
    type: Boolean,
    default: false
  },
  task: {
    type: Object,
    default: null
  }
});

const emit = defineEmits(['update:modelValue']);

const currentTask = computed(() => props.task);

const handleDialogClose = () => {
  emit('update:modelValue', false);
};

// 格式化日期时间
const formatDateTime = (dateString) => {
  if (!dateString) return '';
  try {
    let utcString = dateString;
    if (!dateString.endsWith('Z') && !dateString.includes('+')) {
      utcString = dateString + 'Z';
    }
    const date = new Date(utcString);
    return date.toLocaleString('zh-CN', {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit',
      timeZone: 'Asia/Shanghai'
    });
  } catch {
    return dateString;
  }
};

// 格式化JSON - 增强错误处理和边界情况
const formatJson = (jsonString) => {
  if (!jsonString) return '';
  try {
    // 处理字符串和对象两种情况
    let obj;
    if (typeof jsonString === 'string') {
      // 检查是否真的是JSON格式
      const trimmed = jsonString.trim();
      if (!trimmed.startsWith('{') && !trimmed.startsWith('[')) {
        // 不是JSON，直接返回
        return jsonString;
      }
      obj = JSON.parse(jsonString);
    } else {
      obj = jsonString;
    }
    
    if (!obj || (typeof obj !== 'object')) {
      return String(jsonString);
    }
    
    return JSON.stringify(obj, null, 2);
  } catch (error) {
    // JSON解析失败，返回原始字符串
    console.warn('JSON格式化失败:', error);
    return String(jsonString);
  }
};

// 状态标签类型
const getStatusTagType = (status) => {
  const typeMap = {
    'Applying': 'info',
    0: 'info',
    'Applied': 'info',
    1: 'info',
    'AppliedConfirmed': 'warning',
    2: 'warning',
    'Pending': 'warning',
    3: 'warning',
    'InProgress': 'primary',
    'Running': 'primary',
    4: 'primary',
    'Completed': 'success',
    5: 'success',
    'OrderStopping': 'danger',
    6: 'danger',
    'Stopped': 'danger',
    7: 'danger',
    'Incomplete': 'info',
    'Skipped': 'info',
    8: 'info',
    'Cancelled': 'danger',
    9: 'danger'
  };
  return typeMap[status] || 'info';
};

// 状态文本
const getStatusText = (status) => {
  const textMap = {
    'Applying': '待申请',
    0: '待申请',
    'Applied': '已申请',
    1: '已申请',
    'AppliedConfirmed': '已就绪',
    2: '已就绪',
    'Pending': '待执行',
    3: '待执行',
    'InProgress': '执行中',
    'Running': '执行中',
    4: '执行中',
    'Completed': '已完成',
    5: '已完成',
    'OrderStopping': '停止中',
    6: '停止中',
    'Stopped': '已停止',
    7: '已停止',
    'Incomplete': '异常',
    'Skipped': '已跳过',
    8: '异常',
    'Cancelled': '已取消',
    9: '已取消'
  };
  return textMap[status] || status;
};

// 安全的JSON解析工具函数
const safeParseJson = (jsonString) => {
  if (!jsonString) return null;
  try {
    return typeof jsonString === 'string' 
      ? JSON.parse(jsonString) 
      : jsonString;
  } catch (error) {
    console.warn('JSON解析失败:', error);
    return null;
  }
};

// 获取操作名称
const getOperationName = (task) => {
  if (!task || !task.dataPayload) return '操作任务';
  
  const payload = safeParseJson(task.dataPayload);
  if (!payload) return task.opId || '操作任务';
  
  return payload.OperationName || payload.Title || task.opId || '操作任务';
};

// 获取显示标题（与 TaskItem.vue 中的 displayTitle 逻辑保持一致）
// 优先使用 DataPayload.Title，其次使用 taskTitle，最后使用操作名称
const getDisplayTitle = (task) => {
  if (!task) return '操作任务';
  
  // 首先尝试从 DataPayload 获取 Title
  if (task.dataPayload) {
    try {
      const payload = safeParseJson(task.dataPayload);
      if (payload && payload.Title) {
        return payload.Title;
      }
    } catch (e) {
      // ignore
    }
  }
  
  // 其次尝试使用 taskTitle
  if (task.taskSource === 'ExecutionTask' && task.taskTitle) {
    return task.taskTitle;
  }
  
  // 最后使用操作名称
  return getOperationName(task);
};

// 获取操作名称（带详细信息）- 优先显示详细的描述而不仅仅是操作代码
const getOperationNameWithDetails = (task) => {
  if (!task || !task.dataPayload) return '操作任务';
  
  const payload = safeParseJson(task.dataPayload);
  if (!payload) return task.opId || '操作任务';
  
  // 优先返回 Title（这通常包含详细信息），然后是 OperationName
  // 避免仅返回"操作任务"这样的通用文本
  if (payload.Title && payload.Title !== '操作任务' && !payload.Title.startsWith('操作：操作')) {
    return payload.Title;
  }
  
  if (payload.OperationName && payload.OperationName !== '操作任务') {
    return payload.OperationName;
  }
  
  return payload.OpId || payload.opId || '操作任务';
};

// 获取操作代码
const getOperationCode = (task) => {
  if (!task || !task.dataPayload) return '-';
  
  const payload = safeParseJson(task.dataPayload);
  if (!payload) return task.opId || '-';
  
  return payload.OpId || task.opId || '-';
};

// 获取操作部位
const getOperationSite = (task) => {
  if (!task || !task.dataPayload) return null;
  
  const payload = safeParseJson(task.dataPayload);
  if (!payload) return null;
  
  return payload.OperationSite || null;
};

// 获取准备物品列表
const getPreparationItems = (task) => {
  if (!task || !task.dataPayload) return [];
  
  const payload = safeParseJson(task.dataPayload);
  if (!payload || !Array.isArray(payload.PreparationItems)) return [];
  
  return payload.PreparationItems;
};

// 获取任务说明
const getTaskDescription = (task) => {
  if (!task || !task.dataPayload) return null;
  
  const payload = safeParseJson(task.dataPayload);
  if (!payload) return null;
  
  return payload.Description || null;
};

// 任务类别文本 - 统一转换为中文，保持最简单的文本状态
const getCategoryText = (category) => {
  const textMap = {
    // 英文映射
    'Immediate': '即刻执行',
    'Duration': '持续任务',
    'ResultPending': '结果待定',
    'DataCollection': '数据采集',
    'Verification': '核对验证',
    'Routine': '常规护理',
    'ReMeasure': '复测任务',
    'Supplement': '补充检测',
    'ApplicationWithPrint': '申请打印',
    'DischargeConfirmation': '出院确认',
    // 数字映射（如果后端返回数字枚举值）
    1: '即刻执行',
    2: '持续任务',
    3: '结果待定',
    4: '数据采集',
    5: '核对验证',
    6: '申请打印',
    11: '出院确认'
  };
  // 如果 category 存在但不在映射表中，返回一个安全的默认值
  const result = textMap[category];
  return result !== undefined ? result : '未知类别';
};

// 任务类别标签类型
const getCategoryTagType = (category) => {
  const typeMap = {
    'Immediate': 'success',
    'Duration': 'primary',
    'ResultPending': 'warning',
    'DataCollection': 'info',
    'Verification': 'info',
    'Routine': 'info',
    'ReMeasure': 'warning',
    'ApplicationWithPrint': 'info',
    'DischargeConfirmation': 'danger',
    // 数字映射
    1: 'success',
    2: 'primary',
    3: 'warning',
    4: 'info',
    5: 'info',
    6: 'info',
    11: 'danger'
  };
  return typeMap[category] || 'info';
};

// 格式化延迟分钟数 - 处理负数和无效值
const formatDelayMinutes = (delay) => {
  // 检查是否为有效的数字
  if (delay === null || delay === undefined || isNaN(delay)) {
    return '-';
  }
  
  const delayNum = Number(delay);
  
  // 处理负数情况（表示提前完成）
  if (delayNum < 0) {
    return `提前 ${Math.abs(delayNum)} 分钟`;
  }
  
  // 处理零值
  if (delayNum === 0) {
    return '按时';
  }
  
  // 处理正数（延迟）
  return `延迟 ${delayNum} 分钟`;
};

// 格式化允许延迟分钟数
const formatAllowedDelayMinutes = (delay) => {
  if (delay === null || delay === undefined || isNaN(delay)) {
    return '-';
  }
  
  const delayNum = Number(delay);
  if (delayNum <= 0) {
    return '不允许延迟';
  }
  
  return `${delayNum} 分钟`;
};
</script>

<style scoped>
.task-detail-dialog {
  --el-dialog-border-radius: 12px;
}

.task-detail-content {
  max-height: 600px;
  overflow-y: auto;
  padding: 0 8px;
  animation: slideDown 0.3s ease-out;
}

.preparation-items {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.preparation-items .el-tag {
  margin-bottom: 4px;
  cursor: default;
  transition: all 0.2s ease;
}

.preparation-items .el-tag:hover {
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.15);
}

.task-description {
  margin: 0;
  line-height: 1.6;
  color: #606266;
  font-size: 14px;
}

.json-display {
  background: linear-gradient(135deg, #f5f7fa, #f0f2f5);
  padding: 12px;
  border-radius: 6px;
  font-size: 12px;
  line-height: 1.5;
  max-height: 300px;
  overflow-y: auto;
  margin: 0;
  border: 1px solid #ebeef5;
}

:deep(.el-descriptions__cell) {
  padding: 14px 16px;
}

:deep(.el-descriptions__label.has-colon::after) {
  content: '';
}

:deep(.el-descriptions__label) {
  font-weight: 600;
  color: #303133;
  width: 100px;
  white-space: nowrap;
  flex-shrink: 0;
}

:deep(.el-descriptions__content) {
  width: 100px;
  color: #606266;
  word-break: break-word;
  word-wrap: break-word;
  white-space: normal;
}

:deep(.el-tag) {
  border-radius: 4px;
}

@keyframes slideDown {
  from {
    opacity: 0;
    transform: translateY(-10px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}
</style>
