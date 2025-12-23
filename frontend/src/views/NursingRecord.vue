<template>
  <div class="order-application">
    <PatientListPanel 
      :patient-list="patientList"
      :selected-patients="selectedPatients"
      :my-ward-id="currentScheduledWardId"
      :multi-select="false"
      :enable-multi-select-mode="false"
      title="患者列表"
      badge-field=""
      :show-pending-filter="false"
      @patient-select="handlePatientSelect"
    />

    <div class="work-area">
      <PatientInfoBar 
        :patients="selectedPatients"
        :is-multi-select="false"
        :show-sort-control="false"
      />

      <div v-if="selectedPatients.length === 0" class="no-patient-bar">
        <el-icon><InfoFilled /></el-icon>
        <span>请从左侧患者列表中选择患者查看护理记录</span>
      </div>

      <div v-else class="record-content">
        <NursingRecordList
          :records="nursingRecords"
          :loading="loading"
          @start-input="handleStartInput"
          @view-detail="handleViewDetail"
          @date-change="handleDateChange"
        />
      </div>
    </div>

    <NursingRecordForm
      v-model="dialogVisible"
      :record-data="currentRecord"
      :mode="dialogMode"
      :current-nurse-id="currentNurseId"
      @submit-success="handleSubmitRecord"
    />
  </div>
</template>

<script setup>
import { ref, watch, onMounted } from 'vue';
import { ElMessage } from 'element-plus';
import { InfoFilled } from '@element-plus/icons-vue';

// 组件导入
import PatientListPanel from '@/components/PatientListPanel.vue';
import PatientInfoBar from '@/components/PatientInfoBar.vue';
import NursingRecordList from '@/components/NursingRecordList.vue';
import NursingRecordForm from '@/components/NursingRecordForm.vue';

// API 与 Composable 导入
import { usePatientData } from '@/composables/usePatientData';
import { submitVitalSigns, getPatientNursingTasks } from '@/api/nursing';

// ==================== 状态管理 (使用统一的 Composable) ====================
const { 
  patientList,
  selectedPatient, 
  selectedPatients,
  currentScheduledWardId,
  selectSinglePatient,
  initializePatientData,
  getCurrentNurse
} = usePatientData();

// 页面特有状态
const nursingRecords = ref([]);
const loading = ref(false);
const dialogVisible = ref(false);
const dialogMode = ref('input'); // 'input' 或 'view'
const currentRecord = ref({});
const selectedDate = ref(new Date().toISOString().split('T')[0]);

// 从 Composable 获取当前护士 ID
const nurseInfo = getCurrentNurse();
const currentNurseId = ref(nurseInfo?.staffId || '');

// ==================== 生命周期 ====================
onMounted(async () => {
  // 调用统一的初始化逻辑
  await initializePatientData();
  console.log('📋 护理记录页面初始化完成');
});

// ==================== 核心方法 ====================

/**
 * 处理患者选择 (调用 Composable 提供的方法确保高亮同步)
 */
const handlePatientSelect = (eventData) => {
  // 兼容不同组件发射的数据格式
  const patient = eventData.patient || eventData;
  
  // 使用 Composable 的方法，这会更新全局的 selectedPatients 数组，从而触发高亮
  selectSinglePatient(patient);
  
  // 加载该患者的护理记录
  if (patient && patient.patientId) {
    loadNursingRecords(patient.patientId, selectedDate.value);
  }
};

/**
 * 加载护理记录
 */
const loadNursingRecords = async (patientId, date) => {
  try {
    loading.value = true;
    const data = await getPatientNursingTasks(patientId, date);
    
    if (data && data.tasks && Array.isArray(data.tasks)) {
      nursingRecords.value = data.tasks;
    } else {
      nursingRecords.value = [];
    }
  } catch (error) {
    console.error('加载护理记录失败:', error);
    ElMessage.error('加载护理记录失败');
    nursingRecords.value = [];
  } finally {
    loading.value = false;
  }
};

/**
 * 提交护理记录成功后的回调
 */
const handleSubmitRecord = async (formData) => {
  try {
    loading.value = true;
    await submitVitalSigns(formData);
    ElMessage.success('护理记录提交成功');
    dialogVisible.value = false;
    
    // 重新加载当前选中患者的记录
    if (selectedPatient.value) {
      await loadNursingRecords(selectedPatient.value.patientId, selectedDate.value);
    }
  } catch (error) {
    ElMessage.error(error.response?.data?.message || '提交失败');
  } finally {
    loading.value = false;
  }
};

// ==================== 其他交互方法 ====================
const handleStartInput = (record) => {
  currentRecord.value = record;
  dialogMode.value = 'input';
  dialogVisible.value = true;
};

const handleViewDetail = (record) => {
  currentRecord.value = record;
  dialogMode.value = 'view';
  dialogVisible.value = true;
};

const handleDateChange = (date) => {
  selectedDate.value = date;
  if (selectedPatient.value) {
    loadNursingRecords(selectedPatient.value.patientId, date);
  }
};

// 监听选中患者变化（清空逻辑）
watch(() => selectedPatients.value, (newVal) => {
  if (newVal.length === 0) {
    nursingRecords.value = [];
  }
});
</script>

<style scoped>
/* ==================== 整体布局 (已同步宽度) ==================== */
.order-application {
  display: grid;
  grid-template-columns: 250px 1fr; 
  gap: 16px;
  padding: 20px;
  height: calc(100vh - 60px); /* 建议统一高度计算方式 */
  background: #f0f2f5;
  overflow: hidden;
}

/* ==================== 工作区 ==================== */
.work-area {
  display: flex;
  flex-direction: column;
  gap: 16px;
  overflow: hidden;
  background: white;
  border-radius: 8px;
  padding: 20px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.06);
}

/* ==================== 提示信息栏 ==================== */
.no-patient-bar {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 12px;
  padding: 20px 24px;
  background: linear-gradient(135deg, #e8f4ff 0%, #f0f8ff 100%);
  border: 1px solid #b3d8ff;
  border-radius: 8px;
  color: #409eff;
  font-size: 15px;
  font-weight: 500;
}

/* ==================== 护理记录内容区 ==================== */
.record-content {
  flex: 1;
  overflow: hidden;
  display: flex;
  flex-direction: column;
}

/* ==================== 响应式 ==================== */
@media (max-width: 768px) {
  .order-application {
    grid-template-columns: 1fr;
  }
}
</style>