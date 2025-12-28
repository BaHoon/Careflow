<template>
  <div class="nursing-record-list">
    <!-- 添加护理记录按钮（在Tab上方） -->
    <div class="add-record-bar">
      <el-button 
        type="default" 
        @click="handleViewRecentStats"
      >
        📊 查看近期情况
      </el-button>
      <el-button 
        type="primary" 
        @click="handleAddSupplementRecord"
        :icon="Plus"
      >
        添加护理记录
      </el-button>
    </div>

    <!-- Tab切换 -->
    <el-tabs v-model="activeTab" class="record-tabs">
      <!-- 待录入Tab -->
      <el-tab-pane name="pending">
        <template #label>
          <span class="tab-label">
            <el-icon color="#e6a23c"><Clock /></el-icon>
            <span>待录入 ({{ pendingRecords.length }})</span>
          </span>
        </template>
        
        
        <div class="record-list">
          <el-empty 
            v-if="pendingRecords.length === 0" 
            description="该患者暂无待录入的新护理记录"
            :image-size="100"
          />
          <div 
            v-for="record in pendingRecords" 
            :key="record.id"
            :class="['record-card', getRecordStatusClass(record)]"
          >
            <div class="record-header">
              <div class="record-time">
                <el-icon><Clock /></el-icon>
                <span class="time-text">{{ formatTime(record.plannedStartTime || record.scheduledTime) }}</span>
              </div>
              <el-tag 
                :type="getRecordTagType(record)" 
                size="small"
                effect="plain"
              >
                {{ getRecordStatusText(record) }}
              </el-tag>
            </div>
            
            <div class="record-body">
              <div class="record-info">
                <span class="record-type">{{ getRecordTypeDisplay(record) }}</span>
                <span v-if="record.description" class="record-desc">{{ record.description }}</span>
              </div>
              
              <!-- 负责护士 -->
              <div class="nurse-info" v-if="record.assignedNurseName || record.assignedNurseId">
                <span class="meta-label">负责护士:</span>
                <el-tag 
                  :type="isMyTask(record) ? 'primary' : 'info'" 
                  size="small"
                  effect="plain"
                >
                  {{ record.assignedNurseName || record.assignedNurseId }}
                  <span v-if="isMyTask(record)" style="margin-left: 4px;">(我)</span>
                </el-tag>
              </div>
              
              <!-- 延迟信息 -->
              <div v-if="record.delayMinutes !== undefined" class="delay-info">
                <el-tag 
                  v-if="record.excessDelayMinutes > 0" 
                  type="danger" 
                  size="small"
                  effect="dark"
                >
                  超时 {{ record.excessDelayMinutes }} 分钟
                </el-tag>
                <el-tag 
                  v-else-if="record.delayMinutes > -60 && record.delayMinutes < 0" 
                  type="warning" 
                  size="small"
                >
                  {{ Math.abs(record.delayMinutes) }} 分钟后到期
                </el-tag>
              </div>
            </div>
            
            <div class="record-actions">
              <el-button 
                type="primary" 
                size="default"
                @click="handleStartInput(record)"
                :icon="Edit"
              >
                开始录入
              </el-button>
              <el-button 
                type="danger" 
                plain
                size="default"
                @click="handleCancelTask(record)"
                :icon="Close"
              >
                取消任务
              </el-button>
            </div>
          </div>
        </div>
      </el-tab-pane>

      <!-- 已录入Tab -->
      <el-tab-pane name="completed">
        <template #label>
          <span class="tab-label">
            <el-icon color="#67c23a"><Check /></el-icon>
            <span>已录入 ({{ completedRecords.length }})</span>
          </span>
        </template>
        
        <div class="record-list">
          <el-empty 
            v-if="completedRecords.length === 0" 
            description="暂无已录入记录"
            :image-size="100"
          />
          <div 
            v-for="record in completedRecords" 
            :key="record.id"
            class="record-card completed-card"
          >
            <div class="record-header">
              <div class="record-time">
                <el-icon><Clock /></el-icon>
                <span class="time-text">{{ formatTime(record.plannedStartTime || record.scheduledTime) }}</span>
              </div>
              <el-tag type="success" size="small" effect="plain">
                已完成
              </el-tag>
            </div>
            
            <div class="record-body">
              <div class="record-info">
                <span class="record-type">{{ getRecordTypeDisplay(record) }}</span>
                <span v-if="record.description" class="record-desc">{{ record.description }}</span>
              </div>
              
              <div class="record-meta">
                <div class="meta-item" v-if="record.assignedNurseName || record.assignedNurseId">
                  <span class="meta-label">负责护士:</span>
                  <span class="meta-value">
                    {{ record.assignedNurseName || record.assignedNurseId }}
                    <span v-if="isMyTask(record)" style="color: #409eff;">(我)</span>
                  </span>
                </div>
                <div class="meta-item" v-if="record.actualStartTime || record.executeTime">
                  <span class="meta-label">录入时间:</span>
                  <span class="meta-value">{{ formatDateTime(record.actualStartTime || record.executeTime) }}</span>
                </div>
                <div class="meta-item" v-if="record.executorNurse">
                  <span class="meta-label">录入护士:</span>
                  <span class="meta-value">{{ record.executorNurse }}</span>
                </div>
              </div>
            </div>
            
            <div class="record-actions">
              <el-button 
                type="primary" 
                plain
                size="default"
                @click="handleViewDetail(record)"
                :icon="View"
              >
                查看详情
              </el-button>
            </div>
          </div>
        </div>
      </el-tab-pane>

      <!-- 已取消Tab -->
      <el-tab-pane name="cancelled">
        <template #label>
          <span class="tab-label">
            <el-icon color="#f56c6c"><Close /></el-icon>
            <span>已取消 ({{ cancelledRecords.length }})</span>
          </span>
        </template>
        
        <div class="record-list">
          <el-empty 
            v-if="cancelledRecords.length === 0" 
            description="暂无已取消记录"
            :image-size="100"
          />
          <div 
            v-for="record in cancelledRecords" 
            :key="record.id"
            class="record-card cancelled-card"
          >
            <div class="record-header">
              <div class="record-time">
                <el-icon><Clock /></el-icon>
                <span class="time-text">{{ formatTime(record.plannedStartTime || record.scheduledTime) }}</span>
              </div>
              <el-tag type="danger" size="small" effect="plain">
                已取消
              </el-tag>
            </div>
            
            <div class="record-body">
              <div class="record-info">
                <span class="record-type">{{ getRecordTypeDisplay(record) }}</span>
                <span v-if="record.description" class="record-desc">{{ record.description }}</span>
              </div>
              
              <div class="record-meta">
                <div class="meta-item" v-if="record.assignedNurseName || record.assignedNurseId">
                  <span class="meta-label">负责护士:</span>
                  <span class="meta-value">
                    {{ record.assignedNurseName || record.assignedNurseId }}
                    <span v-if="isMyTask(record)" style="color: #409eff;">(我)</span>
                  </span>
                </div>
                <div class="meta-item" v-if="record.executeTime">
                  <span class="meta-label">取消时间:</span>
                  <span class="meta-value">{{ formatDateTime(record.executeTime) }}</span>
                </div>
                <div class="meta-item" v-if="record.executorNurseName || record.executorNurseId || record.executorNurse">
                  <span class="meta-label">取消护士:</span>
                  <span class="meta-value">{{ record.executorNurseName || record.executorNurseId || record.executorNurse }}</span>
                </div>
                <div class="meta-item" v-if="record.cancelReason">
                  <span class="meta-label">取消理由:</span>
                  <span class="meta-value">{{ record.cancelReason }}</span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </el-tab-pane>
    </el-tabs>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue';
import { Clock, Check, Edit, View, Close, Plus } from '@element-plus/icons-vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { cancelNursingTask } from '@/api/nursing';
const props = defineProps({
  records: {
    type: Array,
    default: () => []
  },
  loading: {
    type: Boolean,
    default: false
  }
});

const emit = defineEmits(['start-input', 'view-detail', 'date-change', 'task-cancelled', 'add-supplement-record', 'view-recent-stats']);

// 获取当前登录护士ID
const getCurrentNurseId = () => {
  const userInfo = localStorage.getItem('userInfo');
  if (userInfo) {
    const user = JSON.parse(userInfo);
    return user.staffId;
  }
  return null;
};

const currentNurseId = ref(getCurrentNurseId());

// 当前激活的Tab
const activeTab = ref('pending');

// 当前选中日期
const selectedDate = ref(new Date().toISOString().split('T')[0]);

// 计算属性
const totalCount = computed(() => {
  const uniqueRecords = Array.from(
    new Map(props.records.map(r => [r.id, r])).values()
  );
  return uniqueRecords.length;
});

// 待录入记录（状态为 Pending = 3，从现在到未来排列）
const pendingRecords = computed(() => {
  // 先根据ID去重
  const uniqueRecords = Array.from(
    new Map(props.records.map(r => [r.id, r])).values()
  );
  
  return uniqueRecords
    .filter(r => r.status === 3) // Pending
    .sort((a, b) => new Date(a.plannedStartTime || a.scheduledTime) - new Date(b.plannedStartTime || b.scheduledTime));
});

// 已录入记录（状态为 Completed = 5，从现在到过去排列）
const completedRecords = computed(() => {
  // 先根据ID去重
  const uniqueRecords = Array.from(
    new Map(props.records.map(r => [r.id, r])).values()
  );
  
  return uniqueRecords
    .filter(r => r.status === 5) // Completed
    .sort((a, b) => {
      const timeA = new Date(b.actualStartTime || b.executeTime || b.plannedStartTime || b.scheduledTime);
      const timeB = new Date(a.actualStartTime || a.executeTime || a.plannedStartTime || a.scheduledTime);
      return timeA - timeB;
    });
});

// 已取消记录（状态为 Cancelled = 9，从现在到过去排列）
const cancelledRecords = computed(() => {
  // 先根据ID去重
  const uniqueRecords = Array.from(
    new Map(props.records.map(r => [r.id, r])).values()
  );
  
  return uniqueRecords
    .filter(r => r.status === 8 || r.status === 9 || r.status === 'Cancelled') // Incomplete(8) 或 PendingReturn(9) 或 Cancelled
    .sort((a, b) => {
      const timeA = new Date(b.executeTime || b.plannedStartTime || b.scheduledTime);
      const timeB = new Date(a.executeTime || a.plannedStartTime || a.scheduledTime);
      return timeA - timeB;
    });
});

// 方法
const handleDateChange = (value) => {
  emit('date-change', value);
};

const handleStartInput = (record) => {
  emit('start-input', record);
};

const handleViewDetail = (record) => {
  emit('view-detail', record);
};

const handleAddSupplementRecord = () => {
  emit('add-supplement-record');
};

const handleViewRecentStats = () => {
  emit('view-recent-stats');
};

// 取消任务
const handleCancelTask = async (record) => {
  try {
    // 弹出输入框要求填写取消理由
    const { value: cancelReason } = await ElMessageBox.prompt(
      '请填写取消任务的理由',
      '确认取消',
      {
        confirmButtonText: '确定',
        cancelButtonText: '取消',
        inputType: 'textarea',
        inputPlaceholder: '请输入取消理由...',
        inputValidator: (value) => {
          if (!value || value.trim().length === 0) {
            return '取消理由不能为空';
          }
          return true;
        },
      }
    );

    // 调用API取消任务
    await cancelNursingTask(record.id, currentNurseId.value, cancelReason);
    ElMessage.success('任务已取消');
    
    // 通知父组件刷新数据
    emit('task-cancelled', record.id);
  } catch (error) {
    if (error !== 'cancel') {
      console.error('取消任务失败:', error);
      ElMessage.error(error.response?.data?.message || '取消任务失败');
    }
  }
};

// 判断是否是我负责的任务
const isMyTask = (record) => {
  return record.assignedNurseId === currentNurseId.value;
};

const formatTime = (datetime) => {
  if (!datetime) return '';
  try {
    // 确保UTC时间字符串带有Z标识
    let utcString = datetime;
    if (!datetime.endsWith('Z') && !datetime.includes('+')) {
      utcString = datetime + 'Z';
    }
    const date = new Date(utcString);
    return date.toLocaleTimeString('zh-CN', { 
      hour: '2-digit', 
      minute: '2-digit',
      timeZone: 'Asia/Shanghai'
    });
  } catch {
    return datetime;
  }
};

const formatDateTime = (datetime) => {
  if (!datetime) return '';
  try {
    // 确保UTC时间字符串带有Z标识
    let utcString = datetime;
    if (!datetime.endsWith('Z') && !datetime.includes('+')) {
      utcString = datetime + 'Z';
    }
    const date = new Date(utcString);
    return date.toLocaleString('zh-CN', { 
      month: '2-digit', 
      day: '2-digit',
      hour: '2-digit', 
      minute: '2-digit',
      timeZone: 'Asia/Shanghai'
    });
  } catch {
    return datetime;
  }
};

// 获取记录状态样式类
const getRecordStatusClass = (record) => {
  if (record.excessDelayMinutes > 0) return 'overdue';
  if (record.delayMinutes > -60 && record.delayMinutes < 0) return 'due-soon';
  return 'normal';
};

// 获取记录标签类型
const getRecordTagType = (record) => {
  if (record.excessDelayMinutes > 0) return 'danger';
  if (record.delayMinutes > -60 && record.delayMinutes < 0) return 'warning';
  return 'info';
};

// 获取记录状态文本
const getRecordStatusText = (record) => {
  if (record.excessDelayMinutes > 0) return '已超时';
  if (record.delayMinutes > -60 && record.delayMinutes < 0) return '临期';
  return '待录入';
};

// 获取任务类型显示文本
const getRecordTypeDisplay = (record) => {
  // 首先检查taskType（来自新的Supplement补充检测）
  const taskType = record?.taskType;
  if (taskType === 'Routine') return '常规测量';
  if (taskType === 'Supplement') return '补充检测';
  if (taskType === 'ReMeasure') return '复测';
  
  // 然后检查category字段（来自后端API返回的数据）
  const category = record?.category;
  if (category === 'Routine') return '常规测量';
  if (category === 'Supplement') return '补充检测';
  if (category === 'ReMeasure') return '复测';
  
  // 默认显示复测
  return '复测';
};
</script>

<style scoped>
.nursing-record-list {
  height: 100%;
  display: flex;
  flex-direction: column;
  background: white;
}

/* 添加护理记录按钮栏 */
.add-record-bar {
  padding: 12px 20px;
  border-bottom: 1px solid #e4e7ed;
  background: #fafafa;
  display: flex;
  justify-content: flex-end;
  gap: 12px;
}

/* Tab容器 */
.record-tabs {
  flex: 1;
  display: flex;
  flex-direction: column;
  background: white;
  overflow: hidden;
}

.record-tabs :deep(.el-tabs__header) {
  margin: 0;
  background: white;
  border-bottom: 1px solid #e4e7ed;
  padding: 0 20px;
}

.record-tabs :deep(.el-tabs__nav-wrap::after) {
  display: none;
}

.record-tabs :deep(.el-tabs__item) {
  padding: 16px 20px;
  font-size: 15px;
  font-weight: 500;
  color: #606266;
  transition: all 0.3s;
}

.record-tabs :deep(.el-tabs__item:hover) {
  color: #409eff;
}

.record-tabs :deep(.el-tabs__item.is-active) {
  color: #409eff;
}

.record-tabs :deep(.el-tabs__active-bar) {
  height: 2px;
  background: #409eff;
}

.record-tabs :deep(.el-tabs__content) {
  flex: 1;
  overflow: hidden;
  padding: 0;
}

.record-tabs :deep(.el-tab-pane) {
  height: 100%;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.tab-label {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 15px;
}

.tab-subtitle {
  padding: 10px 20px;
  font-size: 13px;
  color: #909399;
  background: #fafafa;
  border-bottom: 1px solid #e4e7ed;
  flex-shrink: 0;
}

/* 记录列表 */
.record-list {
  flex: 1;
  overflow-y: auto;
  padding: 20px;
  display: flex;
  flex-direction: column;
  gap: 16px;
  background: white;
}

/* 记录卡片 */
.record-card {
  border: 1px solid #e4e7ed;
  border-radius: 10px;
  padding: 18px 20px;
  background: white;
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
  flex-shrink: 0;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.04);
}

.record-card:hover {
  box-shadow: 0 6px 16px rgba(0, 0, 0, 0.12);
  transform: translateY(-3px);
  border-color: #d0d7de;
}

.record-card.overdue {
  border-left: 5px solid #f56c6c;
  background: linear-gradient(to right, #fef0f0 0%, white 8%);
}

.record-card.due-soon {
  border-left: 5px solid #e6a23c;
  background: linear-gradient(to right, #fdf6ec 0%, white 8%);
}

.record-card.normal {
  border-left: 5px solid #409eff;
}

.record-card.completed-card {
  border-left: 5px solid #67c23a;
  background: linear-gradient(to right, #f0f9ff 0%, white 8%);
  opacity: 0.95;
}

/* 记录头部 */
.record-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 14px;
  padding-bottom: 10px;
  border-bottom: 1px solid #f0f0f0;
}

.record-time {
  display: flex;
  align-items: center;
  gap: 8px;
  color: #606266;
}

.time-text {
  font-size: 18px;
  font-weight: 600;
  color: #303133;
}

/* 记录主体 */
.record-body {
  margin-bottom: 14px;
}

.record-info {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 10px;
}

.record-type {
  font-size: 15px;
  font-weight: 600;
  color: #303133;
}

.record-desc {
  font-size: 13px;
  color: #909399;
}

/* 护士信息 */
.nurse-info {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-top: 8px;
  padding: 8px 12px;
  background: #f8f9fa;
  border-radius: 6px;
}

.nurse-info .meta-label {
  color: #909399;
  font-size: 13px;
  font-weight: 500;
}

.record-meta {
  display: flex;
  flex-direction: column;
  gap: 6px;
  font-size: 13px;
  color: #606266;
  background: #f8f9fa;
  padding: 8px 12px;
  border-radius: 6px;
}

.meta-item {
  display: flex;
  gap: 8px;
}

.meta-label {
  color: #909399;
  font-weight: 500;
}

.meta-value {
  color: #606266;
}

.delay-info {
  margin-top: 8px;
}

/* 记录操作 */
.record-actions {
  display: flex;
  justify-content: flex-end;
}

.record-actions .el-button {
  border-radius: 6px;
  font-weight: 500;
}

/* 已取消卡片样式 */
.cancelled-card {
  border-left: 4px solid #f56c6c;
  background: #fef0f0;
}

.cancelled-card:hover {
  box-shadow: 0 4px 12px rgba(245, 108, 108, 0.15);
}
</style>
