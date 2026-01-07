<template>
  <el-collapse v-model="activeNames" class="summary-card medical-orders">
    <el-collapse-item :name="date">
      <template #title>
        <div class="card-header">
          <span class="icon">💊</span>
          <span class="title">医嘱执行</span>
          <el-tag type="info" size="small">{{ summary.totalCount }} 条</el-tag>
        </div>
      </template>
      
      <div class="card-content">
        <div 
          v-for="record in summary.records" 
          :key="record.orderId"
          class="order-item"
          @click="handleOrderClick(record.orderId)"
        >
          <div class="order-header">
            <el-tag :type="getOrderTypeColor(record.orderType)" size="small">
              {{ getOrderTypeName(record.orderType) }}
            </el-tag>
            <el-tag :type="record.isLongTerm ? 'primary' : 'warning'" size="small">
              {{ record.isLongTerm ? '长期' : '临时' }}
            </el-tag>
            <span class="order-id">#{{ record.orderId }}</span>
            <span class="order-content">{{ formatOrderContent(record) }}</span>
          </div>
          
          <div class="task-list">
            <div 
              v-for="task in record.tasks" 
              :key="task.id"
              class="task-item"
            >
              <span class="task-id">#{{ task.id }}</span>
              <span class="task-title">{{ getTaskTitle(task) }}</span>
              <span class="task-separator">|</span>
              <el-icon><Clock /></el-icon>
              <span class="time">{{ formatTime(task.actualStartTime) }}</span>
              <span v-if="task.executorName" class="executor">执行: {{ task.executorName }}</span>
              <el-tag 
                v-if="getTaskStatusText(task.status) !== '未知'"
                :type="getTaskStatusColor(task.status)" 
                size="small"
                class="status-tag"
              >
                {{ getTaskStatusText(task.status) }}
              </el-tag>
            </div>
          </div>
        </div>
      </div>
    </el-collapse-item>
  </el-collapse>
</template>

<script setup>
import { ref } from 'vue';
import { Clock } from '@element-plus/icons-vue';

const props = defineProps({
  summary: {
    type: Object,
    required: true
  },
  date: {
    type: String,
    required: true
  }
});

const emit = defineEmits(['order-click']);

// 默认展开今天的卡片
const activeNames = ref([]);
const today = new Date().toISOString().split('T')[0];
if (props.date === today) {
  activeNames.value = [props.date];
}

// 处理医嘱点击
const handleOrderClick = (orderId) => {
  emit('order-click', orderId, props.date);
};

// 获取医嘱类型颜色
const getOrderTypeColor = (type) => {
  const colorMap = {
    'MedicationOrder': 'primary',
    'InspectionOrder': 'success',
    'OperationOrder': 'warning',
    'SurgicalOrder': 'danger',
    'DischargeOrder': 'info'
  };
  return colorMap[type] || 'info';
};

// 获取医嘱类型名称
const getOrderTypeName = (type) => {
  const nameMap = {
    'MedicationOrder': '药品',
    'InspectionOrder': '检查',
    'OperationOrder': '操作',
    'SurgicalOrder': '手术',
    'DischargeOrder': '出院'
  };
  return nameMap[type] || '其他';
};

// 获取任务状态颜色（与OrderDetailPanel保持一致）
const getTaskStatusColor = (status) => {
  const statusMap = {
    0: 'info',      // 待申请
    1: 'info',      // 已申请
    2: 'primary',   // 已确认
    3: 'warning',   // 待执行
    4: 'primary',   // 进行中
    5: 'success',   // 已完成
    6: 'warning',   // 停止锁定
    7: 'info',      // 已停止
    8: 'danger',    // 异常
    9: 'danger',    // 待退药
    10: 'danger'    // 异常取消待退药
  };
  return statusMap[status] || 'info';
};

// 获取任务状态文本（与OrderDetailPanel保持一致）
const getTaskStatusText = (status) => {
  const textMap = {
    0: '待申请',
    1: '已申请',
    2: '已确认',
    3: '待执行',
    4: '进行中',
    5: '已完成',
    6: '停止锁定',
    7: '已停止',
    8: '异常',
    9: '待退药',
    10: '异常取消待退药'
  };
  return textMap[status] || `状态${status}`;
};

// 格式化时间
const formatTime = (timeStr) => {
  if (!timeStr) return '--:--';
  const date = new Date(timeStr);
  return date.toLocaleTimeString('zh-CN', {
    hour: '2-digit',
    minute: '2-digit'
  });
};

// 格式化医嘱内容（参考护士端医嘱查询）
const formatOrderContent = (record) => {
  // 如果是出院医嘱，显示特殊格式
  if (record.orderType === 'DischargeOrder') {
    const dischargeTime = record.plantEndTime || record.createTime;
    return `出院医嘱-预计出院时间: ${formatDateTime(dischargeTime)}`;
  }
  // 其他医嘱使用orderContent字段
  return record.orderContent || record.summary || '医嘱详情';
};

// 格式化完整日期时间
const formatDateTime = (dateTimeString) => {
  if (!dateTimeString) return '--';
  
  try {
    let utcString = dateTimeString;
    if (!dateTimeString.endsWith('Z') && !dateTimeString.includes('+')) {
      utcString = dateTimeString + 'Z';
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
  } catch (error) {
    return dateTimeString;
  }
};

// 解析任务的DataPayload获取标题
const getTaskTitle = (task) => {
  if (!task.dataPayload) {
    return '执行任务';
  }
  
  try {
    const payload = JSON.parse(task.dataPayload);
    // 优先使用Title字段，如果没有则使用TaskType或默认值
    return payload.Title || payload.title || payload.TaskType || '执行任务';
  } catch (error) {
    return '执行任务';
  }
};
</script>

<style scoped lang="scss">
.summary-card {
  margin-bottom: 16px;
  border-radius: 8px;
  overflow: hidden;
  background: #ffffff;
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.08);
  
  :deep(.el-collapse-item__header) {
    background: #ffffff;
    padding: 16px 20px;
    border: none;
    font-size: 16px;
    font-weight: 600;
    transition: all 0.3s;
    
    &:hover {
      background: #f5f7fa;
    }
  }
  
  :deep(.el-collapse-item__wrap) {
    border: none;
  }
  
  :deep(.el-collapse-item__content) {
    padding: 0;
  }
  
  .card-header {
    display: flex;
    align-items: center;
    gap: 12px;
    width: 100%;
    
    .icon {
      font-size: 22px;
    }
    
    .title {
      font-size: 16px;
      font-weight: 600;
      color: #303133;
    }
  }
  
  .card-content {
    padding: 0 20px 20px;
    
    .order-item {
      background: #f9fafb;
      border-radius: 8px;
      padding: 14px;
      margin-bottom: 12px;
      cursor: pointer;
      transition: all 0.3s;
      border: 1px solid transparent;
      
      &:hover {
        background: #e6f7ff;
        border-color: #409eff;
        transform: translateX(4px);
        box-shadow: 0 2px 8px rgba(64, 158, 255, 0.2);
      }
      
      &:last-child {
        margin-bottom: 0;
      }
      
      .order-header {
        display: flex;
        align-items: center;
        gap: 10px;
        margin-bottom: 10px;
        flex-wrap: wrap;
        
        .order-id {
          font-size: 0.85rem;
          font-weight: 700;
          color: #409eff;
          background: #ecf5ff;
          padding: 2px 8px;
          border-radius: 4px;
          font-family: 'Courier New', monospace;
        }
        
        .order-content {
          font-weight: 600;
          color: #303133;
          font-size: 15px;
          flex: 1;
          min-width: 150px;
        }
        
        .order-spec {
          color: #909399;
          font-size: 13px;
        }
      }
      
      .task-list {
        .task-item {
          display: flex;
          align-items: center;
          gap: 10px;
          font-size: 13px;
          color: #606266;
          padding: 6px 0;
          border-top: 1px solid #e4e7ed;
          
          &:first-child {
            border-top: none;
            padding-top: 8px;
          }
          
          .task-id {
            font-size: 0.8rem;
            font-weight: 700;
            color: #67c23a;
            background: #f0f9ff;
            padding: 2px 6px;
            border-radius: 3px;
            font-family: 'Courier New', monospace;
            flex-shrink: 0;
          }
          
          .task-title {
            font-weight: 600;
            color: #303133;
            font-size: 14px;
            flex-shrink: 0;
            max-width: 200px;
            overflow: hidden;
            text-overflow: ellipsis;
            white-space: nowrap;
          }
          
          .task-separator {
            color: #dcdfe6;
            font-weight: normal;
          }
          
          .el-icon {
            color: #909399;
          }
          
          .time {
            font-weight: 600;
            color: #409eff;
            min-width: 50px;
          }
          
          .executor {
            color: #606266;
            flex: 1;
          }
          
          .status-tag {
            margin-left: auto;
          }
        }
      }
    }
  }
}

// 医嘱类型特定样式
.medical-orders {
  .card-header .icon {
    filter: drop-shadow(0 2px 4px rgba(64, 158, 255, 0.3));
  }
}
</style>
