<template>
  <div 
    class="task-item" 
    :class="{ 
      'task-highlight': highlight,
      'task-overdue': task.excessDelayMinutes > 0 && task.status !== 'Completed',
      'task-due-soon': task.status === 'Pending' && task.delayMinutes >= -60 && task.excessDelayMinutes <= 0
    }"
    @click="handleClick"
  >
    <div class="task-header">
      <div class="task-title">
        <el-icon :size="18" class="task-icon">
          <component :is="categoryIcon" />
        </el-icon>
        <span class="task-category">{{ categoryText }}</span>
      </div>
      <el-tag :type="statusTagType" size="small">{{ statusText }}</el-tag>
    </div>

    <div class="task-content">
      <div class="task-patient">
        <el-icon><User /></el-icon>
        <span>{{ task.patientName }}</span>
        <el-tag size="small" type="info">{{ task.bedId }}</el-tag>
      </div>

      <div class="task-time">
        <el-icon><Clock /></el-icon>
        <span>计划时间：{{ formatTime(task.plannedStartTime) }}</span>
        <!-- 只在超时任务和临期任务显示延迟信息 -->
        <span v-if="task.excessDelayMinutes > 0 && task.status !== 'Completed'" class="overdue-text">
          (超出容忍期 {{ task.excessDelayMinutes }}分钟)
        </span>
        <span v-else-if="task.delayMinutes > 0 && task.delayMinutes >= -60 && task.status === 'Pending'" class="delay-text">
          (延迟 {{ task.delayMinutes }}分钟，容忍期内)
        </span>
        <span v-else-if="task.delayMinutes < 0 && task.delayMinutes >= -60 && task.status === 'Pending'" class="due-soon-text">
          (还有 {{ Math.abs(task.delayMinutes) }}分钟)
        </span>
      </div>

      <div v-if="task.actualStartTime" class="task-time">
        <el-icon><Check /></el-icon>
        <span>开始时间：{{ formatTime(task.actualStartTime) }}</span>
      </div>

      <div v-if="task.actualEndTime" class="task-time">
        <el-icon><CircleCheck /></el-icon>
        <span>完成时间：{{ formatTime(task.actualEndTime) }}</span>
      </div>
    </div>

    <div class="task-actions">
      <el-button 
        v-if="task.status === 'Pending'" 
        type="primary" 
        size="small"
        @click.stop="handleStart"
      >
        开始执行
      </el-button>
      <el-button 
        v-if="task.status === 'Running'" 
        type="success" 
        size="small"
        @click.stop="handleComplete"
      >
        完成任务
      </el-button>
      <el-button 
        size="small"
        @click.stop="handleViewDetail"
      >
        查看详情
      </el-button>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue';
import {
  User,
  Clock,
  Check,
  CircleCheck,
  Coffee,
  Document,
  VideoCamera,
  Bell
} from '@element-plus/icons-vue';

const props = defineProps({
  task: {
    type: Object,
    required: true
  },
  highlight: {
    type: Boolean,
    default: false
  }
});

const emit = defineEmits(['click', 'start', 'complete', 'view-detail']);

// 任务类别图标
const categoryIcon = computed(() => {
  const iconMap = {
    'Immediate': Coffee,
    'Duration': Coffee,
    'ResultPending': Document,
    'DataCollection': Bell,
    'Verification': Check
  };
  return iconMap[props.task.category] || Document;
});

// 任务类别文本
const categoryText = computed(() => {
  const textMap = {
    'Immediate': '即刻执行',
    'Duration': '持续任务',
    'ResultPending': '结果待定',
    'DataCollection': '数据采集',
    'Verification': '核对验证'
  };
  return textMap[props.task.category] || props.task.category;
});

// 状态标签类型
const statusTagType = computed(() => {
  const typeMap = {
    'Pending': 'warning',
    'Running': 'primary',
    'Completed': 'success',
    'Skipped': 'info',
    'Cancelled': 'danger'
  };
  return typeMap[props.task.status] || 'info';
});

// 状态文本
const statusText = computed(() => {
  const textMap = {
    'Pending': '待执行',
    'Running': '执行中',
    'Completed': '已完成',
    'Skipped': '已跳过',
    'Cancelled': '已取消'
  };
  return textMap[props.task.status] || props.task.status;
});

// 格式化时间
const formatTime = (dateString) => {
  if (!dateString) return '';
  const date = new Date(dateString);
  return date.toLocaleString('zh-CN', {
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit'
  });
};

// 事件处理
const handleClick = () => {
  emit('click', props.task);
};

const handleStart = () => {
  emit('start', props.task);
};

const handleComplete = () => {
  emit('complete', props.task);
};

const handleViewDetail = () => {
  emit('view-detail', props.task);
};
</script>

<style scoped>
.task-item {
  background: #fff;
  border: 1px solid #ebeef5;
  border-radius: 8px;
  padding: 16px;
  cursor: pointer;
  transition: all 0.3s ease;
}

.task-item:hover {
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.1);
  transform: translateX(4px);
}

.task-highlight {
  border-width: 2px;
}

.task-overdue {
  border-color: #f56c6c;
  background: #fef0f0;
}

.task-due-soon {
  border-color: #e6a23c;
  background: #fdf6ec;
}

.task-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 12px;
}

.task-title {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 16px;
  font-weight: 600;
}

.task-icon {
  color: #409eff;
}

.task-content {
  margin-bottom: 12px;
}

.task-patient,
.task-time {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 8px;
  font-size: 14px;
  color: #606266;
}

.task-patient .el-icon,
.task-time .el-icon {
  color: #909399;
}

.overdue-text {
  color: #f56c6c;
  font-weight: 600;
}

.delay-text {
  color: #e6a23c;
  font-weight: 500;
}

.due-soon-text {
  color: #409eff;
  font-weight: 600;
}

.task-actions {
  display: flex;
  justify-content: flex-end;
  gap: 8px;
  padding-top: 12px;
  border-top: 1px solid #ebeef5;
}
</style>
