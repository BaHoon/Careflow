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
          <el-descriptions-item label="计划开始时间" :span="2">
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
          <el-descriptions-item v-if="currentTask.assignedNurseName" label="负责护士">
            {{ currentTask.assignedNurseName }}
          </el-descriptions-item>
          <el-descriptions-item v-if="currentTask.executorNurseName" label="执行护士">
            {{ currentTask.executorNurseName }}
          </el-descriptions-item>
          
          <!-- 延迟信息 -->
          <el-descriptions-item v-if="currentTask.delayMinutes !== undefined && currentTask.delayMinutes !== null" label="延迟分钟数">
            {{ currentTask.delayMinutes }} 分钟
          </el-descriptions-item>
          <el-descriptions-item v-if="currentTask.allowedDelayMinutes !== undefined && currentTask.allowedDelayMinutes !== null" label="允许延迟">
            {{ currentTask.allowedDelayMinutes }} 分钟
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
            <el-tag type="primary" size="large">{{ getOperationName(currentTask) }}</el-tag>
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
          <el-descriptions-item label="计划开始时间" :span="2">
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
          <el-descriptions-item v-if="currentTask.assignedNurseName" label="负责护士">
            {{ currentTask.assignedNurseName }}
          </el-descriptions-item>
          <el-descriptions-item v-if="currentTask.executorNurseName" label="执行护士">
            {{ currentTask.executorNurseName }}
          </el-descriptions-item>
          
          <!-- 延迟信息 -->
          <el-descriptions-item v-if="currentTask.delayMinutes !== undefined && currentTask.delayMinutes !== null" label="延迟分钟数">
            {{ currentTask.delayMinutes }} 分钟
          </el-descriptions-item>
          <el-descriptions-item v-if="currentTask.allowedDelayMinutes !== undefined && currentTask.allowedDelayMinutes !== null" label="允许延迟">
            {{ currentTask.allowedDelayMinutes }} 分钟
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

// 格式化JSON
const formatJson = (jsonString) => {
  if (!jsonString) return '';
  try {
    const obj = typeof jsonString === 'string' ? JSON.parse(jsonString) : jsonString;
    return JSON.stringify(obj, null, 2);
  } catch (error) {
    return jsonString;
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

// 获取操作名称
const getOperationName = (task) => {
  if (task.dataPayload) {
    try {
      const payload = typeof task.dataPayload === 'string' 
        ? JSON.parse(task.dataPayload) 
        : task.dataPayload;
      return payload.OperationName || payload.Title || task.opId || '操作任务';
    } catch (e) {
      console.error('解析dataPayload失败:', e);
    }
  }
  return task.opId || '操作任务';
};

// 获取操作代码
const getOperationCode = (task) => {
  if (task.dataPayload) {
    try {
      const payload = typeof task.dataPayload === 'string' 
        ? JSON.parse(task.dataPayload) 
        : task.dataPayload;
      return payload.OpId || task.opId || '-';
    } catch (e) {
      console.error('解析dataPayload失败:', e);
    }
  }
  return task.opId || '-';
};

// 获取操作部位
const getOperationSite = (task) => {
  if (task.dataPayload) {
    try {
      const payload = typeof task.dataPayload === 'string' 
        ? JSON.parse(task.dataPayload) 
        : task.dataPayload;
      return payload.OperationSite || null;
    } catch (e) {
      console.error('解析dataPayload失败:', e);
    }
  }
  return null;
};

// 获取准备物品列表
const getPreparationItems = (task) => {
  if (task.dataPayload) {
    try {
      const payload = typeof task.dataPayload === 'string' 
        ? JSON.parse(task.dataPayload) 
        : task.dataPayload;
      return payload.PreparationItems || [];
    } catch (e) {
      console.error('解析dataPayload失败:', e);
    }
  }
  return [];
};

// 获取任务说明
const getTaskDescription = (task) => {
  if (task.dataPayload) {
    try {
      const payload = typeof task.dataPayload === 'string' 
        ? JSON.parse(task.dataPayload) 
        : task.dataPayload;
      return payload.Description || null;
    } catch (e) {
      console.error('解析dataPayload失败:', e);
    }
  }
  return null;
};

// 任务类别文本
const getCategoryText = (category) => {
  const textMap = {
    'Immediate': '即刻执行',
    'Duration': '持续任务',
    'ResultPending': '结果待定',
    'DataCollection': '数据采集',
    'Verification': '核对验证',
    'Routine': '常规护理',
    'ReMeasure': '复测任务'
  };
  return textMap[category] || category;
};

// 任务类别标签类型
const getCategoryTagType = (category) => {
  const typeMap = {
    'Immediate': 'success',
    'Duration': 'primary',
    'ResultPending': 'warning',
    'DataCollection': 'info',
    'Verification': '',
    'Routine': 'info',
    'ReMeasure': 'warning'
  };
  return typeMap[category] || '';
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
}

:deep(.el-descriptions__content) {
  color: #606266;
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
