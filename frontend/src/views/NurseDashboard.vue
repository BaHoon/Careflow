<template>
  <div class="nurse-dashboard">
    <!-- 统计信息卡片 -->
    <div class="dashboard-stats">
      <el-row :gutter="16">
        <el-col :span="6">
          <el-card shadow="hover">
            <div class="stat-card">
              <div class="stat-icon" style="background: #ecf5ff">
                <el-icon :size="32" color="#409eff"><House /></el-icon>
              </div>
              <div class="stat-content">
                <div class="stat-value">{{ overview.totalBeds }}</div>
                <div class="stat-label">总床位数</div>
              </div>
            </div>
          </el-card>
        </el-col>
        <el-col :span="6">
          <el-card shadow="hover">
            <div class="stat-card">
              <div class="stat-icon" style="background: #fef0f0">
                <el-icon :size="32" color="#f56c6c"><User /></el-icon>
              </div>
              <div class="stat-content">
                <div class="stat-value">{{ overview.occupiedBeds }}</div>
                <div class="stat-label">在院患者</div>
              </div>
            </div>
          </el-card>
        </el-col>
        <el-col :span="6">
          <el-card shadow="hover">
            <div class="stat-card">
              <div class="stat-icon" style="background: #f0f9ff">
                <el-icon :size="32" color="#67c23a"><CircleCheck /></el-icon>
              </div>
              <div class="stat-content">
                <div class="stat-value">{{ overview.availableBeds }}</div>
                <div class="stat-label">空闲床位</div>
              </div>
            </div>
          </el-card>
        </el-col>
        <el-col :span="6">
          <el-card shadow="hover">
            <div class="stat-card">
              <div class="stat-icon" style="background: #fdf6ec">
                <el-icon :size="32" color="#e6a23c"><DataAnalysis /></el-icon>
              </div>
              <div class="stat-content">
                <div class="stat-value">{{ bedOccupancyRate }}%</div>
                <div class="stat-label">床位使用率</div>
              </div>
            </div>
          </el-card>
        </el-col>
      </el-row>
    </div>

    <!-- 患者管理区域 -->
    <div class="patient-management-section">
      <el-divider content-position="left">
        <span style="font-size: 18px; font-weight: 600; color: #303133;">
          <el-icon style="vertical-align: middle;"><User /></el-icon>
          患者管理
        </span>
      </el-divider>

      <!-- 搜索和筛选工具栏 -->
      <div class="patient-filter-toolbar">
        <div class="filter-left">
          <!-- 病区筛选 -->
          <div class="filter-group">
            <span class="filter-label">病区:</span>
            <el-select 
              v-model="selectedWardId" 
              placeholder="全部病区" 
              clearable
              @change="handleWardFilterChange"
              size="default"
              class="ward-select"
            >
              <el-option label="全部病区" :value="''" />
              <el-option
                v-for="ward in availableWards"
                :key="ward.wardId"
                :label="ward.wardName"
                :value="ward.wardId"
              />
            </el-select>
          </div>

          <!-- 状态筛选 -->
          <div class="filter-group">
            <span class="filter-label">患者状态:</span>
            <el-select 
              v-model="patientFilterStatus" 
              placeholder="选择状态" 
              multiple
              collapse-tags
              collapse-tags-tooltip
              clearable
              @change="loadPatientData"
              size="default"
              class="status-select"
              style="width: 200px;"
            >
              <el-option label="在院" :value="1" />
              <el-option label="待出院" :value="2" />
            </el-select>
          </div>

          <!-- 搜索框 -->
          <div class="filter-group">
            <el-input
              v-model="patientSearchKeyword"
              placeholder="搜索患者ID / 身份证号 / 姓名"
              clearable
              @input="handlePatientSearch"
              size="default"
              class="search-input"
              style="width: 320px"
            >
              <template #prefix>
                <el-icon><Search /></el-icon>
              </template>
            </el-input>
          </div>
        </div>

        <div class="filter-right">
          <!-- 新增患者按钮 -->
          <el-button 
            type="primary" 
            :icon="Plus"
            @click="handleAddPatient"
            size="default"
          >
            新增患者入院
          </el-button>
        </div>
      </div>

      <!-- 患者卡片 - 按病区分组显示 -->
      <div class="patients-by-ward">
        <!-- 加载状态 -->
        <div v-if="loadingPatients" class="loading-state">
          <el-icon class="is-loading"><Loading /></el-icon>
          <p>加载中...</p>
        </div>

        <!-- 空状态 -->
        <div v-else-if="patientWardGroups.length === 0" class="empty-state">
          <div class="empty-icon">🏥</div>
          <p>暂无患者信息</p>
          <p class="empty-hint">点击右上角"新增患者入院"按钮添加患者</p>
        </div>

        <!-- 按病区分组显示患者 -->
        <div v-else>
          <el-card 
            v-for="wardGroup in patientWardGroups" 
            :key="wardGroup.wardId"
            shadow="never"
            style="margin-bottom: 20px"
          >
            <template #header>
              <div class="card-header">
                <span>{{ wardGroup.wardName }}</span>
                <div class="header-tags">
                  <el-tag type="primary">{{ wardGroup.patients.length }} 位患者</el-tag>
                  <el-tag type="success">{{ getWardAvailableBeds(wardGroup.wardId) }} 张空床位</el-tag>
                </div>
              </div>
            </template>

            <div class="patient-cards-grid">
              <!-- 患者卡片 -->
              <el-popover
                v-for="patient in wardGroup.patients" 
                :key="patient.id"
                placement="right"
                :width="280"
                trigger="hover"
                popper-class="patient-staff-popover"
              >
                <template #reference>
                  <div 
                    class="patient-card"
                    @click="handlePatientCardClick(patient)"
                  >
                <!-- 卡片头部 -->
                <div class="patient-card-header">
                  <!-- 状态标签 -->
                  <el-tag 
                    :type="getPatientStatusColor(patient.status)" 
                    size="default"
                    class="status-tag"
                  >
                    {{ getPatientStatusText(patient.status) }}
                  </el-tag>

                  <!-- 护理级别标签 -->
                  <el-tag 
                    :type="getPatientNursingGradeColor(patient.nursingGrade)" 
                    size="small"
                    class="nursing-tag"
                  >
                    {{ getPatientNursingGradeText(patient.nursingGrade) }}
                  </el-tag>

                  <!-- 异常状态标签 -->
                  <el-tag 
                    v-if="patient.nursingAnomalyStatus === 1"
                    type="danger"
                    size="small"
                    class="anomaly-tag"
                  >
                    异常
                  </el-tag>

                  <!-- 患者ID -->
                  <span class="patient-id">{{ patient.id }}</span>
                </div>

                <!-- 患者基本信息 -->
                <div class="patient-card-body">
                  <!-- 姓名和性别 -->
                  <div class="info-row name-row">
                    <span class="name">{{ patient.name }}</span>
                    <el-tag :type="patient.gender === '男' ? '' : 'danger'" size="small">
                      {{ patient.gender }}
                    </el-tag>
                    <span class="age">{{ patient.age }}岁</span>
                  </div>

                  <!-- 床位信息 -->
                  <div class="info-row">
                    <el-icon><LocationInformation /></el-icon>
                    <span class="label">床位:</span>
                    <span class="value">{{ patient.bedId || '未分配' }}</span>
                  </div>

                  <!-- 科室病区 -->
                  <div class="info-row" v-if="patient.department || patient.ward">
                    <el-icon><OfficeBuilding /></el-icon>
                    <span class="label">科室:</span>
                    <span class="value">{{ patient.department }} - {{ patient.ward }}</span>
                  </div>
                </div>

                <!-- 卡片底部操作栏 -->
                <div class="patient-card-footer">
                  <!-- 查看详情按钮 -->
                  <el-button 
                    size="small" 
                    type="primary"
                    link
                    @click.stop="handleViewPatientDetail(patient)"
                  >
                    查看详情
                  </el-button>

                  <!-- 入院按钮（待入院状态显示） -->
                  <el-button 
                    v-if="patient.status === 0"
                    size="small" 
                    type="warning"
                    @click.stop="handlePatientAdmission(patient)"
                  >
                    办理入院
                  </el-button>

                  <!-- 出院按钮（待出院状态显示） -->
                  <el-button 
                    v-if="patient.status === 2"
                    size="small" 
                    type="success"
                    @click.stop="handlePatientDischarge(patient)"
                  >
                    办理出院
                  </el-button>
                </div>
              </div>
                </template>

                <div class="staff-info-content">
                  <div class="staff-group">
                    <div class="group-title">
                      <el-icon><Avatar /></el-icon> 责任医生
                    </div>
                    <div class="info-list">
                      <div class="info-item">
                        <span class="label">姓名:</span>
                        <span class="value">{{ patient.responsibleDoctorName || '未分配' }}</span>
                      </div>
                      <div class="info-item">
                        <span class="label">ID:</span>
                        <span class="value">{{ patient.responsibleDoctorId || '-' }}</span>
                      </div>
                      <div class="info-item">
                        <span class="label">电话:</span>
                        <span class="value">{{ patient.responsibleDoctorPhone || '-' }}</span>
                      </div>
                    </div>
                  </div>
                  
                  <el-divider style="margin: 12px 0" />
                  
                  <div class="staff-group">
                    <div class="group-title">
                      <el-icon><FirstAidKit /></el-icon> 责任护士 (当前)
                    </div>
                    <div class="info-list">
                      <div class="info-item">
                        <span class="label">姓名:</span>
                        <span class="value">{{ patient.responsibleNurseName || '未分配' }}</span>
                      </div>
                      <div class="info-item">
                        <span class="label">ID:</span>
                        <span class="value">{{ patient.responsibleNurseId || '-' }}</span>
                      </div>
                      <div class="info-item">
                        <span class="label">电话:</span>
                        <span class="value">{{ patient.responsibleNursePhone || '-' }}</span>
                      </div>
                    </div>
                  </div>
                </div>
              </el-popover>

              <!-- 空闲床位卡片 -->
              <div 
                v-for="bedIndex in getWardAvailableBeds(wardGroup.wardId)" 
                :key="'bed-' + wardGroup.wardId + '-' + bedIndex"
                class="empty-bed-card"
              >
                <div class="empty-bed-icon">
                  <el-icon :size="48" color="#c0c4cc"><House /></el-icon>
                </div>
                <div class="empty-bed-label">空闲床位</div>
              </div>
            </div>

            <el-empty v-if="wardGroup.patients.length === 0 && getWardAvailableBeds(wardGroup.wardId) === 0" description="该病区暂无床位" />
          </el-card>
        </div>
      </div>
    </div>

    <!-- 患者详情对话框 -->
    <el-dialog
      v-model="patientDetailDialogVisible"
      :title="`患者详情 - ${currentPatient.name} (${currentPatient.id})`"
      width="800px"
      :close-on-click-modal="false"
    >
      <el-form
        ref="patientDetailFormRef"
        :model="patientDetailForm"
        :rules="patientDetailRules"
        label-width="120px"
        v-loading="loadingPatientDetail"
      >
        <!-- 基本信息（不可修改） -->
        <el-divider content-position="left">
          <span style="font-size: 16px; font-weight: 600; color: #303133;">基本信息</span>
        </el-divider>
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="姓名">
              <el-input v-model="currentPatient.name" disabled class="readonly-input" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="身份证号">
              <el-input v-model="currentPatient.idCard" disabled class="readonly-input" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="住院号">
              <el-input v-model="currentPatient.id" disabled class="readonly-input" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="出生日期">
              <el-input v-model="currentPatient.dateOfBirth" disabled class="readonly-input" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="年龄">
              <el-input :value="currentPatient.age + '岁'" disabled class="readonly-input" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="24">
            <el-form-item label="门诊诊断">
              <el-input 
                v-model="currentPatient.outpatientDiagnosis" 
                type="textarea"
                :rows="2"
                disabled
                class="readonly-input"
              />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="预约入院时间">
              <el-input v-model="currentPatient.scheduledAdmissionTime" disabled class="readonly-input" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="实际入院时间">
              <el-input v-model="currentPatient.actualAdmissionTime" disabled class="readonly-input" />
            </el-form-item>
          </el-col>
        </el-row>

        <!-- 可编辑信息 -->
        <el-divider content-position="left">
          <span style="font-size: 16px; font-weight: 600; color: #303133;">可编辑信息</span>
        </el-divider>
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="性别" prop="gender">
              <el-select v-model="patientDetailForm.gender" placeholder="请选择性别" style="width: 100%">
                <el-option label="男" value="男" />
                <el-option label="女" value="女" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="电话号码" prop="phoneNumber">
              <el-input v-model="patientDetailForm.phoneNumber" placeholder="请输入电话号码" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="身高(cm)" prop="height">
              <el-input-number 
                v-model="patientDetailForm.height" 
                :min="0" 
                :max="300" 
                :precision="1"
                placeholder="请输入身高"
                style="width: 100%"
              />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="体重(kg)" prop="weight">
              <el-input-number 
                v-model="patientDetailForm.weight" 
                :min="0" 
                :max="500" 
                :precision="1"
                placeholder="请输入体重"
                style="width: 100%"
              />
            </el-form-item>
          </el-col>
        </el-row>
        <!-- 住院信息（只读） -->
        <el-divider content-position="left">
          <span style="font-size: 16px; font-weight: 600; color: #303133;">住院信息</span>
        </el-divider>
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="当前病床">
              <el-input :value="currentPatient.bedId || '未分配'" disabled class="readonly-input" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="所属科室">
              <el-input v-model="currentPatient.department" disabled class="readonly-input" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="所属病区">
              <el-input v-model="currentPatient.ward" disabled class="readonly-input" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="护理级别">
              <el-tag :type="getPatientNursingGradeColor(currentPatient.nursingGrade)">
                {{ getPatientNursingGradeText(currentPatient.nursingGrade) }}
              </el-tag>
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="当前状态">
              <el-tag :type="getPatientStatusColor(currentPatient.status)">
                {{ getPatientStatusText(currentPatient.status) }}
              </el-tag>
            </el-form-item>
          </el-col>
        </el-row>
      </el-form>

      <template #footer>
        <span class="dialog-footer">
          <el-button @click="patientDetailDialogVisible = false">取消</el-button>
          <el-button type="primary" @click="handleSavePatientDetail" :loading="savingPatientDetail">
            保存修改
          </el-button>
        </span>
      </template>
    </el-dialog>

    <!-- 出院检查对话框 -->
    <el-dialog
      v-model="dischargeCheckDialogVisible"
      title="无法办理出院"
      width="600px"
      :close-on-click-modal="false"
    >
      <div class="discharge-check-content">
        <el-alert
          title="检测到患者有未完成的医嘱任务"
          type="warning"
          :closable="false"
          show-icon
        >
          <template #default>
            <p style="margin: 8px 0;">请先完成以下任务后再办理出院：</p>
          </template>
        </el-alert>

        <div class="unfinished-tasks-section">
          <div class="section-title">【未完成任务列表】</div>
          <div class="tasks-list">
            <div 
              v-for="(task, index) in unfinishedTasks" 
              :key="index"
              class="task-item"
            >
              <div class="task-header">
                <span class="task-number">{{ index + 1 }}.</span>
                <span class="task-name">{{ formatTaskTitle(task) }}</span>
                <span class="task-id">(ID: {{ task.orderId }})</span>
                <el-tag :type="getOrderTypeTagColor(task.orderType)" size="small">
                  {{ getOrderTypeDisplayName(task.orderType) }}
                </el-tag>
              </div>
              <div class="task-details">
                <div class="task-detail-row">
                  <span class="detail-label">状态:</span>
                  <span class="detail-value">{{ task.statusDisplay }}</span>
                </div>
                <div class="task-detail-row" v-if="task.latestTaskTime">
                  <span class="detail-label">最晚执行:</span>
                  <span class="detail-value">{{ formatDateTime(task.latestTaskTime) }}</span>
                </div>
              </div>
            </div>
          </div>
        </div>

        <el-alert
          title="提示：请前往医嘱管理界面处理"
          type="info"
          :closable="false"
          show-icon
          style="margin-top: 16px;"
        />
      </div>

      <template #footer>
        <span class="dialog-footer">
          <el-button type="primary" @click="dischargeCheckDialogVisible = false">确定</el-button>
        </span>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { ElMessage, ElMessageBox } from 'element-plus';
import {
  Refresh,
  House,
  User,
  CircleCheck,
  DataAnalysis,
  Search,
  Plus,
  Loading,
  LocationInformation,
  OfficeBuilding,
  Avatar,
  FirstAidKit,
  WarningFilled
} from '@element-plus/icons-vue';
import { 
  getPatientManagementList,
  getPatientFullInfo,
  updatePatientInfo,
  checkPatientDischarge,
  processPatientDischarge,
  getPatientStatusText as getStatusText,
  getPatientStatusColor as getStatusColor,
  getNursingGradeText as getGradeText
} from '@/api/patient';
import { getWardOverview } from '@/api/nursing';

const router = useRouter();

// 当前护士信息
const getCurrentNurse = () => {
  const userInfo = localStorage.getItem('userInfo');
  if (userInfo) {
    try {
      const user = JSON.parse(userInfo);
      return {
        staffId: user.staffId,
        deptCode: user.deptCode,
        name: user.fullName
      };
    } catch (error) {
      console.error('解析用户信息失败:', error);
    }
  }
  return null;
};

// 数据状态
const overview = reactive({
  departmentId: '',
  departmentName: '',
  totalBeds: 0,
  occupiedBeds: 0,
  availableBeds: 0
});

// 病区数据
const availableWards = ref([]);
const selectedWardId = ref('');

// 患者管理相关状态
const loadingPatients = ref(false);
const patientList = ref([]);
const patientWardGroups = ref([]); // 患者按病区分组
const patientFilterStatus = ref([]); // 多选状态数组
const patientSearchKeyword = ref('');
let patientSearchTimer = null;

// 患者详情对话框状态
const patientDetailDialogVisible = ref(false);
const loadingPatientDetail = ref(false);
const savingPatientDetail = ref(false);
const patientDetailFormRef = ref(null);
const currentPatient = ref({
  id: '',
  name: '',
  idCard: '',
  dateOfBirth: '',
  age: 0,
  bedId: '',
  department: '',
  ward: '',
  status: 1,
  nursingGrade: 2,
  outpatientDiagnosis: '',
  scheduledAdmissionTime: '',
  actualAdmissionTime: ''
});
const patientDetailForm = reactive({
  gender: '',
  phoneNumber: '',
  height: null,
  weight: null
});

// 表单验证规则
const patientDetailRules = {
  gender: [
    { required: true, message: '请选择性别', trigger: 'change' }
  ],
  phoneNumber: [
    { pattern: /^1[3-9]\d{9}$/, message: '请输入正确的手机号码', trigger: 'blur' }
  ],
  height: [
    { type: 'number', min: 0, max: 300, message: '身高范围为0-300cm', trigger: 'blur' }
  ],
  weight: [
    { type: 'number', min: 0, max: 500, message: '体重范围为0-500kg', trigger: 'blur' }
  ]
};

// 出院检查对话框状态
const dischargeCheckDialogVisible = ref(false);
const unfinishedTasks = ref([]);

// 床位使用率
const bedOccupancyRate = computed(() => {
  if (overview.totalBeds === 0) return 0;
  return Math.round((overview.occupiedBeds / overview.totalBeds) * 100);
});

// ==================== 数据加载方法 ====================

/**
 * 加载床位统计数据
 */
const loadStatistics = async () => {
  const nurseInfo = getCurrentNurse();
  if (!nurseInfo || !nurseInfo.deptCode) {
    console.warn('未找到护士科室信息');
    return;
  }

  try {
    // 调用后端API获取病区概览统计数据
    const data = await getWardOverview(selectedWardId.value, nurseInfo.deptCode);
    
    console.log('床位统计数据:', data);
    
    if (data) {
      // 更新科室信息
      overview.departmentId = data.departmentId || nurseInfo.deptCode;
      overview.departmentName = data.departmentName || '';
      
      // 如果返回的是科室级别的数据（包含多个病区）
      if (data.wards && Array.isArray(data.wards)) {
        // 更新病区列表
        availableWards.value = data.wards.map(ward => ({
          wardId: ward.wardId,
          wardName: ward.wardName,
          totalBeds: ward.totalBeds,
          occupiedBeds: ward.occupiedBeds,
          availableBeds: ward.availableBeds
        }));
        
        // 更新统计汇总数据
        overview.totalBeds = data.totalBeds || 0;
        overview.occupiedBeds = data.occupiedBeds || 0;
        overview.availableBeds = data.availableBeds || 0;
      } else {
        // 单病区数据
        overview.totalBeds = data.totalBeds || 0;
        overview.occupiedBeds = data.occupiedBeds || 0;
        overview.availableBeds = data.availableBeds || 0;
        
        // 如果有病区信息，添加到列表
        if (data.wardId && data.wardName) {
          availableWards.value = [{
            wardId: data.wardId,
            wardName: data.wardName,
            totalBeds: data.totalBeds,
            occupiedBeds: data.occupiedBeds,
            availableBeds: data.availableBeds
          }];
        }
      }
      
      console.log('统计数据更新成功:', {
        totalBeds: overview.totalBeds,
        occupiedBeds: overview.occupiedBeds,
        availableBeds: overview.availableBeds,
        wards: availableWards.value
      });
    }
  } catch (error) {
    console.error('加载床位统计数据失败:', error);
    ElMessage.error('加载统计数据失败: ' + (error.message || '未知错误'));
  }
};

/**
 * 病区筛选变化处理
 */
const handleWardFilterChange = () => {
  // 重新加载统计数据和患者数据
  loadStatistics();
  loadPatientData();
};

// ==================== 患者管理相关方法 ====================

/**
 * 加载患者数据（基于床位展示）
 */
const loadPatientData = async () => {
  const nurseInfo = getCurrentNurse();
  if (!nurseInfo || !nurseInfo.deptCode) {
    console.warn('未找到护士科室信息');
    return;
  }

  loadingPatients.value = true;
  
  try {
    const params = {
      departmentId: nurseInfo.deptCode
    };
    
    // 添加病区筛选
    if (selectedWardId.value) {
      params.wardId = selectedWardId.value;
    }
    
    // 添加状态筛选（多选）
    if (patientFilterStatus.value && patientFilterStatus.value.length > 0) {
      params.statuses = patientFilterStatus.value.join(',');
    }
    // 注意：后端默认已排除待入院和已出院患者，无需前端额外处理
    
    // 添加搜索关键词
    if (patientSearchKeyword.value && patientSearchKeyword.value.trim()) {
      params.keyword = patientSearchKeyword.value.trim();
    }
    
    // 调用API
    const data = await getPatientManagementList(params);
    patientList.value = data || [];
    
    // 按病区分组
    groupPatientsByWard();
    
    console.log('患者列表加载成功（已排除出院患者）:', patientList.value);
  } catch (error) {
    console.error('加载患者列表失败:', error);
    ElMessage.error('加载患者列表失败: ' + (error.message || '未知错误'));
    patientList.value = [];
    patientWardGroups.value = [];
  } finally {
    loadingPatients.value = false;
  }
};

/**
 * 按病区分组患者
 */
const groupPatientsByWard = () => {
  if (!patientList.value || patientList.value.length === 0) {
    patientWardGroups.value = [];
    return;
  }

  // 使用 Map 来分组
  const wardMap = new Map();
  
  patientList.value.forEach(patient => {
    const wardId = patient.ward || 'unknown';
    const wardName = patient.ward || '未分配病区';
    
    if (!wardMap.has(wardId)) {
      wardMap.set(wardId, {
        wardId: wardId,
        wardName: wardName,
        patients: []
      });
    }
    
    wardMap.get(wardId).patients.push(patient);
  });
  
  // 转换为数组
  patientWardGroups.value = Array.from(wardMap.values());
  
  console.log('患者按病区分组:', patientWardGroups.value);
};

/**
 * 搜索防抖处理
 */
const handlePatientSearch = () => {
  // 清除之前的定时器
  if (patientSearchTimer) {
    clearTimeout(patientSearchTimer);
  }
  
  // 500ms后执行搜索
  patientSearchTimer = setTimeout(() => {
    loadPatientData();
  }, 500);
};

/**
 * 患者卡片点击
 */
const handlePatientCardClick = (patient) => {
  console.log('点击患者卡片:', patient);
  handleViewPatientDetail(patient);
};

/**
 * 查看患者详情
 */
const handleViewPatientDetail = async (patient) => {
  try {
    loadingPatientDetail.value = true;
    patientDetailDialogVisible.value = true;
    
    // 获取患者完整信息
    const fullInfo = await getPatientFullInfo(patient.id);
    
    // 更新当前患者基本信息（不可修改部分）
    currentPatient.value = {
      id: fullInfo.id || patient.id,
      name: fullInfo.name || patient.name,
      idCard: fullInfo.idCard || '',
      dateOfBirth: fullInfo.dateOfBirth || '',
      age: fullInfo.age || patient.age,
      bedId: fullInfo.bedId || patient.bedId,
      department: fullInfo.department || patient.department,
      ward: fullInfo.ward || patient.ward,
      status: fullInfo.status !== undefined ? fullInfo.status : patient.status,
      nursingGrade: fullInfo.nursingGrade !== undefined ? fullInfo.nursingGrade : 2,
      outpatientDiagnosis: fullInfo.outpatientDiagnosis || '',
      scheduledAdmissionTime: fullInfo.scheduledAdmissionTime || '',
      actualAdmissionTime: fullInfo.actualAdmissionTime || ''
    };
    
    // 更新可编辑表单
    patientDetailForm.gender = fullInfo.gender || patient.gender || '';
    patientDetailForm.phoneNumber = fullInfo.phoneNumber || '';
    patientDetailForm.height = fullInfo.height || null;
    patientDetailForm.weight = fullInfo.weight || null;
    
    console.log('患者详情加载成功:', fullInfo);
  } catch (error) {
    console.error('加载患者详情失败:', error);
    ElMessage.error('加载患者详情失败: ' + (error.message || '未知错误'));
    patientDetailDialogVisible.value = false;
  } finally {
    loadingPatientDetail.value = false;
  }
};

/**
 * 保存患者详情修改
 */
const handleSavePatientDetail = async () => {
  if (!patientDetailFormRef.value) return;
  
  // 准备更新数据 - 只发送有值的字段
  const updateData = {};
  
  try {
    // 表单验证
    await patientDetailFormRef.value.validate();
    
    savingPatientDetail.value = true;
    
    if (patientDetailForm.gender) {
      updateData.gender = patientDetailForm.gender;
    }
    
    if (patientDetailForm.phoneNumber) {
      updateData.phoneNumber = patientDetailForm.phoneNumber;
    }
    
    if (patientDetailForm.height !== null && patientDetailForm.height !== undefined) {
      updateData.height = patientDetailForm.height;
    }
    
    if (patientDetailForm.weight !== null && patientDetailForm.weight !== undefined) {
      updateData.weight = patientDetailForm.weight;
    }
    
    // 如果没有任何字段需要更新，直接关闭对话框
    if (Object.keys(updateData).length === 0) {
      ElMessage.info('没有修改任何信息');
      patientDetailDialogVisible.value = false;
      savingPatientDetail.value = false;
      return;
    }
    
    // 添加操作员信息
    const nurseInfo = getCurrentNurse();
    if (nurseInfo) {
      updateData.operatorId = nurseInfo.staffId;
      updateData.operatorType = 'Nurse'; // 护士类型
    }
    
    // 调用更新API
    await updatePatientInfo(currentPatient.value.id, updateData);
    
    ElMessage.success('患者信息更新成功');
    patientDetailDialogVisible.value = false;
    
    // 重新加载患者列表
    await loadPatientData();
  } catch (error) {
    if (error.message && error.message !== 'validation failed') {
      console.error('保存患者信息失败:', error);
      console.error('错误详情:', error.response?.data);
      console.error('发送的数据:', updateData);
      
      const errorMsg = error.response?.data?.message || error.response?.data?.title || error.message || '未知错误';
      ElMessage.error('保存失败: ' + errorMsg);
    }
  } finally {
    savingPatientDetail.value = false;
  }
};

/**
 * 办理入院
 */
const handlePatientAdmission = async (patient) => {
  // 跳转到入院页面，传递患者ID
  router.push({
    path: '/nurse/patient-admission',
    query: { patientId: patient.id }
  });
};

/**
 * 格式化任务标题
 */
const formatTaskTitle = (task) => {
  if (!task) return '';
  
  // 根据医嘱类型格式化标题
  switch (task.orderType) {
    case 'DischargeOrder':
    case 'Discharge':
      // 出院医嘱：显示 "出院医嘱-代取药品：药品名称"
      // 从 medicationOrderItems 中提取药品名称（兼容大小写）
      const dischargeMeds = task.medicationOrderItems || task.MedicationOrderItems;
      if (dischargeMeds && dischargeMeds.length > 0) {
        const firstDrug = dischargeMeds[0].drug?.drugName || dischargeMeds[0].Drug?.DrugName || '未知药品';
        const suffix = dischargeMeds.length > 1 ? '等' : '';
        return `出院医嘱-代取药品：${firstDrug}${suffix}`;
      }
      return `出院医嘱-代取药品：${task.orderSummary || '未知药品'}`;
    
    case 'MedicationOrder':
    case 'Medication':
      // 药品医嘱：显示 "待用药：药品名称"
      // 从 medicationOrderItems 中提取药品名称（兼容大小写）
      const meds = task.medicationOrderItems || task.MedicationOrderItems;
      if (meds && meds.length > 0) {
        const firstDrug = meds[0].drug?.drugName || meds[0].Drug?.DrugName || '未知药品';
        const suffix = meds.length > 1 ? '等' : '';
        return `待用药：${firstDrug}${suffix}`;
      }
      return `待用药：${task.orderSummary || '未知药品'}`;
    
    case 'OperationOrder':
    case 'Operation':
      // 操作医嘱：显示操作名称 operationName
      return task.operationName || task.OperationName || task.orderSummary || '未知操作';
    
    case 'SurgicalOrder':
    case 'Surgical':
      // 手术医嘱：显示手术名称 surgeryName
      return task.surgeryName || task.SurgeryName || task.orderSummary || '未知手术';
    
    case 'InspectionOrder':
    case 'Inspection':
      // 检查医嘱：显示检查项目名称 itemName
      return task.itemName || task.ItemName || task.orderSummary || '未知检查';
    
    default:
      return task.orderSummary || '未知医嘱';
  }
};

/**
 * 获取医嘱类型中文名称
 */
const getOrderTypeDisplayName = (orderType) => {
  const typeMap = {
    'MedicationOrder': '药品',
    'Medication': '药品',
    'OperationOrder': '操作',
    'Operation': '操作',
    'SurgicalOrder': '手术',
    'Surgical': '手术',
    'InspectionOrder': '检查',
    'Inspection': '检查',
    'DischargeOrder': '出院',
    'Discharge': '出院'
  };
  return typeMap[orderType] || orderType;
};

/**
 * 获取医嘱类型标签颜色
 */
const getOrderTypeTagColor = (orderType) => {
  const colorMap = {
    'MedicationOrder': 'primary',
    'Medication': 'primary',
    'OperationOrder': 'warning',
    'Operation': 'warning',
    'SurgicalOrder': 'danger',
    'Surgical': 'danger',
    'InspectionOrder': 'info',
    'Inspection': 'info',
    'DischargeOrder': 'success',
    'Discharge': 'success'
  };
  return colorMap[orderType] || 'info';
};

/**
 * 办理出院
 */
const handlePatientDischarge = async (patient) => {
  try {
    // 先检查是否可以出院
    const checkResult = await checkPatientDischarge(patient.id);
    
    if (!checkResult.canDischarge) {
      // 有未完成的任务，显示提示对话框
      unfinishedTasks.value = checkResult.unfinishedTasks || [];
      dischargeCheckDialogVisible.value = true;
      return;
    }
    
    // 可以出院，确认操作
    await ElMessageBox.confirm(
      `确认为患者 ${patient.name} (${patient.id}) 办理出院？`,
      '确认出院',
      {
        confirmButtonText: '确认出院',
        cancelButtonText: '取消',
        type: 'warning'
      }
    );
    
    // 获取当前护士信息
    const nurseInfo = getCurrentNurse();
    if (!nurseInfo) {
      ElMessage.error('无法获取当前护士信息');
      return;
    }
    
    // 调用出院API
    await processPatientDischarge(patient.id, {
      patientId: patient.id,
      operatorId: nurseInfo.staffId,
      operatorType: 'Nurse',
      remarks: `护士 ${nurseInfo.name} 于 ${new Date().toLocaleString()} 办理出院`
    });
    
    ElMessage.success('出院办理成功');
    
    // 重新加载数据
    await Promise.all([
      loadStatistics(),
      loadPatientData()
    ]);
  } catch (error) {
    if (error !== 'cancel') {
      console.error('办理出院失败:', error);
      ElMessage.error('办理出院失败: ' + (error.message || '未知错误'));
    }
  }
};

/**
 * 新增患者
 */
const handleAddPatient = () => {
  // 跳转到入院页面（不传递patientId，需要上传条形码）
  router.push('/nurse/patient-admission');
};

/**
 * 获取患者状态显示文本
 */
const getPatientStatusText = (status) => {
  return getStatusText(status);
};

/**
 * 获取患者状态标签颜色
 */
const getPatientStatusColor = (status) => {
  return getStatusColor(status);
};

/**
 * 获取护理级别显示文本
 */
const getPatientNursingGradeText = (grade) => {
  return getGradeText(grade);
};

/**
 * 获取护理级别颜色
 */
const getPatientNursingGradeColor = (grade) => {
  const colorMap = {
    0: 'danger',   // 特级 - 红色
    1: 'warning',  // 一级 - 橙色
    2: 'primary',  // 二级 - 蓝色
    3: 'info'      // 三级 - 灰色
  };
  return colorMap[grade] || 'info';
};

/**
 * 获取病区空闲床位数
 */
const getWardAvailableBeds = (wardId) => {
  const ward = availableWards.value.find(w => w.wardId === wardId);
  return ward ? ward.availableBeds || 0 : 0;
};

/**
 * 格式化日期时间
 */
const formatDateTime = (dateTime) => {
  if (!dateTime) return '';
  const date = new Date(dateTime);
  return date.toLocaleString('zh-CN', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit'
  });
};

// 组件挂载
onMounted(() => {
  // 先加载床位统计数据
  loadStatistics();
  // 再加载患者数据
  loadPatientData();
});
</script>

<style scoped>
.nurse-dashboard {
  padding: 20px;
}

.dashboard-stats {
  margin-bottom: 20px;
}

.stat-card {
  display: flex;
  align-items: center;
  gap: 16px;
}

.stat-icon {
  width: 64px;
  height: 64px;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.stat-content {
  flex: 1;
}

.stat-value {
  font-size: 28px;
  font-weight: bold;
  color: #303133;
  margin-bottom: 4px;
}

.stat-label {
  font-size: 14px;
  color: #909399;
}

/* ==================== 患者管理区域样式 ==================== */
.patient-management-section {
  margin: 30px 0;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  font-size: 16px;
  font-weight: 600;
}

.card-header .header-tags {
  display: flex;
  gap: 8px;
}

.patient-filter-toolbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 16px 20px;
  background-color: #ffffff;
  border-radius: 8px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
  margin-bottom: 20px;
}

.filter-left {
  display: flex;
  align-items: center;
  gap: 20px;
  flex: 1;
}

.filter-right {
  display: flex;
  align-items: center;
  gap: 12px;
}

.filter-group {
  display: flex;
  align-items: center;
  gap: 8px;
}

.filter-label {
  font-size: 14px;
  color: #606266;
  white-space: nowrap;
  font-weight: 500;
}

.ward-select {
  min-width: 160px;
}

.status-select {
  min-width: 140px;
}

.search-input {
  width: 320px;
}

.patients-by-ward {
  min-height: 200px;
}

.patient-cards-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
  gap: 16px;
  padding: 4px;
}

/* 患者卡片样式 */
.patient-card {
  background-color: #ffffff;
  border-radius: 8px;
  padding: 16px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
  cursor: pointer;
  transition: all 0.3s ease;
  display: flex;
  flex-direction: column;
  gap: 12px;
  border: 1px solid #e4e7ed;
}

.patient-card:hover {
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.12);
  transform: translateY(-2px);
}

.patient-card-header {
  display: flex;
  align-items: center;
  gap: 8px;
  padding-bottom: 12px;
  border-bottom: 1px solid #e4e7ed;
}

.patient-card-header .status-tag {
  font-weight: 600;
}

.patient-card-header .nursing-tag {
  margin-left: auto;
}

.patient-card-header .patient-id {
  font-size: 12px;
  color: #909399;
  font-family: 'Consolas', monospace;
}

.patient-card-body {
  display: flex;
  flex-direction: column;
  gap: 8px;
  flex: 1;
}

.patient-card-body .info-row {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 14px;
  color: #303133;
}

.patient-card-body .name-row {
  margin-bottom: 4px;
}

.patient-card-body .name {
  font-size: 18px;
  font-weight: 600;
  color: #303133;
}

.patient-card-body .age {
  font-size: 14px;
  color: #606266;
  margin-left: 8px;
}

.patient-card-body .info-row .el-icon {
  color: #409eff;
  font-size: 16px;
}

.patient-card-body .info-row .label {
  color: #909399;
  font-size: 13px;
}

.patient-card-body .info-row .value {
  color: #303133;
  font-size: 14px;
  font-weight: 500;
}

.patient-card-footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding-top: 12px;
  border-top: 1px solid #e4e7ed;
}

/* 空闲床位卡片样式 */
.empty-bed-card {
  background: linear-gradient(135deg, #f5f7fa 0%, #ebeef5 100%);
  border-radius: 8px;
  padding: 24px 16px;
  border: 2px dashed #dcdfe6;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 12px;
  min-height: 150px;
  transition: all 0.3s ease;
}

.empty-bed-card:hover {
  border-color: #c0c4cc;
  background: linear-gradient(135deg, #ebeef5 0%, #e4e7ed 100%);
}

.empty-bed-icon {
  opacity: 0.5;
}

.empty-bed-label {
  font-size: 14px;
  color: #909399;
  font-weight: 500;
}

.empty-bed-card:hover .empty-bed-label {
  color: #606266;
}

/* 加载和空状态 */
.loading-state,
.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  min-height: 300px;
  color: #909399;
}

.loading-state .el-icon {
  font-size: 48px;
  color: #409eff;
  margin-bottom: 16px;
}

.loading-state p {
  font-size: 16px;
  margin: 0;
}

.empty-icon {
  font-size: 64px;
  margin-bottom: 16px;
  opacity: 0.6;
}

.empty-state p {
  font-size: 16px;
  margin: 8px 0;
}

.empty-hint {
  font-size: 14px;
  color: #c0c4cc;
}

@media (max-width: 768px) {
  .beds-grid {
    grid-template-columns: repeat(auto-fill, minmax(240px, 1fr));
  }
  
  .patient-filter-toolbar {
    flex-direction: column;
    gap: 12px;
    align-items: stretch;
  }

  .filter-left {
    flex-direction: column;
    align-items: stretch;
  }

  .search-input {
    width: 100%;
  }

  .patient-cards-grid {
    grid-template-columns: 1fr;
  }
}

/* ==================== 患者详情对话框样式 ==================== */
.dialog-footer {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
}

/* 只读输入框样式 - 显示黑色文字 */
.readonly-input :deep(.el-input__inner),
.readonly-input :deep(.el-textarea__inner) {
  color: #303133 !important;
  -webkit-text-fill-color: #303133 !important;
  cursor: default;
}

/* ==================== 出院检查对话框样式 ==================== */
.discharge-check-content {
  padding: 12px 0;
}

.unfinished-tasks-section {
  margin: 20px 0;
  padding: 16px;
  background-color: #fafafa;
  border-radius: 8px;
  border: 1px solid #e4e7ed;
}

.section-title {
  font-size: 15px;
  font-weight: 600;
  color: #303133;
  margin-bottom: 16px;
}

.tasks-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.task-item {
  background-color: #ffffff;
  padding: 12px 16px;
  border-radius: 6px;
  border: 1px solid #e4e7ed;
}

.task-header {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 8px;
}

.task-number {
  font-weight: 600;
  color: #606266;
}

.task-name {
  font-size: 15px;
  font-weight: 500;
  color: #303133;
}

.task-id {
  font-size: 12px;
  color: #909399;
  font-family: 'Courier New', monospace;
  margin-right: 8px;
}

.task-header .el-tag {
  margin-left: auto;
}

.task-details {
  padding-left: 24px;
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.task-detail-row {
  display: flex;
  align-items: center;
  font-size: 14px;
}

.detail-label {
  color: #909399;
  margin-right: 8px;
  min-width: 70px;
}

.detail-value {
  color: #606266;
}

.anomaly-tag {
  min-width: 40px;
  text-align: center;
}

/* 医护人员信息弹窗样式 */
.staff-info-content {
  padding: 4px;
}

.staff-group {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.group-title {
  font-size: 14px;
  font-weight: 600;
  color: #303133;
  display: flex;
  align-items: center;
  gap: 6px;
}

.group-title .el-icon {
  color: #409eff;
}

.info-list {
  display: flex;
  flex-direction: column;
  gap: 4px;
  padding-left: 22px;
}

.info-item {
  font-size: 13px;
  display: flex;
  align-items: center;
}

.info-item .label {
  color: #909399;
  width: 40px;
  margin-right: 8px;
}

.info-item .value {
  color: #606266;
  font-family: 'Consolas', monospace;
}
</style>
