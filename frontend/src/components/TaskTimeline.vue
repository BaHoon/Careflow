<template>
  <div class="task-timeline">
    <!-- 任务统计卡片 -->
    <div class="timeline-header">
      <el-row :gutter="16">
        <el-col :span="4">
          <el-card shadow="hover" class="stat-card overdue-card" @click="handleStatCardClick('overdue')">
            <div class="stat-item">
              <div class="stat-icon">
                <el-icon><WarningFilled /></el-icon>
              </div>
              <div class="stat-content">
                <div class="stat-value overdue">{{ statistics.overdueCount }}</div>
                <div class="stat-label">超时任务</div>
              </div>
            </div>
          </el-card>
        </el-col>
        <el-col :span="4">
          <el-card shadow="hover" class="stat-card in-progress-card" @click="handleStatCardClick('inProgress')">
            <div class="stat-item">
              <div class="stat-icon">
                <el-icon><Loading /></el-icon>
              </div>
              <div class="stat-content">
                <div class="stat-value in-progress">{{ statistics.inProgressCount }}</div>
                <div class="stat-label">执行中</div>
              </div>
            </div>
          </el-card>
        </el-col>
        <el-col :span="4">
          <el-card shadow="hover" class="stat-card due-soon-card" @click="handleStatCardClick('dueSoon')">
            <div class="stat-item">
              <div class="stat-icon">
                <el-icon><Clock /></el-icon>
              </div>
              <div class="stat-content">
                <div class="stat-value due-soon">{{ statistics.dueSoonCount }}</div>
                <div class="stat-label">临期任务</div>
              </div>
            </div>
          </el-card>
        </el-col>
        <el-col :span="4">
          <el-card shadow="hover" class="stat-card pending-card" @click="handleStatCardClick('pending')">
            <div class="stat-item">
              <div class="stat-icon">
                <el-icon><List /></el-icon>
              </div>
              <div class="stat-content">
                <div class="stat-value pending">{{ statistics.pendingCount }}</div>
                <div class="stat-label">待执行</div>
              </div>
            </div>
          </el-card>
        </el-col>
        <el-col :span="4">
          <el-card 
            shadow="hover" 
            class="stat-card pending-orders-card" 
            @click="handleNavigate('order-acknowledgement')"
          >
            <div class="stat-item">
              <div class="stat-icon">
                <el-icon><DocumentChecked /></el-icon>
              </div>
              <div class="stat-content">
                <div class="stat-value pending-orders">{{ pendingOrdersCount }}</div>
                <div class="stat-label">待签收医嘱</div>
              </div>
            </div>
          </el-card>
        </el-col>
        <el-col :span="4">
          <el-card 
            shadow="hover" 
            class="stat-card pending-returns-card" 
            @click="handleNavigate('order-application')"
          >
            <div class="stat-item">
              <div class="stat-icon">
                <el-icon><RefreshLeft /></el-icon>
              </div>
              <div class="stat-content">
                <div class="stat-value pending-returns">{{ pendingReturnsCount }}</div>
                <div class="stat-label">待退药申请</div>
              </div>
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

        <!-- 执行中任务组 -->
        <el-timeline-item
          v-if="groupedTasks.inProgress.length > 0"
          color="#13c2c2"
          size="large"
        >
          <template #dot>
            <el-icon :size="20"><Loading /></el-icon>
          </template>
          <div class="timeline-group">
            <div class="group-header in-progress-header">
              <h3>执行中任务 ({{ groupedTasks.inProgress.length }})</h3>
            </div>
            <div class="task-list">
              <TaskItem
                v-for="task in groupedTasks.inProgress"
                :key="task.id"
                :task="task"
                :highlight="true"
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
import { useRouter } from 'vue-router';
import { WarningFilled, Clock, List, Check, DocumentChecked, RefreshLeft, Loading } from '@element-plus/icons-vue';
import TaskItem from './TaskItem.vue';

const router = useRouter();

const props = defineProps({
  tasks: {
    type: Array,
    default: () => []
  },
  pendingOrdersCount: {
    type: Number,
    default: 0
  },
  pendingReturnsCount: {
    type: Number,
    default: 0
  }
});

const emit = defineEmits(['task-click', 'start-input', 'view-detail', 'task-cancelled', 'print-inspection-guide']);

// 统计卡片点击处理
const handleStatCardClick = (type) => {
  // 可以在这里实现滚动到对应任务组的功能
  console.log('点击统计卡片:', type);
};

// 导航到指定页面
const handleNavigate = (routeName) => {
  router.push({ name: routeName });
};

// 任务统计
const statistics = computed(() => {
  // 定义可执行状态（不包括InProgress）
  const isExecutableStatus = (status) => {
    return status === 0 || status === 'Applying' ||
           status === 1 || status === 'Applied' ||
           status === 2 || status === 'AppliedConfirmed' ||
           status === 3 || status === 'Pending';
  };

  return {
    // 超时任务：超出容忍期的可执行状态任务
    overdueCount: props.tasks.filter(t => 
      isExecutableStatus(t.status) &&
      t.excessDelayMinutes > 0
    ).length,
    // 执行中任务：InProgress状态的任务
    inProgressCount: props.tasks.filter(t => 
      t.status === 4 || t.status === 'InProgress'
    ).length,
    // 临期任务：前一小时到容忍期内的可执行状态任务
    dueSoonCount: props.tasks.filter(t => 
      isExecutableStatus(t.status) &&
      t.delayMinutes >= -60 && 
      t.excessDelayMinutes <= 0
    ).length,
    // 待执行任务：还没到前一小时的可执行状态任务
    pendingCount: props.tasks.filter(t => 
      isExecutableStatus(t.status) &&
      t.delayMinutes < -60 &&
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
  // 定义可执行状态（不包括InProgress）
  const isExecutableStatus = (status) => {
    return status === 0 || status === 'Applying' ||
           status === 1 || status === 'Applied' ||
           status === 2 || status === 'AppliedConfirmed' ||
           status === 3 || status === 'Pending';
  };

  return {
    // 超时任务：超出容忍期的可执行状态任务
    overdue: props.tasks.filter(t => 
      isExecutableStatus(t.status) &&
      t.excessDelayMinutes > 0
    ),
    // 执行中任务：InProgress状态的任务
    inProgress: props.tasks.filter(t => 
      t.status === 4 || t.status === 'InProgress'
    ),
    // 临期任务：前一小时到容忍期内的可执行状态任务
    // Applying、Applied、AppliedConfirmed、Pending 都按时间统一处理
    dueSoon: props.tasks.filter(t => 
      isExecutableStatus(t.status) &&
      t.delayMinutes >= -60 && 
      t.excessDelayMinutes <= 0
    ),
    // 待执行任务：还没到前一小时的可执行状态任务（不包括InProgress）
    pending: props.tasks.filter(t => 
      isExecutableStatus(t.status) &&
      t.delayMinutes < -60 &&
      t.excessDelayMinutes <= 0
    ),
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

/* 统计卡片基础样式 */
.stat-card {
  cursor: pointer;
  transition: all 0.3s ease;
  border-radius: 12px;
  overflow: hidden;
}

.stat-card:hover {
  transform: translateY(-4px);
  box-shadow: 0 8px 16px rgba(0, 0, 0, 0.15) !important;
}

.stat-item {
  display: flex;
  align-items: center;
  padding: 20px 16px;
  gap: 16px;
}

.stat-icon {
  flex-shrink: 0;
  width: 48px;
  height: 48px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 12px;
  font-size: 24px;
  transition: all 0.3s ease;
}

.stat-content {
  flex: 1;
  min-width: 0;
}

.stat-value {
  font-size: 32px;
  font-weight: 700;
  margin-bottom: 4px;
  letter-spacing: -0.5px;
  line-height: 1;
}

.stat-label {
  font-size: 13px;
  color: #909399;
  font-weight: 500;
  white-space: nowrap;
}

/* 超时任务卡片 */
.overdue-card .stat-icon {
  background: linear-gradient(135deg, #fef0f0 0%, #fde2e2 100%);
  color: #f56c6c;
}

.overdue-card:hover .stat-icon {
  background: linear-gradient(135deg, #fde2e2 0%, #f56c6c 100%);
  color: white;
}

.stat-value.overdue {
  color: #f56c6c;
}

/* 执行中任务卡片 */
.in-progress-card .stat-icon {
  background: linear-gradient(135deg, #e6fffb 0%, #b5f5ec 100%);
  color: #13c2c2;
}

.in-progress-card:hover .stat-icon {
  background: linear-gradient(135deg, #b5f5ec 0%, #13c2c2 100%);
  color: white;
}

.stat-value.in-progress {
  color: #13c2c2;
}

/* 临期任务卡片 */
.due-soon-card .stat-icon {
  background: linear-gradient(135deg, #fdf6ec 0%, #faecd8 100%);
  color: #e6a23c;
}

.due-soon-card:hover .stat-icon {
  background: linear-gradient(135deg, #faecd8 0%, #e6a23c 100%);
  color: white;
}

.stat-value.due-soon {
  color: #e6a23c;
}

/* 待执行任务卡片 */
.pending-card .stat-icon {
  background: linear-gradient(135deg, #ecf5ff 0%, #d9ecff 100%);
  color: #409eff;
}

.pending-card:hover .stat-icon {
  background: linear-gradient(135deg, #d9ecff 0%, #409eff 100%);
  color: white;
}

.stat-value.pending {
  color: #409eff;
}

/* 已完成任务卡片 */
.completed-card .stat-icon {
  background: linear-gradient(135deg, #f0f9ff 0%, #e1f3d8 100%);
  color: #67c23a;
}

.completed-card:hover .stat-icon {
  background: linear-gradient(135deg, #e1f3d8 0%, #67c23a 100%);
  color: white;
}

.stat-value.completed {
  color: #67c23a;
}

/* 待签收医嘱卡片 */
.pending-orders-card .stat-icon {
  background: linear-gradient(135deg, #f3e5f5 0%, #e1bee7 100%);
  color: #9c27b0;
}

.pending-orders-card:hover .stat-icon {
  background: linear-gradient(135deg, #e1bee7 0%, #9c27b0 100%);
  color: white;
}

.stat-value.pending-orders {
  color: #9c27b0;
}

/* 待退药申请卡片 */
.pending-returns-card .stat-icon {
  background: linear-gradient(135deg, #fbe9e7 0%, #ffccbc 100%);
  color: #ff5722;
}

.pending-returns-card:hover .stat-icon {
  background: linear-gradient(135deg, #ffccbc 0%, #ff5722 100%);
  color: white;
}

.stat-value.pending-returns {
  color: #ff5722;
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

.in-progress-header {
  background: #e6fffb;
  color: #13c2c2;
  border-left: 4px solid #13c2c2;
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
