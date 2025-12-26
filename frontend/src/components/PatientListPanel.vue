<template>
  <!-- ============================== 
    【患者列表面板组件】
    可复用的患者列表面板，支持：
    - 搜索（床号/姓名）
    - 病区筛选
    - 待签收筛选
    - 单选/多选模式
    - 我负责的病区高亮
    - 折叠/展开
    ============================== -->
  <aside class="patient-panel" :class="{ collapsed: collapsed }">
    <!-- 面板头部 -->
    <div class="panel-header">
      <span class="panel-title" v-show="!collapsed">{{ title }}</span>
      <button @click="toggleCollapse" class="collapse-btn" :title="collapsed ? '展开' : '折叠'">
        {{ collapsed ? '>' : '<' }}
      </button>
    </div>

    <div class="panel-content" v-show="!collapsed">
      <!-- 搜索框 -->
      <div class="search-box">
        <el-input 
          v-model="searchKeyword" 
          :placeholder="searchPlaceholder"
          clearable
          size="small"
        >
          <template #prefix>🔍</template>
        </el-input>
      </div>

      <!-- 筛选工具栏 -->
      <div class="filter-toolbar">
        <!-- 病区筛选 -->
        <div class="filter-group" v-if="showWardFilter">
          <el-select 
            v-model="selectedWard" 
            placeholder="病区筛选" 
            clearable 
            size="small"
            class="ward-filter"
          >
            <el-option label="全部病区" value=""></el-option>
            <el-option 
              v-for="ward in wardOptions" 
              :key="ward.wardId"
              :label="ward.wardName"
              :value="ward.wardId"
            >
              <span>{{ ward.wardName }}</span>
              <span v-if="ward.isMyWard" class="my-ward-tag">★ 我负责</span>
            </el-option>
          </el-select>
        </div>
        
        <!-- 自定义筛选选项 -->
        <div class="filter-group" v-if="showPendingFilter">
          <el-checkbox 
            v-model="showOnlyPending" 
            size="small"
            class="pending-filter"
          >
            {{ pendingFilterLabel }}
          </el-checkbox>
        </div>

        <!-- 多选模式 -->
        <div class="filter-group" v-if="enableMultiSelectMode">
          <el-checkbox 
            :model-value="multiSelect"
            @change="handleMultiSelectToggle"
            size="small"
            class="multi-select-toggle"
          >
            多选模式
          </el-checkbox>
        </div>

        <!-- 额外的筛选选项（插槽） -->
        <slot name="extra-filters"></slot>
      </div>

      <!-- 患者列表 -->
      <div class="patient-list">
        <div 
          v-for="patient in filteredPatients" 
          :key="patient.patientId"
          :class="['patient-card', { 
            active: isPatientSelected(patient),
            'has-pending': patient.unacknowledgedCount > 0,
            'my-ward': isMyWard(patient.wardId)
          }]"
          @click="handlePatientClick(patient)"
        >
          <!-- 多选模式复选框 -->
          <el-checkbox 
            v-if="multiSelect"
            :model-value="isPatientSelected(patient)"
            @click.stop
            @change="handleCheckboxChange(patient)"
            class="patient-checkbox"
          />
          
          <!-- 床号标签 -->
          <div class="bed-badge">{{ patient.bedId }}</div>
          
          <!-- 患者基本信息 -->
          <div class="patient-basic">
            <span class="p-name">{{ patient.patientName }}</span>
            <span class="p-info">{{ patient.gender }} {{ patient.age }}岁</span>
          </div>
          
          <!-- 患者元数据 -->
          <div class="patient-meta">
            <span class="p-care">护理{{ patient.nursingGrade }}级</span>
          </div>
          
          <!-- 数字徽章标记 -->
          <span 
            v-if="shouldShowBadge(patient)" 
            class="pending-badge"
            :title="getBadgeTitle(patient)"
          >
            {{ getBadgeValue(patient) }}
          </span>
        </div>

        <!-- 空状态 -->
        <div v-if="filteredPatients.length === 0" class="empty-state">
          <div class="empty-icon">👥</div>
          <p>{{ emptyText }}</p>
        </div>
      </div>

      <!-- 底部操作区域插槽 -->
      <div class="bottom-actions" v-if="$slots['bottom-actions']">
        <slot name="bottom-actions"></slot>
      </div>
    </div>

    <!-- 折叠状态显示 -->
    <div class="collapsed-content" v-show="collapsed">
      <div class="collapsed-text">{{ title }}</div>
      <div class="patient-count">{{ patientList.length }}人</div>
    </div>
  </aside>
</template>

<script setup>
import { ref, computed, watch } from 'vue';

// ==================== Props ====================
const props = defineProps({
  // 患者列表数据
  patientList: {
    type: Array,
    default: () => []
  },
  // 已选中的患者（单选或多选）
  selectedPatients: {
    type: Array,
    default: () => []
  },
  // 面板标题
  title: {
    type: String,
    default: '患者列表'
  },
  // 搜索框占位符
  searchPlaceholder: {
    type: String,
    default: '搜索床号/姓名'
  },
  // 空状态提示文本
  emptyText: {
    type: String,
    default: '暂无患者'
  },
  // 我负责的病区ID
  myWardId: {
    type: String,
    default: ''
  },
  // 是否显示病区筛选
  showWardFilter: {
    type: Boolean,
    default: true
  },
  // 是否显示待签收筛选
  showPendingFilter: {
    type: Boolean,
    default: true
  },
  // 自定义筛选标签文本
  pendingFilterLabel: {
    type: String,
    default: '仅显示待签收'
  },
  // 徽章字段名（患者对象中的字段名，如 'unacknowledgedCount'、'pendingTaskCount' 等）
  badgeField: {
    type: String,
    default: 'unacknowledgedCount'
  },
  // 徽章显示条件（函数，返回是否显示徽章）
  badgeFilter: {
    type: Function,
    default: (patient, badgeValue) => badgeValue > 0
  },
  // 是否启用多选模式功能
  enableMultiSelectMode: {
    type: Boolean,
    default: true
  },
  // 外部传入的多选状态（从 usePatientData 传入）
  multiSelect: {
    type: Boolean,
    default: false
  },
  // 初始折叠状态
  initialCollapsed: {
    type: Boolean,
    default: false
  }
});

// ==================== Emits ====================
const emit = defineEmits([
  'patient-select',      // 患者选择事件
  'multi-select-toggle', // 多选模式切换事件
  'update:collapsed'     // 折叠状态更新
]);

// ==================== 状态管理 ====================
const searchKeyword = ref('');
const selectedWard = ref('');
const showOnlyPending = ref(false);
const collapsed = ref(props.initialCollapsed);

// ==================== 计算属性 ====================

// 病区选项（从患者列表中提取唯一病区）
const wardOptions = computed(() => {
  const wards = new Map();
  props.patientList.forEach(p => {
    if (!wards.has(p.wardId)) {
      wards.set(p.wardId, {
        wardId: p.wardId,
        wardName: p.wardName,
        isMyWard: p.wardId === props.myWardId
      });
    }
  });
  return Array.from(wards.values()).sort((a, b) => {
    // 我负责的病区排在前面
    if (a.isMyWard && !b.isMyWard) return -1;
    if (!a.isMyWard && b.isMyWard) return 1;
    return a.wardName.localeCompare(b.wardName);
  });
});

// 过滤后的患者列表
const filteredPatients = computed(() => {
  let filtered = props.patientList;
  
  // 搜索过滤
  if (searchKeyword.value) {
    const keyword = searchKeyword.value.toLowerCase();
    filtered = filtered.filter(p => 
      p.bedId.toLowerCase().includes(keyword) ||
      p.patientName.includes(keyword)
    );
  }
  
  // 病区过滤
  if (selectedWard.value) {
    filtered = filtered.filter(p => p.wardId === selectedWard.value);
  }
  
  // 自定义筛选（根据徽章字段）
  if (showOnlyPending.value) {
    filtered = filtered.filter(p => {
      const badgeValue = getBadgeValue(p);
      return props.badgeFilter(p, badgeValue);
    });
  }
  
  return filtered;
});

// ==================== 方法 ====================

// 判断是否是我负责的病区
const isMyWard = (wardId) => {
  return wardId === props.myWardId;
};

// 判断患者是否被选中
const isPatientSelected = (patient) => {
  return props.selectedPatients.some(p => p.patientId === patient.patientId);
};

// 切换折叠状态
const toggleCollapse = () => {
  collapsed.value = !collapsed.value;
  emit('update:collapsed', collapsed.value);
};

// 处理患者点击
const handlePatientClick = (patient) => {
  emit('patient-select', {
    patient,
    isMultiSelect: props.multiSelect
  });
};

// 处理复选框变化
const handleCheckboxChange = (patient) => {
  emit('patient-select', {
    patient,
    isMultiSelect: true,
    isCheckboxClick: true
  });
};

// 获取徽章值
const getBadgeValue = (patient) => {
  if (!props.badgeField) return 0;
  const value = patient[props.badgeField];
  return typeof value === 'number' ? value : 0;
};

// 判断是否显示徽章
const shouldShowBadge = (patient) => {
  const badgeValue = getBadgeValue(patient);
  return props.badgeFilter(patient, badgeValue);
};

// 获取徽章提示文本
const getBadgeTitle = (patient) => {
  const badgeValue = getBadgeValue(patient);
  // 根据字段名生成提示文本
  const fieldNameMap = {
    'unacknowledgedCount': '待签收医嘱',
    'pendingTaskCount': '待处理任务',
    'urgentCount': '紧急事项',
    'unreadCount': '未读消息'
  };
  const fieldLabel = fieldNameMap[props.badgeField] || '待处理';
  return `${fieldLabel}: ${badgeValue}`;
};

// 处理多选模式切换
const handleMultiSelectToggle = (value) => {
  emit('multi-select-toggle', value);
};
</script>

<style scoped>
/* ============================== 
  【样式说明】
  所有颜色、字体、间距都已提取为CSS变量
  可以通过外部覆盖这些变量来定制样式
============================== */

/* ==================== 全局变量 ==================== */
.patient-panel {
  --primary-color: #409eff;
  --danger-color: #f56c6c;
  --bg-card: #ffffff;
  --bg-secondary: #f9fafc;
  --border-color: #dcdfe6;
  --text-primary: #303133;
  --text-regular: #606266;
  --text-secondary: #909399;
  --radius-large: 8px;
  --radius-medium: 6px;
  --radius-small: 4px;

  background: var(--bg-card);
  border-radius: var(--radius-large);
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.08);
  display: flex;
  flex-direction: column;
  overflow: hidden;
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
  max-height: calc(100vh - 100px);
}

.patient-panel.collapsed {
  width: 40px !important;
}

/* ==================== 面板头部 ==================== */
.panel-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 12px 15px;
  border-bottom: 1px solid #e8e8e8;
  background: #fafafa;
  flex-shrink: 0;
}

.patient-panel.collapsed .panel-header {
  flex-direction: column;
  padding: 12px 5px;
  justify-content: center;
  background: #f5f5f5;
}

.panel-title {
  font-size: 1rem;
  font-weight: 600;
  color: #000;
  margin: 0;
  display: flex;
  align-items: center;
  gap: 6px;
  letter-spacing: 0.3px;
}

.collapse-btn {
  background: #ddd;
  color: #666;
  border: none;
  width: 22px;
  height: 22px;
  border-radius: 50%;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 0.8rem;
  font-weight: normal;
  transition: all 0.25s;
  flex-shrink: 0;
}

.collapse-btn:hover {
  background: #bbb;
  color: #333;
  transform: scale(1.05);
}

/* ==================== 面板内容 ==================== */
.panel-content {
  flex: 1;
  overflow: hidden;
  display: flex;
  flex-direction: column;
}

/* ==================== 搜索框 ==================== */
.search-box {
  padding: 12px 15px;
  border-bottom: 1px solid #f0f0f0;
  flex-shrink: 0;
}

.search-box :deep(.el-input__inner) {
  font-size: 1rem;
  color: var(--text-regular);
  font-weight: normal;
}

.search-box :deep(.el-input__inner::placeholder) {
  color: #999;
  font-weight: normal;
}

/* ==================== 筛选工具栏 ==================== */
.filter-toolbar {
  padding: 10px 15px;
  border-bottom: 1px solid #e8e8e8;
  background: #fafafa;
  display: flex;
  flex-direction: column;
  gap: 8px;
  flex-shrink: 0;
}

.filter-group {
  display: flex;
  align-items: center;
}

.ward-filter {
  width: 100%;
}

.ward-filter :deep(.el-input__inner) {
  font-size: 13px;
}

.my-ward-tag {
  color: #f59e0b;
  font-weight: 600;
  margin-left: 8px;
  font-size: 12px;
}

.pending-filter,
.multi-select-toggle {
  font-size: 13px;
}

.pending-filter :deep(.el-checkbox__label),
.multi-select-toggle :deep(.el-checkbox__label) {
  font-size: 13px;
  color: #606266;
}

/* ==================== 患者列表 ==================== */
.patient-list {
  flex: 1;
  overflow-y: auto;
  padding: 10px;
}

.patient-card {
  background: white;
  border: 1.5px solid var(--border-color);
  border-radius: var(--radius-medium);
  padding: 12px;
  margin-bottom: 10px;
  cursor: pointer;
  transition: all 0.3s;
  position: relative;
  display: flex;
  flex-direction: column;
}

/* 我负责的病区样式 */
.patient-card.my-ward {
  border-left: 3px solid #f59e0b;
  background: linear-gradient(90deg, #fffbeb 0%, white 20%);
}

/* 多选模式复选框 */
.patient-checkbox {
  position: absolute;
  top: 12px;
  left: 12px;
  z-index: 1;
}

.patient-card:has(.patient-checkbox) {
  padding-left: 40px;
}

/* hover效果 */
.patient-card:hover {
  border-color: var(--primary-color);
  transform: translateX(4px);
  box-shadow: -3px 0 12px rgba(64, 158, 255, 0.15);
}

/* 选中状态 */
.patient-card.active {
  background: linear-gradient(135deg, #e8f4ff 0%, #f0f8ff 100%);
  border-color: var(--primary-color);
  border-width: 2px;
  box-shadow: -4px 0 16px rgba(64, 158, 255, 0.25);
}

/* 待签收标记 */
.pending-badge {
  position: absolute;
  top: 4px;
  right: 4px;
  background: var(--danger-color);
  color: white;
  font-size: 11px;
  padding: 2px 7px;
  border-radius: 10px;
  font-weight: 600;
  box-shadow: 0 2px 4px rgba(245, 108, 108, 0.3);
}

/* 床号标签 */
.bed-badge {
  background: var(--primary-color);
  color: white;
  padding: 3px 8px;
  border-radius: 4px;
  font-size: 0.75rem;
  font-weight: bold;
  display: inline-block;
  margin-bottom: 8px;
}

.patient-card.active .bed-badge {
  background: linear-gradient(135deg, #409eff 0%, #66b1ff 100%);
  box-shadow: 0 2px 6px rgba(64, 158, 255, 0.3);
}

/* 患者基本信息 */
.patient-basic {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 6px;
}

.p-name {
  font-weight: 600;
  font-size: 1rem;
  color: var(--text-primary);
  letter-spacing: 0.3px;
}

.p-info {
  font-size: 0.85rem;
  color: var(--text-secondary);
}

/* 患者元数据 */
.patient-meta {
  display: flex;
  gap: 10px;
  margin-bottom: 6px;
}

.p-care {
  font-size: 0.8rem;
  color: var(--primary-color);
  background: #e8f4ff;
  padding: 2px 8px;
  border-radius: 10px;
}

/* ==================== 空状态 ==================== */
.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 40px 20px;
  color: var(--text-secondary);
}

.empty-icon {
  font-size: 48px;
  margin-bottom: 12px;
  opacity: 0.5;
}

.empty-state p {
  font-size: 0.95rem;
  color: var(--text-secondary);
}

/* ==================== 底部操作区 ==================== */
.bottom-actions {
  padding: 12px 15px;
  border-top: 1px solid var(--border-color);
  background-color: var(--bg-secondary);
}

/* ==================== 折叠状态 ==================== */
.collapsed-content {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 20px 0;
}

.collapsed-text {
  writing-mode: vertical-rl;
  font-size: 0.9rem;
  font-weight: 600;
  color: var(--text-primary);
  letter-spacing: 2px;
  margin-bottom: 20px;
}

.patient-count {
  background: var(--primary-color);
  color: white;
  width: 28px;
  height: 28px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 0.85rem;
  font-weight: bold;
  margin-top: 10px;
}
</style>
