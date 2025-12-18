<template>
  <div class="order-container">
    <nav class="navbar">
      <div class="logo">CareFlow | 医嘱开具工作台</div>
      <div class="user-info">
        <span>{{ currentUser.fullName }} ({{ currentUser.role }})</span>
        <button @click="$router.push('/home')" class="back-btn">返回首页</button>
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
        <div class="diagnosis">诊断：肺部感染 / 待查</div>
      </header>

      <div class="main-content">
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
              <div class="form-row">
                <label>医嘱类型：</label>
                <el-radio-group v-model="currentOrder.isLongTerm">
                  <el-radio :label="true">长期</el-radio>
                  <el-radio :label="false">临时</el-radio>
                </el-radio-group>
              </div>

              <div class="drug-group-box">
                <div v-for="(item, index) in currentOrder.items" :key="index" class="drug-item-row">
                  <el-select v-model="item.drugId" filterable placeholder="搜索药品名称/简拼" class="drug-select">
                    <el-option v-for="d in drugDict" :key="d.id" :label="d.genericName" :value="d.id">
                      <span>{{ d.genericName }} [{{ d.specification }}]</span>
                    </el-option>
                  </el-select>
                  <el-input v-model="item.dosage" placeholder="剂量" style="width: 100px" />
                  <el-input v-model="item.note" placeholder="备注" style="width: 120px" />
                  <button @click="removeDrug(index)" class="btn-icon">×</button>
                </div>
                <button @click="addDrug" class="btn-add-drug">+ 添加组合药物 (溶媒/加强)</button>
              </div>

              <div class="form-grid">
                <div class="grid-item">
                  <label>给药途径</label>
                  <el-select v-model="currentOrder.usageRoute">
                    <el-option label="静脉滴注" :value="20" />
                    <el-option label="口服" :value="1" />
                    <el-option label="皮下注射" :value="11" />
                  </el-select>
                </div>
                <div class="grid-item">
                  <label>执行频次</label>
                  <el-select v-model="currentOrder.freqCode">
                    <el-option label="QD (每天一次)" value="QD" />
                    <el-option label="BID (每天两次)" value="BID" />
                    <el-option label="TID (每天三次)" value="TID" />
                  </el-select>
                </div>
              </div>

              <div class="time-slots-selector">
                <label>执行时段预览 (SmartSlots):</label>
                <div class="slots-grid">
                  <div v-for="slot in timeSlotDict" :key="slot.id" 
                       :class="['slot-tag', { selected: (currentOrder.smartSlotsMask & slot.id) }]"
                       @click="toggleSlot(slot.id)">
                    {{ slot.slotName }}
                  </div>
                </div>
              </div>
            </div>

            <div v-else class="placeholder-form">
              正在开发 {{ activeType }} 的详细表单...
            </div>

            <div class="form-actions">
              <button @click="addToCart" class="btn-primary">暂存医嘱</button>
            </div>
          </div>
        </section>

        <section class="cart-area">
          <h3>待提交医嘱 ({{ orderCart.length }})</h3>
          <div class="cart-list">
            <div v-for="(o, idx) in orderCart" :key="idx" class="cart-item">
              <div class="cart-item-header">
                <span class="type-tag">{{ o.orderType }}</span>
                <button @click="orderCart.splice(idx, 1)">删除</button>
              </div>
              <div class="cart-item-content">
                <div v-for="item in o.items" :key="item.drugId" class="med-line">
                  {{ getDrugName(item.drugId) }} {{ item.dosage }}
                </div>
                <div class="usage-line">{{ o.usageRoute }} | {{ o.freqCode }}</div>
              </div>
            </div>
          </div>
          <button @click="submitAll" class="btn-submit-all" :disabled="!orderCart.length">
            确认并上传签署
          </button>
        </section>
      </div>
    </main>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue';

const currentUser = ref({ fullName: '张医生', role: 'Doctor' });
const activeType = ref('MedicationOrder');
const selectedPatient = ref({ id: 'P001', name: '张三', gender: '男', age: 34, weight: 70.5, bedId: 'IM-W01-001', nursingGrade: 2 });

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
  smartSlotsMask: 512, // 默认上午
  timingStrategy: 'SLOTS'
});

const orderCart = ref([]);
const drugDict = ref([]); // 对应从后端获取的 Drug[]
const timeSlotDict = ref([]); // 对应 HospitalTimeSlot[]

const addDrug = () => currentOrder.items.push({ drugId: '', dosage: '', note: '' });
const removeDrug = (index) => currentOrder.items.splice(index, 1);
const toggleSlot = (bit) => currentOrder.smartSlotsMask ^= bit;

const addToCart = () => {
  // 深拷贝当前医嘱到暂存区
  orderCart.value.push(JSON.parse(JSON.stringify({
    ...currentOrder,
    orderType: activeType.value,
    patientId: selectedPatient.value.id
  })));
};

const submitAll = () => {
  console.log("提交给后端 API:", orderCart.value);
  alert("医嘱已成功同步至 CareFlow 数据库");
  orderCart.value = [];
};

// 模拟加载数据
onMounted(() => {
  // 实际开发中通过 API 获取
  drugDict.value = [
    { id: 'DRUG002', genericName: '0.9%氯化钠注射液', specification: '250ml/袋' },
    { id: 'DRUG008', genericName: '头孢曲松钠', specification: '1.0g/瓶' }
  ];
  timeSlotDict.value = [
    { id: 1, slotName: '早餐前' }, { id: 512, slotName: '上午' }, { id: 2, slotName: '早餐后' }
  ];
});

const getDrugName = (id) => drugDict.value.find(d => d.id === id)?.genericName || id;
</script>

<style scoped>
/* 继承 Home.vue 的风格 */
.navbar {
  display: flex; justify-content: space-between; padding: 0.8rem 2rem;
  background-color: #2c3e50; color: white; align-items: center;
}
.order-layout { padding: 20px; background: #f4f7f9; min-height: calc(100vh - 60px); }

/* 患者上下文卡片 */
.patient-context {
  display: flex; align-items: center; background: white;
  padding: 15px 25px; border-radius: 8px; margin-bottom: 20px;
  box-shadow: 0 2px 10px rgba(0,0,0,0.05); border-left: 5px solid #409eff;
}
.patient-badge { background: #409eff; color: white; padding: 5px 12px; border-radius: 4px; font-weight: bold; margin-right: 20px; }
.patient-info .name { font-size: 1.2rem; font-weight: bold; margin-right: 15px; }
.patient-info .detail { color: #666; margin-right: 20px; }

/* 主区域布局 */
.main-content { display: grid; grid-template-columns: 1fr 350px; gap: 20px; }

/* 表单录入 */
.tabs-header { display: flex; margin-bottom: -1px; }
.tab-item {
  padding: 10px 25px; border: none; background: #e0e0e0; cursor: pointer;
  border-radius: 8px 8px 0 0; margin-right: 5px; color: #666;
}
.tab-item.active { background: white; color: #409eff; font-weight: bold; }

.form-card { background: white; padding: 25px; border-radius: 0 8px 8px 8px; box-shadow: 0 2px 12px rgba(0,0,0,0.1); }

/* 混合药物样式 */
.drug-group-box {
  background: #f9fafc; border: 1px dashed #dcdfe6;
  padding: 15px; border-radius: 6px; margin: 15px 0;
}
.drug-item-row { display: flex; gap: 10px; margin-bottom: 10px; align-items: center; }
.btn-add-drug { border: none; background: none; color: #409eff; cursor: pointer; padding: 5px; font-size: 0.9rem; }

/* 掩码选择样式 */
.slots-grid { display: flex; gap: 10px; margin-top: 10px; flex-wrap: wrap; }
.slot-tag {
  padding: 6px 15px; border: 1px solid #dcdfe6; border-radius: 20px;
  font-size: 0.85rem; cursor: pointer; transition: all 0.2s;
}
.slot-tag.selected { background: #409eff; color: white; border-color: #409eff; }

/* 购物车区域 */
.cart-area { background: #fff; padding: 20px; border-radius: 8px; box-shadow: 0 2px 12px rgba(0,0,0,0.1); display: flex; flex-direction: column; }
.cart-list { flex: 1; overflow-y: auto; margin: 15px 0; }
.cart-item { border: 1px solid #ebeef5; border-radius: 6px; padding: 12px; margin-bottom: 10px; font-size: 0.9rem; }
.type-tag { font-size: 0.7rem; background: #f0f9eb; color: #67c23a; padding: 2px 5px; }
.btn-submit-all {
  background: #67c23a; color: white; border: none; padding: 12px;
  border-radius: 4px; cursor: pointer; font-weight: bold;
}
.btn-submit-all:disabled { background: #c8e6c9; cursor: not-allowed; }
</style>