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
            <span class="info-text">{{ formatDateTime(recordData.scheduledTime) }}</span>
          </el-form-item>
        </el-col>
        <el-col :span="12">
          <el-form-item label="任务类型">
            <el-tag :type="recordData.taskType === 'Routine' ? 'primary' : 'warning'">
              {{ recordData.taskType === 'Routine' ? '常规测量' : '复测' }}
            </el-tag>
          </el-form-item>
        </el-col>
      </el-row>

      <el-form-item label="录入时间" prop="executionTime">
        <el-date-picker
          v-model="formData.executionTime"
          type="datetime"
          placeholder="选择录入时间"
          format="YYYY-MM-DD HH:mm"
          value-format="YYYY-MM-DD HH:mm:ss"
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
          <el-form-item label="体温 (℃)" prop="temperature">
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
          <el-form-item label="测温方式" prop="tempType">
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
          <el-form-item label="脉搏 (次/分)" prop="pulse">
            <el-input-number
              v-model="formData.pulse"
              :min="40"
              :max="180"
              style="width: 100%"
            />
          </el-form-item>
        </el-col>
        <el-col :span="12">
          <el-form-item label="呼吸 (次/分)" prop="respiration">
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
          <el-form-item label="收缩压 (mmHg)" prop="sysBp">
            <el-input-number
              v-model="formData.sysBp"
              :min="60"
              :max="250"
              style="width: 100%"
            />
          </el-form-item>
        </el-col>
        <el-col :span="12">
          <el-form-item label="舒张压 (mmHg)" prop="diaBp">
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
          <el-form-item label="血氧 (%)" prop="spo2">
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

      <el-form-item label="笔记内容">
        <el-input
          v-model="formData.noteContent"
          type="textarea"
          :rows="4"
          placeholder="记录护理观察、特殊情况等..."
          maxlength="500"
          show-word-limit
        />
      </el-form-item>
    </el-form>

    <!-- 查看模式 -->
    <div v-else class="view-mode">
      <!-- 任务信息 -->
      <el-descriptions title="任务信息" :column="2" border>
        <el-descriptions-item label="计划时间">
          {{ formatDateTime(recordData.scheduledTime) }}
        </el-descriptions-item>
        <el-descriptions-item label="任务类型">
          <el-tag :type="recordData.taskType === 'Routine' ? 'primary' : 'warning'">
            {{ recordData.taskType === 'Routine' ? '常规测量' : '复测' }}
          </el-tag>
        </el-descriptions-item>
        <el-descriptions-item label="录入时间">
          {{ formatDateTime(recordData.executeTime) }}
        </el-descriptions-item>
        <el-descriptions-item label="录入护士">
          {{ recordData.executorNurse || '未知' }}
        </el-descriptions-item>
      </el-descriptions>

      <!-- 生命体征 -->
      <el-descriptions title="生命体征" :column="2" border class="mt-20">
        <el-descriptions-item label="体温">
          {{ vitalSignsData.temperature }}℃ ({{ vitalSignsData.tempType }})
        </el-descriptions-item>
        <el-descriptions-item label="脉搏">
          {{ vitalSignsData.pulse }} 次/分
        </el-descriptions-item>
        <el-descriptions-item label="呼吸">
          {{ vitalSignsData.respiration }} 次/分
        </el-descriptions-item>
        <el-descriptions-item label="血压">
          {{ vitalSignsData.sysBp }}/{{ vitalSignsData.diaBp }} mmHg
        </el-descriptions-item>
        <el-descriptions-item label="血氧">
          {{ vitalSignsData.spo2 }}%
        </el-descriptions-item>
        <el-descriptions-item label="疼痛评分">
          {{ vitalSignsData.painScore }} 分
        </el-descriptions-item>
      </el-descriptions>

      <!-- 护理笔记 -->
      <div v-if="vitalSignsData.noteContent" class="mt-20">
        <el-divider content-position="left">护理笔记</el-divider>
        <div class="note-content">{{ vitalSignsData.noteContent }}</div>
      </div>
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

// 表单数据
const formData = ref({
  taskId: 0,
  currentNurseId: '',
  executionTime: new Date().toISOString().slice(0, 19).replace('T', ' '),
  temperature: 36.5,
  tempType: '腋温',
  pulse: 80,
  respiration: 18,
  sysBp: 120,
  diaBp: 80,
  spo2: 98,
  painScore: 0,
  noteContent: ''
});

// 查看模式的体征数据（从recordData解析）
const vitalSignsData = computed(() => {
  if (!props.recordData.dataPayload) return {};
  try {
    return JSON.parse(props.recordData.dataPayload);
  } catch {
    return {};
  }
});

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
watch(() => props.modelValue, (newVal) => {
  if (newVal && !isViewMode.value) {
    resetForm();
    formData.value.taskId = props.recordData.id;
    formData.value.currentNurseId = props.currentNurseId;
  }
});

// 重置表单
const resetForm = () => {
  formData.value = {
    taskId: 0,
    currentNurseId: '',
    executionTime: new Date().toISOString().slice(0, 19).replace('T', ' '),
    temperature: 36.5,
    tempType: '腋温',
    pulse: 80,
    respiration: 18,
    sysBp: 120,
    diaBp: 80,
    spo2: 98,
    painScore: 0,
    noteContent: ''
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
    
    // 触发提交事件
    emit('submit-success', formData.value);
    
  } catch (error) {
    ElMessage.warning('请完整填写必填项');
  } finally {
    submitting.value = false;
  }
};

// 格式化日期时间
const formatDateTime = (datetime) => {
  if (!datetime) return '';
  const date = new Date(datetime);
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
.info-text {
  color: #606266;
  font-size: 14px;
}

.view-mode {
  padding: 10px 0;
}

.mt-20 {
  margin-top: 20px;
}

.note-content {
  padding: 16px;
  background: #f5f7fa;
  border-radius: 4px;
  line-height: 1.8;
  color: #606266;
  white-space: pre-wrap;
}

.dialog-footer {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
}
</style>
