<template>
  <div class="nurse-task-list">
    <!-- 顶部操作栏 -->
    <div class="task-header">
      <div class="header-left">
        <h2>我的任务</h2>
        <el-breadcrumb separator="/">
          <el-breadcrumb-item>护士工作台</el-breadcrumb-item>
          <el-breadcrumb-item>我的任务</el-breadcrumb-item>
        </el-breadcrumb>
      </div>
      <div class="header-right">
        <el-date-picker
          v-model="selectedDate"
          type="date"
          placeholder="选择日期"
          style="width: 180px; margin-right: 12px"
          @change="loadTasks"
        />
        <el-select
          v-model="selectedPatient"
          placeholder="筛选病人"
          style="width: 160px; margin-right: 12px"
          clearable
          filterable
          @change="filterTasks"
        >
          <el-option
            v-for="patient in patientList"
            :key="patient.id"
            :label="patient.name + ' (' + patient.bedId + ')'"
            :value="patient.id"
          />
        </el-select>
        <el-select
          v-model="selectedStatus"
          placeholder="任务状态"
          style="width: 140px; margin-right: 12px"
          clearable
          @change="filterTasks"
        >
          <el-option label="全部" value="" />
          <el-option label="待执行" value="Pending" />
          <el-option label="执行中" value="Running" />
          <el-option label="已完成" value="Completed" />
        </el-select>
        <el-button type="primary" :icon="Refresh" @click="loadTasks">刷新</el-button>
        <el-button :icon="Operation" @click="goToDashboard">床位概览</el-button>
      </div>
    </div>

    <!-- 任务时间轴 -->
    <div class="task-content" v-loading="loading">
      <TaskTimeline
        :tasks="filteredTasks"
        @task-click="handleTaskClick"
        @start="handleTaskStart"
        @complete="handleTaskComplete"
        @view-detail="handleViewDetail"
      />
    </div>

    <!-- 任务详情对话框 -->
    <el-dialog
      v-model="detailDialogVisible"
      title="任务详情"
      width="600px"
    >
      <div v-if="currentTask" class="task-detail">
        <el-descriptions :column="2" border>
          <el-descriptions-item label="任务ID">
            {{ currentTask.id }}
          </el-descriptions-item>
          <el-descriptions-item label="医嘱ID">
            {{ currentTask.medicalOrderId }}
          </el-descriptions-item>
          <el-descriptions-item label="患者姓名">
            {{ currentTask.patientName }}
          </el-descriptions-item>
          <el-descriptions-item label="床号">
            {{ currentTask.bedId }}
          </el-descriptions-item>
          <el-descriptions-item label="任务类别">
            {{ currentTask.category }}
          </el-descriptions-item>
          <el-descriptions-item label="任务状态">
            <el-tag :type="getStatusTagType(currentTask.status)">
              {{ getStatusText(currentTask.status) }}
            </el-tag>
          </el-descriptions-item>
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
          <el-descriptions-item label="任务数据" :span="2">
            <pre class="json-display">{{ formatJson(currentTask.dataPayload) }}</pre>
          </el-descriptions-item>
          <el-descriptions-item
            v-if="currentTask.resultPayload"
            label="执行结果"
            :span="2"
          >
            <pre class="json-display">{{ formatJson(currentTask.resultPayload) }}</pre>
          </el-descriptions-item>
        </el-descriptions>
      </div>
      <template #footer>
        <el-button @click="detailDialogVisible = false">关闭</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { ElMessage, ElMessageBox } from 'element-plus';
import { Refresh, Operation } from '@element-plus/icons-vue';
import TaskTimeline from '@/components/TaskTimeline.vue';
import { getMyTasks } from '@/api/nursing';

const router = useRouter();

// 数据状态
const loading = ref(false);
const tasks = ref([]);
const filteredTasks = ref([]);
const selectedDate = ref(new Date());
const selectedStatus = ref('');
const selectedPatient = ref('');
const patientList = ref([]);
const detailDialogVisible = ref(false);
const currentTask = ref(null);

// 当前护士信息（从localStorage获取）
const getCurrentNurse = () => {
  const userInfo = localStorage.getItem('userInfo');
  if (userInfo) {
    try {
      const user = JSON.parse(userInfo);
      return user.staffId; // Login.vue 存储的字段名是 staffId
    } catch (error) {
      console.error('解析用户信息失败:', error);
    }
  }
  return null;
};

// 加载任务列表
const loadTasks = async () => {
  const nurseId = getCurrentNurse();
  if (!nurseId) {
    ElMessage.error('未找到护士信息，请重新登录');
    router.push('/login');
    return;
  }

  // 清空旧数据
  tasks.value = [];
  filteredTasks.value = [];
  patientList.value = [];
  selectedPatient.value = '';

  loading.value = true;
  try {
    // 使用本地日期格式，避免时区转换问题
    const year = selectedDate.value.getFullYear();
    const month = String(selectedDate.value.getMonth() + 1).padStart(2, '0');
    const day = String(selectedDate.value.getDate()).padStart(2, '0');
    const dateStr = `${year}-${month}-${day}`;
    
    const response = await getMyTasks(nurseId, dateStr, null); // 不在API层过滤状态
    tasks.value = response.tasks || [];
    
    // 确保每个任务都有延迟状态字段（兼容旧数据）
    tasks.value = tasks.value.map(task => ({
      ...task,
      delayMinutes: task.delayMinutes ?? 0,
      allowedDelayMinutes: task.allowedDelayMinutes ?? 0,
      excessDelayMinutes: task.excessDelayMinutes ?? 0,
      severityLevel: task.severityLevel ?? 'Normal'
    }));
    
    // 提取病人列表
    const patients = new Map();
    tasks.value.forEach(task => {
      if (!patients.has(task.patientId)) {
        patients.set(task.patientId, {
          id: task.patientId,
          name: task.patientName,
          bedId: task.bedId
        });
      }
    });
    patientList.value = Array.from(patients.values());
    
    // 应用筛选
    filterTasks();
    
    ElMessage.success(`加载了 ${tasks.value.length} 个任务`);
  } catch (error) {
    console.error('加载任务列表失败:', error);
    ElMessage.error(error.message || '加载任务失败');
  } finally {
    loading.value = false;
  }
};

// 筛选任务
const filterTasks = () => {
  let result = [...tasks.value];
  
  // 按病人筛选
  if (selectedPatient.value) {
    result = result.filter(task => task.patientId === selectedPatient.value);
  }
  
  // 按状态筛选
  if (selectedStatus.value) {
    result = result.filter(task => task.status === selectedStatus.value);
  }
  
  filteredTasks.value = result;
};

// 任务点击
const handleTaskClick = (task) => {
  console.log('任务点击:', task);
};

// 开始任务
const handleTaskStart = async (task) => {
  try {
    await ElMessageBox.confirm(
      `确定开始执行任务：${task.patientName} - ${task.category}？`,
      '开始任务',
      {
        confirmButtonText: '确定',
        cancelButtonText: '取消',
        type: 'info'
      }
    );

    // 这里应该调用后端API开始任务
    ElMessage.success('任务已开始（功能待实现）');
    loadTasks(); // 刷新列表
  } catch (error) {
    // 用户取消
  }
};

// 完成任务
const handleTaskComplete = async (task) => {
  try {
    await ElMessageBox.confirm(
      `确定完成任务：${task.patientName} - ${task.category}？`,
      '完成任务',
      {
        confirmButtonText: '确定',
        cancelButtonText: '取消',
        type: 'success'
      }
    );

    // 这里应该调用后端API完成任务
    ElMessage.success('任务已完成（功能待实现）');
    loadTasks(); // 刷新列表
  } catch (error) {
    // 用户取消
  }
};

// 查看详情
const handleViewDetail = (task) => {
  currentTask.value = task;
  detailDialogVisible.value = true;
};

// 跳转到床位概览
const goToDashboard = () => {
  router.push({ name: 'nurse-dashboard' });
};

// 格式化日期时间
const formatDateTime = (dateString) => {
  if (!dateString) return '';
  const date = new Date(dateString);
  return date.toLocaleString('zh-CN', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit'
  });
};

// 格式化JSON
const formatJson = (jsonString) => {
  if (!jsonString) return '';
  try {
    const obj = JSON.parse(jsonString);
    return JSON.stringify(obj, null, 2);
  } catch (error) {
    return jsonString;
  }
};

// 状态标签类型
const getStatusTagType = (status) => {
  const typeMap = {
    'Pending': 'warning',
    'Running': 'primary',
    'Completed': 'success',
    'Skipped': 'info',
    'Cancelled': 'danger'
  };
  return typeMap[status] || 'info';
};

// 状态文本
const getStatusText = (status) => {
  const textMap = {
    'Pending': '待执行',
    'Running': '执行中',
    'Completed': '已完成',
    'Skipped': '已跳过',
    'Cancelled': '已取消'
  };
  return textMap[status] || status;
};

// 组件挂载
onMounted(() => {
  loadTasks();
});
</script>

<style scoped>
.nurse-task-list {
  padding: 20px;
  max-width: 1400px;
  margin: 0 auto;
}

.task-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
}

.header-left h2 {
  margin: 0 0 8px 0;
  font-size: 24px;
  color: #303133;
}

.header-right {
  display: flex;
  align-items: center;
}

.task-content {
  margin-top: 20px;
}

.task-detail {
  max-height: 600px;
  overflow-y: auto;
}

.json-display {
  background: #f5f7fa;
  padding: 12px;
  border-radius: 4px;
  font-size: 12px;
  line-height: 1.5;
  max-height: 300px;
  overflow-y: auto;
  margin: 0;
}
</style>
