<template>
  <div class="order-application">
    <!-- 左侧患者列表面板 -->
    <PatientListPanel 
      :patient-list="patientList"
      :selected-patients="selectedPatients"
      :my-ward-id="currentScheduledWardId"
      :multi-select="enableMultiSelect"
      title="患者列表"
      pending-filter-label="仅显示有待申请"
      badge-field="pendingApplicationCount"
      :collapsed="false"
      @patient-select="handlePatientSelect"
      @multi-select-toggle="toggleMultiSelectMode"
    />


    <!-- 右侧医嘱申请工作区 -->
    <div class="work-area">
      <!-- 患者信息栏 -->
      <PatientInfoBar 
        :patients="selectedPatients"
        :is-multi-select="enableMultiSelect"
        :sort-by="sortBy"
        @sort-change="handleSortChange"
      />

      <!-- Tab导航栏（点击切换） -->
      <div class="tab-navigation">
        <div 
          class="tab-item"
          :class="{ active: activeTab === 'medication' }"
          @click="handleTabClick('medication')"
        >
          <span class="tab-icon">💊</span>
          <span class="tab-label">药品申请</span>
          <span v-if="pendingMedicationCount > 0" class="badge-dot"></span>
        </div>
        <div 
          class="tab-item"
          :class="{ active: activeTab === 'inspection' }"
          @click="handleTabClick('inspection')"
        >
          <span class="tab-icon">🔬</span>
          <span class="tab-label">检查申请</span>
          <span v-if="pendingInspectionCount > 0" class="badge-dot"></span>
        </div>
      </div>

      <!-- 提示信息：未选择患者 -->
      <div v-if="selectedPatients.length === 0" class="no-patient-bar">
        <el-icon><InfoFilled /></el-icon>
        <span>请从左侧患者列表中选择患者查看待申请项</span>
      </div>

      <!-- 筛选工具栏 -->
      <div v-if="selectedPatients.length > 0" class="filter-toolbar">
        <!-- 状态筛选 -->
        <div class="filter-group">
          <span class="filter-label">状态:</span>
          <el-checkbox-group v-model="statusFilter" @change="loadApplications">
            <el-checkbox label="Applying">待申请</el-checkbox>
            <el-checkbox label="Applied">已申请</el-checkbox>
            <el-checkbox label="AppliedConfirmed">已确认</el-checkbox>
            <el-checkbox label="PendingReturn">待退回</el-checkbox>
            <!-- 仅药品申请显示异常取消待退药选项 -->
            <el-checkbox v-if="activeTab === 'medication'" label="PendingReturnCancelled">异常取消待退药</el-checkbox>
          </el-checkbox-group>
        </div>

        <!-- 时间范围（仅药品申请显示） -->
        <div v-if="activeTab === 'medication'" class="filter-group">
          <span class="filter-label">时间:</span>
          <el-date-picker
            v-model="timeRange"
            type="datetimerange"
            range-separator="至"
            start-placeholder="开始时间"
            end-placeholder="结束时间"
            value-format="YYYY-MM-DDTHH:mm:ss"
            @change="loadApplications"
            class="time-picker"
          />
        </div>
      </div>

      <!-- 批量操作工具栏 -->
      <div v-if="selectedPatients.length > 0" class="batch-toolbar">
        <el-checkbox 
          v-model="selectAll"
          @change="handleSelectAllChange"
          :indeterminate="isIndeterminate"
        >
          全选 ({{ selectedCount }}/{{ applicationList.length }})
        </el-checkbox>
        
        <div class="batch-actions">
          <el-button 
            type="primary" 
            :disabled="selectedCount === 0"
            @click="handleBatchApply"
            class="action-btn"
          >
            批量申请 ({{ selectedCount }})
          </el-button>
        </div>
      </div>

      <!-- 申请项列表 -->
      <div v-if="!loading && applicationList.length > 0" class="application-list">
        <div 
          v-for="item in sortedApplications" 
          :key="item.relatedId"
          class="application-item"
        >
          <!-- 多选框 -->
          <el-checkbox 
            v-model="item.selected" 
            @change="handleItemSelectChange"
          />
          
          <!-- 申请内容 -->
          <div class="application-content">
            <!-- 患者信息（多选模式时显示） -->
            <div v-if="enableMultiSelect" class="application-patient-tag">
              <span class="patient-bed-tag">{{ item.bedId }}</span>
              <span class="patient-name-tag">{{ item.patientName }}</span>
            </div>

            <!-- 申请头部 -->
            <div class="application-header">
                            
              <!-- 状态标签 -->
              <el-tag 
                :type="getStatusColor(item.status)" 
                size="small"
                class="status-tag"
              >
                {{ getStatusText(item.status) }}
              </el-tag>
              <!-- 医嘱类型标签（长期/临时） -->
              <el-tag 
                :type="item.isLongTerm ? 'primary' : 'warning'" 
                size="small"
              >
                {{ item.isLongTerm ? '长期' : '临时' }}
              </el-tag>
              
              <!-- 医嘱分类标签（药品/检查/手术） -->
              <el-tag 
                :type="getOrderTypeColor(item.orderType)" 
                size="small"
              >
                {{ getOrderTypeName(item.orderType) }}
              </el-tag>
              
              <!-- 任务ID -->
              <span class="task-id">#{{ item.relatedId }}</span>
              
              <!-- 主要内容：药品申请显示 "计划时间 - 第一个药品" -->
              <!-- 手术类药品申请显示 "手术日期 - 手术名称" -->
              <span v-if="activeTab === 'medication' && item.medications && item.medications.length > 0" class="order-main-text">
                <template v-if="item.orderType === 'Surgical' && item.surgeryName">
                  {{ formatDateTime(item.surgeryScheduleTime || item.plannedStartTime) }} - {{ item.surgeryName }}
                </template>
                <template v-else>
                  {{ formatDateTime(item.plannedStartTime) }} - {{ item.medications[0].drugName }}{{ item.medications.length > 1 ? '等' : '' }}
                </template>
              </span>
              <span v-else class="order-main-text">{{ item.displayText }}</span>
              
              <!-- 检查来源（仅检查类） -->
              <span v-if="item.inspectionSource" class="inspection-source">
                · {{ item.inspectionSource }}
              </span>

              
              <!-- 加急标识 -->
              <span v-if="item.isUrgent" class="urgent-badge">🔥 加急</span>
            </div>

            <!-- 药品申请详情 -->
            <div v-if="activeTab === 'medication' && item.medications" class="application-details">
              <div class="detail-section">
                <span class="detail-label">药品:</span>
                <div class="drug-list">
                  <div v-for="(drug, idx) in item.medications" :key="idx" class="drug-item">
                    <span class="drug-name">{{ drug.drugName }}</span>
                    <span class="drug-spec">{{ drug.specification }}</span>
                    <span class="drug-dose">{{ drug.dosage }}</span>
                    <span v-if="drug.note" class="drug-note">({{ drug.note }})</span>
                  </div>
                </div>
              </div>

              <div class="detail-section">
                <span class="detail-label">时间策略:</span>
                <span class="detail-value">{{ formatTimingStrategy(item) }}</span>
              </div>

              <div class="detail-section">
                <span class="detail-label">用法:</span>
                <span class="detail-value">{{ formatUsageRoute(item.usageRoute) }}</span>
              </div>

              <div class="application-meta">
                <span>创建: {{ formatDateTime(item.createTime) }}</span>
                <span v-if="item.applyTime">申请: {{ formatDateTime(item.applyTime) }}</span>
                <span v-if="item.applyNurseName">护士: {{ item.applyNurseName }}</span>
              </div>
            </div>

            <!-- 检查申请详情 -->
            <div v-if="activeTab === 'inspection' && item.inspectionInfo" class="application-details">
              <div class="detail-section">
                <span class="detail-label">检查项:</span>
                <span class="detail-value">{{ item.inspectionInfo.itemName }}</span>
              </div>

              <div v-if="item.inspectionInfo.location" class="detail-section">
                <span class="detail-label">检查地点:</span>
                <span class="detail-value">{{ item.inspectionInfo.location }}</span>
              </div>

              <div v-if="item.inspectionInfo.precautions" class="detail-section">
                <span class="detail-label">注意事项:</span>
                <span class="detail-value">{{ item.inspectionInfo.precautions }}</span>
              </div>

              <div v-if="item.inspectionInfo.appointmentTime" class="detail-section">
                <span class="detail-label">预约时间:</span>
                <span class="detail-value">{{ formatDateTime(item.inspectionInfo.appointmentTime) }}</span>
              </div>

              <div v-if="item.inspectionInfo.appointmentPlace" class="detail-section">
                <span class="detail-label">预约地点:</span>
                <span class="detail-value">{{ item.inspectionInfo.appointmentPlace }}</span>
              </div>

              <div v-if="item.remarks" class="detail-section">
                <span class="detail-label">备注:</span>
                <span class="detail-value">{{ item.remarks }}</span>
              </div>

              <div class="application-meta">
                <span>创建: {{ formatDateTime(item.createTime) }}</span>
                <span v-if="item.applyTime">申请: {{ formatDateTime(item.applyTime) }}</span>
                <span v-if="item.applyNurseName">护士: {{ item.applyNurseName }}</span>
              </div>
            </div>
          </div>

          <!-- 操作按钮区（仅待申请状态显示） -->
          <div v-if="item.status === 'Applying'" class="application-actions">
            <!-- 加急选项 -->
            <el-checkbox v-model="item.isUrgent" class="urgent-checkbox">
              加急
            </el-checkbox>

            <!-- 申请按钮 -->
            <el-button 
              type="primary" 
              @click="handleSingleApply(item)"
              class="action-btn-small"
            >
              申请
            </el-button>
          </div>

          <!-- 已申请状态显示撤销申请按钮 -->
          <div v-else-if="item.status === 'Applied'" class="application-actions">
            <el-button 
              type="warning" 
              @click="handleCancelApplication(item)"
              class="action-btn-small"
            >
              撤销申请
            </el-button>
          </div>

          <!-- 已确认状态显示退药/取消安排按钮 -->
          <div v-else-if="item.status === 'AppliedConfirmed'" class="application-actions">
            <el-button 
              type="danger" 
              @click="handleReturnMedication(item)"
              class="action-btn-small"
            >
              {{ item.orderType === 'Inspection' || item.orderType === 'InspectionOrder' ? '取消安排' : '退药' }}
            </el-button>
          </div>

          <!-- 待退药/取消状态显示确认按钮 -->
          <div v-else-if="item.status === 'PendingReturn'" class="application-actions">
            <el-tag type="danger" size="small" class="return-notice">
              {{ item.orderType === 'Inspection' || item.orderType === 'InspectionOrder' ? '需要取消' : '需要退药' }}
            </el-tag>
            <el-button 
              type="primary" 
              @click="handleConfirmReturn(item)"
              class="action-btn-small"
            >
              {{ item.orderType === 'Inspection' || item.orderType === 'InspectionOrder' ? '确认取消' : '确认退药' }}
            </el-button>
          </div>

          <!-- 异常取消待退药状态显示确认退药按钮 -->
          <div v-else-if="item.status === 'PendingReturnCancelled'" class="application-actions">
            <el-tag type="danger" size="small" class="return-notice">
              任务已取消，{{ item.orderType === 'Inspection' || item.orderType === 'InspectionOrder' ? '需要取消安排' : '需要退药' }}
            </el-tag>
            <el-button 
              type="primary" 
              @click="handleConfirmCancelledReturn(item)"
              class="action-btn-small"
            >
              {{ item.orderType === 'Inspection' || item.orderType === 'InspectionOrder' ? '确认取消' : '确认退药' }}
            </el-button>
          </div>
        </div>
      </div>

      <!-- 加载状态 -->
      <div v-if="loading" class="loading-state">
        <el-icon class="is-loading"><Loading /></el-icon>
        <p>加载中...</p>
      </div>

    </div>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { Loading, InfoFilled } from '@element-plus/icons-vue';
import PatientListPanel from '@/components/PatientListPanel.vue';
import PatientInfoBar from '@/components/PatientInfoBar.vue';
import { usePatientData } from '@/composables/usePatientData';
import { 
  getMedicationApplications,
  getInspectionApplications,
  submitMedicationApplication,
  submitInspectionApplication,
  cancelMedicationApplication,
  cancelInspectionApplication,
  requestReturnMedication,
  confirmReturnMedication,
  confirmCancelledReturn
} from '@/api/orderApplication';

// 使用患者数据组合
const { 
  patientList,
  selectedPatient, 
  selectedPatients,
  currentScheduledWardId,
  enableMultiSelect,
  selectSinglePatient,
  togglePatientSelection,
  toggleMultiSelectMode,
  initializePatientData,
  getCurrentNurse
} = usePatientData();

// Tab状态
const activeTab = ref('medication'); // 'medication' | 'inspection'

// 筛选条件
const statusFilter = ref(['Applying']); // 默认显示待申请
const timeRange = ref(null); // [startTime, endTime]
const sortBy = ref('time'); // 'time' | 'patient'

// 申请列表数据
const applicationList = ref([]);
const loading = ref(false);

// 待申请数量统计（用于红点提示）
const pendingMedicationCount = ref(0);
const pendingInspectionCount = ref(0);

// 多选相关
const selectAll = ref(false);
const isIndeterminate = computed(() => {
  const count = selectedCount.value;
  return count > 0 && count < applicationList.value.length;
});

const selectedCount = computed(() => {
  return applicationList.value.filter(item => item.selected).length;
});

// Tab切换处理
const handleTabClick = (tab) => {
  if (activeTab.value === tab) return;
  activeTab.value = tab;
  // 切换tab时重置筛选条件
  statusFilter.value = ['Applying'];
  timeRange.value = null;
  loadApplications();
};

// 排序方式变化处理
const handleSortChange = (newSortBy) => {
  sortBy.value = newSortBy;
};

// 监听患者选择变化（单选模式）
watch(selectedPatient, async () => {
  if (!enableMultiSelect.value && selectedPatient.value) {
    loadApplications();
    // 更新当前患者的待申请数量（用于红点显示）
    await updateCurrentPatientPendingCount();
  } else if (!enableMultiSelect.value && !selectedPatient.value) {
    applicationList.value = [];
    pendingMedicationCount.value = 0;
    pendingInspectionCount.value = 0;
  }
});

// 监听多选患者列表变化（多选模式）
watch(selectedPatients, async () => {
  if (enableMultiSelect.value && selectedPatients.value.length > 0) {
    loadApplications();
  } else if (enableMultiSelect.value && selectedPatients.value.length === 0) {
    applicationList.value = [];
    pendingMedicationCount.value = 0;
    pendingInspectionCount.value = 0;
  }
}, { deep: true });

// 患者选择处理
const handlePatientSelect = (eventData) => {
  console.log('患者选择事件触发:', eventData);
  
  // PatientListPanel发射的是对象：{ patient, isMultiSelect, isCheckboxClick? }
  // 需要从中解构出实际的patient对象
  const { patient, isMultiSelect } = eventData;
  
  if (isMultiSelect) {
    // 多选模式：切换选中状态
    togglePatientSelection(patient);
  } else {
    // 单选模式：选中单个患者
    selectSinglePatient(patient);
  }
  
  // 注意：不需要手动调用 loadApplications()
  // 因为 watch(selectedPatient) 会自动触发加载
};

// 组件挂载时初始化
onMounted(async () => {
  // 设置默认时间范围：前一天到后一天（中国时间）
  const now = new Date();
  const yesterday = new Date(now);
  yesterday.setDate(yesterday.getDate() - 1);
  yesterday.setHours(0, 0, 0, 0);
  
  const tomorrow = new Date(now);
  tomorrow.setDate(tomorrow.getDate() + 1);
  tomorrow.setHours(23, 59, 59, 999);
  
  // 格式化为 YYYY-MM-DDTHH:mm:ss 格式
  const formatToDateTimeLocal = (date) => {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    const hours = String(date.getHours()).padStart(2, '0');
    const minutes = String(date.getMinutes()).padStart(2, '0');
    const seconds = String(date.getSeconds()).padStart(2, '0');
    return `${year}-${month}-${day}T${hours}:${minutes}:${seconds}`;
  };
  
  timeRange.value = [
    formatToDateTimeLocal(yesterday),
    formatToDateTimeLocal(tomorrow)
  ];
  
  await initializePatientData();
  // 初始化后更新所有患者的待申请数量
  await updateAllPatientsPendingCount();
});

// 更新单个患者的待申请数量
const updatePatientPendingCount = async (patientId) => {
  try {
    // 获取该患者的待申请项数量（状态为Applying）
    const medicationRequest = {
      applicationType: 'Medication',
      patientIds: [patientId],
      statusFilter: ['Applying']
    };
    const inspectionRequest = {
      applicationType: 'Inspection',
      patientIds: [patientId],
      statusFilter: ['Applying']
    };

    const [medicationRes, inspectionRes] = await Promise.all([
      getMedicationApplications(medicationRequest).catch(() => []),
      getInspectionApplications(inspectionRequest).catch(() => [])
    ]);

    const totalPending = 
      (Array.isArray(medicationRes) ? medicationRes.length : 0) +
      (Array.isArray(inspectionRes) ? inspectionRes.length : 0);

    // 更新患者列表中的待申请数量
    const patient = patientList.value.find(p => p.patientId === patientId);
    if (patient) {
      patient.pendingApplicationCount = totalPending;
    }
  } catch (error) {
    console.error('更新患者待申请数量失败:', error);
  }
};

// 更新当前选中患者的待申请数量（用于红点显示）
const updateCurrentPatientPendingCount = async () => {
  if (!selectedPatient.value) {
    pendingMedicationCount.value = 0;
    pendingInspectionCount.value = 0;
    return;
  }

  try {
    const medicationRequest = {
      applicationType: 'Medication',
      patientIds: [selectedPatient.value.patientId],
      statusFilter: ['Applying']
    };
    const inspectionRequest = {
      applicationType: 'Inspection',
      patientIds: [selectedPatient.value.patientId],
      statusFilter: ['Applying']
    };

    const [medicationRes, inspectionRes] = await Promise.all([
      getMedicationApplications(medicationRequest).catch(() => []),
      getInspectionApplications(inspectionRequest).catch(() => [])
    ]);

    // 更新标签红点的数量
    pendingMedicationCount.value = Array.isArray(medicationRes) ? medicationRes.length : 0;
    pendingInspectionCount.value = Array.isArray(inspectionRes) ? inspectionRes.length : 0;
  } catch (error) {
    console.error('更新当前患者待申请数量失败:', error);
  }
};

// 更新所有患者的待申请数量
const updateAllPatientsPendingCount = async () => {
  if (patientList.value.length === 0) return;
  
  try {
    // 批量获取所有患者的待申请数量
    const patientIds = patientList.value.map(p => p.patientId);
    
    const medicationRequest = {
      applicationType: 'Medication',
      patientIds: patientIds,
      statusFilter: ['Applying']
    };
    const inspectionRequest = {
      applicationType: 'Inspection',
      patientIds: patientIds,
      statusFilter: ['Applying']
    };

    const [medicationRes, inspectionRes] = await Promise.all([
      getMedicationApplications(medicationRequest).catch(() => []),
      getInspectionApplications(inspectionRequest).catch(() => [])
    ]);

    // 统计每个患者的待申请数量
    const countMap = new Map();
    
    if (Array.isArray(medicationRes)) {
      medicationRes.forEach(item => {
        const count = countMap.get(item.patientId) || 0;
        countMap.set(item.patientId, count + 1);
      });
    }
    
    if (Array.isArray(inspectionRes)) {
      inspectionRes.forEach(item => {
        const count = countMap.get(item.patientId) || 0;
        countMap.set(item.patientId, count + 1);
      });
    }

    // 更新患者列表
    patientList.value.forEach(patient => {
      patient.pendingApplicationCount = countMap.get(patient.patientId) || 0;
    });
    
    console.log('✅ 已更新所有患者的待申请数量');
  } catch (error) {
    console.error('批量更新待申请数量失败:', error);
  }
};

// 操作成功后刷新患者列表和红点（统一刷新方法）
const refreshAfterAction = async () => {
  // 1. 刷新任务列表
  await loadApplications();
  
  // 2. 刷新患者列表中的数字徽章
  if (enableMultiSelect.value && selectedPatients.value.length > 0) {
    // 多选模式：更新所有选中患者的待申请数量
    await Promise.all(
      selectedPatients.value.map(patient => updatePatientPendingCount(patient.patientId))
    );
  } else if (selectedPatient.value) {
    // 单选模式：更新当前患者的待申请数量
    await updatePatientPendingCount(selectedPatient.value.patientId);
  }
  
  // 3. 刷新导航栏红点（药品申请和检查申请的未完成标识）
  await updateCurrentPatientPendingCount();
};

// 加载申请列表
const loadApplications = async () => {
  // 多选模式：检查selectedPatients
  // 单选模式：检查selectedPatient
  const hasPatients = enableMultiSelect.value 
    ? selectedPatients.value.length > 0 
    : selectedPatient.value !== null;
  
  if (!hasPatients) {
    applicationList.value = [];
    return;
  }

  // 如果没有选中任何状态，直接返回空列表
  if (!statusFilter.value || statusFilter.value.length === 0) {
    applicationList.value = [];
    return;
  }

  loading.value = true;
  try {
    const currentNurse = getCurrentNurse();
    if (!currentNurse) {
      ElMessage.error('未找到当前护士信息');
      return;
    }

    // 获取患者ID列表
    const patientIds = enableMultiSelect.value
      ? selectedPatients.value.map(p => p.patientId)
      : [selectedPatient.value.patientId];

    // 构造请求参数（与后端DTO匹配）
    const requestData = {
      applicationType: activeTab.value === 'medication' ? 'Medication' : 'Inspection',
      patientIds: patientIds
    };

    // 添加状态筛选
    requestData.statusFilter = statusFilter.value;

    // 仅药品申请时添加时间范围参数
    // 需要将本地时间转换为UTC时间（PostgreSQL要求）
    if (activeTab.value === 'medication' && timeRange.value && timeRange.value.length === 2) {
      if (timeRange.value[0]) {
        // timeRange.value[0] 格式: "2025-12-22T08:00:00" (本地时间)
        // 需要转换为 "2025-12-22T00:00:00Z" (UTC时间)
        const localDate = new Date(timeRange.value[0]);
        requestData.startTime = localDate.toISOString(); // 自动转为UTC并添加Z后缀
      }
      if (timeRange.value[1]) {
        const localDate = new Date(timeRange.value[1]);
        requestData.endTime = localDate.toISOString();
      }
    }

    console.log('📤 发送申请列表请求:', requestData);
    console.log('📤 请求JSON:', JSON.stringify(requestData));

    let response;
    if (activeTab.value === 'medication') {
      response = await getMedicationApplications(requestData);
    } else {
      response = await getInspectionApplications(requestData);
    }

    console.log('📥 收到申请列表响应:', response);

    // 后端直接返回数组，不是 { success, data } 格式
    if (Array.isArray(response)) {
      applicationList.value = response.map(item => ({
        ...item,
        selected: false,
        isUrgent: item.isUrgent || false
      }));
      console.log('✅ 成功加载', applicationList.value.length, '条申请记录');
      
      // 更新患者的待申请数量
      if (enableMultiSelect.value) {
        // 多选模式：更新所有选中患者的待申请数量
        selectedPatients.value.forEach(patient => {
          updatePatientPendingCount(patient.patientId);
        });
      } else if (selectedPatient.value) {
        // 单选模式：更新当前患者的待申请数量
        updatePatientPendingCount(selectedPatient.value.patientId);
      }
    } else if (response && response.success) {
      // 兼容可能的标准格式响应
      applicationList.value = (response.data || []).map(item => ({
        ...item,
        selected: false,
        isUrgent: item.isUrgent || false
      }));
    } else {
      ElMessage.error(response?.message || '加载申请列表失败');
      applicationList.value = [];
    }
  } catch (error) {
    console.error('加载申请列表失败:', error);
    console.error('错误详情:', {
      message: error.message,
      response: error.response?.data,
      status: error.response?.status
    });
    
    // 显示详细的验证错误
    if (error.response?.data?.errors) {
      console.error('验证错误详情:', error.response.data.errors);
      const errors = error.response.data.errors;
      const errorMessages = Object.entries(errors)
        .map(([field, messages]) => `${field}: ${Array.isArray(messages) ? messages.join(', ') : messages}`)
        .join('\n');
      ElMessage.error(`验证失败:\n${errorMessages}`);
    } else {
      const errorMsg = error.response?.data?.title 
        || error.response?.data?.message 
        || error.message 
        || '加载申请列表失败';
      ElMessage.error(errorMsg);
    }
    
    applicationList.value = [];
  } finally {
    loading.value = false;
  }
};

// 排序后的申请列表
const sortedApplications = computed(() => {
  const list = [...applicationList.value];
  
  // 多选模式下，支持按患者分组排序
  if (enableMultiSelect.value && sortBy.value === 'patient') {
    return list.sort((a, b) => {
      // 先按床位号排序
      const bedCompare = a.bedId.localeCompare(b.bedId);
      if (bedCompare !== 0) return bedCompare;
      // 同一患者按计划开始时间排序
      return new Date(a.plannedStartTime) - new Date(b.plannedStartTime);
    });
  }
  
  switch (sortBy.value) {
    case 'time':
    case 'createTime':
      // 按计划开始时间从早到晚排序（升序）
      return list.sort((a, b) => new Date(a.plannedStartTime) - new Date(b.plannedStartTime));
    case 'bedId':
      return list.sort((a, b) => (a.bedId || '').localeCompare(b.bedId || ''));
    case 'status':
      const statusOrder = { Applying: 0, Applied: 1, AppliedConfirmed: 2 };
      return list.sort((a, b) => statusOrder[a.status] - statusOrder[b.status]);
    default:
      return list;
  }
});

// 全选处理
const handleSelectAllChange = (value) => {
  applicationList.value.forEach(item => {
    if (item.status === 'Applying') { // 仅可申请待申请状态的项
      item.selected = value;
    }
  });
};

// 单项选择变化
const handleItemSelectChange = () => {
  const selectableCount = applicationList.value.filter(item => item.status === 'Applying').length;
  const selectedApplyingCount = applicationList.value.filter(item => item.status === 'Applying' && item.selected).length;
  selectAll.value = selectableCount > 0 && selectedApplyingCount === selectableCount;
};

// 单个申请
const handleSingleApply = async (item) => {
  const currentNurse = getCurrentNurse();
  if (!currentNurse) {
    ElMessage.error('未找到当前护士信息');
    return;
  }

  // 加急确认
  if (item.isUrgent) {
    try {
      await ElMessageBox.confirm(
        '您选择了加急申请，将优先处理。是否继续？',
        '加急申请确认',
        {
          confirmButtonText: '确认申请',
          cancelButtonText: '取消',
          type: 'warning',
          customClass: 'order-action-confirm'
        }
      );
    } catch {
      return; // 用户取消
    }
  }

  loading.value = true;
  try {
    let response;
    if (activeTab.value === 'medication') {
      response = await submitMedicationApplication({
        nurseId: currentNurse.staffId,  // ✅ 使用 staffId 字段
        taskIds: [item.relatedId],
        isUrgent: item.isUrgent,
        remarks: item.remarks || ''
      });
    } else {
      response = await submitInspectionApplication({
        nurseId: currentNurse.staffId,  // ✅ 使用 staffId 字段
        taskIds: [item.relatedId],  // ✅ 使用 taskIds 而不是 orderIds
        isUrgent: item.isUrgent,
        remarks: item.remarks || ''
      });
    }

    if (response.success) {
      ElMessage.success('申请成功');
      await refreshAfterAction(); // 刷新列表、患者徽章和导航栏红点
    } else {
      ElMessage.error(response.message || '申请失败');
    }
  } catch (error) {
    console.error('申请失败:', error);
    ElMessage.error('申请失败');
  } finally {
    loading.value = false;
  }
};

// 批量申请
const handleBatchApply = async () => {
  const selectedItems = applicationList.value.filter(item => item.selected && item.status === 'Applying');
  
  if (selectedItems.length === 0) {
    ElMessage.warning('请至少选择一项');
    return;
  }

  const currentNurse = getCurrentNurse();
  if (!currentNurse) {
    ElMessage.error('未找到当前护士信息');
    return;
  }

  // 分离加急和非加急申请
  const urgentItems = selectedItems.filter(item => item.isUrgent);
  const normalItems = selectedItems.filter(item => !item.isUrgent);

  // 加急确认
  if (urgentItems.length > 0) {
    try {
      await ElMessageBox.confirm(
        `您选择了 ${selectedItems.length} 项申请，其中 ${urgentItems.length} 项为加急。是否继续？`,
        '批量申请确认',
        {
          confirmButtonText: '确认申请',
          cancelButtonText: '取消',
          type: 'warning',
          customClass: 'order-action-confirm'
        }
      );
    } catch {
      return; // 用户取消
    }
  }

  loading.value = true;
  try {
    let totalSuccess = 0;
    const responses = [];

    // 分别提交加急和非加急申请
    if (urgentItems.length > 0) {
      if (activeTab.value === 'medication') {
        const response = await submitMedicationApplication({
          nurseId: currentNurse.staffId,
          taskIds: urgentItems.map(item => item.relatedId),
          isUrgent: true,
          remarks: '批量申请（加急）'
        });
        responses.push(response);
        if (response.success) {
          totalSuccess += response.processedIds?.length || urgentItems.length;
        }
      } else {
        const response = await submitInspectionApplication({
          nurseId: currentNurse.staffId,
          taskIds: urgentItems.map(item => item.relatedId),
          isUrgent: true,
          remarks: '批量申请（加急）'
        });
        responses.push(response);
        if (response.success) {
          totalSuccess += response.processedIds?.length || urgentItems.length;
        }
      }
    }

    if (normalItems.length > 0) {
      if (activeTab.value === 'medication') {
        const response = await submitMedicationApplication({
          nurseId: currentNurse.staffId,
          taskIds: normalItems.map(item => item.relatedId),
          isUrgent: false,
          remarks: '批量申请'
        });
        responses.push(response);
        if (response.success) {
          totalSuccess += response.processedIds?.length || normalItems.length;
        }
      } else {
        const response = await submitInspectionApplication({
          nurseId: currentNurse.staffId,
          taskIds: normalItems.map(item => item.relatedId),
          isUrgent: false,
          remarks: '批量申请'
        });
        responses.push(response);
        if (response.success) {
          totalSuccess += response.processedIds?.length || normalItems.length;
        }
      }
    }

    // 检查是否全部成功
    const allSuccess = responses.every(r => r.success);
    
    if (allSuccess) {
      ElMessage.success(`批量申请成功：${totalSuccess} 项`);
    } else {
      const failedCount = selectedItems.length - totalSuccess;
      ElMessage.warning(`部分申请成功：成功 ${totalSuccess} 项，失败 ${failedCount} 项`);
    }
    
    await refreshAfterAction(); // 刷新列表、患者徽章和导航栏红点
    selectAll.value = false;
  } catch (error) {
    console.error('批量申请失败:', error);
    ElMessage.error('批量申请失败');
  } finally {
    loading.value = false;
  }
};

// 撤销申请（Applied状态）
const handleCancelApplication = async (item) => {
  try {
    await ElMessageBox.confirm(
      '确定要撤销此申请吗？药房可能正在配药中。',
      '撤销申请确认',
      {
        confirmButtonText: '确认',
        cancelButtonText: '取消',
        type: 'warning',
        customClass: 'order-action-confirm'
      }
    );
  } catch {
    return; // 用户取消
  }

  const currentNurse = getCurrentNurse();
  if (!currentNurse) {
    ElMessage.error('未找到当前护士信息');
    return;
  }

  loading.value = true;
  try {
    const response = await cancelMedicationApplication({
      nurseId: currentNurse.staffId,
      ids: [item.relatedId],
      reason: '护士撤销申请'
    });

    if (response.success) {
      ElMessage.success('撤销成功');
      await refreshAfterAction(); // 刷新列表、患者徽章和导航栏红点
    } else {
      ElMessage.error(response.message || '撤销失败');
    }
  } catch (error) {
    console.error('撤销申请失败:', error);
    ElMessage.error('撤销申请失败');
  } finally {
    loading.value = false;
  }
};

// 申请退药/取消安排（AppliedConfirmed状态）
const handleReturnMedication = async (item) => {
  try {
    const isInspection = item.orderType === 'Inspection' || item.orderType === 'InspectionOrder';
    const { value: reason } = await ElMessageBox.prompt(
      isInspection ? '检查科室已安排，请输入取消原因：' : '药房已配好药，请输入退药原因：',
      isInspection ? '申请取消安排' : '申请退药',
      {
        confirmButtonText: isInspection ? '确认取消' : '确认退药',
        cancelButtonText: '取消',
        inputPattern: /\S+/,
        inputErrorMessage: isInspection ? '取消原因不能为空' : '退药原因不能为空',
        inputType: 'textarea'
      }
    );

    const currentNurse = getCurrentNurse();
    if (!currentNurse) {
      ElMessage.error('未找到当前护士信息');
      return;
    }

    loading.value = true;
    const response = await requestReturnMedication(
      item.relatedId,
      currentNurse.staffId,
      reason
    );

    if (response.success) {
      const isInspection = item.orderType === 'Inspection' || item.orderType === 'InspectionOrder';
      ElMessage.success(isInspection ? '取消申请已提交' : '退药申请已提交');
      await refreshAfterAction(); // 刷新列表、患者徽章和导航栏红点
    } else {
      const isInspection = item.orderType === 'Inspection' || item.orderType === 'InspectionOrder';
      ElMessage.error(response.message || (isInspection ? '取消申请失败' : '退药申请失败'));
    }
  } catch (error) {
    if (error !== 'cancel') {
      const isInspection = item.orderType === 'Inspection' || item.orderType === 'InspectionOrder';
      console.error(isInspection ? '申请取消失败:' : '申请退药失败:', error);
      ElMessage.error(isInspection ? '申请取消失败' : '申请退药失败');
    }
  } finally {
    loading.value = false;
  }
};

// 确认退药/取消（PendingReturn状态）
const handleConfirmReturn = async (item) => {
  try {
    const isInspection = item.orderType === 'Inspection' || item.orderType === 'InspectionOrder';
    await ElMessageBox.confirm(
      isInspection ? '确认取消该检查安排？取消后任务将被停止。' : '确认退回该药品？退药后任务将被停止。',
      isInspection ? '确认取消' : '确认退药',
      {
        confirmButtonText: isInspection ? '确认取消' : '确认退药',
        cancelButtonText: '取消',
        type: 'warning',
        customClass: 'order-action-confirm'
      }
    );

    const currentNurse = getCurrentNurse();
    if (!currentNurse) {
      ElMessage.error('未找到当前护士信息');
      return;
    }

    loading.value = true;
    const response = await confirmReturnMedication(
      item.relatedId,
      currentNurse.staffId
    );

    if (response.success) {
      const isInspection = item.orderType === 'Inspection' || item.orderType === 'InspectionOrder';
      ElMessage.success(isInspection ? '取消确认成功' : '退药确认成功');
      await refreshAfterAction(); // 刷新列表、患者徽章和导航栏红点
    } else {
      const isInspection = item.orderType === 'Inspection' || item.orderType === 'InspectionOrder';
      ElMessage.error(response.message || (isInspection ? '取消确认失败' : '退药确认失败'));
    }
  } catch (error) {
    if (error !== 'cancel') {
      const isInspection = item.orderType === 'Inspection' || item.orderType === 'InspectionOrder';
      console.error(isInspection ? '确认取消失败:' : '确认退药失败:', error);
      ElMessage.error(isInspection ? '确认取消失败' : '确认退药失败');
    }
  } finally {
    loading.value = false;
  }
};

// 确认异常取消退药（PendingReturnCancelled状态，将任务改为Incomplete）
const handleConfirmCancelledReturn = async (item) => {
  try {
    const isInspection = item.orderType === 'Inspection' || item.orderType === 'InspectionOrder';
    await ElMessageBox.confirm(
      isInspection 
        ? '确认该任务已取消安排？确认后任务将标记为异常状态。' 
        : '确认该任务已退药？确认后任务将标记为异常状态。',
      isInspection ? '确认取消' : '确认退药',
      {
        confirmButtonText: '确认',
        cancelButtonText: '取消',
        type: 'warning',
        customClass: 'order-action-confirm'
      }
    );

    const currentNurse = getCurrentNurse();
    if (!currentNurse) {
      ElMessage.error('未找到当前护士信息');
      return;
    }

    loading.value = true;
    const response = await confirmCancelledReturn(
      item.relatedId,
      currentNurse.staffId
    );

    if (response.success) {
      const isInspection = item.orderType === 'Inspection' || item.orderType === 'InspectionOrder';
      ElMessage.success(isInspection ? '取消确认成功，任务已标记为异常' : '退药确认成功，任务已标记为异常');
      await refreshAfterAction(); // 刷新列表、患者徽章和导航栏红点
    } else {
      const isInspection = item.orderType === 'Inspection' || item.orderType === 'InspectionOrder';
      ElMessage.error(response.message || (isInspection ? '确认失败' : '确认失败'));
    }
  } catch (error) {
    if (error !== 'cancel') {
      const isInspection = item.orderType === 'Inspection' || item.orderType === 'InspectionOrder';
      console.error(isInspection ? '确认取消失败:' : '确认退药失败:', error);
      ElMessage.error(isInspection ? '确认失败' : '确认失败');
    }
  } finally {
    loading.value = false;
  }
};

// 取消申请
const handleCancelApply = async (item) => {
  try {
    await ElMessageBox.confirm(
      '确定要取消此申请吗？',
      '取消申请确认',
      {
        confirmButtonText: '确认',
        cancelButtonText: '取消',
        type: 'warning',
        customClass: 'order-action-confirm'
      }
    );
  } catch {
    return; // 用户取消
  }

  const currentNurse = getCurrentNurse();
  if (!currentNurse) {
    ElMessage.error('未找到当前护士信息');
    return;
  }

  loading.value = true;
  try {
    let response;
    if (activeTab.value === 'medication') {
      response = await cancelMedicationApplication({
        nurseId: currentNurse.staffId,
        ids: [item.relatedId],
        reason: '护士取消'
      });
    } else {
      response = await cancelInspectionApplication({
        nurseId: currentNurse.staffId,
        ids: [item.relatedId],
        reason: '护士取消'
      });
    }

    if (response.success) {
      ElMessage.success('取消成功');
      await refreshAfterAction(); // 刷新列表、患者徽章和导航栏红点
    } else {
      ElMessage.error(response.message || '取消失败');
    }
  } catch (error) {
    console.error('取消申请失败:', error);
    ElMessage.error('取消失败');
  } finally {
    loading.value = false;
  }
};

// 医嘱类型颜色映射
const getOrderTypeColor = (orderType) => {
  const colorMap = {
    Medication: 'success',
    Inspection: 'info',
    Surgical: 'danger',
    Operation: 'warning'
  };
  return colorMap[orderType] || 'info';
};

// 医嘱类型名称映射
const getOrderTypeName = (orderType) => {
  const nameMap = {
    Medication: '药品',
    Inspection: '检查',
    Surgical: '手术',
    Operation: '操作',
    MedicationOrder: '药品',
    InspectionOrder: '检查',
    SurgicalOrder: '手术',
    OperationOrder: '操作',
    DischargeOrder: '出院',
    Discharge: '出院'
  };
  return nameMap[orderType] || orderType;
};

// 状态颜色映射
const getStatusColor = (status) => {
  const colorMap = {
    Applying: 'warning',
    Applied: 'primary',
    AppliedConfirmed: 'success',
    PendingReturn: 'danger',
    PendingReturnCancelled: 'danger'
  };
  return colorMap[status] || 'info';
};

// 状态文本映射
const getStatusText = (status) => {
  const textMap = {
    Applying: '待申请',
    Applied: '已申请',
    AppliedConfirmed: '已确认',
    PendingReturn: '待退药',
    PendingReturnCancelled: '异常取消待退药'
  };
  return textMap[status] || status;
};

// 格式化日期时间（自动将UTC时间转换为北京时间）
const formatDateTime = (dateString) => {
  if (!dateString) return '-';
  try {
    // 确保UTC时间字符串带有Z标识
    let utcString = dateString;
    if (!dateString.endsWith('Z') && !dateString.includes('+')) {
      utcString = dateString + 'Z';
    }
    const date = new Date(utcString);
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
    return dateString;
  }
};

// 格式化时间策略
const formatTimingStrategy = (item) => {
  if (!item.timingStrategy) return '-';
  
  switch (item.timingStrategy) {
    case 'IMMEDIATE':
      return '立即';
    
    case 'SPECIFIC':
      // 指定时间：显示开始时间
      return `指定时间: ${formatDateTime(item.startTime || item.plannedStartTime)}`;
    
    case 'SLOTS':
      // Slot 策略：显示开始结束时间 + 选定的slot中文
      const slotText = formatSlotsMask(item.smartSlotsMask);
      const intervalDaysText = item.intervalDays && item.intervalDays > 1 
        ? `每${item.intervalDays}天` 
        : '每天';
      return `${formatDateTime(item.startTime || item.plannedStartTime)} 至 ${formatDateTime(item.plantEndTime)} (${intervalDaysText} ${slotText})`;
    
    case 'CYCLIC':
      // Cycle 策略：显示开始结束时间 + 间隔时间
      const intervalText = formatIntervalHours(item.intervalHours);
      return `${formatDateTime(item.startTime || item.plannedStartTime)} 至 ${formatDateTime(item.plantEndTime)} (${intervalText})`;
    
    default:
      return item.timingStrategy;
  }
};

// 格式化间隔时间
const formatIntervalHours = (hours) => {
  if (!hours) return '未指定间隔';
  
  if (hours < 1) {
    const minutes = Math.round(hours * 60);
    return `每${minutes}分钟`;
  } else if (hours === 1) {
    return '每小时';
  } else if (hours % 24 === 0) {
    const days = hours / 24;
    return `每${days}天`;
  } else {
    return `每${hours}小时`;
  }
};

// 格式化 Slots 掩码
const formatSlotsMask = (mask) => {
  if (!mask) return '';
  
  // 根据掩码解析选定的时段
  // 假设 bit 0-7 分别代表：早晨、上午、中午、下午、晚上、深夜、凌晨、其他
  const slotNames = ['早晨', '上午', '中午', '下午', '晚上', '深夜', '凌晨', '其他'];
  const selectedSlots = [];
  
  for (let i = 0; i < slotNames.length; i++) {
    if (mask & (1 << i)) {
      selectedSlots.push(slotNames[i]);
    }
  }
  
  return selectedSlots.length > 0 ? selectedSlots.join('、') : '未指定时段';
};

// 格式化用法途径
const formatUsageRoute = (usageRoute) => {
  if (!usageRoute) return '-';
  
  const usageMap = {
    'PO': '口服',
    'Topical': '外用/涂抹',
    'IM': '肌内注射',
    'SC': '皮下注射',
    'IVP': '静脉推注',
    'IVGTT': '静脉滴注',
    'ST': '皮试'
  };
  
  return usageMap[usageRoute] || usageRoute;
};
</script>

<style scoped>
/* ==================== 主布局 ==================== */

/* ==================== 全局变量 ==================== */
.order-application {
  --primary-color: #409eff;
  --success-color: #67c23a;
  --warning-color: #e6a23c;
  --danger-color: #f56c6c;
  --info-color: #909399;
  
  --bg-page: #f4f7f9;
  --bg-card: #ffffff;
  --bg-secondary: #f9fafc;
  
  --border-color: #dcdfe6;
  --text-primary: #303133;
  --text-regular: #606266;
  --text-secondary: #909399;
  
  --radius-large: 8px;
  --radius-medium: 6px;
  --radius-small: 4px;
  --radius-round: 20px;

  display: grid;
  grid-template-columns: 250px 1fr;
  height: calc(100vh - 60px);
  background: var(--bg-page);
  gap: 20px;
  padding: 20px;
}

.work-area {
  background: var(--bg-card);
  border-radius: var(--radius-large);
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.08);
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

/* ==================== 未选择患者提示栏 ==================== */

.no-patient-bar {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 10px;
  padding: 20px;
  background: #f0f9ff;
  border-bottom: 1px solid #b3e0ff;
  color: var(--primary-color);
  font-size: 0.95rem;
}

.no-patient-bar .el-icon {
  font-size: 1.2rem;
}

/* ==================== 空状态工作区 ==================== */

.empty-work-area {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  color: var(--text-secondary);
}

.empty-work-area .empty-icon {
  font-size: 64px;
  margin-bottom: 16px;
  opacity: 0.5;
}

.empty-work-area p {
  font-size: 1.1rem;
  color: var(--text-secondary);
}

/* ==================== Tab导航栏 ==================== */

.tab-navigation {
  display: flex;
  gap: 0;
  background: #f8f9fa;
  border-bottom: 2px solid var(--border-color);
  padding: 0 20px;
}

.tab-item {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 16px 24px;
  cursor: pointer;
  font-size: 1rem;
  font-weight: 500;
  color: var(--text-secondary);
  position: relative;
  transition: all 0.3s;
  user-select: none;
}

.tab-item:hover {
  color: var(--primary-color);
  background: rgba(64, 158, 255, 0.05);
}

.tab-item.active {
  color: var(--primary-color);
  font-weight: 600;
}

.tab-item.active::after {
  content: '';
  position: absolute;
  bottom: -2px;
  left: 0;
  right: 0;
  height: 3px;
  background: var(--primary-color);
}

.tab-icon {
  font-size: 1.2rem;
}

.tab-label {
  font-size: 1rem;
}

/* 红点提示 */
.badge-dot {
  position: absolute;
  top: 10px;
  right: 10px;
  width: 8px;
  height: 8px;
  background: #f56c6c;
  border-radius: 50%;
  border: 2px solid white;
  box-shadow: 0 0 0 1px rgba(245, 108, 108, 0.3);
  animation: badge-pulse 2s ease-in-out infinite;
}

@keyframes badge-pulse {
  0%, 100% {
    opacity: 1;
    transform: scale(1);
  }
  50% {
    opacity: 0.8;
    transform: scale(1.1);
  }
}

/* ==================== 筛选工具栏 ==================== */

.filter-toolbar {
  display: flex;
  align-items: center;
  gap: 24px;
  padding: 15px 25px;
  background: white;
  border-bottom: 1px solid var(--border-color);
  flex-wrap: wrap;
}

.filter-group {
  display: flex;
  align-items: center;
  gap: 10px;
}

.filter-label {
  font-size: 0.9rem;
  font-weight: 600;
  color: var(--text-regular, #606266);
  white-space: nowrap;
}

.time-picker {
  width: 360px;
}

.sort-select {
  width: 140px;
}

.multi-select-btn {
  font-weight: 600;
}

/* ==================== 批量操作工具栏 ==================== */

.batch-toolbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 12px 20px;
  background: #f0f9ff;
  border-bottom: 1px solid #b3e0ff;
}

.batch-actions {
  display: flex;
  gap: 12px;
}

.action-btn {
  font-weight: 600;
  border-radius: var(--radius-small, 4px);
}

/* ==================== 申请项列表 ==================== */

.application-list {
  flex: 1;
  overflow-y: auto;
  padding: 0 25px 16px 25px;
  display: flex;
  flex-direction: column;
  gap: 16px;
  max-height: calc(100vh - 400px);
}

.application-item {
  display: flex;
  gap: 16px;
  padding: 20px;
  background: white;
  border: 1px solid var(--border-color, #e4e7ed);
  border-radius: var(--radius-medium, 8px);
  transition: all 0.3s;
}

.application-item:hover {
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.08);
  border-color: var(--primary-color, #409eff);
}

.application-content {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 12px;
}

/* ==================== 排序控制（多选模式） ==================== */

.sort-control {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-left: auto;
}

.sort-label {
  font-size: 0.9rem;
  color: var(--text-regular);
  font-weight: 500;
}

.sort-radio :deep(.el-radio-button__inner) {
  padding: 6px 15px;
  font-size: 0.85rem;
}

/* ==================== 患者标签（多选模式） ==================== */

.application-patient-tag {
  display: flex;
  gap: 8px;
  margin-bottom: 4px;
}

.patient-bed-tag {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  padding: 4px 12px;
  border-radius: 12px;
  font-size: 0.85rem;
  font-weight: 700;
  min-width: 60px;
  text-align: center;
}

.patient-name-tag {
  background: var(--primary-color, #409eff);
  color: white;
  padding: 4px 12px;
  border-radius: 12px;
  font-size: 0.85rem;
  font-weight: 600;
}

/* ==================== 申请头部 ==================== */

.application-header {
  display: flex;
  align-items: center;
  gap: 10px;
  flex-wrap: wrap;
}

.task-id {
  font-size: 0.85rem;
  font-weight: 700;
  color: var(--primary-color);
  background: #ecf5ff;
  padding: 2px 8px;
  border-radius: 4px;
  font-family: 'Courier New', monospace;
}

.order-main-text {
  font-size: 0.95rem;
  color: var(--text-primary, #303133);
  font-weight: 600;
  flex: 1;
  min-width: 150px;
}

.inspection-source {
  font-size: 0.85rem;
  color: var(--text-secondary, #909399);
  font-weight: 500;
}

.status-tag {
  margin-left: auto;
}

.application-id {
  font-size: 0.9rem;
  color: var(--text-secondary, #909399);
  font-weight: 500;
}

.urgent-badge {
  background: linear-gradient(135deg, #ff6b6b 0%, #ff4757 100%);
  color: white;
  padding: 4px 10px;
  border-radius: 12px;
  font-size: 0.8rem;
  font-weight: 700;
  animation: pulse 1.5s ease-in-out infinite;
}

@keyframes pulse {
  0%, 100% {
    transform: scale(1);
  }
  50% {
    transform: scale(1.05);
  }
}

/* ==================== 申请详情 ==================== */

.application-details {
  display: flex;
  flex-direction: column;
  gap: 8px;
  font-size: 0.85rem;
  line-height: 1.6;
}

.detail-section {
  display: flex;
  gap: 8px;
  font-size: 0.85rem;
  line-height: 1.6;
}

.detail-label {
  color: var(--text-secondary, #909399);
  min-width: 70px;
  font-weight: 500;
}

.detail-value {
  color: var(--text-regular, #606266);
  flex: 1;
}

/* ==================== 药品列表 ==================== */

.drug-list {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.drug-item {
  display: flex;
  gap: 8px;
  align-items: center;
  padding: 4px 0;
}

.drug-name {
  font-weight: 600;
  color: var(--primary-color, #409eff);
  font-size: 0.9rem;
}

.drug-spec {
  color: var(--text-secondary, #909399);
  font-size: 0.8rem;
}

.drug-dose {
  font-weight: 600;
  color: var(--success-color, #67c23a);
  font-size: 0.9rem;
}

.drug-note {
  color: var(--warning-color, #e6a23c);
  font-size: 0.8rem;
  font-style: italic;
}

/* ==================== 元数据 ==================== */

.application-meta {
  display: flex;
  gap: 16px;
  font-size: 0.8rem;
  color: var(--text-secondary, #909399);
  margin-top: 4px;
  padding-top: 8px;
  border-top: 1px dashed var(--border-color, #e4e7ed);
}

/* ==================== 操作区 ==================== */

.application-actions {
  display: flex;
  flex-direction: column;
  gap: 10px;
  justify-content: center;
  align-items: center;
  min-width: 90px;
}

.urgent-checkbox {
  font-weight: 600;
}

.action-btn-small {
  width: 80px !important;
  height: 36px !important;
  padding: 0 !important;
  font-size: 0.9rem !important;
  font-weight: 600 !important;
  border-radius: var(--radius-small, 4px) !important;
  transition: all 0.3s !important;
  display: flex !important;
  align-items: center !important;
  justify-content: center !important;
}

.action-btn-small:not(:disabled):hover {
  transform: translateY(-1px);
  box-shadow: 0 3px 8px rgba(0, 0, 0, 0.15);
}

.action-btn-small.el-button--primary {
  background: var(--primary-color, #409eff) !important;
  border-color: var(--primary-color, #409eff) !important;
}

.action-btn-small.el-button--primary:not(:disabled):hover {
  background: #66b1ff !important;
  border-color: #66b1ff !important;
}

.action-btn-small.el-button--warning {
  background: var(--warning-color, #e6a23c) !important;
  border-color: var(--warning-color, #e6a23c) !important;
}

.action-btn-small.el-button--warning:not(:disabled):hover {
  background: #f0c78a !important;
  border-color: #f0c78a !important;
}

/* ==================== 状态显示 ==================== */

.loading-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 60px 20px;
  color: var(--text-secondary, #909399);
  gap: 16px;
}

.loading-state .el-icon {
  font-size: 48px;
}

.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 60px 20px;
  color: var(--text-secondary, #909399);
}

.empty-icon {
  font-size: 64px;
  margin-bottom: 16px;
  opacity: 0.5;
}

.empty-state p {
  font-size: 1rem;
  color: var(--text-secondary, #909399);
}

/* ==================== 确认弹窗样式 ==================== */

:deep(.order-action-confirm) {
  width: 500px;
  max-width: 90vw;
}

:deep(.order-action-confirm .el-message-box__message) {
  line-height: 1.6;
}

/* ==================== 响应式 ==================== */

@media (max-width: 768px) {
  .order-application {
    grid-template-columns: 1fr;
  }

  .filter-toolbar {
    flex-direction: column;
    align-items: flex-start;
  }

  .time-picker {
    width: 100%;
  }
}
</style>
