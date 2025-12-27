<template>
  <div class="task-timeline">
    <!-- 任务统计卡片 -->
    <div class="timeline-header">
      <el-row :gutter="16">
        <el-col :span="6">
          <el-card shadow="hover">
            <div class="stat-item">
              <div class="stat-value overdue">{{ statistics.overdueCount }}</div>
              <div class="stat-label">超时任务</div>
            </div>
          </el-card>
        </el-col>
        <el-col :span="6">
          <el-card shadow="hover">
            <div class="stat-item">
              <div class="stat-value due-soon">{{ statistics.dueSoonCount }}</div>
              <div class="stat-label">临期任务</div>
            </div>
          </el-card>
        </el-col>
        <el-col :span="6">
          <el-card shadow="hover">
            <div class="stat-item">
              <div class="stat-value pending">{{ statistics.pendingCount }}</div>
              <div class="stat-label">待执行</div>
            </div>
          </el-card>
        </el-col>
        <el-col :span="6">
          <el-card shadow="hover">
            <div class="stat-item">
              <div class="stat-value completed">{{ statistics.completedCount }}</div>
              <div class="stat-label">已完成</div>
            </div>
          </el-card>
        </el-col>
      </el-row>
    </div>

    <!-- 时间轴 -->
    <div class="timeline-content">
      <el-timeline>
        <!-- 超时任务组 -->
        <el-timeline-item
          v-if="groupedTasks.overdue.length > 0"
          color="#f56c6c"
          size="large"
        >
          <template #dot>
            <el-icon :size="20"><WarningFilled /></el-icon>
          </template>
          <div class="timeline-group">
            <div class="group-header overdue-header">
              <h3>超时任务 ({{ groupedTasks.overdue.length }})</h3>
            </div>
            <div class="task-list">
              <TaskItem
                v-for="task in groupedTasks.overdue"
                :key="task.id"
                :task="task"
                :highlight="true"
                :is-overdue="true"
                @click="handleTaskClick"
                @start-input="handleStartInput"
                @view-detail="handleViewDetail"
                @task-cancelled="handleTaskCancelled"
                @print-inspection-guide="handlePrintInspectionGuide"
              />
            </div>
          </div>
        </el-timeline-item>

        <!-- 临期任务组 -->
        <el-timeline-item
          v-if="groupedTasks.dueSoon.length > 0"
          color="#e6a23c"
          size="large"
        >
          <template #dot>
            <el-icon :size="20"><Clock /></el-icon>
          </template>
          <div class="timeline-group">
            <div class="group-header due-soon-header">
              <h3>临期任务  ({{ groupedTasks.dueSoon.length }})</h3>
            </div>
            <div class="task-list">
              <TaskItem
                v-for="task in groupedTasks.dueSoon"
                :key="task.id"
                :task="task"
                :highlight="true"
                :is-due-soon="true"
                @click="handleTaskClick"
                @start-input="handleStartInput"
                @view-detail="handleViewDetail"
                @task-cancelled="handleTaskCancelled"
                @print-inspection-guide="handlePrintInspectionGuide"
              />
            </div>
          </div>
        </el-timeline-item>

        <!-- 待执行任务组 -->
        <el-timeline-item
          v-if="groupedTasks.pending.length > 0"
          color="#409eff"
          size="large"
        >
          <template #dot>
            <el-icon :size="20"><List /></el-icon>
          </template>
          <div class="timeline-group">
            <div class="group-header pending-header">
              <h3>待执行任务 ({{ groupedTasks.pending.length }})</h3>
            </div>
            <div class="task-list">
              <TaskItem
                v-for="task in groupedTasks.pending"
                :key="task.id"
                :task="task"
                @click="handleTaskClick"
                @start-input="handleStartInput"
                @view-detail="handleViewDetail"
                @task-cancelled="handleTaskCancelled"
                @print-inspection-guide="handlePrintInspectionGuide"
              />
            </div>
          </div>
        </el-timeline-item>

        <!-- 已完成任务组 -->
        <el-timeline-item
          v-if="groupedTasks.completed.length > 0"
          color="#67c23a"
          size="large"
        >
          <template #dot>
            <el-icon :size="20"><Check /></el-icon>
          </template>
          <div class="timeline-group">
            <div class="group-header completed-header">
              <h3>已完成任务 ({{ groupedTasks.completed.length }})</h3>
            </div>
            <div class="task-list">
              <TaskItem
                v-for="task in groupedTasks.completed"
                :key="task.id"
                :task="task"
                @click="handleTaskClick"
                @start-input="handleStartInput"
                @view-detail="handleViewDetail"
                @task-cancelled="handleTaskCancelled"
                @print-inspection-guide="handlePrintInspectionGuide"
              />
            </div>
          </div>
        </el-timeline-item>
      </el-timeline>

      <!-- 空状态 -->
      <el-empty
        v-if="tasks.length === 0"
        description="暂无任务"
      />
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue';
import { WarningFilled, Clock, List, Check } from '@element-plus/icons-vue';
import TaskItem from './TaskItem.vue';

const props = defineProps({
  tasks: {
    type: Array,
    default: () => []
  }
});

const emit = defineEmits(['task-click', 'start-input', 'view-detail', 'task-cancelled', 'print-inspection-guide']);

// 任务统计
const statistics = computed(() => {
  return {
    // 超时任务：超出容忍期的未完成且未取消的任务
    overdueCount: props.tasks.filter(t => 
      t.excessDelayMinutes > 0 && 
      t.status !== 5 && 
      t.status !== 'Completed' &&
      t.status !== 9 && 
      t.status !== 'Cancelled'
    ).length,
    // 临期任务：前一小时到容忍期内的待执行任务（Pending状态）
    dueSoonCount: props.tasks.filter(t => 
      (t.status === 3 || t.status === 'Pending') &&
      t.delayMinutes >= -60 && 
      t.excessDelayMinutes <= 0
    ).length,
    // 待执行任务：包括 AppliedConfirmed、Pending、InProgress，且没有超出容忍期
    pendingCount: props.tasks.filter(t => 
      (t.status === 2 || t.status === 'AppliedConfirmed' ||
       t.status === 3 || t.status === 'Pending' ||
       t.status === 4 || t.status === 'InProgress') &&
      t.excessDelayMinutes <= 0
    ).length,
    // 已完成任务
    completedCount: props.tasks.filter(t => 
      t.status === 5 || t.status === 'Completed'
    ).length
  };
});

// 任务分组
const groupedTasks = computed(() => {
  return {
    // 超时任务：超出容忍期的未完成且未取消的任务
    overdue: props.tasks.filter(t => 
      t.excessDelayMinutes > 0 && 
      t.status !== 5 && 
      t.status !== 'Completed' &&
      t.status !== 9 && 
      t.status !== 'Cancelled'
    ),
    // 临期任务：前一小时到容忍期内的待执行任务（Pending状态）
    // 注意：applying、applied状态不放在临期组，因为它们不应该有时间压力提示
    dueSoon: props.tasks.filter(t => 
      (t.status === 3 || t.status === 'Pending') &&
      t.delayMinutes >= -60 && 
      t.excessDelayMinutes <= 0
    ),
    // 待执行任务：还没到前一小时的任务（包括 Applying、Applied、AppliedConfirmed、Pending、InProgress）
    pending: props.tasks.filter(t => {
      // Applying(0)、Applied(1)、AppliedConfirmed(2) - 药房申请相关状态，都属于待执行
      if ((t.status === 0 || t.status === 'Applying' ||
          t.status === 1 || t.status === 'Applied' ||
          t.status === 2 || t.status === 'AppliedConfirmed'||
          t.status === 3 || t.status === 'Pending' ||
          t.status === 4 || t.status === 'InProgress') &&
          t.excessDelayMinutes <= 0 &&
          !(t.status === 3 || t.status === 'Pending')) {
        return true;
      }
      return false;
    }),
    // 已完成任务
    completed: props.tasks.filter(t => 
      t.status === 5 || t.status === 'Completed'
    )
  };
});

// 任务点击事件
const handleTaskClick = (task) => {
  emit('task-click', task);
};

// 开始录入事件
const handleStartInput = (task) => {
  emit('start-input', task);
};

// 查看详情事件
const handleViewDetail = (task) => {
  emit('view-detail', task);
};

// 任务取消事件
const handleTaskCancelled = (taskId) => {
  emit('task-cancelled', taskId);
};

// 打印检查导引单事件
const handlePrintInspectionGuide = (data) => {
  emit('print-inspection-guide', data);
};
</script>

<style scoped>
.task-timeline {
  width: 100%;
}

.timeline-header {
  margin-bottom: 24px;
}

.stat-item {
  text-align: center;
  padding: 16px 0;
  transition: all 0.3s ease;
}

.stat-value {
  font-size: 40px;
  font-weight: 700;
  margin-bottom: 8px;
  letter-spacing: -0.5px;
}

.stat-value.overdue {
  color: #f56c6c;
}

.stat-value.due-soon {
  color: #e6a23c;
}

.stat-value.pending {
  color: #409eff;
}

.stat-value.completed {
  color: #67c23a;
}

.stat-label {
  font-size: 15px;
  color: #606266;
  font-weight: 500;
}

.timeline-content {
  margin-top: 20px;
}

.timeline-group {
  margin-bottom: 16px;
}

.group-header {
  padding: 14px 16px;
  border-radius: 8px;
  margin-bottom: 16px;
  display: flex;
  align-items: center;
  background: #f5f7fa;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.05);
}

.group-header h3 {
  margin: 0;
  font-size: 16px;
  font-weight: 600;
}

.overdue-header {
  background: #fef0f0;
  color: #f56c6c;
  border-left: 4px solid #f56c6c;
}

.due-soon-header {
  background: #fdf6ec;
  color: #e6a23c;
  border-left: 4px solid #e6a23c;
}

.pending-header {
  background: #ecf5ff;
  color: #409eff;
  border-left: 4px solid #409eff;
}

.completed-header {
  background: #f0f9ff;
  color: #67c23a;
  border-left: 4px solid #67c23a;
}

.task-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
  padding: 0 4px;
}
</style>
