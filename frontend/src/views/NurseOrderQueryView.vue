<template>
  <div class="nurse-order-query-view">
    <!-- ============================== 
      【护士端医嘱查询界面】
      左侧：患者列表（多选模式）
      右侧：医嘱查询工作区
    ============================== -->

    <!-- 左侧患者列表面板 -->
    <PatientListPanel 
      :patient-list="patientList"
      :selected-patients="selectedPatients"
      :my-ward-id="currentScheduledWardId"
      :multi-select="enableMultiSelect"
      :enable-multi-select-mode="true"
      title="患者列表"
      :show-pending-filter="false"
      :show-badge="false"
      :collapsed="false"
      @patient-select="handlePatientSelect"
      @multi-select-toggle="handleMultiSelectToggle"
    >
      <template #bottom-actions>
        <el-button type="primary" @click="openBarcodePrintDialog" size="small" style="width: 100%;">
          🖨️ 单据打印
        </el-button>
      </template>
    </PatientListPanel>

    <!-- 右侧医嘱查询工作区 -->
    <div class="work-area">
      <!-- 患者信息栏 -->
      <PatientInfoBar 
        :patients="selectedPatients"
        :is-multi-select="enableMultiSelect"
        :show-sort-control="selectedPatients.length > 1"
        :sort-by="sortBy"
        @sort-change="handleSortChange"
      />

      <!-- 未选择患者提示 -->
      <div v-if="selectedPatients.length === 0" class="no-patient-hint">
        <el-icon><InfoFilled /></el-icon>
        <span>请从左侧患者列表中选择患者查看医嘱</span>
      </div>

      <!-- 工作区内容容器 -->
      <div v-if="selectedPatients.length > 0" class="content-container">
        <!-- ==================== 筛选工具栏 ==================== -->
        <div class="filter-toolbar">
          <!-- 时间范围筛选 -->
          <div class="filter-group">
            <span class="filter-label">开具时间:</span>
            <el-date-picker
              v-model="timeRange"
              type="datetimerange"
              range-separator="至"
              start-placeholder="开始时间"
              end-placeholder="结束时间"
              value-format="YYYY-MM-DDTHH:mm:ss"
              @change="applyFilters"
              class="time-picker"
              size="small"
            />
          </div>

          <!-- 医嘱类型筛选 -->
          <div class="filter-group">
            <span class="filter-label">类型:</span>
            <el-checkbox-group v-model="typeFilter" @change="applyFilters" size="small">
              <el-checkbox label="MedicationOrder">药品</el-checkbox>
              <el-checkbox label="InspectionOrder">检查</el-checkbox>
              <el-checkbox label="OperationOrder">操作</el-checkbox>
              <el-checkbox label="SurgicalOrder">手术</el-checkbox>
              <el-checkbox label="DischargeOrder">出院</el-checkbox>
            </el-checkbox-group>
          </div>

          <!-- 医嘱状态筛选 -->
          <div class="filter-group">
            <span class="filter-label">状态:</span>
            <el-checkbox-group v-model="statusFilter" @change="applyFilters" size="small">
              <el-checkbox :label="1">未签收</el-checkbox>
              <el-checkbox :label="2">已签收</el-checkbox>
              <el-checkbox :label="3">进行中</el-checkbox>
              <el-checkbox :label="4">已结束</el-checkbox>
              <el-checkbox :label="7">已退回</el-checkbox>
              <el-checkbox :label="9">停止中</el-checkbox>
            </el-checkbox-group>
          </div>

          <!-- 新开/新停筛选 -->
          <div class="filter-group">
            <span class="filter-label">标识:</span>
            <el-checkbox v-model="showNewCreated" @change="applyFilters" size="small">
              新开
            </el-checkbox>
            <el-checkbox v-model="showNewStopped" @change="applyFilters" size="small">
              新停
            </el-checkbox>
          </div>

          <!-- 内容搜索 -->
          <div class="filter-group search-group">
            <el-input
              v-model="searchKeyword"
              placeholder="搜索医嘱内容（药品名/检查项/手术名）"
              clearable
              @input="applyFilters"
              size="small"
              class="search-input"
            >
              <template #prefix>
                <el-icon><Search /></el-icon>
              </template>
            </el-input>
          </div>
        </div>

        <!-- ==================== 医嘱列表 ==================== -->
        <div class="order-list-container">
          <!-- 加载状态 -->
          <div v-if="loading" class="loading-state">
            <el-icon class="is-loading"><Loading /></el-icon>
            <p>加载中...</p>
          </div>

          <!-- 空状态 -->
          <div v-else-if="displayOrders.length === 0" class="empty-state">
            <div class="empty-icon">📋</div>
            <p>暂无符合条件的医嘱</p>
          </div>

          <!-- 医嘱列表：按时间混合排序 -->
          <div v-else-if="sortBy === 'time'" class="order-list">
            <div 
              v-for="order in displayOrders" 
              :key="order.id"
              class="order-card"
              @click="handleOrderClick(order)"
            >
              <!-- 医嘱头部 -->
              <div class="order-header">
                <!-- 状态标签 -->
                <el-tag 
                  :type="getStatusColor(order.status)" 
                  size="small"
                  class="status-tag"
                >
                  {{ getStatusText(order.status) }}
                </el-tag>

                <!-- 医嘱类型标签 -->
                <el-tag 
                  :type="getOrderTypeColor(order.orderType)" 
                  size="small"
                >
                  {{ getOrderTypeName(order.orderType) }}
                </el-tag>

                <!-- 长期/临时标签 -->
                <el-tag 
                  :type="order.isLongTerm ? 'primary' : 'warning'" 
                  size="small"
                >
                  {{ order.isLongTerm ? '长期' : '临时' }}
                </el-tag>

                <!-- 新开医嘱徽章 -->
                <span 
                  v-if="isNewlyCreated(order)" 
                  class="new-badge"
                  title="24小时内新开医嘱"
                >
                  🆕 新开
                </span>

                <!-- 新停医嘱徽章 -->
                <span 
                  v-if="isNewlyStopped(order)" 
                  class="new-stopped-badge"
                  title="24小时内新停医嘱"
                >
                  🛑 新停
                </span>

                <!-- 医嘱ID和摘要 -->
                <span class="order-id">#{{ order.id }}</span>
                <span class="order-summary">{{ formatOrderSummary(order) }}</span>

                <!-- 患者信息（多患者模式下显示） -->
                <span v-if="selectedPatients.length > 1" class="patient-badge-mini">
                  {{ order.bedId }} {{ order.patientName }}
                </span>
              </div>

              <!-- 医嘱元信息 -->
              <div class="order-meta">
                <div class="meta-row">
                  <span class="meta-label">开单医生:</span>
                  <span class="meta-value">{{ order.doctorName }}</span>
                </div>
                <div class="meta-row">
                  <span class="meta-label">创建时间:</span>
                  <span class="meta-value">{{ formatDateTime(order.createTime) }}</span>
                </div>
                <div class="meta-row">
                  <span class="meta-label">计划结束:</span>
                  <span class="meta-value">{{ formatDateTime(order.plantEndTime) }}</span>
                </div>
              </div>

              <!-- 任务统计 -->
              <div class="order-tasks-summary">
                <span class="task-count">任务: {{ getCompletedTaskCount(order) }}/{{ order.taskCount }}</span>
                <el-progress 
                  :percentage="calculateTaskProgress(order)" 
                  :color="getProgressColor(order)"
                  :stroke-width="6"
                  style="width: 200px;"
                />
              </div>

              <!-- 操作按钮区 -->
              <div class="order-actions">
                <!-- 检查医嘱：显示查看报告按钮 -->
                <el-button 
                  v-if="order.orderType === 'InspectionOrder'"
                  :type="order.reportId ? 'success' : 'info'"
                  :disabled="!order.reportId"
                  size="small"
                  @click.stop="handleViewReport(order)"
                >
                  📄 {{ order.reportId ? '查看报告' : '报告未出' }}
                </el-button>
                <el-button 
                  type="primary" 
                  size="small"
                  @click.stop="viewOrderDetail(order)"
                >
                  查看详情
                </el-button>
              </div>
            </div>
          </div>

          <!-- 医嘱列表：按患者分组排序 -->
          <div v-else class="order-list-grouped">
            <div 
              v-for="patient in selectedPatients" 
              :key="patient.patientId"
              class="patient-group"
            >
              <div class="patient-group-header">
                <span class="bed-badge">{{ patient.bedId }}</span>
                <span class="patient-name">{{ patient.patientName }}</span>
                <span class="order-count">{{ getOrderCountByPatient(patient.patientId) }} 条医嘱</span>
              </div>

              <div class="patient-orders">
                <div 
                  v-for="order in getOrdersByPatientId(patient.patientId)" 
                  :key="order.id"
                  class="order-card"
                  @click="handleOrderClick(order)"
                >
                  <!-- 医嘱头部 -->
                  <div class="order-header">
                    <el-tag :type="getStatusColor(order.status)" size="small">
                      {{ getStatusText(order.status) }}
                    </el-tag>
                    <el-tag :type="getOrderTypeColor(order.orderType)" size="small">
                      {{ getOrderTypeName(order.orderType) }}
                    </el-tag>
                    <el-tag :type="order.isLongTerm ? 'primary' : 'warning'" size="small">
                      {{ order.isLongTerm ? '长期' : '临时' }}
                    </el-tag>
                    <span v-if="isNewlyCreated(order)" class="new-badge">🆕 新开</span>
                    <span v-if="isNewlyStopped(order)" class="new-stopped-badge">🛑 新停</span>
                    <span class="order-id">#{{ order.id }}</span>
                    <span class="order-summary">{{ formatOrderSummary(order) }}</span>
                  </div>

                  <div class="order-meta">
                    <div class="meta-row">
                      <span class="meta-label">开单医生:</span>
                      <span class="meta-value">{{ order.doctorName }}</span>
                    </div>
                    <div class="meta-row">
                      <span class="meta-label">创建时间:</span>
                      <span class="meta-value">{{ formatDateTime(order.createTime) }}</span>
                    </div>
                  </div>

                  <div class="order-tasks-summary">
                    <span class="task-count">任务: {{ getCompletedTaskCount(order) }}/{{ order.taskCount }}</span>
                    <el-progress 
                      :percentage="calculateTaskProgress(order)" 
                      :color="getProgressColor(order)"
                      :stroke-width="6"
                      style="width: 180px;"
                    />
                  </div>

                  <div class="order-actions">
                    <!-- 检查医嘱：显示查看报告按钮 -->
                    <el-button 
                      v-if="order.orderType === 'InspectionOrder'"
                      :type="order.reportId ? 'success' : 'info'"
                      :disabled="!order.reportId"
                      size="small"
                      @click.stop="handleViewReport(order)"
                    >
                      📄 {{ order.reportId ? '查看报告' : '报告未出' }}
                    </el-button>
                    <el-button type="primary" size="small" @click.stop="viewOrderDetail(order)">
                      查看详情
                    </el-button>
                  </div>
                </div>

                <!-- 该患者无医嘱 -->
                <div v-if="getOrderCountByPatient(patient.patientId) === 0" class="no-orders">
                  暂无符合条件的医嘱
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- ==================== 医嘱详情弹窗 ==================== -->
    <el-dialog
      v-model="detailDialogVisible"
      :title="`医嘱详情 - ${currentOrderDetail?.summary || ''}`"
      width="900px"
      class="order-detail-dialog"
      :close-on-click-modal="false"
    >
      <div class="order-detail-dialog-body">
        <OrderDetailPanel 
          v-if="currentOrderDetail"
          :detail="currentOrderDetail"
          :nurse-mode="true"
          @update-task-execution="handleUpdateTaskExecution"
          @print-task-sheet="handlePrintTaskSheet"
          @print-inspection-guide="handlePrintInspectionGuide"
          @view-inspection-report="handleViewInspectionReport"
          @task-updated="handleTaskUpdated"
        />
      </div>
    </el-dialog>

    <!-- ==================== 检查导引单打印弹窗 ==================== -->
    <InspectionGuidePrintDialog
      v-model="guidePrintDialogVisible"
      :order-id="currentGuideOrderId"
      :task-id="currentGuideTaskId"
    />

    <!-- ==================== 单据打印弹窗 ==================== -->
    <el-dialog
      v-model="barcodePrintDialogVisible"
      title="📋 任务单据打印"
      width="1200px"
      :close-on-click-modal="false"
      class="barcode-print-dialog"
    >
      <div class="barcode-print-container">
        <!-- 患者姓名搜索 -->
        <div v-if="!loadingBarcodes && barcodeList.length > 0" class="barcode-search-bar">
          <el-input
            v-model="barcodeSearchText"
            placeholder="输入患者姓名搜索..."
            clearable
            prefix-icon="Search"
            class="search-input"
          >
            <template #append>
              <span class="search-count">{{ filteredBarcodeList.length }}/{{ barcodeList.length }}</span>
            </template>
          </el-input>
        </div>

        <div v-if="loadingBarcodes" class="loading-state">
          <el-icon class="is-loading"><Loading /></el-icon>
          <p>加载条形码中...</p>
        </div>

        <div v-else-if="barcodeList.length === 0" class="empty-state">
          <p>暂无生成的任务条形码</p>
        </div>

        <div v-else-if="filteredBarcodeList.length === 0" class="empty-state">
          <p>未找到匹配的患者</p>
        </div>

        <div v-else class="barcode-grid">
          <div
            v-for="barcode in filteredBarcodeList"
            :key="barcode.taskId"
            class="barcode-item"
            :class="{ 'selected': selectedBarcodes.includes(barcode.taskId) }"
            @click="toggleBarcodeSelection(barcode.taskId)"
          >
            <div class="barcode-checkbox">
              <el-checkbox
                :model-value="selectedBarcodes.includes(barcode.taskId)"
                @change="toggleBarcodeSelection(barcode.taskId)"
              />
            </div>
            <div class="barcode-image">
              <img :src="barcode.barcodeBase64" :alt="`任务 ${barcode.taskId}`" />
            </div>
            <div class="barcode-info">
              <div class="info-row">
                <span class="label">患者:</span>
                <span class="value">{{ barcode.patientName }} ({{ barcode.patientId }})</span>
              </div>
              <div class="info-row">
                <span class="label">任务:</span>
                <span class="value">{{ barcode.orderSummary }}</span>
              </div>
              <div class="info-row">
                <span class="label">类型:</span>
                <span class="value">{{ getTaskCategoryName(barcode.taskCategory) }}</span>
              </div>
              <div class="info-row">
                <span class="label">计划时间:</span>
                <span class="value">{{ formatDateTime(barcode.plannedTime) }}</span>
              </div>
            </div>
          </div>
        </div>
      </div>

      <template #footer>
        <div class="dialog-footer">
          <div class="footer-info">
            已选择 {{ selectedBarcodes.length }} 个条形码
          </div>
          <div class="footer-actions">
            <el-button @click="barcodePrintDialogVisible = false">取消</el-button>
            <el-button type="primary" @click="selectAllBarcodes">全选</el-button>
            <el-button type="primary" @click="printSelectedBarcodes" :disabled="selectedBarcodes.length === 0">
              打印选中
            </el-button>
          </div>
        </div>
      </template>
    </el-dialog>


  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue';
import { ElMessage } from 'element-plus';
import { InfoFilled, Search, Loading } from '@element-plus/icons-vue';
import PatientListPanel from '@/components/PatientListPanel.vue';
import PatientInfoBar from '@/components/PatientInfoBar.vue';
import OrderDetailPanel from '@/components/OrderDetailPanel.vue';
import InspectionGuidePrintDialog from '@/components/InspectionGuidePrintDialog.vue';
import { usePatientData } from '@/composables/usePatientData';
import { 
  queryMultiPatientOrders, 
  isNewlyCreatedOrder,
  isNewlyStoppedOrder,
  applyNewOrderFilter,
  applyContentSearch,
  getOrderDetail 
} from '@/api/nurseOrder';

// ==================== 患者数据管理 ====================
const { 
  patientList,
  selectedPatient, 
  selectedPatients,
  enableMultiSelect,
  currentScheduledWardId,
  selectPatient,
  clearSelection,
  toggleMultiSelectMode,
  initializePatientData
} = usePatientData();

// ==================== 排序方式 ====================
// 'time': 按时间混合排序（所有患者的医嘱按时间排列）
// 'patient': 按患者分组排序（先按患者分组，组内按时间排列）
const sortBy = ref('time');

// ==================== 筛选条件 ====================
// 时间范围
const timeRange = ref(null);
// 医嘱类型（默认显示所有类型）
const typeFilter = ref(['MedicationOrder', 'InspectionOrder', 'OperationOrder', 'SurgicalOrder', 'DischargeOrder']);
// 医嘱状态（默认显示未签收(1)、已签收(2)、进行中(3)、停止中(9)）
const statusFilter = ref([1, 2, 3, 9]);
// 新开医嘱筛选
const showNewCreated = ref(false);
// 新停医嘱筛选
const showNewStopped = ref(false);
// 搜索关键词
const searchKeyword = ref('');

// ==================== 医嘱数据 ====================
const orderList = ref([]); // 原始医嘱列表
const loading = ref(false);

// ==================== 医嘱详情弹窗 ====================
const detailDialogVisible = ref(false);
const currentOrderDetail = ref(null);

// ==================== 检查导引单打印弹窗 ====================
const guidePrintDialogVisible = ref(false);
const currentGuideOrderId = ref(null);
const currentGuideTaskId = ref(null);

// ==================== 单据打印弹窗 ====================
const barcodePrintDialogVisible = ref(false);
const loadingBarcodes = ref(false);
const barcodeList = ref([]);
const selectedBarcodes = ref([]);
const barcodeSearchText = ref(''); // 患者姓名搜索

/**
 * 根据患者姓名筛选后的条形码列表
 */
const filteredBarcodeList = computed(() => {
  if (!barcodeSearchText.value.trim()) {
    return barcodeList.value;
  }
  const searchText = barcodeSearchText.value.trim().toLowerCase();
  return barcodeList.value.filter(barcode => 
    barcode.patientName && barcode.patientName.toLowerCase().includes(searchText)
  );
});

// ==================== 计算属性 ====================
/**
 * 显示的医嘱列表（应用所有筛选条件后）
 */
const displayOrders = computed(() => {
  let filtered = [...orderList.value];

  // 应用新开/新停筛选
  if (showNewCreated.value || showNewStopped.value) {
    filtered = applyNewOrderFilter(filtered, {
      showNewCreated: showNewCreated.value,
      showNewStopped: showNewStopped.value,
      hoursThreshold: 24
    });
  }

  // 应用内容搜索
  if (searchKeyword.value) {
    filtered = applyContentSearch(filtered, searchKeyword.value);
  }

  // 按时间排序（降序，最新的在前）
  filtered.sort((a, b) => new Date(b.createTime) - new Date(a.createTime));

  return filtered;
});

// ==================== 患者选择处理 ====================
/**
 * 处理患者选择事件
 * @param {Object} eventData - 事件数据
 * @param {Object} eventData.patient - 选中的患者对象
 * @param {boolean} eventData.isMultiSelect - 是否为多选模式
 */
const handlePatientSelect = (eventData) => {
  const { patient, isMultiSelect } = eventData;
  selectPatient(patient, isMultiSelect);
  
  console.log(`✅ 患者选择事件: ${patient.patientName}, 多选模式: ${isMultiSelect}`);
  console.log(`📊 当前选中患者数: ${selectedPatients.value.length}`);
};

/**
 * 处理多选模式切换
 * @param {boolean} enabled - 是否启用多选
 */
const handleMultiSelectToggle = (enabled) => {
  toggleMultiSelectMode(enabled);
  console.log(`🔄 多选模式切换: ${enabled ? '开启' : '关闭'}`);
};

// ==================== 排序切换处理 ====================
/**
 * 处理排序方式变化
 * @param {string} value - 新的排序方式 ('time' | 'patient')
 */
const handleSortChange = (value) => {
  sortBy.value = value;
  console.log(`🔄 排序方式切换: ${value === 'time' ? '按时间' : '按患者'}`);
};

// ==================== 医嘱加载逻辑 ====================
/**
 * 加载多患者医嘱
 * 并发查询所有选中患者的医嘱，合并结果
 */
const loadOrders = async () => {
  if (selectedPatients.value.length === 0) {
    orderList.value = [];
    return;
  }

  // 如果没有选择任何状态，清空列表
  if (statusFilter.value.length === 0) {
    orderList.value = [];
    return;
  }

  // 如果没有选择任何类型，清空列表
  if (typeFilter.value.length === 0) {
    orderList.value = [];
    return;
  }

  loading.value = true;
  try {
    console.log(`🔄 开始加载 ${selectedPatients.value.length} 位患者的医嘱...`);

    // 状态映射：将前端筛选项映射为后端状态值
    const statusMapping = {
      1: [1, 8],  // 未签收 → PendingReceive(1), PendingStop(8)
      2: [2],     // 已签收 → Accepted(2)
      3: [3],     // 进行中 → InProgress(3)
      4: [4, 5],  // 已结束 → Completed(4), Stopped(5)
      6: [6],     // 已撤回 → Cancelled(6)
      7: [7],     // 已退回 → Rejected(7)
      9: [9]      // 停止中 → StoppingInProgress(9)
    };

    // 将选中的筛选项映射为实际状态值
    const mappedStatuses = [];
    statusFilter.value.forEach(filterValue => {
      if (statusMapping[filterValue]) {
        mappedStatuses.push(...statusMapping[filterValue]);
      }
    });

    // 构建筛选条件
    const filters = {
      statuses: mappedStatuses,
      orderTypes: typeFilter.value.length > 0 ? typeFilter.value : null,
      sortBy: 'CreateTime',
      sortDescending: true
    };

    // 添加时间范围
    if (timeRange.value && timeRange.value.length === 2) {
      // 将本地时间转换为 UTC 时间字符串（ISO 8601 格式）
      // PostgreSQL 要求 timestamp with time zone 必须是 UTC 格式
      const startDate = new Date(timeRange.value[0]);
      const endDate = new Date(timeRange.value[1]);
      
      filters.createTimeFrom = startDate.toISOString(); // 转换为 UTC: "2025-12-25T02:30:00.000Z"
      filters.createTimeTo = endDate.toISOString();     // 转换为 UTC: "2025-12-25T14:30:00.000Z"
      
      console.log(`🕐 时间范围筛选: ${timeRange.value[0]} ~ ${timeRange.value[1]}`);
      console.log(`🌍 转换为UTC: ${filters.createTimeFrom} ~ ${filters.createTimeTo}`);
    }

    // 并发查询多患者医嘱
    const result = await queryMultiPatientOrders(selectedPatients.value, filters);
    
    orderList.value = result.orders || [];
    
    console.log(`✅ 加载成功，共 ${orderList.value.length} 条医嘱`);
    
    if (orderList.value.length > 0) {
      //ElMessage.success(`加载了 ${orderList.value.length} 条医嘱`);
    }
  } catch (error) {
    console.error('❌ 加载医嘱列表失败:', error);
    ElMessage.error('加载医嘱列表失败');
    orderList.value = [];
  } finally {
    loading.value = false;
  }
};

/**
 * 应用筛选条件（重新加载医嘱）
 */
const applyFilters = () => {
  loadOrders();
};

// ==================== 按患者分组相关方法 ====================
/**
 * 获取指定患者的医嘱列表
 */
const getOrdersByPatientId = (patientId) => {
  return displayOrders.value.filter(order => order.patientId === patientId);
};

/**
 * 获取指定患者的医嘱数量
 */
const getOrderCountByPatient = (patientId) => {
  return getOrdersByPatientId(patientId).length;
};

// ==================== 医嘱详情查看 ====================
/**
 * 医嘱卡片点击事件
 */
const handleOrderClick = (order) => {
  viewOrderDetail(order);
};

/**
 * 查看医嘱详情
 */
const viewOrderDetail = async (order) => {
  try {
    console.log('📖 查看医嘱详情:', order.id, order.summary);
    
    // 获取完整的医嘱详情（包含任务列表）
    const detail = await getOrderDetail(order.id);
    currentOrderDetail.value = detail;
    detailDialogVisible.value = true;
    
    console.log('✅ 医嘱详情加载成功');
  } catch (error) {
    console.error('❌ 获取医嘱详情失败:', error);
    ElMessage.error('获取医嘱详情失败');
  }
};

/**
 * 修改任务执行情况（TODO：等待后端接口）
 */
const handleUpdateTaskExecution = (taskId) => {
  console.log('🔧 修改任务执行情况:', taskId);
  ElMessage.warning('此功能接口尚未实现，请等待后端开发');
  // TODO: 打开修改执行情况弹窗
  // TODO: 调用 updateTaskExecution(taskId, data) 接口
};

/**
 * 任务更新后的处理：刷新医嘱详情和医嘱列表
 */
const handleTaskUpdated = async (taskId) => {
  console.log('🔄 任务已更新，刷新数据:', taskId);
  
  try {
    // 1. 刷新当前医嘱详情
    if (currentOrderDetail.value && currentOrderDetail.value.id) {
      const orderId = currentOrderDetail.value.id;
      const detail = await getOrderDetail(orderId);
      currentOrderDetail.value = detail; // API 拦截器已自动解包 response.data
      console.log('✅ 医嘱详情已刷新');
    }
    
    // 2. 刷新医嘱列表
    await loadOrders();
    console.log('✅ 医嘱列表已刷新');
  } catch (error) {
    console.error('❌ 刷新数据失败:', error);
    // 不显示错误提示，因为任务已经成功执行
  }
};

/**
 * 打印任务执行单（TODO：等待后端接口）
 */
const handlePrintTaskSheet = (taskId) => {
  console.log('🖨️ 打印任务执行单:', taskId);
  ElMessage.warning('此功能接口尚未实现，请等待后端开发');
  // TODO: 调用 printTaskExecutionSheet(taskId) 接口
  // TODO: 下载并打开 PDF 文件
};

/**
 * 打印检查导引单
 */
const handlePrintInspectionGuide = ({ taskId, orderId }) => {
  console.log('🔬 打印检查导引单:', { taskId, orderId });
  currentGuideTaskId.value = taskId;
  currentGuideOrderId.value = orderId;
  guidePrintDialogVisible.value = true;
};

/**
 * 查看检查报告
 */
const handleViewInspectionReport = (reportInfo) => {
  console.log('📄 查看检查报告:', reportInfo);
  
  // 验证报告URL是否存在
  if (!reportInfo.reportUrl) {
    ElMessage.warning('报告文件不存在或尚未生成');
    return;
  }
  
  // 构建报告URL，使用相对路径
  const baseUrl = '';
  const reportUrl = `${baseUrl}/${reportInfo.reportUrl}`;
  
  console.log('🔗 报告URL:', reportUrl);
  
  // 在新窗口打开PDF报告
  window.open(reportUrl, '_blank');
  
  ElMessage.success('正在打开检查报告...');
};

/**
 * 直接从医嘱列表查看报告
 */
const handleViewReport = (order) => {
  console.log('📄 从医嘱列表查看报告:', order);
  
  if (!order.attachmentUrl) {
    ElMessage.warning('报告文件不存在');
    return;
  }
  
  // 构建报告URL
  const baseUrl = '';
  const reportUrl = `${baseUrl}/${order.attachmentUrl}`;
  
  console.log('🔗 报告URL:', reportUrl);
  
  // 在新窗口打开PDF报告
  window.open(reportUrl, '_blank');
  
  ElMessage.success('正在打开检查报告...');
};

// ==================== 单据打印相关 ====================
/**
 * 打开单据打印弹窗
 */
const openBarcodePrintDialog = async () => {
  barcodePrintDialogVisible.value = true;
  await loadTaskBarcodes();
};

/**
 * 加载任务条形码列表
 */
const loadTaskBarcodes = async () => {
  loadingBarcodes.value = true;
  try {
    // 不传递 wardId，显示所有条形码
    const response = await fetch(`/api/BarcodePrint/task-barcodes`);
    const result = await response.json();
    
    if (result.success) {
      barcodeList.value = result.data || [];
      selectedBarcodes.value = [];
      console.log(`✅ 加载了 ${barcodeList.value.length} 个任务条形码`, result);
    } else {
      throw new Error(result.message || '加载失败');
    }
  } catch (error) {
    console.error('❌ 加载任务条形码失败:', error);
    ElMessage.error('加载条形码列表失败: ' + error.message);
    barcodeList.value = [];
  } finally {
    loadingBarcodes.value = false;
  }
};

/**
 * 切换条形码选择状态
 */
const toggleBarcodeSelection = (taskId) => {
  const index = selectedBarcodes.value.indexOf(taskId);
  if (index > -1) {
    selectedBarcodes.value.splice(index, 1);
  } else {
    selectedBarcodes.value.push(taskId);
  }
};

/**
 * 全选条形码
 */
const selectAllBarcodes = () => {
  if (selectedBarcodes.value.length === barcodeList.value.length) {
    // 已全选，则取消全选
    selectedBarcodes.value = [];
  } else {
    // 未全选，则全选
    selectedBarcodes.value = barcodeList.value.map(b => b.taskId);
  }
};

/**
 * 打印选中的条形码
 */
const printSelectedBarcodes = () => {
  if (selectedBarcodes.value.length === 0) {
    ElMessage.warning('请先选择要打印的条形码');
    return;
  }

  console.log('🖨️ 打印选中的条形码:', selectedBarcodes.value);

  // 创建打印窗口
  const printWindow = window.open('', '_blank', 'width=800,height=600');
  
  if (!printWindow) {
    ElMessage.error('无法打开打印窗口，请检查浏览器弹窗拦截设置');
    return;
  }

  // 构建打印内容
  const selectedItems = barcodeList.value.filter(b => selectedBarcodes.value.includes(b.taskId));
  
  let printContent = `
    <!DOCTYPE html>
    <html>
    <head>
      <title>任务条形码打印</title>
      <style>
        body {
          font-family: Arial, sans-serif;
          padding: 20px;
        }
        .barcode-item {
          page-break-inside: avoid;
          margin-bottom: 30px;
          border: 1px solid #ddd;
          padding: 15px;
          border-radius: 8px;
        }
        .barcode-image {
          text-align: center;
          margin-bottom: 15px;
        }
        .barcode-image img {
          max-width: 100%;
          height: auto;
        }
        .barcode-info {
          font-size: 14px;
          line-height: 1.8;
        }
        .info-row {
          margin-bottom: 5px;
        }
        .label {
          font-weight: bold;
          color: #666;
          margin-right: 10px;
        }
        .value {
          color: #333;
        }
        @media print {
          .barcode-item {
            page-break-inside: avoid;
          }
        }
      </style>
    </head>
    <body>
      <h1>任务条形码单据</h1>
      <p>打印时间: ${new Date().toLocaleString('zh-CN')}</p>
      <hr>
  `;

  selectedItems.forEach(barcode => {
    printContent += `
      <div class="barcode-item">
        <div class="barcode-image">
          <img src="${barcode.barcodeBase64}" alt="任务 ${barcode.taskId}" />
        </div>
        <div class="barcode-info">
          <div class="info-row">
            <span class="label">患者:</span>
            <span class="value">${barcode.patientName} (${barcode.patientId})</span>
          </div>
          <div class="info-row">
            <span class="label">任务:</span>
            <span class="value">${barcode.orderSummary}</span>
          </div>
          <div class="info-row">
            <span class="label">类型:</span>
            <span class="value">${getTaskCategoryName(barcode.taskCategory)}</span>
          </div>
          <div class="info-row">
            <span class="label">计划时间:</span>
            <span class="value">${formatDateTime(barcode.plannedTime)}</span>
          </div>
        </div>
      </div>
    `;
  });

  printContent += `
    </body>
    </html>
  `;

  printWindow.document.write(printContent);
  printWindow.document.close();

  // 等待图片加载完成后打印
  printWindow.onload = () => {
    setTimeout(() => {
      printWindow.print();
    }, 500);
  };

  ElMessage.success(`正在打印 ${selectedBarcodes.value.length} 个任务条形码`);
};

/**
 * 获取任务类别名称
 */
const getTaskCategoryName = (category) => {
  const categoryMap = {
    'Immediate': '即刻执行',
    'Duration': '持续执行',
    'ResultPending': '结果等待',
    'DataCollection': '数据采集',
    'Verification': '核对用药',
    'ApplicationWithPrint': '检查申请',
    'DischargeConfirmation': '出院确认'
  };
  return categoryMap[category] || '其他任务';
};

// ==================== 新开/新停判断 ====================
/**
 * 判断是否为新开医嘱
 */
const isNewlyCreated = (order) => {
  return isNewlyCreatedOrder(order, 24);
};

/**
 * 判断是否为新停医嘱（24h内的PendingStop、StoppingInProgress、Stopped）
 */
const isNewlyStopped = (order) => {
  // 检查状态是否为停止相关状态
  if (order.status !== 8 && order.status !== 9 && order.status !== 5) {
    return false;
  }
  
  // 检查是否在24小时内
  if (!order.stopConfirmedAt && !order.createTime) {
    return false;
  }
  
  try {
    // 使用停止确认时间或创建时间
    const timeString = order.stopConfirmedAt || order.createTime;
    let utcString = timeString;
    if (!timeString.endsWith('Z') && !timeString.includes('+')) {
      utcString = timeString + 'Z';
    }
    const orderTime = new Date(utcString);
    const now = new Date();
    const hoursDiff = (now - orderTime) / (1000 * 60 * 60);
    return hoursDiff <= 24;
  } catch {
    return false;
  }
};

// ==================== 获取完成任务数 ====================
// 获取完成任务数（Completed + Incomplete）
// 注：后端 completedTaskCount 已包含 Completed 和 Incomplete 状态
const getCompletedTaskCount = (order) => {
  if (order.tasks && Array.isArray(order.tasks)) {
    // 如果有任务列表（如医嘱详情），从任务中重新计算
    return order.tasks.filter(task => 
      task.status === 5 || task.status === 'Completed' ||
      task.status === 8 || task.status === 'Incomplete'
    ).length;
  }
  // 否则直接使用后端返回的 completedTaskCount（已包含 Incomplete）
  return order.completedTaskCount || 0;
};

// ==================== 计算任务进度 ====================
const calculateTaskProgress = (order) => {
  if (order.taskCount === 0) return 0;
  const completedCount = getCompletedTaskCount(order);
  return Math.round((completedCount / order.taskCount) * 100);
};

const getProgressColor = (order) => {
  const progress = calculateTaskProgress(order);
  if (progress === 100) return '#67c23a';
  if (progress >= 50) return '#409eff';
  return '#e6a23c';
};

// ==================== 状态和类型映射 ====================
const getStatusText = (status) => {
  const statusMap = {
    0: '草稿',
    1: '未签收',
    2: '已签收',
    3: '进行中',
    4: '已完成',
    5: '已停止',
    6: '已取消',
    7: '已退回',
    8: '等待停嘱'
  };
  return statusMap[status] || `状态${status}`;
};

const getStatusColor = (status) => {
  const colorMap = {
    0: 'info',
    1: 'warning',
    2: 'primary',
    3: 'success',
    4: 'success',
    5: 'info',
    6: 'info',
    7: 'danger',
    8: 'warning'
  };
  return colorMap[status] || 'info';
};

const getOrderTypeName = (orderType) => {
  const nameMap = {
    MedicationOrder: '药品',
    InspectionOrder: '检查',
    OperationOrder: '操作',
    SurgicalOrder: '手术',
    DischargeOrder: '出院'
  };
  return nameMap[orderType] || orderType;
};

const getOrderTypeColor = (orderType) => {
  const colorMap = {
    MedicationOrder: 'success',
    InspectionOrder: 'info',
    OperationOrder: 'warning',
    SurgicalOrder: 'danger',
    DischargeOrder: 'primary'
  };
  return colorMap[orderType] || 'info';
};

// ==================== 格式化医嘱标题 ====================
const formatOrderSummary = (order) => {
  // 如果是出院医嘱，显示特殊格式
  if (order.orderType === 'DischargeOrder') {
    const dischargeTime = order.plantEndTime || order.createTime;
    return `出院医嘱-预计出院时间: ${formatDateTime(dischargeTime)}`;
  }
  // 其他医嘱直接返回 summary
  return order.summary;
};

// ==================== 格式化日期时间 ====================
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

// ==================== 监听患者选择变化 ====================
watch(selectedPatients, (newPatients) => {
  console.log(`📋 患者选择变化，当前选中: ${newPatients.length} 位患者`);
  if (newPatients.length > 0) {
    console.log('👥 选中的患者:', newPatients.map(p => `${p.patientName}(${p.bedId})`).join(', '));
    // 患者变化时重新加载医嘱
    loadOrders();
  } else {
    // 清空医嘱列表
    orderList.value = [];
  }
}, { deep: true });

// ==================== 组件挂载 ====================
onMounted(async () => {
  console.log('🚀 护士端医嘱查询界面初始化...');
  
  // 初始化患者数据（获取排班病区 + 加载患者列表）
  await initializePatientData();
  
  console.log(`✅ 初始化完成，当前排班病区: ${currentScheduledWardId.value}`);
  console.log(`📊 患者列表加载完成，共 ${patientList.value.length} 位患者`);
});
</script>

<style scoped>
/* ============================== 
  【护士端医嘱查询界面样式】
  完全复用医生端的设计系统
============================== */

/* ==================== 设计系统变量 ==================== */
.nurse-order-query-view {
  /* 主题色 */
  --primary-color: #409eff;
  --success-color: #67c23a;
  --warning-color: #e6a23c;
  --danger-color: #f56c6c;
  --info-color: #909399;
  
  /* 背景色 */
  --bg-page: #f4f7f9;
  --bg-card: #ffffff;
  --bg-secondary: #f9fafc;
  
  /* 边框和文本 */
  --border-color: #dcdfe6;
  --text-primary: #303133;
  --text-regular: #606266;
  --text-secondary: #909399;
  
  /* 圆角 */
  --radius-large: 8px;
  --radius-medium: 6px;
  --radius-small: 4px;

  /* 布局：网格布局，左侧患者列表250px，右侧自适应 */
  display: grid;
  grid-template-columns: 250px 1fr;
  height: calc(100vh - 60px);
  background: var(--bg-page);
  gap: 20px;
  padding: 20px;
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

/* ==================== 未选择患者提示 ==================== */
.no-patient-hint {
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

.no-patient-hint .el-icon {
  font-size: 1.2rem;
}

/* ==================== 内容容器 ==================== */
.content-container {
  flex: 1;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

/* ==================== 筛选工具栏 ==================== */
.filter-toolbar {
  flex-shrink: 0;
  background: white;
  border-bottom: 1px solid var(--border-color);
  padding: 15px 25px;
  display: flex;
  flex-wrap: wrap;
  gap: 20px;
  align-items: center;
}

.filter-group {
  display: flex;
  align-items: center;
  gap: 10px;
}

.filter-label {
  font-size: 0.9rem;
  font-weight: 600;
  color: var(--text-regular);
  white-space: nowrap;
}

.time-picker {
  width: 360px;
}

.search-group {
  flex: 1;
  min-width: 300px;
}

.search-input {
  width: 100%;
}

/* ==================== 医嘱列表容器 ==================== */
.order-list-container {
  flex: 1;
  overflow-y: auto;
  background: var(--bg-secondary);
}

/* ==================== 医嘱列表（按时间排序） ==================== */
.order-list {
  padding: 20px 25px;
  display: flex;
  flex-direction: column;
  gap: 16px;
}

/* ==================== 医嘱列表（按患者分组） ==================== */
.order-list-grouped {
  padding: 20px 25px;
  display: flex;
  flex-direction: column;
  gap: 24px;
}

.patient-group {
  background: white;
  border-radius: var(--radius-medium);
  overflow: hidden;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.06);
}

.patient-group-header {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 15px 20px;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
}

.bed-badge {
  background: rgba(255, 255, 255, 0.2);
  padding: 4px 12px;
  border-radius: var(--radius-small);
  font-weight: bold;
  font-size: 1rem;
}

.patient-name {
  font-size: 1.1rem;
  font-weight: 600;
  flex: 1;
}

.order-count {
  font-size: 0.9rem;
  opacity: 0.9;
}

.patient-orders {
  padding: 16px;
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.no-orders {
  text-align: center;
  color: var(--text-secondary);
  padding: 30px;
  font-size: 0.9rem;
}

/* ==================== 医嘱卡片 ==================== */
.order-card {
  padding: 20px;
  background: white;
  border: 2px solid var(--border-color);
  border-radius: var(--radius-medium);
  transition: all 0.3s;
  cursor: pointer;
}

.order-card:hover {
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.1);
  border-color: var(--primary-color);
  transform: translateY(-2px);
}

/* 分组模式下的卡片 */
.patient-orders .order-card {
  border-color: #e8e8e8;
}

.patient-orders .order-card:hover {
  border-color: var(--primary-color);
}

/* ==================== 医嘱头部 ==================== */
.order-header {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-bottom: 12px;
  flex-wrap: wrap;
}

.order-id {
  font-size: 0.85rem;
  font-weight: 700;
  color: var(--primary-color);
  background: #ecf5ff;
  padding: 2px 8px;
  border-radius: var(--radius-small);
  font-family: 'Courier New', monospace;
}

.order-summary {
  font-size: 1rem;
  font-weight: 600;
  color: var(--text-primary);
  flex: 1;
  min-width: 200px;
}

/* ==================== 新开/新停徽章 ==================== */
.new-badge {
  background: linear-gradient(135deg, #67c23a 0%, #85ce61 100%);
  color: white;
  padding: 4px 10px;
  border-radius: 12px;
  font-size: 0.8rem;
  font-weight: 700;
  box-shadow: 0 2px 4px rgba(103, 194, 58, 0.3);
}

.new-stopped-badge {
  background: linear-gradient(135deg, #e6a23c 0%, #f56c6c 100%);
  color: white;
  padding: 4px 10px;
  border-radius: 12px;
  font-size: 0.8rem;
  font-weight: 700;
  box-shadow: 0 2px 4px rgba(245, 108, 108, 0.3);
}

/* ==================== 患者信息徽章（多患者模式） ==================== */
.patient-badge-mini {
  background: linear-gradient(135deg, #409eff 0%, #66b1ff 100%);
  color: white;
  padding: 4px 12px;
  border-radius: 12px;
  font-size: 0.85rem;
  font-weight: 600;
  white-space: nowrap;
}

/* ==================== 医嘱元信息 ==================== */
.order-meta {
  display: flex;
  gap: 20px;
  margin-bottom: 12px;
  flex-wrap: wrap;
}

.meta-row {
  display: flex;
  gap: 6px;
  font-size: 0.85rem;
}

.meta-label {
  color: var(--text-secondary);
  font-weight: 500;
}

.meta-value {
  color: var(--text-regular);
  font-weight: 600;
}

/* ==================== 任务统计 ==================== */
.order-tasks-summary {
  display: flex;
  align-items: center;
  gap: 16px;
  margin-bottom: 12px;
}

.task-count {
  font-size: 0.9rem;
  font-weight: 600;
  color: var(--text-regular);
  min-width: 90px;
}

/* ==================== 操作按钮 ==================== */
.order-actions {
  display: flex;
  gap: 10px;
  justify-content: flex-end;
}

/* ==================== 加载和空状态 ==================== */
.loading-state,
.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 60px 20px;
  color: var(--text-secondary);
  gap: 16px;
}

.loading-state .el-icon {
  font-size: 48px;
}

.empty-icon {
  font-size: 64px;
  opacity: 0.5;
}

/* ==================== 医嘱详情弹窗 ==================== */
.order-detail-dialog :deep(.el-dialog__body) {
  padding: 20px;
  max-height: 70vh;
  overflow-y: auto;
}

.order-detail-dialog-body {
  max-height: 70vh;
  overflow-y: auto;
  padding-right: 8px;
}

/* 自定义滚动条样式 */
.order-detail-dialog-body::-webkit-scrollbar {
  width: 6px;
}

.order-detail-dialog-body::-webkit-scrollbar-track {
  background: #f1f1f1;
  border-radius: 3px;
}

.order-detail-dialog-body::-webkit-scrollbar-thumb {
  background: #c0c4cc;
  border-radius: 3px;
}

.order-detail-dialog-body::-webkit-scrollbar-thumb:hover {
  background: #909399;
}

/* ==================== 单据打印弹窗 ==================== */
.barcode-print-dialog .el-dialog__body {
  padding: 20px;
  max-height: 70vh;
  overflow-y: auto;
}

.barcode-print-container {
  min-height: 400px;
}

.barcode-search-bar {
  margin-bottom: 20px;
}

.barcode-search-bar .search-input {
  width: 100%;
}

.barcode-search-bar .search-count {
  color: #909399;
  font-size: 14px;
  padding: 0 10px;
  white-space: nowrap;
}

.barcode-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(350px, 1fr));
  gap: 20px;
}

.barcode-item {
  border: 2px solid #e4e7ed;
  border-radius: 8px;
  padding: 15px;
  cursor: pointer;
  transition: all 0.3s;
  position: relative;
  background: white;
}

.barcode-item:hover {
  border-color: #409eff;
  box-shadow: 0 2px 12px rgba(64, 158, 255, 0.2);
}

.barcode-item.selected {
  border-color: #409eff;
  background: #f0f7ff;
  box-shadow: 0 2px 12px rgba(64, 158, 255, 0.3);
}

.barcode-checkbox {
  position: absolute;
  top: 10px;
  right: 10px;
  z-index: 1;
}

.barcode-image {
  text-align: center;
  padding: 10px;
  background: white;
  border-radius: 4px;
  margin-bottom: 15px;
}

.barcode-image img {
  max-width: 100%;
  height: auto;
  display: block;
  margin: 0 auto;
}

.barcode-info {
  font-size: 13px;
}

.barcode-info .info-row {
  display: flex;
  margin-bottom: 8px;
  line-height: 1.5;
}

.barcode-info .label {
  font-weight: 600;
  color: #606266;
  min-width: 80px;
  flex-shrink: 0;
}

.barcode-info .value {
  color: #303133;
  flex: 1;
  word-break: break-word;
}

.dialog-footer {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 15px 20px;
  border-top: 1px solid #e4e7ed;
}

.footer-info {
  font-size: 14px;
  color: #606266;
  font-weight: 500;
}

.footer-actions {
  display: flex;
  gap: 10px;
}

/* ==================== 响应式布局 ==================== */
@media (max-width: 768px) {
  .nurse-order-query-view {
    grid-template-columns: 1fr;
    height: auto;
  }
}
</style>
