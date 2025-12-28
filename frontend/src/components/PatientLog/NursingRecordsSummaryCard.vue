<template>
  <el-collapse v-model="activeNames" class="summary-card nursing-records">
    <el-collapse-item :name="date">
      <template #title>
        <div class="card-header">
          <span class="icon">📋</span>
          <span class="title">护理记录</span>
          <el-tag type="info" size="small">{{ summary.totalCount }} 条</el-tag>
          <el-tag 
            v-if="summary.hasAbnormal" 
            type="danger" 
            size="small"
            effect="dark"
            class="abnormal-tag"
          >
            <el-icon><Warning /></el-icon>
            异常
          </el-tag>
        </div>
      </template>
      
      <div class="card-content">
        <!-- 异常提示 -->
        <div v-if="summary.hasAbnormal && summary.abnormalDescriptions?.length" class="abnormal-alerts">
          <el-alert
            v-for="(desc, index) in summary.abnormalDescriptions"
            :key="index"
            :title="desc"
            type="warning"
            :closable="false"
            show-icon
          />
        </div>
        
        <!-- 生命体征记录列表 -->
        <div 
          v-for="record in summary.records" 
          :key="record.id"
          class="record-item"
          :class="{ 'has-abnormal': record.isAbnormal }"
          @click="handleRecordClick(record.id)"
        >
          <div class="record-header">
            <el-icon><Clock /></el-icon>
            <span class="time">{{ formatDateTime(record.recordTime) }}</span>
            <span v-if="record.recorderNurseName" class="recorder">
              记录: {{ record.recorderNurseName }}
            </span>
            <el-tag 
              v-if="record.isAbnormal" 
              type="danger" 
              size="small"
              effect="dark"
            >
              异常
            </el-tag>
          </div>
          
          <div class="vital-signs">
            <div 
              class="vital-item" 
              :class="{ 'abnormal': isVitalAbnormal(record, 'temperature') }"
            >
              <span class="label">体温</span>
              <span class="value">{{ record.temperature || '--' }}</span>
              <span class="unit">°C</span>
            </div>
            
            <div 
              class="vital-item" 
              :class="{ 'abnormal': isVitalAbnormal(record, 'pulse') }"
            >
              <span class="label">脉搏</span>
              <span class="value">{{ record.pulse || '--' }}</span>
              <span class="unit">次/分</span>
            </div>
            
            <div 
              class="vital-item" 
              :class="{ 'abnormal': isVitalAbnormal(record, 'bloodPressure') }"
            >
              <span class="label">血压</span>
              <span class="value">
                {{ record.sysBp || '--' }}/{{ record.diaBp || '--' }}
              </span>
              <span class="unit">mmHg</span>
            </div>
            
            <div 
              class="vital-item" 
              :class="{ 'abnormal': isVitalAbnormal(record, 'spo2') }"
            >
              <span class="label">血氧</span>
              <span class="value">{{ record.spo2 || '--' }}</span>
              <span class="unit">%</span>
            </div>
          </div>
          
          <!-- 异常项提示 -->
          <div v-if="record.isAbnormal && record.abnormalItems?.length" class="abnormal-items">
            <el-tag
              v-for="item in record.abnormalItems"
              :key="item"
              type="danger"
              size="small"
              effect="plain"
            >
              {{ getAbnormalItemName(item) }}
            </el-tag>
          </div>
        </div>
      </div>
    </el-collapse-item>
  </el-collapse>
</template>

<script setup>
import { ref } from 'vue';
import { Clock, Warning } from '@element-plus/icons-vue';

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

const emit = defineEmits(['record-click']);

// 默认展开今天的卡片
const activeNames = ref([]);
const today = new Date().toISOString().split('T')[0];
if (props.date === today) {
  activeNames.value = [props.date];
}

// 处理记录点击
const handleRecordClick = (recordId) => {
  emit('record-click', recordId, props.date);
};

// 判断某个生命体征是否异常
const isVitalAbnormal = (record, vitalType) => {
  if (!record.isAbnormal || !record.abnormalItems?.length) {
    return false;
  }
  
  const abnormalMap = {
    'temperature': 'Temperature',
    'pulse': 'Pulse',
    'bloodPressure': 'BloodPressure',
    'spo2': 'SpO2'
  };
  
  return record.abnormalItems.includes(abnormalMap[vitalType]);
};

// 获取异常项中文名
const getAbnormalItemName = (item) => {
  const nameMap = {
    'Temperature': '体温异常',
    'Pulse': '脉搏异常',
    'BloodPressure': '血压异常',
    'SpO2': '血氧异常'
  };
  return nameMap[item] || item;
};

// 格式化日期时间
const formatDateTime = (timeStr) => {
  if (!timeStr) return '--';
  const date = new Date(timeStr);
  return date.toLocaleString('zh-CN', {
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit'
  });
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
    
    .abnormal-tag {
      margin-left: 4px;
      animation: pulse 1.5s ease-in-out infinite;
      
      .el-icon {
        margin-right: 4px;
      }
    }
  }
  
  .card-content {
    padding: 0 20px 20px;
    
    .abnormal-alerts {
      margin-bottom: 16px;
      
      .el-alert {
        margin-bottom: 8px;
        
        &:last-child {
          margin-bottom: 0;
        }
      }
    }
    
    .record-item {
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
        box-shadow: 0 2px 8px rgba(64, 158, 255, 0.2);
      }
      
      &.has-abnormal {
        border-color: #f56c6c;
        background: #fef0f0;
        
        &:hover {
          background: #fde2e2;
          border-color: #f56c6c;
          box-shadow: 0 2px 8px rgba(245, 108, 108, 0.3);
        }
      }
      
      &:last-child {
        margin-bottom: 0;
      }
      
      .record-header {
        display: flex;
        align-items: center;
        gap: 10px;
        margin-bottom: 12px;
        font-size: 14px;
        
        .el-icon {
          color: #909399;
        }
        
        .time {
          font-weight: 600;
          color: #303133;
        }
        
        .recorder {
          color: #606266;
          flex: 1;
        }
      }
      
      .vital-signs {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(120px, 1fr));
        gap: 12px;
        
        .vital-item {
          display: flex;
          align-items: baseline;
          gap: 6px;
          padding: 8px 12px;
          background: #ffffff;
          border-radius: 6px;
          border: 1px solid #e4e7ed;
          transition: all 0.3s;
          
          &.abnormal {
            background: #fef0f0;
            border-color: #f56c6c;
            
            .value {
              color: #f56c6c;
              font-weight: 700;
              animation: shake 0.5s;
            }
          }
          
          .label {
            font-size: 13px;
            color: #909399;
          }
          
          .value {
            font-size: 16px;
            font-weight: 600;
            color: #303133;
          }
          
          .unit {
            font-size: 12px;
            color: #909399;
          }
        }
      }
      
      .abnormal-items {
        margin-top: 12px;
        padding-top: 12px;
        border-top: 1px solid #f56c6c;
        display: flex;
        flex-wrap: wrap;
        gap: 8px;
      }
    }
  }
}

// 护理记录特定样式
.nursing-records {
  .card-header .icon {
    filter: drop-shadow(0 2px 4px rgba(103, 194, 58, 0.3));
  }
}

// 动画效果
@keyframes pulse {
  0%, 100% {
    opacity: 1;
    transform: scale(1);
  }
  50% {
    opacity: 0.8;
    transform: scale(1.05);
  }
}

@keyframes shake {
  0%, 100% {
    transform: translateX(0);
  }
  25% {
    transform: translateX(-4px);
  }
  75% {
    transform: translateX(4px);
  }
}
</style>
