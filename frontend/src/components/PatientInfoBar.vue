<template>
  <!-- ============================== 
    【患者信息栏组件】
    显示选中患者的详细信息，支持：
    - 单选模式：显示单个患者的完整信息
    - 多选模式：显示选中患者数量和床号列表
    - 排序控制（多选模式）
    ============================== -->
  <header class="patient-info-bar" v-if="patients.length > 0">
    <!-- 单选模式 -->
    <template v-if="!isMultiSelect && patients.length === 1">
      <div class="patient-badge">{{ patients[0].bedId }}</div>
      <div class="patient-details">
        <span class="name">{{ patients[0].patientName }}</span>
        <span class="meta">
          {{ patients[0].gender }} | {{ patients[0].age }}岁 
          <template v-if="patients[0].weight"> | {{ patients[0].weight }}kg</template>
        </span>
        <span class="tag">护理{{ patients[0].nursingGrade }}级</span>
      </div>
    </template>
    
    <!-- 多选模式 -->
    <template v-else-if="isMultiSelect && patients.length > 0">
      <div class="multi-patient-header">
        <!-- 选中患者数量 -->
        <div class="selected-count">
          <span class="count-badge">{{ patients.length }}</span>
          <span class="count-text">位患者</span>
        </div>
        
        <!-- 患者床号列表 -->
        <div class="patient-badges">
          <span 
            v-for="p in displayPatients" 
            :key="p.patientId"
            class="mini-badge"
            :title="`${p.bedId} ${p.patientName}`"
          >
            {{ p.bedId }}
          </span>
          <span v-if="patients.length > maxDisplayCount" class="more-badge">
            +{{ patients.length - maxDisplayCount }}
          </span>
        </div>
        
        <!-- 排序控制 -->
        <div class="sort-control" v-if="showSortControl">
          <span class="sort-label">排序:</span>
          <el-radio-group 
            :model-value="sortBy" 
            @update:model-value="handleSortChange"
            size="small" 
            class="sort-radio"
          >
            <el-radio-button label="time">时间</el-radio-button>
            <el-radio-button label="patient">患者</el-radio-button>
          </el-radio-group>
        </div>
      </div>
    </template>
  </header>
</template>

<script setup>
import { computed } from 'vue';

// ==================== Props ====================
const props = defineProps({
  // 选中的患者列表
  patients: {
    type: Array,
    default: () => []
  },
  // 是否多选模式
  isMultiSelect: {
    type: Boolean,
    default: false
  },
  // 多选模式下最多显示的患者数量
  maxDisplayCount: {
    type: Number,
    default: 5
  },
  // 是否显示排序控制
  showSortControl: {
    type: Boolean,
    default: true
  },
  // 当前排序方式
  sortBy: {
    type: String,
    default: 'time',
    validator: (value) => ['time', 'patient'].includes(value)
  }
});

// ==================== Emits ====================
const emit = defineEmits([
  'sort-change' // 排序方式变化事件
]);

// ==================== 计算属性 ====================

// 显示的患者列表（限制数量）
const displayPatients = computed(() => {
  return props.patients.slice(0, props.maxDisplayCount);
});

// ==================== 方法 ====================

// 处理排序变化
const handleSortChange = (value) => {
  emit('sort-change', value);
};
</script>

<style scoped>
/* ============================== 
  【样式说明】
  所有颜色、字体、间距都已提取为CSS变量
  可以通过外部覆盖这些变量来定制样式
============================== */

/* ==================== 全局变量 ==================== */
.patient-info-bar {
  --primary-color: #409eff;
  --bg-card: #ffffff;
  --text-primary: #303133;
  --text-regular: #606266;
  --text-secondary: #909399;
  --radius-small: 4px;
  --radius-round: 20px;

  display: flex;
  align-items: center;
  gap: 16px;
  padding: 15px 25px;
  background: var(--bg-card);
  border-bottom: 2px solid #f0f0f0;
  border-left: 5px solid var(--primary-color);
  flex-shrink: 0;
}

/* ==================== 单选模式 ==================== */

/* 床号标签 */
.patient-badge {
  background: var(--primary-color);
  color: white;
  padding: 8px 16px;
  border-radius: var(--radius-small);
  font-weight: bold;
  font-size: 1.1rem;
  flex-shrink: 0;
}

/* 患者详细信息 */
.patient-details {
  flex: 1;
  display: flex;
  align-items: center;
  gap: 15px;
}

.patient-details .name {
  font-size: 1.2rem;
  font-weight: bold;
  color: var(--text-primary);
}

.patient-details .meta {
  font-size: 0.95rem;
  color: var(--text-secondary);
}

.patient-details .tag {
  background: #e8f4ff;
  color: var(--primary-color);
  padding: 4px 12px;
  border-radius: var(--radius-round);
  font-size: 0.85rem;
}

/* ==================== 多选模式 ==================== */

.multi-patient-header {
  display: flex;
  align-items: center;
  gap: 20px;
  flex: 1;
}

/* 选中患者数量 */
.selected-count {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-shrink: 0;
}

.count-badge {
  background: linear-gradient(135deg, var(--primary-color) 0%, #66b1ff 100%);
  color: white;
  padding: 6px 14px;
  border-radius: 20px;
  font-weight: bold;
  font-size: 1.2rem;
  box-shadow: 0 3px 8px rgba(64, 158, 255, 0.3);
}

.count-text {
  font-size: 0.95rem;
  color: var(--text-regular);
  font-weight: 500;
}

/* 患者床号列表 */
.patient-badges {
  display: flex;
  align-items: center;
  gap: 6px;
  flex-wrap: wrap;
  flex: 1;
}

.mini-badge {
  background: #e8f4ff;
  color: var(--primary-color);
  padding: 4px 10px;
  border-radius: 6px;
  font-size: 0.85rem;
  font-weight: 600;
  border: 1px solid var(--primary-color);
  transition: all 0.2s;
  cursor: pointer;
}

.mini-badge:hover {
  background: var(--primary-color);
  color: white;
  transform: translateY(-1px);
  box-shadow: 0 2px 6px rgba(64, 158, 255, 0.3);
}

.more-badge {
  background: #f3f4f6;
  color: #6b7280;
  padding: 4px 10px;
  border-radius: 6px;
  font-size: 0.85rem;
  font-weight: 600;
}

/* 排序控制 */
.sort-control {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-left: auto;
  flex-shrink: 0;
}

.sort-label {
  font-size: 0.9rem;
  color: var(--text-regular);
  font-weight: 500;
}

.sort-radio :deep(.el-radio-button__inner) {
  padding: 6px 15px;
  font-size: 0.85rem;
}
</style>
