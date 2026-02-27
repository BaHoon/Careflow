<template>
  <div class="order-test-page">
    <!-- 左侧患者列表面板 - 自定义配置示例 -->
    <PatientListPanel
      :patient-list="customPatientList"
      :selected-patients="selectedPatients"
      :my-ward-id="currentScheduledWardId"
      title="患者列表"
      pending-filter-label="仅显示有待处理任务"
      badge-field="pendingTaskCount"
      :badge-filter="customBadgeFilter"
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
      
      <!-- 未选中患者时的占位提示 -->
      <div v-if="selectedPatients.length === 0" class="no-patient-bar">
        <el-icon><InfoFilled /></el-icon>
        <span>请从左侧患者列表中选择患者</span>
      </div>

      <!-- 工作区内容 -->
      <div class="content-area">
        <div class="config-demo">
          <h3>自定义配置演示</h3>
          
          <el-card class="demo-card">
            <template #header>
              <div class="card-header">
                <span>配置说明</span>
              </div>
            </template>
            
            <el-descriptions :column="1" border>
              <el-descriptions-item label="筛选标签">
                仅显示有待处理任务
              </el-descriptions-item>
              <el-descriptions-item label="徽章字段">
                pendingTaskCount (待处理任务数)
              </el-descriptions-item>
              <el-descriptions-item label="徽章条件">
                显示任务数 ≥ 3 的患者
              </el-descriptions-item>
            </el-descriptions>
          </el-card>

          <el-divider />

          <el-card class="demo-card">
            <template #header>
              <div class="card-header">
                <span>其他配置示例</span>
              </div>
            </template>
            
            <div class="config-examples">
              <h4>1. 医嘱签收场景</h4>
              <pre><code>pending-filter-label="仅显示待签收"
badge-field="unacknowledgedCount"
:badge-filter="(patient, value) => value > 0"</code></pre>

              <h4>2. 护理记录场景</h4>
              <pre><code>pending-filter-label="仅显示需记录"
badge-field="pendingRecordCount"
:badge-filter="(patient, value) => value > 0"</code></pre>

              <h4>3. 生命体征场景</h4>
              <pre><code>pending-filter-label="仅显示超时未测"
badge-field="overdueVitalSignCount"
:badge-filter="(patient, value) => value > 0"</code></pre>

              <h4>4. 紧急标记场景</h4>
              <pre><code>pending-filter-label="仅显示紧急患者"
badge-field="urgentCount"
:badge-filter="(patient, value) => value > 0 || patient.isUrgent"</code></pre>

              <h4>5. 完全自定义</h4>
              <pre><code>pending-filter-label="自定义筛选条件"
badge-field="customScore"
:badge-filter="(patient, value) => {
  // 复杂的自定义逻辑
  return value >= 80 && patient.status === 'active';
}"</code></pre>
            </div>
          </el-card>

          <el-divider />

          <div class="action-buttons">
            <el-button @click="changeConfig(1)" type="primary">
              切换配置1：待签收
            </el-button>
            <el-button @click="changeConfig(2)" type="success">
              切换配置2：待记录
            </el-button>
            <el-button @click="changeConfig(3)" type="warning">
              切换配置3：紧急标记
            </el-button>
          </div>
        </div>
      </div>
    </section>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue';
import { InfoFilled } from '@element-plus/icons-vue';
import PatientListPanel from '@/components/PatientListPanel.vue';
import PatientInfoBar from '@/components/PatientInfoBar.vue';
import { usePatientData } from '@/composables/usePatientData';

console.log('📋 医嘱测试页面（自定义配置）初始化...');

// 使用患者数据组合式函数
const {
  patientList,
  selectedPatients,
  enableMultiSelect,
  currentScheduledWardId,
  loading,
  initializePatientData,
  selectPatient,
  toggleMultiSelectMode
} = usePatientData();

// 排序方式
const sortBy = ref('time');

// 自定义患者列表（添加额外字段用于演示）
const customPatientList = computed(() => {
  return patientList.value.map(patient => ({
    ...patient,
    // 模拟额外的字段
    pendingTaskCount: Math.floor(Math.random() * 10),
    pendingRecordCount: Math.floor(Math.random() * 5),
    overdueVitalSignCount: Math.floor(Math.random() * 3),
    urgentCount: Math.floor(Math.random() * 2),
    customScore: Math.floor(Math.random() * 100),
    isUrgent: Math.random() > 0.8
  }));
});

// 自定义徽章过滤器（显示任务数 >= 3 的患者）
const customBadgeFilter = (patient, value) => {
  return value >= 3;
};

// 当前配置
const currentConfig = ref(1);

// 切换配置
const changeConfig = (configId) => {
  currentConfig.value = configId;
  console.log(`🔄 切换到配置 ${configId}`);
  // 实际应用中，这里可以动态修改 props 或重新加载组件
};

// 处理患者选择
const handlePatientSelect = (eventData) => {
  console.log('👤 选择患者事件:', eventData);
  const { patient, isMultiSelect } = eventData;
  selectPatient(patient, isMultiSelect);
};

// 处理多选模式切换
const handleMultiSelectToggle = (enabled) => {
  console.log('🔄 多选模式切换:', enabled);
  toggleMultiSelectMode(enabled);
};

// 处理排序变更
const handleSortChange = (newSortBy) => {
  console.log('📊 排序变更:', newSortBy);
  sortBy.value = newSortBy;
};

// 初始化数据
initializePatientData();
</script>

<style scoped>
.order-test-page {
  display: flex;
  height: 100vh;
  background-color: #f5f7fa;
}

.work-area {
  flex: 1;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.content-area {
  flex: 1;
  overflow-y: auto;
  padding: 20px;
}

.config-demo {
  max-width: 1200px;
  margin: 0 auto;
}

.config-demo h3 {
  font-size: 1.5rem;
  color: #303133;
  margin-bottom: 20px;
}

.demo-card {
  margin-bottom: 20px;
}

.card-header {
  font-weight: 600;
  color: #303133;
}

.config-examples {
  padding: 10px 0;
}

.config-examples h4 {
  font-size: 1rem;
  color: #606266;
  margin: 15px 0 10px 0;
}

.config-examples pre {
  background-color: #f5f7fa;
  border: 1px solid #e4e7ed;
  border-radius: 4px;
  padding: 12px;
  margin: 8px 0;
  overflow-x: auto;
}

.config-examples code {
  font-family: 'Consolas', 'Monaco', 'Courier New', monospace;
  font-size: 0.9rem;
  color: #303133;
  line-height: 1.6;
}

.action-buttons {
  display: flex;
  gap: 15px;
  flex-wrap: wrap;
  margin-top: 20px;
}

.action-buttons .el-button {
  flex: 1;
  min-width: 150px;
}

/* 未选中患者占位提示 */
.no-patient-bar {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 15px 25px;
  background: linear-gradient(135deg, #f0f9ff 0%, #e0f2fe 100%);
  border-left: 5px solid #409eff;
  font-size: 0.95rem;
  color: #606266;
  box-shadow: 0 2px 4px rgba(0,0,0,0.05);
}

.no-patient-bar .el-icon {
  font-size: 1.2rem;
  color: #409eff;
}
</style>
