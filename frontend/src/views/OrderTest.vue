<template>
  <div class="order-test-page">
    <!-- 左侧患者列表面板 -->
    <PatientListPanel
      :patient-list="patientList"
      :selected-patients="selectedPatients"
      :my-ward-id="currentScheduledWardId"
      title="患者列表"
      @patient-select="handlePatientSelect"
      @multi-select-toggle="handleMultiSelectToggle"
    />

    <!-- 右侧工作区 -->
    <section class="work-area">
      <!-- 患者信息栏 -->
      <PatientInfoBar
        :patients="selectedPatients"
        :is-multi-select="enableMultiSelect"
        :sort-by="sortBy"
        @sort-change="handleSortChange"
      />

      <!-- 工作区内容（占位） -->
      <div class="content-placeholder" v-if="selectedPatients.length === 0">
        <div class="placeholder-icon">📋</div>
        <p>请从左侧选择患者</p>
        <p class="placeholder-subtitle">这是一个测试页面，用于验证患者列表组件是否正常工作</p>
      </div>

      <!-- 选中患者后显示的内容 -->
      <div class="content-placeholder success" v-else>
        <div class="placeholder-icon">✅</div>
        <h3>组件测试成功！</h3>
        <div class="test-info">
          <p><strong>已选中患者数量：</strong>{{ selectedPatients.length }}</p>
          <p><strong>多选模式：</strong>{{ enableMultiSelect ? '开启' : '关闭' }}</p>
          <p><strong>当前排班病区：</strong>{{ currentScheduledWardId || '未获取' }}</p>
          <p><strong>排序方式：</strong>{{ sortBy === 'time' ? '按时间' : '按患者' }}</p>
        </div>

        <!-- 显示选中的患者信息 -->
        <div class="selected-patients-detail">
          <h4>选中的患者信息：</h4>
          <el-table :data="selectedPatients" style="width: 100%; margin-top: 10px;" stripe>
            <el-table-column prop="bedId" label="床号" width="80" />
            <el-table-column prop="patientName" label="姓名" width="100" />
            <el-table-column prop="gender" label="性别" width="60" />
            <el-table-column prop="age" label="年龄" width="60" />
            <el-table-column prop="nursingGrade" label="护理等级" width="100">
              <template #default="scope">
                护理{{ scope.row.nursingGrade }}级
              </template>
            </el-table-column>
            <el-table-column prop="wardName" label="病区" />
            <el-table-column prop="unacknowledgedCount" label="待签收" width="80">
              <template #default="scope">
                <el-tag 
                  v-if="scope.row.unacknowledgedCount > 0" 
                  type="danger" 
                  size="small"
                >
                  {{ scope.row.unacknowledgedCount }}
                </el-tag>
                <span v-else style="color: #909399;">0</span>
              </template>
            </el-table-column>
          </el-table>
        </div>

        <!-- 功能测试按钮 -->
        <div class="test-actions">
          <el-button 
            type="primary" 
            @click="testRefresh"
            :icon="'Refresh'"
          >
            刷新患者列表
          </el-button>
          <el-button 
            @click="testClearSelection"
            :icon="'Close'"
          >
            清空选择
          </el-button>
          <el-button 
            type="success"
            @click="testToggleMultiSelect"
            :icon="'Operation'"
          >
            切换多选模式
          </el-button>
        </div>
      </div>
    </section>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue';
import { ElMessage } from 'element-plus';
import PatientListPanel from '@/components/PatientListPanel.vue';
import PatientInfoBar from '@/components/PatientInfoBar.vue';
import { usePatientData } from '@/composables/usePatientData';

// 使用患者数据管理 composable
const {
  patientList,
  selectedPatients,
  enableMultiSelect,
  currentScheduledWardId,
  initializePatientData,
  selectPatient,
  toggleMultiSelectMode,
  clearSelection
} = usePatientData();

// 排序方式
const sortBy = ref('time');

// 初始化
onMounted(async () => {
  console.log('📋 医嘱测试页面初始化...');
  await initializePatientData();
  console.log('✅ 患者数据加载完成:', patientList.value.length, '个患者');
});

// 处理患者选择
const handlePatientSelect = ({ patient, isMultiSelect }) => {
  console.log('👤 选择患者:', patient.patientName, '多选模式:', isMultiSelect);
  selectPatient(patient, isMultiSelect);
};

// 处理多选模式切换
const handleMultiSelectToggle = (enabled) => {
  console.log('🔄 多选模式切换:', enabled);
  toggleMultiSelectMode(enabled);
};

// 处理排序变化
const handleSortChange = (newSortBy) => {
  console.log('📊 排序方式变更:', newSortBy);
  sortBy.value = newSortBy;
};

// 测试功能：刷新患者列表
const testRefresh = async () => {
  ElMessage.info('正在刷新患者列表...');
  await initializePatientData();
  ElMessage.success('刷新成功！');
};

// 测试功能：清空选择
const testClearSelection = () => {
  clearSelection();
  ElMessage.success('已清空选择');
};

// 测试功能：切换多选模式
const testToggleMultiSelect = () => {
  toggleMultiSelectMode(!enableMultiSelect.value);
  ElMessage.success(enableMultiSelect.value ? '已开启多选模式' : '已关闭多选模式');
};
</script>

<style scoped>
.order-test-page {
  display: grid;
  grid-template-columns: 250px 1fr;
  height: calc(100vh - 60px);
  background: #f4f7f9;
  gap: 20px;
  padding: 20px;
}

.work-area {
  background: #ffffff;
  border-radius: 8px;
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.08);
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.content-placeholder {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 40px;
  color: #909399;
}

.content-placeholder.success {
  background: linear-gradient(135deg, #f0f9ff 0%, #e0f2fe 100%);
  color: #303133;
}

.placeholder-icon {
  font-size: 64px;
  margin-bottom: 20px;
  opacity: 0.6;
}

.content-placeholder h3 {
  font-size: 1.5rem;
  color: #409eff;
  margin-bottom: 20px;
}

.content-placeholder p {
  font-size: 1rem;
  margin: 5px 0;
}

.placeholder-subtitle {
  font-size: 0.9rem;
  color: #909399;
  margin-top: 10px;
}

.test-info {
  background: white;
  border-radius: 8px;
  padding: 20px;
  margin: 20px 0;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.06);
  min-width: 400px;
}

.test-info p {
  font-size: 0.95rem;
  padding: 8px 0;
  border-bottom: 1px solid #f0f0f0;
  color: #606266;
}

.test-info p:last-child {
  border-bottom: none;
}

.test-info strong {
  color: #303133;
  margin-right: 10px;
}

.selected-patients-detail {
  width: 100%;
  max-width: 900px;
  margin: 20px 0;
}

.selected-patients-detail h4 {
  font-size: 1.1rem;
  color: #303133;
  margin-bottom: 10px;
}

.test-actions {
  display: flex;
  gap: 12px;
  margin-top: 20px;
}

/* 响应式优化 */
@media (max-width: 1200px) {
  .order-test-page {
    grid-template-columns: 200px 1fr;
  }
}
</style>
