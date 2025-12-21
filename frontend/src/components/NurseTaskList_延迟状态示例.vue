// 护理任务列表组件 - 延迟状态显示示例

<template>
  <div class="task-list">
    <!-- 严重超时任务 -->
    <div v-if="severeTasksCount > 0" class="task-section severe">
      <div class="section-header">
        <span class="icon">🚨</span>
        <span class="title">严重超时 ({{ severeTasksCount }})</span>
      </div>
      <div v-for="task in severeTasks" :key="task.id" class="task-card severe-card">
        <div class="task-title">{{ task.category }}</div>
        <div class="task-patient">
          <span class="patient-name">{{ task.patientName }}</span>
          <span class="bed-id">{{ task.bedId }}</span>
        </div>
        <div class="task-time">
          <span>计划时间：{{ formatTime(task.plannedStartTime) }}</span>
          <span class="delay-badge severe">
            超出正常范围 {{ task.excessDelayMinutes }}分钟
          </span>
        </div>
        <div class="task-actions">
          <button @click="startTask(task)" class="btn-execute">开始执行</button>
          <button @click="viewDetail(task)" class="btn-detail">查看详情</button>
        </div>
      </div>
    </div>

    <!-- 轻度超时任务 -->
    <div v-if="warningTasksCount > 0" class="task-section warning">
      <div class="section-header">
        <span class="icon">⚠️</span>
        <span class="title">超时提醒 ({{ warningTasksCount }})</span>
      </div>
      <div v-for="task in warningTasks" :key="task.id" class="task-card warning-card">
        <div class="task-title">{{ task.category }}</div>
        <div class="task-patient">
          <span class="patient-name">{{ task.patientName }}</span>
          <span class="bed-id">{{ task.bedId }}</span>
        </div>
        <div class="task-time">
          <span>计划时间：{{ formatTime(task.plannedStartTime) }}</span>
          <span class="delay-badge warning">
            超出正常范围 {{ task.excessDelayMinutes }}分钟
          </span>
        </div>
        <div class="task-actions">
          <button @click="startTask(task)" class="btn-execute">开始执行</button>
          <button @click="viewDetail(task)" class="btn-detail">查看详情</button>
        </div>
      </div>
    </div>

    <!-- 正常延迟任务（容忍期内） -->
    <div v-if="normalDelayedTasksCount > 0" class="task-section normal-delayed">
      <div class="section-header">
        <span class="icon">⏰</span>
        <span class="title">正在处理中 ({{ normalDelayedTasksCount }})</span>
      </div>
      <div v-for="task in normalDelayedTasks" :key="task.id" class="task-card normal-card">
        <div class="task-title">{{ task.category }}</div>
        <div class="task-patient">
          <span class="patient-name">{{ task.patientName }}</span>
          <span class="bed-id">{{ task.bedId }}</span>
        </div>
        <div class="task-time">
          <span>计划时间：{{ formatTime(task.plannedStartTime) }}</span>
          <span class="delay-badge normal">
            延迟 {{ task.delayMinutes }}分钟（正常范围内，允许{{ task.allowedDelayMinutes }}分钟）
          </span>
        </div>
        <div class="task-actions">
          <button @click="startTask(task)" class="btn-execute">开始执行</button>
          <button @click="viewDetail(task)" class="btn-detail">查看详情</button>
        </div>
      </div>
    </div>

    <!-- 即将到期任务 -->
    <div v-if="upcomingTasksCount > 0" class="task-section upcoming">
      <div class="section-header">
        <span class="icon">🔔</span>
        <span class="title">即将到期 ({{ upcomingTasksCount }})</span>
      </div>
      <div v-for="task in upcomingTasks" :key="task.id" class="task-card upcoming-card">
        <div class="task-title">{{ task.category }}</div>
        <div class="task-patient">
          <span class="patient-name">{{ task.patientName }}</span>
          <span class="bed-id">{{ task.bedId }}</span>
        </div>
        <div class="task-time">
          <span>计划时间：{{ formatTime(task.plannedStartTime) }}</span>
          <span class="delay-badge upcoming">
            {{ Math.abs(task.delayMinutes) }}分钟后到期
          </span>
        </div>
        <div class="task-actions">
          <button @click="startTask(task)" class="btn-execute">开始执行</button>
          <button @click="viewDetail(task)" class="btn-detail">查看详情</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue';
import { ElMessage } from 'element-plus';

const props = defineProps({
  tasks: {
    type: Array,
    default: () => []
  }
});

// 任务分类
const severeTasks = computed(() => 
  props.tasks.filter(t => t.status === 'Pending' && t.severityLevel === 'Severe')
);

const warningTasks = computed(() => 
  props.tasks.filter(t => t.status === 'Pending' && t.severityLevel === 'Warning')
);

const normalDelayedTasks = computed(() => 
  props.tasks.filter(t => 
    t.status === 'Pending' && 
    t.severityLevel === 'Normal' && 
    t.delayMinutes > 0
  )
);

const upcomingTasks = computed(() => 
  props.tasks.filter(t => 
    t.status === 'Pending' && 
    t.isDueSoon && 
    t.delayMinutes < 0
  )
);

// 统计数量
const severeTasksCount = computed(() => severeTasks.value.length);
const warningTasksCount = computed(() => warningTasks.value.length);
const normalDelayedTasksCount = computed(() => normalDelayedTasks.value.length);
const upcomingTasksCount = computed(() => upcomingTasks.value.length);

// 工具函数
const formatTime = (datetime) => {
  if (!datetime) return '';
  const date = new Date(datetime);
  const month = date.getMonth() + 1;
  const day = date.getDate();
  const hours = String(date.getHours()).padStart(2, '0');
  const minutes = String(date.getMinutes()).padStart(2, '0');
  return `${month}/${day} ${hours}:${minutes}`;
};

const startTask = (task) => {
  // 执行任务逻辑
  ElMessage.success('开始执行任务');
};

const viewDetail = (task) => {
  // 查看详情逻辑
  console.log('查看任务详情:', task);
};
</script>

<style scoped>
.task-list {
  padding: 20px;
}

.task-section {
  margin-bottom: 30px;
}

.section-header {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 12px 16px;
  border-radius: 8px;
  font-weight: 600;
  font-size: 16px;
  margin-bottom: 12px;
}

.task-section.severe .section-header {
  background: #fee;
  color: #c00;
  border-left: 4px solid #c00;
}

.task-section.warning .section-header {
  background: #fff3e0;
  color: #e65100;
  border-left: 4px solid #ff9800;
}

.task-section.normal-delayed .section-header {
  background: #e3f2fd;
  color: #1976d2;
  border-left: 4px solid #2196f3;
}

.task-section.upcoming .section-header {
  background: #f3e5f5;
  color: #6a1b9a;
  border-left: 4px solid #9c27b0;
}

.task-card {
  background: white;
  border: 1px solid #e0e0e0;
  border-radius: 8px;
  padding: 16px;
  margin-bottom: 12px;
  transition: all 0.3s;
}

.task-card:hover {
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
  transform: translateY(-2px);
}

.severe-card {
  border-left: 4px solid #f44336;
}

.warning-card {
  border-left: 4px solid #ff9800;
}

.normal-card {
  border-left: 4px solid #2196f3;
}

.upcoming-card {
  border-left: 4px solid #9c27b0;
}

.task-title {
  font-size: 16px;
  font-weight: 600;
  margin-bottom: 8px;
  color: #333;
}

.task-patient {
  display: flex;
  gap: 12px;
  margin-bottom: 12px;
  font-size: 14px;
}

.patient-name {
  font-weight: 500;
  color: #555;
}

.bed-id {
  color: #999;
}

.task-time {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 16px;
  font-size: 14px;
}

.delay-badge {
  padding: 4px 12px;
  border-radius: 12px;
  font-size: 12px;
  font-weight: 500;
}

.delay-badge.severe {
  background: #ffebee;
  color: #c62828;
}

.delay-badge.warning {
  background: #fff3e0;
  color: #e65100;
}

.delay-badge.normal {
  background: #e3f2fd;
  color: #1976d2;
}

.delay-badge.upcoming {
  background: #f3e5f5;
  color: #6a1b9a;
}

.task-actions {
  display: flex;
  gap: 12px;
}

.btn-execute,
.btn-detail {
  flex: 1;
  padding: 10px;
  border: none;
  border-radius: 6px;
  cursor: pointer;
  font-size: 14px;
  font-weight: 500;
  transition: all 0.3s;
}

.btn-execute {
  background: #2196f3;
  color: white;
}

.btn-execute:hover {
  background: #1976d2;
}

.btn-detail {
  background: #f5f5f5;
  color: #666;
}

.btn-detail:hover {
  background: #e0e0e0;
}
</style>
