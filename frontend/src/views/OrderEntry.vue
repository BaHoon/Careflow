<template>
  <div class="order-container">
    <nav class="navbar">
      <div class="logo">
        <i class="el-icon-s-order"></i> CareFlow | 医嘱开具工作台
      </div>
      <div class="user-info">
        <span class="user-name">{{ currentUser.fullName }}</span>
        <span class="user-role">({{ currentUser.role }})</span>
        <button @click="$router.push('/home')" class="btn-back">
          <i class="el-icon-back"></i> 返回首页
        </button>
      </div>
    </nav>

    <main class="order-layout">
      <header class="patient-context" v-if="selectedPatient">
        <div class="patient-badge">{{ selectedPatient.bedId }}</div>
        <div class="patient-info">
          <span class="name">{{ selectedPatient.name }}</span>
          <span class="detail">{{ selectedPatient.gender }} | {{ selectedPatient.age }}岁 | {{ selectedPatient.weight }}kg</span>
          <span class="tag">护理级别: {{ selectedPatient.nursingGrade }}级</span>
        </div>
      </header>

      <div class="main-content" :style="{ gridTemplateColumns: gridTemplateColumns }">
        <!-- 左侧：患者列表面板 -->
        <aside class="patient-panel" :class="{ collapsed: leftCollapsed }">
          <div class="panel-header">
            <span class="panel-title" v-show="!leftCollapsed">患者列表</span>
            <button @click="toggleLeft" class="collapse-btn" :title="leftCollapsed ? '展开' : '折叠'">
              {{ leftCollapsed ? '>' : '<' }}
            </button>
          </div>

          <div class="panel-content" v-show="!leftCollapsed">
            <!-- 搜索框 -->
            <div class="search-box">
              <el-input 
                v-model="patientSearch" 
                placeholder="搜索床号/姓名"
                clearable
                size="small"
              >
                <template #prefix>🔍</template>
              </el-input>
            </div>

            <!-- 患者列表 -->
            <div class="patient-list">
              <div 
                v-for="patient in filteredPatients" 
                :key="patient.id"
                :class="['patient-card', { active: patient.id === selectedPatient.id }]"
                @click="handlePatientClick(patient)"
              >
                <div class="bed-badge">{{ patient.bedId }}</div>
                <div class="patient-basic">
                  <span class="p-name">{{ patient.name }}</span>
                  <span class="p-info">{{ patient.gender }} {{ patient.age }}岁</span>
                </div>
                <div class="patient-meta">
                  <span class="p-care">护理{{ patient.nursingGrade }}级</span>
                </div>
              </div>
            </div>
          </div>

          <!-- 折叠状态显示 -->
          <div class="collapsed-content" v-show="leftCollapsed">
            <div class="collapsed-text">患者列表</div>
            <div class="patient-count">{{ patientList.length }}人</div>
          </div>
        </aside>

        <!-- 中间：医嘱表单区域 -->
        <section class="form-area">
          <div class="tabs-header">
            <button v-for="t in types" :key="t.val" 
                    :class="['tab-item', { active: activeType === t.val }]"
                    @click="activeType = t.val">
              {{ t.label }}
            </button>
          </div>

          <div class="form-card">
            <div v-if="activeType === 'MedicationOrder'" class="med-form">
              <!-- 步骤1：医嘱类型选择 -->
              <div class="form-section">
                <div class="section-header">
                  <i class="el-icon-document-checked"></i>
                  <span>医嘱基本信息</span>
                </div>
                <div class="form-row">
                  <label class="required">医嘱类型：</label>
                  <el-radio-group v-model="currentOrder.isLongTerm" @change="onOrderTypeChange">
                    <el-radio-button :label="true">
                      <i class="el-icon-time"></i> 长期医嘱
                    </el-radio-button>
                    <el-radio-button :label="false">
                      <i class="el-icon-lightning"></i> 临时医嘱
                    </el-radio-button>
                  </el-radio-group>
                  <span class="tip-text">{{ currentOrder.isLongTerm ? '长期医嘱需配置执行周期' : '临时医嘱为单次执行' }}</span>
                </div>

                <!-- 步骤2：时间策略选择 -->
                <div class="form-row" v-if="!currentOrder.isLongTerm">
                  <label class="required">执行时间：</label>
                  <el-radio-group v-model="currentOrder.timingStrategy" @change="onStrategyChange">
                    <el-radio label="Immediate">
                      <i class="el-icon-video-play"></i> 立即执行
                    </el-radio>
                    <el-radio label="Specific">
                      <i class="el-icon-alarm-clock"></i> 指定时间单次执行
                    </el-radio>
                  </el-radio-group>
                </div>

                <div class="form-row" v-if="currentOrder.isLongTerm">
                  <label class="required">执行策略：</label>
                  <el-radio-group v-model="currentOrder.timingStrategy" @change="onStrategyChange">
                    <el-radio label="Slots">
                      <i class="el-icon-clock"></i> 按时段执行 (如早餐前、午餐后)
                    </el-radio>
                    <el-radio label="Cyclic">
                      <i class="el-icon-refresh"></i> 固定间隔执行 (如每6小时一次)
                    </el-radio>
                  </el-radio-group>
                </div>

                <!-- 步骤3：根据策略显示对应配置 -->
                <div class="strategy-config">
                  <!-- 3.1 SPECIFIC策略：日期时间选择器 -->
                  <div class="form-row" v-if="currentOrder.timingStrategy === 'Specific'">
                    <label class="required">指定执行时间：</label>
                    <el-date-picker 
                      v-model="currentOrder.specificExecutionTime"
                      type="datetime"
                      placeholder="选择具体日期和时间"
                      :disabled-date="disablePastDates"
                      format="YYYY-MM-DD HH:mm"
                      value-format="YYYY-MM-DDTHH:mm:ss"
                      style="width: 280px"
                    />
                  </div>

                  <!-- 3.2 CYCLIC策略：间隔天数 -->
                  <div class="form-row" v-if="currentOrder.timingStrategy === 'Cyclic'">
                    <label class="required">间隔天数：</label>
                    <el-input-number 
                      v-model="currentOrder.intervalDays" 
                      :min="1" 
                      :max="30"
                      placeholder="每隔N天执行"
                      style="width: 150px"
                    />
                    <span class="tip-text">如填1表示每天，填2表示隔天</span>
                  </div>

                  <!-- 3.3 长期医嘱：开始时间 -->
                  <div class="form-row" v-if="currentOrder.isLongTerm">
                    <label class="required">开始时间：</label>
                    <el-date-picker 
                      v-model="currentOrder.startTime"
                      type="datetime"
                      placeholder="长期医嘱生效开始时间"
                      :disabled-date="disablePastDates"
                      format="YYYY-MM-DD HH:mm"
                      value-format="YYYY-MM-DDTHH:mm:ss"
                      style="width: 280px"
                    />
                  </div>

                  <!-- 3.4 长期医嘱：计划结束时间(可选) -->
                  <div class="form-row" v-if="currentOrder.isLongTerm">
                    <label>计划结束时间：</label>
                    <el-date-picker 
                      v-model="currentOrder.plantEndTime"
                      type="datetime"
                      placeholder="不填表示持续至医嘱停止"
                      format="YYYY-MM-DD HH:mm"
                      value-format="YYYY-MM-DDTHH:mm:ss"
                      style="width: 280px"
                    />
                  </div>
                </div>
              </div>

              <!-- 步骤4：药品信息录入 -->
              <div class="form-section">
                <div class="section-header">
                  <i class="el-icon-medicine-box"></i>
                  <span>药品信息</span>
                </div>
                <div class="drug-group-box">
                  <div class="drug-group-header">
                    <span>混合药物配置</span>
                    <button @click="addDrug" class="btn-icon-text">
                      + 添加药品
                    </button>
                  </div>
                  <div v-for="(item, index) in currentOrder.items" :key="index" class="drug-item-row">
                    <div class="item-index">{{ index + 1 }}</div>
                    <el-select 
                      v-model="item.drugId" 
                      filterable 
                      placeholder="搜索药品名称/简拼/条码"
                      class="drug-select"
                    >
                      <el-option 
                        v-for="d in drugDict" 
                        :key="d.id" 
                        :label="`${d.genericName} [${d.specification}]`" 
                        :value="d.id"
                      >
                        <div class="drug-option">
                          <span class="drug-name">{{ d.genericName }}</span>
                          <span class="drug-spec">{{ d.specification }}</span>
                        </div>
                      </el-option>
                    </el-select>
                    <el-input 
                      v-model="item.dosage" 
                      placeholder="剂量 (如 0.5g)" 
                      class="dosage-input"
                      style="width: 120px"
                    />
                    <el-input 
                      v-model="item.note" 
                      placeholder="备注 (可选)" 
                      class="note-input"
                      style="width: 140px"
                    />
                    <button 
                      @click="removeDrug(index)" 
                      class="btn-icon-danger"
                      :disabled="currentOrder.items.length === 1"
                    >
                      ×
                    </button>
                  </div>
                </div>
              </div>

              <!-- 步骤5：给药途径与频次 -->
              <div class="form-section">
                <div class="section-header">
                  <i class="el-icon-connection"></i>
                  <span>给药方式</span>
                </div>
                <div class="form-grid">
                  <div class="grid-item">
                    <label class="required">给药途径：</label>
                    <el-select v-model="currentOrder.usageRoute" placeholder="请选择" style="width: 100%">
                      <el-option label="静脉滴注 (IV Drip)" :value="20" />
                      <el-option label="静脉推注 (IV Push)" :value="21" />
                      <el-option label="口服 (PO)" :value="1" />
                      <el-option label="肌肉注射 (IM)" :value="10" />
                      <el-option label="皮下注射 (SC)" :value="11" />
                      <el-option label="皮内注射 (ID)" :value="12" />
                    </el-select>
                  </div>
                  <div class="grid-item">
                    <label class="required">执行频次：</label>
                    <el-select v-model="currentOrder.freqCode" @change="onFreqChange" placeholder="请选择" style="width: 100%">
                      <el-option label="单次给药 (ONCE)" value="ONCE" />
                      <el-option label="每日一次 (QD)" value="QD" />
                      <el-option label="每日两次 (BID)" value="BID" />
                      <el-option label="每日三次 (TID)" value="TID" />
                      <el-option label="每日四次 (QID)" value="QID" />
                      <el-option label="每6小时一次 (Q6H)" value="Q6H" />
                      <el-option label="每8小时一次 (Q8H)" value="Q8H" />
                      <el-option label="每12小时一次 (Q12H)" value="Q12H" />
                      <el-option label="需要时 (PRN)" value="PRN" />
                      <el-option label="持续给药 (CONT)" value="CONT" />
                    </el-select>
                  </div>
                </div>
                <div class="freq-description" v-if="currentOrder.freqCode">
                  <i class="el-icon-info"></i> {{ getFreqDescription(currentOrder.freqCode) }}
                </div>
              </div>

              <!-- 步骤6：时段选择器 (仅SLOTS策略显示) -->
              <div class="form-section" v-if="currentOrder.timingStrategy === 'Slots'">
                <div class="section-header">
                  <i class="el-icon-date"></i>
                  <span>执行时段配置</span>
                </div>
                <div class="time-slots-selector">
                  <div class="slot-category">
                    <div class="category-title">📅 餐食相关时段</div>
                    <div class="slots-grid">
                      <div v-for="slot in mealTimeSlots" :key="slot.id" 
                           :class="['slot-tag', { selected: isSlotSelected(slot.id) }]"
                           @click="toggleSlot(slot.id)">
                        <i class="el-icon-check" v-if="isSlotSelected(slot.id)"></i>
                        {{ slot.slotName }}
                        <span class="time-hint">{{ formatTime(slot.defaultTime) }}</span>
                      </div>
                    </div>
                  </div>
                  <div class="slot-category">
                    <div class="category-title">⏰ 一般时段</div>
                    <div class="slots-grid">
                      <div v-for="slot in generalTimeSlots" :key="slot.id" 
                           :class="['slot-tag', { selected: isSlotSelected(slot.id) }]"
                           @click="toggleSlot(slot.id)">
                        <i class="el-icon-check" v-if="isSlotSelected(slot.id)"></i>
                        {{ slot.slotName }}
                        <span class="time-hint">{{ formatTime(slot.defaultTime) }}</span>
                      </div>
                    </div>
                  </div>
                  <div class="freq-reminder" v-if="currentOrder.smartSlotsMask > 0">
                    <i class="el-icon-info"></i> 
                    已选择 {{ getSelectedSlotsCount() }} 个时段，配合频次 <strong>{{ currentOrder.freqCode }}</strong> 将生成对应的执行任务
                  </div>
                </div>
              </div>

              <!-- 步骤7：医嘱备注 -->
              <div class="form-section">
                <div class="form-row">
                  <label>医嘱备注：</label>
                  <el-input 
                    v-model="currentOrder.remarks"
                    type="textarea"
                    :rows="2"
                    placeholder="可填写特殊嘱托，如过敏史、注意事项等"
                    maxlength="200"
                    show-word-limit
                  />
                </div>
              </div>
            </div>

            <div v-else class="placeholder-form">
              正在开发 {{ activeType }} 的详细表单...
            </div>

            <div class="form-actions">
              <button @click="clearForm" class="btn-default">
                <i class="el-icon-refresh-left"></i> 清空表单
              </button>
              <button @click="addToCart" class="btn-primary" :disabled="!isFormValid">
                <i class="el-icon-folder-add"></i> 暂存医嘱
              </button>
            </div>
          </div>
        </section>

        <!-- 右侧：待提交医嘱面板 -->
        <aside class="cart-panel" :class="{ collapsed: rightCollapsed }">
          <div class="panel-header">
            <button @click="toggleRight" class="collapse-btn" :title="rightCollapsed ? '展开' : '折叠'">
              {{ rightCollapsed ? '<' : '>' }}
            </button>
            <h3 class="panel-title" v-show="!rightCollapsed">
              待提交医嘱
              <span class="cart-count">{{ orderCart.length }}</span>
            </h3>
            <button @click="clearCart" class="btn-text-danger" v-if="orderCart.length && !rightCollapsed">
              × 清空
            </button>
          </div>

          <div class="panel-content" v-show="!rightCollapsed">
            <div v-if="orderCart.length" class="cart-list">
              <div v-for="(o, idx) in orderCart" :key="idx" class="cart-item-compact">
                <!-- 精简摘要 -->
                <div class="order-summary-line">
                  <el-tag :type="o.isLongTerm ? 'primary' : 'warning'" size="small">
                    {{ o.isLongTerm ? '长期' : '临时' }}
                  </el-tag>
                  <span class="order-title">{{ getOrderSummary(o) }}</span>
                  <button @click="toggleOrderDetail(idx)" class="btn-detail">
                    {{ expandedOrders.includes(idx) ? '▲' : '▼' }}
                  </button>
                  <button @click="removeFromCart(idx)" class="btn-mini-danger">
                    ×
                  </button>
                </div>
                
                <!-- 基本信息（始终显示） -->
                <div class="order-basic-info">
                  <span class="info-item">{{ getRouteName(o.usageRoute) }}</span>
                  <span class="info-divider">|</span>
                  <span class="info-item">{{ o.freqCode }}</span>
                </div>

                <!-- 详细信息（可展开） -->
                <div v-show="expandedOrders.includes(idx)" class="order-detail-expand">
                  <div class="detail-section">
                    <div class="detail-label">药品明细：</div>
                    <div v-for="(item, i) in o.items" :key="i" class="detail-value">
                      {{ i + 1 }}. {{ getDrugName(item.drugId) }} {{ item.dosage }}
                      <span v-if="item.note" class="note-text">({{ item.note }})</span>
                    </div>
                  </div>
                  <div class="detail-section">
                    <div class="detail-label">时间策略：</div>
                    <div class="detail-value">{{ getStrategyDescription(o) }}</div>
                  </div>
                </div>
              </div>

              <!-- 空状态 -->
              <div v-if="!orderCart.length" class="cart-empty">
                <div class="empty-icon">📋</div>
                <p>暂无待提交医嘱</p>
              </div>
            </div>
          </div>

          <div class="cart-footer">
            <button 
              @click="submitAll" 
              class="btn-submit-all" 
              :disabled="!orderCart.length || submitting"
            >
              <span v-if="!submitting">✓ 确认并提交</span>
              <span v-else>提交中...</span>
            </button>
          </div>

          <!-- 折叠状态显示 -->
          <div class="collapsed-content" v-show="rightCollapsed">
            <div class="collapsed-text">待提交</div>
            <div class="cart-count-vertical">{{ orderCart.length }}</div>
            <div class="collapsed-icon">✓</div>
          </div>
        </aside>
      </div>
    </main>
  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted } from 'vue';
import { ElMessage } from 'element-plus';

const currentUser = ref({ fullName: '张医生', role: '主任医师' });
const activeType = ref('MedicationOrder');
const selectedPatient = ref({ 
  id: 'P001', 
  name: '张三', 
  gender: '男', 
  age: 34, 
  weight: 70.5, 
  bedId: 'IM-W01-001', 
  nursingGrade: 2 
});

const types = [
  { label: '药物医嘱', val: 'MedicationOrder' },
  { label: '检查申请', val: 'InspectionOrder' },
  { label: '手术/操作', val: 'SurgicalOrder' }
];

// 核心医嘱对象（对应 MedicationOrder.cs 结构）
const currentOrder = reactive({
  isLongTerm: true,
  items: [{ drugId: '', dosage: '', note: '' }],
  usageRoute: 20,
  freqCode: 'QD',
  smartSlotsMask: 0,
  timingStrategy: 'Slots',  // 默认策略
  specificExecutionTime: null,
  startTime: null,
  plantEndTime: null,
  intervalDays: 1,
  remarks: ''
});

const orderCart = ref([]);
const drugDict = ref([]);
const timeSlotDict = ref([]);
const submitting = ref(false);

// 患者列表相关
const patientList = ref([]);
const patientSearch = ref('');

// 折叠状态
const leftCollapsed = ref(false);
const rightCollapsed = ref(false);

// 医嘱详情展开状态
const expandedOrders = ref([]);

// 计算属性：餐食相关时段
const mealTimeSlots = computed(() => 
  timeSlotDict.value.filter(s => [1, 2, 4, 8, 16, 32, 64, 128].includes(s.id))
);

// 计算属性：一般时段
const generalTimeSlots = computed(() => 
  timeSlotDict.value.filter(s => [256, 512, 1024, 2048, 4096, 8192, 16384, 32768].includes(s.id))
);

// 计算属性：过滤后的患者列表
const filteredPatients = computed(() => {
  if (!patientSearch.value) return patientList.value;
  const keyword = patientSearch.value.toLowerCase();
  return patientList.value.filter(p => 
    p.bedId.toLowerCase().includes(keyword) ||
    p.name.includes(keyword)
  );
});

// 计算属性：栅格列宽度
const gridTemplateColumns = computed(() => {
  const left = leftCollapsed.value ? '40px' : '250px';
  const right = rightCollapsed.value ? '40px' : '300px';
  return `${left} 1fr ${right}`;
});

// 计算属性：表单验证
const isFormValid = computed(() => {
  // 基础校验
  if (!currentOrder.items.some(i => i.drugId && i.dosage)) return false;
  if (!currentOrder.usageRoute || !currentOrder.freqCode) return false;

  // 策略特定校验
  switch (currentOrder.timingStrategy) {
    case 'Specific':
      if (!currentOrder.specificExecutionTime) return false;
      if (new Date(currentOrder.specificExecutionTime) <= new Date()) return false;
      break;
    case 'Slots':
      if (currentOrder.smartSlotsMask === 0) return false;
      break;
    case 'Cyclic':
      if (!currentOrder.intervalDays || currentOrder.intervalDays < 1) return false;
      break;
  }

  // 长期医嘱必须有开始时间
  if (currentOrder.isLongTerm && !currentOrder.startTime) return false;

  return true;
});

// 医嘱类型切换
const onOrderTypeChange = (isLongTerm) => {
  if (isLongTerm) {
    currentOrder.timingStrategy = 'Slots';
    currentOrder.startTime = new Date().toISOString();
    currentOrder.intervalDays = 1;
    currentOrder.specificExecutionTime = null;
  } else {
    currentOrder.timingStrategy = 'Immediate';
    currentOrder.startTime = null;
    currentOrder.plantEndTime = null;
    currentOrder.smartSlotsMask = 0;
  }
};

// 策略切换
const onStrategyChange = () => {
  // 清空相关字段
  currentOrder.specificExecutionTime = null;
  currentOrder.smartSlotsMask = 0;
  currentOrder.intervalDays = 1;
};

// 频次改变
const onFreqChange = () => {
  console.log('频次已更改为:', currentOrder.freqCode);
};

// 时段操作
const toggleSlot = (slotId) => {
  currentOrder.smartSlotsMask ^= slotId;
};

const isSlotSelected = (slotId) => {
  return (currentOrder.smartSlotsMask & slotId) !== 0;
};

const getSelectedSlotsCount = () => {
  let count = 0;
  let mask = currentOrder.smartSlotsMask;
  while (mask) {
   折叠切换
const toggleLeft = () => {
  leftCollapsed.value = !leftCollapsed.value;
};

const toggleRight = () => {
  rightCollapsed.value = !rightCollapsed.value;
};

// 患者切换
const handlePatientClick = (patient) => {
  if (patient.id === selectedPatient.value.id) return;
  
  const hasUnsubmittedData = 
    currentOrder.items.some(i => i.drugId && i.dosage) || 
    orderCart.value.length > 0;
  
  if (hasUnsubmittedData) {
    ElMessageBox.confirm(
      '切换患者将清空当前表单和待提交清单，是否继续？',
      '确认切换',
      {
        confirmButtonText: '确认切换',
        cancelButtonText: '取消',
        type: 'warning',
      }
    ).then(() => {
      selectedPatient.value = { ...patient };
      clearForm();
      orderCart.value = [];
      expandedOrders.value = [];
      ElMessage.success(`已切换至患者：${patient.name} (${patient.bedId})`);
    }).catch(() => {
      ElMessage.info('已取消切换');
    });
  } else {
    selectedPatient.value = { ...patient };
    ElMessage.success(`已切换至患者：${patient.name} (${patient.bedId})`);
  }
};

// 切换医嘱详情展开状态
const toggleOrderDetail = (index) => {
  const idx = expandedOrders.value.indexOf(index);
  if (idx > -1) {
    expandedOrders.value.splice(idx, 1);
  } else {
    expandedOrders.value.push(index);
  }
};

//  count += mask & 1;
    mask >>= 1;
  }
  return count;
};

// 药品操作
const addDrug = () => {
  currentOrder.items.push({ drugId: '', dosage: '', note: '' });
};

const removeDrug = (index) => {
  if (currentOrder.items.length > 1) {
    currentOrder.items.splice(index, 1);
  }
};

// 表单操作
const clearForm = () => {
  currentOrder.items = [{ drugId: '', dosage: '', note: '' }];
  currentOrder.usageRoute = 20;
  currentOrder.freqCode = 'QD';
  currentOrder.smartSlotsMask = 0;
  currentOrder.specificExecutionTime = null;
  currentOrder.startTime = currentOrder.isLongTerm ? new Date().toISOString() : null;
  currentOrder.plantEndTime = null;
  currentOrder.intervalDays = 1;
  currentOrder.remarks = '';
  ElMessage.success('表单已清空');
};

const addToCart = () => {
  if (!isFormValid.value) {
    ElMessage.warning('请完善必填项后再暂存');
    return;
  }
  
  // 深拷贝当前医嘱到暂存区
  orderCart.value.push(JSON.parse(JSON.stringify({
    ...currentOrder,
    orderType: activeType.value,
    patientId: selectedPatient.value.id
  })));
  
  ElMessage.success('医嘱已暂存到待提交清单');
  clearForm();
};

const removeFromCart = (index) => {
  orderCart.value.splice(index, 1);
  ElMessage.info('已从清单中移除');
};

const clearCart = () => {
  orderCart.value = [];
  ElMessage.info('已清空待提交清单');
};

const submitAll = async () => {
  if (!orderCart.value.length) return;
  
  submitting.value = true;
  try {
    console.log('提交给后端 API:', orderCart.value);
    // TODO: 调用实际的 API
    // await axios.post('/api/MedicalOrder/create', orderCart.value);
    
    await new Promise(resolve => setTimeout(resolve, 1000)); // 模拟网络延迟
    
    ElMessage.success(`成功提交 ${orderCart.value.length} 条医嘱`);
    orderCart.value = [];
  } catch (error) {
    ElMessage.error('提交失败: ' + error.message);
  } finally {
    submitting.value = false;
  }
};

// 辅助函数
const disablePastDates = (time) => {
  return time.getTime() < Date.now() - 24 * 60 * 60 * 1000;
};

const formatTime = (timeSpan) => {
  if (!timeSpan) return '';
  // timeSpan 格式: "07:00:00"
  const parts = timeSpan.split(':');
  return `${parts[0]}:${parts[1]}`;
};

const getDrugName = (id) => {
  return drugDict.value.find(d => d.id === id)?.genericName || id;
};

// 患者列表
patientList.value = [
    {
      id: 'P001',
      bedId: 'IM-W01-001',
      name: '张三',
      gender: '男',
      age: 34,
      weight: 70.5,
      nursingGrade: 2,
      department: '内科'
    },
    {
      id: 'P002',
      bedId: 'IM-W01-002',
      name: '李四',
      gender: '女',
      age: 45,
      weight: 62.0,
      nursingGrade: 1,
      department: '内科'
    },
    {
      id: 'P003',
      bedId: 'IM-W01-003',
      name: '王五',
      gender: '男',
      age: 56,
      weight: 75.0,
      nursingGrade: 3,
      department: '内科'
    },
    {
      id: 'P004',
      bedId: 'IM-W01-004',
      name: '赵六',
      gender: '女',
      age: 38,
      weight: 58.5,
      nursingGrade: 2,
      department: '内科'
    },
    {
      id: 'P005',
      bedId: 'IM-W01-005',
      name: '钱七',
      gender: '男',
      age: 67,
      weight: 68.0,
      nursingGrade: 2,
      department: '内科'
    }
  ];

const getRouteName = (routeId) => {
  const routes = {
    1: '口服', 10: '肌肉注射', 11: '皮下注射', 12: '皮内注射',
    20: '静脉滴注', 21: '静脉推注'
  };
  return routes[routeId] || routeId;
};

const getFreqDescription = (freqCode) => {
  const descriptions = {
    'ONCE': '单次给药',
    'QD': '每日一次',
    'BID': '每日两次',
    'TID': '每日三次',
    'QID': '每日四次',
    'Q6H': '每6小时一次',
    'Q8H': '每8小时一次',
    'Q12H': '每12小时一次',
    'PRN': '需要时给药',
    'CONT': '持续给药'
  };
  return descriptions[freqCode] || freqCode;
};

const getOrderSummary = (order) => {
  const drugNames = order.items.map(i => getDrugName(i.drugId)).join('+');
  return `${drugNames} (${order.freqCode})`;
};

const getStrategyDescription = (order) => {
  switch (order.timingStrategy) {
    case 'Immediate':
      return '立即执行';
    case 'Specific':
      return `指定时间: ${order.specificExecutionTime}`;
    case 'Cyclic':
      return `每${order.intervalDays}天执行`;
    case 'Slots':
      const slots = timeSlotDict.value.filter(s => (order.smartSlotsMask & s.id) !== 0);
      return `时段: ${slots.map(s => s.slotName).join(', ')}`;
    default:
      return order.timingStrategy;
  }
};

// 模拟加载数据
onMounted(async () => {
  // TODO: 实际开发中通过 API 获取
  drugDict.value = [
    { id: 'DRUG001', genericName: '阿莫西林胶囊', specification: '0.25g/粒' },
    { id: 'DRUG002', genericName: '0.9%氯化钠注射液', specification: '250ml/袋' },
    { id: 'DRUG003', genericName: '5%葡萄糖注射液', specification: '500ml/袋' },
    { id: 'DRUG004', genericName: '头孢曲松钠', specification: '1.0g/瓶' },
    { id: 'DRUG005', genericName: '布洛芬缓释胶囊', specification: '0.3g/粒' }
  ];
  
  timeSlotDict.value = [
    { id: 1, slotCode: 'PRE_BREAKFAST', slotName: '早餐前', defaultTime: '07:00:00' },
    { id: 2, slotCode: 'POST_BREAKFAST', slotName: '早餐后', defaultTime: '08:30:00' },
    { id: 4, slotCode: 'PRE_LUNCH', slotName: '午餐前', defaultTime: '11:30:00' },
    { id: 8, slotCode: 'POST_LUNCH', slotName: '午餐后', defaultTime: '13:00:00' },
    { id: 16, slotCode: 'PRE_DINNER', slotName: '晚餐前', defaultTime: '17:30:00' },
    { id: 32, slotCode: 'POST_DINNER', slotName: '晚餐后', defaultTime: '19:00:00' },
    { id: 64, slotCode: 'BEDTIME', slotName: '睡前', defaultTime: '21:00:00' },
    { id: 128, slotCode: 'MIDNIGHT', slotName: '夜间', defaultTime: '00:00:00' },
    { id: 256, slotCode: 'EARLY_MORNING', slotName: '清晨', defaultTime: '06:00:00' },
    { id: 512, slotCode: 'MORNING', slotName: '上午', defaultTime: '09:00:00' },
    { id: 1024, slotCode: 'NOON', slotName: '中午', defaultTime: '12:00:00' },
    { id: 2048, slotCode: 'AFTERNOON', slotName: '下午', defaultTime: '15:00:00' },
    { id: 4096, slotCode: 'EVENING', slotName: '傍晚', defaultTime: '18:00:00' },
    { id: 8192, slotCode: 'NIGHT', slotName: '夜晚', defaultTime: '22:00:00' },
    { id: 16384, slotCode: 'LATE_NIGHT', slotName: '深夜', defaultTime: '02:00:00' },
    { id: 32768, slotCode: 'DAWN', slotName: '黎明', defaultTime: '04:00:00' }
  ];

  // 初始化开始时间
  if (currentOrder.isLongTerm) {
    currentOrder.startTime = new Date().toISOString();
  }
});
</script>

<style scoped>
/* ==================== 全局变量 ==================== */
.order-container {
  --primary-color: #409eff;
  --success-color: #67c23a;
  --warning-color: #e6a23c;
  --danger-color: #f56c6c;
  --info-color: #909399;
  
  --bg-page: #f4f7f9;
  --bg-card: #ffffff;
  --bg-secondary: #f9fafc;
  
  --border-color: #dcdfe6;
  --text-primary: #303133;
  --text-regular: #606266;
  --text-secondary: #909399;
  
  --radius-large: 8px;
  --radius-medium: 6px;
  --radius-small: 4px;
  --radius-round: 20px;
}

/* ==================== 顶部导航栏 ==================== */
.navbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 0.8rem 2rem;
  background: linear-gradient(135deg, #2c3e50 0%, #34495e 100%);
  color: white;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.logo {
  font-size: 1.3rem;
  font-weight: bold;
  display: flex;
  align-items: center;
  gap: 8px;
}

.user-info {
  display: flex;
  align-items: center;
  gap: 15px;
}

.user-name {
  font-weight: 600;
  font-size: 1rem;
}

.user-role {
  color: #ecf0f1;
  font-size: 0.9rem;
}

.btn-back {
  background: rgba(255, 255, 255, 0.2);
  border: 1px solid rgba(255, 255, 255, 0.3);
  color: white;
  padding: 8px 16px;
  border-radius: var(--radius-small);
  cursor: pointer;
  transition: all 0.3s;
  display: flex;
  align-items: center;
  gap: 5px;
  font-size: 0.9rem;
}

.btn-back:hover {
  background: rgba(255, 255, 255, 0.3);
  transform: translateY(-1px);
}

/* ==================== 主布局 ==================== */
.order-layout {
  padding: 20px;
  background: var(--bg-page);
  min-height: calc(100vh - 60px);
}

/* 患者上下文卡片 */
.patient-context {
  display: flex;
  align-items: center;
  background: var(--bg-card);
  padding: 15px 25px;
  border-radius: var(--radius-large);
  margin-bottom: 20px;
  box-shadow: 0 2px 10px rgba(0, 0, 0, 0.05);
  border-left: 5px solid var(--primary-color);
}

.patient-badge {
  background: var(--primary-color);
  color: white;
  padding: 8px 16px;
  border-radius: var(--radius-small);
  font-weight: bold;
  margin-right: 20px;
  font-size: 1.1rem;
}

.patient-info {
  display: flex;
  align-items: center;
  flex: 1;
}

.patient-info .name {
  font-size: 1.2rem;
  font-weight: bold;
  margin-right: 15px;
  color: var(--text-primary);
}

.patient-info .detail {
  color: var(--text-secondary);
  margin-right: 20px;
  font-size: 0.95rem;
}

.patient-info .tag {
  background: #e8f4ff;
  color: var(--primary-color);
  padding: 4px 12px;
  border-radius: var(--radius-round);
  font-size: 0.85rem;
}

/* 主内容区域 - 三栏布局 */
.main-content {
  display: grid;
  gap: 20px;
  transition: grid-template-columns 0.3s ease;
}

/* ==================== 标签页导航 ==================== */
.tabs-header {
  display: flex;
  margin-bottom: -1px;
}

.tab-item {
  padding: 12px 28px;
  border: none;
  background: #e0e0e0;
  cursor: pointer;
  border-radius: var(--radius-large) var(--radius-large) 0 0;
  margin-right: 5px;
  color: var(--text-secondary);
  transition: all 0.3s;
  font-size: 0.95rem;
  font-weight: 500;
}

.tab-item:hover {
  background: #d0d0d0;
}

.tab-item.active {
  background: var(--bg-card);
  color: var(--primary-color);
  font-weight: bold;
  box-shadow: 0 -2px 8px rgba(0, 0, 0, 0.05);
}

/* ==================== 表单卡片 ==================== */
.form-card {
  background: var(--bg-card);
  padding: 25px;
  border-radius: 0 var(--radius-large) var(--radius-large) var(--radius-large);
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.08);
  min-height: 600px;
}

.placeholder-form {
  padding: 60px 20px;
  text-align: center;
  color: var(--text-secondary);
  font-size: 1.1rem;
}

/* ==================== 表单分组 ==================== */
.form-section {
  margin-bottom: 25px;
  padding-bottom: 20px;
  border-bottom: 1px solid #f0f0f0;
}

.form-section:last-child {
  border-bottom: none;
}

.section-header {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 15px;
  font-size: 1.05rem;
  font-weight: 600;
  color: var(--text-primary);
}

.section-header i {
  color: var(--primary-color);
  font-size: 1.2rem;
}

.form-row {
  display: flex;
  align-items: center;
  margin-bottom: 15px;
  gap: 15px;
}

.form-row label {
  min-width: 120px;
  color: var(--text-regular);
  font-size: 0.95rem;
  font-weight: 500;
}

.form-row label.required::before {
  content: '* ';
  color: var(--danger-color);
  font-weight: bold;
}

.tip-text {
  color: var(--text-secondary);
  font-size: 0.85rem;
  font-style: italic;
}

/* ==================== 药品选择区域 ==================== */
.drug-group-box {
  background: var(--bg-secondary);
  border: 1px dashed var(--border-color);
  padding: 20px;
  border-radius: var(--radius-medium);
  margin: 15px 0;
}

.drug-group-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 15px;
  padding-bottom: 10px;
  border-bottom: 1px solid var(--border-color);
}

.drug-group-header span {
  font-weight: 600;
  color: var(--text-primary);
}

.drug-item-row {
  display: flex;
  gap: 10px;
  margin-bottom: 12px;
  align-items: center;
  padding: 10px;
  background: white;
  border-radius: var(--radius-small);
}

.item-index {
  width: 30px;
  height: 30px;
  background: var(--primary-color);
  color: white;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: bold;
  font-size: 0.9rem;
  flex-shrink: 0;
}

.drug-select {
  flex: 1;
  min-width: 250px;
}

.drug-option {
  display: flex;
  justify-content: space-between;
  align-items: center;
  width: 100%;
}

.drug-name {
  font-weight: 500;
  color: var(--text-primary);
}

.drug-spec {
  color: var(--text-secondary);
  font-size: 0.85rem;
  margin-left: 10px;
}

/* ==================== 表单网格布局 ==================== */
.form-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 15px;
  margin-top: 10px;
}

.grid-item {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.grid-item label {
  font-size: 0.9rem;
  color: var(--text-regular);
  font-weight: 500;
}

.freq-description {
  margin-top: 8px;
  padding: 10px;
  background: #e8f4ff;
  border-left: 3px solid var(--primary-color);
  border-radius: var(--radius-small);
  color: var(--text-regular);
  font-size: 0.9rem;
  display: flex;
  align-items: center;
  gap: 8px;
}

/* ==================== 时段选择器 ==================== */
.time-slots-selector {
  margin-top: 10px;
}

.slot-category {
  margin-bottom: 20px;
}

.category-title {
  font-size: 0.95rem;
  font-weight: 600;
  color: var(--text-primary);
  margin-bottom: 12px;
  padding-bottom: 8px;
  border-bottom: 2px solid var(--border-color);
}

.slots-grid {
  display: flex;
  gap: 10px;
  flex-wrap: wrap;
}

.slot-tag {
  padding: 8px 16px;
  border: 1.5px solid var(--border-color);
  border-radius: var(--radius-round);
  font-size: 0.9rem;
  cursor: pointer;
  transition: all 0.3s;
  background: white;
  color: var(--text-regular);
  display: flex;
  align-items: center;
  gap: 6px;
  position: relative;
}

.slot-tag:hover {
  border-color: var(--primary-color);
  transform: translateY(-2px);
  box-shadow: 0 2px 8px rgba(64, 158, 255, 0.2);
}

.slot-tag.selected {
  background: var(--primary-color);
  color: white;
  border-color: var(--primary-color);
  font-weight: 600;
}

.slot-tag .time-hint {
  font-size: 0.75rem;
  opacity: 0.8;
  margin-left: 4px;
}

.freq-reminder {
  margin-top: 15px;
  padding: 12px;
  background: #fff7e6;
  border-left: 3px solid var(--warning-color);
  border-radius: var(--radius-small);
  color: var(--text-regular);
  font-size: 0.9rem;
  display: flex;
  align-items: center;
  gap: 8px;
}

/* ==================== 按钮样式 ==================== */
.btn-default {
  background: white;
  border: 1px solid var(--border-color);
  color: var(--text-regular);
  padding: 10px 20px;
  border-radius: var(--radius-small);
  cursor: pointer;
  transition: all 0.3s;
  display: inline-flex;
  align-items: center;
  gap: 6px;
  font-size: 0.95rem;
}

.btn-default:hover {
  border-color: var(--primary-color);
  color: var(--primary-color);
}

.btn-primary {
  background: var(--primary-color) !important;
  border: none;
  color: white;
  padding: 10px 24px;
  border-radius: var(--radius-small);
  cursor: pointer;
  transition: all 0.3s;
  display: inline-flex;
  align-items: center;
  gap: 6px;
  font-size: 0.95rem;
  font-weight: 600;
}

.btn-primary:hover:not(:disabled) {
  background: #66b1ff !important;
  transform: translateY(-1px);
  box-shadow: 0 4px 12px rgba(64, 158, 255, 0.4);
}

.btn-primary:disabled {
  background: #a0cfff !important;
  cursor: not-allowed;
  opacity: 0.6;
}

.btn-icon-text {
  background: transparent;
  border: 1px dashed var(--primary-color);
  color: var(--primary-color);
  padding: 6px 12px;
  border-radius: var(--radius-small);
  cursor: pointer;
  transition: all 0.3s;
  display: inline-flex;
  align-items: center;
  gap: 4px;
  font-size: 0.85rem;
}

.btn-icon-text:hover {
  background: #e8f4ff;
  border-style: solid;
}

.btn-icon-danger {
  background: transparent;
  border: 1px solid var(--danger-color);
  color: var(--danger-color);
  padding: 6px 10px;
  border-radius: var(--radius-small);
  cursor: pointer;
  transition: all 0.3s;
  display: inline-flex;
  align-items: center;
}

.btn-icon-danger:hover:not(:disabled) {
  background: var(--danger-color);
  color: white;
}

.btn-icon-danger:disabled {
  opacity: 0.4;
  cursor: not-allowed;
}

.btn-text-danger {
  background: transparent;
  border: none;
  color: var(--danger-color);
  padding: 6px 12px;
  cursor: pointer;
  transition: all 0.3s;
  display: inline-flex;
  align-items: center;
  gap: 4px;
  font-size: 0.9rem;
}

.btn-text-danger:hover {
  color: #f78989;
}

/* ==================== 表单操作区 ==================== */
.form-actions {
  display: flex;
  justify-content: flex-end;
  gap: 15px;
  margin-top: 30px;
  padding-top: 20px;
  border-top: 2px solid #f0f0f0;
}

/* ==================== 侧边面板通用样式 ==================== */
.patient-panel,
.cart-panel {
  background: var(--bg-card);
  border-radius: var(--radius-large);
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.08);
  display: flex;
  flex-direction: column;
  max-height: calc(100vh - 260px);
  overflow: hidden;
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
}

.patient-panel.collapsed,
.cart-panel.collapsed {
  width: 40px !important;
}

.panel-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 12px 15px;
  border-bottom: 1px solid #e8e8e8;
  background: #fafafa;
  flex-shrink: 0;
}

.patient-panel.collapsed .panel-header,
.cart-panel.collapsed .panel-header {
  flex-direction: column;
  padding: 12px 5px;
  justify-content: center;
  background: #f5f5f5;
}

.cart-count {
  background: var(--primary-color);
  color: white;
  padding: 2px 10px;
  border-radius: 12px;
  font-size: 0.8rem;
  font-weight: 600;
  margin-left: 6px;
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

.panel-content {
  flex: 1;
  overflow: hidden;
  display: flex;
  flex-direction: column;
}

/* ==================== 患者列表面板 ==================== */
.patient-panel {
  position: relative;
}

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
}

.patient-card:hover {
  border-color: var(--primary-color);
  transform: translateX(4px);
  box-shadow: -3px 0 12px rgba(64, 158, 255, 0.15);
}

.patient-card.active {
  background: linear-gradient(135deg, #e8f4ff 0%, #f0f8ff 100%);
  border-color: var(--primary-color);
  border-width: 2px;
  box-shadow: -4px 0 16px rgba(64, 158, 255, 0.25);
}

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

.patient-count,
.cart-count-vertical {
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

.collapsed-icon {
  font-size: 1.5rem;
  color: var(--success-color);
  margin-top: 20px;
}

/* ==================== 待提交医嘱面板 ==================== */
.cart-panel {
  position: relative;
}

.cart-panel .panel-header {
  display: flex;
  align-items: center;
  gap: 10px;
}

.cart-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding-bottom: 15px;
  border-bottom: 2px solid #f0f0f0;
  margin-bottom: 15px;
}

.cart-header h3 {
  margin: 0;
  font-size: 1.1rem;
  color: var(--text-primary);
  display: flex;
  align-items: center;
  gap: 8px;
}

.cart-count {
  background: var(--primary-color);
  color: white;
  padding: 2px 10px;
  border-radius: 12px;
  font-size: 0.85rem;
  font-weight: normal;
}

.cart-list {
  flex: 1;
  overflow-y: auto;
  margin-bottom: 15px;
}

.cart-item {
  border: 1px solid var(--border-color);
  border-radius: var(--radius-medium);
  padding: 15px;
  margin-bottom: 12px;
  transition: all 0.3s;
  background: white;
}

.cart-item:hover {
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.08);
  transform: translateY(-2px);
}

.cart-list {
  flex: 1;
  overflow-y: auto;
  padding: 10px;
}

/* 紧凑型医嘱卡片 */
.cart-item-compact {
  background: white;
  border: 1.5px solid var(--border-color);
  border-radius: var(--radius-medium);
  padding: 10px;
  margin-bottom: 10px;
  transition: all 0.3s;
}

.cart-item-compact:hover {
  border-color: var(--primary-color);
  box-shadow: 0 3px 10px rgba(0, 0, 0, 0.08);
}

.order-summary-line {
  display: flex;
  align-items: center;
  gap: 6px;
  margin-bottom: 6px;
}

.order-title {
  flex: 1;
  font-weight: 600;
  font-size: 0.9rem;
  color: var(--text-primary);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.btn-detail {
  background: transparent;
  border: none;
  color: var(--primary-color);
  cursor: pointer;
  padding: 2px 6px;
  font-size: 0.75rem;
  transition: all 0.2s;
}

.btn-detail:hover {
  color: #66b1ff;
  transform: scale(1.1);
}

.btn-mini-danger {
  background: transparent;
  border: none;
  color: var(--danger-color);
  cursor: pointer;
  padding: 0;
  width: 20px;
  height: 20px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 50%;
  font-size: 1.1rem;
  font-weight: bold;
  transition: all 0.2s;
}

.btn-mini-danger:hover {
  background: var(--danger-color);
  color: white;
}

.order-basic-info {
  font-size: 0.8rem;
  color: var(--text-secondary);
  margin-bottom: 6px;
  padding-left: 2px;
}

.info-item {
  color: var(--text-regular);
}

.info-divider {
  margin: 0 6px;
  color: var(--border-color);
}

.order-detail-expand {
  margin-top: 8px;
  padding-top: 8px;
  border-top: 1px dashed var(--border-color);
  animation: slideDown 0.3s ease;
}

@keyframes slideDown {
  from {
    opacity: 0;
    transform: translateY(-10px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

.detail-section {
  margin-bottom: 8px;
}

.detail-label {
  font-size: 0.75rem;
  color: var(--text-secondary);
  font-weight: 500;
  margin-bottom: 3px;
}

.detail-value {
  font-size: 0.8rem;
  color: var(--text-regular);
  line-height: 1.5;
}

.note-text {
  color: var(--text-secondary);
  font-style: italic;
  font-size: 0.75rem;
}

.cart-footer {
  padding: 15px;
  border-top: 2px solid #f0f0f0;
  flex-shrink: 0;
}

.cart-empty {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  color: var(--text-secondary);
  padding: 40px 20px;
}

.empty-icon {
  font-size: 3rem;
  opacity: 0.3;
  margin-bottom: 12px;
}

.cart-empty p {
  font-size: 0.85rem;
  margin: 0;
}

/* ==================== 响应式调整 ==================== */
@media (max-width: 1600px) {
  .patient-panel:not(.collapsed) {
    width: 220px;
  }
  
  .cart-panel:not(.collapsed) {
    width: 280px;
  }
}

@media (max-width: 1400px) {
  .patient-panel:not(.collapsed) {
    width: 200px;
  }
  
  .cart-panel:not(.collapsed) {
    width: 260px;
  }
}

@media (max-width: 1200px) {
  .patient-panel,
  .cart-panel {
    position: fixed;
    top: 60px;
    height: calc(100vh - 60px);
    z-index: 100;
    max-height: none;
  }
  
  .patient-panel {
    left: 0;
    box-shadow: 2px 0 12px rgba(0, 0, 0, 0.15);
  }
  
  .cart-panel {
    right: 0;
    box-shadow: -2px 0 12px rgba(0, 0, 0, 0.15);
  }
}

.btn-submit-all {
  width: 100%;
  background: var(--success-color) !important;
  color: white;
  border: none;
  padding: 14px;
  border-radius: var(--radius-small);
  cursor: pointer;
  font-weight: bold;
  font-size: 1rem;
  transition: all 0.3s;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
}

.btn-submit-all:hover:not(:disabled) {
  background: #85ce61 !important;
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(103, 194, 58, 0.4);
}

.btn-submit-all:disabled {
  background: #c8e6c9 !important;
  cursor: not-allowed;
  opacity: 0.6;
}

/* ==================== 响应式调整 ==================== */
@media (max-width: 1400px) {
  .main-content {
    grid-template-columns: 1fr 340px;
  }
}

@media (max-width: 1200px) {
  .main-content {
    grid-template-columns: 1fr;
  }
  
  .cart-area {
    max-height: 500px;
  }
}
</style>