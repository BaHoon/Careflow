<template>
  <el-dialog
    v-model="visible"
    title="🔬 检查导引单"
    width="800px"
    :close-on-click-modal="false"
    class="inspection-guide-dialog"
    @close="handleClose"
  >
    <div v-if="loading" class="loading-state">
      <el-icon class="is-loading"><Loading /></el-icon>
      <p>加载导引单信息中...</p>
    </div>

    <div v-else-if="guideData" class="guide-container" ref="printArea">
      <!-- 医院标题 -->
      <div class="guide-header">
        <h1>{{ hospitalName }}</h1>
        <h2>检查导引单</h2>
      </div>

      <!-- 患者信息区域 -->
      <div class="section patient-section">
        <div class="section-title">患者信息</div>
        <div class="info-grid">
          <div class="info-item">
            <span class="label">姓名:</span>
            <span class="value">{{ guideData.patientName }}</span>
          </div>
          <div class="info-item">
            <span class="label">患者ID:</span>
            <span class="value">{{ guideData.patientId }}</span>
          </div>
          <div class="info-item">
            <span class="label">性别:</span>
            <span class="value">{{ guideData.gender || '-' }}</span>
          </div>
          <div class="info-item">
            <span class="label">年龄:</span>
            <span class="value">{{ guideData.age || '-' }}</span>
          </div>
          <div class="info-item">
            <span class="label">床号:</span>
            <span class="value">{{ guideData.bedNumber || '-' }}</span>
          </div>
          <div class="info-item">
            <span class="label">科室:</span>
            <span class="value">{{ guideData.department || '-' }}</span>
          </div>
        </div>
      </div>

      <!-- 检查信息区域 -->
      <div class="section inspection-section">
        <div class="section-title">检查信息</div>
        <div class="info-grid">
          <div class="info-item full-width">
            <span class="label">检查项目:</span>
            <span class="value highlight">{{ guideData.itemName }}</span>
          </div>
          <div class="info-item">
            <span class="label">申请单号:</span>
            <span class="value">{{ guideData.risLisId }}</span>
          </div>
          <div class="info-item">
            <span class="label">检查地点:</span>
            <span class="value">{{ guideData.location }}</span>
          </div>
          <div v-if="guideData.appointmentTime" class="info-item">
            <span class="label">预约时间:</span>
            <span class="value highlight">{{ formatDateTime(guideData.appointmentTime) }}</span>
          </div>
          <div v-if="guideData.appointmentPlace" class="info-item">
            <span class="label">预约地点:</span>
            <span class="value">{{ guideData.appointmentPlace }}</span>
          </div>
          <div class="info-item">
            <span class="label">开单医生:</span>
            <span class="value">{{ guideData.doctorName }}</span>
          </div>
          <div class="info-item">
            <span class="label">开单时间:</span>
            <span class="value">{{ formatDateTime(guideData.createTime) }}</span>
          </div>
        </div>
      </div>

      <!-- 注意事项区域 -->
      <div v-if="guideData.precautions" class="section precautions-section">
        <div class="section-title">⚠️ 注意事项</div>
        <div class="precautions-content">
          {{ guideData.precautions }}
        </div>
      </div>

      <!-- 条形码区域 -->
      <div class="section barcode-section">
        <div class="section-title">任务条形码</div>
        <div v-if="barcodeImage" class="barcode-display">
          <img :src="barcodeImage" alt="任务条形码" class="barcode-image" />
          <div class="barcode-label">任务ID: {{ guideData.taskId }}</div>
        </div>
        <div v-else class="barcode-loading">
          <el-icon class="is-loading"><Loading /></el-icon>
          <span>生成条形码中...</span>
        </div>
      </div>

      <!-- 底部说明 -->
      <div class="guide-footer">
        <p>请持此导引单前往检查地点，工作人员将扫描条形码确认身份。</p>
        <p class="print-time">打印时间: {{ currentDateTime }}</p>
      </div>
    </div>

    <template #footer>
      <div class="dialog-footer">
        <el-button @click="handleClose">关闭</el-button>
        <el-button type="primary" @click="handlePrint" :icon="Printer">打印</el-button>
      </div>
    </template>
  </el-dialog>
</template>

<script setup>
import { ref, computed, watch } from 'vue';
import { ElMessage } from 'element-plus';
import { Loading, Printer } from '@element-plus/icons-vue';
import { getOrderDetail } from '@/api/nurseOrder';

// ==================== Props ====================
const props = defineProps({
  modelValue: {
    type: Boolean,
    default: false
  },
  orderId: {
    type: [String, Number],
    default: null
  },
  taskId: {
    type: [String, Number],
    default: null
  },
  nurseId: {
    type: String,
    default: null
  }
});

// ==================== Emits ====================
const emit = defineEmits(['update:modelValue', 'printSuccess']);

// ==================== 数据 ====================
const visible = computed({
  get: () => props.modelValue,
  set: (val) => emit('update:modelValue', val)
});

const loading = ref(false);
const guideData = ref(null);
const barcodeImage = ref('');
const printArea = ref(null);
const hospitalName = ref('CareFlow 智慧医院系统');

const currentDateTime = computed(() => {
  return new Date().toLocaleString('zh-CN', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
    timeZone: 'Asia/Shanghai'
  });
});

// ==================== 方法 ====================
/**
 * 加载导引单数据
 */
const loadGuideData = async () => {
  if (!props.orderId || !props.taskId) {
    console.warn('⚠️ orderId 或 taskId 为空，无法加载导引单');
    return;
  }

  loading.value = true;
  try {
    // 获取医嘱详情
    const orderDetail = await getOrderDetail(props.orderId);
    
    // 查找对应的任务
    const task = orderDetail.tasks?.find(t => t.id.toString() === props.taskId.toString());
    if (!task) {
      throw new Error('未找到对应的任务');
    }

    // 组装导引单数据
    guideData.value = {
      // 患者信息
      patientId: orderDetail.patientId,
      patientName: orderDetail.patientName,
      gender: orderDetail.patientGender,
      age: orderDetail.patientAge,
      bedNumber: orderDetail.bedNumber,
      department: orderDetail.department,
      
      // 检查信息
      itemName: orderDetail.itemName,
      itemCode: orderDetail.itemCode,
      risLisId: orderDetail.risLisId,
      location: orderDetail.location,
      appointmentTime: orderDetail.appointmentTime,
      appointmentPlace: orderDetail.appointmentPlace,
      precautions: orderDetail.precautions,
      
      // 医嘱信息
      doctorName: orderDetail.doctorName,
      createTime: orderDetail.createTime,
      
      // 任务信息
      taskId: task.id,
      plannedStartTime: task.plannedStartTime
    };

    // 生成条形码
    await generateBarcode();
    
    console.log('✅ 导引单数据加载成功:', guideData.value);
  } catch (error) {
    console.error('❌ 加载导引单数据失败:', error);
    ElMessage.error('加载导引单失败: ' + error.message);
    guideData.value = null;
  } finally {
    loading.value = false;
  }
};

/**
 * 生成条形码
 */
const generateBarcode = async () => {
  try {
    const response = await fetch(
      `http://localhost:5181/api/BarcodePrint/generate-task-barcode?taskId=${props.taskId}`
    );
    const result = await response.json();
    
    if (result.success && result.data) {
      barcodeImage.value = result.data.barcodeBase64;
      console.log('✅ 条形码生成成功');
    } else {
      throw new Error(result.message || '生成条形码失败');
    }
  } catch (error) {
    console.error('❌ 生成条形码失败:', error);
    ElMessage.warning('条形码生成失败: ' + error.message);
    barcodeImage.value = '';
  }
};

/**
 * 格式化日期时间
 */
const formatDateTime = (dateString) => {
  if (!dateString) return '-';
  try {
    let utcString = dateString;
    if (!dateString.endsWith('Z') && !dateString.includes('+')) {
      utcString = dateString + 'Z';
    }
    const date = new Date(utcString);
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

/**
 * 打印导引单
 */
const handlePrint = async () => {
  if (!guideData.value) {
    ElMessage.warning('导引单数据未加载');
    return;
  }

  try {
    // 从localStorage获取当前登录用户信息
    const userInfoStr = localStorage.getItem('userInfo');
    let nurseId = null;
    
    if (userInfoStr) {
      try {
        const userInfo = JSON.parse(userInfoStr);
        nurseId = userInfo.staffId; // 使用staffId字段
      } catch (e) {
        console.warn('解析用户信息失败:', e);
      }
    }
    
    // 如果没有获取到nurseId，尝试使用props中的nurseId
    if (!nurseId && props.nurseId) {
      nurseId = props.nurseId;
    }
    
    // 如果还是没有nurseId，给出明确的错误提示
    if (!nurseId) {
      ElMessage.error('无法获取护士信息，请重新登录');
      return;
    }
    
    // 调用API完成任务
    const response = await fetch(
      `http://localhost:5181/api/Nursing/execution-tasks/${props.taskId}/complete`,
      {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({
          nurseId: nurseId
        })
      }
    );
    
    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(`HTTP ${response.status}: ${errorText}`);
    }
    
    const result = await response.json();
    
    ElMessage.success('导引单已打印，任务已完成');
    // 通知父组件刷新任务列表
    emit('printSuccess');
    // 关闭对话框
    visible.value = false;
  } catch (error) {
    console.error('❌ 完成任务失败:', error);
    ElMessage.error('完成任务失败: ' + error.message);
  }
};

/**
 * 关闭弹窗
 */
const handleClose = () => {
  visible.value = false;
  guideData.value = null;
  barcodeImage.value = '';
};

// ==================== 监听 ====================
watch(() => props.modelValue, (newVal) => {
  if (newVal) {
    loadGuideData();
  }
});
</script>

<style scoped>
.inspection-guide-dialog :deep(.el-dialog__body) {
  padding: 20px;
  max-height: 70vh;
  overflow-y: auto;
}

.loading-state {
  text-align: center;
  padding: 60px 20px;
  color: #909399;
}

.loading-state .el-icon {
  font-size: 40px;
  margin-bottom: 15px;
}

.loading-state p {
  font-size: 14px;
}

.guide-container {
  background: #fff;
}

.guide-header {
  text-align: center;
  margin-bottom: 25px;
  padding-bottom: 15px;
  border-bottom: 3px solid #409eff;
}

.guide-header h1 {
  font-size: 22px;
  color: #303133;
  margin-bottom: 8px;
}

.guide-header h2 {
  font-size: 18px;
  color: #606266;
}

.section {
  margin-bottom: 20px;
  border: 1px solid #dcdfe6;
  border-radius: 4px;
  padding: 15px;
  background: #fafafa;
}

.section-title {
  font-size: 15px;
  font-weight: bold;
  color: #409eff;
  margin-bottom: 12px;
  padding-bottom: 8px;
  border-bottom: 2px solid #409eff;
}

.info-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 10px;
}

.info-item {
  display: flex;
  padding: 5px 0;
}

.info-item.full-width {
  grid-column: 1 / -1;
}

.label {
  font-weight: bold;
  color: #606266;
  min-width: 85px;
  flex-shrink: 0;
}

.value {
  color: #303133;
  flex: 1;
}

.value.highlight {
  color: #409eff;
  font-weight: bold;
  font-size: 15px;
}

.precautions-section {
  background: #fff9f0;
  border-color: #ff9800;
}

.precautions-content {
  background: #fff3e0;
  padding: 12px;
  border-left: 4px solid #ff9800;
  color: #e65100;
  line-height: 1.8;
  white-space: pre-line;
}

.barcode-section {
  text-align: center;
  background: #f0f9ff;
  border-color: #409eff;
}

.barcode-display {
  padding: 20px;
}

.barcode-image {
  max-width: 100%;
  height: auto;
  margin-bottom: 10px;
}

.barcode-label {
  font-size: 13px;
  color: #606266;
  font-weight: 600;
}

.barcode-loading {
  padding: 30px;
  color: #909399;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 10px;
}

.guide-footer {
  margin-top: 25px;
  padding-top: 15px;
  border-top: 1px dashed #dcdfe6;
  text-align: center;
  color: #606266;
}

.guide-footer p {
  margin: 6px 0;
  font-size: 13px;
}

.print-time {
  font-size: 12px;
  color: #909399;
  font-style: italic;
}

.dialog-footer {
  display: flex;
  justify-content: flex-end;
  gap: 10px;
}
</style>
