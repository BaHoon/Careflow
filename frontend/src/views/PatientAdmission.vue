<template>
  <div class="patient-admission">
    <div class="page-header">
      <h2>患者入院办理</h2>
    </div>

    <div class="admission-container">
      <!-- 步骤指示器 -->
      <div class="step-indicator">
        <div v-for="(step, index) in steps" :key="index" class="step">
          <div :class="['step-circle', { active: currentStep === index, completed: currentStep > index }]">
            {{ index + 1 }}
          </div>
          <div class="step-label">{{ step }}</div>
        </div>
      </div>

      <!-- 步骤内容 -->
      <div class="step-content">
        <!-- 步骤1：上传患者条形码 -->
        <div v-if="currentStep === 0" class="step-panel">
          <h3>📷 上传患者条形码</h3>
          <p class="step-desc">请拍摄或上传患者条形码，系统将自动识别并填充患者信息</p>
          
          <div class="upload-box">
            <input 
              ref="barcodeInput" 
              type="file" 
              accept="image/*" 
              @change="handleBarcodeUpload" 
              style="display:none" 
            />
            <div class="upload-area" @click="$refs.barcodeInput?.click()">
              <div style="font-size: 3rem">📷</div>
              <div>点击上传或拍摄患者条形码</div>
              <small>支持 JPG、PNG、BMP</small>
            </div>
            <img v-if="barcodePreview" :src="barcodePreview" class="preview" />
          </div>

          <div v-if="recognizedPatientId" class="recognition-result">
            <el-alert
              :title="`识别成功：患者ID ${recognizedPatientId}`"
              type="success"
              :closable="false"
              show-icon
            />
          </div>

          <div v-if="errorMessage" class="error-message">
            <el-alert
              :title="errorMessage"
              type="error"
              :closable="false"
              show-icon
            />
          </div>
        </div>

        <!-- 步骤2：填写入院信息 -->
        <div v-else-if="currentStep === 1" class="step-panel">
          <h3>📋 填写入院信息</h3>
          <p class="step-desc">请确认患者信息并选择床位等必要信息</p>

          <el-form
            ref="admissionFormRef"
            :model="admissionForm"
            :rules="admissionFormRules"
            label-width="120px"
            class="admission-form"
          >
            <!-- 基本信息（只读） -->
            <el-card shadow="never" class="info-card">
              <template #header>
                <span>基本信息</span>
              </template>
              <el-row :gutter="20">
                <el-col :span="12">
                  <el-form-item label="患者ID">
                    <el-input v-model="patientInfo.patientId" disabled />
                  </el-form-item>
                </el-col>
                <el-col :span="12">
                  <el-form-item label="姓名">
                    <el-input v-model="patientInfo.name" disabled />
                  </el-form-item>
                </el-col>
                <el-col :span="12">
                  <el-form-item label="性别">
                    <el-input v-model="patientInfo.gender" disabled />
                  </el-form-item>
                </el-col>
                <el-col :span="12">
                  <el-form-item label="年龄">
                    <el-input v-model="patientInfo.age" disabled />
                  </el-form-item>
                </el-col>
                <el-col :span="12">
                  <el-form-item label="身份证号">
                    <el-input v-model="patientInfo.idCard" disabled />
                  </el-form-item>
                </el-col>
                <el-col :span="12">
                  <el-form-item label="出生日期">
                    <el-input :value="formatDate(patientInfo.dateOfBirth)" disabled />
                  </el-form-item>
                </el-col>
              </el-row>
            </el-card>

            <!-- 可编辑信息 -->
            <el-card shadow="never" class="info-card" style="margin-top: 20px">
              <template #header>
                <span>入院信息</span>
              </template>
              <el-row :gutter="20">
                <el-col :span="12">
                  <el-form-item label="床位" prop="bedId">
                    <el-select
                      v-model="admissionForm.bedId"
                      placeholder="请选择床位（必填）"
                      filterable
                      style="width: 100%"
                      @change="handleBedChange"
                      :loading="loadingBeds"
                      clearable
                    >
                      <el-option-group
                        v-for="wardGroup in bedGroups"
                        :key="wardGroup.wardId"
                        :label="`${wardGroup.wardName} (${wardGroup.departmentName}) - ${wardGroup.beds.length}个空床位`"
                      >
                        <el-option
                          v-for="bed in wardGroup.beds"
                          :key="bed.bedId"
                          :label="bed.bedId"
                          :value="bed.bedId"
                        >
                          <span style="font-weight: 500">{{ bed.bedId }}</span>
                          <span style="float: right; color: #909399; font-size: 12px; margin-left: 10px">
                            {{ bed.wardName }} - {{ bed.departmentName }}
                          </span>
                        </el-option>
                      </el-option-group>
                      <el-option
                        v-if="availableBeds.length === 0 && !loadingBeds"
                        disabled
                        label="暂无可用床位"
                        value=""
                      />
                    </el-select>
                    <div v-if="availableBeds.length > 0" style="margin-top: 5px; font-size: 12px; color: #909399">
                      共 {{ availableBeds.length }} 个可用床位
                    </div>
                  </el-form-item>
                </el-col>
                <el-col :span="12">
                  <el-form-item label="护理级别" prop="nursingGrade">
                    <el-select v-model="admissionForm.nursingGrade" placeholder="请选择护理级别" style="width: 100%">
                      <el-option label="特级护理" :value="0" />
                      <el-option label="一级护理" :value="1" />
                      <el-option label="二级护理" :value="2" />
                      <el-option label="三级护理" :value="3" />
                    </el-select>
                  </el-form-item>
                </el-col>
                <el-col :span="24">
                  <el-form-item label="门诊诊断" prop="outpatientDiagnosis">
                    <el-input
                      v-model="admissionForm.outpatientDiagnosis"
                      type="textarea"
                      :rows="3"
                      placeholder="请输入门诊诊断"
                    />
                  </el-form-item>
                </el-col>
                <el-col :span="12">
                  <el-form-item label="实际入院时间" prop="actualAdmissionTime">
                    <el-date-picker
                      v-model="admissionForm.actualAdmissionTime"
                      type="datetime"
                      placeholder="选择入院时间（留空则使用当前时间）"
                      style="width: 100%"
                      format="YYYY-MM-DD HH:mm:ss"
                      value-format="YYYY-MM-DDTHH:mm:ss"
                      clearable
                    />
                  </el-form-item>
                </el-col>
                <el-col :span="12">
                  <el-form-item label="身高 (cm)">
                    <el-input-number
                      v-model="patientInfo.height"
                      :min="0"
                      :max="300"
                      :precision="1"
                      style="width: 100%"
                      disabled
                    />
                  </el-form-item>
                </el-col>
                <el-col :span="12">
                  <el-form-item label="体重 (kg)">
                    <el-input-number
                      v-model="patientInfo.weight"
                      :min="0"
                      :max="500"
                      :precision="1"
                      style="width: 100%"
                      disabled
                    />
                  </el-form-item>
                </el-col>
                <el-col :span="12">
                  <el-form-item label="联系电话">
                    <el-input v-model="patientInfo.phoneNumber" disabled />
                  </el-form-item>
                </el-col>
                <el-col :span="24">
                  <el-form-item label="主治医生">
                    <el-input :value="patientInfo.attendingDoctorName" disabled />
                  </el-form-item>
                </el-col>
              </el-row>
            </el-card>
          </el-form>
        </div>

        <!-- 步骤3：确认信息 -->
        <div v-else-if="currentStep === 2" class="step-panel">
          <h3>✓ 确认入院信息</h3>
          <p class="step-desc">请确认以下信息无误后提交</p>

          <el-card shadow="never" class="confirm-card">
            <el-descriptions title="患者信息" :column="2" border>
              <el-descriptions-item label="患者ID">{{ patientInfo.patientId }}</el-descriptions-item>
              <el-descriptions-item label="姓名">{{ patientInfo.name }}</el-descriptions-item>
              <el-descriptions-item label="性别">{{ patientInfo.gender }}</el-descriptions-item>
              <el-descriptions-item label="年龄">{{ patientInfo.age }}岁</el-descriptions-item>
              <el-descriptions-item label="床位">
                {{ getSelectedBedLabel() }}
              </el-descriptions-item>
              <el-descriptions-item label="护理级别">
                {{ getNursingGradeText(admissionForm.nursingGrade) }}
              </el-descriptions-item>
              <el-descriptions-item label="门诊诊断" :span="2">
                {{ admissionForm.outpatientDiagnosis || '无' }}
              </el-descriptions-item>
              <el-descriptions-item label="实际入院时间" :span="2">
                {{ admissionForm.actualAdmissionTime ? formatDateTime(admissionForm.actualAdmissionTime) : formatDateTime(new Date().toISOString()) }}
              </el-descriptions-item>
            </el-descriptions>
          </el-card>
        </div>
      </div>

      <!-- 操作按钮 -->
      <div class="action-btns">
        <el-button v-if="currentStep > 0" @click="goBack">← 返回</el-button>
        <el-button 
          v-if="currentStep === 1" 
          type="primary" 
          @click="nextStep"
          :loading="loading"
        >
          下一步 →
        </el-button>
        <el-button 
          v-if="currentStep === 2" 
          type="success" 
          @click="submitAdmission"
          :loading="submitting"
        >
          确认办理入院 ✓
        </el-button>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { ElMessage, ElMessageBox } from 'element-plus';
import {
  recognizePatientBarcode,
  getPendingAdmissionPatient,
  getAvailableBeds,
  processPatientAdmission,
  getNursingGradeText
} from '@/api/patient';

const router = useRouter();
const route = useRoute();

// 步骤
const steps = ['上传患者码', '填写入院信息', '确认信息'];
const currentStep = ref(0);

// 条形码上传
const barcodeInput = ref(null);
const barcodePreview = ref('');
let barcodeFile = null;
const recognizedPatientId = ref('');
const errorMessage = ref('');

// 患者信息
const patientInfo = ref({
  patientId: '',
  name: '',
  gender: '',
  idCard: '',
  dateOfBirth: null,
  age: 0,
  height: 0,
  weight: 0,
  phoneNumber: '',
  outpatientDiagnosis: '',
  scheduledAdmissionTime: null,
  nursingGrade: 0,
  attendingDoctorId: '',
  attendingDoctorName: ''
});

// 入院表单
const admissionFormRef = ref(null);
const admissionForm = ref({
  bedId: '',
  nursingGrade: 2, // 默认二级护理
  outpatientDiagnosis: '',
  actualAdmissionTime: null // 默认当前时间，由后端处理
});

// 表单验证规则
const admissionFormRules = {
  bedId: [
    { required: true, message: '请选择床位', trigger: 'change' }
  ],
  nursingGrade: [
    { required: true, message: '请选择护理级别', trigger: 'change' }
  ]
};

// 可用床位列表
const availableBeds = ref([]);
const loading = ref(false);
const loadingBeds = ref(false);
const submitting = ref(false);

// 按病区分组的床位列表
const bedGroups = computed(() => {
  if (!availableBeds.value || availableBeds.value.length === 0) {
    return [];
  }
  
  // 按病区ID分组
  const groups = new Map();
  
  availableBeds.value.forEach(bed => {
    const key = bed.wardId || 'unknown';
    if (!groups.has(key)) {
      groups.set(key, {
        wardId: bed.wardId,
        wardName: bed.wardName,
        departmentId: bed.departmentId,
        departmentName: bed.departmentName,
        beds: []
      });
    }
    groups.get(key).beds.push(bed);
  });
  
  // 转换为数组并排序
  return Array.from(groups.values()).sort((a, b) => {
    // 先按科室名称排序，再按病区名称排序
    if (a.departmentName !== b.departmentName) {
      return a.departmentName.localeCompare(b.departmentName);
    }
    return a.wardName.localeCompare(b.wardName);
  });
});

// 获取当前登录用户信息
const getCurrentNurse = () => {
  try {
    const userInfoStr = localStorage.getItem('userInfo');
    if (userInfoStr) {
      return JSON.parse(userInfoStr);
    }
  } catch (error) {
    console.error('解析用户信息失败:', error);
  }
  return null;
};

// 处理条形码上传
const handleBarcodeUpload = async (e) => {
  const file = e.target.files?.[0];
  if (!file) return;

  try {
    errorMessage.value = '';
    const msg = ElMessage.info({ message: '识别条形码中...', duration: 0 });
    barcodeFile = file;

    // 显示预览
    const reader = new FileReader();
    reader.onload = r => barcodePreview.value = r.target?.result;
    reader.readAsDataURL(file);

    // 调用后端识别条形码
    const result = await recognizePatientBarcode(file);
    msg.close();

    if (result.success && result.patientId) {
      recognizedPatientId.value = result.patientId;
      
      // 获取患者信息
      await loadPatientInfo(result.patientId);
      
      ElMessage.success('条形码识别成功，患者信息已加载');
      
      // 自动进入下一步
      setTimeout(() => {
        nextStep();
      }, 1000);
    } else {
      errorMessage.value = result.message || '识别失败';
      ElMessage.error(result.message || '识别失败');
    }
  } catch (err) {
    errorMessage.value = err.response?.data?.message || err.message || '识别失败';
    ElMessage.error('识别失败: ' + (err.response?.data?.message || err.message));
  }
};

// 加载患者信息
const loadPatientInfo = async (patientId) => {
  try {
    loading.value = true;
    const info = await getPendingAdmissionPatient(patientId);
    
    // 填充患者信息
    patientInfo.value = {
      patientId: info.patientId,
      name: info.name,
      gender: info.gender,
      idCard: info.idCard,
      dateOfBirth: info.dateOfBirth,
      age: info.age,
      height: info.height,
      weight: info.weight,
      phoneNumber: info.phoneNumber,
      outpatientDiagnosis: info.outpatientDiagnosis || '',
      scheduledAdmissionTime: info.scheduledAdmissionTime,
      nursingGrade: info.nursingGrade,
      attendingDoctorId: info.attendingDoctorId,
      attendingDoctorName: info.attendingDoctorName
    };
    
    // 填充入院表单
    admissionForm.value.nursingGrade = info.nursingGrade;
    admissionForm.value.outpatientDiagnosis = info.outpatientDiagnosis || '';
    
    // 加载可用床位
    await loadAvailableBeds();
  } catch (err) {
    ElMessage.error('加载患者信息失败: ' + (err.response?.data?.message || err.message));
    throw err;
  } finally {
    loading.value = false;
  }
};

// 加载可用床位（不限制科室，显示所有空床位）
const loadAvailableBeds = async () => {
  try {
    loadingBeds.value = true;
    
    // 不传任何参数，获取所有空床位
    const beds = await getAvailableBeds({});
    availableBeds.value = beds || [];
    
    if (availableBeds.value.length === 0) {
      ElMessage.warning('当前没有可用床位，请联系管理员');
    } else {
      console.log(`成功加载 ${availableBeds.value.length} 个可用床位`);
    }
  } catch (err) {
    console.error('加载可用床位失败:', err);
    ElMessage.error('加载可用床位失败: ' + (err.response?.data?.message || err.message));
    availableBeds.value = [];
  } finally {
    loadingBeds.value = false;
  }
};

// 床位选择变化
const handleBedChange = (bedId) => {
  if (bedId) {
    const selectedBed = availableBeds.value.find(b => b.bedId === bedId);
    if (selectedBed) {
      console.log('选择床位:', {
        bedId: selectedBed.bedId,
        wardName: selectedBed.wardName,
        departmentName: selectedBed.departmentName
      });
    }
  }
};

// 获取选中床位的显示标签
const getSelectedBedLabel = () => {
  const bed = availableBeds.value.find(b => b.bedId === admissionForm.value.bedId);
  if (bed) {
    return `${bed.bedId} - ${bed.wardName} - ${bed.departmentName}`;
  }
  return admissionForm.value.bedId || '未选择';
};

// 下一步
const nextStep = async () => {
  if (currentStep.value === 1) {
    // 验证表单
    if (!admissionFormRef.value) return;
    
    try {
      await admissionFormRef.value.validate();
      currentStep.value = 2;
    } catch (err) {
      ElMessage.warning('请完善必填信息');
    }
  } else {
    currentStep.value++;
  }
};

// 返回上一步
const goBack = () => {
  if (currentStep.value > 0) {
    currentStep.value--;
  }
};

// 提交入院
const submitAdmission = async () => {
  try {
    await ElMessageBox.confirm(
      `确认为患者 ${patientInfo.value.name} (${patientInfo.value.patientId}) 办理入院？`,
      '确认办理入院',
      {
        confirmButtonText: '确认',
        cancelButtonText: '取消',
        type: 'warning'
      }
    );

    submitting.value = true;

    const nurseInfo = getCurrentNurse();
    if (!nurseInfo) {
      ElMessage.error('无法获取当前护士信息');
      return;
    }

    // 准备提交数据
    const admissionData = {
      patientId: patientInfo.value.patientId,
      bedId: admissionForm.value.bedId,
      actualAdmissionTime: admissionForm.value.actualAdmissionTime || new Date().toISOString(),
      nursingGrade: admissionForm.value.nursingGrade,
      outpatientDiagnosis: admissionForm.value.outpatientDiagnosis || null,
      operatorId: nurseInfo.staffId,
      operatorType: 'Nurse',
      remarks: `护士 ${nurseInfo.name} 于 ${new Date().toLocaleString()} 办理入院`
    };

    // 调用API
    await processPatientAdmission(admissionData);

    ElMessage.success('入院办理成功');

    // 返回护士工作台
    setTimeout(() => {
      router.push('/nurse/dashboard');
    }, 1500);
  } catch (err) {
    if (err !== 'cancel') {
      console.error('办理入院失败:', err);
      ElMessage.error('办理入院失败: ' + (err.response?.data?.message || err.message || '未知错误'));
    }
  } finally {
    submitting.value = false;
  }
};

// 格式化日期
const formatDate = (date) => {
  if (!date) return '';
  const d = new Date(date);
  return d.toLocaleDateString('zh-CN');
};

// 格式化日期时间
const formatDateTime = (dateTime) => {
  if (!dateTime) return '';
  const d = new Date(dateTime);
  return d.toLocaleString('zh-CN');
};

// 初始化：如果路由参数中有patientId，直接加载
onMounted(async () => {
  const patientId = route.query.patientId;
  if (patientId) {
    try {
      await loadPatientInfo(patientId);
      recognizedPatientId.value = patientId;
      currentStep.value = 1; // 直接进入第二步
    } catch (err) {
      console.error('加载患者信息失败:', err);
    }
  } else {
    // 加载可用床位（用于显示）
    await loadAvailableBeds();
  }
});
</script>

<style scoped>
.patient-admission {
  padding: 20px;
  max-width: 1200px;
  margin: 0 auto;
}

.page-header {
  margin-bottom: 30px;
}

.page-header h2 {
  margin: 0;
  font-size: 24px;
  color: #303133;
}

.admission-container {
  background: #fff;
  border-radius: 8px;
  padding: 30px;
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.1);
}

.step-indicator {
  display: flex;
  justify-content: space-between;
  margin-bottom: 40px;
  position: relative;
}

.step-indicator::before {
  content: '';
  position: absolute;
  top: 20px;
  left: 0;
  right: 0;
  height: 2px;
  background: #e4e7ed;
  z-index: 0;
}

.step {
  display: flex;
  flex-direction: column;
  align-items: center;
  position: relative;
  z-index: 1;
  flex: 1;
}

.step-circle {
  width: 40px;
  height: 40px;
  border-radius: 50%;
  background: #e4e7ed;
  color: #909399;
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: bold;
  margin-bottom: 10px;
  transition: all 0.3s;
}

.step-circle.active {
  background: #409eff;
  color: #fff;
}

.step-circle.completed {
  background: #67c23a;
  color: #fff;
}

.step-label {
  font-size: 14px;
  color: #606266;
}

.step-content {
  min-height: 400px;
  margin-bottom: 30px;
}

.step-panel h3 {
  margin-top: 0;
  margin-bottom: 10px;
  font-size: 20px;
  color: #303133;
}

.step-desc {
  color: #909399;
  margin-bottom: 30px;
}

.upload-box {
  margin: 30px 0;
}

.upload-area {
  border: 2px dashed #dcdfe6;
  border-radius: 8px;
  padding: 40px;
  text-align: center;
  cursor: pointer;
  transition: all 0.3s;
}

.upload-area:hover {
  border-color: #409eff;
  background: #f5f7fa;
}

.upload-area div {
  margin: 10px 0;
}

.upload-area small {
  color: #909399;
}

.preview {
  max-width: 100%;
  max-height: 300px;
  margin-top: 20px;
  border-radius: 8px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.recognition-result,
.error-message {
  margin-top: 20px;
}

.admission-form {
  margin-top: 20px;
}

.info-card {
  margin-bottom: 20px;
}

.confirm-card {
  margin-top: 20px;
}

.action-btns {
  display: flex;
  justify-content: space-between;
  padding-top: 20px;
  border-top: 1px solid #e4e7ed;
}
</style>

