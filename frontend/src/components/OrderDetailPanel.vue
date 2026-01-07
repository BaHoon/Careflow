<template>
  <div class="order-detail-panel">
    <!-- 使用风琴式折叠面板 -->
    <el-collapse v-model="activeNames">
      <!-- 基础信息 -->
      <el-collapse-item name="basic" class="detail-collapse-item">
        <template #title>
          <div class="collapse-title">
            <span class="title-icon">📋</span>
            <span class="title-text">基础信息</span>
          </div>
        </template>
        
        <div class="info-grid">
          <div class="info-item">
            <span class="label">医嘱ID:</span>
            <span class="value">{{ detail.id }}</span>
          </div>
          <div class="info-item">
            <span class="label">医嘱类型:</span>
            <el-tag :type="getOrderTypeColor(detail.orderType)" size="small">
              {{ getOrderTypeName(detail.orderType) }}
            </el-tag>
          </div>
          <div class="info-item">
            <span class="label">状态:</span>
            <el-tag :type="getStatusColor(detail.status)" size="small">
              {{ getStatusText(detail.status) }}
            </el-tag>
          </div>
          <div class="info-item">
            <span class="label">医嘱类别:</span>
            <el-tag :type="detail.isLongTerm ? 'primary' : 'warning'" size="small">
              {{ detail.isLongTerm ? '长期医嘱' : '临时医嘱' }}
            </el-tag>
          </div>
          
          <div class="info-item full-width">
            <span class="label">患者信息:</span>
            <span class="value">{{ detail.patientName }} (ID: {{ detail.patientId }})</span>
          </div>
          
          <div class="info-item">
            <span class="label">开单医生:</span>
            <span class="value">{{ detail.doctorName }}</span>
          </div>
          <div class="info-item">
            <span class="label">负责护士:</span>
            <span class="value">{{ detail.nurseName || '未分配' }}</span>
          </div>
          
          <div class="info-item">
            <span class="label">创建时间:</span>
            <span class="value">{{ formatDateTime(detail.createTime) }}</span>
          </div>
          <div class="info-item">
            <span class="label">计划结束:</span>
            <span class="value">{{ formatDateTime(detail.plantEndTime) }}</span>
          </div>
          
          <div v-if="detail.signedAt" class="info-item">
            <span class="label">签收时间:</span>
            <span class="value">{{ formatDateTime(detail.signedAt) }}</span>
          </div>
          <div v-if="detail.signedByNurseName" class="info-item">
            <span class="label">签收护士:</span>
            <span class="value">{{ detail.signedByNurseName }}</span>
          </div>
          
          <div v-if="detail.stopReason" class="info-item full-width stop-info">
            <span class="label">停嘱原因:</span>
            <span class="value danger">{{ detail.stopReason }}</span>
          </div>
          <div v-if="detail.stopOrderTime" class="info-item">
            <span class="label">停嘱时间:</span>
            <span class="value">{{ formatDateTime(detail.stopOrderTime) }}</span>
          </div>
          <div v-if="detail.stopDoctorName" class="info-item">
            <span class="label">停嘱医生:</span>
            <span class="value">{{ detail.stopDoctorName }}</span>
          </div>
          
          <div v-if="detail.remarks" class="info-item full-width">
            <span class="label">备注:</span>
            <span class="value">{{ detail.remarks }}</span>
          </div>
        </div>
      </el-collapse-item>

      <!-- 药品医嘱详情 -->
      <el-collapse-item 
        v-if="detail.orderType === 'MedicationOrder'"
        name="medication" 
        class="detail-collapse-item"
      >
        <template #title>
          <div class="collapse-title">
            <span class="title-icon">💊</span>
            <span class="title-text">药品信息</span>
          </div>
        </template>
        
        <div class="medication-info">
          <div class="info-grid">
            <div class="info-item">
              <span class="label">用药途径:</span>
              <span class="value">{{ getUsageRouteName(detail.usageRoute) }}</span>
            </div>
            <div class="info-item">
              <span class="label">时间策略:</span>
              <span class="value">{{ getTimingStrategyName(detail.timingStrategy) }}</span>
            </div>
            <div v-if="detail.startTime" class="info-item">
              <span class="label">开始时间:</span>
              <span class="value">{{ formatDateTime(detail.startTime) }}</span>
            </div>
            <div v-if="detail.intervalHours" class="info-item">
              <span class="label">执行间隔:</span>
              <span class="value">每{{ detail.intervalHours }}小时</span>
            </div>
            <div v-if="detail.intervalDays" class="info-item">
              <span class="label">间隔天数:</span>
              <span class="value">{{ detail.intervalDays }}天</span>
            </div>
            <div v-if="detail.timingStrategy === 'SLOTS' && detail.smartSlotsMask" class="info-item full-width">
              <span class="label">执行时间点:</span>
              <span class="value">{{ getSlotNamesFromMask(detail.smartSlotsMask) }}</span>
            </div>
          </div>
          
          <div v-if="detail.medicationItems && detail.medicationItems.length > 0" class="drug-list">
            <div class="drug-list-header">药品列表</div>
            <div v-for="item in detail.medicationItems" :key="item.id" class="drug-item">
              <span class="drug-name">{{ item.drugName }}</span>
              <span class="drug-dosage">{{ item.dosage }}</span>
              <span v-if="item.note" class="drug-note">({{ item.note }})</span>
            </div>
          </div>
        </div>
      </el-collapse-item>

      <!-- 手术医嘱详情 -->
      <el-collapse-item 
        v-if="detail.orderType === 'SurgicalOrder'"
        name="surgical"
        class="detail-collapse-item"
      >
        <template #title>
          <div class="collapse-title">
            <span class="title-icon">🏥</span>
            <span class="title-text">手术信息</span>
          </div>
        </template>
        
        <div class="info-grid">
          <div class="info-item full-width">
            <span class="label">手术名称:</span>
            <span class="value highlight">{{ detail.surgeryName }}</span>
          </div>
          <div class="info-item">
            <span class="label">手术时间:</span>
            <span class="value">{{ formatDateTime(detail.scheduleTime) }}</span>
          </div>
          <div class="info-item">
            <span class="label">麻醉方式:</span>
            <span class="value">{{ detail.anesthesiaType }}</span>
          </div>
          <div class="info-item">
            <span class="label">切口部位:</span>
            <span class="value">{{ detail.incisionSite }}</span>
          </div>
          <div class="info-item">
            <span class="label">主刀医生:</span>
            <span class="value">{{ detail.surgeonName }}</span>
          </div>
          
          <div v-if="detail.requiredTalk && detail.requiredTalk.length > 0" class="info-item full-width">
            <span class="label">术前宣讲:</span>
            <div class="requirement-list">
              <div v-for="(item, index) in detail.requiredTalk" :key="index" class="requirement-item">
                • {{ item }}
              </div>
            </div>
          </div>
          
          <div v-if="detail.requiredOperation && detail.requiredOperation.length > 0" class="info-item full-width">
            <span class="label">术前操作:</span>
            <div class="requirement-list">
              <div v-for="(item, index) in detail.requiredOperation" :key="index" class="requirement-item">
                • {{ item }}
              </div>
            </div>
          </div>
          
          <div v-if="detail.surgicalItems && detail.surgicalItems.length > 0" class="info-item full-width">
            <span class="label">手术药品:</span>
            <div class="drug-list">
              <div v-for="item in detail.surgicalItems" :key="item.id" class="drug-item">
                <span class="drug-name">{{ item.drugName }}</span>
                <span class="drug-dosage">{{ item.dosage }}</span>
                <span v-if="item.note" class="drug-note">({{ item.note }})</span>
              </div>
            </div>
          </div>
        </div>
      </el-collapse-item>

      <!-- 检查医嘱详情 -->
      <el-collapse-item 
        v-if="detail.orderType === 'InspectionOrder'"
        name="inspection"
        class="detail-collapse-item"
      >
        <template #title>
          <div class="collapse-title">
            <span class="title-icon">🔬</span>
            <span class="title-text">检查信息</span>
          </div>
        </template>
        
        <div class="info-grid">
          <div class="info-item">
            <span class="label">检查项目:</span>
            <span class="value highlight">{{ detail.itemName }}</span>
          </div>
          <div class="info-item">
            <span class="label">项目代码:</span>
            <span class="value">{{ detail.itemCode }}</span>
          </div>
        </div>
      </el-collapse-item>

      <!-- 操作医嘱详情 -->
      <el-collapse-item 
        v-if="detail.orderType === 'OperationOrder'"
        name="operation"
        class="detail-collapse-item"
      >
        <template #title>
          <div class="collapse-title">
            <span class="title-icon">⚕️</span>
            <span class="title-text">操作信息</span>
          </div>
        </template>
        
        <div class="info-grid">
          <div class="info-item full-width">
            <span class="label">操作名称:</span>
            <span class="value highlight">{{ detail.operationName || detail.operationCode || '未知操作' }}</span>
          </div>
          <div v-if="detail.timingStrategy" class="info-item">
            <span class="label">时间策略:</span>
            <span class="value">{{ getTimingStrategyName(detail.timingStrategy) }}</span>
          </div>
          <div v-if="detail.startTime" class="info-item">
            <span class="label">开始时间:</span>
            <span class="value">{{ formatDateTime(detail.startTime) }}</span>
          </div>
          <div v-if="detail.plantEndTime" class="info-item">
            <span class="label">结束时间:</span>
            <span class="value">{{ formatDateTime(detail.plantEndTime) }}</span>
          </div>
          <div v-if="detail.intervalHours" class="info-item">
            <span class="label">执行间隔:</span>
            <span class="value">每{{ detail.intervalHours }}小时</span>
          </div>
          <div v-if="detail.intervalDays" class="info-item">
            <span class="label">间隔天数:</span>
            <span class="value">{{ detail.intervalDays }}天</span>
          </div>
          <div v-if="detail.timingStrategy === 'SLOTS' && detail.smartSlotsMask" class="info-item full-width">
            <span class="label">执行时间点:</span>
            <span class="value">{{ getSlotNamesFromMask(detail.smartSlotsMask) }}</span>
          </div>
        </div>
      </el-collapse-item>

      <!-- 关联任务列表 -->
      <el-collapse-item name="tasks" class="detail-collapse-item">
        <template #title>
          <div class="collapse-title">
            <span class="title-icon">📋</span>
            <span class="title-text">关联任务 ({{ filteredTasks.length }})</span>
            <el-button 
              v-if="!expandAllTasks" 
              text 
              @click.stop="toggleExpandAllTasks"
              size="small"
              class="expand-btn"
            >
              全部展开
            </el-button>
            <el-button 
              v-else 
              text 
              @click.stop="toggleExpandAllTasks"
              size="small"
              class="expand-btn"
            >
              全部收起
            </el-button>
          </div>
        </template>
        
        <el-collapse v-model="activeTaskIds" class="task-collapse">
          <el-collapse-item 
            v-for="(task, index) in filteredTasks" 
            :key="task.id"
            :name="task.id"
            class="task-collapse-item"
          >
            <template #title>
              <div class="task-header">
                <span class="task-number">{{ index + 1 }}</span>
                <el-tag :type="getTaskStatusColor(task.status)" size="small">
                  {{ getTaskStatusText(task.status) }}
                </el-tag>
                <el-tag 
                  size="small" 
                  :type="getTaskCategoryStyle(task.category).type"
                  :style="{ borderColor: getTaskCategoryStyle(task.category).color, color: getTaskCategoryStyle(task.category).color }"
                >
                  {{ getTaskCategoryStyle(task.category).name }}
                </el-tag>
                <!-- 显示任务标题（从DataPayload中解析的Title） -->
                <span class="task-title">{{ getTaskTitle(task) }}</span>
                <!-- <span v-if="getTaskTimingStatus(task).text" class="timing-status" :class="getTaskTimingStatus(task).class">
                  {{ getTaskTimingStatus(task).text }}
                </span> -->
                <span class="task-time-separator">|</span>
                <span class="task-time">计划: {{ formatDateTime(task.plannedStartTime) }}</span>
                <span v-if="task.statusBeforeLocking !== null" class="lock-indicator" title="此任务已被停嘱锁定">
                  🔒 锁前: {{ getTaskStatusText(task.statusBeforeLocking) }}
                </span>
              </div>
            </template>
            
            <div class="task-detail">
              <!-- 时间线 -->
              <div class="task-section">
                <div class="section-title">⏰ 时间线</div>
                <div class="timeline-item">
                  <span class="timeline-label">{{'计划时间' }}:</span>
                  <span class="timeline-value">{{ formatDateTime(task.plannedStartTime) }}</span>
                </div>
                <!-- 检查类任务不显示开始时间，只显示计划时间（预约时间） -->
                <div v-if="task.actualStartTime && !isInspectionTask(task)" class="timeline-item">
                  <span class="timeline-label">开始:</span>
                  <span class="timeline-value">{{ formatDateTime(task.actualStartTime) }}</span>
                  <span v-if="getDelayMinutes(task.plannedStartTime, task.actualStartTime) !== null" class="timeline-badge" :class="getDelayClass(getDelayMinutes(task.plannedStartTime, task.actualStartTime))">
                    [{{ formatDelayText(getDelayMinutes(task.plannedStartTime, task.actualStartTime)) }}]
                  </span>
                </div>
                <div v-if="task.actualEndTime" class="timeline-item">
                  <span class="timeline-label">结束:</span>
                  <span class="timeline-value">{{ formatDateTime(task.actualEndTime) }}</span>
                  <span v-if="getDurationMinutes(task.actualStartTime, task.actualEndTime) && !isInspectionTask(task)" class="timeline-badge duration">
                    [耗时{{ getDurationMinutes(task.actualStartTime, task.actualEndTime) }}分钟]
                  </span>
                </div>
              </div>

              <!-- 护士信息 -->
              <div class="task-section">
                <div class="section-title">👨‍⚕️ 护士信息</div>
                <div v-if="task.assignedNurseName" class="timeline-item">
                  <span class="timeline-label">计划执行护士:</span>
                  <span class="timeline-value">{{ task.assignedNurseName }}</span>
                </div>
                <div v-if="task.executorName" class="timeline-item">
                  <span class="timeline-label">实际开始执行护士:</span>
                  <span class="timeline-value">{{ task.executorName }}</span>
                </div>
                <div v-if="task.completerNurseName" class="timeline-item">
                  <span class="timeline-label">实际结束执行护士:</span>
                  <span class="timeline-value">{{ task.completerNurseName }}</span>
                </div>
                <div v-if="!task.assignedNurseName && !task.executorName && !task.completerNurseName" class="timeline-item">
                  <span class="timeline-label">护士信息:</span>
                  <span class="timeline-value" style="color: #909399;">暂无</span>
                </div>
              </div>

              <!-- 执行结果（仅对ResultPending类任务且有结果时显示） -->
              <div v-if="task.resultPayload && task.resultPayload.trim()" class="task-section">
                <div class="section-title">📊 执行结果</div>
                <div class="timeline-item">
                  <div class="result-content">{{ task.resultPayload }}</div>
                </div>
              </div>

              <!-- 执行备注（有备注时显示） -->
              <div v-if="task.executionRemarks && task.executionRemarks.trim()" class="task-section">
                <div class="section-title">📝 执行备注</div>
                <div class="timeline-item">
                  <div class="remarks-content">{{ task.executionRemarks }}</div>
                </div>
              </div>

              <!-- 护士模式：任务操作按钮 -->
              <div v-if="nurseMode" class="nurse-actions">
                <!-- Applying(0)：去申请 + 取消任务 -->
                <template v-if="task.status === 0 || task.status === 'Applying'">
                  <el-button 
                    type="primary" 
                    size="small"
                    @click.stop="handleGoToApplication(task)"
                  >
                    去申请
                  </el-button>
                  <el-button 
                    type="danger" 
                    plain
                    size="small"
                    @click.stop="handleCancelExecution(task)"
                  >
                    取消任务
                  </el-button>
                </template>

                <!-- Applied(1)：等待药房确认 + 去退药 -->
                <template v-if="task.status === 1 || task.status === 'Applied'">
                  <el-tag 
                    type="info"
                    size="default"
                  >
                    等待药房确认
                  </el-tag>
                  <el-button 
                    type="warning"
                    size="small"
                    @click.stop="handleGoToReturn(task)"
                  >
                    去退药
                  </el-button>
                </template>

                <!-- AppliedConfirmed(2) 或 Pending(3) -->
                <template v-if="task.status === 2 || task.status === 'AppliedConfirmed' || task.status === 3 || task.status === 'Pending'">
                  <!-- ApplicationWithPrint: 显示打印报告单按钮 -->
                  <template v-if="task.category === 6 || task.category === 'ApplicationWithPrint'">
                    <el-button 
                      type="success" 
                      size="small"
                      :icon="Printer"
                      @click.stop="emit('print-inspection-guide', { taskId: task.id, orderId: detail.id, task: task })"
                    >
                      打印导引单
                    </el-button>
                    <!-- 检查医嘱显示查看报告按钮 -->
                    <el-button 
                      v-if="detail.orderType === 'InspectionOrder'"
                      :type="hasInspectionReport() ? 'success' : 'info'"
                      size="small"
                      @click.stop="handleInspectionReport(task)"
                      :icon="Printer"
                      :disabled="!hasInspectionReport()"
                    >
                      {{ hasInspectionReport() ? '查看检查报告' : '报告未出' }}
                    </el-button>
                  </template>
                  <!-- 其他任务：显示完成任务按钮 -->
                  <template v-else>
                    <el-button 
                      type="primary" 
                      size="small"
                      @click.stop="handleStartCompletion(task)"
                    >
                      {{ getCompletionButtonLabel(task.category, false) }}
                    </el-button>
                  </template>
                  <!-- AppliedConfirmed(2)：取消任务按钮（带退药选项） -->
                  <el-button 
                    v-if="task.status === 2 || task.status === 'AppliedConfirmed'"
                    type="danger" 
                    plain
                    size="small"
                    @click.stop="() => { 
                      console.log('━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━');
                      console.log('🔴 [OrderDetailPanel按钮点击] AppliedConfirmed - 取消任务');
                      console.log('任务信息:', { 
                        id: task.id, 
                        status: task.status,
                        statusType: typeof task.status,
                        patientName: currentPatient?.name 
                      });
                      console.log('━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━');
                      handleCancelWithReturn(task); 
                    }"
                  >
                    取消任务
                  </el-button>
                  
                  <!-- Pending(3)：取消任务按钮（不带退药选项） -->
                  <el-button 
                    v-if="task.status === 3 || task.status === 'Pending'"
                    type="danger" 
                    plain
                    size="small"
                    @click.stop="() => { 
                      console.log('━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━');
                      console.log('🔴 [OrderDetailPanel按钮点击] Pending - 取消任务');
                      console.log('任务信息:', { 
                        id: task.id, 
                        status: task.status,
                        statusType: typeof task.status,
                        patientName: currentPatient?.name 
                      });
                      console.log('━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━');
                      handleCancelExecution(task); 
                    }"
                  >
                    取消任务
                  </el-button>
                </template>

                <!-- InProgress(4)：结束任务 -->
                <template v-if="task.status === 4 || task.status === 'InProgress'">
                  <el-button 
                    type="success" 
                    size="small"
                    @click.stop="handleFinishTask(task)"
                  >
                    {{ getCompletionButtonLabel(task.category, true) }}
                  </el-button>
                </template>

                <!-- Completed(5)：打印执行单（除了检查类医嘱） -->
                <template v-if="task.status === 5 || task.status === 'Completed'">
                  <el-button 
                    v-if="detail.orderType !== 'InspectionOrder'"
                    type="success"
                    size="small"
                    :icon="Printer"
                    @click.stop="handlePrintTaskBarcode(task)"
                  >
                    打印执行单
                  </el-button>
                </template>
                
                <!-- 所有非检查类医嘱的任务显示打印执行单按钮 -->
                <template v-if="detail.orderType !== 'InspectionOrder' && task.status !== 5 && task.status !== 'Completed'">
                  <el-button 
                    type="primary"
                    size="small"
                    :icon="Printer"
                    @click.stop="handlePrintTaskBarcode(task)"
                  >
                    打印执行单
                  </el-button>
                </template>

                <!-- 其他状态(OrderStopping, Stopped, Skipped, PendingReturn等)：无按钮 -->
              </div>
            </div>
          </el-collapse-item>
        </el-collapse>
        
        <div v-if="filteredTasks.length === 0" class="no-tasks">
          {{ props.filterDate ? `该日期（${props.filterDate}）无执行任务` : '暂无关联任务' }}
        </div>
      </el-collapse-item>
    </el-collapse>
  </div>
</template>

<script setup>
import { ref, computed, watch } from 'vue';
import { useRouter } from 'vue-router';
import { EditPen, Printer, Close, VideoPlay, Check } from '@element-plus/icons-vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { 
  completeExecutionTask, 
  cancelExecutionTask 
} from '@/api/nursing';

const router = useRouter();

// ==================== Props ====================
const props = defineProps({
  detail: {
    type: Object,
    required: true
  },
  // 护士模式：显示任务操作按钮
  nurseMode: {
    type: Boolean,
    default: false
  },
  // 日期过滤：只显示指定日期的任务（用于患者日志）
  filterDate: {
    type: String,
    default: null
  }
});

// ==================== Emits ====================
const emit = defineEmits([
  'update-task-execution',    // 修改任务执行情况
  'print-task-sheet',         // 打印任务执行单
  'print-inspection-guide',   // 打印检查导引单
  'view-inspection-report',   // 查看检查报告
  'task-updated',             // 任务已更新，需要刷新数据
  'view-task-detail'          // 查看任务详情
]);

// ==================== 风琴控制 ====================
// 主风琴面板控制（基础信息、药品信息等）
const activeNames = ref(['basic', 'tasks']); // 默认展开基础信息和任务列表

// 任务风琴控制
const activeTaskIds = ref([]);
const expandAllTasks = ref(false);

// ==================== 任务过滤（用于患者日志） ====================
// 过滤后的任务列表：如果指定了filterDate，只显示该日期的任务
const filteredTasks = computed(() => {
  if (!props.filterDate || !props.detail.tasks) {
    return props.detail.tasks || [];
  }
  
  // 过滤出指定日期的任务
  return props.detail.tasks.filter(task => {
    if (!task.actualStartTime) return false;
    
    const taskDate = new Date(task.actualStartTime).toISOString().split('T')[0];
    return taskDate === props.filterDate;
  });
});

// 全部展开/收起任务
const toggleExpandAllTasks = () => {
  if (expandAllTasks.value) {
    activeTaskIds.value = [];
    expandAllTasks.value = false;
  } else {
    activeTaskIds.value = filteredTasks.value.map(t => t.id);
    expandAllTasks.value = true;
  }
};

// 监听detail变化，重置展开状态
watch(() => props.detail, (newDetail) => {
  // 根据医嘱类型自动展开对应的信息面板
  activeNames.value = ['basic', 'tasks'];
  if (newDetail.orderType === 'MedicationOrder') {
    activeNames.value.push('medication');
  } else if (newDetail.orderType === 'SurgicalOrder') {
    activeNames.value.push('surgical');
  } else if (newDetail.orderType === 'InspectionOrder') {
    activeNames.value.push('inspection');
  } else if (newDetail.orderType === 'OperationOrder') {
    activeNames.value.push('operation');
  }
  
  activeTaskIds.value = [];
  expandAllTasks.value = false;
}, { immediate: true });

// ==================== 检查报告相关 ====================
// 判断是否为检查申请任务（检查医嘱且任务类型为ApplicationWithPrint）
const isInspectionApplicationTask = (task, index) => {
  // 检查医嘱的"检查申请"任务，category为6（ApplicationWithPrint）
  return props.detail.orderType === 'InspectionOrder' && task.category === 6;
};

// 判断是否为检查类任务（用于时间线显示）
const isInspectionTask = (task) => {
  return props.detail.orderType === 'InspectionOrder';
};

// 判断检查报告是否已经出来
const hasInspectionReport = () => {
  return props.detail.reportTime != null && props.detail.reportId != null;
};

// 处理查看检查报告
const handleInspectionReport = (task) => {
  if (hasInspectionReport()) {
    // 发送事件通知父组件打开报告
    emit('view-inspection-report', {
      orderId: props.detail.id,
      reportId: props.detail.reportId,
      reportUrl: props.detail.attachmentUrl  // 使用真实的 attachmentUrl
    });
  } else {
    // 报告还未出来，提示用户
    // 按钮已禁用，这里不会执行
  }
};

// ==================== ExecutionTask 按钮处理逻辑 ====================
// 获取当前护士ID
const getCurrentNurseId = () => {
  const userInfo = localStorage.getItem('userInfo');
  if (userInfo) {
    const user = JSON.parse(userInfo);
    return user.staffId;
  }
  return null;
};

// 获取完成按钮标签
const getCompletionButtonLabel = (category, isFinishing) => {
  if (category === 1 || category === 'Immediate') {
    return '完成任务';
  } else if (category === 2 || category === 'Duration') {
    return isFinishing ? '结束任务' : '完成任务';
  } else if (category === 3 || category === 'ResultPending') {
    return isFinishing ? '结束任务（需录入结果）' : '完成任务';
  } else if (category === 5 || category === 'Verification') {
    return '核对完成';
  }
  return isFinishing ? '结束任务' : '完成任务';
};

// 解析药品医嘱的DataPayload
const parseMedicationPayload = (payload) => {
  let html = `<div style="font-size: 13px; line-height: 1.8; color: #333;">`;
  
  if (payload.Title) {
    html += `<div style="margin-bottom: 12px;">`;
    html += `<h4 style="margin: 0 0 8px 0; color: #409eff; font-size: 14px; font-weight: 600;">📋 ${payload.Title}</h4>`;
    html += `</div>`;
  }
  
  if (payload.Description) {
    html += `<div style="margin-bottom: 12px; padding: 10px 14px; background: #f0f9ff; border-radius: 6px; box-shadow: 0 1px 4px rgba(64, 158, 255, 0.1);">`;
    html += `${payload.Description}`;
    html += `</div>`;
  }
  
  if (payload.MedicationInfo) {
    const med = payload.MedicationInfo;
    html += `<div style="margin-bottom: 12px; padding: 14px; background: #f5f7fa; border-radius: 6px; box-shadow: 0 1px 4px rgba(0, 0, 0, 0.06);">`;
    html += `<h4 style="margin: 0 0 10px 0; color: #409eff; font-size: 14px; font-weight: 600;">💊 药品信息</h4>`;
    
    const medDetails = [];
    if (med.DrugName) medDetails.push(`${med.DrugName}`);
    if (med.Specification) medDetails.push(`规格：${med.Specification}`);
    if (med.Dosage) medDetails.push(`剂量：${med.Dosage}`);
    if (med.Route) medDetails.push(`途径：${med.Route}`);
    if (med.Frequency) medDetails.push(`频次：${med.Frequency}`);
    
    html += `<div style="display: grid; gap: 6px;">`;
    medDetails.forEach(detail => {
      html += `<div style="padding: 4px 0; color: #606266;">• ${detail}</div>`;
    });
    html += `</div>`;
    html += `</div>`;
  }
  
  if (payload.IsChecklist && payload.Items && Array.isArray(payload.Items)) {
    html += `<div style="margin-bottom: 0; padding: 14px; background: #f5f7fa; border-radius: 6px; box-shadow: 0 1px 4px rgba(0, 0, 0, 0.06);">`;
    html += `<h4 style="margin: 0 0 10px 0; color: #67c23a; font-size: 14px; font-weight: 600;">✓ 核对项目</h4>`;
    html += `<div style="display: flex; flex-direction: column; gap: 8px;">`;
    
    payload.Items.forEach((item) => {
      if (item.text) {
        const status = item.isChecked 
          ? '<span style="color: #67c23a; font-weight: 600;">✓</span>' 
          : '<span style="color: #dcdfe6;">☐</span>';
        const required = item.required ? '<span style="color: #f56c6c; margin-left: 2px;">*必填</span>' : '';
        html += `<div style="display: flex; align-items: center; gap: 8px; padding: 4px 0; color: #606266;">
          ${status} <span>${item.text}</span> ${required}
        </div>`;
      }
    });
    
    html += `</div></div>`;
  }
  
  html += `</div>`;
  return html;
};

// 解析通用DataPayload
const parseTaskDataPayload = (dataPayload) => {
  if (!dataPayload) return '';
  
  try {
    const payload = JSON.parse(dataPayload);
    
    if (payload.TaskType === 'MEDICATION_ADMINISTRATION' || payload.taskType === 'RetrieveMedication') {
      return parseMedicationPayload(payload);
    }
    
    let html = `<div style="font-size: 13px; line-height: 1.8; color: #333;">`;
    
    const friendlyFields = {
      'Title': '标题',
      'title': '标题',
      'Description': '说明',
      'description': '说明',
      'Content': '内容',
      'content': '内容',
      'Remark': '备注',
      'remark': '备注',
      'Notes': '说明',
      'notes': '说明'
    };
    
    let hasContent = false;
    
    Object.entries(payload).forEach(([key, value]) => {
      const label = friendlyFields[key];
      if (!label) return;
      
      if (typeof value === 'object' && value !== null) {
        const objStr = JSON.stringify(value, null, 2);
        if (objStr.length < 100) {
          html += `<div style="margin-bottom: 8px; padding: 8px 12px; background: #f5f7fa; border-radius: 4px;">`;
          html += `<div style="font-weight: 600; color: #409eff; margin-bottom: 4px;">${label}</div>`;
          html += `<div style="white-space: pre-wrap; word-break: break-word;">${objStr}</div>`;
          html += `</div>`;
          hasContent = true;
        }
      } else if (value && value.toString().trim() !== '') {
        html += `<div style="margin-bottom: 8px; padding: 8px 12px; background: #f5f7fa; border-radius: 4px;">`;
        html += `<div style="font-weight: 600; color: #409eff; margin-bottom: 4px;">${label}</div>`;
        html += `<div style="color: #606266; word-break: break-word;">${value}</div>`;
        html += `</div>`;
        hasContent = true;
      }
    });
    
    if (!hasContent) {
      html += `<div style="padding: 8px 12px; background: #f5f7fa; border-radius: 4px; color: #606266;">`;
      html += `任务已准备就绪，请确认执行`;
      html += `</div>`;
    }
    
    html += `</div>`;
    return html;
  } catch {
    return `<div style="padding: 8px 12px; background: #f5f7fa; border-radius: 4px; color: #606266;">
      任务已准备就绪，请确认执行
    </div>`;
  }
};

// 跳转到医嘱申请界面
const handleGoToApplication = (task) => {
  router.push({
    path: '/nurse/application',
    query: {
      patientId: props.detail.patientId
    }
  });
};

// 跳转到医嘱申请界面（退药）
const handleGoToReturn = (task) => {
  router.push({
    path: '/nurse/application',
    query: {
      patientId: props.detail.patientId,
      returnMode: 'true'
    }
  });
};

// 开始完成（第一阶段）
const handleStartCompletion = async (task) => {
  try {
    const category = task.category;
    const taskDetails = parseTaskDataPayload(task.dataPayload);

    let message = `<div style="text-align: left; font-size: 13px; line-height: 1.8;">
      <div style="margin-bottom: 16px; padding: 16px; background: #f0f9ff; border-radius: 8px; box-shadow: 0 2px 8px rgba(64, 158, 255, 0.1);">
        <div style="display: grid; grid-template-columns: auto 1fr; gap: 8px 12px; align-items: center;">
          <span style="color: #909399;">👤 患者：</span>
          <span style="color: #303133; font-weight: 600;">${props.detail.patientName}</span>
          
          <span style="color: #909399;">📋 类型：</span>
          <span style="color: #303133; font-weight: 600;">${getOrderTypeName(props.detail.orderType)}</span>
          
          <span style="color: #909399;">📝 任务：</span>
          <span style="color: #303133; font-weight: 600;">${getTaskTitle(task)}</span>
          
          <span style="color: #909399;">🕑 计划时间：</span>
          <span style="color: #606266;">${formatDateTime(task.plannedStartTime)}</span>
          
          <span style="color: #909399;">📊 当前状态：</span>
          <span style="color: #606266;">${getTaskStatusText(task.status)}</span>
        </div>
      </div>`;
    
    if (taskDetails) {
      message += `<div style="margin-top: 12px; padding: 16px; background: #f5f7fa; border-radius: 8px; box-shadow: 0 2px 8px rgba(0, 0, 0, 0.05);">
        <div style="color: #409eff; font-weight: 600; margin-bottom: 10px; font-size: 14px;">📌 任务详情</div>
        <div>${taskDetails}</div>
      </div>`;
    }
    
    if (category === 1 || category === 'Immediate') {
      message += `<div style="margin-top: 12px; padding: 8px 12px; background: #fdf6ec; border-radius: 4px; color: #e6a23c; font-size: 12px;">
        ⚡ 此任务将直接标记为完成
      </div></div>`;
      
      // 询问是否需要输入备注
      const { value: remarkValue } = await ElMessageBox.prompt(
        message,
        '确认完成任务',
        {
          confirmButtonText: '确认完成',
          cancelButtonText: '取消',
          type: 'warning',
          inputType: 'textarea',
          inputPlaceholder: '请输入完成备注（可选）...',
          dangerouslyUseHTMLString: true,
          customClass: 'task-completion-dialog'
        }
      );

      const nurseId = getCurrentNurseId();
      if (!nurseId) {
        ElMessage.error('未找到护士信息');
        return;
      }

      // 备注格式
      let executionRemarks = null;
      if (remarkValue && remarkValue.trim()) {
        executionRemarks = remarkValue;
      }

      const response = await completeExecutionTask(task.id, nurseId, null, executionRemarks);
      ElMessage.success(response.message || '任务已完成');
      emit('task-updated', task.id);
      return;
    } else if (category === 5 || category === 'Verification') {
      message += `<div style="margin-top: 12px; padding: 8px 12px; background: #f0f9ff; border-radius: 4px; color: #409eff; font-size: 12px;">
        ✓ 核对完成后将更新任务状态
      </div></div>`;
      
      // 询问是否需要输入备注
      const { value: remarkValue } = await ElMessageBox.prompt(
        message,
        '确认核对完成',
        {
          confirmButtonText: '确认完成',
          cancelButtonText: '取消',
          type: 'warning',
          inputType: 'textarea',
          inputPlaceholder: '请输入核对备注（可选）...',
          dangerouslyUseHTMLString: true,
          customClass: 'task-completion-dialog'
        }
      );

      const nurseId = getCurrentNurseId();
      if (!nurseId) {
        ElMessage.error('未找到护士信息');
        return;
      }

      // 备注格式
      let executionRemarks = null;
      if (remarkValue && remarkValue.trim()) {
        executionRemarks = remarkValue;
      }

      const response = await completeExecutionTask(task.id, nurseId, null, executionRemarks);
      ElMessage.success(response.message || '任务已完成');
      emit('task-updated', task.id);
      return;
    } else if (category === 2 || category === 'Duration' || category === 3 || category === 'ResultPending') {
      message += `<div style="margin-top: 12px; padding: 8px 12px; background: #f0f9ff; border-radius: 4px; color: #409eff; font-size: 12px;">
        ▶ 任务开始执行，稍后需要完成或上传结果
      </div></div>`;
      
      // 询问是否需要输入备注（第一阶段）
      const { value: remarkValue } = await ElMessageBox.prompt(
        message,
        '确认开始执行',
        {
          confirmButtonText: '确认开始',
          cancelButtonText: '取消',
          type: 'info',
          inputType: 'textarea',
          inputPlaceholder: '请输入开始备注（可选）...',
          dangerouslyUseHTMLString: true,
          customClass: 'task-completion-dialog'
        }
      );
      
      const nurseId = getCurrentNurseId();
      if (!nurseId) {
        ElMessage.error('未找到护士信息');
        return;
      }

      // 第一阶段备注格式
      let executionRemarks = null;
      if (remarkValue && remarkValue.trim()) {
        executionRemarks = remarkValue;
      }

      const response = await completeExecutionTask(task.id, nurseId, null, executionRemarks);
      ElMessage.success(response.message || '任务已开始执行，请继续完成第二阶段');
      emit('task-updated', task.id);
      return;
    } else {
      ElMessage.warning(`任务类别 ${category} 的流程暂未实现`);
      return;
    }
  } catch (error) {
    if (error !== 'cancel') {
      console.error('开始完成任务失败:', error);
      ElMessage.error(error.response?.data?.message || '操作失败');
    }
  }
};

// 结束任务（第二阶段）
const handleFinishTask = async (task) => {
  try {
    const category = task.category;
    let resultPayload = null;
    let remarkValue = ''; // 用于存储备注信息
    const taskDetails = parseTaskDataPayload(task.dataPayload);

    let message = `<div style="text-align: left; font-size: 13px; line-height: 1.8;">
      <div style="margin-bottom: 16px; padding: 16px; background: #f0f9ff; border-radius: 8px; box-shadow: 0 2px 8px rgba(64, 158, 255, 0.1);">
        <div style="display: grid; grid-template-columns: auto 1fr; gap: 8px 12px; align-items: center;">
          <span style="color: #909399;">👤 患者：</span>
          <span style="color: #303133; font-weight: 600;">${props.detail.patientName}</span>
          
          <span style="color: #909399;">📋 类型：</span>
          <span style="color: #303133; font-weight: 600;">${getOrderTypeName(props.detail.orderType)}</span>
          
          <span style="color: #909399;">📝 任务：</span>
          <span style="color: #303133; font-weight: 600;">${getTaskTitle(task)}</span>
          
          <span style="color: #909399;">🕑 计划时间：</span>
          <span style="color: #606266;">${formatDateTime(task.plannedStartTime)}</span>`;
    
    if (task.actualStartTime) {
      message += `
          <span style="color: #909399;">▶️ 开始时间：</span>
          <span style="color: #67c23a; font-weight: 600;">${formatDateTime(task.actualStartTime)}</span>`;
      
      const startTime = new Date(task.actualStartTime.endsWith('Z') ? task.actualStartTime : task.actualStartTime + 'Z');
      const now = new Date();
      const durationMinutes = Math.floor((now - startTime) / (1000 * 60));
      if (durationMinutes >= 0) {
        message += `
          <span style="color: #909399;">⏱️ 执行时长：</span>
          <span style="color: #606266;">${durationMinutes} 分钟</span>`;
      }
    }
    
    message += `
          <span style="color: #909399;">📊 当前状态：</span>
          <span style="color: #409eff; font-weight: 600;">执行中</span>
        </div>
      </div>`;
    
    if (taskDetails) {
      message += `<div style="margin-bottom: 12px; padding: 16px; background: #f5f7fa; border-radius: 8px; box-shadow: 0 2px 8px rgba(0, 0, 0, 0.05);">
        <div style="color: #409eff; font-weight: 600; margin-bottom: 10px; font-size: 14px;">📌 任务详情</div>
        <div>${taskDetails}</div>
      </div>`;
    }

    if (category === 3 || category === 'ResultPending') {
      message += `<div style="margin-top: 12px; padding: 8px 12px; background: #fdf6ec; border-radius: 4px; color: #e6a23c; font-size: 12px;">
        📥 请分别输入执行结果和结束备注
      </div>
      <div style="margin-top: 16px;">
        <div style="margin-bottom: 12px;">
          <label style="display: block; margin-bottom: 6px; color: #606266; font-weight: 600;">
            <span style="color: #f56c6c;">*</span> 执行结果：
          </label>
          <textarea id="result-input" rows="3" placeholder="请输入执行结果（必填）..." 
            style="width: 100%; padding: 8px 12px; border: 1px solid #dcdfe6; border-radius: 4px; font-size: 13px; resize: vertical; font-family: inherit;"></textarea>
        </div>
        <div>
          <label style="display: block; margin-bottom: 6px; color: #606266; font-weight: 600;">
            结束备注：
          </label>
          <textarea id="remarks-input" rows="3" placeholder="请输入结束备注信息（可选）..." 
            style="width: 100%; padding: 8px 12px; border: 1px solid #dcdfe6; border-radius: 4px; font-size: 13px; resize: vertical; font-family: inherit;"></textarea>
        </div>
      </div></div>`;
      
      let resultValue = '';
      
      try {
        await ElMessageBox.confirm(
          message,
          '结束任务',
          {
            confirmButtonText: '确认完成',
            cancelButtonText: '取消',
            dangerouslyUseHTMLString: true,
            customClass: 'task-completion-dialog',
            beforeClose: (action, instance, done) => {
              if (action === 'confirm') {
                const resultInput = document.getElementById('result-input');
                const remarksInput = document.getElementById('remarks-input');
                
                if (resultInput) {
                  resultValue = resultInput.value?.trim() || '';
                }
                if (remarksInput) {
                  remarkValue = remarksInput.value?.trim() || '';
                }
                
                // 验证执行结果必填
                if (!resultValue) {
                  ElMessage.warning('执行结果不能为空');
                  return;
                }
                
                done();
              } else {
                done();
              }
            }
          }
        );
      } catch (error) {
        if (error === 'cancel') {
          return;
        }
        throw error;
      }
      
      resultPayload = resultValue;
      // remarkValue 将作为独立参数传递给 API
    } else if (category === 2 || category === 'Duration') {
      message += `<div style="margin-top: 12px; padding: 8px 12px; background: #f0f9ff; border-radius: 4px; color: #409eff; font-size: 12px;">
        📝 请在下方输入结束备注信息
      </div></div>`;
      
      const { value } = await ElMessageBox.prompt(
        message,
        '结束任务',
        {
          confirmButtonText: '确认完成',
          cancelButtonText: '取消',
          inputType: 'textarea',
          inputPlaceholder: '请输入结束备注信息（可选）...',
          dangerouslyUseHTMLString: true,
          customClass: 'task-completion-dialog'
        }
      );
      remarkValue = value || '';
    } else {
      ElMessage.warning(`任务类别 ${category} 的流程暂未实现`);
      return;
    }

    const nurseId = getCurrentNurseId();
    if (!nurseId) {
      ElMessage.error('未找到护士信息');
      return;
    }

    const response = await completeExecutionTask(task.id, nurseId, resultPayload, remarkValue);
    ElMessage.success(response.message || '任务已完成');
    emit('task-updated', task.id);
  } catch (error) {
    if (error !== 'cancel') {
      console.error('结束任务失败:', error);
      ElMessage.error(error.response?.data?.message || '操作失败');
    }
  }
};

// 取消执行任务（Applying/Applied/Pending状态）
const handleCancelExecution = async (task) => {
  console.log('=== OrderDetailPanel handleCancelExecution 开始 ===');
  console.log('任务信息:', { id: task.id, status: task.status, patientName: props.detail.patientName });
  
  try {
    console.log('📝 准备显示取消任务弹窗（不带退药选项）...');
    
    // 使用 ElMessageBox.prompt 获取取消理由
    const { value: cancelReason } = await ElMessageBox.prompt(
      '请填写取消任务的理由（该操作将被记录）',
      '确认取消任务',
      {
        confirmButtonText: '确认取消',
        cancelButtonText: '不取消',
        inputPlaceholder: '请输入取消理由...',
        inputType: 'textarea',
        inputValidator: (value) => {
          if (!value || !value.trim()) {
            return '取消理由不能为空';
          }
          return true;
        }
      }
    );

    console.log('✅ 用户确认取消，理由:', cancelReason);

    const nurseId = getCurrentNurseId();
    console.log('获取护士ID:', nurseId);
    if (!nurseId) {
      ElMessage.error('未找到护士信息');
      return;
    }

    console.log('=== 准备调用 cancelExecutionTask API ===');
    console.log('参数:', { taskId: task.id, nurseId, cancelReason, needReturn: false });
    const response = await cancelExecutionTask(task.id, nurseId, cancelReason);
    console.log('=== OrderDetailPanel API 响应 ===', response);
    ElMessage.success(response?.message || '任务已取消');
    emit('task-updated', task.id);
  } catch (error) {
    console.error('❌ OrderDetailPanel handleCancelExecution 捕获错误:', error);
    
    // ElMessageBox 取消操作会抛出 'cancel' 字符串或包含 action: 'cancel' 的对象
    if (error === 'cancel' || error?.action === 'cancel') {
      console.log('✋ 用户取消了操作');
      return;
    }
    
    console.error('取消执行任务失败 - 详细错误:', error);
    console.error('错误堆栈:', error?.stack);
    ElMessage.error(error?.response?.data?.message || error?.message || '取消任务失败');
  }
};

// 取消任务（AppliedConfirmed状态，带退药选项）
const handleCancelWithReturn = async (task) => {
  console.log('=== OrderDetailPanel handleCancelWithReturn 开始 ===');
  console.log('任务信息:', { id: task.id, status: task.status, patientName: props.detail.patientName });
  
  try {
    console.log('📝 准备显示取消任务弹窗（带退药选项）...');
    
    // 判断是否为检查类任务
    const isInspection = props.detail.orderType === 'InspectionOrder';
    console.log('任务类型:', isInspection ? '检查' : '药品');
    
    // 第一步：使用 prompt 获取取消理由
    const { value: cancelReason } = await ElMessageBox.prompt(
      '请填写取消任务的理由（该操作将被记录）',
      '确认取消任务',
      {
        confirmButtonText: '下一步',
        cancelButtonText: '取消',
        inputPlaceholder: '请输入取消理由...',
        inputType: 'textarea',
        inputValidator: (value) => {
          if (!value || !value.trim()) {
            return '取消理由不能为空';
          }
          return true;
        }
      }
    );

    console.log('✅ 用户输入取消理由:', cancelReason);

    // 第二步：根据任务类型询问是否需要退药或取消检查预约
    const confirmMessage = isInspection 
      ? '该任务已确认检查预约，是否要通知检查站取消安排检查？'
      : '该任务已确认药品，是否需要立即退药？';
    
    const confirmTitle = isInspection ? '检查取消确认' : '退药确认';
    const confirmButtonText = isInspection ? '通知检查站取消' : '需要退药';
    const cancelButtonText = isInspection ? '暂不通知' : '暂不退药';

    const { value: needReturnAction } = await ElMessageBox.confirm(
      confirmMessage,
      confirmTitle,
      {
        confirmButtonText: confirmButtonText,
        cancelButtonText: cancelButtonText,
        type: 'warning',
        distinguishCancelAndClose: true
      }
    ).then(() => ({ value: true }))
      .catch((action) => {
        if (action === 'cancel') {
          return { value: false };
        }
        throw action; // 用户点击了关闭按钮，抛出异常
      });

    console.log('✅ 用户选择:', needReturnAction ? (isInspection ? '通知检查站取消' : '需要退药') : (isInspection ? '暂不通知' : '暂不退药'));

    const nurseId = getCurrentNurseId();
    console.log('获取护士ID:', nurseId);
    if (!nurseId) {
      ElMessage.error('未找到护士信息');
      return;
    }

    console.log('=== 准备调用 cancelExecutionTask API (带退药/取消预约选项) ===');
    console.log('参数:', { taskId: task.id, nurseId, cancelReason, needReturn: needReturnAction });
    const response = await cancelExecutionTask(task.id, nurseId, cancelReason, needReturnAction);
    console.log('=== OrderDetailPanel API 响应 ===', response);
    ElMessage.success(response?.message || '任务已取消');
    emit('task-updated', task.id);
  } catch (error) {
    console.error('❌ OrderDetailPanel handleCancelWithReturn 捕获错误:', error);
    
    // ElMessageBox 取消操作会抛出 'cancel' 字符串或包含 action: 'cancel' 的对象
    if (error === 'cancel' || error?.action === 'cancel' || error === 'close') {
      console.log('✋ 用户取消了操作');
      return;
    }
    
    console.error('取消执行任务失败 - 详细错误:', error);
    console.error('错误堆栈:', error?.stack);
    ElMessage.error(error?.response?.data?.message || error?.message || '取消任务失败');
  }
};

// 查看任务详情
const handleViewTaskDetail = (task) => {
  emit('view-task-detail', task);
};


// 直接从检查信息区域查看报告
// ==================== DataPayload解析 ====================
/**
 * 解析任务的DataPayload JSON字符串，提取Title
 * @param {Object} task - 任务对象
 * @returns {string} 任务标题，如果解析失败则返回默认标题
 */
const getTaskTitle = (task) => {
  if (!task.dataPayload) {
    return getTaskCategoryStyle(task.category).name;
  }
  
  try {
    const payload = JSON.parse(task.dataPayload);
    // 优先使用Title字段，如果没有则使用TaskType或默认值
    return payload.Title || payload.title || payload.TaskType || getTaskCategoryStyle(task.category).name;
  } catch (error) {
    // JSON解析失败，返回默认标题
    console.warn('解析任务DataPayload失败:', error, 'Task ID:', task.id);
    return getTaskCategoryStyle(task.category).name;
  }
};

// ==================== 格式化方法 ====================
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

// ==================== 状态映射 ====================
const getStatusText = (status) => {
  const statusMap = {
    0: '草稿', 1: '未签收', 2: '已签收', 3: '进行中',
    4: '已完成', 5: '已停止', 6: '已取消', 7: '已退回', 
    8: '等待停嘱', 9: '停止中', 10: '异常态'
  };
  return statusMap[status] || `状态${status}`;
};

const getStatusColor = (status) => {
  const colorMap = {
    0: 'info', 1: 'warning', 2: 'primary', 3: 'success',
    4: 'success', 5: 'info', 6: 'info', 7: 'danger', 
    8: 'warning', 9: 'warning', 10: 'danger'
  };
  return colorMap[status] || 'info';
};

const getOrderTypeName = (orderType) => {
  const nameMap = {
    MedicationOrder: '药品医嘱',
    InspectionOrder: '检查医嘱',
    OperationOrder: '操作医嘱',
    SurgicalOrder: '手术医嘱',
    DischargeOrder: '出院医嘱'
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

// 修正后的用药途径映射 - 匹配后端UsageRoute枚举值
const getUsageRouteName = (route) => {
  if (route === null || route === undefined) return '未指定';
  
  const routeMap = {
    1: '口服 (PO)',
    2: '外用/涂抹 (Topical)',
    10: '肌内注射 (IM)',
    11: '皮下注射 (SC)',
    12: '静脉推注 (IVP)',
    20: '静脉滴注 (IVGTT)',
    30: '皮试 (ST)'
  };
  return routeMap[route] || `未知途径(${route})`;
};

// 时间策略名称映射
const getTimingStrategyName = (strategy) => {
  if (!strategy) return '未指定';
  
  const strategyMap = {
    'IMMEDIATE': '立即执行',
    'SPECIFIC': '指定时间',
    'CYCLIC': '周期执行',
    'SLOTS': '时段执行',
    'OnceDaily': '每日一次',
    'TwiceDaily': '每日两次',
    'ThreeTimesDaily': '每日三次',
    'FourTimesDaily': '每日四次',
    'EveryOtherDay': '隔日一次',
    'StatDose': '立即执行',
    'CustomSchedule': '自定义时间',
    'Hourly': '按小时'
  };
  return strategyMap[strategy] || strategy;
};

// 根据时间槽掩码获取中文时间点名称
const getSlotNamesFromMask = (mask) => {
  if (!mask) return '未指定';
  
  const slotMap = {
    1: '早餐前',
    2: '早餐后',
    4: '午餐前',
    8: '午餐后',
    16: '晚餐前',
    32: '晚餐后',
    64: '睡前'
  };
  
  const selectedSlots = [];
  for (let bit = 1; bit <= 64; bit *= 2) {
    if (mask & bit) {
      selectedSlots.push(slotMap[bit]);
    }
  }
  
  return selectedSlots.length > 0 ? selectedSlots.join('、') : '未指定';
};

const getTaskStatusText = (status) => {
  const statusMap = {
    0: '待申请', 
    1: '已申请', 
    2: '已确认', 
    3: '待执行',
    4: '进行中', 
    5: '已完成', 
    6: '停止锁定', 
    7: '已停止',
    8: '异常',
    9: '待退药',
    10: '异常取消待退药'
  };
  return statusMap[status] || `状态${status}`;
};

const getTaskStatusColor = (status) => {
  const colorMap = {
    0: 'info', 
    1: 'warning', 
    2: 'primary', 
    3: 'primary',
    4: 'success', 
    5: 'success', 
    6: 'warning', 
    7: 'info',
    8: 'danger',
    9: 'warning',
    10: 'danger'
  };
  return colorMap[status] || 'info';
};

// 获取任务类型样式和名称（使用正确的TaskCategory枚举：1-6）
const getTaskCategoryStyle = (category) => {
  const categoryMap = {
    1: { name: '操作', color: '#67c23a', type: 'success' },      // Immediate 即刻执行
    2: { name: '操作', color: '#409eff', type: 'primary' },      // Duration 持续执行
    3: { name: '操作', color: '#e6a23c', type: 'warning' },      // ResultPending 结果等待
    4: { name: '操作', color: '#9b59b6', type: 'info' },         // DataCollection 护理记录
    5: { name: '取药核对', color: '#909399', type: 'info' },      // Verification 核对类
    6: { name: '检查申请', color: '#17a2b8', type: 'info' }       // ApplicationWithPrint 申请打印
  };
  return categoryMap[category] || { name: '未知', color: '#909399', type: 'info' };
};

// 格式化只显示时间（HH:mm）
const formatTime = (dateString) => {
  if (!dateString) return '--:--';
  try {
    // 确保UTC时间字符串带有Z标识
    let utcString = dateString;
    if (!dateString.endsWith('Z') && !dateString.includes('+')) {
      utcString = dateString + 'Z';
    }
    const date = new Date(utcString);
    return date.toLocaleTimeString('zh-CN', { 
      hour: '2-digit', 
      minute: '2-digit',
      timeZone: 'Asia/Shanghai'
    });
  } catch {
    return '--:--';
  }
};

// 计算延迟分钟数
const getDelayMinutes = (plannedTime, actualTime) => {
  if (!plannedTime || !actualTime) return null;
  try {
    const planned = new Date(plannedTime);
    const actual = new Date(actualTime);
    return Math.round((actual - planned) / 60000);
  } catch {
    return null;
  }
};

// 计算持续时间（分钟）
const getDurationMinutes = (startTime, endTime) => {
  if (!startTime || !endTime) return null;
  try {
    const start = new Date(startTime);
    const end = new Date(endTime);
    return Math.round((end - start) / 60000);
  } catch {
    return null;
  }
};

// 格式化延迟文本
const formatDelayText = (minutes) => {
  if (minutes > 15) return `延迟${minutes}分钟`;
  if (minutes > 5) return `延迟${minutes}分钟`;
  if (minutes < -5) return `提前${-minutes}分钟`;
  return '按时';
};

// 获取延迟样式类
const getDelayClass = (minutes) => {
  if (minutes > 15) return 'delay-serious';
  if (minutes > 5) return 'delay-minor';
  if (minutes < -5) return 'early';
  return 'ontime';
};

// 获取任务时效状态（显示在标题栏）
const getTaskTimingStatus = (task) => {
  // 未完成且有异常
  if (task.status === 8 && task.exceptionReason) {
    return { text: '❌异常', class: 'status-exception' };
  }
  
  // 已完成，不显示提前/延后信息（这些信息在展开后的详情中显示）
  if (task.status === 5) {
    return { text: '', class: '' };
  }
  
  // 停嘱锁定
  if (task.status === 6) {
    return { text: '🔒锁定', class: 'status-locked' };
  }
  
  return { text: '', class: '' };
};

// 打印任务条形码
const handlePrintTaskBarcode = async (task) => {
  const taskId = task.id;
  if (!taskId) {
    ElMessage.error('任务ID无效');
    return;
  }

  try {
    // 先从API获取条形码数据
    const response = await fetch(`/api/BarcodePrint/generate-task-barcode?taskId=${taskId}`);
    const result = await response.json();
    
    if (!result.success || !result.data) {
      throw new Error(result.message || '获取条形码失败');
    }
    
    const barcodeData = result.data;
    
    // 获取任务类别名称的函数（与任务单据打印页面一致）
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
    
    // 打开新窗口显示条形码并打印
    const printWindow = window.open('', '_blank', 'width=800,height=600');
    
    if (!printWindow) {
      ElMessage.error('无法打开打印窗口，请检查浏览器弹窗拦截设置');
      return;
    }

    // 构建打印内容 - 使用任务单据打印格式
    printWindow.document.write(`
      <!DOCTYPE html>
      <html>
      <head>
        <title>打印条形码 - ${taskId}</title>
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
            body {
              padding: 0;
            }
            .barcode-item {
              page-break-inside: avoid;
            }
          }
        </style>
      </head>
      <body>
        <div class="barcode-item">
          <div class="barcode-image">
            <img src="${barcodeData.barcodeBase64}" alt="任务 ${taskId}" onload="window.print(); setTimeout(() => window.close(), 1000);" />
          </div>
          <div class="barcode-info">
            <div class="info-row">
              <span class="label">患者:</span>
              <span class="value">${barcodeData.patientName || props.detail.patientName || '-'} (${barcodeData.patientId || props.detail.patientId || '-'})</span>
            </div>
            <div class="info-row">
              <span class="label">任务:</span>
              <span class="value">${barcodeData.orderSummary}</span>
            </div>
            <div class="info-row">
              <span class="label">类型:</span>
              <span class="value">${getTaskCategoryName(barcodeData.taskCategory)}</span>
            </div>
            <div class="info-row">
              <span class="label">计划时间:</span>
              <span class="value">${formatDateTime(task.plannedStartTime)}</span>
            </div>
          </div>
        </div>
      </body>
      </html>
    `);
    
    printWindow.document.close();
  } catch (error) {
    console.error('打印条形码失败:', error);
    ElMessage.error('打印失败: ' + error.message);
  }
};
</script>

<style scoped>
.order-detail-panel {
  display: flex;
  flex-direction: column;
  /* 移除 max-height 和 overflow-y，让整个面板可滚动 */
}

/* 风琴面板样式 */
.detail-collapse-item {
  margin-bottom: 12px;
  border: 1px solid #e4e7ed;
  border-radius: 8px;
  overflow: hidden;
  background: #fff;
}

/* 为风琴面板标题添加内边距 */
.detail-collapse-item :deep(.el-collapse-item__header) {
  padding-left: 20px;
  padding-right: 20px;
}

/* 展开按钮（箭头）样式 - 确保显示 */
.detail-collapse-item :deep(.el-collapse-item__arrow) {
  display: inline-block !important;
  margin-right: 12px;
  margin-left: 0 !important;
  color: #409eff !important;
  font-size: 14px !important;
  font-weight: bold;
  order: -1;
}

/* 确保箭头在header内正确定位 */
.detail-collapse-item :deep(.el-collapse-item__header) {
  display: flex !important;
  align-items: center;
}

.collapse-title {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 1rem;
  font-weight: 600;
  color: #303133;
  flex: 1;
  padding-left: 0;
}

.title-icon {
  font-size: 1.2rem;
}

.title-text {
  flex: 1;
}

.expand-btn {
  margin-left: auto;
  margin-right: 48px;
}

.info-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 16px;
  padding: 16px 32px;
}

.info-item {
  display: flex;
  gap: 8px;
  font-size: 0.9rem;
  align-items: flex-start;
}

.info-item.full-width {
  grid-column: 1 / -1;
}

.label {
  color: #909399;
  font-weight: 500;
  min-width: 90px;
  flex-shrink: 0;
}

.value {
  color: #606266;
  font-weight: 600;
  flex: 1;
}

.value.highlight {
  color: #409eff;
  font-size: 1rem;
}

.value.danger {
  color: #f56c6c;
}

.stop-info {
  background: #fef0f0;
  padding: 12px;
  border-radius: 6px;
  border-left: 4px solid #f56c6c;
}

.medication-info {
  display: flex;
  flex-direction: column;
  gap: 16px;
  padding: 16px 32px;
}

.drug-list-header {
  font-weight: 600;
  color: #606266;
  margin-bottom: 8px;
  font-size: 0.95rem;
}

.drug-list {
  display: flex;
  flex-direction: column;
  gap: 8px;
  margin-top: 12px;
}

.drug-item {
  display: flex;
  gap: 12px;
  align-items: center;
  padding: 10px 14px;
  background: #f0f9ff;
  border-radius: 6px;
 
}

.drug-name {
  font-weight: 600;
  color: #409eff;
  font-size: 0.9rem;
}

.drug-dosage {
  font-weight: 600;
  color: #67c23a;
  font-size: 0.9rem;
}

.drug-note {
  color: #e6a23c;
  font-size: 0.85rem;
  font-style: italic;
}

.requirement-list {
  display: flex;
  flex-direction: column;
  gap: 6px;
  margin-top: 4px;
}

.requirement-item {
  color: #606266;
  font-size: 0.9rem;
  padding-left: 8px;
}

/* 任务风琴样式 */
.task-collapse {
  border: none;
  padding: 8px 32px;
}

.task-collapse-item {
  margin-bottom: 8px;
  border: 1px solid #e4e7ed;
  border-radius: 6px;
  overflow: hidden;
  background: #fafafa;
}

/* 任务子项的箭头样式 */
.task-collapse-item :deep(.el-collapse-item__arrow) {
  display: inline-block !important;
  margin-right: 8px;
  margin-left: 0 !important;
  color: #409eff !important;
  font-size: 12px !important;
  font-weight: bold;
  order: -1;
}

.task-header {
  display: flex;
  align-items: center;
  gap: 10px;
  flex: 1;
  padding-right: 20px;
}

.task-number {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 24px;
  height: 24px;
  background: #409eff;
  color: #fff;
  font-size: 0.8rem;
  font-weight: bold;
  border-radius: 50%;
  flex-shrink: 0;
}

.task-title {
  font-size: 0.9rem;
  font-weight: 600;
  color: #303133;
  margin-left: 8px;
  flex-shrink: 0;
  max-width: 300px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.task-executor {
  font-size: 0.85rem;
  color: #606266;
  font-weight: 500;
  margin-left: 8px;
  flex-shrink: 0;
}

.task-time-separator {
  color: #dcdfe6;
  margin: 0 8px;
  font-weight: normal;
}

.task-time {
  font-size: 0.85rem;
  color: #909399;
  font-weight: normal;
}

.timing-status {
  font-size: 0.8rem;
  padding: 2px 8px;
  border-radius: 3px;
  font-weight: 500;
  margin-left: 8px;
}

.timing-status.status-ontime {
  background: #f0f9ff;
  color: #67c23a;
}

.timing-status.status-early {
  background: #f0f9ff;
  color: #409eff;
}

.timing-status.status-delay-minor {
  background: #fdf6ec;
  color: #e6a23c;
}

.timing-status.status-delay-serious {
  background: #fef0f0;
  color: #f56c6c;
}

.timing-status.status-exception {
  background: #fef0f0;
  color: #f56c6c;
  font-weight: 600;
}

.timing-status.status-locked {
  background: #fef0f0;
  color: #f56c6c;
}

.lock-indicator {
  background: #fef0f0;
  color: #f56c6c;
  padding: 4px 8px;
  border-radius: 4px;
  font-size: 0.75rem;
  font-weight: 600;
  border: 1px solid #fbc4c4;
}

.task-detail {
  padding: 16px;
  background: #fff;
  border-top: 1px solid #e4e7ed;
}

.task-section {
  margin-bottom: 16px;
}

.task-section:last-child {
  margin-bottom: 0;
}

.section-title {
  font-size: 0.85rem;
  font-weight: 600;
  color: #606266;
  margin-bottom: 12px;
  padding-bottom: 6px;
  border-bottom: 1px solid #f0f0f0;
}

.timeline-item {
  display: flex;
  align-items: baseline;
  margin-bottom: 8px;
  font-size: 0.9rem;
  line-height: 1.8;
}

.timeline-item:last-child {
  margin-bottom: 0;
}

.timeline-label {
  color: #909399;
  font-weight: 500;
  min-width: 80px;
  flex-shrink: 0;
}

.timeline-value {
  color: #606266;
  font-weight: 500;
  margin-right: 8px;
}

.timeline-value.danger {
  color: #f56c6c;
  font-weight: 600;
}

.timeline-badge {
  font-size: 0.8rem;
  font-weight: 500;
  margin-left: 4px;
}

.timeline-badge.ontime {
  color: #67c23a;
}

.timeline-badge.early {
  color: #409eff;
}

.timeline-badge.delay-minor {
  color: #e6a23c;
}

.timeline-badge.delay-serious {
  color: #f56c6c;
}

.timeline-badge.duration {
  color: #909399;
}

.result-content,
.remarks-content {
  color: #303133;
  font-weight: 500;
  padding: 12px;
  background: #f5f7fa;
  border-radius: 4px;
  border-left: 3px solid #409eff;
  line-height: 1.6;
  white-space: pre-wrap;
  word-break: break-word;
}

.remarks-content {
  border-left-color: #67c23a;
}

.task-detail-row {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 10px;
  font-size: 0.9rem;
}

.task-detail-row:last-child {
  margin-bottom: 0;
}

.timing-badge {
  font-size: 0.75rem;
  padding: 2px 8px;
  border-radius: 3px;
  font-weight: 500;
  margin-left: 8px;
}

.timing-badge.ontime {
  background: #f0f9ff;
  color: #67c23a;
}

.timing-badge.early {
  background: #f0f9ff;
  color: #409eff;
}

.timing-badge.delay-minor {
  background: #fdf6ec;
  color: #e6a23c;
}

.timing-badge.delay-serious {
  background: #fef0f0;
  color: #f56c6c;
}

.timing-badge.duration {
  background: #f4f4f5;
  color: #909399;
}

.task-label {
  color: #909399;
  font-weight: 500;
  min-width: 90px;
  flex-shrink: 0;
}

.task-value {
  color: #606266;
  font-weight: 600;
  flex: 1;
}

.task-value.danger {
  color: #f56c6c;
}

.no-tasks {
  text-align: center;
  color: #c0c4cc;
  padding: 40px 16px;
  font-size: 0.9rem;
}

/* ==================== 护士操作按钮 ==================== */
.nurse-actions {
  display: flex;
  gap: 10px;
  justify-content: flex-end;
  margin-top: 16px;
  padding-top: 16px;
  border-top: 1px dashed #e4e7ed;
}

.nurse-actions .el-button {
  flex: 0 0 auto;
}
</style>
