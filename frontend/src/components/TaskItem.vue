<template>
  <div 
    class="task-item" 
    :class="{ 
      'task-highlight': highlight,
      'task-overdue': isOverdue,
      'task-due-soon': isDueSoon
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
          - Immediate(即刻执行)：Pending → Completed，显示"完成任务"
          - Duration(持续任务)：Pending → InProgress → Completed，显示"完成"或"结束"
          - ResultPending(结果待定)：Pending → InProgress → Completed，显示"完成"或"结束任务（需录入结果）"
        -->
        
        <!-- AppliedConfirmed(2) 或 Pending(3)：显示根据category定制的"完成"按钮 -->
        <el-button 
          v-if="task.status === 2 || task.status === 'AppliedConfirmed' || task.status === 3 || task.status === 'Pending'" 
          type="primary" 
          size="small"
          :icon="VideoPlay"
          @click.stop="handleStartCompletion"
        >
          {{ getCompletionButtonLabel(task.category, false) }}
        </el-button>

        <!-- InProgress(4)：显示"结束任务"或"结束任务（需录入结果）" -->
        <el-button 
          v-if="task.status === 4 || task.status === 'InProgress'" 
          type="success" 
          size="small"
          :icon="Check"
          @click.stop="handleFinishTask"
        >
          {{ getCompletionButtonLabel(task.category, true) }}
        </el-button>

        <!-- 未完成状态显示"取消任务" -->
        <el-button 
          v-if="(task.status === 2 || task.status === 'AppliedConfirmed' || 
                 task.status === 3 || task.status === 'Pending' || 
                 task.status === 4 || task.status === 'InProgress')" 
          type="danger" 
          plain
          size="small"
          :icon="Close"
          @click.stop="handleCancelExecution"
        >
          取消任务
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
  VideoPlay
} from '@element-plus/icons-vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { 
  cancelNursingTask, 
  completeExecutionTask, 
  cancelExecutionTask 
} from '@/api/nursing';

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

const emit = defineEmits(['click', 'start-input', 'view-detail', 'task-cancelled']);

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
    9: 'danger'
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
    9: '已取消'
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
  let html = `<div style="font-size: 13px; line-height: 1.8;">`;
  
  if (payload.Title) {
    html += `<p><strong>任务：</strong>${payload.Title}</p>`;
  }
  
  if (payload.Description) {
    html += `<p><strong>医嘱内容：</strong>${payload.Description}</p>`;
  }
  
  // 解析药品信息
  if (payload.MedicationInfo) {
    const med = payload.MedicationInfo;
    html += `<div style="margin-top: 8px; padding: 8px; background: #f0f9ff; border-left: 3px solid #409eff;">`;
    html += `<p style="margin: 0; font-weight: 600; color: #409eff;">💊 药品信息</p>`;
    if (med.DrugName) html += `<p style="margin: 4px 0;">药品名称：${med.DrugName}</p>`;
    if (med.Specification) html += `<p style="margin: 4px 0;">规格：${med.Specification}</p>`;
    if (med.Dosage) html += `<p style="margin: 4px 0;">剂量：${med.Dosage}</p>`;
    if (med.Route) html += `<p style="margin: 4px 0;">途径：${med.Route}</p>`;
    if (med.Frequency) html += `<p style="margin: 4px 0;">频次：${med.Frequency}</p>`;
    html += `</div>`;
  }
  
  // 解析核对项
  if (payload.IsChecklist && payload.Items && Array.isArray(payload.Items)) {
    html += `<div style="margin-top: 8px;">`;
    html += `<p style="font-weight: 600; margin-bottom: 4px;">✓ 核对项目：</p>`;
    html += `<ul style="margin: 0; padding-left: 20px;">`;
    payload.Items.forEach((item, index) => {
      if (item.text) {
        const status = item.isChecked ? '✅' : '⬜';
        const required = item.required ? '<span style="color: red;">*</span>' : '';
        html += `<li>${status} ${item.text} ${required}</li>`;
      }
    });
    html += `</ul></div>`;
  }
  
  html += `</div>`;
  return html;
};

// 解析通用DataPayload
const parseDataPayload = (dataPayload) => {
  if (!dataPayload) return '';
  
  try {
    const payload = JSON.parse(dataPayload);
    
    // 如果是药品医嘱，使用专门的解析函数
    if (payload.TaskType === 'MEDICATION_ADMINISTRATION') {
      return parseMedicationPayload(payload);
    }
    
    // 其他类型使用通用格式
    let html = `<div style="font-size: 13px; line-height: 1.8;">`;
    Object.entries(payload).forEach(([key, value]) => {
      if (typeof value === 'object' && value !== null) {
        html += `<p><strong>${key}:</strong></p>`;
        html += `<pre style="margin: 4px 0; padding: 8px; background: #f5f5f5; border-radius: 4px; font-size: 12px;">${JSON.stringify(value, null, 2)}</pre>`;
      } else {
        html += `<p><strong>${key}:</strong> ${value}</p>`;
      }
    });
    html += `</div>`;
    return html;
  } catch {
    return `<pre style="font-size: 12px;">${dataPayload}</pre>`;
  }
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

    // 构建确认消息
    let message = `<div style="text-align: left;">
      <p><strong>任务信息：</strong></p>
      <p>患者：${props.task.patientName} (${props.task.bedId})</p>
      <p>类型：${props.task.orderTypeName || '执行任务'}</p>
      <p>内容：${props.task.taskTitle || categoryText.value}</p>`;
    
    if (taskDetails) {
      message += `<p style="margin-top: 10px;"><strong>详细信息：</strong></p>
      <div style="background: #f5f5f5; padding: 12px; border-radius: 4px; max-height: 300px; overflow-y: auto;\">${taskDetails}</div>`;
    }
    
    // Immediate 类别：直接完成
    if (category === 'Immediate') {
      message += `<p style="margin-top: 10px; color: #409eff;"><strong>确认完成此任务</strong></p></div>`;
      
      await ElMessageBox.confirm(
        message,
        '确认完成',
        {
          confirmButtonText: '确认完成',
          cancelButtonText: '取消',
          type: 'warning',
          dangerouslyUseHTMLString: true
        }
      );
    } 
    // Verification 类别：直接完成（核对类）
    else if (category === 'Verification') {
      message += `<p style="margin-top: 10px; color: #409eff;"><strong>确认核对完成</strong></p></div>`;
      
      await ElMessageBox.confirm(
        message,
        '确认核对完成',
        {
          confirmButtonText: '确认核对完成',
          cancelButtonText: '取消',
          type: 'warning',
          dangerouslyUseHTMLString: true
        }
      );
    }
    // Duration 和 ResultPending 类别：开始执行
    else if (category === 'Duration' || category === 'ResultPending') {
      message += `<p style="margin-top: 10px; color: #409eff;"><strong>确认开始执行</strong></p></div>`;
      
      await ElMessageBox.confirm(
        message,
        '确认开始执行',
        {
          confirmButtonText: '确认开始',
          cancelButtonText: '取消',
          type: 'info',
          dangerouslyUseHTMLString: true
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

    // 构建基础消息
    let message = `<div style="text-align: left;">
      <p><strong>任务信息：</strong></p>
      <p>患者：${props.task.patientName} (${props.task.bedId})</p>
      <p>类型：${props.task.orderTypeName || '执行任务'}</p>
      <p>内容：${props.task.taskTitle || categoryText.value}</p>`;
    
    if (taskDetails) {
      message += `<p style="margin-top: 10px;"><strong>详细信息：</strong></p>
      <div style="background: #f5f5f5; padding: 12px; border-radius: 4px; max-height: 300px; overflow-y: auto;\">${taskDetails}</div>`;
    }

    // ResultPending 类别：需要录入结果
    if (category === 'ResultPending') {
      message += `<p style="margin-top: 10px; color: #e6a23c;"><strong>请在下方录入执行结果</strong></p></div>`;
      
      const { value } = await ElMessageBox.prompt(
        message,
        '结束任务并录入结果',
        {
          confirmButtonText: '确认完成',
          cancelButtonText: '取消',
          inputType: 'textarea',
          inputPlaceholder: '请输入执行结果（JSON或文本格式）...',
          inputValidator: (value) => {
            if (!value || value.trim().length === 0) {
              return '执行结果不能为空';
            }
            return true;
          },
          dangerouslyUseHTMLString: true
        }
      );
      resultPayload = value;
    } 
    // Duration 类别：直接结束
    else if (category === 'Duration') {
      message += `<p style="margin-top: 10px; color: #409eff;"><strong>确认结束执行</strong></p></div>`;
      
      await ElMessageBox.confirm(
        message,
        '结束任务',
        {
          confirmButtonText: '确认完成',
          cancelButtonText: '取消',
          type: 'success',
          dangerouslyUseHTMLString: true
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
        }
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
  background: linear-gradient(to bottom, #409eff, #85ce61);
  transition: all 0.3s ease;
}

.task-item:hover {
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.12);
  transform: translateY(-2px);
  border-color: #409eff;
  background: linear-gradient(to bottom right, #fff, #f5f7fa);
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
  background: linear-gradient(135deg, #fff 0%, #fef0f0 100%);
  box-shadow: 0 0 0 1px #f56c6c inset;
}

.task-overdue::before {
  background: linear-gradient(to bottom, #f56c6c, #fd7271);
  width: 6px;
}

.task-due-soon {
  border-color: #e6a23c;
  background: linear-gradient(135deg, #fff 0%, #fdf6ec 100%);
}

.task-due-soon::before {
  background: linear-gradient(to bottom, #e6a23c, #f5a623);
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
</style>
