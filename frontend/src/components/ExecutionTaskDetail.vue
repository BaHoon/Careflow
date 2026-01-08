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
        <el-descriptions-item 
          v-if="task.category !== 'ApplicationWithPrint'"
          label="计划开始时间" 
          :span="2"
        >
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
          v-if="task.executorNurseName && task.status === 5"
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

      <!-- ResultPayload 详情（隐藏取药任务的执行结果） -->
      <div v-if="task.resultPayload && !isRetrieveMedicationTask(task)" class="detail-section">
        <h4>执行结果</h4>
        <pre class="json-display">{{ formatJson(task.resultPayload) }}</pre>
      </div>

      <!-- 异常原因 -->
      <div v-if="task.exceptionReason && task.exceptionReason.trim()" class="detail-section exception-section">
        <h4>⚠️ 异常原因</h4>
        <div class="exception-content">{{ task.exceptionReason }}</div>
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

// 判断是否为取药任务
const isRetrieveMedicationTask = (task) => {
  if (!task) return false;
  
  // 检查 resultPayload 中是否包含 scannedDrugIds 字段（取药任务特有的执行结果格式）
  if (task.resultPayload) {
    try {
      const resultPayload = JSON.parse(task.resultPayload);
      if (resultPayload && (resultPayload.scannedDrugIds || resultPayload.ScannedDrugIds)) {
        return true;
      }
    } catch (e) {
      // 如果解析失败，检查字符串中是否包含 scannedDrugIds
      if (task.resultPayload.includes('scannedDrugIds') || task.resultPayload.includes('ScannedDrugIds')) {
        return true;
      }
    }
  }
  
  // 检查 dataPayload 中的 Title 是否包含"取药"
  if (task.dataPayload) {
    try {
      const dataPayload = JSON.parse(task.dataPayload);
      if (dataPayload && dataPayload.Title && dataPayload.Title.includes('取药')) {
        return true;
      }
    } catch (e) {
      // 忽略解析错误
    }
  }
  
  // 检查 taskTitle 是否包含"取药"
  if (task.taskTitle && task.taskTitle.includes('取药')) {
    return true;
  }
  
  return false;
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
  
  // 如果有药品清单（MedicationInfo.Items），优先显示药品列表
  if (payload.MedicationInfo && payload.MedicationInfo.Items && Array.isArray(payload.MedicationInfo.Items)) {
    const items = payload.MedicationInfo.Items;
    if (items.length > 0) {
      html += `<div style="margin-bottom: 12px; padding: 10px; background: #f0f9ff; border-left: 3px solid #409eff; border-radius: 4px;">`;
      html += `<p style="margin: 0 0 8px 0; font-weight: 600; color: #409eff;">💊 药品清单</p>`;
      html += `<table style="width: 100%; border-collapse: collapse;">`;
      html += `<thead><tr style="background: #e8f4ff;">
        <th style="padding: 6px; text-align: left; border: 1px solid #d9ecff;">药品名称</th>
        <th style="padding: 6px; text-align: left; border: 1px solid #d9ecff; width: 100px;">规格</th>
        <th style="padding: 6px; text-align: center; border: 1px solid #d9ecff; width: 80px;">剂量</th>
        <th style="padding: 6px; text-align: left; border: 1px solid #d9ecff; width: 120px;">备注</th>
      </tr></thead><tbody>`;
      
      items.forEach(item => {
        const drugName = item.DrugName || item.drugName || '-';
        const specification = item.Specification || item.specification || '-';
        const dosage = item.Dosage || item.dosage || '-';
        const note = item.Note || item.note || '';
        
        html += `<tr>
          <td style="padding: 6px; border: 1px solid #d9ecff; font-weight: 600;">${drugName}</td>
          <td style="padding: 6px; border: 1px solid #d9ecff; color: #606266;">${specification}</td>
          <td style="padding: 6px; text-align: center; border: 1px solid #d9ecff; font-weight: 600; color: #67c23a;">${dosage}</td>
          <td style="padding: 6px; border: 1px solid #d9ecff; color: #909399; font-size: 12px;">${note}</td>
        </tr>`;
      });
      
      html += `</tbody></table></div>`;
    }
  }
  
  // 显示给药信息
  if (payload.MedicationInfo) {
    const med = payload.MedicationInfo;
    if (med.UsageRoute !== undefined || med.FrequencyDescription || med.ExecutionTime) {
      html += `<div style="margin-top: 8px; padding: 8px; background: #fef0f0; border-left: 3px solid #f56c6c; border-radius: 4px;">`;
      html += `<p style="margin: 0 0 4px 0; font-weight: 600; color: #f56c6c;">📋 给药信息</p>`;
      if (med.UsageRoute !== undefined) {
        const routeNames = {1: '口服', 2: '外用/涂抹', 10: '肌内注射', 11: '皮下注射', 12: '静脉推注', 20: '静脉滴注', 30: '皮试'};
        html += `<p style="margin: 4px 0;">途径：${routeNames[med.UsageRoute] || '未知途径'}</p>`;
      }
      if (med.FrequencyDescription) html += `<p style="margin: 4px 0;">频次：${med.FrequencyDescription}</p>`;
      if (med.ExecutionTime) html += `<p style="margin: 4px 0;">执行时间：${med.ExecutionTime}</p>`;
      if (med.SlotName) html += `<p style="margin: 4px 0;">时间段：${med.SlotName}</p>`;
      html += `</div>`;
    }
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

// 解析物品核对任务（手术类）
const parseSupplyCheckPayload = (payload) => {
  let html = `<div style="font-size: 13px; line-height: 1.8;">`;
  
  if (payload.Description) {
    html += `<p style="color: #606266; margin-bottom: 12px;">${payload.Description}</p>`;
  }
  
  // 显示物品清单
  if (payload.Items && Array.isArray(payload.Items) && payload.Items.length > 0) {
    html += `<div style="margin-bottom: 12px; padding: 10px; background: #fef0f0; border-left: 3px solid #f56c6c; border-radius: 4px;">`;
    html += `<p style="margin: 0 0 8px 0; font-weight: 600; color: #f56c6c;">📦 物品清单</p>`;
    html += `<table style="width: 100%; border-collapse: collapse;">`;
    html += `<thead><tr style="background: #fde2e2;">
      <th style="padding: 6px; text-align: left; border: 1px solid #fcd3d3;">名称</th>
      <th style="padding: 6px; text-align: center; border: 1px solid #fcd3d3; width: 80px;">数量</th>
      <th style="padding: 6px; text-align: center; border: 1px solid #fcd3d3; width: 80px;">类型</th>
      <th style="padding: 6px; text-align: left; border: 1px solid #fcd3d3;">备注</th>
    </tr></thead><tbody>`;
    
    payload.Items.forEach(item => {
      const typeTag = item.Type === 'Drug' ? '<span style="color: #409eff;">药品</span>' : 
                      item.Type === 'Equipment' ? '<span style="color: #67c23a;">器械</span>' : item.Type || '-';
      html += `<tr>
        <td style="padding: 6px; border: 1px solid #fcd3d3;">${item.Name || '-'}</td>
        <td style="padding: 6px; text-align: center; border: 1px solid #fcd3d3;">${item.Count || '-'}</td>
        <td style="padding: 6px; text-align: center; border: 1px solid #fcd3d3;">${typeTag}</td>
        <td style="padding: 6px; border: 1px solid #fcd3d3; color: #909399;">${item.Note || '-'}</td>
      </tr>`;
    });
    
    html += `</tbody></table></div>`;
  }
  
  // 核对项
  if (payload.IsChecklist) {
    html += `<p style="color: #e6a23c; font-size: 12px; margin-top: 8px;">⚠️ 请逐一核对上述物品</p>`;
  }
  
  html += `</div>`;
  return html;
};

// 解析手术宣教任务
const parseEducationPayload = (payload) => {
  let html = `<div style="font-size: 13px; line-height: 1.8;">`;
  
  if (payload.Description) {
    html += `<div style="padding: 10px; background: #f0f9ff; border-left: 3px solid #409eff; border-radius: 4px;">`;
    html += `<p style="margin: 0; color: #303133;">${payload.Description}</p>`;
    html += `</div>`;
  }
  
  html += `<p style="color: #909399; font-size: 12px; margin-top: 8px;">💡 完成宣教后点击"确认完成"</p>`;
  html += `</div>`;
  return html;
};

// 解析术前操作任务
const parseNursingOpPayload = (payload) => {
  let html = `<div style="font-size: 13px; line-height: 1.8;">`;
  
  if (payload.Description) {
    html += `<div style="padding: 10px; background: #fef0f0; border-left: 3px solid #e6a23c; border-radius: 4px;">`;
    html += `<p style="margin: 0; color: #303133;">${payload.Description}</p>`;
    html += `</div>`;
  }
  
  html += `</div>`;
  return html;
};

// 解析通用任务（简化显示）
const parseGenericPayload = (payload) => {
  let html = `<div style="font-size: 13px; line-height: 1.8;">`;
  
  // 只显示关键信息
  if (payload.Title && payload.Title !== payload.Description) {
    html += `<p><strong>标题：</strong>${payload.Title}</p>`;
  }
  
  if (payload.Description) {
    html += `<p><strong>说明：</strong>${payload.Description}</p>`;
  }
  
  // 不显示过多的技术字段（如TaskType等）
  html += `</div>`;
  return html;
};

// 解析通用DataPayload为HTML
const parseDataPayloadHtml = (dataPayload) => {
  if (!dataPayload) return '<p style="color: #909399;">无数据</p>';
  
  try {
    const payload = JSON.parse(dataPayload);
    
    // 药品给药任务
    if (payload.TaskType === 'MEDICATION_ADMINISTRATION') {
      return parseMedicationPayload(payload);
    }
    
    // 物品核对任务（手术类）
    if (payload.TaskType === 'SUPPLY_CHECK') {
      return parseSupplyCheckPayload(payload);
    }
    
    // 手术宣教任务
    if (payload.TaskType === 'EDUCATION') {
      return parseEducationPayload(payload);
    }
    
    // 术前操作任务
    if (payload.TaskType === 'NURSING_OP') {
      return parseNursingOpPayload(payload);
    }
    
    // 通用格式（简化显示，不显示过多技术细节）
    return parseGenericPayload(payload);
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

.exception-section {
  border-top: 2px dashed #f56c6c;
  padding-top: 16px;
  margin-top: 16px;
}

.exception-content {
  color: #f56c6c;
  font-weight: 600;
  padding: 12px;
  background: #fef0f0;
  border-radius: 4px;
  border-left: 3px solid #f56c6c;
  line-height: 1.6;
  white-space: pre-wrap;
  word-break: break-word;
}
</style>
