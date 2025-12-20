<template>
  <div class="nurse-dashboard">
    <!-- 顶部操作栏 -->
    <div class="dashboard-header">
      <div class="header-left">
        <h2>床位概览</h2>
        <el-breadcrumb separator="/">
          <el-breadcrumb-item>护士工作台</el-breadcrumb-item>
          <el-breadcrumb-item>床位概览</el-breadcrumb-item>
        </el-breadcrumb>
      </div>
      <div class="header-right">
        <el-select
          v-model="selectedWard"
          placeholder="选择病区"
          style="width: 200px; margin-right: 12px"
          @change="handleWardChange"
        >
          <el-option
            v-for="ward in wards"
            :key="ward.id"
            :label="ward.name"
            :value="ward.id"
          />
        </el-select>
        <el-button type="primary" :icon="Refresh" @click="loadData">刷新</el-button>
      </div>
    </div>

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

    <!-- 床位卡片网格 -->
    <div class="dashboard-beds">
      <el-card shadow="never">
        <template #header>
          <div class="card-header">
            <span>{{ overview.departmentName }} - {{ overview.wardName }}</span>
            <el-tag type="info">{{ beds.length }} 张床位</el-tag>
          </div>
        </template>

        <div v-loading="loading" class="beds-grid">
          <BedCard
            v-for="bed in beds"
            :key="bed.bedId"
            :bed="bed"
            @click="handleBedClick"
          />
        </div>

        <el-empty v-if="!loading && beds.length === 0" description="暂无床位数据" />
      </el-card>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted } from 'vue';
import { ElMessage } from 'element-plus';
import {
  Refresh,
  House,
  User,
  CircleCheck,
  DataAnalysis
} from '@element-plus/icons-vue';
import BedCard from '@/components/BedCard.vue';
import { getWardOverview } from '@/api/nursing';

// 数据状态
const loading = ref(false);
const selectedWard = ref('');
const beds = ref([]);
const overview = reactive({
  wardId: '',
  wardName: '',
  departmentId: '',
  departmentName: '',
  totalBeds: 0,
  occupiedBeds: 0,
  availableBeds: 0
});

// 病区列表（可以从后端获取，这里先硬编码）
const wards = ref([
  { id: 'IM-W01', name: '内科一病区' },
  { id: 'IM-W02', name: '内科二病区' },
  { id: 'SUR-W01', name: '外科病区' },
  { id: 'PED-W01', name: '儿科病区' }
]);

// 床位使用率
const bedOccupancyRate = computed(() => {
  if (overview.totalBeds === 0) return 0;
  return Math.round((overview.occupiedBeds / overview.totalBeds) * 100);
});

// 加载数据
const loadData = async () => {
  if (!selectedWard.value) {
    ElMessage.warning('请先选择病区');
    return;
  }

  loading.value = true;
  try {
    const data = await getWardOverview(selectedWard.value);
    console.log('API 响应数据:', data);
    
    if (!data) {
      throw new Error('未返回数据');
    }

    // 更新概览信息
    overview.wardId = data.wardId;
    overview.wardName = data.wardName;
    overview.departmentId = data.departmentId;
    overview.departmentName = data.departmentName;
    overview.totalBeds = data.totalBeds;
    overview.occupiedBeds = data.occupiedBeds;
    overview.availableBeds = data.availableBeds;

    // 更新床位列表
    beds.value = data.beds;

    ElMessage.success('数据加载成功');
  } catch (error) {
    console.error('加载病区概览失败:', error);
    console.error('错误详情:', error.response?.data);
    const errorMsg = error.response?.data?.message || error.response?.data?.error || '加载数据失败';
    ElMessage.error(errorMsg);
  } finally {
    loading.value = false;
  }
};

// 病区切换
const handleWardChange = () => {
  loadData();
};

// 床位卡片点击
const handleBedClick = (bed) => {
  if (bed.patient) {
    // 跳转到患者详情页（待实现）
    ElMessage.info(`查看患者：${bed.patient.name} (${bed.bedId})`);
    // router.push({ name: 'patient-detail', params: { id: bed.patient.id } });
  }
};

// 组件挂载
onMounted(() => {
  // 默认选择第一个病区
  if (wards.value.length > 0) {
    selectedWard.value = wards.value[0].id;
    loadData();
  }
});
</script>

<style scoped>
.nurse-dashboard {
  padding: 20px;
}

.dashboard-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
}

.header-left h2 {
  margin: 0 0 8px 0;
  font-size: 24px;
  color: #303133;
}

.header-right {
  display: flex;
  align-items: center;
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

.dashboard-beds {
  margin-top: 20px;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  font-size: 16px;
  font-weight: 600;
}

.beds-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
  gap: 16px;
  min-height: 200px;
}

@media (max-width: 768px) {
  .beds-grid {
    grid-template-columns: repeat(auto-fill, minmax(240px, 1fr));
  }
}
</style>
