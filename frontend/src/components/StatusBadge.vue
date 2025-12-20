<template>
  <div class="status-badge" :class="badgeClass">
    <el-icon :size="iconSize">
      <component :is="iconComponent" />
    </el-icon>
    <span v-if="text" class="badge-text">{{ text }}</span>
  </div>
</template>

<script setup>
import { computed } from 'vue';
import {
  WarningFilled,
  Warning,
  Clock,
  Document,
  VideoCamera
} from '@element-plus/icons-vue';

const props = defineProps({
  type: {
    type: String,
    required: true,
    validator: (value) => {
      return ['overdue', 'surgery', 'abnormal', 'new-order', 'pending'].includes(value);
    }
  },
  icon: {
    type: String,
    default: ''
  },
  text: {
    type: String,
    default: ''
  },
  size: {
    type: String,
    default: 'small',
    validator: (value) => ['small', 'medium', 'large'].includes(value)
  }
});

// 图标映射
const iconMap = {
  'WarningFilled': WarningFilled,
  'Warning': Warning,
  'Clock': Clock,
  'Document': Document,
  'Knife': VideoCamera // 使用 VideoCamera 作为替代，或者使用其他图标
};

// 图标组件
const iconComponent = computed(() => {
  return iconMap[props.icon] || WarningFilled;
});

// 图标尺寸
const iconSize = computed(() => {
  const sizeMap = {
    small: 14,
    medium: 16,
    large: 18
  };
  return sizeMap[props.size] || 14;
});

// 徽章样式类
const badgeClass = computed(() => {
  const classes = [`badge-${props.type}`, `badge-size-${props.size}`];
  
  // 超时任务添加闪烁动画
  if (props.type === 'overdue') {
    classes.push('badge-blink');
  }
  
  return classes;
});
</script>

<style scoped>
.status-badge {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  padding: 4px 8px;
  border-radius: 4px;
  font-size: 12px;
  font-weight: 500;
  white-space: nowrap;
}

.badge-text {
  line-height: 1;
}

/* 尺寸变体 */
.badge-size-small {
  padding: 3px 6px;
  font-size: 11px;
}

.badge-size-medium {
  padding: 4px 8px;
  font-size: 12px;
}

.badge-size-large {
  padding: 5px 10px;
  font-size: 13px;
}

/* 类型样式 */
.badge-overdue {
  background: #fef0f0;
  color: #f56c6c;
  border: 1px solid #fbc4c4;
}

.badge-surgery {
  background: #fdf6ec;
  color: #e6a23c;
  border: 1px solid #f5dab1;
}

.badge-abnormal {
  background: #fef0f0;
  color: #f56c6c;
  border: 1px solid #fbc4c4;
}

.badge-new-order {
  background: #ecf5ff;
  color: #409eff;
  border: 1px solid #b3d8ff;
}

.badge-pending {
  background: #f4f4f5;
  color: #909399;
  border: 1px solid #d3d4d6;
}

/* 闪烁动画（用于超时任务） */
.badge-blink {
  animation: blink 1.5s ease-in-out infinite;
}

@keyframes blink {
  0%, 100% {
    opacity: 1;
  }
  50% {
    opacity: 0.6;
  }
}

/* 悬停效果 */
.status-badge:hover {
  transform: scale(1.05);
  transition: transform 0.2s ease;
}
</style>
