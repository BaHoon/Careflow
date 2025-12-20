<template>
  <div 
    class="bed-card" 
    :class="[bedStatusClass, { 'has-patient': bed.patient }]"
    @click="handleCardClick"
  >
    <!-- 床号标题 -->
    <div class="bed-header">
      <div class="bed-number">{{ bedNumber }}</div>
      <div v-if="!bed.patient" class="bed-status-text">空闲</div>
    </div>

    <!-- 患者信息 -->
    <div v-if="bed.patient" class="patient-info">
      <div class="patient-name">{{ bed.patient.name }}</div>
      <div class="patient-details">
        <el-tag size="small" :type="genderTagType">{{ bed.patient.gender }}</el-tag>
        <span class="patient-age">{{ bed.patient.age }}岁</span>
      </div>
      
      <!-- 护理等级标签 -->
      <div class="nursing-grade">
        <el-tag 
          size="small" 
          :color="nursingGradeColor" 
          effect="dark"
        >
          {{ nursingGradeText }}
        </el-tag>
      </div>
    </div>

    <!-- 状态角标区域 -->
    <div v-if="bed.patient && hasAnyStatus" class="status-badges">
      <StatusBadge
        v-if="bed.statusFlags.hasOverdueTask"
        type="overdue"
        icon="WarningFilled"
        text="超时"
      />
      <StatusBadge
        v-if="bed.statusFlags.hasSurgeryToday"
        type="surgery"
        icon="Knife"
        text="待手术"
      />
      <StatusBadge
        v-if="bed.statusFlags.hasAbnormalVitalSign"
        type="abnormal"
        icon="Warning"
        text="体征异常"
      />
      <StatusBadge
        v-if="bed.statusFlags.hasNewOrder"
        type="new-order"
        icon="Document"
        text="新医嘱"
      />
      <StatusBadge
        v-if="bed.statusFlags.hasPendingTask && !bed.statusFlags.hasOverdueTask"
        type="pending"
        icon="Clock"
        text="待执行"
      />
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue';
import StatusBadge from './StatusBadge.vue';

const props = defineProps({
  bed: {
    type: Object,
    required: true
  }
});

const emit = defineEmits(['click']);

// 床号简化显示（去除病区前缀）
const bedNumber = computed(() => {
  const parts = props.bed.bedId.split('-');
  return parts.length >= 3 ? parts[parts.length - 1] : props.bed.bedId;
});

// 床位状态样式类
const bedStatusClass = computed(() => {
  if (!props.bed.patient) return 'status-empty';
  
  // 根据护理等级返回不同的边框颜色
  const gradeMap = {
    0: 'status-special',  // 特级 - 红色
    1: 'status-grade1',   // 一级 - 橙色
    2: 'status-grade2',   // 二级 - 黄色
    3: 'status-grade3'    // 三级 - 绿色
  };
  
  return gradeMap[props.bed.patient.nursingGrade] || 'status-normal';
});

// 性别标签类型
const genderTagType = computed(() => {
  return props.bed.patient?.gender === '男' ? 'primary' : 'danger';
});

// 护理等级文本
const nursingGradeText = computed(() => {
  if (!props.bed.patient) return '';
  
  const gradeMap = {
    0: '特级护理',
    1: '一级护理',
    2: '二级护理',
    3: '三级护理'
  };
  
  return gradeMap[props.bed.patient.nursingGrade] || '未知';
});

// 护理等级颜色
const nursingGradeColor = computed(() => {
  if (!props.bed.patient) return '#909399';
  
  const colorMap = {
    0: '#f56c6c',  // 特级 - 红色
    1: '#e6a23c',  // 一级 - 橙色
    2: '#f7ba2a',  // 二级 - 黄色
    3: '#67c23a'   // 三级 - 绿色
  };
  
  return colorMap[props.bed.patient.nursingGrade] || '#909399';
});

// 是否有任何状态标识
const hasAnyStatus = computed(() => {
  const flags = props.bed.statusFlags;
  return flags.hasOverdueTask || 
         flags.hasSurgeryToday || 
         flags.hasAbnormalVitalSign || 
         flags.hasNewOrder || 
         flags.hasPendingTask;
});

// 卡片点击事件
const handleCardClick = () => {
  if (props.bed.patient) {
    emit('click', props.bed);
  }
};
</script>

<style scoped>
.bed-card {
  background: #fff;
  border-radius: 8px;
  padding: 16px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
  transition: all 0.3s ease;
  border: 2px solid transparent;
  cursor: pointer;
  position: relative;
  min-height: 150px;
}

.bed-card:hover {
  transform: translateY(-4px);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
}

.bed-card.has-patient {
  cursor: pointer;
}

/* 床位状态边框颜色 */
.status-empty {
  border-color: #dcdfe6;
  background: #f5f7fa;
  cursor: default;
}

.status-special {
  border-color: #f56c6c;
}

.status-grade1 {
  border-color: #e6a23c;
}

.status-grade2 {
  border-color: #f7ba2a;
}

.status-grade3 {
  border-color: #67c23a;
}

/* 床位头部 */
.bed-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 12px;
}

.bed-number {
  font-size: 18px;
  font-weight: bold;
  color: #303133;
}

.bed-status-text {
  font-size: 12px;
  color: #909399;
}

/* 患者信息 */
.patient-info {
  margin-bottom: 12px;
}

.patient-name {
  font-size: 16px;
  font-weight: 600;
  color: #303133;
  margin-bottom: 8px;
}

.patient-details {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 8px;
}

.patient-age {
  font-size: 14px;
  color: #606266;
}

.nursing-grade {
  margin-top: 8px;
}

/* 状态角标区域 */
.status-badges {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
  margin-top: 12px;
  padding-top: 12px;
  border-top: 1px solid #ebeef5;
}
</style>
