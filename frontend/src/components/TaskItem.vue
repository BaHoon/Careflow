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
      <!-- 未完成且未取消的任务显示开始录入按钮 -->
      <el-button 
        v-if="task.status !== 'Completed' && task.status !== 5 && task.status !== 'Cancelled' && task.status !== 9" 
        type="primary" 
        size="small"
        :icon="Edit"
        @click.stop="handleStartInput"
      >
        开始录入
      </el-button>
      <!-- 未完成且未取消的任务显示取消按钮 -->
      <el-button 
        v-if="task.status === 'Pending' || task.status === 3" 
        type="danger" 
        plain
        size="small"
        :icon="Close"
        @click.stop="handleCancelTask"
      >
        取消任务
      </el-button>
      <!-- 已完成的任务显示查看详情按钮 -->
      <el-button 
        v-if="task.status === 'Completed' || task.status === 5" 
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
  Bell,
  Edit,
  Close
} from '@element-plus/icons-vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { cancelNursingTask } from '@/api/nursing';

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

const emit = defineEmits(['click', 'start-input', 'view-detail', 'task-cancelled']);

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
  const status = props.task.status;
  const typeMap = {
    'Pending': 'warning',
    3: 'warning',
    'Running': 'primary',
    4: 'primary',
    'Completed': 'success',
    5: 'success',
    'Skipped': 'info',
    8: 'info',
    'Cancelled': 'danger',
    9: 'danger',
    'Stopped': 'danger',
    7: 'danger'
  };
  return typeMap[status] || 'info';
});

// 状态文本
const statusText = computed(() => {
  const status = props.task.status;
  const textMap = {
    'Pending': '待执行',
    3: '待执行',
    'Running': '执行中',
    4: '执行中',
    'Completed': '已完成',
    5: '已完成',
    'Skipped': '已跳过',
    8: '已跳过',
    'Cancelled': '已取消',
    9: '已取消',
    'Stopped': '已停止',
    7: '已停止'
  };
  return textMap[status] || status;
});

// 格式化时间
const formatTime = (dateString) => {
  if (!dateString) return '';
  try {
    // 确保UTC时间字符串带有Z标识
    let utcString = dateString;
    if (!dateString.endsWith('Z') && !dateString.includes('+')) {
      utcString = dateString + 'Z';
    }
    const date = new Date(utcString);
    return date.toLocaleString('zh-CN', {
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit',
      timeZone: 'Asia/Shanghai'
    });
  } catch {
    return dateString;
  }
};

// 事件处理
const handleClick = () => {
  emit('click', props.task);
};

const handleStartInput = () => {
  emit('start-input', props.task);
};

const handleViewDetail = () => {
  emit('view-detail', props.task);
};

// 获取当前护士ID
const getCurrentNurseId = () => {
  const userInfo = localStorage.getItem('userInfo');
  if (userInfo) {
    const user = JSON.parse(userInfo);
    return user.staffId;
  }
  return null;
};

// 取消任务
const handleCancelTask = async () => {
  try {
    // 弹出输入框要求填写取消理由
    const { value: cancelReason } = await ElMessageBox.prompt(
      '请填写取消任务的理由',
      '确认取消',
      {
        confirmButtonText: '确定',
        cancelButtonText: '取消',
        inputType: 'textarea',
        inputPlaceholder: '请输入取消理由...',
        inputValidator: (value) => {
          if (!value || value.trim().length === 0) {
            return '取消理由不能为空';
          }
          return true;
        },
      }
    );

    const nurseId = getCurrentNurseId();
    if (!nurseId) {
      ElMessage.error('未找到护士信息');
      return;
    }

    // 验证taskId
    const taskId = props.task.id;
    console.log('取消任务 - taskId:', taskId, 'task对象:', props.task, '理由:', cancelReason);
    
    if (!taskId) {
      ElMessage.error('任务ID无效');
      return;
    }

    // 调用API取消任务
    await cancelNursingTask(taskId, nurseId, cancelReason);
    ElMessage.success('任务已取消');
    
    // 通知父组件刷新数据
    emit('task-cancelled', taskId);
  } catch (error) {
    if (error !== 'cancel') {
      console.error('取消任务失败:', error);
      ElMessage.error(error.response?.data?.message || '取消任务失败');
    }
  }
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
