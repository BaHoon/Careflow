<template>
  <div class="patient-management-view">
    <!-- ============================== 
      【患者管理界面】
      顶部：筛选栏 + 搜索 + 新增按钮
      中间：患者卡片网格
      弹窗：患者详情编辑 / 出院检查
    ============================== -->

    <!-- 顶部筛选工具栏 -->
    <div class="filter-toolbar">
      <div class="filter-left">
        <!-- 状态筛选 -->
        <div class="filter-group">
          <span class="filter-label">患者状态:</span>
          <el-select 
            v-model="filterStatus" 
            placeholder="选择状态" 
            clearable
            @change="handleStatusFilterChange"
            size="default"
            class="status-select"
          >
            <el-option label="全部状态" :value="null" />
            <el-option label="在院" :value="1" />
            <el-option label="待出院" :value="2" />
          </el-select>
        </div>

        <!-- 搜索框 -->
        <div class="filter-group">
          <el-input
            v-model="searchKeyword"
            placeholder="搜索患者ID / 身份证号 / 姓名"
            clearable
            @input="handleSearch"
            size="default"
            class="search-input"
            style="width: 320px"
          >
            <template #prefix>
              <el-icon><Search /></el-icon>
            </template>
          </el-input>
        </div>
      </div>

      <div class="filter-right">
        <!-- 新增患者按钮（仅护士端显示） -->
        <el-button 
          v-if="isNurse"
          type="primary" 
          :icon="Plus"
          @click="handleAddPatient"
          size="default"
        >
          新增患者入院
        </el-button>
      </div>
    </div>

    <!-- 患者卡片网格 -->
    <div class="patient-grid-container">
      <!-- 加载状态 -->
      <div v-if="loading" class="loading-state">
        <el-icon class="is-loading"><Loading /></el-icon>
        <p>加载中...</p>
      </div>

      <!-- 空状态 -->
      <div v-else-if="patientList.length === 0" class="empty-state">
        <div class="empty-icon">🏥</div>
        <p>暂无患者信息</p>
        <p class="empty-hint">点击右上角"新增患者入院"按钮添加患者</p>
      </div>

      <!-- 患者卡片列表 -->
      <div v-else class="patient-grid">
        <el-popover
          v-for="patient in patientList" 
          :key="patient.id"
          placement="right"
          :width="280"
          trigger="hover"
          popper-class="patient-staff-popover"
        >
          <template #reference>
            <div 
              class="patient-card"
              :class="{ 'highlighted': shouldHighlight(patient) }"
              @click="handleCardClick(patient)"
            >
              <!-- 卡片头部 -->
              <div class="card-header">
            <!-- 状态标签 -->
            <el-tag 
              :type="getStatusColor(patient.status)" 
              size="default"
              class="status-tag"
            >
              {{ getStatusText(patient.status) }}
            </el-tag>

            <!-- 护理级别标签 -->
            <el-tag 
              :type="getNursingGradeColor(patient.nursingGrade)" 
              size="small"
              class="nursing-tag"
            >
              {{ getNursingGradeText(patient.nursingGrade) }}
            </el-tag>

            <!-- 异常状态标签 -->
            <el-tag 
              v-if="patient.nursingAnomalyStatus === 1"
              type="danger"
              size="small"
              class="anomaly-tag"
            >
              异常
            </el-tag>

            <!-- 患者ID -->
            <span class="patient-id">{{ patient.id }}</span>
          </div>

          <!-- 患者基本信息 -->
          <div class="card-body">
            <!-- 姓名和性别 -->
            <div class="info-row name-row">
              <span class="name">{{ patient.name }}</span>
              <el-tag :type="patient.gender === '男' ? '' : 'danger'" size="small">
                {{ patient.gender }}
              </el-tag>
              <span class="age">{{ patient.age }}岁</span>
            </div>

            <!-- 床位信息 -->
            <div class="info-row">
              <el-icon><LocationInformation /></el-icon>
              <span class="label">床位:</span>
              <span class="value">{{ patient.bedId || '未分配' }}</span>
            </div>

            <!-- 科室 -->
            <div class="info-row" v-if="patient.department">
              <el-icon><OfficeBuilding /></el-icon>
              <span class="label">科室:</span>
              <span class="value">{{ patient.department }}</span>
            </div>
          </div>

          <!-- 卡片底部操作栏 -->
          <div class="card-footer">
            <!-- 查看详情按钮 -->
            <el-button 
              size="small" 
              type="primary"
              link
              @click.stop="handleViewDetail(patient)"
            >
              查看详情
            </el-button>

            <!-- 护士端操作按钮 -->
            <template v-if="isNurse">
              <!-- 入院按钮（待入院状态显示） -->
              <el-button 
                v-if="patient.status === 0"
                size="small" 
                type="warning"
                @click.stop="handleAdmission(patient)"
              >
                办理入院
              </el-button>

              <!-- 出院按钮（待出院状态显示） -->
              <el-button 
                v-if="patient.status === 2"
                size="small" 
                type="success"
                @click.stop="handleDischarge(patient)"
              >
                办理出院
              </el-button>
            </template>
          </div>
        </div>
          </template>

          <div class="staff-info-content">
            <div class="staff-group">
              <div class="group-title">
                <el-icon><Avatar /></el-icon> 责任医生
              </div>
              <div class="info-list">
                <div class="info-item">
                  <span class="label">姓名:</span>
                  <span class="value">{{ patient.responsibleDoctorName || '未分配' }}</span>
                </div>
                <div class="info-item">
                  <span class="label">ID:</span>
                  <span class="value">{{ patient.responsibleDoctorId || '-' }}</span>
                </div>
                <div class="info-item">
                  <span class="label">电话:</span>
                  <span class="value">{{ patient.responsibleDoctorPhone || '-' }}</span>
                </div>
              </div>
            </div>
            
            <el-divider style="margin: 12px 0" />
            
            <div class="staff-group">
              <div class="group-title">
                <el-icon><FirstAidKit /></el-icon> 责任护士 (当前)
              </div>
              <div class="info-list">
                <div class="info-item">
                  <span class="label">姓名:</span>
                  <span class="value">{{ patient.responsibleNurseName || '未分配' }}</span>
                </div>
                <div class="info-item">
                  <span class="label">ID:</span>
                  <span class="value">{{ patient.responsibleNurseId || '-' }}</span>
                </div>
                <div class="info-item">
                  <span class="label">电话:</span>
                  <span class="value">{{ patient.responsibleNursePhone || '-' }}</span>
                </div>
              </div>
            </div>
          </div>
        </el-popover>
      </div>
    </div>

    <!-- 患者详情对话框 -->
    <el-dialog
      v-model="patientDetailDialogVisible"
      :title="`患者详情 - ${currentPatient.name} (${currentPatient.id})`"
      width="800px"
      :close-on-click-modal="false"
    >
      <el-form
        ref="patientDetailFormRef"
        :model="patientDetailForm"
        :rules="patientDetailRules"
        label-width="120px"
        v-loading="loadingPatientDetail"
      >
        <!-- 基本信息（不可修改） -->
        <el-divider content-position="left">
          <span style="font-size: 16px; font-weight: 600; color: #303133;">基本信息</span>
        </el-divider>
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="姓名">
              <el-input v-model="currentPatient.name" disabled class="readonly-input" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="身份证号">
              <el-input v-model="currentPatient.idCard" disabled class="readonly-input" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="住院号">
              <el-input v-model="currentPatient.id" disabled class="readonly-input" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="出生日期">
              <el-input v-model="currentPatient.dateOfBirth" disabled class="readonly-input" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="年龄">
              <el-input :value="currentPatient.age + '岁'" disabled class="readonly-input" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="24">
            <el-form-item label="门诊诊断">
              <el-input 
                v-model="currentPatient.outpatientDiagnosis" 
                type="textarea"
                :rows="2"
                disabled
                class="readonly-input"
              />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="预约入院时间">
              <el-input :value="formatDateTime(currentPatient.scheduledAdmissionTime)" disabled class="readonly-input" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="实际入院时间">
              <el-input :value="formatDateTime(currentPatient.actualAdmissionTime)" disabled class="readonly-input" />
            </el-form-item>
          </el-col>
        </el-row>

        <!-- 可编辑信息 -->
        <el-divider content-position="left">
          <span style="font-size: 16px; font-weight: 600; color: #303133;">可编辑信息</span>
        </el-divider>
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="性别" prop="gender">
              <el-select v-model="patientDetailForm.gender" placeholder="请选择性别" style="width: 100%">
                <el-option label="男" value="男" />
                <el-option label="女" value="女" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="电话号码" prop="phoneNumber">
              <el-input v-model="patientDetailForm.phoneNumber" placeholder="请输入电话号码" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="身高(cm)" prop="height">
              <el-input-number 
                v-model="patientDetailForm.height" 
                :min="0" 
                :max="300" 
                :precision="1"
                placeholder="请输入身高"
                style="width: 100%"
              />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="体重(kg)" prop="weight">
              <el-input-number 
                v-model="patientDetailForm.weight" 
                :min="0" 
                :max="500" 
                :precision="1"
                placeholder="请输入体重"
                style="width: 100%"
              />
            </el-form-item>
          </el-col>
        </el-row>

        <!-- 住院信息（只读） -->
        <el-divider content-position="left">
          <span style="font-size: 16px; font-weight: 600; color: #303133;">住院信息</span>
        </el-divider>
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="当前病床">
              <el-input :value="currentPatient.bedId || '未分配'" disabled class="readonly-input" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="所属科室">
              <el-input v-model="currentPatient.department" disabled class="readonly-input" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="所属病区">
              <el-input v-model="currentPatient.ward" disabled class="readonly-input" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="护理级别">
              <el-tag :type="getNursingGradeColor(currentPatient.nursingGrade)">
                {{ getNursingGradeText(currentPatient.nursingGrade) }}
              </el-tag>
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="当前状态">
              <el-tag :type="getStatusColor(currentPatient.status)">
                {{ getStatusText(currentPatient.status) }}
              </el-tag>
            </el-form-item>
          </el-col>
        </el-row>
      </el-form>

      <template #footer>
        <span class="dialog-footer">
          <el-button @click="patientDetailDialogVisible = false">取消</el-button>
          <el-button type="primary" @click="handleSavePatientDetail" :loading="savingPatientDetail">
            保存修改
          </el-button>
        </span>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted, computed } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { 
  Search, 
  Plus, 
  Loading,
  LocationInformation,
  OfficeBuilding,
  WarningFilled,
  Avatar,
  FirstAidKit
} from '@element-plus/icons-vue';
import { 
  getPatientManagementList,
  getPatientFullInfo,
  updatePatientInfo,
  getPatientStatusText,
  getPatientStatusColor,
  getNursingGradeText
} from '@/api/patient';

// ==================== Props ====================
const props = defineProps({
  // 用户角色：'Nurse' 或 'Doctor'
  userRole: {
    type: String,
    default: 'Nurse',
    validator: (value) => ['Nurse', 'Doctor'].includes(value)
  }
});

// ==================== 响应式数据 ====================
const loading = ref(false);
const patientList = ref([]);
const filterStatus = ref(null); // 状态筛选
const searchKeyword = ref(''); // 搜索关键词

// 患者详情对话框状态
const patientDetailDialogVisible = ref(false);
const loadingPatientDetail = ref(false);
const savingPatientDetail = ref(false);
const patientDetailFormRef = ref(null);
const currentPatient = reactive({
  id: '',
  name: '',
  idCard: '',
  dateOfBirth: '',
  age: 0,
  outpatientDiagnosis: '',
  scheduledAdmissionTime: '',
  actualAdmissionTime: '',
  bedId: '',
  department: '',
  ward: '',
  nursingGrade: 0,
  status: 0
});

const patientDetailForm = reactive({
  gender: '',
  phoneNumber: '',
  height: null,
  weight: null
});

const patientDetailRules = {
  gender: [
    { required: true, message: '请选择性别', trigger: 'change' }
  ],
  phoneNumber: [
    { pattern: /^1[3-9]\d{9}$/, message: '请输入有效的手机号码', trigger: 'blur' }
  ],
  height: [
    { type: 'number', min: 0, max: 300, message: '身高必须在0-300cm之间', trigger: 'blur' }
  ],
  weight: [
    { type: 'number', min: 0, max: 500, message: '体重必须在0-500kg之间', trigger: 'blur' }
  ]
};

// 从 localStorage 获取用户信息
const userInfo = ref(null);

// 计算属性：是否为护士角色
const isNurse = computed(() => {
  return props.userRole === 'Nurse';
});

// 搜索防抖定时器
let searchTimer = null;

// ==================== 生命周期 ====================
onMounted(() => {
  loadPatientList();
});

// ==================== 获取当前用户信息 ====================
const getCurrentUser = () => {
  const userStr = localStorage.getItem('userInfo');
  if (!userStr) {
    ElMessage.error('未找到当前用户信息，请重新登录');
    return null;
  }
  try {
    const user = JSON.parse(userStr);
    console.log('当前用户信息:', user);
    return {
      staffId: user.staffId,
      deptCode: user.deptCode,
      fullName: user.fullName,
      role: user.role
    };
  } catch (error) {
    console.error('解析用户信息失败:', error);
    ElMessage.error('用户信息格式错误');
    return null;
  }
};

// ==================== 数据加载 ====================
/**
 * 加载患者列表
 */
const loadPatientList = async () => {
  loading.value = true;
  
  try {
    const params = {};
    
    // 不再传递状态筛选参数，前端通过高亮处理
    
    // 添加搜索关键词
    if (searchKeyword.value && searchKeyword.value.trim()) {
      params.keyword = searchKeyword.value.trim();
    }
    
    // 如果是医生，添加科室过滤
    if (!isNurse.value) {
      const currentUser = getCurrentUser();
      if (!currentUser || !currentUser.deptCode) {
        ElMessage.error('无法获取医生科室信息');
        return;
      }
      params.departmentId = currentUser.deptCode;
      console.log('医生科室过滤:', currentUser.deptCode);
    }
    
    // 调用API
    const data = await getPatientManagementList(params);
    patientList.value = data || [];
    
    console.log('患者列表加载成功:', patientList.value);
  } catch (error) {
    console.error('加载患者列表失败:', error);
    ElMessage.error('加载患者列表失败: ' + (error.message || '未知错误'));
    patientList.value = [];
  } finally {
    loading.value = false;
  }
};

/**
 * 搜索防抖处理
 */
const handleSearch = () => {
  // 清除之前的定时器
  if (searchTimer) {
    clearTimeout(searchTimer);
  }
  
  // 500ms后执行搜索
  searchTimer = setTimeout(() => {
    loadPatientList();
  }, 500);
};

/**
 * 状态筛选变化处理
 */
const handleStatusFilterChange = () => {
  // 状态筛选变化时不重新加载，只是更新高亮样式
  console.log('状态筛选变化:', filterStatus.value);
};

/**
 * 判断患者是否应该被高亮
 */
const shouldHighlight = (patient) => {
  // 如果没有选择特定状态，不高亮任何患者
  if (filterStatus.value === null || filterStatus.value === undefined) {
    return false;
  }
  // 高亮匹配状态的患者
  return patient.status === filterStatus.value;
};

// ==================== 事件处理 ====================
/**
 * 卡片点击事件
 */
const handleCardClick = (patient) => {
  console.log('点击患者卡片:', patient);
  handleViewDetail(patient);
};

/**
 * 查看详情
 */
const handleViewDetail = async (patient) => {
  loadingPatientDetail.value = true;
  
  try {
    const data = await getPatientFullInfo(patient.id);
    
    // 填充只读信息
    Object.assign(currentPatient, {
      id: data.id || '',
      name: data.name || '',
      idCard: data.idCard || '',
      dateOfBirth: data.dateOfBirth || '',
      age: data.age || 0,
      outpatientDiagnosis: data.outpatientDiagnosis || '',
      scheduledAdmissionTime: data.scheduledAdmissionTime || '',
      actualAdmissionTime: data.actualAdmissionTime || '',
      bedId: data.bedId || '',
      department: data.department || '',
      ward: data.ward || '',
      nursingGrade: data.nursingGrade || 0,
      status: data.status || 0
    });
    
    // 填充可编辑表单 - 确保gender不为null
    Object.assign(patientDetailForm, {
      gender: data.gender || '',  // 确保不是null或undefined
      phoneNumber: data.phoneNumber || '',
      height: data.height ?? null,  // 使用??操作符保持0值
      weight: data.weight ?? null
    });
    
    console.log('患者详情加载成功:', data);
    console.log('表单数据:', patientDetailForm);
    
    // 数据加载完成后再显示对话框
    patientDetailDialogVisible.value = true;
  } catch (error) {
    console.error('加载患者详情失败:', error);
    ElMessage.error('加载患者详情失败');
  } finally {
    loadingPatientDetail.value = false;
  }
};

/**
 * 保存患者详情修改
 */
const handleSavePatientDetail = async () => {
  if (!patientDetailFormRef.value) {
    console.error('表单引用不存在');
    return;
  }

  // 表单验证
  const valid = await patientDetailFormRef.value.validate().catch(() => false);
  if (!valid) {
    return;
  }

  savingPatientDetail.value = true;
  
  try {
    // 获取当前用户信息
    const currentUser = getCurrentUser();
    if (!currentUser) {
      ElMessage.error('无法获取当前用户信息');
      return;
    }

    // 构造更新数据（只包含可编辑字段）
    const updateData = {
      operatorId: currentUser.staffId,
      operatorType: isNurse.value ? 'Nurse' : 'Doctor'
    };

    // 只添加非空字段
    if (patientDetailForm.gender) {
      updateData.gender = patientDetailForm.gender;
    }
    if (patientDetailForm.phoneNumber) {
      updateData.phoneNumber = patientDetailForm.phoneNumber;
    }
    if (patientDetailForm.height !== null && patientDetailForm.height !== undefined) {
      updateData.height = patientDetailForm.height;
    }
    if (patientDetailForm.weight !== null && patientDetailForm.weight !== undefined) {
      updateData.weight = patientDetailForm.weight;
    }

    console.log('更新患者信息:', currentPatient.id, updateData);

    // 调用API
    await updatePatientInfo(currentPatient.id, updateData);

    ElMessage.success('保存成功');
    patientDetailDialogVisible.value = false;

    // 刷新患者列表
    await loadPatientList();
  } catch (error) {
    console.error('保存患者详情失败:', error);
    ElMessage.error('保存失败: ' + (error.response?.data?.message || error.message || '未知错误'));
  } finally {
    savingPatientDetail.value = false;
  }
};

/**
 * 办理入院
 */
const handleAdmission = async (patient) => {
  // TODO: 实现入院办理功能
  ElMessage.info({ message: `入院办理功能将在后续版本实现（患者: ${patient.name}）`, duration: 3000 });
};

/**
 * 办理出院
 */
const handleDischarge = async (patient) => {
  // TODO: 第四阶段实现出院检查弹窗
  ElMessage.info({ message: `出院检查功能将在第四阶段实现（患者ID: ${patient.id}）`, duration: 3000 });
};

/**
 * 新增患者
 */
const handleAddPatient = () => {
  ElMessage.info({ message: '新增患者功能将在后续版本实现', duration: 3000 });
};

// ==================== 辅助方法 ====================
/**
 * 获取状态显示文本
 */
const getStatusText = (status) => {
  return getPatientStatusText(status);
};

/**
 * 获取状态标签颜色
 */
const getStatusColor = (status) => {
  return getPatientStatusColor(status);
};

/**
 * 获取护理级别颜色
 */
const getNursingGradeColor = (grade) => {
  const colorMap = {
    0: 'danger',   // 特级 - 红色
    1: 'warning',  // 一级 - 橙色
    2: 'primary',  // 二级 - 蓝色
    3: 'info'      // 三级 - 灰色
  };
  return colorMap[grade] || 'info';
};

/**
 * 格式化日期时间
 */
const formatDateTime = (dateTime) => {
  if (!dateTime) return '-';
  try {
    const date = new Date(dateTime);
    // JavaScript的toLocaleString会自动转换为本地时区（北京时间UTC+8）
    return date.toLocaleString('zh-CN', { 
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit',
      timeZone: 'Asia/Shanghai'
    });
  } catch {
    return dateTime;
  }
};
</script>

<style scoped>
/* ==================== 布局容器 ==================== */
.patient-management-view {
  display: flex;
  flex-direction: column;
  height: 100vh;
  background-color: var(--bg-page, #f4f7f9);
  padding: 20px;
  box-sizing: border-box;
}

/* ==================== 筛选工具栏 ==================== */
.filter-toolbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 16px 20px;
  background-color: var(--bg-card, #ffffff);
  border-radius: 8px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
  margin-bottom: 20px;
}

.filter-left {
  display: flex;
  align-items: center;
  gap: 20px;
  flex: 1;
}

.filter-right {
  display: flex;
  align-items: center;
  gap: 12px;
}

.filter-group {
  display: flex;
  align-items: center;
  gap: 8px;
}

.filter-label {
  font-size: 14px;
  color: var(--text-secondary, #606266);
  white-space: nowrap;
  font-weight: 500;
}

.status-select {
  min-width: 140px;
}

.search-input {
  width: 320px;
}

/* ==================== 患者网格容器 ==================== */
.patient-grid-container {
  flex: 1;
  overflow-y: auto;
  background-color: var(--bg-page, #f4f7f9);
}

.patient-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
  gap: 20px;
  padding: 4px; /* 防止阴影被裁剪 */
}

/* ==================== 患者卡片 ==================== */
.patient-card {
  background-color: var(--bg-card, #ffffff);
  border-radius: 8px;
  padding: 16px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
  cursor: pointer;
  transition: all 0.3s ease;
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.patient-card:hover {
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.12);
  transform: translateY(-2px);
}

/* 高亮样式 */
.patient-card.highlighted {
  border: 2px solid #409eff;
  box-shadow: 0 4px 20px rgba(64, 158, 255, 0.3);
  background: linear-gradient(135deg, #fff 0%, #f0f9ff 100%);
}

/* 卡片头部 */
.card-header {
  display: flex;
  align-items: center;
  gap: 8px;
  padding-bottom: 12px;
  border-bottom: 1px solid var(--border-color, #e4e7ed);
}

.status-tag {
  font-weight: 600;
}

.nursing-tag {
  margin-left: auto;
}

.patient-id {
  font-size: 12px;
  color: var(--text-secondary, #909399);
  font-family: 'Consolas', monospace;
}

/* 卡片主体 */
.card-body {
  display: flex;
  flex-direction: column;
  gap: 8px;
  flex: 1;
}

.info-row {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 14px;
  color: var(--text-primary, #303133);
}

.name-row {
  margin-bottom: 4px;
}

.name {
  font-size: 18px;
  font-weight: 600;
  color: var(--text-primary, #303133);
}

.age {
  font-size: 14px;
  color: var(--text-secondary, #606266);
  margin-left: 8px;
}

.info-row .el-icon {
  color: var(--primary-color, #409eff);
  font-size: 16px;
}

.info-row .label {
  color: var(--text-secondary, #909399);
  font-size: 13px;
}

.info-row .value {
  color: var(--text-primary, #303133);
  font-size: 14px;
  font-weight: 500;
}

/* 卡片底部 */
.card-footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding-top: 12px;
  border-top: 1px solid var(--border-color, #e4e7ed);
}

/* ==================== 加载和空状态 ==================== */
.loading-state,
.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  min-height: 400px;
  color: var(--text-secondary, #909399);
}

.loading-state .el-icon {
  font-size: 48px;
  color: var(--primary-color, #409eff);
  margin-bottom: 16px;
}

.loading-state p {
  font-size: 16px;
  margin: 0;
}

.empty-icon {
  font-size: 64px;
  margin-bottom: 16px;
  opacity: 0.6;
}

.empty-state p {
  font-size: 16px;
  margin: 8px 0;
}

.empty-hint {
  font-size: 14px;
  color: var(--text-placeholder, #c0c4cc);
}

/* ==================== 响应式布局 ==================== */
@media (max-width: 1400px) {
  .patient-grid {
    grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
  }
}

.anomaly-tag {
  min-width: 40px;
  text-align: center;
}

@media (max-width: 768px) {
  .filter-toolbar {
    flex-direction: column;
    gap: 12px;
    align-items: stretch;
  }

  .filter-left {
    flex-direction: column;
    align-items: stretch;
  }

  .search-input {
    width: 100%;
  }

  .patient-grid {
    grid-template-columns: 1fr;
  }
}

/* ==================== 对话框样式 ==================== */
.dialog-footer {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
}

/* 只读输入框 - 黑色字体 */
.readonly-input :deep(.el-input__inner),
.readonly-input :deep(.el-textarea__inner) {
  color: #303133 !important;
  -webkit-text-fill-color: #303133 !important;
  background-color: #f5f7fa !important;
}

/* ==================== 医护人员信息弹窗样式 ==================== */
.staff-info-content {
  padding: 4px;
}

.staff-group {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.group-title {
  font-size: 14px;
  font-weight: 600;
  color: #303133;
  display: flex;
  align-items: center;
  gap: 6px;
}

.group-title .el-icon {
  color: #409eff;
}

.info-list {
  display: flex;
  flex-direction: column;
  gap: 4px;
  padding-left: 22px;
}

.info-item {
  font-size: 13px;
  display: flex;
  align-items: center;
}

.info-item .label {
  color: #909399;
  width: 40px;
  margin-right: 8px;
}

.info-item .value {
  color: #606266;
  font-family: 'Consolas', monospace;
}
</style>
