<template>
  <div class="nurse-dashboard">
    <!-- 顶部操作栏 -->
    <div class="dashboard-header">
      <div class="header-left">
        <h2>床位概览</h2>
        <el-breadcrumb separator="/">
          <el-breadcrumb-item>护士工作台</el-breadcrumb-item>
          <el-breadcrumb-item>床位概览</el-breadcrumb-item>
          <el-breadcrumb-item v-if="overview.departmentName">{{ overview.departmentName }}</el-breadcrumb-item>
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
            v-for="ward in availableWards"
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

    <!-- 床位卡片网格 - 按病区分组显示 -->
    <div class="dashboard-beds">
      <div v-if="wardGroups.length > 0">
        <el-card 
          v-for="wardGroup in wardGroups" 
          :key="wardGroup.wardId"
          shadow="never"
          style="margin-bottom: 20px"
        >
          <template #header>
            <div class="card-header">
              <span>{{ wardGroup.wardName }}</span>
              <el-tag type="info">{{ wardGroup.totalBeds }} 张床位</el-tag>
            </div>
          </template>

          <div v-loading="loading" class="beds-grid">
            <BedCard
              v-for="bed in wardGroup.beds"
              :key="bed.bedId"
              :bed="bed"
              @click="handleBedClick"
            />
          </div>

          <el-empty v-if="!loading && wardGroup.beds.length === 0" description="暂无床位数据" />
        </el-card>
      </div>
      
      <el-empty v-if="!loading && wardGroups.length === 0" description="暂无床位数据" />
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
const loading = ref(false);
const selectedWard = ref('');
const beds = ref([]);
const wardGroups = ref([]); // 病区分组数据
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
  { id: 'IM-W01', name: '内科一病区', deptCode: 'IM' },
  { id: 'IM-W02', name: '内科二病区', deptCode: 'IM' },
  { id: 'SUR-W01', name: '外科病区', deptCode: 'SUR' },
  { id: 'PED-W01', name: '儿科病区', deptCode: 'PED' }
]);

// 根据护士科室过滤病区
const availableWards = computed(() => {
  const nurseInfo = getCurrentNurse();
  if (!nurseInfo || !nurseInfo.deptCode) {
    return wards.value;
  }
  return wards.value.filter(w => w.deptCode === nurseInfo.deptCode);
});

// 床位使用率
const bedOccupancyRate = computed(() => {
  if (overview.totalBeds === 0) return 0;
  return Math.round((overview.occupiedBeds / overview.totalBeds) * 100);
});

// 加载数据
const loadData = async () => {
  const nurseInfo = getCurrentNurse();
  if (!nurseInfo) {
    ElMessage.error('未找到护士信息，请重新登录');
    router.push('/login');
    return;
  }

  // 清空旧数据，避免缓存
  beds.value = [];
  wardGroups.value = [];
  overview.departmentId = '';
  overview.departmentName = '';
  overview.wardId = '';
  overview.wardName = '';
  overview.totalBeds = 0;
  overview.occupiedBeds = 0;
  overview.availableBeds = 0;

  // 如果没有选择病区，使用科室ID加载该科室所有病区
  if (!selectedWard.value) {
    loading.value = true;
    try {
      const data = await getWardOverview(null, nurseInfo.deptCode);
      console.log('========== API 响应数据 ==========');
      console.log('完整数据:', data);
      console.log('wards数组:', data.wards);
      console.log('wards是否为数组:', Array.isArray(data.wards));
      if (data.wards && data.wards.length > 0) {
        console.log('第一个病区:', data.wards[0]);
        console.log('第一个病区的beds:', data.wards[0].beds);
      }
      console.log('==================================');
      
      if (!data) {
        throw new Error('未返回数据');
      }

      // 处理科室级别的数据（包含多个病区）
      if (data.wards && Array.isArray(data.wards)) {
        // 更新概览信息
        overview.departmentId = data.departmentId;
        overview.departmentName = data.departmentName;
        overview.totalBeds = data.totalBeds;
        overview.occupiedBeds = data.occupiedBeds;
        overview.availableBeds = data.availableBeds;

        // 设置病区分组数据
        wardGroups.value = data.wards;
        console.log('设置wardGroups.value为:', wardGroups.value);
        beds.value = []; // 清空单一床位列表
      } else {
        // 单病区数据（向后兼容）
        overview.wardId = data.wardId;
        overview.wardName = data.wardName;
        overview.departmentId = data.departmentId;
        overview.departmentName = data.departmentName;
        overview.totalBeds = data.totalBeds;
        overview.occupiedBeds = data.occupiedBeds;
        overview.availableBeds = data.availableBeds;

        beds.value = data.beds;
        wardGroups.value = []; // 清空分组数据
      }

      ElMessage.success('数据加载成功');
    } catch (error) {
      console.error('加载病区概览失败:', error);
      console.error('错误详情:', error.response?.data);
      const errorMsg = error.response?.data?.message || error.response?.data?.error || '加载数据失败';
      ElMessage.error(errorMsg);
    } finally {
      loading.value = false;
    }
  } else {
    // 选择了特定病区，加载该病区数据
    loading.value = true;
    try {
      const data = await getWardOverview(selectedWard.value);
      console.log('API 响应数据:', data);
      
      if (!data) {
        throw new Error('未返回数据');
      }

      // 更新概览信息（单病区）
      overview.wardId = data.wardId;
      overview.wardName = data.wardName;
      overview.departmentId = data.departmentId;
      overview.departmentName = data.departmentName;
      overview.totalBeds = data.totalBeds;
      overview.occupiedBeds = data.occupiedBeds;
      overview.availableBeds = data.availableBeds;

      // 更新床位列表
      beds.value = data.beds;
      
      // 转换为病区分组格式以保持一致
      wardGroups.value = [{
        wardId: data.wardId,
        wardName: data.wardName,
        beds: data.beds,
        totalBeds: data.totalBeds,
        occupiedBeds: data.occupiedBeds,
        availableBeds: data.availableBeds
      }];

      ElMessage.success('数据加载成功');
    } catch (error) {
      console.error('加载病区概览失败:', error);
      console.error('错误详情:', error.response?.data);
      const errorMsg = error.response?.data?.message || error.response?.data?.error || '加载数据失败';
      ElMessage.error(errorMsg);
    } finally {
      loading.value = false;
    }
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
  loadData();
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
