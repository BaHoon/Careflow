<template>
  <div class="doctor-order-view">
    <!-- 左侧患者列表面板 -->
    <PatientListPanel 
      :patient-list="patientList"
      :selected-patients="selectedPatients"
      :my-ward-id="currentDoctorWardId"
      :multi-select="false"
      title="患者列表"
      :show-pending-filter="false"
      :show-badge="false"
      :collapsed="false"
      @patient-select="handlePatientSelect"
    />

    <!-- 右侧医嘱查询工作区 -->
    <div class="work-area">
      <!-- 患者信息栏 -->
      <PatientInfoBar 
        :patients="selectedPatients"
        :is-multi-select="false"
        :show-sort-control="false"
      />

      <!-- 未选择患者提示 -->
      <div v-if="selectedPatients.length === 0" class="no-patient-hint">
        <el-icon><InfoFilled /></el-icon>
        <span>请从左侧患者列表中选择患者查看医嘱</span>
      </div>

      <!-- 筛选工具栏 -->
      <div v-if="selectedPatients.length > 0" class="filter-toolbar">
        <!-- 时间范围 -->
        <div class="filter-group">
          <span class="filter-label">开具时间:</span>
          <el-date-picker
            v-model="timeRange"
            type="datetimerange"
            range-separator="至"
            start-placeholder="开始时间"
            end-placeholder="结束时间"
            value-format="YYYY-MM-DDTHH:mm:ss"
            @change="loadOrders"
            class="time-picker"
            size="small"
          />
        </div>

        <!-- 类型筛选 -->
        <div class="filter-group">
          <span class="filter-label">类型:</span>
          <el-checkbox-group v-model="typeFilter" @change="loadOrders" size="small">
            <el-checkbox label="MedicationOrder">药品</el-checkbox>
            <el-checkbox label="InspectionOrder">检查</el-checkbox>
            <el-checkbox label="OperationOrder">操作</el-checkbox>
            <el-checkbox label="SurgicalOrder">手术</el-checkbox>
            <el-checkbox label="DischargeOrder">出院</el-checkbox>
          </el-checkbox-group>
        </div>

        <!-- 状态筛选 -->
        <div class="filter-group">
          <span class="filter-label">状态:</span>
          <el-checkbox-group v-model="statusFilter" @change="loadOrders" size="small">
            <el-checkbox :label="1">未签收</el-checkbox>
            <el-checkbox :label="2">已签收</el-checkbox>
            <el-checkbox :label="3">进行中</el-checkbox>
            <el-checkbox :label="4">已结束</el-checkbox>
            <el-checkbox :label="6">已取消</el-checkbox>
            <el-checkbox :label="7">已退回</el-checkbox>
            <el-checkbox :label="9">停止中</el-checkbox>
            <el-checkbox :label="10">异常态</el-checkbox>
          </el-checkbox-group>
        </div>

        <!-- 内容搜索 -->
        <div class="filter-group search-group">
          <el-input
            v-model="searchKeyword"
            placeholder="搜索医嘱内容（药品名/检查项/手术名）"
            clearable
            @input="loadOrders"
            size="small"
            class="search-input"
          >
            <template #prefix>
              <el-icon><Search /></el-icon>
            </template>
          </el-input>
        </div>
      </div>

      <!-- 医嘱列表 -->
      <div v-if="!loading && orderList.length > 0" class="order-list">
        <div 
          v-for="order in orderList" 
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

            <!-- 医嘱ID和摘要 -->
            <span class="order-id">#{{ order.id }}</span>
            <span class="order-summary">{{ formatOrderSummary(order) }}</span>

            <!-- 停嘱标识：只在医嘱处于停嘱相关状态时显示 -->
            <span 
              v-if="order.stopReason && (order.status === 8 || order.status === 5 || order.status === 9)" 
              class="stop-badge" 
              :title="order.stopReason"
            >
              🛑 {{ order.status === 9 ? '停止中' : '已停嘱' }}
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
            <el-button 
              type="primary" 
              size="small"
              @click.stop="viewOrderDetail(order)"
            >
              查看详情
            </el-button>
            <el-button 
              v-if="canStopOrder(order)"
              type="danger" 
              size="small"
              @click.stop="handleStopOrder(order)"
            >
              停止医嘱
            </el-button>
            <!-- 等待停嘱状态：撤回停嘱申请 -->
            <el-button 
              v-if="order.status === 8"
              type="warning" 
              size="small"
              @click.stop="handleWithdrawStop(order)"
            >
              撤回停嘱
            </el-button>
            <!-- 异常态医嘱：处理异常按钮 -->
            <el-button 
              v-if="order.status === 10"
              type="danger" 
              size="small"
              @click.stop="handleAbnormalOrder(order)"
            >
              处理异常
            </el-button>
            <!-- 已退回医嘱的操作按钮 -->
            <el-button 
              v-if="order.status === 7"
              type="success" 
              size="small"
              @click.stop="handleResubmit(order)"
            >
              重新提交
            </el-button>
            <el-button 
              v-if="order.status === 7"
              type="warning" 
              size="small"
              @click.stop="handleCancel(order)"
            >
              撤销
            </el-button>
          </div>
        </div>
      </div>

      <!-- 加载状态 -->
      <div v-if="loading" class="loading-state">
        <el-icon class="is-loading"><Loading /></el-icon>
        <p>加载中...</p>
      </div>

      <!-- 空状态 -->
      <div v-if="!loading && orderList.length === 0 && selectedPatients.length > 0" class="empty-state">
        <div class="empty-icon">📋</div>
        <p>该患者暂无符合条件的医嘱</p>
      </div>
    </div>

    <!-- 医嘱详情弹窗 -->
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
        />
      </div>
    </el-dialog>

    <!-- 停嘱确认弹窗 -->
    <el-dialog
      v-model="stopDialogVisible"
      title="停止医嘱"
      width="800px"
      class="stop-order-dialog"
      :close-on-click-modal="false"
    >
      <StopOrderPanel 
        v-if="currentStopOrder"
        :order="currentStopOrder"
        :tasks="currentStopOrder.tasks || []"
        @confirm="handleStopConfirm"
        @cancel="stopDialogVisible = false"
      />
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { Loading, InfoFilled, Search } from '@element-plus/icons-vue';
import PatientListPanel from '@/components/PatientListPanel.vue';
import PatientInfoBar from '@/components/PatientInfoBar.vue';
import OrderDetailPanel from '@/components/OrderDetailPanel.vue';
import StopOrderPanel from '@/components/StopOrderPanel.vue';
import { usePatientData } from '@/composables/usePatientData';
import { queryOrders, getOrderDetail, stopOrder, resubmitRejectedOrder, cancelRejectedOrder, withdrawStopOrder, handleAbnormalTask } from '@/api/doctorOrder';

// ==================== 患者数据 ====================
const { 
  patientList,
  selectedPatient, 
  selectedPatients,
  currentDoctorWardId,
  selectSinglePatient,
  initializePatientData,
  getCurrentDoctor
} = usePatientData();

// ==================== 筛选条件 ====================
// 默认显示未签收(1,8)、已签收(2)、进行中(3)、停止中(9)、异常态(10)的医嘱
const statusFilter = ref([1, 8, 2, 3, 9, 10]);
// 默认显示所有类型
const typeFilter = ref(['MedicationOrder', 'InspectionOrder', 'OperationOrder', 'SurgicalOrder', 'DischargeOrder']);
// 时间范围
const timeRange = ref(null);
// 搜索关键词
const searchKeyword = ref('');

// ==================== 医嘱列表数据 ====================
const orderList = ref([]);
const loading = ref(false);

// ==================== 详情弹窗 ====================
const detailDialogVisible = ref(false);
const currentOrderDetail = ref(null);

// ==================== 停嘱弹窗 ====================
const stopDialogVisible = ref(false);
const currentStopOrder = ref(null);

// ==================== 监听患者选择 ====================
watch(selectedPatient, () => {
  if (selectedPatient.value) {
    loadOrders();
  } else {
    orderList.value = [];
  }
});

// ==================== 患者选择处理 ====================
const handlePatientSelect = (eventData) => {
  const { patient } = eventData;
  selectSinglePatient(patient);
};

// ==================== 加载医嘱列表 ====================
const loadOrders = async () => {
  if (!selectedPatient.value) {
    orderList.value = [];
    return;
  }

  // 如果没有选择任何状态，不显示医嘱
  if (statusFilter.value.length === 0) {
    orderList.value = [];
    return;
  }

  loading.value = true;
  try {
    // 状态映射：将前端筛选项映射为后端状态值
    const statusMapping = {
      1: [1, 8],  // 未签收 → PendingReceive(1), PendingStop(8)
      2: [2],     // 已签收 → Accepted(2)
      3: [3],     // 进行中 → InProgress(3)
      4: [4, 5],  // 已结束 → Completed(4), Stopped(5)
      6: [6],     // 已撤回 → Cancelled(6)
      7: [7],     // 已退回 → Rejected(7)
      9: [9],     // 停止中 → StoppingInProgress(9)
      10: [10]    // 异常态 → Abnormal(10)
    };

    // 将选中的筛选项映射为实际状态值
    const mappedStatuses = [];
    statusFilter.value.forEach(filterValue => {
      if (statusMapping[filterValue]) {
        mappedStatuses.push(...statusMapping[filterValue]);
      }
    });

    const requestData = {
      patientId: selectedPatient.value.patientId,
      statuses: mappedStatuses,
      orderTypes: typeFilter.value.length > 0 ? typeFilter.value : null
    };

    // 添加时间范围
    if (timeRange.value && timeRange.value.length === 2) {
      requestData.createTimeFrom = timeRange.value[0];
      requestData.createTimeTo = timeRange.value[1];
    }

    const response = await queryOrders(requestData);
    let orders = response.orders || [];
    
    // 应用搜索过滤
    if (searchKeyword.value && searchKeyword.value.trim()) {
      const keyword = searchKeyword.value.trim().toLowerCase();
      orders = orders.filter(order => {
        // 搜索医嘱摘要/内容
        const summary = (order.summary || '').toLowerCase();
        const content = (order.orderContent || '').toLowerCase();
        return summary.includes(keyword) || content.includes(keyword);
      });
    }
    
    orderList.value = orders;
    
    console.log(`✅ 加载成功，共 ${orderList.value.length} 条医嘱`);
  } catch (error) {
    console.error('加载医嘱列表失败:', error);
    ElMessage.error('加载医嘱列表失败');
    orderList.value = [];
  } finally {
    loading.value = false;
  }
};

// ==================== 医嘱卡片点击 ====================
const handleOrderClick = (order) => {
  viewOrderDetail(order);
};

// ==================== 查看医嘱详情 ====================
const viewOrderDetail = async (order) => {
  try {
    const detail = await getOrderDetail(order.id);
    currentOrderDetail.value = detail;
    detailDialogVisible.value = true;
  } catch (error) {
    console.error('获取医嘱详情失败:', error);
    ElMessage.error('获取医嘱详情失败');
  }
};

// ==================== 停止医嘱 ====================
const handleStopOrder = async (order) => {
  try {
    // 特殊处理：出院医嘱且已签收或进行中状态，直接停止所有任务，不让医生选择
    if (order.orderType === 'DischargeOrder' && (order.status === 2 || order.status === 3)) {
      // 先获取任务列表，找到第一个任务作为停止节点
      const detail = await getOrderDetail(order.id);
      if (!detail.tasks || detail.tasks.length === 0) {
        ElMessage.error('该医嘱没有任务，无法停止');
        return;
      }

      await ElMessageBox.confirm(
        '出院医嘱停止后将停止所有相关任务，确认停止该医嘱吗？',
        '停止出院医嘱',
        {
          confirmButtonText: '确认停止',
          cancelButtonText: '取消',
          type: 'warning'
        }
      );

      const { value: stopReason } = await ElMessageBox.prompt(
        '请输入停止原因',
        '停止原因',
        {
          confirmButtonText: '确认',
          cancelButtonText: '取消',
          inputPattern: /\S+/,
          inputErrorMessage: '停止原因不能为空',
          inputType: 'textarea',
          inputPlaceholder: '例如：患者病情好转，无需出院'
        }
      );

      const currentDoctor = getCurrentDoctor();
      // 使用第一个任务作为停止节点（停止第一个任务后的所有任务，即停止所有任务）
      const firstTask = detail.tasks[0];
      const requestData = {
        orderId: order.id,
        doctorId: currentDoctor.staffId,
        stopReason: stopReason,
        stopAfterTaskId: firstTask.id
      };

      const result = await stopOrder(requestData);
      
      if (result.success) {
        ElMessage.success(`停嘱成功，已锁定 ${result.lockedTaskIds?.length || 0} 个任务`);
        await loadOrders();
      } else {
        ElMessage.error(result.message || '停嘱失败');
      }
      return;
    }

    // 其他医嘱：显示任务选择面板
    const detail = await getOrderDetail(order.id);
    currentStopOrder.value = {
      ...order,
      tasks: detail.tasks
    };
    stopDialogVisible.value = true;
  } catch (error) {
    if (error !== 'cancel') {
      console.error('停止医嘱失败:', error);
      ElMessage.error(error.message || '停止医嘱失败');
    }
  }
};

// 确认停嘱
const handleStopConfirm = async (stopData) => {
  try {
    const currentDoctor = getCurrentDoctor();
    
    const requestData = {
      orderId: stopData.orderId,
      doctorId: currentDoctor.staffId,
      stopReason: stopData.stopReason,
      stopAfterTaskId: stopData.stopAfterTaskId
    };

    const result = await stopOrder(requestData);
    
    if (result.success) {
      ElMessage.success(`停嘱成功，已锁定 ${result.lockedTaskIds?.length || 0} 个任务`);
      stopDialogVisible.value = false;
      currentStopOrder.value = null;
      
      // 刷新医嘱列表
      await loadOrders();
    } else {
      ElMessage.error(result.message || '停嘱失败');
    }
  } catch (error) {
    console.error('停嘱失败:', error);
    ElMessage.error('停嘱失败: ' + (error.message || '未知错误'));
  }
};

// ==================== 判断是否可以停止医嘱 ====================
const canStopOrder = (order) => {
  // 待签收(1)、已签收(2)、进行中(3)或停止中(9)状态可以停止
  if (order.status === 1 || order.status === 2 || order.status === 3 || order.status === 9) {
    return true;
  }
  
  // 不允许已停止(5)状态再次停止
  return false;
};

// ==================== 重新提交已退回的医嘱 ====================
const handleResubmit = async (order) => {
  try {
    await ElMessageBox.confirm(
      '确认重新提交该医嘱？提交后将重新进入护士待签收列表。',
      '重新提交确认',
      {
        confirmButtonText: '确认提交',
        cancelButtonText: '取消',
        type: 'warning'
      }
    );

    const currentDoctor = getCurrentDoctor();
    await resubmitRejectedOrder(order.id, currentDoctor.staffId);
    
    ElMessage.success('重新提交成功');
    await loadOrders();
  } catch (error) {
    if (error !== 'cancel') {
      console.error('重新提交失败:', error);
      ElMessage.error(error.message || '重新提交失败');
    }
  }
};

// ==================== 撤销已退回的医嘱 ====================
const handleCancel = async (order) => {
  try {
    const { value: cancelReason } = await ElMessageBox.prompt(
      '请输入撤销原因（撤销后医嘱将无法恢复）',
      '撤销医嘱',
      {
        confirmButtonText: '确认撤销',
        cancelButtonText: '取消',
        inputPattern: /\S+/,
        inputErrorMessage: '撤销原因不能为空',
        inputType: 'textarea'
      }
    );

    const currentDoctor = getCurrentDoctor();
    await cancelRejectedOrder(order.id, currentDoctor.staffId, cancelReason);
    
    ElMessage.success('撤销成功');
    await loadOrders();
  } catch (error) {
    if (error !== 'cancel') {
      console.error('撤销失败:', error);
      ElMessage.error(error.message || '撤销失败');
    }
  }
};

// ==================== 医生撤回停嘱申请 ====================
const handleWithdrawStop = async (order) => {
  try {
    const { value: withdrawReason } = await ElMessageBox.prompt(
      '确认撤回停嘱申请？撤回后医嘱将继续执行，被锁定的任务将解锁。',
      '撤回停嘱',
      {
        confirmButtonText: '确认撤回',
        cancelButtonText: '取消',
        inputPattern: /\S+/,
        inputErrorMessage: '撤回原因不能为空',
        inputType: 'textarea',
        inputPlaceholder: '请输入撤回原因，例如：病情有变化，暂不停嘱'
      }
    );

    const currentDoctor = getCurrentDoctor();
    const result = await withdrawStopOrder({
      orderId: order.id,
      doctorId: currentDoctor.staffId,
      withdrawReason: withdrawReason
    });

    if (result.success) {
      ElMessage.success(`撤回成功，已解锁 ${result.restoredTaskIds?.length || 0} 个任务`);
      await loadOrders();
    } else {
      ElMessage.error(result.message || '撤回失败');
    }
  } catch (error) {
    if (error !== 'cancel') {
      console.error('撤回停嘱失败:', error);
      ElMessage.error(error.message || '撤回停嘱失败');
    }
  }
};

// ==================== 处理异常态医嘱 ====================
const handleAbnormalOrder = async (order) => {
  try {
    const { value: handleNote } = await ElMessageBox.prompt(
      `医嘱当前为异常状态，请输入处理说明：`,
      '处理异常医嘱',
      {
        confirmButtonText: '确认处理',
        cancelButtonText: '取消',
        inputPlaceholder: '请输入处理说明',
        inputValidator: (value) => {
          if (!value || value.trim() === '') {
            return '请输入处理说明';
          }
          return true;
        }
      }
    );

    const currentDoctor = getCurrentDoctor();
    const result = await handleAbnormalTask({
      orderId: order.id,
      doctorId: currentDoctor.staffId,
      handleNote: handleNote.trim()
    });

    if (result.success) {
      const statusText = result.newOrderStatus === 3 ? '进行中' : '已完成';
      ElMessage.success(`处理成功，医嘱状态已变更为【${statusText}】`);
      await loadOrders();
    } else {
      ElMessage.error(result.message || '处理失败');
    }
  } catch (error) {
    if (error !== 'cancel') {
      console.error('处理异常医嘱失败:', error);
      ElMessage.error(error.message || '处理异常医嘱失败');
    }
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

// ==================== 进度条颜色 ====================
const getProgressColor = (order) => {
  const progress = calculateTaskProgress(order);
  if (progress === 100) return '#67c23a';
  if (progress >= 50) return '#409eff';
  return '#e6a23c';
};

// ==================== 状态映射 ====================
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
    8: '等待停嘱',
    9: '停止中',
    10: '异常态'
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
    8: 'warning',
    9: 'warning',  // 停止中显示为警告色
    10: 'danger'   // 异常态显示为危险色
  };
  return colorMap[status] || 'info';
};

// ==================== 类型映射 ====================
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

// ==================== 组件挂载 ====================
onMounted(async () => {
  // 医生端不需要排班信息，跳过排班检查
  await initializePatientData(null, true);
});
</script>

<style scoped>
/* ==================== 主布局 ==================== */
.doctor-order-view {
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
  max-width: 400px;
}

/* ==================== 医嘱列表 ==================== */
.order-list {
  flex: 1;
  overflow-y: auto;
  padding: 20px 25px;
  display: flex;
  flex-direction: column;
  gap: 16px;
}

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

.stop-badge {
  background: linear-gradient(135deg, #ff6b6b 0%, #ff4757 100%);
  color: white;
  padding: 4px 10px;
  border-radius: 12px;
  font-size: 0.8rem;
  font-weight: 700;
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

/* ==================== 响应式 ==================== */
@media (max-width: 768px) {
  .doctor-order-view {
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
