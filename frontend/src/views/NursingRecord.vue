<template>
  <div class="order-application">
    <!-- 左侧患者列表面板 -->
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

    <!-- 右侧护理记录工作区 -->
    <div class="work-area">
      <!-- 患者信息栏 -->
      <PatientInfoBar 
        :patients="selectedPatients"
        :is-multi-select="false"
        :show-sort-control="false"
      />

      <!-- 提示信息：未选择患者 -->
      <div v-if="selectedPatients.length === 0" class="no-patient-bar">
        <el-icon><InfoFilled /></el-icon>
        <span>请从左侧患者列表中选择患者查看护理记录</span>
      </div>

      <!-- 护理记录列表 -->
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

    <!-- 护理记录表单对话框 -->
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
import { ref, computed, onMounted, watch } from 'vue';
import { ElMessage } from 'element-plus';
import { InfoFilled } from '@element-plus/icons-vue';
import PatientListPanel from '@/components/PatientListPanel.vue';
import PatientInfoBar from '@/components/PatientInfoBar.vue';
import NursingRecordList from '@/components/NursingRecordList.vue';
import NursingRecordForm from '@/components/NursingRecordForm.vue';
import { getWardOverview, submitVitalSigns, getPatientNursingTasks } from '@/api/nursing';

// ==================== 状态管理 ====================
const patientList = ref([]);
const selectedPatients = ref([]);
const nursingRecords = ref([]);
const loading = ref(false);
const dialogVisible = ref(false);
const dialogMode = ref('input'); // 'input' 或 'view'
const currentRecord = ref({});
const selectedDate = ref(new Date().toISOString().split('T')[0]);

// 当前护士信息（从localStorage获取）
const getUserInfo = () => {
  try {
    const userInfoStr = localStorage.getItem('userInfo');
    return userInfoStr ? JSON.parse(userInfoStr) : null;
  } catch {
    return null;
  }
};

const userInfo = getUserInfo();
console.log('📋 用户信息:', userInfo);

const currentNurseId = ref(userInfo?.staffId || '');
const currentDepartmentId = ref(userInfo?.deptCode || null);
const currentScheduledWardId = ref(''); // 用于高亮我负责的病区

// ==================== 生命周期 ====================
onMounted(async () => {
  console.log('🔍 当前护士ID:', currentNurseId.value);
  console.log('🔍 当前科室代码:', currentDepartmentId.value);
  await loadPatientList();
  console.log('📋 护理记录页面初始化完成');
});

// ==================== 方法 ====================

/**
 * 加载患者列表
 */
const loadPatientList = async () => {
  try {
    loading.value = true;
    
    // 使用科室代码获取患者列表（与NurseDashboard保持一致）
    const deptId = currentDepartmentId.value;
    
    console.log('🔍 请求参数 - wardId: null, deptId:', deptId);
    
    if (!deptId) {
      ElMessage.warning('未获取到科室信息，请重新登录');
      loading.value = false;
      return;
    }
    
    const data = await getWardOverview(null, deptId);
    console.log('📦 API返回数据:', data);
    
    if (!data) {
      throw new Error('未返回数据');
    }
    
    // 处理返回的数据结构
    let patients = [];
    
    if (data.patients) {
      // 直接返回patients数组
      patients = data.patients;
    } else if (data.wards && Array.isArray(data.wards)) {
      // 返回的是wards数组，需要提取所有病区的患者
      data.wards.forEach(ward => {
        if (ward.beds && Array.isArray(ward.beds)) {
          ward.beds.forEach(bed => {
            if (bed.patient) {
              // 映射后端返回的字段名到前端需要的字段名
              patients.push({
                patientId: bed.patient.id,        // id -> patientId
                patientName: bed.patient.name,    // name -> patientName
                gender: bed.patient.gender,
                age: bed.patient.age,
                nursingGrade: bed.patient.nursingGrade,
                bedId: bed.patient.bedId,
                wardId: ward.wardId,
                wardName: ward.wardName
              });
            }
          });
        }
      });
    }
    
    patientList.value = patients;
  } catch (error) {
    console.error('加载患者列表失败:', error);
    ElMessage.error(error.response?.data || error.message || '加载患者列表失败');
  } finally {
    loading.value = false;
  }
};

/**
 * 处理患者选择
 */
const handlePatientSelect = ({ patient, isMultiSelect, isCheckboxClick }) => {
  // 强制单选模式，忽略多选逻辑
  // 只选中当前点击的这一个患者
  selectedPatients.value = [patient];
  
  // 加载该患者的护理记录
  if (patient && patient.patientId) {
    loadNursingRecords(patient.patientId, selectedDate.value);
  } else {
    ElMessage.error('患者数据不完整，无法加载护理记录');
  }
};

/**
 * 加载护理记录
 * 获取指定患者的所有护理任务（无论责任护士是否是我）
 */
const loadNursingRecords = async (patientId, date) => {
  try {
    loading.value = true;
    
    console.log('🔍 请求参数:', { patientId, date });
    
    // 使用新的patient-nursing-tasks API获取该患者的所有护理任务
    const data = await getPatientNursingTasks(patientId, date);
    
    console.log('📦 API返回原始数据:', data);
    
    // 处理API返回的数据结构
    if (data && data.tasks && Array.isArray(data.tasks)) {
      console.log('📋 任务列表:', data.tasks);
      console.log('📊 任务数量:', data.tasks.length);
      
      // 检查是否有重复的任务ID
      const taskIds = data.tasks.map(t => t.id);
      const uniqueIds = new Set(taskIds);
      console.log('🔢 总任务数:', taskIds.length);
      console.log('🔢 唯一ID数:', uniqueIds.size);
      
      if (taskIds.length !== uniqueIds.size) {
        console.warn('⚠️ 检测到重复的任务ID!');
        const duplicates = taskIds.filter((id, index) => taskIds.indexOf(id) !== index);
        console.log('重复的ID:', duplicates);
      }
      
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
 * 处理开始录入
 */
const handleStartInput = (record) => {
  currentRecord.value = record;
  dialogMode.value = 'input';
  dialogVisible.value = true;
};

/**
 * 处理查看详情
 */
const handleViewDetail = (record) => {
  currentRecord.value = record;
  dialogMode.value = 'view';
  dialogVisible.value = true;
  console.log('👁️ 查看护理记录:', record.id);
};

/**
 * 处理日期变化
 */
const handleDateChange = (date) => {
  selectedDate.value = date;
  if (selectedPatients.value.length > 0) {
    loadNursingRecords(selectedPatients.value[0].patientId, date);
  }
  console.log('📅 日期变更:', date);
};

/**
 * 提交护理记录
 */
const handleSubmitRecord = async (formData) => {
  try {
    loading.value = true;
    
    // 调用API提交数据
    await submitVitalSigns(formData);
    
    ElMessage.success('护理记录提交成功');
    dialogVisible.value = false;
    
    // 重新加载记录
    if (selectedPatients.value.length > 0) {
      await loadNursingRecords(selectedPatients.value[0].patientId, selectedDate.value);
    }
    
    console.log('✅ 护理记录提交成功');
  } catch (error) {
    console.error('❌ 提交护理记录失败:', error);
    ElMessage.error(error.message || '提交失败，请重试');
  } finally {
    loading.value = false;
  }
};

// ==================== 监听选中患者变化 ====================
watch(() => selectedPatients.value, (newVal) => {
  if (newVal.length === 0) {
    nursingRecords.value = [];
  }
});
</script>

<style scoped>
/* ==================== 整体布局 ==================== */
.order-application {
  display: grid;
  grid-template-columns: 340px 1fr;
  gap: 16px;
  padding: 16px;
  height: 100%;
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
  padding: 16px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.06);
}

/* ==================== 提示信息栏 ==================== */
.no-patient-bar {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 20px 24px;
  background: linear-gradient(135deg, #e8f4ff 0%, #f0f8ff 100%);
  border: 1px solid #b3d8ff;
  border-radius: 8px;
  color: #409eff;
  font-size: 15px;
  font-weight: 500;
  box-shadow: 0 2px 8px rgba(64, 158, 255, 0.1);
}

.no-patient-bar .el-icon {
  font-size: 20px;
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
