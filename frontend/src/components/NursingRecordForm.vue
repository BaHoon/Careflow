<template>
  <el-dialog
    v-model="visible"
    :title="dialogTitle"
    width="800px"
    :close-on-click-modal="false"
    @close="handleClose"
  >
    <el-form
      v-if="!isViewMode"
      ref="formRef"
      :model="formData"
      :rules="formRules"
      label-width="120px"
      label-position="right"
    >
      <!-- 任务信息 -->
      <el-divider content-position="left">
        <el-icon><InfoFilled /></el-icon>
        <span>任务信息</span>
      </el-divider>
      
      <el-row :gutter="20">
        <el-col :span="12">
          <el-form-item label="计划时间">
            <span class="info-text">{{ formatDateTime(recordData.plannedStartTime || recordData.scheduledTime) }}</span>
          </el-form-item>
        </el-col>
        <el-col :span="12">
          <el-form-item label="任务类型">
            <el-tag :type="(recordData.category === 'Routine' || recordData.taskType === 'Routine') ? 'primary' : 'warning'">
              {{ (recordData.category === 'Routine' || recordData.taskType === 'Routine') ? '常规测量' : '复测' }}
            </el-tag>
          </el-form-item>
        </el-col>
      </el-row>

      <el-form-item label="录入时间" prop="executionTime" required>
        <el-date-picker
          v-model="formData.executionTime"
          type="datetime"
          placeholder="选择录入时间"
          format="YYYY-MM-DD HH:mm"
          value-format="YYYY-MM-DDTHH:mm:ss"
          style="width: 100%"
        />
      </el-form-item>

      <!-- 生命体征 -->
      <el-divider content-position="left">
        <el-icon><Compass /></el-icon>
        <span>生命体征</span>
      </el-divider>

      <el-row :gutter="20">
        <el-col :span="12">
          <el-form-item label="体温 (℃)" prop="temperature" required>
            <el-input-number
              v-model="formData.temperature"
              :min="35"
              :max="42"
              :precision="1"
              :step="0.1"
              style="width: 100%"
            />
          </el-form-item>
        </el-col>
        <el-col :span="12">
          <el-form-item label="测温方式" prop="tempType" required>
            <el-select v-model="formData.tempType" style="width: 100%">
              <el-option label="腋温" value="腋温" />
              <el-option label="口温" value="口温" />
              <el-option label="肛温" value="肛温" />
            </el-select>
          </el-form-item>
        </el-col>
      </el-row>

      <el-row :gutter="20">
        <el-col :span="12">
          <el-form-item label="脉搏 (次/分)" prop="pulse" required>
            <el-input-number
              v-model="formData.pulse"
              :min="40"
              :max="180"
              style="width: 100%"
            />
          </el-form-item>
        </el-col>
        <el-col :span="12">
          <el-form-item label="呼吸 (次/分)" prop="respiration" required>
            <el-input-number
              v-model="formData.respiration"
              :min="10"
              :max="60"
              style="width: 100%"
            />
          </el-form-item>
        </el-col>
      </el-row>

      <el-row :gutter="20">
        <el-col :span="12">
          <el-form-item label="收缩压(mmHg)" prop="sysBp" required>
            <el-input-number
              v-model="formData.sysBp"
              :min="60"
              :max="250"
              style="width: 100%"
            />
          </el-form-item>
        </el-col>
        <el-col :span="12">
          <el-form-item label="舒张压(mmHg)" prop="diaBp" required>
            <el-input-number
              v-model="formData.diaBp"
              :min="30"
              :max="150"
              style="width: 100%"
            />
          </el-form-item>
        </el-col>
      </el-row>

      <el-row :gutter="20">
        <el-col :span="12">
          <el-form-item label="血氧 (%)" prop="spo2" required>
            <el-input-number
              v-model="formData.spo2"
              :min="0"
              :max="100"
              :precision="1"
              :step="0.1"
              style="width: 100%"
            />
          </el-form-item>
        </el-col>
        <el-col :span="12">
          <el-form-item label="疼痛评分" prop="painScore">
            <el-rate
              v-model="formData.painScore"
              :max="10"
              show-score
              :colors="['#99A9BF', '#F7BA2A', '#FF9900']"
            />
          </el-form-item>
        </el-col>
      </el-row>

      <!-- 护理笔记 -->
      <el-divider content-position="left">
        <el-icon><EditPen /></el-icon>
        <span>护理笔记（可选）</span>
      </el-divider>

      <el-row :gutter="20">
        <el-col :span="12">
          <el-form-item label="意识状态">
            <el-select v-model="formData.consciousness" placeholder="选择意识状态" style="width: 100%">
              <el-option label="清醒" value="清醒" />
              <el-option label="嗜睡" value="嗜睡" />
              <el-option label="昏迷" value="昏迷" />
              <el-option label="谵妄" value="谵妄" />
            </el-select>
          </el-form-item>
        </el-col>
        <el-col :span="12">
          <el-form-item label="皮肤状况">
            <el-select v-model="formData.skinCondition" placeholder="选择皮肤状况" style="width: 100%">
              <el-option label="完好" value="完好" />
              <el-option label="苍白" value="苍白" />
              <el-option label="发绀" value="发绀" />
              <el-option label="黄染" value="黄染" />
              <el-option label="压疮" value="压疮" />
            </el-select>
          </el-form-item>
        </el-col>
      </el-row>

      <el-row :gutter="20">
        <el-col :span="12">
          <el-form-item label="入量 (ml)">
            <el-input-number
              v-model="formData.intakeVolume"
              :min="0"
              :max="5000"
              placeholder="饮水、输液等"
              style="width: 100%"
            />
          </el-form-item>
        </el-col>
        <el-col :span="12">
          <el-form-item label="出量 (ml)">
            <el-input-number
              v-model="formData.outputVolume"
              :min="0"
              :max="5000"
              placeholder="尿量、引流液等"
              style="width: 100%"
            />
          </el-form-item>
        </el-col>
      </el-row>

      <el-form-item label="病情观察">
        <el-input
          v-model="formData.noteContent"
          type="textarea"
          :rows="3"
          placeholder="记录患者病情观察、特殊情况等..."
          maxlength="500"
          show-word-limit
        />
      </el-form-item>

      <el-form-item label="健康教育">
        <el-input
          v-model="formData.healthEducation"
          type="textarea"
          :rows="2"
          placeholder="记录对患者的健康教育内容..."
          maxlength="300"
          show-word-limit
        />
      </el-form-item>
    </el-form>

    <!-- 查看模式 -->
    <div v-else class="view-mode">
      <!-- 任务信息 -->
      <el-descriptions title="任务信息" :column="2" border>
        <el-descriptions-item label="计划时间">
          {{ formatDateTime(recordData.plannedStartTime || recordData.scheduledTime) }}
        </el-descriptions-item>
        <el-descriptions-item label="任务类型">
          <el-tag :type="recordData.category === 'Routine' || recordData.taskType === 'Routine' ? 'primary' : 'warning'">
            {{ (recordData.category === 'Routine' || recordData.taskType === 'Routine') ? '常规测量' : '复测' }}
          </el-tag>
        </el-descriptions-item>
        <el-descriptions-item label="录入时间">
          {{ formatDateTime(recordData.actualStartTime || recordData.executeTime) || '未知' }}
        </el-descriptions-item>
        <el-descriptions-item label="录入护士">
          {{ recordData.executorNurseName || recordData.assignedNurseName || recordData.executorNurse || '未知' }}
        </el-descriptions-item>
      </el-descriptions>

      <!-- 生命体征 -->
      <el-descriptions title="生命体征" :column="2" border class="mt-20">
        <el-descriptions-item label="体温">
          {{ vitalSignsData.temperature || '-' }}℃ ({{ vitalSignsData.tempType || vitalSignsData.temp_type || '-' }})
        </el-descriptions-item>
        <el-descriptions-item label="脉搏">
          {{ vitalSignsData.pulse || '-' }} 次/分
        </el-descriptions-item>
        <el-descriptions-item label="呼吸">
          {{ vitalSignsData.respiration || '-' }} 次/分
        </el-descriptions-item>
        <el-descriptions-item label="血压">
          {{ vitalSignsData.sysBp || vitalSignsData.sys_bp || '-' }}/{{ vitalSignsData.diaBp || vitalSignsData.dia_bp || '-' }} mmHg
        </el-descriptions-item>
        <el-descriptions-item label="血氧">
          {{ vitalSignsData.spo2 || '-' }}%
        </el-descriptions-item>
        <el-descriptions-item label="疼痛评分">
          {{ vitalSignsData.painScore || vitalSignsData.pain_score || '0' }} 分
        </el-descriptions-item>
      </el-descriptions>

      <!-- 护理笔记 -->
      <el-descriptions title="护理笔记" :column="2" border class="mt-20">
        <!-- 意识状态 - 有值才显示 -->
        <el-descriptions-item v-if="vitalSignsData.consciousness" label="意识状态">
          {{ vitalSignsData.consciousness }}
        </el-descriptions-item>
        <!-- 皮肤状况 - 有值才显示 -->
        <el-descriptions-item v-if="vitalSignsData.skinCondition || vitalSignsData.skin_condition" label="皮肤状况">
          {{ vitalSignsData.skinCondition || vitalSignsData.skin_condition }}
        </el-descriptions-item>
        <!-- 入量 - 有值才显示 -->
        <el-descriptions-item v-if="vitalSignsData.intakeVolume || vitalSignsData.intake_volume" label="入量">
          {{ vitalSignsData.intakeVolume || vitalSignsData.intake_volume }} ml
          <span v-if="vitalSignsData.intakeType || vitalSignsData.intake_type" class="sub-info">
            ({{ vitalSignsData.intakeType || vitalSignsData.intake_type }})
          </span>
        </el-descriptions-item>
        <!-- 出量 - 有值才显示 -->
        <el-descriptions-item v-if="vitalSignsData.outputVolume || vitalSignsData.output_volume" label="出量">
          {{ vitalSignsData.outputVolume || vitalSignsData.output_volume }} ml
          <span v-if="vitalSignsData.outputType || vitalSignsData.output_type" class="sub-info">
            ({{ vitalSignsData.outputType || vitalSignsData.output_type }})
          </span>
        </el-descriptions-item>
        <!-- 体重 - 有值才显示 -->
        <el-descriptions-item v-if="vitalSignsData.weight" label="体重">
          {{ vitalSignsData.weight }} kg
        </el-descriptions-item>
        <!-- 干预措施 - 有值才显示 -->
        <el-descriptions-item v-if="vitalSignsData.intervention" label="干预措施" :span="2">
          {{ vitalSignsData.intervention }}
        </el-descriptions-item>
        <!-- 病情观察 - 始终显示 -->
        <el-descriptions-item label="病情观察" :span="2">
          <div class="note-content">
            {{ vitalSignsData.noteContent || vitalSignsData.note_content || '无' }}
          </div>
        </el-descriptions-item>
        <!-- 健康教育 - 有值才显示 -->
        <el-descriptions-item v-if="vitalSignsData.healthEducation || vitalSignsData.health_education" label="健康教育" :span="2">
          <div class="note-content">
            {{ vitalSignsData.healthEducation || vitalSignsData.health_education }}
          </div>
        </el-descriptions-item>
      </el-descriptions>
    </div>

    <template #footer>
      <span class="dialog-footer">
        <el-button @click="handleClose">{{ isViewMode ? '关闭' : '取消' }}</el-button>
        <el-button v-if="!isViewMode" type="primary" @click="handleSubmit" :loading="submitting">
          提交
        </el-button>
      </span>
    </template>
  </el-dialog>
</template>

<script setup>
import { ref, computed, watch } from 'vue';
import { InfoFilled, Compass, EditPen } from '@element-plus/icons-vue';
import { ElMessage } from 'element-plus';

const props = defineProps({
  modelValue: {
    type: Boolean,
    default: false
  },
  recordData: {
    type: Object,
    default: () => ({})
  },
  mode: {
    type: String,
    default: 'input', // 'input' 或 'view'
    validator: (value) => ['input', 'view'].includes(value)
  },
  currentNurseId: {
    type: String,
    required: true
  }
});

const emit = defineEmits(['update:modelValue', 'submit-success']);

const formRef = ref(null);
const submitting = ref(false);

const visible = computed({
  get: () => props.modelValue,
  set: (val) => emit('update:modelValue', val)
});

const isViewMode = computed(() => props.mode === 'view');

const dialogTitle = computed(() => {
  return isViewMode.value ? '查看护理记录' : '录入护理记录';
});

// 获取中国当前时间（ISO 8601格式）
const getChinaTime = () => {
  const now = new Date();
  // 格式化为本地时间的ISO格式：2025-12-23T15:45:00
  const year = now.getFullYear();
  const month = String(now.getMonth() + 1).padStart(2, '0');
  const day = String(now.getDate()).padStart(2, '0');
  const hours = String(now.getHours()).padStart(2, '0');
  const minutes = String(now.getMinutes()).padStart(2, '0');
  const seconds = String(now.getSeconds()).padStart(2, '0');
  return `${year}-${month}-${day}T${hours}:${minutes}:${seconds}`;
};

// 表单数据
const formData = ref({
  taskId: 0,
  currentNurseId: '',
  executionTime: getChinaTime(),
  // 生命体征（必填）
  temperature: 36.5,
  tempType: '腋温',
  pulse: 80,
  respiration: 18,
  sysBp: 120,
  diaBp: 80,
  spo2: 98,
  painScore: 0,
  weight: null,
  intervention: '',
  // 护理笔记（可选）
  consciousness: '清醒',
  skinCondition: '完好',
  intakeVolume: null,
  outputVolume: null,
  noteContent: '',
  healthEducation: ''
});

// 查看模式的体征数据（从 API 获取）
const vitalSignsData = ref({});

// 加载体征数据
const loadVitalSignsData = async () => {
  const taskId = props.recordData.id;
  if (!taskId) {
    console.warn('未找到任务ID');
    return;
  }
  
  try {
    console.log('📋 加载体征数据，recordData:', props.recordData);
    
    // 方案1: 如果有 vitalSigns 字段，直接使用
    if (props.recordData.vitalSigns) {
      vitalSignsData.value = props.recordData.vitalSigns;
      console.log('✅ 从 vitalSigns 字段加载数据');
      return;
    }
    
    // 方案2: 从 resultPayload 解析（后端返回的JSON字符串）
    if (props.recordData.resultPayload) {
      try {
        const payload = typeof props.recordData.resultPayload === 'string' 
          ? JSON.parse(props.recordData.resultPayload) 
          : props.recordData.resultPayload;
        vitalSignsData.value = payload;
        console.log('✅ 从 resultPayload 解析数据:', payload);
        return;
      } catch (parseError) {
        console.error('解析 resultPayload 失败:', parseError);
      }
    }
    
    // 方案3: 从 dataPayload 解析（可能是任务参数）
    if (props.recordData.dataPayload) {
      try {
        const payload = typeof props.recordData.dataPayload === 'string' 
          ? JSON.parse(props.recordData.dataPayload) 
          : props.recordData.dataPayload;
        vitalSignsData.value = payload;
        console.log('✅ 从 dataPayload 解析数据:', payload);
        return;
      } catch (parseError) {
        console.error('解析 dataPayload 失败:', parseError);
      }
    }
    
    // TODO: 方案4：调用后端 API 获取 VitalSignsRecord 数据
    // const response = await getVitalSignsByTaskId(taskId);
    // vitalSignsData.value = response.data;
    
    console.log('⚠️ 未找到体征数据');
    vitalSignsData.value = {};
  } catch (error) {
    console.error('加载体征数据失败:', error);
    vitalSignsData.value = {};
  }
};

// 表单验证规则
const formRules = {
  executionTime: [
    { required: true, message: '请选择录入时间', trigger: 'change' }
  ],
  temperature: [
    { required: true, message: '请输入体温', trigger: 'blur' },
    { type: 'number', min: 35, max: 42, message: '体温范围: 35-42℃', trigger: 'blur' }
  ],
  pulse: [
    { required: true, message: '请输入脉搏', trigger: 'blur' },
    { type: 'number', min: 40, max: 180, message: '脉搏范围: 40-180次/分', trigger: 'blur' }
  ],
  respiration: [
    { required: true, message: '请输入呼吸', trigger: 'blur' },
    { type: 'number', min: 10, max: 60, message: '呼吸范围: 10-60次/分', trigger: 'blur' }
  ],
  sysBp: [
    { required: true, message: '请输入收缩压', trigger: 'blur' },
    { type: 'number', min: 60, max: 250, message: '收缩压范围: 60-250mmHg', trigger: 'blur' }
  ],
  diaBp: [
    { required: true, message: '请输入舒张压', trigger: 'blur' },
    { type: 'number', min: 30, max: 150, message: '舒张压范围: 30-150mmHg', trigger: 'blur' }
  ],
  spo2: [
    { required: true, message: '请输入血氧', trigger: 'blur' },
    { type: 'number', min: 0, max: 100, message: '血氧范围: 0-100%', trigger: 'blur' }
  ]
};

// 监听对话框打开，初始化表单数据
watch(() => props.modelValue, async (newVal) => {
  if (newVal) {
    console.log('📋 NursingRecordForm 接收数据:', props.recordData);
    console.log('  - plannedStartTime:', props.recordData.plannedStartTime);
    console.log('  - scheduledTime:', props.recordData.scheduledTime);
    console.log('  - category:', props.recordData.category);
    console.log('  - taskType:', props.recordData.taskType);
    
    if (isViewMode.value) {
      // 查看模式：加载体征数据
      await loadVitalSignsData();
    } else {
      // 录入模式：重置表单
      resetForm();
      formData.value.taskId = props.recordData.id;
      formData.value.currentNurseId = props.currentNurseId;
    }
  }
});

// 重置表单
const resetForm = () => {
  formData.value = {
    taskId: 0,
    currentNurseId: '',
    executionTime: getChinaTime(),
    // 生命体征（必填）
    temperature: 36.5,
    tempType: '腋温',
    pulse: 80,
    respiration: 18,
    sysBp: 120,
    diaBp: 80,
    spo2: 98,
    painScore: 0,
    weight: null,
    intervention: '',
    // 护理笔记（可选）
    consciousness: '清醒',
    skinCondition: '完好',
    intakeVolume: null,
    outputVolume: null,
    noteContent: '',
    healthEducation: ''
  };
};

// 关闭对话框
const handleClose = () => {
  visible.value = false;
  if (!isViewMode.value && formRef.value) {
    formRef.value.resetFields();
  }
};

// 提交表单
const handleSubmit = async () => {
  if (!formRef.value) return;
  
  try {
    await formRef.value.validate();
    submitting.value = true;
    
    // 构造提交数据，确保字段名与后端DTO匹配
    const submitData = {
      taskId: formData.value.taskId,
      currentNurseId: formData.value.currentNurseId,
      executionTime: formData.value.executionTime,
      // 生命体征
      temperature: parseFloat(formData.value.temperature),
      tempType: formData.value.tempType,
      pulse: parseInt(formData.value.pulse),
      respiration: parseInt(formData.value.respiration),
      sysBp: parseInt(formData.value.sysBp),
      diaBp: parseInt(formData.value.diaBp),
      spo2: parseFloat(formData.value.spo2),
      painScore: parseInt(formData.value.painScore),
      weight: formData.value.weight ? parseFloat(formData.value.weight) : null,
      intervention: formData.value.intervention || '',
      // 护理笔记（可选）
      consciousness: formData.value.consciousness || null,
      pupilLeft: null,  // 暂未实现
      pupilRight: null, // 暂未实现
      skinCondition: formData.value.skinCondition || null,
      pipeCareData: null, // 暂未实现
      intakeVolume: formData.value.intakeVolume ? parseFloat(formData.value.intakeVolume) : null,
      intakeType: formData.value.intakeVolume ? '口服+输液' : null,
      outputVolume: formData.value.outputVolume ? parseFloat(formData.value.outputVolume) : null,
      outputType: formData.value.outputVolume ? '尿液+引流' : null,
      noteContent: formData.value.noteContent || null,
      healthEducation: formData.value.healthEducation || null
    };
    
    console.log('📋 提交数据详情:');
    console.log('  TaskId:', submitData.taskId, typeof submitData.taskId);
    console.log('  CurrentNurseId:', submitData.currentNurseId, typeof submitData.currentNurseId);
    console.log('  ExecutionTime:', submitData.executionTime, typeof submitData.executionTime);
    console.log('  Temperature:', submitData.temperature, typeof submitData.temperature);
    console.log('  Pulse:', submitData.pulse, typeof submitData.pulse);
    console.log('  完整数据:', JSON.stringify(submitData, null, 2));
    
    // 触发提交事件
    emit('submit-success', submitData);
    
  } catch (error) {
    console.error('表单验证失败:', error);
    ElMessage.warning('请完整填写必填项');
  } finally {
    submitting.value = false;
  }
};

// 格式化日期时间
const formatDateTime = (datetime) => {
  if (!datetime) return '';
  const date = new Date(datetime);
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  const hours = String(date.getHours()).padStart(2, '0');
  const minutes = String(date.getMinutes()).padStart(2, '0');
  return `${year}-${month}-${day} ${hours}:${minutes}`;
};
</script>

<style scoped>
.info-text {
  color: #606266;
  font-size: 14px;
}

.view-mode {
  padding: 10px 0;
  max-height: 70vh;
  overflow-y: auto;
}

.mt-20 {
  margin-top: 20px;
}

.note-content {
  padding: 12px 16px;
  background: #f5f7fa;
  border-radius: 4px;
  line-height: 1.8;
  color: #606266;
  white-space: pre-wrap;
  min-height: 40px;
}

.sub-info {
  color: #909399;
  font-size: 12px;
  margin-left: 4px;
}

.dialog-footer {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
}

/* 自定义 descriptions 样式 */
:deep(.el-descriptions__title) {
  font-size: 16px;
  font-weight: 600;
  color: #303133;
  margin-bottom: 12px;
}

:deep(.el-descriptions__label) {
  font-weight: 500;
}
</style>
