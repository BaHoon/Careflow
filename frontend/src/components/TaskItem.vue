<template>
  <div 
    class="task-item" 
    :class="{ 
      'task-highlight': highlight,
      'task-overdue': isOverdue,
      'task-due-soon': isDueSoon,
      'task-completed': isCompleted
    }"
  >
    <div class="task-clickable-area" @click="handleClick">
    <div class="task-header">
      <div class="task-title">
        <el-icon :size="18" class="task-icon">
          <component :is="categoryIcon" />
        </el-icon>
        <!-- 任务类型标签 -->
        <el-tag 
          :type="task.taskSource === 'ExecutionTask' ? 'success' : 'info'" 
          size="small"
          class="task-type-tag"
        >
          {{ task.taskSource === 'ExecutionTask' ? '医嘱任务' : '护理任务' }}
        </el-tag>
        <!-- ExecutionTask 显示医嘱类型和任务标题 -->
        <span v-if="task.taskSource === 'ExecutionTask' && task.orderTypeName" class="task-order-type">
          {{ task.orderTypeName }}
        </span>
        <span class="task-category">{{ displayTitle }}</span>
      </div>
      <el-tag :type="statusTagType" size="small">{{ statusText }}</el-tag>
    </div>

    <div class="task-content">
      <div class="task-patient">
        <el-icon><User /></el-icon>
        <span>{{ task.patientName }}</span>
        <el-tag size="small" type="info">{{ task.bedId }}</el-tag>
      </div>

      <div class="task-time">
        <el-icon><Clock /></el-icon>
        <span>计划时间：{{ formatTime(task.plannedStartTime) }}</span>
        <!-- 只在未完成的超时任务和临期任务显示延迟信息，已完成任务不显示 -->
        <span v-if="task.excessDelayMinutes > 0 && task.status !== 'Completed' && task.status !== 5" class="overdue-text">
          (超出容忍期 {{ task.excessDelayMinutes }}分钟)
        </span>
        <span v-else-if="task.delayMinutes > 0 && task.delayMinutes >= -60 && task.status === 'Pending'" class="delay-text">
          (延迟 {{ task.delayMinutes }}分钟，容忍期内)
        </span>
        <span v-else-if="task.delayMinutes < 0 && task.delayMinutes >= -60 && task.status === 'Pending'" class="due-soon-text">
          (还有 {{ Math.abs(task.delayMinutes) }}分钟)
        </span>
      </div>

      <div v-if="task.actualStartTime" class="task-time">
        <el-icon><Check /></el-icon>
        <span>开始时间：{{ formatTime(task.actualStartTime) }}</span>
      </div>

      <div v-if="task.actualEndTime" class="task-time">
        <el-icon><CircleCheck /></el-icon>
        <span>完成时间：{{ formatTime(task.actualEndTime) }}</span>
      </div>
    </div>

    <div class="task-actions">
      <!-- ExecutionTask 的按钮逻辑 -->
      <template v-if="task.taskSource === 'ExecutionTask'">
        <!-- 
          业务流程：
          - 药房申请流程：Applying(0) → Applied(1) → AppliedConfirmed(2)
          - 执行流程：Pending(3) → InProgress(4) → Completed(5)
        -->
        
        <!-- Applying(0)：去申请 + 取消任务 -->
        <el-button 
          v-if="task.status === 2 || task.status === 'AppliedConfirmed' || task.status === 3 || task.status === 'Pending'" 
          type="primary" 
          size="small"
          @click.stop="handleGoToApplication"
        >
          去申请
        </el-button>
        <el-button 
          v-if="task.status === 0 || task.status === 'Applying'" 
          type="danger" 
          plain
          size="small"
          :icon="Close"
          @click.stop="handleCancelExecution"
        >
          取消任务
        </el-button>

        <!-- Applied(1)：等待药房确认 + 去退药 -->
        <el-tag 
          v-if="task.status === 1 || task.status === 'Applied'" 
          type="info"
          size="default"
        >
          等待药房确认
        </el-tag>

        <el-button 
          v-if="task.status === 1 || task.status === 'Applied'" 
          type="warning"
          size="small"
          @click.stop="handleGoToReturn"
        >
          去退药
        </el-button>

        <!-- ApplicationWithPrint: 显示打印报告单按钮 -->
        <el-button 
          v-if="task.category === 'ApplicationWithPrint' && (task.status === 2 || task.status === 'AppliedConfirmed' || task.status === 3 || task.status === 'Pending')" 
          type="success" 
          size="small"
          :icon="Printer"
          @click.stop="handlePrintReport"
        >
          打印报告单
        </el-button>
        <!-- AppliedConfirmed(2) 或 Pending(3)：显示根据category定制的"完成"按钮 -->
        <el-button 
          v-if="task.category !== 'ApplicationWithPrint' && (task.status === 2 || task.status === 'AppliedConfirmed' || task.status === 3 || task.status === 'Pending')" 
          type="primary" 
          size="small"
          :icon="VideoPlay"
          @click.stop="handleStartCompletion"
        >
          {{ getCompletionButtonLabel(task.category, false) }}
        </el-button>


        <!-- AppliedConfirmed(2) 或 Pending(3)：核对完成 + 取消任务 -->
        <el-button 
          v-if="task.status === 2 || task.status === 'AppliedConfirmed' || task.status === 3 || task.status === 'Pending'" 
          type="primary" 
          size="small"
          :icon="VideoPlay"
          @click.stop="handleStartCompletion"
        >
          {{ getCompletionButtonLabel(task.category, false) }}
        </el-button>
        <el-button 
          v-if="task.status === 2 || task.status === 'AppliedConfirmed' || task.status === 3 || task.status === 'Pending'" 
          type="danger" 
          plain
          size="small"
          :icon="Close"
          @click.stop="handleCancelWithReturn"
        >
          取消任务
        </el-button>

        <!-- InProgress(4)：结束任务 -->
        <el-button 
          v-if="task.status === 4 || task.status === 'InProgress'" 
          type="success" 
          size="small"
          :icon="Check"
          @click.stop="handleFinishTask"
        >
          {{ getCompletionButtonLabel(task.category, true) }}
        </el-button>

        <!-- Completed(5)：显示"查看详情" -->
        <el-button 
          v-if="task.status === 5 || task.status === 'Completed'" 
          size="small"
          @click.stop="handleViewDetail"
        >
          查看详情
        </el-button>
      </template>

      <!-- NursingTask 的按钮逻辑（原有逻辑） -->
      <template v-else-if="task.taskSource === 'NursingTask'">
        <!-- 未完成且未取消的任务显示开始录入按钮 -->
        <el-button 
          v-if="task.status !== 'Completed' && task.status !== 5 && task.status !== 'Cancelled' && task.status !== 9" 
          type="primary" 
          size="small"
          :icon="Edit"
          @click.stop="handleStartInput"
        >
          开始录入
        </el-button>
        <!-- 未完成且未取消的任务显示取消按钮 -->
        <el-button 
          v-if="task.status === 'Pending' || task.status === 3" 
          type="danger" 
          plain
          size="small"
          :icon="Close"
          @click.stop="handleCancelTask"
        >
          取消任务
        </el-button>
        <!-- 已完成的任务显示查看详情按钮 -->
        <el-button 
          v-if="task.status === 'Completed' || task.status === 5" 
          size="small"
          @click.stop="handleViewDetail"
        >
          查看详情
        </el-button>
      </template>
    </div>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue';
import { useRouter } from 'vue-router';
import {
  User,
  Clock,
  Check,
  CircleCheck,
  Coffee,
  Document,
  VideoCamera,
  Bell,
  Edit,
  Close,
  VideoPlay,
  Printer
} from '@element-plus/icons-vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { 
  cancelNursingTask, 
  completeExecutionTask, 
  cancelExecutionTask 
} from '@/api/nursing';

const router = useRouter();

const props = defineProps({
  task: {
    type: Object,
    required: true
  },
  highlight: {
    type: Boolean,
    default: false
  },
  isOverdue: {
    type: Boolean,
    default: false
  },
  isDueSoon: {
    type: Boolean,
    default: false
  }
});

const emit = defineEmits(['click', 'start-input', 'view-detail', 'task-cancelled', 'print-inspection-guide']);

// 已完成任务判断
const isCompleted = computed(() => {
  return props.task.status === 'Completed' || props.task.status === 5;
});

// 显示标题（优先使用 taskTitle，否则使用类别文本）
const displayTitle = computed(() => {
  if (props.task.taskSource === 'ExecutionTask' && props.task.taskTitle) {
    return props.task.taskTitle;
  }
  return categoryText.value;
});

// 任务类别图标
const categoryIcon = computed(() => {
  const iconMap = {
    // ExecutionTask 类别
    'Immediate': Coffee,
    'Duration': Coffee,
    'ResultPending': Document,
    'DataCollection': Bell,
    'Verification': Check,
    'ApplicationWithPrint': Document,
    // NursingTask 类别
    'Routine': Bell,
    'ReMeasure': VideoCamera
  };
  return iconMap[props.task.category] || Document;
});

// 任务类别文本
const categoryText = computed(() => {
  const textMap = {
    // ExecutionTask 类别
    'Immediate': '即刻执行',
    'Duration': '持续任务',
    'ResultPending': '结果待定',
    'DataCollection': '数据采集',
    'Verification': '核对验证',
    'ApplicationWithPrint': '申请打印',
    // NursingTask 类别
    'Routine': '常规护理',
    'ReMeasure': '复测任务'
  };
  return textMap[props.task.category] || props.task.category;
});

// 操作名称（优先使用dataPayload中的OperationName或Title，否则使用opId）
const operationName = computed(() => {
  if (props.task.dataPayload) {
    try {
      const payload = typeof props.task.dataPayload === 'string' 
        ? JSON.parse(props.task.dataPayload) 
        : props.task.dataPayload;
      return payload.OperationName || payload.Title || props.task.opId || '操作任务';
    } catch (e) {
      console.error('解析dataPayload失败:', e);
    }
  }
  return props.task.opId || '操作任务';
});

// 任务类别标签类型
const getCategoryTagType = (category) => {
  const typeMap = {
    'Immediate': 'success',
    'Duration': 'primary',
    'ResultPending': 'warning',
    'DataCollection': 'info',
    'Verification': ''
  };
  return typeMap[category] || '';
};

// 状态标签类型
const statusTagType = computed(() => {
  const status = props.task.status;
  const typeMap = {
    'Applying': 'info',
    0: 'info',
    'Applied': 'info',
    1: 'info',
    'AppliedConfirmed': 'warning',
    2: 'warning',
    'Pending': 'warning',
    3: 'warning',
    'InProgress': 'primary',
    'Running': 'primary',
    4: 'primary',
    'Completed': 'success',
    5: 'success',
    'OrderStopping': 'danger',
    6: 'danger',
    'Stopped': 'danger',
    7: 'danger',
    'Incomplete': 'info',
    'Skipped': 'info',
    8: 'info',
    'Cancelled': 'danger',
    9: 'danger',
    'PendingReturn': 'danger',
    'PendingReturnCancelled': 'danger',
    10: 'danger'
  };
  return typeMap[status] || 'info';
});

// 状态文本
const statusText = computed(() => {
  const status = props.task.status;
  const textMap = {
    'Applying': '待申请',
    0: '待申请',
    'Applied': '已申请',
    1: '已申请',
    'AppliedConfirmed': '已就绪',
    2: '已就绪',
    'Pending': '待执行',
    3: '待执行',
    'InProgress': '执行中',
    'Running': '执行中',
    4: '执行中',
    'Completed': '已完成',
    5: '已完成',
    'OrderStopping': '停止中',
    6: '停止中',
    'Stopped': '已停止',
    7: '已停止',
    'Incomplete': '异常',
    'Skipped': '已跳过',
    8: '异常',
    'Cancelled': '已取消',
    9: '已取消',
    'PendingReturn': '待退药',
    'PendingReturnCancelled': '异常取消待退药',
    10: '异常取消待退药'
  };
  return textMap[status] || status;
});

// 格式化时间
const formatTime = (dateString) => {
  if (!dateString) return '';
  try {
    // 确保UTC时间字符串带有Z标识
    let utcString = dateString;
    if (!dateString.endsWith('Z') && !dateString.includes('+')) {
      utcString = dateString + 'Z';
    }
    const date = new Date(utcString);
    return date.toLocaleString('zh-CN', {
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

const handleClick = () => {
  console.log('TaskItem handleClick 触发');
  emit('click', props.task);
  // 当点击任务块时，自动打开详情
  emit('view-detail', props.task);
};

const handleStartInput = () => {
  emit('start-input', props.task);
};

const handleViewDetail = () => {
  emit('view-detail', props.task);
};

// 获取当前护士ID
const getCurrentNurseId = () => {
  const userInfo = localStorage.getItem('userInfo');
  if (userInfo) {
    const user = JSON.parse(userInfo);
    return user.staffId;
  }
  return null;
};

// 取消任务
const handleCancelTask = async () => {
  try {
    // 弹出输入框要求填写取消理由
    const { value: cancelReason } = await ElMessageBox.prompt(
      '请填写取消任务的理由',
      '确认取消',
      {
        confirmButtonText: '确定',
        cancelButtonText: '取消',
        inputType: 'textarea',
        inputPlaceholder: '请输入取消理由...',
        inputValidator: (value) => {
          if (!value || value.trim().length === 0) {
            return '取消理由不能为空';
          }
          return true;
        },
      }
    );

    const nurseId = getCurrentNurseId();
    if (!nurseId) {
      ElMessage.error('未找到护士信息');
      return;
    }

    // 验证taskId
    const taskId = props.task.id;
    console.log('取消任务 - taskId:', taskId, 'task对象:', props.task, '理由:', cancelReason);
    
    if (!taskId) {
      ElMessage.error('任务ID无效');
      return;
    }

    // 调用API取消任务
    await cancelNursingTask(taskId, nurseId, cancelReason);
    ElMessage.success('任务已取消');
    
    // 通知父组件刷新数据
    emit('task-cancelled', taskId);
  } catch (error) {
    if (error !== 'cancel') {
      console.error('取消任务失败:', error);
      ElMessage.error(error.response?.data?.message || '取消任务失败');
    }
  }
};

// ==================== ExecutionTask 事件处理 ====================

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
  
  // 解析药品信息
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
  
  // 解析核对项
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

// 解析通用DataPayload - 简化版，隐藏技术细节
const parseDataPayload = (dataPayload) => {
  if (!dataPayload) return '';
  
  try {
    const payload = JSON.parse(dataPayload);
    
    // 如果是药品医嘱，使用专门的解析函数
    if (payload.TaskType === 'MEDICATION_ADMINISTRATION' || payload.taskType === 'RetrieveMedication') {
      return parseMedicationPayload(payload);
    }
    
    // 其他类型：仅显示人类可读的信息，不显示技术字段
    let html = `<div style="font-size: 13px; line-height: 1.8; color: #333;">`;
    
    // 只显示用户友好的字段
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
      // 检查是否是显示友好的字段
      const label = friendlyFields[key];
      if (!label) return; // 跳过技术字段
      
      if (typeof value === 'object' && value !== null) {
        // 对象类型，尝试提取有用信息
        const objStr = JSON.stringify(value, null, 2);
        if (objStr.length < 100) {
          html += `<div style="margin-bottom: 8px; padding: 8px 12px; background: #f5f7fa; border-radius: 4px;">`;
          html += `<div style="font-weight: 600; color: #409eff; margin-bottom: 4px;">${label}</div>`;
          html += `<div style="white-space: pre-wrap; word-break: break-word;">${objStr}</div>`;
          html += `</div>`;
          hasContent = true;
        }
      } else if (value && value.toString().trim() !== '') {
        // 字符串类型，只显示非空值
        html += `<div style="margin-bottom: 8px; padding: 8px 12px; background: #f5f7fa; border-radius: 4px;">`;
        html += `<div style="font-weight: 600; color: #409eff; margin-bottom: 4px;">${label}</div>`;
        html += `<div style="color: #606266; word-break: break-word;">${value}</div>`;
        html += `</div>`;
        hasContent = true;
      }
    });
    
    // 如果没有友好字段，显示简单的提示
    if (!hasContent) {
      html += `<div style="padding: 8px 12px; background: #f5f7fa; border-radius: 4px; color: #606266;">`;
      html += `任务已准备就绪，请确认执行`;
      html += `</div>`;
    }
    
    html += `</div>`;
    return html;
  } catch {
    // 如果JSON解析失败，返回友好的提示
    return `<div style="padding: 8px 12px; background: #f5f7fa; border-radius: 4px; color: #606266;">
      任务已准备就绪，请确认执行
    </div>`;
  }
};

// 打印报告单处理函数
const handlePrintReport = () => {
  // 发射事件给父组件，由父组件打开打印对话框
  emit('print-inspection-guide', { 
    taskId: props.task.id, 
    orderId: props.task.medicalOrderId,
    task: props.task
  });
};

// 获取完成按钮标签
const getCompletionButtonLabel = (category, isFinishing) => {
  if (category === 'Immediate') {
    return '完成任务';
  } else if (category === 'Duration') {
    return isFinishing ? '结束任务' : '完成任务';
  } else if (category === 'ResultPending') {
    return isFinishing ? '结束任务（需录入结果）' : '完成任务';
  } else if (category === 'Verification') {
    return '核对完成';
  }
  return isFinishing ? '结束任务' : '完成任务';
};

// 开始完成（第一阶段：Pending → Completed or InProgress）
const handleStartCompletion = async () => {
  try {
    const category = props.task.category;
    
    // 解析任务详情
    const taskDetails = parseDataPayload(props.task.dataPayload);

    // 构建确认消息 - 美化版本
    let message = `<div style="text-align: left; font-size: 13px; line-height: 1.8;">
      <div style="margin-bottom: 16px; padding: 16px; background: #f0f9ff; border-radius: 8px; box-shadow: 0 2px 8px rgba(64, 158, 255, 0.1);">
        <div style="display: grid; grid-template-columns: auto 1fr; gap: 8px 12px; align-items: center;">
          <span style="color: #909399;">👤 患者：</span>
          <span style="color: #303133; font-weight: 600;">${props.task.patientName} <span style="color: #909399; font-weight: 400;">(🛏️ ${props.task.bedId})</span></span>
          
          <span style="color: #909399;">📋 类型：</span>
          <span style="color: #303133; font-weight: 600;">${props.task.orderTypeName || '执行任务'}</span>
          
          <span style="color: #909399;">📝 任务：</span>
          <span style="color: #303133; font-weight: 600;">${props.task.taskTitle || categoryText.value}</span>
          
          <span style="color: #909399;">🕑 计划时间：</span>
          <span style="color: #606266;">${formatTime(props.task.plannedStartTime)}</span>`;
    
    // 添加延迟信息
    if (props.task.excessDelayMinutes > 0) {
      message += `
          <span style="color: #909399;">⚠️ 延迟状态：</span>
          <span style="color: #f56c6c; font-weight: 600;">已超出容忍期 ${props.task.excessDelayMinutes} 分钟</span>`;
    } else if (props.task.delayMinutes > 0) {
      message += `
          <span style="color: #909399;">⚠️ 延迟状态：</span>
          <span style="color: #e6a23c;">延迟 ${props.task.delayMinutes} 分钟（容忍期内）</span>`;
    } else if (props.task.delayMinutes < 0) {
      message += `
          <span style="color: #909399;">⏰ 剩余时间：</span>
          <span style="color: #67c23a;">还有 ${Math.abs(props.task.delayMinutes)} 分钟</span>`;
    }
    
    // 添加任务状态
    message += `
          <span style="color: #909399;">📊 当前状态：</span>
          <span style="color: #606266;">${statusText.value}</span>
        </div>
      </div>`;
    
    if (taskDetails) {
      message += `<div style="margin-top: 12px; padding: 16px; background: #f5f7fa; border-radius: 8px; box-shadow: 0 2px 8px rgba(0, 0, 0, 0.05);">
        <div style="color: #409eff; font-weight: 600; margin-bottom: 10px; font-size: 14px;">📌 任务详情</div>
        <div>${taskDetails}</div>
      </div>`;
    }
    
    // Immediate 类别：直接完成
    if (category === 'Immediate') {
      message += `<div style="margin-top: 12px; padding: 8px 12px; background: #fdf6ec; border-radius: 4px; color: #e6a23c; font-size: 12px;">
        ⚡ 此任务将直接标记为完成
      </div></div>`;
      
      await ElMessageBox.confirm(
        message,
        '确认完成任务',
        {
          confirmButtonText: '确认完成',
          cancelButtonText: '取消',
          type: 'warning',
          dangerouslyUseHTMLString: true,
          customClass: 'task-completion-dialog'
        }
      );
    } 
    // Verification 类别：直接完成（核对类）
    else if (category === 'Verification') {
      message += `<div style="margin-top: 12px; padding: 8px 12px; background: #f0f9ff; border-radius: 4px; color: #409eff; font-size: 12px;">
        ✓ 核对完成后将更新任务状态
      </div></div>`;
      
      await ElMessageBox.confirm(
        message,
        '确认核对完成',
        {
          confirmButtonText: '确认完成',
          cancelButtonText: '取消',
          type: 'warning',
          dangerouslyUseHTMLString: true,
          customClass: 'task-completion-dialog'
        }
      );
    }
    // Duration 和 ResultPending 类别：开始执行
    else if (category === 'Duration' || category === 'ResultPending') {
      message += `<div style="margin-top: 12px; padding: 8px 12px; background: #f0f9ff; border-radius: 4px; color: #409eff; font-size: 12px;">
        ▶ 任务开始执行，稍后需要完成或上传结果
      </div></div>`;
      
      await ElMessageBox.confirm(
        message,
        '确认开始执行',
        {
          confirmButtonText: '确认开始',
          cancelButtonText: '取消',
          type: 'info',
          dangerouslyUseHTMLString: true,
          customClass: 'task-completion-dialog'
        }
      );
    } else {
      // TODO: 其他类别的处理
      ElMessage.warning(`任务类别 ${category} 的流程暂未实现`);
      return;
    }

    const nurseId = getCurrentNurseId();
    if (!nurseId) {
      ElMessage.error('未找到护士信息');
      return;
    }

    const taskId = props.task.id;
    if (!taskId) {
      ElMessage.error('任务ID无效');
      return;
    }

    // 调用API完成第一阶段（Immediate直接到Completed，Duration/ResultPending到InProgress）
    const response = await completeExecutionTask(taskId, nurseId, null);
    ElMessage.success(response.message || '任务已更新');
    
    // 通知父组件刷新数据
    emit('task-cancelled', taskId);
  } catch (error) {
    if (error !== 'cancel') {
      console.error('开始完成任务失败:', error);
      ElMessage.error(error.response?.data?.message || '操作失败');
    }
  }
};

// 结束任务（第二阶段：InProgress → Completed，可能需要录入结果）
const handleFinishTask = async () => {
  try {
    const category = props.task.category;
    let resultPayload = null;

    // 解析任务详情
    const taskDetails = parseDataPayload(props.task.dataPayload);

    // 构建基础消息 - 美化版本
    let message = `<div style="text-align: left; font-size: 13px; line-height: 1.8;">
      <div style="margin-bottom: 16px; padding: 16px; background: #f0f9ff; border-radius: 8px; box-shadow: 0 2px 8px rgba(64, 158, 255, 0.1);">
        <div style="display: grid; grid-template-columns: auto 1fr; gap: 8px 12px; align-items: center;">
          <span style="color: #909399;">👤 患者：</span>
          <span style="color: #303133; font-weight: 600;">${props.task.patientName} <span style="color: #909399; font-weight: 400;">(🛏️ ${props.task.bedId})</span></span>
          
          <span style="color: #909399;">📋 类型：</span>
          <span style="color: #303133; font-weight: 600;">${props.task.orderTypeName || '执行任务'}</span>
          
          <span style="color: #909399;">📝 任务：</span>
          <span style="color: #303133; font-weight: 600;">${props.task.taskTitle || categoryText.value}</span>
          
          <span style="color: #909399;">🕑 计划时间：</span>
          <span style="color: #606266;">${formatTime(props.task.plannedStartTime)}</span>`;
    
    // 添加实际开始时间
    if (props.task.actualStartTime) {
      message += `
          <span style="color: #909399;">▶️ 开始时间：</span>
          <span style="color: #67c23a; font-weight: 600;">${formatTime(props.task.actualStartTime)}</span>`;
    }
    
    // 添加执行时长
    if (props.task.actualStartTime) {
      const startTime = new Date(props.task.actualStartTime.endsWith('Z') ? props.task.actualStartTime : props.task.actualStartTime + 'Z');
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

    // ResultPending 类别：需要录入结果
    if (category === 'ResultPending') {
      message += `<div style="margin-top: 12px; padding: 8px 12px; background: #fdf6ec; border-radius: 4px; color: #e6a23c; font-size: 12px;">
        📥 请在下方输入执行结果
      </div></div>`;
      
      const { value } = await ElMessageBox.prompt(
        message,
        '结束任务并录入结果',
        {
          confirmButtonText: '确认完成',
          cancelButtonText: '取消',
          inputType: 'textarea',
          inputPlaceholder: '请输入执行结果（必填）...',
          inputValidator: (value) => {
            if (!value || value.trim().length === 0) {
              return '执行结果不能为空';
            }
            return true;
          },
          dangerouslyUseHTMLString: true,
          customClass: 'task-completion-dialog'
        }
      );
      resultPayload = value;
    } 
    // Duration 类别：直接结束
    else if (category === 'Duration') {
      message += `<div style="margin-top: 12px; padding: 8px 12px; background: #f0f9ff; border-radius: 4px; color: #409eff; font-size: 12px;">
        ✓ 任务完成后将更新为已完成状态
      </div></div>`;
      
      await ElMessageBox.confirm(
        message,
        '结束任务',
        {
          confirmButtonText: '确认完成',
          cancelButtonText: '取消',
          type: 'success',
          dangerouslyUseHTMLString: true,
          customClass: 'task-completion-dialog'
        }
      );
    } else {
      // TODO: 其他类别的处理
      ElMessage.warning(`任务类别 ${category} 的流程暂未实现`);
      return;
    }

    const nurseId = getCurrentNurseId();
    if (!nurseId) {
      ElMessage.error('未找到护士信息');
      return;
    }

    const taskId = props.task.id;
    if (!taskId) {
      ElMessage.error('任务ID无效');
      return;
    }

    // 调用API结束任务
    const response = await completeExecutionTask(taskId, nurseId, resultPayload);
    ElMessage.success(response.message || '任务已完成');
    
    // 通知父组件刷新数据
    emit('task-cancelled', taskId);
  } catch (error) {
    if (error !== 'cancel') {
      console.error('结束任务失败:', error);
      ElMessage.error(error.response?.data?.message || '操作失败');
    }
  }
};

// 完成执行任务（已废弃，改为 handleStartCompletion 和 handleFinishTask）
const handleCompleteExecution = async () => {
  try {
    const category = props.task.category;
    let resultPayload = null;

    // 根据任务类别判断是否需要录入结果
    if (category === 'ResultPending' || category === 'DataCollection' || category === 'Verification') {
      // 需要录入结果的任务类别，弹出输入框
      const { value } = await ElMessageBox.prompt(
        '请录入执行结果',
        '完成任务',
        {
          confirmButtonText: '确认完成',
          cancelButtonText: '取消',
          inputType: 'textarea',
          inputPlaceholder: '请输入执行结果（JSON格式或文本）...',
          inputValidator: (value) => {
            if (!value || value.trim().length === 0) {
              return '执行结果不能为空';
            }
            return true;
          }
        }
      );
      resultPayload = value;
    } else {
      // Duration、Immediate 等类别，直接确认完成
      await ElMessageBox.confirm(
        '确认完成任务？',
        '完成任务',
        {
          confirmButtonText: '确认完成',
          cancelButtonText: '取消',
          type: 'success'
        }
      );
    }

    const nurseId = getCurrentNurseId();
    if (!nurseId) {
      ElMessage.error('未找到护士信息');
      return;
    }

    const taskId = props.task.id;
    if (!taskId) {
      ElMessage.error('任务ID无效');
      return;
    }

    // 调用API完成任务
    const response = await completeExecutionTask(taskId, nurseId, resultPayload);
    ElMessage.success(response.message || '任务已完成');
    
    // 通知父组件刷新数据
    emit('task-cancelled', taskId);
  } catch (error) {
    if (error !== 'cancel') {
      console.error('完成任务失败:', error);
      ElMessage.error(error.response?.data?.message || '完成任务失败');
    }
  }
};

// 取消执行任务
const handleCancelExecution = async () => {
  try {
    // 弹出输入框要求填写取消理由
    const { value: cancelReason } = await ElMessageBox.prompt(
      `<div style="text-align: left; font-size: 13px; line-height: 1.8;">
        <div style="margin-bottom: 12px; padding: 16px; background: #fef0f0; border-radius: 8px; box-shadow: 0 2px 8px rgba(245, 108, 108, 0.1);">
          <div style="display: grid; grid-template-columns: auto 1fr; gap: 8px 12px; align-items: center;">
            <span style="color: #909399;">👤 患者：</span>
            <span style="color: #303133; font-weight: 600;">${props.task.patientName} <span style="color: #909399; font-weight: 400;">(🛏️ ${props.task.bedId})</span></span>
            
            <span style="color: #909399;">📋 类型：</span>
            <span style="color: #303133; font-weight: 600;">${props.task.orderTypeName || '执行任务'}</span>
            
            <span style="color: #909399;">📝 任务：</span>
            <span style="color: #303133; font-weight: 600;">${props.task.taskTitle || categoryText.value}</span>
            
            <span style="color: #909399;">🕑 计划时间：</span>
            <span style="color: #606266;">${formatTime(props.task.plannedStartTime)}</span>
            
            <span style="color: #909399;">📊 当前状态：</span>
            <span style="color: #f56c6c; font-weight: 600;">${statusText.value}</span>
          </div>
        </div>
        <div style="padding: 12px; background: #fdf6ec; border-radius: 8px; color: #e6a23c; font-size: 12px; box-shadow: 0 2px 8px rgba(230, 162, 60, 0.1);">
          ⚠️ 请说明取消此任务的原因，该操作将被记录
        </div>
      </div>`,
      '确认取消任务',
      {
        confirmButtonText: '确认取消',
        cancelButtonText: '不取消',
        inputType: 'textarea',
        inputPlaceholder: '请输入取消理由...',
        inputValidator: (value) => {
          if (!value || value.trim().length === 0) {
            return '取消理由不能为空';
          }
          return true;
        },
        dangerouslyUseHTMLString: true,
        customClass: 'task-completion-dialog'
      }
    );

    const nurseId = getCurrentNurseId();
    if (!nurseId) {
      ElMessage.error('未找到护士信息');
      return;
    }

    const taskId = props.task.id;
    if (!taskId) {
      ElMessage.error('任务ID无效');
      return;
    }

    // 调用API取消任务
    const response = await cancelExecutionTask(taskId, nurseId, cancelReason);
    ElMessage.success(response.message || '任务已取消');
    
    // 通知父组件刷新数据
    emit('task-cancelled', taskId);
  } catch (error) {
    if (error !== 'cancel') {
      console.error('取消执行任务失败:', error);
      ElMessage.error(error.response?.data?.message || '取消任务失败');
    }
  }
};

// AppliedConfirmed状态的取消任务（带是否退药选项）
const handleCancelWithReturn = async () => {
  try {
    // 自定义弹窗内容
    const { value: formData } = await ElMessageBox({
      title: '确认取消任务',
      message: `
        <div style="font-size: 14px;">
          <p style="margin-bottom: 12px; color: #606266;">请填写取消任务的理由：</p>
          <textarea 
            id="cancel-reason-input" 
            placeholder="请输入取消理由..." 
            style="width: 100%; height: 80px; padding: 8px; border: 1px solid #dcdfe6; border-radius: 4px; resize: vertical; font-family: inherit;"
          ></textarea>
          <div style="margin-top: 12px;">
            <label style="display: flex; align-items: center; cursor: pointer;">
              <input type="checkbox" id="need-return-checkbox" style="margin-right: 8px; cursor: pointer;" />
              <span>需要直接退药</span>
            </label>
            <p style="margin: 8px 0 0 24px; font-size: 12px; color: #909399;">
              勾选后将直接标记为异常状态，不勾选则进入待退药状态
            </p>
          </div>
        </div>
      `,
      dangerouslyUseHTMLString: true,
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      beforeClose: (action, instance, done) => {
        if (action === 'confirm') {
          const reasonInput = document.getElementById('cancel-reason-input');
          const needReturnCheckbox = document.getElementById('need-return-checkbox');
          const reason = reasonInput?.value?.trim();
          
          if (!reason) {
            ElMessage.error('取消理由不能为空');
            return;
          }
          
          instance.confirmButtonLoading = true;
          instance.confirmButtonText = '处理中...';
          
          // 返回表单数据
          done({
            reason: reason,
            needReturn: needReturnCheckbox?.checked || false
          });
        } else {
          done();
        }
      }
    });

    if (!formData) return;

    const nurseId = getCurrentNurseId();
    if (!nurseId) {
      ElMessage.error('未找到护士信息');
      return;
    }

    const taskId = props.task.id;
    if (!taskId) {
      ElMessage.error('任务ID无效');
      return;
    }

    // 调用API取消任务，传递needReturn参数
    const response = await cancelExecutionTask(
      taskId, 
      nurseId, 
      formData.reason, 
      formData.needReturn
    );
    
    ElMessage.success(response.message || '任务已取消');
    
    // 通知父组件刷新数据
    emit('task-cancelled', taskId);
  } catch (error) {
    if (error !== 'cancel') {
      console.error('取消执行任务失败:', error);
      ElMessage.error(error.response?.data?.message || '取消任务失败');
    }
  }
};

// 跳转到医嘱申请界面
const handleGoToApplication = () => {
  router.push({
    path: '/nurse/application',
    query: {
      patientId: props.task.patientId
    }
  });
};

// 跳转到医嘱申请界面（退药）
const handleGoToReturn = () => {
  router.push({
    path: '/nurse/application',
    query: {
      patientId: props.task.patientId,
      returnMode: 'true'
    }
  });
};

</script>

<style scoped>
.task-item {
  background: #fff;
  border: 1px solid #ebeef5;
  border-radius: 12px;
  padding: 16px;
  transition: all 0.3s ease;
  position: relative;
  overflow: hidden;
  user-select: none;
}

.task-clickable-area {
  cursor: pointer;
}

.task-item::before {
  content: '';
  position: absolute;
  left: 0;
  top: 0;
  bottom: 0;
  width: 4px;
  background: #409eff;
  transition: all 0.3s ease;
}

.task-item:hover {
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.12);
  transform: translateY(-2px);
  border-color: #409eff;
  background: #fff;
}

.task-item:hover::before {
  width: 6px;
}

.task-item:active {
  transform: translateY(0);
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
}

.task-highlight {
  border-width: 2px;
}

.task-highlight.task-overdue {
  border-color: #f56c6c;
  animation: overdue-pulse 2s infinite;
}

.task-highlight.task-due-soon {
  border-color: #e6a23c;
}

.task-overdue {
  border-color: #f56c6c;
  background: #fef0f0;
  box-shadow: 0 0 0 1px #f56c6c inset;
}

.task-overdue::before {
  background: #f56c6c;
  width: 6px;
}

.task-due-soon {
  border-color: #e6a23c;
  background: #fdf6ec;
}

.task-due-soon::before {
  background: #e6a23c;
  width: 6px;
}

.task-completed {
  border-color: #67c23a;
}

.task-completed::before {
  background: #67c23a;
  width: 6px;
}

/* 超时任务闪烁动画 */
@keyframes overdue-pulse {
  0%, 100% {
    box-shadow: 0 0 0 0 rgba(245, 108, 108, 0.7), inset 0 0 0 1px #f56c6c;
  }
  50% {
    box-shadow: 0 0 0 6px rgba(245, 108, 108, 0), inset 0 0 0 1px #f56c6c;
  }
}

.task-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 12px;
}

.task-title {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 16px;
  font-weight: 600;
  flex: 1;
  flex-wrap: wrap;
}

.task-icon {
  color: #409eff;
  flex-shrink: 0;
}

.task-type-tag {
  flex-shrink: 0;
  margin-right: 4px;
  font-weight: 600;
}

.task-order-type {
  color: #909399;
  font-size: 14px;
  font-weight: 400;
  margin: 0 4px;
}

.task-category {
  color: #303133;
  flex-wrap: nowrap;
}

.task-content {
  margin-bottom: 12px;
}

.task-patient,
.task-time {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 8px;
  font-size: 14px;
  color: #606266;
  flex-wrap: wrap;
}

.task-patient .el-icon,
.task-time .el-icon {
  color: #909399;
  flex-shrink: 0;
}

.task-patient .el-tag {
  flex-shrink: 0;
}

.overdue-text {
  color: #f56c6c;
  font-weight: 600;
}

.delay-text {
  color: #e6a23c;
  font-weight: 500;
}

.due-soon-text {
  color: #409eff;
  font-weight: 600;
}

.task-actions {
  display: flex;
  justify-content: flex-end;
  gap: 8px;
  padding-top: 12px;
  border-top: 1px solid #ebeef5;
  flex-wrap: wrap;
}

/* 按钮样式 */
.task-actions :deep(.el-button) {
  transition: all 0.2s ease;
  font-weight: 500;
}

.task-actions :deep(.el-button.is-plain) {
  background-color: #fff;
  border-color: #dcdfe6;
  color: #606266;
}

.task-actions :deep(.el-button.is-plain:hover) {
  background-color: #f5f7fa;
  border-color: #c0c4cc;
  color: #303133;
}

.task-actions :deep(.el-button--primary) {
  background-color: #409eff;
  border-color: #409eff;
  color: #fff;
}

.task-actions :deep(.el-button--primary:hover) {
  background-color: #66b1ff;
  border-color: #66b1ff;
}

.task-actions :deep(.el-button--primary:active) {
  background-color: #3a8ee6;
  border-color: #3a8ee6;
}

.task-actions :deep(.el-button--success) {
  background-color: #67c23a;
  border-color: #67c23a;
  color: #fff;
}

.task-actions :deep(.el-button--success:hover) {
  background-color: #85ce61;
  border-color: #85ce61;
}

.task-actions :deep(.el-button--success:active) {
  background-color: #5daf34;
  border-color: #5daf34;
}

.task-actions :deep(.el-button--danger) {
  background-color: #f56c6c;
  border-color: #f56c6c;
  color: #fff;
}

.task-actions :deep(.el-button--danger:hover) {
  background-color: #f78989;
  border-color: #f78989;
}

.task-actions :deep(.el-button--danger:active) {
  background-color: #dd6161;
  border-color: #dd6161;
}
</style>

<style>
/* 全局样式：自定义 ElMessageBox 宽度和固定大小 */
.task-completion-dialog {
  width: 800px !important;
  max-width: 92vw !important;
}

/* 隐藏 Element Plus 消息框的图标 */
.task-completion-dialog .el-message-box__status {
  display: none !important;
}

/* 调整内容区域，因为没有图标了 */
.task-completion-dialog .el-message-box__message {
  margin-left: 0 !important;
  padding-left: 0 !important;
}

.task-completion-dialog .el-message-box__content {
  min-height: 320px !important;
  max-height: 650px !important;
  overflow-y: auto;
  padding: 24px 28px !important;
}

.task-completion-dialog .el-message-box__message {
  width: 100%;
  line-height: 1.6;
}

.task-completion-dialog .el-message-box__message > div {
  min-width: 100%;
}

/* 输入框样式优化 */
.task-completion-dialog .el-textarea__inner {
  min-height: 120px !important;
  max-height: 300px !important;
  font-size: 13px;
  line-height: 1.6;
  resize: vertical;
}

.task-completion-dialog .el-input__inner {
  font-size: 13px;
}

/* 滚动条美化 */
.task-completion-dialog .el-message-box__content::-webkit-scrollbar {
  width: 8px;
}

.task-completion-dialog .el-message-box__content::-webkit-scrollbar-track {
  background: #f1f1f1;
  border-radius: 4px;
}

.task-completion-dialog .el-message-box__content::-webkit-scrollbar-thumb {
  background: #c1c1c1;
  border-radius: 4px;
}

.task-completion-dialog .el-message-box__content::-webkit-scrollbar-thumb:hover {
  background: #a8a8a8;
}

/* 按钮样式优化 */
.task-completion-dialog .el-message-box__btns {
  padding: 18px 28px;
  border-top: 1px solid #ebeef5;
}

.task-completion-dialog .el-button {
  padding: 11px 28px;
  font-size: 14px;
  font-weight: 500;
  min-width: 90px;
}

/* 标题样式优化 */
.task-completion-dialog .el-message-box__header {
  padding: 20px 28px 16px;
  border-bottom: 1px solid #ebeef5;
}

.task-completion-dialog .el-message-box__title {
  font-size: 16px;
  font-weight: 600;
}
</style>
