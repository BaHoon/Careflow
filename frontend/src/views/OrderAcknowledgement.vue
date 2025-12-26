<template>
  <div class="order-acknowledgement">
    <!-- 左侧患者列表 -->
    <aside class="patient-panel" :class="{ collapsed: leftCollapsed }">
      <div class="panel-header">
        <span class="panel-title" v-show="!leftCollapsed">患者列表</span>
        <button @click="toggleLeft" class="collapse-btn" :title="leftCollapsed ? '展开' : '折叠'">
          {{ leftCollapsed ? '>' : '<' }}
        </button>
      </div>

      <div class="panel-content" v-show="!leftCollapsed">
        <!-- 搜索框 -->
        <div class="search-box">
          <el-input 
            v-model="patientSearch" 
            placeholder="搜索床号/姓名"
            clearable
            size="small"
          >
            <template #prefix>🔍</template>
          </el-input>
        </div>

        <!-- 筛选工具栏 -->
        <div class="filter-toolbar">
          <div class="filter-group">
            <el-select 
              v-model="wardFilter" 
              placeholder="病区筛选" 
              clearable 
              size="small"
              class="ward-filter"
            >
              <el-option label="全部病区" value=""></el-option>
              <el-option 
                v-for="ward in wardOptions" 
                :key="ward.wardId"
                :label="ward.wardName"
                :value="ward.wardId"
              >
                <span>{{ ward.wardName }}</span>
                <span v-if="ward.isMyWard" class="my-ward-tag">★ 我负责</span>
              </el-option>
            </el-select>
          </div>
          
          <div class="filter-group">
            <el-checkbox 
              v-model="showOnlyPending" 
              size="small"
              class="pending-filter"
            >
              仅显示待签收
            </el-checkbox>
          </div>

          <div class="filter-group">
            <el-checkbox 
              v-model="enableMultiSelect" 
              size="small"
              class="multi-select-toggle"
              @change="handleMultiSelectToggle"
            >
              多选模式
            </el-checkbox>
          </div>
        </div>

        <!-- 患者列表 -->
        <div class="patient-list">
          <div 
            v-for="patient in filteredPatients" 
            :key="patient.patientId"
            :class="['patient-card', { 
              active: isPatientSelected(patient),
              'has-pending': patient.unacknowledgedCount > 0,
              'my-ward': isMyWard(patient.wardId)
            }]"
            @click="selectPatient(patient)"
          >
            <!-- 多选模式复选框 -->
            <el-checkbox 
              v-if="enableMultiSelect"
              :model-value="isPatientSelected(patient)"
              @click.stop
              @change="togglePatientSelection(patient)"
              class="patient-checkbox"
            />
            
            <div class="bed-badge">{{ patient.bedId }}</div>
            <div class="patient-basic">
              <span class="p-name">{{ patient.patientName }}</span>
              <span class="p-info">{{ patient.gender }} {{ patient.age }}岁</span>
            </div>
            <div class="patient-meta">
              <span class="p-care">护理{{ patient.nursingGrade }}级</span>
            </div>
            <!-- 红点标注 -->
            <span v-if="patient.unacknowledgedCount > 0" class="pending-badge">
              {{ patient.unacknowledgedCount }}
            </span>
          </div>

          <!-- 空状态 -->
          <div v-if="filteredPatients.length === 0" class="empty-state">
            <div class="empty-icon">👥</div>
            <p>暂无患者</p>
          </div>
        </div>
      </div>

      <!-- 折叠状态显示 -->
      <div class="collapsed-content" v-show="leftCollapsed">
        <div class="collapsed-text">患者列表</div>
        <div class="patient-count">{{ patientList.length }}人</div>
      </div>
    </aside>

    <!-- 右侧工作区 -->
    <section class="work-area">
      <!-- 患者信息栏 -->
      <header class="patient-info-bar" v-if="selectedPatients.length > 0">
        <!-- 单选模式 -->
        <template v-if="!enableMultiSelect">
          <div class="patient-badge">{{ selectedPatients[0].bedId }}</div>
          <div class="patient-details">
            <span class="name">{{ selectedPatients[0].patientName }}</span>
            <span class="meta">
              {{ selectedPatients[0].gender }} | {{ selectedPatients[0].age }}岁 | {{ selectedPatients[0].weight }}kg
            </span>
            <span class="tag">护理{{ selectedPatients[0].nursingGrade }}级</span>
          </div>
        </template>
        
        <!-- 多选模式 -->
        <template v-else>
          <div class="multi-patient-header">
            <div class="selected-count">
              <span class="count-badge">{{ selectedPatients.length }}</span>
              <span class="count-text">位患者</span>
            </div>
            <div class="patient-badges">
              <span 
                v-for="p in selectedPatients.slice(0, 5)" 
                :key="p.patientId"
                class="mini-badge"
                :title="`${p.bedId} ${p.patientName}`"
              >
                {{ p.bedId }}
              </span>
              <span v-if="selectedPatients.length > 5" class="more-badge">
                +{{ selectedPatients.length - 5 }}
              </span>
            </div>
            
            <!-- 排序控制 -->
            <div class="sort-control">
              <span class="sort-label">排序:</span>
              <el-radio-group v-model="orderSortBy" size="small" class="sort-radio">
                <el-radio-button label="time">时间</el-radio-button>
                <el-radio-button label="patient">患者</el-radio-button>
              </el-radio-group>
            </div>
          </div>
        </template>
      </header>

      <!-- 提示信息：未选择患者 -->
      <div v-if="selectedPatients.length === 0" class="empty-work-area">
        <div class="empty-icon">📋</div>
        <p>请从左侧选择患者查看待签收医嘱</p>
      </div>

      <!-- Tab切换: 新开医嘱 / 停止医嘱 -->
      <el-tabs v-if="selectedPatients.length > 0" v-model="activeTab" @tab-click="handleTabClick" class="order-tabs">
        <el-tab-pane :label="`新开医嘱 (${sortedNewOrders.length})`" name="new">
          <div v-if="sortedNewOrders.length > 0" class="order-list">
            <!-- 批量操作栏 -->
            <div class="batch-toolbar">
              <el-checkbox 
                v-model="selectAllNew" 
                @change="handleSelectAllNew"
                :indeterminate="isIndeterminateNew"
              >
                全选
              </el-checkbox>
              <div class="batch-actions">
                <el-button 
                  type="primary" 
                  :disabled="selectedNewCount === 0"
                  @click="acknowledgeBatch"
                  class="action-btn"
                >
                  批量签收 ({{ selectedNewCount }})
                </el-button>
                <el-button 
                  type="danger"
                  :disabled="selectedNewCount === 0"
                  @click="rejectBatch"
                  class="action-btn"
                >
                  批量退回 ({{ selectedNewCount }})
                </el-button>
              </div>
            </div>

            <!-- 医嘱列表 -->
            <div v-for="order in sortedNewOrders" 
                 :key="order.orderId"
                 class="order-item">
              <el-checkbox v-model="order.selected" @change="handleOrderSelectChange" />
              
              <div class="order-content">
                <!-- 多选模式下显示患者信息 -->
                <div v-if="enableMultiSelect" class="order-patient-tag">
                  <span class="patient-bed-tag">{{ order.bedId }}</span>
                  <span class="patient-name-tag">{{ order.patientName }}</span>
                </div>
                
                <!-- 医嘱头部 -->
                <div class="order-header">
                  <el-tag 
                    :type="order.isLongTerm ? 'primary' : 'warning'" 
                    size="small"
                  >
                    {{ order.isLongTerm ? '长期' : '临时' }}
                  </el-tag>
                  <el-tag 
                    :type="getOrderTypeColor(order.orderType)" 
                    size="small"
                  >
                    {{ getOrderTypeName(order.orderType) }}
                  </el-tag>
                  <span class="order-text">{{ order.displayText }}</span>
                </div>

                <!-- 医嘱详情 -->
                <div class="order-details">
                  <!-- 药品明细 -->
                  <div v-if="order.items && order.items.length > 0" class="detail-section">
                    <span class="detail-label">药品:</span>
                    <div class="drug-list">
                      <div v-for="(item, idx) in order.items" :key="idx" class="drug-item">
                        <span class="drug-name">{{ item.drugName }}</span>
                        <span class="drug-spec">{{ item.specification }}</span>
                        <span class="drug-dose">{{ item.dosage }}</span>
                        <span v-if="item.note" class="drug-note">({{ item.note }})</span>
                      </div>
                    </div>
                  </div>

                  <!-- 时间策略 -->
                  <div v-if="order.timingStrategy" class="detail-section">
                    <span class="detail-label">策略:</span>
                    <span class="detail-value">{{ getTimingStrategyText(order) }}</span>
                  </div>

                  <!-- 给药途径 -->
                  <div v-if="order.usageRoute" class="detail-section">
                    <span class="detail-label">途径:</span>
                    <span class="detail-value">{{ getUsageRouteText(order.usageRoute) }}</span>
                  </div>

                  <!-- 检查地点 -->
                  <div v-if="order.location" class="detail-section">
                    <span class="detail-label">地点:</span>
                    <span class="detail-value">{{ order.location }}</span>
                  </div>

                  <!-- 手术时间 -->
                  <div v-if="order.scheduleTime" class="detail-section">
                    <span class="detail-label">手术时间:</span>
                    <span class="detail-value">{{ formatDateTime(order.scheduleTime) }}</span>
                  </div>

                  <!-- 元数据 -->
                  <div class="order-meta">
                    <span>开立: {{ formatDateTime(order.createTime) }}</span>
                    <span>医生: {{ order.doctorName }}</span>
                  </div>
                </div>
              </div>

              <!-- 操作按钮 -->
              <div class="order-actions">
                <el-button 
                  type="primary" 
                  @click="acknowledgeOne(order)"
                  class="action-btn-small"
                >
                  签收
                </el-button>
                <el-button 
                  type="danger"
                  @click="rejectOne(order)"
                  class="action-btn-small"
                >
                  退回
                </el-button>
              </div>
            </div>
          </div>

          <!-- 空状态 -->
          <div v-else class="empty-state">
            <div class="empty-icon">✅</div>
            <p>该患者暂无待签收的新开医嘱</p>
          </div>
        </el-tab-pane>

        <el-tab-pane :label="`停止医嘱 (${sortedStoppedOrders.length})`" name="stopped">
          <div v-if="sortedStoppedOrders.length > 0" class="order-list">
            <!-- 批量操作栏 -->
            <div class="batch-toolbar">
              <el-checkbox 
                v-model="selectAllStopped" 
                @change="handleSelectAllStopped"
                :indeterminate="isIndeterminateStopped"
              >
                全选
              </el-checkbox>
              <div class="batch-actions">
                <el-button 
                  type="primary" 
                  :disabled="selectedStoppedCount === 0"
                  @click="acknowledgeStoppedBatch"
                  class="action-btn"
                >
                  批量签收 ({{ selectedStoppedCount }})
                </el-button>
              </div>
            </div>

            <!-- 停止医嘱列表 -->
            <div v-for="order in sortedStoppedOrders" 
                 :key="order.orderId"
                 class="order-item stopped">
              <el-checkbox v-model="order.selected" @change="handleOrderSelectChange" />
              
              <div class="order-content">
                <!-- 多选模式下显示患者信息 -->
                <div v-if="enableMultiSelect" class="order-patient-tag">
                  <span class="patient-bed-tag">{{ order.bedId }}</span>
                  <span class="patient-name-tag">{{ order.patientName }}</span>
                </div>
                
                <div class="order-header">
                  <el-tag type="danger" size="small">已停止</el-tag>
                  <el-tag 
                    :type="getOrderTypeColor(order.orderType)" 
                    size="small"
                  >
                    {{ getOrderTypeName(order.orderType) }}
                  </el-tag>
                  <span class="order-text">{{ order.displayText }}</span>
                </div>

                <div class="order-details">
                  <div class="detail-section">
                    <span class="detail-label">停止时间:</span>
                    <span class="detail-value">{{ formatDateTime(order.stopTime) }}</span>
                  </div>
                  <div v-if="order.stopReason" class="detail-section">
                    <span class="detail-label">停止原因:</span>
                    <span class="detail-value">{{ order.stopReason }}</span>
                  </div>
                  <div class="order-meta">
                    <span>原医嘱开立: {{ formatDateTime(order.createTime) }}</span>
                    <span>医生: {{ order.doctorName }}</span>
                  </div>
                </div>
              </div>

              <div class="order-actions">
                <el-button 
                  type="primary" 
                  @click="acknowledgeStoppedOne(order)"
                  class="action-btn-small"
                >
                  签收
                </el-button>
              </div>
            </div>
          </div>

          <!-- 空状态 -->
          <div v-else class="empty-state">
            <div class="empty-icon">✅</div>
            <p>该患者暂无待签收的停止医嘱</p>
          </div>
        </el-tab-pane>
      </el-tabs>
    </section>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
// 患者相关API已移至 patient.js
import { 
  getPatientsWithPendingCount, 
  getCurrentWard 
} from '../api/patient';
// 医嘱签收相关API
import { 
  getPatientPendingOrders,
  acknowledgeOrders,
  rejectOrders,
  requestMedicationImmediately,
  requestInspection,
  cancelMedicationRequest
} from '../api/orderAcknowledgement';

// ==================== 状态管理 ====================

const patientList = ref([]);
const selectedPatient = ref(null);
const selectedPatients = ref([]); // 多选患者列表
const pendingOrders = ref({ newOrders: [], stoppedOrders: [] });
const activeTab = ref('new');
const patientSearch = ref('');
const leftCollapsed = ref(false);
const selectAllNew = ref(false);
const selectAllStopped = ref(false);

// 筛选相关状态
const wardFilter = ref('');
const showOnlyPending = ref(false);
const enableMultiSelect = ref(false);
const orderSortBy = ref('time'); // 'time' | 'patient'
const currentScheduledWardId = ref(null); // 护士当前排班的病区ID

// 当前护士信息
const getCurrentNurse = () => {
  try {
    const userInfoStr = localStorage.getItem('userInfo');
    if (userInfoStr) {
      return JSON.parse(userInfoStr);
    }
  } catch (error) {
    console.error('解析用户信息失败:', error);
  }
  return { staffId: 'NUR001', fullName: '未登录', wardId: 'IM-W01' };
};

const currentNurse = ref(getCurrentNurse());

// 获取护士当前排班病区
const fetchCurrentScheduledWard = async () => {
  try {
    const nurseId = currentNurse.value.staffId;
    const result = await getCurrentWard(nurseId);
    currentScheduledWardId.value = result.wardId;
    
    if (result.wardId) {
      console.log(`✅ 护士当前排班病区: ${result.wardId}`);
    } else {
      console.log('ℹ️ 护士今日无排班记录，使用默认病区');
      currentScheduledWardId.value = currentNurse.value.wardId;
    }
  } catch (error) {
    console.error('获取当前排班病区失败:', error);
    // 失败时使用护士基本信息中的病区
    currentScheduledWardId.value = currentNurse.value.wardId;
  }
};

// ==================== 计算属性 ====================

// 病区选项（从患者列表中提取唯一病区）
const wardOptions = computed(() => {
  const wards = new Map();
  patientList.value.forEach(p => {
    if (!wards.has(p.wardId)) {
      wards.set(p.wardId, {
        wardId: p.wardId,
        wardName: p.wardName,
        // 使用当前排班病区判断是否是"我负责"
        isMyWard: p.wardId === currentScheduledWardId.value
      });
    }
  });
  return Array.from(wards.values()).sort((a, b) => {
    // 我负责的病区排在前面
    if (a.isMyWard && !b.isMyWard) return -1;
    if (!a.isMyWard && b.isMyWard) return 1;
    return a.wardName.localeCompare(b.wardName);
  });
});

// 过滤后的患者列表
const filteredPatients = computed(() => {
  let filtered = patientList.value;
  
  // 搜索过滤
  if (patientSearch.value) {
    const keyword = patientSearch.value.toLowerCase();
    filtered = filtered.filter(p => 
      p.bedId.toLowerCase().includes(keyword) ||
      p.patientName.includes(keyword)
    );
  }
  
  // 病区过滤
  if (wardFilter.value) {
    filtered = filtered.filter(p => p.wardId === wardFilter.value);
  }
  
  // 仅显示待签收
  if (showOnlyPending.value) {
    filtered = filtered.filter(p => p.unacknowledgedCount > 0);
  }
  
  return filtered;
});

// 判断是否是我负责的病区（使用当前排班病区）
const isMyWard = (wardId) => {
  return wardId === currentScheduledWardId.value;
};

// 判断患者是否被选中
const isPatientSelected = (patient) => {
  if (!enableMultiSelect.value) {
    return selectedPatient.value?.patientId === patient.patientId;
  }
  return selectedPatients.value.some(p => p.patientId === patient.patientId);
};

// 排序后的医嘱列表
const sortedNewOrders = computed(() => {
  if (!enableMultiSelect.value || orderSortBy.value === 'time') {
    // 按时间排序
    return [...pendingOrders.value.newOrders].sort((a, b) => 
      new Date(a.createTime) - new Date(b.createTime)
    );
  } else {
    // 按患者排序
    return [...pendingOrders.value.newOrders].sort((a, b) => {
      const patientA = selectedPatients.value.find(p => p.patientId === a.patientId);
      const patientB = selectedPatients.value.find(p => p.patientId === b.patientId);
      if (!patientA || !patientB) return 0;
      return patientA.bedId.localeCompare(patientB.bedId);
    });
  }
});

const sortedStoppedOrders = computed(() => {
  if (!enableMultiSelect.value || orderSortBy.value === 'time') {
    return [...pendingOrders.value.stoppedOrders].sort((a, b) => 
      new Date(a.createTime) - new Date(b.createTime)
    );
  } else {
    return [...pendingOrders.value.stoppedOrders].sort((a, b) => {
      const patientA = selectedPatients.value.find(p => p.patientId === a.patientId);
      const patientB = selectedPatients.value.find(p => p.patientId === b.patientId);
      if (!patientA || !patientB) return 0;
      return patientA.bedId.localeCompare(patientB.bedId);
    });
  }
});

// 新开医嘱选中数量
const selectedNewCount = computed(() => {
  return sortedNewOrders.value.filter(o => o.selected).length;
});

// 停止医嘱选中数量
const selectedStoppedCount = computed(() => {
  return sortedStoppedOrders.value.filter(o => o.selected).length;
});

// 新开医嘱全选状态
const isIndeterminateNew = computed(() => {
  const count = selectedNewCount.value;
  return count > 0 && count < sortedNewOrders.value.length;
});

// 停止医嘱全选状态
const isIndeterminateStopped = computed(() => {
  const count = selectedStoppedCount.value;
  return count > 0 && count < sortedStoppedOrders.value.length;
});

// ==================== 初始化加载 ====================

onMounted(async () => {
  // 先获取当前排班病区
  await fetchCurrentScheduledWard();
  
  // 再加载患者列表
  await loadPatientList();
  
  // 启动定时刷新
  startAutoRefresh();
});

onUnmounted(() => {
  // 组件卸载时清除定时器
  stopAutoRefresh();
});

// 加载患者列表（带未签收统计）
const loadPatientList = async () => {
  try {
    const deptCode = currentNurse.value.deptCode;
    if (!deptCode) {
      ElMessage.error('未找到护士所属科室信息');
      return;
    }

    const summary = await getPatientsWithPendingCount(deptCode);
    patientList.value = summary;
    
    ElMessage.success(`加载了 ${summary.length} 个患者`);
  } catch (error) {
    console.error('加载患者列表失败:', error);
    ElMessage.error(error.message || '加载患者列表失败');
  }
};

// ==================== 患者选择 ====================

// 切换多选模式
const handleMultiSelectToggle = (enabled) => {
  if (!enabled) {
    // 关闭多选模式，保留第一个选中的患者
    if (selectedPatients.value.length > 0) {
      selectedPatient.value = selectedPatients.value[0];
      selectedPatients.value = [selectedPatients.value[0]];
    }
  } else {
    // 开启多选模式
    if (selectedPatient.value) {
      selectedPatients.value = [selectedPatient.value];
    }
  }
  // 重新加载医嘱
  loadSelectedPatientsOrders();
};

// 选择患者（兼容单选和多选）
const selectPatient = async (patient) => {
  if (!enableMultiSelect.value) {
    // 单选模式
    if (selectedPatient.value?.patientId === patient.patientId) return;
    selectedPatient.value = patient;
    selectedPatients.value = [patient];
    await loadPatientPendingOrders(patient.patientId);
  } else {
    // 多选模式：切换选中状态
    togglePatientSelection(patient);
  }
};

// 切换患者选中状态（多选模式）
const togglePatientSelection = (patient) => {
  const index = selectedPatients.value.findIndex(p => p.patientId === patient.patientId);
  if (index > -1) {
    selectedPatients.value.splice(index, 1);
  } else {
    selectedPatients.value.push(patient);
  }
  
  // 更新单选引用
  selectedPatient.value = selectedPatients.value[0] || null;
  
  // 重新加载医嘱
  loadSelectedPatientsOrders();
};

// 加载选中患者的医嘱
const loadSelectedPatientsOrders = async () => {
  if (selectedPatients.value.length === 0) {
    pendingOrders.value = { newOrders: [], stoppedOrders: [] };
    return;
  }
  
  try {
    // 并行加载所有选中患者的医嘱
    const promises = selectedPatients.value.map(p => getPatientPendingOrders(p.patientId));
    const results = await Promise.all(promises);
    
    // 合并所有患者的医嘱，并添加患者信息
    const allNewOrders = [];
    const allStoppedOrders = [];
    
    results.forEach((data, index) => {
      const patient = selectedPatients.value[index];
      
      data.newOrders.forEach(order => {
        order.selected = false;
        order.patientId = patient.patientId;
        order.patientName = patient.patientName;
        order.bedId = patient.bedId;
        allNewOrders.push(order);
      });
      
      data.stoppedOrders.forEach(order => {
        order.selected = false;
        order.patientId = patient.patientId;
        order.patientName = patient.patientName;
        order.bedId = patient.bedId;
        allStoppedOrders.push(order);
      });
    });
    
    pendingOrders.value = {
      newOrders: allNewOrders,
      stoppedOrders: allStoppedOrders
    };
    
    // 重置全选状态
    selectAllNew.value = false;
    selectAllStopped.value = false;
  } catch (error) {
    console.error('加载患者待签收医嘱失败:', error);
    ElMessage.error(error.message || '加载医嘱失败');
  }
};

// 加载患者待签收医嘱（单患者）
const loadPatientPendingOrders = async (patientId) => {
  try {
    const data = await getPatientPendingOrders(patientId);
    
    // 为每条医嘱添加selected属性
    data.newOrders.forEach(o => o.selected = false);
    data.stoppedOrders.forEach(o => o.selected = false);
    
    pendingOrders.value = data;
    
    // 重置全选状态
    selectAllNew.value = false;
    selectAllStopped.value = false;
  } catch (error) {
    console.error('加载患者待签收医嘱失败:', error);
    ElMessage.error(error.message || '加载医嘱失败');
  }
};

// ==================== 签收逻辑 ====================

// 单条签收（新开医嘱）
const acknowledgeOne = async (order) => {
  await acknowledgeBatchInternal([order.orderId]);
};

// 批量签收（新开医嘱）
const acknowledgeBatch = async () => {
  const selectedIds = pendingOrders.value.newOrders
    .filter(o => o.selected)
    .map(o => o.orderId);
  
  if (selectedIds.length === 0) {
    ElMessage.warning('请至少选择一条医嘱');
    return;
  }

  await acknowledgeBatchInternal(selectedIds);
};

// 签收核心逻辑
const acknowledgeBatchInternal = async (orderIds) => {
  try {
    const result = await acknowledgeOrders({
      nurseId: currentNurse.value.staffId,
      orderIds: orderIds
    });

    if (!result.success) {
      ElMessage.error(result.message || '签收失败');
      return;
    }

    ElMessage.success(result.message);

    // 处理每条医嘱的签收结果
    for (const item of result.results) {
      await handleAcknowledgeResult(item);
    }

    // 清除选择状态
    selectAllNew.value = false;
    pendingOrders.value.newOrders.forEach(o => o.selected = false);

    // 刷新列表
    await refreshCurrentView();
  } catch (error) {
    console.error('签收失败:', error);
    ElMessage.error(error.message || '签收失败');
  }
};

// 处理签收结果（弹窗提示）
// 【暂时注释】签收后弹窗逻辑
const handleAcknowledgeResult = async (result) => {
  // if (!result.needTodayAction) {
  //   return;
  // }

  // // 查找对应的医嘱详情
  // const order = [...pendingOrders.value.newOrders, ...pendingOrders.value.stoppedOrders]
  //   .find(o => o.orderId === result.orderId);

  // // 药品医嘱：询问是否立即申请药品
  // if (result.actionType === 'RequestMedication') {
  //   try {
  //     // 构建详细的医嘱信息
  //     let orderInfo = '';
  //     if (order) {
  //       // 构建药品明细列表
  //       let itemsHtml = '';
  //       if (order.items && order.items.length > 0) {
  //         itemsHtml = '<div style="margin-bottom: 8px;"><strong>药品明细：</strong></div>';
  //         order.items.forEach((item, idx) => {
  //           itemsHtml += `
  //             <div style="margin-left: 20px; margin-bottom: 6px; padding: 8px; background: #fff; border-left: 3px solid #409eff; border-radius: 4px;">
  //               <div>${idx + 1}. ${item.drugName || '未知药品'}</div>
  //               <div style="font-size: 13px; color: #666; margin-top: 4px;">
  //                 规格: ${item.specification || '未知'} | 剂量: ${item.dosage || '未知'}
  //                 ${item.note ? `<br/>备注: ${item.note}` : ''}
  //               </div>
  //             </div>
  //           `;
  //         });
  //       }
  //       
  //       orderInfo = `
  //         <div style="text-align: left; margin-top: 10px; padding: 15px; background: #f5f7fa; border-radius: 6px; font-size: 14px;">
  //           <div style="margin-bottom: 8px;"><strong>医嘱内容：</strong>${order.displayText || '未知'}</div>
  //           ${itemsHtml}
  //           <div style="margin-bottom: 8px;"><strong>给药途径：</strong>${getUsageRouteText(order.usageRoute) || '未知'}</div>
  //           <div style="margin-bottom: 8px;"><strong>时间策略：</strong>${getTimingStrategyText(order) || '未知'}</div>
  //           <div style="margin-bottom: 8px;"><strong>开始时间：</strong>${order.startTime ? formatDateTime(order.startTime) : '未设置'}</div>
  //           <div style="margin-bottom: 8px;"><strong>计划结束：</strong>${order.plantEndTime ? formatDateTime(order.plantEndTime) : '未设置'}</div>
  //           ${order.remarks ? `<div style="margin-bottom: 8px;"><strong>备注：</strong>${order.remarks}</div>` : ''}
  //         </div>
  //       `;
  //     }

  //     await ElMessageBox.confirm(
  //       `该医嘱今日需要执行，是否立即向药房申请药品？${orderInfo}`,
  //       '提示',
  //       {
  //         confirmButtonText: '立即申请',
  //         cancelButtonText: '稍后申请',
  //         type: 'info',
  //         dangerouslyUseHTMLString: true,
  //         customClass: 'order-action-confirm'
  //       }
  //     );
  //     
  //     // TODO: 阶段三实现 - 调用申请药品接口
  //     // await requestMedicationImmediately({ orderId: result.orderId });
  //     ElMessage.info('药品申请功能待阶段三实现');
  //   } catch {
  //     // 用户选择稍后申请
  //   }
  // }
  // // 检查医嘱：询问是否立即申请检查
  // else if (result.actionType === 'RequestInspection') {
  //   try {
  //     // 构建详细的医嘱信息
  //     const orderInfo = order ? `
  //       <div style="text-align: left; margin-top: 10px; padding: 15px; background: #f5f7fa; border-radius: 6px; font-size: 14px;">
  //         <div style="margin-bottom: 8px;"><strong>医嘱内容：</strong>${order.displayText || '未知'}</div>
  //         <div style="margin-bottom: 8px;"><strong>检查项目代码：</strong>${order.itemCode || '未知'}</div>
  //         <div style="margin-bottom: 8px;"><strong>检查地点：</strong>${order.location || '未知'}</div>
  //         ${order.remarks ? `<div style="margin-bottom: 8px;"><strong>备注：</strong>${order.remarks}</div>` : ''}
  //         <div style="margin-top: 10px; padding: 8px; background: #fff3cd; border-radius: 4px; font-size: 13px;">
  //           💡 提示：如需特殊准备（空腹、憋尿等），请查看完整医嘱详情
  //         </div>
  //       </div>
  //     ` : '';

  //     await ElMessageBox.confirm(
  //       `是否立即向检查站申请检查？${orderInfo}`,
  //       '提示',
  //       {
  //         confirmButtonText: '立即申请',
  //         cancelButtonText: '稍后申请',
  //         type: 'info',
  //         dangerouslyUseHTMLString: true,
  //         customClass: 'order-action-confirm'
  //       }
  //     );
  //     
  //     // TODO: 阶段三实现 - 调用申请检查接口
  //     // await requestInspection({ orderId: result.orderId });
  //     ElMessage.info('检查申请功能待阶段三实现');
  //   } catch {
  //     // 用户选择稍后申请
  //   }
  // }
};

// ==================== 停止医嘱签收 ====================

// 单条签收（停止医嘱）
const acknowledgeStoppedOne = async (order) => {
  await acknowledgeStoppedBatchInternal([order.orderId]);
};

// 批量签收（停止医嘱）
const acknowledgeStoppedBatch = async () => {
  const selectedIds = pendingOrders.value.stoppedOrders
    .filter(o => o.selected)
    .map(o => o.orderId);
  
  if (selectedIds.length === 0) {
    ElMessage.warning('请至少选择一条医嘱');
    return;
  }

  await acknowledgeStoppedBatchInternal(selectedIds);
};

// 停止医嘱签收核心逻辑
const acknowledgeStoppedBatchInternal = async (orderIds) => {
  try {
    const result = await acknowledgeOrders({
      nurseId: currentNurse.value.staffId,
      orderIds: orderIds
    });

    if (!result.success) {
      ElMessage.error(result.message || '签收失败');
      return;
    }

    ElMessage.success(result.message);

    // TODO: 阶段三实现 - 检查是否有待取消的申请
    for (const item of result.results) {
      if (item.hasPendingRequests) {
        await handleStoppedOrderWithPendingRequests(item);
      }
    }

    // 清除选择状态
    selectAllStopped.value = false;
    pendingOrders.value.stoppedOrders.forEach(o => o.selected = false);

    // 刷新列表
    await refreshCurrentView();
  } catch (error) {
    console.error('签收停止医嘱失败:', error);
    ElMessage.error(error.message || '签收失败');
  }
};

// TODO: 阶段三实现 - 处理停止医嘱的待取消申请
const handleStoppedOrderWithPendingRequests = async (result) => {
  try {
    await ElMessageBox.confirm(
      `该医嘱存在 ${result.pendingRequestIds.length} 个已提交但未执行的申请，是否取消这些申请？`,
      '警告',
      {
        confirmButtonText: '取消申请',
        cancelButtonText: '保留申请',
        type: 'warning'
      }
    );
    
    // TODO: 调用取消申请接口
    // await cancelMedicationRequest({ 
    //   orderId: result.orderId, 
    //   requestIds: result.pendingRequestIds 
    // });
    ElMessage.info('取消申请功能待阶段三实现');
  } catch {
    // 用户选择保留申请
  }
};

// ==================== 退回逻辑 ====================

// 单条退回
const rejectOne = async (order) => {
  await rejectBatchInternal([order.orderId]);
};

// 批量退回
const rejectBatch = async () => {
  const selectedIds = pendingOrders.value.newOrders
    .filter(o => o.selected)
    .map(o => o.orderId);
  
  if (selectedIds.length === 0) {
    ElMessage.warning('请至少选择一条医嘱');
    return;
  }

  await rejectBatchInternal(selectedIds);
};

// 退回核心逻辑
const rejectBatchInternal = async (orderIds) => {
  try {
    // 弹窗输入退回原因
    const { value: reason } = await ElMessageBox.prompt(
      '请输入退回原因',
      '退回医嘱',
      {
        confirmButtonText: '确定',
        cancelButtonText: '取消',
        inputPattern: /\S+/,
        inputErrorMessage: '退回原因不能为空'
      }
    );

    const result = await rejectOrders({
      nurseId: currentNurse.value.staffId,
      orderIds: orderIds,
      rejectReason: reason
    });

    if (!result.success) {
      ElMessage.error(result.message || '退回失败');
      return;
    }

    ElMessage.success(result.message);

    // 清除选择状态
    selectAllNew.value = false;
    pendingOrders.value.newOrders.forEach(o => o.selected = false);

    // 刷新列表
    await refreshCurrentView();
  } catch (error) {
    if (error === 'cancel') {
      // 用户取消
      return;
    }
    console.error('退回失败:', error);
    ElMessage.error(error.message || '退回失败');
  }
};

// ==================== 辅助方法 ====================

// 切换左侧面板
const toggleLeft = () => {
  leftCollapsed.value = !leftCollapsed.value;
};

// Tab切换
const handleTabClick = (tab) => {
  // 重置选择状态
  selectAllNew.value = false;
  selectAllStopped.value = false;
};

// 全选新开医嘱
const handleSelectAllNew = (val) => {
  pendingOrders.value.newOrders.forEach(o => o.selected = val);
};

// 全选停止医嘱
const handleSelectAllStopped = (val) => {
  pendingOrders.value.stoppedOrders.forEach(o => o.selected = val);
};

// 医嘱选择状态变化
const handleOrderSelectChange = () => {
  // 更新全选状态
  if (activeTab.value === 'new') {
    const all = pendingOrders.value.newOrders.length;
    const selected = selectedNewCount.value;
    selectAllNew.value = all > 0 && selected === all;
  } else {
    const all = pendingOrders.value.stoppedOrders.length;
    const selected = selectedStoppedCount.value;
    selectAllStopped.value = all > 0 && selected === all;
  }
};

// 定时刷新相关
const refreshInterval = ref(null);
const REFRESH_INTERVAL_MS = 30000; // 30秒刷新一次

// 启动定时刷新
const startAutoRefresh = () => {
  // 清除旧的定时器（如果存在）
  stopAutoRefresh();
  
  // 设置新的定时器
  refreshInterval.value = setInterval(() => {
    refreshCurrentView();
  }, REFRESH_INTERVAL_MS);
};

// 停止定时刷新
const stopAutoRefresh = () => {
  if (refreshInterval.value) {
    clearInterval(refreshInterval.value);
    refreshInterval.value = null;
  }
};

// 刷新当前视图（智能Diff更新，避免闪烁）
const refreshCurrentView = async () => {
  await loadPatientListWithDiff();
  
  // 根据选择模式刷新医嘱
  if (enableMultiSelect.value && selectedPatients.value.length > 0) {
    await loadSelectedPatientsOrdersWithDiff();
  } else if (selectedPatient.value) {
    await loadPatientPendingOrdersWithDiff(selectedPatient.value.patientId);
  }
};

// 智能Diff更新患者列表
const loadPatientListWithDiff = async () => {
  try {
    const deptCode = currentNurse.value.deptCode;
    if (!deptCode) return;

    const newData = await getPendingOrdersSummary(deptCode);
    
    // Diff算法：对比新旧数据
    const oldMap = new Map(patientList.value.map(p => [p.patientId, p]));
    const newMap = new Map(newData.map(p => [p.patientId, p]));
    
    // 1. 删除不存在的患者（从后往前删，避免索引混乱）
    for (let i = patientList.value.length - 1; i >= 0; i--) {
      const patient = patientList.value[i];
      if (!newMap.has(patient.patientId)) {
        patientList.value.splice(i, 1);
      }
    }
    
    // 2. 更新已存在的患者 + 添加新患者
    newData.forEach((newPatient, index) => {
      const oldPatient = oldMap.get(newPatient.patientId);
      
      if (oldPatient) {
        // 已存在：只更新变化的字段
        const oldIndex = patientList.value.findIndex(p => p.patientId === newPatient.patientId);
        if (oldIndex !== -1) {
          // 更新所有可能变化的字段，注意字段名大小写
          const patient = patientList.value[oldIndex];
          if (patient.unacknowledgedCount !== newPatient.unacknowledgedCount) {
            patient.unacknowledgedCount = newPatient.unacknowledgedCount;
          }
          // 更新其他可能变化的字段
          patient.patientName = newPatient.patientName;
          patient.bedId = newPatient.bedId;
          patient.age = newPatient.age;
          patient.weight = newPatient.weight;
          patient.gender = newPatient.gender;
          patient.nursingGrade = newPatient.nursingGrade;
          patient.wardId = newPatient.wardId;
          patient.wardName = newPatient.wardName;
          
          // 如果需要移动位置（保持服务器返回的顺序）
          if (oldIndex !== index) {
            const [movedItem] = patientList.value.splice(oldIndex, 1);
            patientList.value.splice(index, 0, movedItem);
          }
        }
      } else {
        // 新患者：插入到正确位置
        patientList.value.splice(index, 0, newPatient);
      }
    });
    
    // 更新当前选中患者的引用（如果列表中有更新）
    if (selectedPatient.value) {
      const updated = patientList.value.find(p => p.patientId === selectedPatient.value.patientId);
      if (updated) {
        selectedPatient.value = updated;
      }
    }
    
    // 更新多选患者列表的引用
    if (enableMultiSelect.value && selectedPatients.value.length > 0) {
      selectedPatients.value = selectedPatients.value.map(sp => {
        const updated = patientList.value.find(p => p.patientId === sp.patientId);
        return updated || sp;
      });
    }
  } catch (error) {
    console.error('刷新患者列表失败:', error);
  }
};

// 智能Diff更新多患者医嘱列表
const loadSelectedPatientsOrdersWithDiff = async () => {
  if (selectedPatients.value.length === 0) return;
  
  try {
    // 并行加载所有选中患者的医嘱
    const promises = selectedPatients.value.map(p => getPatientPendingOrders(p.patientId));
    const results = await Promise.all(promises);
    
    // 保存旧的选中状态
    const oldSelectedNew = new Set(
      pendingOrders.value.newOrders.filter(o => o.selected).map(o => o.orderId)
    );
    const oldSelectedStopped = new Set(
      pendingOrders.value.stoppedOrders.filter(o => o.selected).map(o => o.orderId)
    );
    
    // 合并所有患者的医嘱
    const allNewOrders = [];
    const allStoppedOrders = [];
    
    results.forEach((data, index) => {
      const patient = selectedPatients.value[index];
      
      data.newOrders.forEach(order => {
        order.selected = oldSelectedNew.has(order.orderId);
        order.patientId = patient.patientId;
        order.patientName = patient.patientName;
        order.bedId = patient.bedId;
        allNewOrders.push(order);
      });
      
      data.stoppedOrders.forEach(order => {
        order.selected = oldSelectedStopped.has(order.orderId);
        order.patientId = patient.patientId;
        order.patientName = patient.patientName;
        order.bedId = patient.bedId;
        allStoppedOrders.push(order);
      });
    });
    
    // Diff更新（保持选中状态）
    diffUpdateOrders(pendingOrders.value.newOrders, allNewOrders);
    diffUpdateOrders(pendingOrders.value.stoppedOrders, allStoppedOrders);
    
  } catch (error) {
    console.error('刷新多患者医嘱列表失败:', error);
  }
};

// 智能Diff更新医嘱列表
const loadPatientPendingOrdersWithDiff = async (patientId) => {
  try {
    const newData = await getPatientPendingOrders(patientId);
    
    // 为新数据添加selected属性（继承旧数据的选中状态）
    const oldSelectedNew = new Set(
      pendingOrders.value.newOrders.filter(o => o.selected).map(o => o.orderId)
    );
    const oldSelectedStopped = new Set(
      pendingOrders.value.stoppedOrders.filter(o => o.selected).map(o => o.orderId)
    );
    
    newData.newOrders.forEach(order => {
      order.selected = oldSelectedNew.has(order.orderId);
    });
    newData.stoppedOrders.forEach(order => {
      order.selected = oldSelectedStopped.has(order.orderId);
    });
    
    // Diff更新新开医嘱
    diffUpdateOrders(pendingOrders.value.newOrders, newData.newOrders);
    
    // Diff更新停止医嘱
    diffUpdateOrders(pendingOrders.value.stoppedOrders, newData.stoppedOrders);
    
  } catch (error) {
    console.error('刷新医嘱列表失败:', error);
  }
};

// 通用的医嘱列表Diff更新函数
const diffUpdateOrders = (oldList, newList) => {
  const newMap = new Map(newList.map(o => [o.orderId, o]));
  
  // 1. 删除不存在的医嘱
  for (let i = oldList.length - 1; i >= 0; i--) {
    if (!newMap.has(oldList[i].orderId)) {
      oldList.splice(i, 1);
    }
  }
  
  // 2. 更新已存在的 + 添加新医嘱
  newList.forEach((newOrder, index) => {
    const oldIndex = oldList.findIndex(o => o.orderId === newOrder.orderId);
    
    if (oldIndex !== -1) {
      // 已存在：更新非selected字段，保持选中状态
      const oldOrder = oldList[oldIndex];
      Object.keys(newOrder).forEach(key => {
        if (key !== 'selected') {
          oldOrder[key] = newOrder[key];
        }
      });
      // 调整顺序
      if (oldIndex !== index) {
        const [movedItem] = oldList.splice(oldIndex, 1);
        oldList.splice(index, 0, movedItem);
      }
    } else {
      // 新医嘱：插入
      oldList.splice(index, 0, newOrder);
    }
  });
};

// 获取医嘱类型名称
const getOrderTypeName = (orderType) => {
  const map = {
    'MedicationOrder': '药品',
    'InspectionOrder': '检查',
    'SurgicalOrder': '手术',
    'OperationOrder': '操作'
  };
  return map[orderType] || orderType;
};

// 获取医嘱类型颜色
const getOrderTypeColor = (orderType) => {
  const map = {
    'MedicationOrder': 'success',
    'InspectionOrder': 'info',
    'SurgicalOrder': 'danger',
    'OperationOrder': 'warning'
  };
  return map[orderType] || '';
};

// 获取时间策略文本
const getTimingStrategyText = (order) => {
  const map = {
    'IMMEDIATE': '立即执行',
    'SPECIFIC': `指定时间 ${formatDateTime(order.startTime)}`,
    'CYCLIC': `周期执行`,
    'SLOTS': '时段执行'
  };
  return map[order.timingStrategy] || order.timingStrategy;
};

// 获取给药途径文本
const getUsageRouteText = (route) => {
  const map = {
    '1': '口服',
    '10': '肌肉注射',
    '11': '皮下注射',
    '12': '皮内注射',
    '20': '静脉滴注',
    '21': '静脉推注'
  };
  return map[route] || route;
};

// 格式化日期时间
const formatDateTime = (dateTime) => {
  if (!dateTime) return '-';
  const date = new Date(dateTime);
  return date.toLocaleString('zh-CN', { 
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit'
  });
};
</script>

<style scoped>
/* ==================== 全局变量 ==================== */
.order-acknowledgement {
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

/* ==================== 左侧患者列表（复用OrderEntry样式）==================== */

.patient-panel {
  background: var(--bg-card);
  border-radius: var(--radius-large);
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.08);
  display: flex;
  flex-direction: column;
  overflow: hidden;
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
  max-height: calc(100vh - 100px);
}

.patient-panel.collapsed {
  width: 40px !important;
}

.panel-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 12px 15px;
  border-bottom: 1px solid #e8e8e8;
  background: #fafafa;
  flex-shrink: 0;
}

.patient-panel.collapsed .panel-header {
  flex-direction: column;
  padding: 12px 5px;
  justify-content: center;
  background: #f5f5f5;
}

.panel-title {
  font-size: 1rem;
  font-weight: 600;
  color: #000;
  margin: 0;
  display: flex;
  align-items: center;
  gap: 6px;
  letter-spacing: 0.3px;
}

.collapse-btn {
  background: #ddd;
  color: #666;
  border: none;
  width: 22px;
  height: 22px;
  border-radius: 50%;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 0.8rem;
  font-weight: normal;
  transition: all 0.25s;
  flex-shrink: 0;
}

.collapse-btn:hover {
  background: #bbb;
  color: #333;
  transform: scale(1.05);
}

.panel-content {
  flex: 1;
  overflow: hidden;
  display: flex;
  flex-direction: column;
}

.search-box {
  padding: 12px 15px;
  border-bottom: 1px solid #f0f0f0;
  flex-shrink: 0;
}

.search-box :deep(.el-input__inner) {
  font-size: 1rem;
  color: var(--text-regular);
  font-weight: normal;
}

.search-box :deep(.el-input__inner::placeholder) {
  color: #999;
  font-weight: normal;
}

/* ==================== 筛选工具栏 ==================== */

.filter-toolbar {
  padding: 10px 15px;
  border-bottom: 1px solid #e8e8e8;
  background: #fafafa;
  display: flex;
  flex-direction: column;
  gap: 8px;
  flex-shrink: 0;
}

.filter-group {
  display: flex;
  align-items: center;
}

.ward-filter {
  width: 100%;
}

.ward-filter :deep(.el-input__inner) {
  font-size: 13px;
}

.my-ward-tag {
  color: #f59e0b;
  font-weight: 600;
  margin-left: 8px;
  font-size: 12px;
}

.pending-filter,
.multi-select-toggle {
  font-size: 13px;
}

.pending-filter :deep(.el-checkbox__label),
.multi-select-toggle :deep(.el-checkbox__label) {
  font-size: 13px;
  color: #606266;
}

/* ==================== 患者列表 ==================== */

.patient-list {
  flex: 1;
  overflow-y: auto;
  padding: 10px;
}

.patient-card {
  background: white;
  border: 1.5px solid var(--border-color);
  border-radius: var(--radius-medium);
  padding: 12px;
  margin-bottom: 10px;
  cursor: pointer;
  transition: all 0.3s;
  position: relative;
  display: flex;
  flex-direction: column;
}

.patient-card.my-ward {
  border-left: 3px solid #f59e0b;
  background: linear-gradient(90deg, #fffbeb 0%, white 20%);
}

.patient-checkbox {
  position: absolute;
  top: 12px;
  left: 12px;
  z-index: 1;
}

.patient-card:has(.patient-checkbox) {
  padding-left: 40px;
}

.patient-card:hover {
  border-color: var(--primary-color);
  transform: translateX(4px);
  box-shadow: -3px 0 12px rgba(64, 158, 255, 0.15);
}

.patient-card.active {
  background: linear-gradient(135deg, #e8f4ff 0%, #f0f8ff 100%);
  border-color: var(--primary-color);
  border-width: 2px;
  box-shadow: -4px 0 16px rgba(64, 158, 255, 0.25);
}

.pending-badge {
  position: absolute;
  top: 4px;
  right: 4px;
  background: var(--danger-color);
  color: white;
  font-size: 11px;
  padding: 2px 7px;
  border-radius: 10px;
  font-weight: 600;
  box-shadow: 0 2px 4px rgba(245, 108, 108, 0.3);
}

.bed-badge {
  background: var(--primary-color);
  color: white;
  padding: 3px 8px;
  border-radius: 4px;
  font-size: 0.75rem;
  font-weight: bold;
  display: inline-block;
  margin-bottom: 8px;
}

.patient-card.active .bed-badge {
  background: linear-gradient(135deg, #409eff 0%, #66b1ff 100%);
  box-shadow: 0 2px 6px rgba(64, 158, 255, 0.3);
}

.patient-basic {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 6px;
}

.p-name {
  font-weight: 600;
  font-size: 1rem;
  color: var(--text-primary);
  letter-spacing: 0.3px;
}

.p-info {
  font-size: 0.85rem;
  color: var(--text-secondary);
}

.p-ward {
  font-size: 0.75rem;
  color: #8b5cf6;
  background: #f3e8ff;
  padding: 2px 6px;
  border-radius: 4px;
  margin-top: 4px;
  display: inline-block;
}

.patient-meta {
  display: flex;
  gap: 10px;
  margin-bottom: 6px;
}

.p-care {
  font-size: 0.8rem;
  color: var(--primary-color);
  background: #e8f4ff;
  padding: 2px 8px;
  border-radius: 10px;
}

.collapsed-content {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 20px 0;
}

.collapsed-text {
  writing-mode: vertical-rl;
  font-size: 0.9rem;
  font-weight: 600;
  color: var(--text-primary);
  letter-spacing: 2px;
  margin-bottom: 20px;
}

.patient-count {
  background: var(--primary-color);
  color: white;
  width: 28px;
  height: 28px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 0.85rem;
  font-weight: bold;
  margin-top: 10px;
}

/* ==================== 右侧工作区 ==================== */

.work-area {
  background: var(--bg-card);
  border-radius: var(--radius-large);
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.08);
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.patient-info-bar {
  display: flex;
  align-items: center;
  gap: 16px;
  padding: 15px 25px;
  background: var(--bg-card);
  border-bottom: 2px solid #f0f0f0;
  border-left: 5px solid var(--primary-color);
}

.patient-info-bar .patient-badge {
  background: var(--primary-color);
  color: white;
  padding: 8px 16px;
  border-radius: var(--radius-small);
  font-weight: bold;
  font-size: 1.1rem;
}

.patient-details {
  flex: 1;
  display: flex;
  align-items: center;
  gap: 15px;
}

.patient-details .name {
  font-size: 1.2rem;
  font-weight: bold;
  color: var(--text-primary);
}

.patient-details .meta {
  font-size: 0.95rem;
  color: var(--text-secondary);
}

.patient-details .tag {
  background: #e8f4ff;
  color: var(--primary-color);
  padding: 4px 12px;
  border-radius: var(--radius-round);
  font-size: 0.85rem;
}

/* ==================== 多选患者头部 ==================== */

.multi-patient-header {
  display: flex;
  align-items: center;
  gap: 20px;
  flex: 1;
}

.selected-count {
  display: flex;
  align-items: center;
  gap: 8px;
}

.count-badge {
  background: linear-gradient(135deg, var(--primary-color) 0%, #66b1ff 100%);
  color: white;
  padding: 6px 14px;
  border-radius: 20px;
  font-weight: bold;
  font-size: 1.2rem;
  box-shadow: 0 3px 8px rgba(64, 158, 255, 0.3);
}

.count-text {
  font-size: 0.95rem;
  color: var(--text-regular);
  font-weight: 500;
}

.patient-badges {
  display: flex;
  align-items: center;
  gap: 6px;
  flex-wrap: wrap;
}

.mini-badge {
  background: #e8f4ff;
  color: var(--primary-color);
  padding: 4px 10px;
  border-radius: 6px;
  font-size: 0.85rem;
  font-weight: 600;
  border: 1px solid var(--primary-color);
  transition: all 0.2s;
}

.mini-badge:hover {
  background: var(--primary-color);
  color: white;
  transform: translateY(-1px);
  box-shadow: 0 2px 6px rgba(64, 158, 255, 0.3);
}

.more-badge {
  background: #f3f4f6;
  color: #6b7280;
  padding: 4px 10px;
  border-radius: 6px;
  font-size: 0.85rem;
  font-weight: 600;
}

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

/* ==================== 医嘱中的患者标签 ==================== */

.order-patient-tag {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 10px;
  padding: 6px 0;
  border-bottom: 1px solid #e8e8e8;
}

.patient-bed-tag {
  background: linear-gradient(135deg, #8b5cf6 0%, #a78bfa 100%);
  color: white;
  padding: 3px 10px;
  border-radius: 6px;
  font-size: 0.8rem;
  font-weight: 700;
  letter-spacing: 0.5px;
  box-shadow: 0 2px 4px rgba(139, 92, 246, 0.25);
}

.patient-name-tag {
  font-size: 0.9rem;
  color: #4b5563;
  font-weight: 600;
}

/* ==================== 工作区 ==================== */

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

/* Tab标签页左边距对齐 */
.order-tabs {
  padding: 0;
}

.order-tabs :deep(.el-tabs__header) {
  margin: 0 0 15px 0;
  padding-left: 25px;
}

.order-tabs :deep(.el-tabs__nav-wrap::after) {
  height: 1px;
  background-color: #e4e7ed;
}

.order-tabs :deep(.el-tabs__item) {
  font-size: 0.95rem;
  font-weight: 500;
  padding: 0 20px;
  height: 40px;
  line-height: 40px;
}

.order-tabs :deep(.el-tabs__item.is-active) {
  color: var(--primary-color);
  font-weight: 600;
}

/* ==================== 医嘱列表 ==================== */

.order-list {
  padding: 0 25px 16px 25px;
  overflow-y: auto;
  max-height: calc(100vh - 280px);
}

.batch-toolbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 12px 16px;
  background: var(--bg-secondary);
  border-radius: var(--radius-medium);
  margin-bottom: 16px;
  border: 1px solid var(--border-color);
}

.batch-actions {
  display: flex;
  gap: 12px;
}

/* 统一操作按钮样式 */
.action-btn {
  padding: 10px 20px !important;
  font-size: 0.95rem !important;
  font-weight: 600 !important;
  border-radius: var(--radius-small) !important;
  transition: all 0.3s !important;
}

.action-btn:not(:disabled):hover {
  transform: translateY(-1px);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
}

.action-btn.el-button--primary {
  background: var(--primary-color) !important;
  border-color: var(--primary-color) !important;
}

.action-btn.el-button--primary:not(:disabled):hover {
  background: #66b1ff !important;
  border-color: #66b1ff !important;
}

.action-btn.el-button--danger {
  background: var(--danger-color) !important;
  border-color: var(--danger-color) !important;
}

.action-btn.el-button--danger:not(:disabled):hover {
  background: #f78989 !important;
  border-color: #f78989 !important;
}

.order-item {
  display: flex;
  gap: 12px;
  padding: 16px;
  border: 1.5px solid var(--border-color);
  border-radius: var(--radius-medium);
  margin-bottom: 12px;
  transition: all 0.3s;
  background: white;
  align-items: center;
}

.order-item:hover {
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.08);
  border-color: var(--primary-color);
  transform: translateY(-2px);
}

.order-item.stopped {
  background: #fff5f5;
  border-color: #fbc4c4;
}

.order-content {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.order-header {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
}

.order-text {
  font-weight: 600;
  font-size: 0.95rem;
  color: var(--text-primary);
}

.order-details {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.detail-section {
  display: flex;
  gap: 8px;
  font-size: 0.85rem;
  line-height: 1.6;
}

.detail-label {
  color: var(--text-secondary);
  min-width: 70px;
  font-weight: 500;
}

.detail-value {
  color: var(--text-regular);
  flex: 1;
}

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
  color: var(--primary-color);
  font-size: 0.9rem;
}

.drug-spec {
  color: var(--text-secondary);
  font-size: 0.8rem;
}

.drug-dose {
  font-weight: 600;
  color: var(--success-color);
  font-size: 0.9rem;
}

.drug-note {
  color: var(--warning-color);
  font-size: 0.8rem;
  font-style: italic;
}

.order-meta {
  display: flex;
  gap: 16px;
  font-size: 0.8rem;
  color: var(--text-secondary);
  margin-top: 4px;
  padding-top: 8px;
  border-top: 1px dashed var(--border-color);
}

.order-actions {
  display: flex;
  flex-direction: column;
  gap: 8px;
  justify-content: center;
  align-items: center;
  min-width: 90px;
  flex-shrink: 0;
}

.order-actions :deep(.el-button + .el-button) {
  margin-left: 0 !important;
}

/* 单个医嘱操作按钮统一样式 */
.action-btn-small {
  width: 80px !important;
  height: 32px !important;
  padding: 0 !important;
  margin: 0 !important;
  font-size: 0.9rem !important;
  font-weight: 600 !important;
  border-radius: var(--radius-small) !important;
  transition: all 0.3s !important;
  display: flex !important;
  align-items: center !important;
  justify-content: center !important;
  line-height: 1 !important;
}

.action-btn-small:not(:disabled):hover {
  transform: translateY(-1px);
  box-shadow: 0 3px 8px rgba(0, 0, 0, 0.15);
}

.action-btn-small.el-button--primary {
  background: var(--primary-color) !important;
  border-color: var(--primary-color) !important;
}

.action-btn-small.el-button--primary:not(:disabled):hover {
  background: #66b1ff !important;
  border-color: #66b1ff !important;
}

.action-btn-small.el-button--danger {
  background: var(--danger-color) !important;
  border-color: var(--danger-color) !important;
}

.action-btn-small.el-button--danger:not(:disabled):hover {
  background: #f78989 !important;
  border-color: #f78989 !important;
}

/* ==================== 空状态 ==================== */

.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 60px 20px;
  color: var(--text-secondary);
}

/* ==================== 医嘱操作确认弹窗样式 ==================== */

:deep(.order-action-confirm) {
  width: 500px;
  max-width: 90vw;
}

:deep(.order-action-confirm .el-message-box__message) {
  line-height: 1.6;
}

:deep(.order-action-confirm .el-message-box__message > div) {
  margin-top: 10px;
}

.empty-icon {
  font-size: 64px;
  margin-bottom: 16px;
  opacity: 0.5;
}

.empty-state p {
  font-size: 1rem;
  color: var(--text-secondary);
}

/* ==================== 响应式 ==================== */

@media (max-width: 768px) {
  .order-acknowledgement {
    grid-template-columns: 1fr;
  }

  .patient-panel {
    display: none;
  }

  .patient-panel.collapsed {
    display: none;
  }
}
</style>
