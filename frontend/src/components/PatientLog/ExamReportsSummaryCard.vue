<template>
  <el-collapse v-model="activeNames" class="summary-card exam-reports">
    <el-collapse-item :name="date">
      <template #title>
        <div class="card-header">
          <span class="icon">🔬</span>
          <span class="title">检查报告</span>
          <el-tag type="info" size="small">{{ summary.totalCount }} 份</el-tag>
        </div>
      </template>
      
      <div class="card-content">
        <div 
          v-for="report in summary.reports" 
          :key="report.id"
          class="report-item"
          @click="handleReportClick(report.id)"
        >
          <div class="report-header">
            <div class="item-info">
              <span class="item-name">{{ report.itemName }}</span>
              <el-tag 
                :type="getReportStatusColor(report.reportStatus)" 
                size="small"
              >
                {{ getReportStatusText(report.reportStatus) }}
              </el-tag>
            </div>
            <div class="time-info">
              <el-icon><Clock /></el-icon>
              <span class="time">{{ formatDateTime(report.reportTime) }}</span>
            </div>
          </div>
          
          <div v-if="report.findings" class="report-section">
            <div class="section-title">
              <el-icon><Document /></el-icon>
              检查所见
            </div>
            <div class="section-content">{{ report.findings }}</div>
          </div>
          
          <div v-if="report.impression" class="report-section impression">
            <div class="section-title">
              <el-icon><Check /></el-icon>
              诊断结论
            </div>
            <div class="section-content">{{ report.impression }}</div>
          </div>
          
          <div v-if="report.reviewerName" class="report-footer">
            <span class="reviewer-label">审核医生:</span>
            <span class="reviewer-name">{{ report.reviewerName }}</span>
          </div>
        </div>
      </div>
    </el-collapse-item>
  </el-collapse>
</template>

<script setup>
import { ref } from 'vue';
import { Clock, Document, Check } from '@element-plus/icons-vue';

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

const emit = defineEmits(['report-click']);

// 默认展开今天的卡片
const activeNames = ref([]);
const today = new Date().toISOString().split('T')[0];
if (props.date === today) {
  activeNames.value = [props.date];
}

// 处理报告点击
const handleReportClick = (reportId) => {
  emit('report-click', reportId, props.date);
};

// 获取报告状态颜色
const getReportStatusColor = (status) => {
  const statusMap = {
    'Pending': 'warning',
    'Completed': 'success',
    'Reviewed': 'primary',
    'Cancelled': 'info'
  };
  return statusMap[status] || 'info';
};

// 获取报告状态文本
const getReportStatusText = (status) => {
  const textMap = {
    'Pending': '待报告',
    'Completed': '已完成',
    'Reviewed': '已审核',
    'Cancelled': '已取消'
  };
  return textMap[status] || status;
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
  }
  
  .card-content {
    padding: 0 20px 20px;
    
    .report-item {
      background: #f9fafb;
      border-radius: 8px;
      padding: 16px;
      margin-bottom: 12px;
      cursor: pointer;
      transition: all 0.3s;
      border: 1px solid transparent;
      
      &:hover {
        background: #e6f7ff;
        border-color: #409eff;
        transform: translateY(-2px);
        box-shadow: 0 4px 12px rgba(64, 158, 255, 0.25);
      }
      
      &:last-child {
        margin-bottom: 0;
      }
      
      .report-header {
        display: flex;
        justify-content: space-between;
        align-items: flex-start;
        margin-bottom: 12px;
        flex-wrap: wrap;
        gap: 10px;
        
        .item-info {
          display: flex;
          align-items: center;
          gap: 10px;
          
          .item-name {
            font-size: 16px;
            font-weight: 600;
            color: #303133;
          }
        }
        
        .time-info {
          display: flex;
          align-items: center;
          gap: 6px;
          font-size: 14px;
          color: #606266;
          
          .el-icon {
            color: #909399;
          }
          
          .time {
            font-weight: 500;
          }
        }
      }
      
      .report-section {
        background: #ffffff;
        border-radius: 6px;
        padding: 12px;
        margin-bottom: 10px;
        border-left: 3px solid #409eff;
        
        &.impression {
          border-left-color: #67c23a;
          
          .section-title {
            color: #67c23a;
          }
        }
        
        &:last-of-type {
          margin-bottom: 12px;
        }
        
        .section-title {
          display: flex;
          align-items: center;
          gap: 6px;
          font-size: 13px;
          font-weight: 600;
          color: #409eff;
          margin-bottom: 8px;
          
          .el-icon {
            font-size: 14px;
          }
        }
        
        .section-content {
          font-size: 14px;
          line-height: 1.6;
          color: #303133;
          white-space: pre-wrap;
          word-wrap: break-word;
        }
      }
      
      .report-footer {
        display: flex;
        align-items: center;
        gap: 8px;
        font-size: 13px;
        padding-top: 10px;
        border-top: 1px solid #e4e7ed;
        
        .reviewer-label {
          color: #909399;
        }
        
        .reviewer-name {
          color: #303133;
          font-weight: 600;
        }
      }
    }
  }
}

// 检查报告特定样式
.exam-reports {
  .card-header .icon {
    filter: drop-shadow(0 2px 4px rgba(230, 162, 60, 0.3));
  }
}
</style>
