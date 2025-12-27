<template>
  <el-dialog
    v-model="visible"
    :title="dialogTitle"
    width="600px"
    @close="handleClose"
  >
    <div v-if="task" class="task-detail">
      <!-- 基本信息 -->
      <el-descriptions :column="2" border>
        <el-descriptions-item label="任务ID">
          {{ task.id }}
        </el-descriptions-item>
        <el-descriptions-item label="医嘱ID">
          {{ task.medicalOrderId }}
        </el-descriptions-item>
        <el-descriptions-item label="患者姓名">
          {{ task.patientName }}
        </el-descriptions-item>
        <el-descriptions-item label="床号">
          {{ task.bedId }}
        </el-descriptions-item>
        <el-descriptions-item label="医嘱类型">
          {{ task.orderTypeName || '执行任务' }}
        </el-descriptions-item>
        <el-descriptions-item label="任务类别">
          <el-tag size="small">{{ getCategoryText(task.category) }}</el-tag>
        </el-descriptions-item>
        <el-descriptions-item label="任务状态">
          <el-tag :type="getStatusTagType(task.status)">
            {{ getStatusText(task.status) }}
          </el-tag>
        </el-descriptions-item>
        <el-descriptions-item label="责任护士">
          {{ task.assignedNurseName || '-' }}
        </el-descriptions-item>
        <el-descriptions-item label="计划开始时间" :span="2">
          {{ formatDateTime(task.plannedStartTime) }}
        </el-descriptions-item>
        <el-descriptions-item
          v-if="task.actualStartTime"
          label="实际开始时间"
          :span="2"
        >
          {{ formatDateTime(task.actualStartTime) }}
        </el-descriptions-item>
        <el-descriptions-item
          v-if="task.executorNurseName"
          label="执行护士"
          :span="2"
        >
          {{ task.executorNurseName }}
        </el-descriptions-item>
        <el-descriptions-item
          v-if="task.actualEndTime"
          label="完成时间"
          :span="2"
        >
          {{ formatDateTime(task.actualEndTime) }}
        </el-descriptions-item>
      </el-descriptions>

      <!-- 任务详情 -->
      <div v-if="task.taskTitle" class="detail-section">
        <h4>任务内容</h4>
        <p>{{ task.taskTitle }}</p>
      </div>

      <!-- DataPayload 详情 -->
      <div v-if="task.dataPayload" class="detail-section">
        <h4>任务数据</h4>
        <div class="payload-content" v-html="parseDataPayloadHtml(task.dataPayload)"></div>
      </div>

      <!-- ResultPayload 详情 -->
      <div v-if="task.resultPayload" class="detail-section">
        <h4>执行结果</h4>
        <pre class="json-display">{{ formatJson(task.resultPayload) }}</pre>
      </div>
    </div>

    <template #footer>
      <el-button @click="handleClose">关闭</el-button>
    </template>
  </el-dialog>
</template>

<script setup>
import { computed } from 'vue';

const props = defineProps({
  modelValue: {
    type: Boolean,
    default: false
  },
  task: {
    type: Object,
    default: null
  }
});

const emit = defineEmits(['update:modelValue']);

const visible = computed({
  get: () => props.modelValue,
  set: (val) => emit('update:modelValue', val)
});

const dialogTitle = computed(() => {
  if (!props.task) return '任务详情';
  return `${props.task.orderTypeName || '执行任务'} - ${props.task.taskTitle || '详情'}`;
});

const handleClose = () => {
  emit('update:modelValue', false);
};

// 格式化日期时间
const formatDateTime = (dateString) => {
  if (!dateString) return '';
  const date = new Date(dateString);
  return date.toLocaleString('zh-CN', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit'
  });
};

// 格式化JSON
const formatJson = (jsonString) => {
  if (!jsonString) return '';
  try {
    const obj = JSON.parse(jsonString);
    return JSON.stringify(obj, null, 2);
  } catch (error) {
    return jsonString;
  }
};

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

// 解析通用DataPayload为HTML
const parseDataPayloadHtml = (dataPayload) => {
  if (!dataPayload) return '<p style="color: #909399;">无数据</p>';
  
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

// 任务类别文本
const getCategoryText = (category) => {
  const textMap = {
    'Immediate': '即刻执行',
    'Duration': '持续任务',
    'ResultPending': '结果待定',
    'DataCollection': '数据采集',
    'Verification': '核对验证',
    'ApplicationWithPrint': '申请打印'
  };
  return textMap[category] || category;
};

// 状态标签类型
const getStatusTagType = (status) => {
  const typeMap = {
    0: 'info',
    1: 'info',
    2: 'warning',
    3: 'warning',
    4: 'primary',
    5: 'success',
    6: 'danger',
    7: 'danger',
    8: 'info',
    9: 'danger'
  };
  return typeMap[status] || 'info';
};

// 状态文本
const getStatusText = (status) => {
  const textMap = {
    0: '待申请',
    1: '已申请',
    2: '已就绪',
    3: '待执行',
    4: '执行中',
    5: '已完成',
    6: '停止中',
    7: '已停止',
    8: '异常',
    9: '已取消'
  };
  return textMap[status] || status;
};
</script>

<style scoped>
.task-detail {
  max-height: 600px;
  overflow-y: auto;
}

.detail-section {
  margin-top: 20px;
}

.detail-section h4 {
  margin-bottom: 10px;
  color: #303133;
  font-size: 14px;
  font-weight: 600;
}

.detail-section p {
  margin: 0;
  padding: 10px;
  background: #f5f7fa;
  border-radius: 4px;
  color: #606266;
}

.payload-content {
  padding: 10px;
  background: #f5f7fa;
  border-radius: 4px;
  color: #606266;
}

.payload-content p {
  margin: 4px 0;
  padding: 0;
  background: transparent;
}

.json-display {
  background: #f5f7fa;
  padding: 12px;
  border-radius: 4px;
  font-size: 12px;
  line-height: 1.5;
  max-height: 300px;
  overflow-y: auto;
  margin: 0;
  color: #303133;
}
</style>
