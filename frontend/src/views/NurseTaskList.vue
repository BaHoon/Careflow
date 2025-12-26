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
      </div>
    </div>

    <!-- 任务时间轴 -->
    <div class="task-content" v-loading="loading">
      <TaskTimeline
        :tasks="filteredTasks"
        @task-click="handleTaskClick"
        @start-input="handleStartInput"
        @view-detail="handleViewDetail"
        @task-cancelled="handleTaskCancelled"
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

    <!-- 护理记录表单对话框 -->
    <NursingRecordForm
      v-model="recordDialogVisible"
      :record-data="currentRecord"
      :mode="recordDialogMode"
      :current-nurse-id="getCurrentNurse()"
      @submit-success="handleRecordSubmit"
    />
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { Refresh } from '@element-plus/icons-vue';
import TaskTimeline from '@/components/TaskTimeline.vue';
import NursingRecordForm from '@/components/NursingRecordForm.vue';
import { getMyTasks, submitVitalSigns } from '@/api/nursing';

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

// 护理记录表单相关状态
const recordDialogVisible = ref(false);
const recordDialogMode = ref('input'); // 'input' 或 'view'
const currentRecord = ref({});

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

// 开始录入护理记录
const handleStartInput = (task) => {
  currentRecord.value = task;
  recordDialogMode.value = 'input';
  recordDialogVisible.value = true;
};

// 查看详情
const handleViewDetail = (task) => {
  // 如果任务已完成，显示护理记录详情
  if (task.status === 'Completed' || task.status === 5) {
    currentRecord.value = task;
    recordDialogMode.value = 'view';
    recordDialogVisible.value = true;
  } else {
    // 否则显示任务详情
    currentTask.value = task;
    detailDialogVisible.value = true;
  }
};

// 护理记录提交成功回调
const handleRecordSubmit = async (formData) => {
  try {
    loading.value = true;
    console.log('提交护理记录数据:', formData);
    
    // 调用后端API提交数据
    await submitVitalSigns(formData);
    
    ElMessage.success('护理记录提交成功');
    recordDialogVisible.value = false;
    
    // 刷新任务列表，更新任务状态
    await loadTasks();
  } catch (error) {
    console.error('提交护理记录失败:', error);
    ElMessage.error(error.response?.data?.message || error.message || '提交失败');
  } finally {
    loading.value = false;
  }
};

// 处理任务取消事件
const handleTaskCancelled = async (taskId) => {
  // 刷新任务列表
  await loadTasks();
};

// 格式化日期时间
const formatDateTime = (dateString) => {
  if (!dateString) return '';
  try {
    // 确保UTC时间字符串带有Z标识
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
