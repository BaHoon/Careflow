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
                :class="['patient-card', { active: patient.id === selectedPatient?.id }]"
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
                    <el-radio label="IMMEDIATE">
                      <i class="el-icon-video-play"></i> 立即执行
                    </el-radio>
                    <el-radio label="SPECIFIC">
                      <i class="el-icon-alarm-clock"></i> 指定时间单次执行
                    </el-radio>
                  </el-radio-group>
                </div>

                <div class="form-row" v-if="currentOrder.isLongTerm">
                  <label class="required">执行策略：</label>
                  <el-radio-group v-model="currentOrder.timingStrategy" @change="onStrategyChange">
                    <el-radio label="SLOTS">
                      <i class="el-icon-clock"></i> 按时段执行 (如早餐前、午餐后)
                    </el-radio>
                    <el-radio label="CYCLIC">
                      <i class="el-icon-refresh"></i> 固定间隔执行 (如每6小时一次)
                    </el-radio>
                  </el-radio-group>
                </div>

                <!-- 步骤3：根据策略显示对应配置 -->
                <div class="strategy-config">
                  <!-- 3.0 IMMEDIATE策略：显示开始执行时间 -->
                  <div class="form-row" v-if="currentOrder.timingStrategy === 'IMMEDIATE'">
                    <label class="required">开始执行时间：</label>
                    <el-date-picker 
                      v-model="currentOrder.startTime"
                      type="datetime"
                      placeholder="立即执行时间"
                      :disabled="true"
                      format="YYYY-MM-DD HH:mm"
                      value-format="YYYY-MM-DDTHH:mm:ss"
                      style="width: 280px"
                    />
                    <span class="tip-text">立即执行，时间不可修改</span>
                  </div>

                  <!-- 3.1 SPECIFIC策略：日期时间选择器 -->
                  <div class="form-row" v-if="currentOrder.timingStrategy === 'SPECIFIC'">
                    <label class="required">指定执行时间：</label>
                    <el-date-picker 
                      v-model="currentOrder.startTime"
                      type="datetime"
                      placeholder="选择具体日期和时间"
                      :disabled-date="disablePastDates"
                      format="YYYY-MM-DD HH:mm"
                      value-format="YYYY-MM-DDTHH:mm:ss"
                      style="width: 280px"
                    />
                  </div>

                  <!-- 3.2 CYCLIC策略：开始时间 + 间隔小时 + 间隔天数 -->
                  <div v-if="currentOrder.timingStrategy === 'CYCLIC'">
                    <div class="form-row">
                      <label class="required">首次执行时间：</label>
                      <el-date-picker 
                        v-model="currentOrder.startTime"
                        type="datetime"
                        placeholder="选择首次执行时间"
                        :disabled-date="disablePastDates"
                        format="YYYY-MM-DD HH:mm"
                        value-format="YYYY-MM-DDTHH:mm:ss"
                        style="width: 280px"
                      />
                    </div>
                    <div class="form-row">
                      <label class="required">间隔小时数：</label>
                      <el-input-number 
                        v-model="currentOrder.intervalHours" 
                        :min="0.5" 
                        :max="168"
                        :step="0.5"
                        :precision="1"
                        placeholder="执行间隔（小时）"
                        style="width: 150px"
                      />
                      <span class="tip-text">每次执行的间隔时间（小时），如8表示每8小时一次</span>
                    </div>
                    <div class="form-row">
                      <label class="required">间隔天数：</label>
                      <el-input-number 
                        v-model="currentOrder.intervalDays" 
                        :min="1" 
                        :max="30"
                        placeholder="间隔天数"
                        style="width: 150px"
                      />
                      <span class="tip-text">1=每天执行，2=隔天执行（通常设为1）</span>
                    </div>
                  </div>

                  <!-- 3.3 SLOTS策略：开始执行时间 -->
                  <div v-if="currentOrder.timingStrategy === 'SLOTS'">
                    <div class="form-row">
                      <label class="required">开始执行时间：</label>
                      <el-date-picker 
                        v-model="currentOrder.startTime"
                        type="datetime"
                        placeholder="选择开始执行时间"
                        :disabled-date="disablePastDates"
                        format="YYYY-MM-DD HH:mm"
                        value-format="YYYY-MM-DDTHH:mm:ss"
                        style="width: 280px"
                      />
                      <span class="tip-text">从什么时间开始按时段执行</span>
                    </div>
                  </div>

                  <!-- 3.4 医嘱结束时间（SPECIFIC策略下隐藏，因为已在上面设置） -->
                  <div class="form-row" v-if="currentOrder.timingStrategy !== 'SPECIFIC'">
                    <label class="required">{{ currentOrder.isLongTerm ? '医嘱结束时间' : '医嘱开始时间' }}：</label>
                    <el-date-picker 
                      v-model="currentOrder.plantEndTime"
                      type="datetime"
                      :placeholder="currentOrder.isLongTerm ? '选择医嘱结束时间' : '选择医嘱开始时间'"
                      :disabled="currentOrder.timingStrategy === 'IMMEDIATE'"
                      :disabled-date="disablePastDates"
                      :disabled-time="currentOrder.isLongTerm ? disableTimesBeforeStart : undefined"
                      format="YYYY-MM-DD HH:mm"
                      value-format="YYYY-MM-DDTHH:mm:ss"
                      style="width: 280px"
                    />
                    <span class="tip-text" v-if="currentOrder.timingStrategy === 'IMMEDIATE'">立即执行，时间不可修改</span>
                    <span class="tip-text" v-else-if="currentOrder.isLongTerm">不能早于开始执行时间</span>
                  </div>

                  <!-- 3.5 SLOTS策略：时段选择 + 间隔天数 -->
                  <div v-if="currentOrder.timingStrategy === 'SLOTS'">
                    <div class="form-row">
                      <label class="required">执行时段：</label>
                      <div class="time-slots-selector" style="margin-top: 10px;">
                        <div class="slot-category">
                          <div class="category-title">🍽️ 三餐前后及睡前</div>
                          <div class="slots-grid">
                            <div v-for="slot in allTimeSlots" :key="slot.id" 
                                 :class="['slot-tag', { selected: isSlotSelected(slot.id) }]"
                                 @click="toggleSlot(slot.id)">
                              <i class="el-icon-check" v-if="isSlotSelected(slot.id)"></i>
                              {{ slot.slotName }}
                              <span class="time-hint">{{ formatTime(slot.defaultTime) }}</span>
                            </div>
                          </div>
                        </div>
                      </div>
                    </div>
                    <div class="form-row">
                      <label class="required">间隔天数：</label>
                      <el-input-number 
                        v-model="currentOrder.intervalDays"
                        :min="1"
                        :max="30"
                        placeholder="间隔天数" 
                        style="width: 150px"
                      />
                      <span class="tip-text">1=每天执行，2=隔天执行，依此类推</span>
                    </div>
                    <div class="freq-reminder" v-if="currentOrder.smartSlotsMask > 0">
                      <i class="el-icon-info"></i> 
                      已选择 <strong>{{ getSelectedSlotsCount() }}</strong> 个时段，每 <strong>{{ currentOrder.intervalDays }}</strong> 天执行 <strong>{{ getSelectedSlotsCount() }}</strong> 次
                    </div>
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
                </div>
              </div>

              <!-- 步骤6：医嘱备注 -->
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

            <!-- TODO: 检查医嘱表单 -->
            <!-- 位置: 检查医嘱（CT、MRI、X光、超声等）开具表单 -->
            <div v-else-if="activeType === 'InspectionOrder'" class="inspection-form">
              <!-- TODO 1: 检查类型选择 -->
              <!-- 需要字段:
                   - inspectionType: 检查类型（下拉框）
                     选项: CT、MRI、X-Ray、Ultrasound（超声）、Endoscopy（内窥镜）等
                   示例: <el-select v-model="inspectionOrder.inspectionType"> -->

              <!-- TODO 2: 检查部位选择 -->
              <!-- 需要字段:
                   - targetOrgan: 检查部位（下拉框或级联选择器）
                     选项: Head（头部）、Chest（胸部）、Abdomen（腹部）、Extremities（四肢）等
                   示例: <el-cascader v-model="inspectionOrder.targetOrgan"> -->

              <!-- TODO 3: 紧急程度选择 -->
              <!-- 需要字段:
                   - urgency: 紧急程度（单选按钮组）
                     选项: urgent（紧急）、normal（常规）、routine（例行）
                   示例: <el-radio-group v-model="inspectionOrder.urgency"> -->

              <!-- TODO 4: 造影剂选项 -->
              <!-- 需要字段:
                   - contrast: 是否使用造影剂（复选框）
                   - 如果勾选，需要显示过敏史确认
                   示例: <el-checkbox v-model="inspectionOrder.contrast"> -->

              <!-- TODO 5: 预约时间选择 -->
              <!-- 需要字段:
                   - scheduledTime: 预约时间（日期时间选择器）
                   - 需要与设备排班联动，显示可用时段
                   示例: <el-date-picker v-model="inspectionOrder.scheduledTime"> -->

              <!-- TODO 6: 临床资料 -->
              <!-- 需要字段:
                   - clinicalInfo: 临床症状、病史（文本域）
                   示例: <el-input type="textarea" v-model="inspectionOrder.clinicalInfo"> -->

              <!-- TODO 7: 备注 -->
              <!-- 需要字段:
                   - remarks: 特殊说明（文本域）
                   示例: <el-input type="textarea" v-model="inspectionOrder.remarks"> -->

              <div class="placeholder-form">
                ⚠️ 检查医嘱表单开发中
                <br>需实现上述7个字段的表单组件
              </div>
            </div>

            <!-- TODO: 手术医嘱表单 -->
            <!-- 位置: 手术/操作类医嘱开具表单 -->
            <div v-else-if="activeType === 'SurgicalOrder'" class="surgical-form">
              <!-- TODO 1: 手术名称 -->
              <!-- 需要字段:
                   - surgeryName: 手术名称（搜索下拉框）
                     示例: 阑尾切除术、胆囊切除术、疝修补术等
                   - 支持模糊搜索
                   示例: <el-autocomplete v-model="surgicalOrder.surgeryName"> -->

              <!-- TODO 2: 手术类型 -->
              <!-- 需要字段:
                   - surgeryType: 手术类型（单选按钮）
                     选项: Elective（择期手术）、Emergency（急诊手术）
                   示例: <el-radio-group v-model="surgicalOrder.surgeryType"> -->

              <!-- TODO 3: 麻醉方式 -->
              <!-- 需要字段:
                   - anesthesiaMethod: 麻醉方式（下拉框）
                     选项: General（全身麻醉）、Local（局部麻醉）、Epidural（硬膜外麻醉）、Spinal（脊髓麻醉）
                   示例: <el-select v-model="surgicalOrder.anesthesiaMethod"> -->

              <!-- TODO 4: 主刀医生 -->
              <!-- 需要字段:
                   - surgeonId: 主刀医生ID（下拉框）
                   - 需要从后端获取外科医生列表
                   示例: <el-select v-model="surgicalOrder.surgeonId" @focus="loadSurgeons"> -->

              <!-- TODO 5: 助手医生（多选） -->
              <!-- 需要字段:
                   - assistantIds: 助手医生ID列表（多选下拉框）
                   - 可以选择0-N个助手
                   示例: <el-select v-model="surgicalOrder.assistantIds" multiple> -->

              <!-- TODO 6: 手术时间 -->
              <!-- 需要字段:
                   - scheduledTime: 计划手术时间（日期时间选择器）
                   - 需要与手术室排班联动
                   示例: <el-date-picker v-model="surgicalOrder.scheduledTime"> -->

              <!-- TODO 7: 预计时长 -->
              <!-- 需要字段:
                   - estimatedDuration: 预计手术时长（数字输入框，单位：分钟）
                   示例: <el-input-number v-model="surgicalOrder.estimatedDuration" :min="15" :step="15"> -->

              <!-- TODO 8: 手术室选择 -->
              <!-- 需要字段:
                   - operatingRoom: 手术室编号（下拉框）
                   - 需要显示手术室状态（空闲/占用）
                   示例: <el-select v-model="surgicalOrder.operatingRoom"> -->

              <!-- TODO 9: 备注 -->
              <!-- 需要字段:
                   - remarks: 特殊准备事项（文本域）
                   示例: <el-input type="textarea" v-model="surgicalOrder.remarks"> -->

              <div class="placeholder-form">
                ⚠️ 手术医嘱表单开发中
                <br>需实现上述9个字段的表单组件
              </div>
            </div>

            <!-- 操作医嘱表单 -->
            <div v-else-if="activeType === 'OperationOrder'" class="operation-form">
              <!-- 操作基本信息 -->
              <div class="placeholder-form">
                ⚠️ 操作医嘱表单开发中
                <br>需实现上述个字段的表单组件
              </div>
            </div>

            <!-- 其他未知类型的占位符 -->
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
import { ref, reactive, computed, onMounted, watch } from 'vue';
import { ElMessage } from 'element-plus';
import { getPatientList } from '../api/patient';
import { getDrugList } from '../api/drug';
import { getTimeSlots } from '../api/hospitalConfig';
import { batchCreateMedicationOrders } from '../api/medicationOrder';
import { batchCreateInspectionOrders } from '../api/inspectionOrder';
import { batchCreateSurgicalOrders } from '../api/surgicalOrder';
import { batchCreateOperationOrders } from '../api/operationOrder';
import { toBeijingTimeISO } from '../utils/timezone';

// 当前用户信息（从localStorage获取登录信息）
const getUserInfo = () => {
  try {
    const userInfoStr = localStorage.getItem('userInfo');
    if (userInfoStr) {
      return JSON.parse(userInfoStr);
    }
  } catch (error) {
    console.error('解析用户信息失败:', error);
  }
  // 如果没有登录信息，返回默认值
  return { 
    staffId: 'DOC001', 
    fullName: '未登录', 
    role: 'Doctor',
    deptCode: '' 
  };
};

const currentUser = ref(getUserInfo());

const activeType = ref('MedicationOrder');
const selectedPatient = ref(null); // 初始为空，从患者列表选择

const types = [
  { label: '药物医嘱', val: 'MedicationOrder' },
  { label: '检查申请', val: 'InspectionOrder' },
  { label: '手术医嘱', val: 'SurgicalOrder' },
  { label: '护理操作', val: 'OperationOrder' }
];

// TODO: 添加检查医嘱的响应式数据
// 参考DTO: DTOs/InspectionOrders/BatchCreateInspectionOrderDto.cs
// const inspectionOrder = reactive({
//   inspectionType: '',        // 检查类型: CT, MRI, X-Ray, Ultrasound
//   targetOrgan: '',           // 检查部位: Head, Chest, Abdomen, Extremities
//   urgency: 'normal',         // 紧急程度: urgent, normal, routine
//   contrast: false,           // 是否造影剂
//   scheduledTime: null,       // 预约时间
//   clinicalInfo: '',          // 临床资料
//   remarks: ''                // 备注
// });

// TODO: 添加手术医嘱的响应式数据
// 参考DTO: DTOs/SurgicalOrders/BatchCreateSurgicalOrderDto.cs
// const surgicalOrder = reactive({
//   surgeryName: '',           // 手术名称
//   surgeryType: 'Elective',   // 手术类型: Elective, Emergency
//   anesthesiaMethod: '',      // 麻醉方式: General, Local, Epidural, Spinal
//   surgeonId: '',             // 主刀医生ID
//   assistantIds: [],          // 助手医生ID数组
//   scheduledTime: null,       // 手术时间
//   estimatedDuration: null,   // 预计时长（分钟）
//   operatingRoom: '',         // 手术室
//   remarks: ''                // 备注
// });

// 操作医嘱的响应式数据
// 参考DTO: DTOs/OperationOrders/BatchCreateOperationOrderDto.cs
// const operationOrder = reactive({
//   operationCode: '',         // 操作代码
//   operationName: '',         // 操作名称
//   targetSite: '',            // 操作部位（可选）
//   scheduledTime: null,       // 执行时间
//   remarks: ''                // 备注
// });

// 药品医嘱响应式数据
const currentOrder = reactive({
  // 基础信息
  isLongTerm: true,  // 医嘱类型：true=长期，false=临时
  items: [{ drugId: '', dosage: '', note: '' }],
  usageRoute: 20,
  
  // 时间策略核心字段（与后端完全对齐）
  timingStrategy: '',      // 'IMMEDIATE' | 'SPECIFIC' | 'CYCLIC' | 'SLOTS'
  startTime: null,         // DateTime? - SPECIFIC/CYCLIC/SLOTS 需要
  plantEndTime: null,      // DateTime - 所有策略必填
  intervalHours: null,     // decimal? - 仅 CYCLIC 使用
  intervalDays: 1,         // int - CYCLIC/SLOTS 使用
  smartSlotsMask: 0,       // int - 仅 SLOTS 使用
  
  remarks: ''
});

// 策略配置映射
const strategyConfig = {
  // 临时医嘱可选策略
  temporary: [
    {
      value: 'IMMEDIATE',
      label: '立即执行',
      icon: '⚡',
      description: '下达后立即执行，适用于紧急用药'
    },
    {
      value: 'SPECIFIC',
      label: '指定时间',
      icon: '📅',
      description: '指定具体执行时间，适用于预约用药'
    }
  ],
  
  // 长期医嘱可选策略
  longTerm: [
    {
      value: 'SLOTS',
      label: '时段执行',
      icon: '🕐',
      description: '按医院标准时段执行（如：早中晚餐前后）'
    },
    {
      value: 'CYCLIC',
      label: '周期执行',
      icon: '🔄',
      description: '按固定时间间隔执行（如：每8小时一次）'
    }
  ]
};

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

// 计算属性：所有时段（三餐前后+睡前）
const allTimeSlots = computed(() => timeSlotDict.value);

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

// 计算属性：表单验证（基础版本，步骤5会完善）
// TODO: 为其他医嘱类型添加表单验证逻辑

const isFormValid = computed(() => {
  // 根据医嘱类型进行不同的表单验证
  if (activeType.value === 'OperationOrder') {
    // TODO: 操作医嘱验证：操作代码、操作名称、执行时间为必填

  } else if (activeType.value === 'InspectionOrder') {
    // TODO: 检查医嘱验证（待实现表单后补充）
    return false;
  } else if (activeType.value === 'SurgicalOrder') {
    // TODO: 手术医嘱验证（待实现表单后补充）
    return false;
  } else {
    // 药品医嘱验证（原有逻辑）
    if (!currentOrder.items.some(i => i.drugId && i.dosage)) return false;
    if (!currentOrder.usageRoute) return false;
    if (!currentOrder.timingStrategy) return false;
    if (!currentOrder.plantEndTime) return false;

    const strategy = currentOrder.timingStrategy.toUpperCase();
    if (strategy === 'SPECIFIC' && !currentOrder.startTime) return false;
    if (strategy === 'CYCLIC' && (!currentOrder.startTime || !currentOrder.intervalHours)) return false;
    if (strategy === 'SLOTS' && (!currentOrder.startTime || currentOrder.smartSlotsMask <= 0)) return false;

    return true;
  }
});

// 计算属性：根据医嘱类型返回可用策略
const availableStrategies = computed(() => {
  return currentOrder.isLongTerm 
    ? strategyConfig.longTerm 
    : strategyConfig.temporary;
});

// 医嘱类型切换
const onOrderTypeChange = (isLongTerm) => {
  currentOrder.isLongTerm = isLongTerm;
  
  // 重置策略选择
  currentOrder.timingStrategy = '';
  
  // 清空所有时间相关字段
  currentOrder.startTime = null;
  currentOrder.plantEndTime = null;
  currentOrder.intervalHours = null;
  currentOrder.intervalDays = 1;
  currentOrder.smartSlotsMask = 0;
};

// 策略选择处理函数（智能设置默认值）
const selectStrategy = (strategy) => {
  currentOrder.timingStrategy = strategy;
  
  // 重置所有策略相关字段
  currentOrder.startTime = null;
  currentOrder.plantEndTime = null;
  currentOrder.intervalHours = null;
  currentOrder.intervalDays = 1;
  currentOrder.smartSlotsMask = 0;
  
  // 根据策略设置智能默认值
  const now = new Date();
  
  switch (strategy.toUpperCase()) {
    case 'IMMEDIATE':
      // 立即执行：开始时间和结束时间都为当前时间（临时医嘱）
      const immediateNow = new Date();
      currentOrder.startTime = getLocalISOString(immediateNow);
      currentOrder.plantEndTime = getLocalISOString(immediateNow);
      break;
      
    case 'SPECIFIC':
      // 指定时间单次执行：开始时间和结束时间相同
      const specificNow = new Date();
      currentOrder.startTime = getLocalISOString(specificNow);
      // plantEndTime 与 startTime 相同（单次执行）
      currentOrder.plantEndTime = getLocalISOString(specificNow);
      break;
      
    case 'CYCLIC':
      // 周期执行：默认每8小时，从当前时间开始
      const cyclicNow = new Date();
      currentOrder.startTime = getLocalISOString(cyclicNow);
      currentOrder.intervalHours = 8;
      currentOrder.intervalDays = 1;
      
      const cyclicEnd = new Date();
      cyclicEnd.setDate(cyclicEnd.getDate() + 7); // 7天后
      currentOrder.plantEndTime = getLocalISOString(cyclicEnd);
      break;
      
    case 'SLOTS':
      // 时段执行：默认从当前时间开始，每天执行
      const slotsNow = new Date();
      currentOrder.startTime = getLocalISOString(slotsNow);
      currentOrder.intervalDays = 1;
      
      const slotsEnd = new Date();
      slotsEnd.setDate(slotsEnd.getDate() + 7); // 7天后
      currentOrder.plantEndTime = getLocalISOString(slotsEnd);
      break;
  }
  
  ElMessage.success(`已切换至「${getStrategyLabel(strategy)}」策略`);
};

// 兼容旧的onStrategyChange调用（如果模板中还有使用）
const onStrategyChange = () => {
  selectStrategy(currentOrder.timingStrategy);
};

// 获取本地时间的 ISO 格式字符串（不带时区标识，用于 el-date-picker 显示）
const getLocalISOString = (date) => {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  const hours = String(date.getHours()).padStart(2, '0');
  const minutes = String(date.getMinutes()).padStart(2, '0');
  const seconds = String(date.getSeconds()).padStart(2, '0');
  return `${year}-${month}-${day}T${hours}:${minutes}:${seconds}`;
};

// 🔥 监听 SPECIFIC 策略的 startTime 变化，自动同步到 plantEndTime
watch(() => currentOrder.startTime, (newVal) => {
  if (currentOrder.timingStrategy === 'SPECIFIC' && newVal) {
    currentOrder.plantEndTime = newVal;
  }
});

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
    count += mask & 1;
    mask >>= 1;
  }
  return count;
};

// 折叠切换
const toggleLeft = () => {
  leftCollapsed.value = !leftCollapsed.value;
};

const toggleRight = () => {
  rightCollapsed.value = !rightCollapsed.value;
};

// 患者切换
const handlePatientClick = (patient) => {
  if (patient.id === selectedPatient.value?.id) return;
  
  const hasUnsubmittedData = 
    currentOrder.items.some(i => i.drugId && i.dosage) || 
    orderCart.value.length > 0;
  
  if (hasUnsubmittedData) {
    if (confirm('切换患者将清空当前表单和待提交清单，是否继续？')) {
      selectedPatient.value = patient;
      clearForm();
      orderCart.value = [];
      expandedOrders.value = [];
      ElMessage.success(`已切换至患者：${patient.name} (${patient.bedId})`);
    }
  } else {
    selectedPatient.value = patient;
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

// TODO: 清空表单时需根据医嘱类型清空对应的数据
const clearForm = () => {
  if (activeType.value === 'OperationOrder') {
    // TODO: 清空操作医嘱表单

  } else if (activeType.value === 'InspectionOrder') {
    // TODO: 清空检查医嘱表单（待实现表单后补充）
  } else if (activeType.value === 'SurgicalOrder') {
    // TODO: 清空手术医嘱表单（待实现表单后补充）
  } else {
    // 清空药品医嘱表单（原有逻辑）
    currentOrder.items = [{ drugId: '', dosage: '', note: '' }];
    currentOrder.usageRoute = 20;
    currentOrder.timingStrategy = '';
    currentOrder.startTime = null;
    currentOrder.plantEndTime = null;
    currentOrder.intervalHours = null;
    currentOrder.intervalDays = 1;
    currentOrder.smartSlotsMask = 0;
    currentOrder.remarks = '';
  }
  ElMessage.success('表单已清空');
};

// 暂存医嘱到待提交清单
const addToCart = () => {
  if (!isFormValid.value) {
    ElMessage.warning('请完善必填项后再暂存');
    return;
  }
  
  // 根据医嘱类型暂存对应数据
  if (activeType.value === 'OperationOrder') {
    // TODO: 暂存操作医嘱
    ElMessage.warning('操作类医嘱表单开发中');
    return;
  } else if (activeType.value === 'InspectionOrder') {
    // TODO: 暂存检查医嘱（待实现表单后补充）
    ElMessage.warning('检查医嘱表单开发中');
    return;
  } else if (activeType.value === 'SurgicalOrder') {
    // TODO: 暂存手术医嘱（待实现表单后补充）
    ElMessage.warning('手术医嘱表单开发中');
    return;
  } else {
    // 暂存药品医嘱（原有逻辑）
    orderCart.value.push({
      ...JSON.parse(JSON.stringify(currentOrder)),
      orderType: 'MedicationOrder',
      patientId: selectedPatient.value.id
    });
  }
  
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
    // 🔥 按医嘱类型分组
    const medicationOrders = orderCart.value.filter(o => o.orderType === 'MedicationOrder' || !o.orderType);
    const inspectionOrders = orderCart.value.filter(o => o.orderType === 'InspectionOrder');
    const surgicalOrders = orderCart.value.filter(o => o.orderType === 'SurgicalOrder');
    const operationOrders = orderCart.value.filter(o => o.orderType === 'OperationOrder');

    const results = [];
    let successCount = 0;
    let errorMessages = [];

    // 💊 提交药品医嘱
    if (medicationOrders.length > 0) {
      const requestData = {
        patientId: selectedPatient.value?.id,
        doctorId: currentUser.value.staffId,
        orders: medicationOrders.map(order => ({
          isLongTerm: order.isLongTerm,
          timingStrategy: order.timingStrategy?.toUpperCase(),
          startTime: toBeijingTimeISO(order.startTime),
          plantEndTime: toBeijingTimeISO(order.plantEndTime),
          intervalHours: order.intervalHours,
          intervalDays: order.intervalDays,
          smartSlotsMask: order.smartSlotsMask,
          usageRoute: order.usageRoute,
          remarks: order.remarks,
          items: order.items
        }))
      };

      console.log('💊 提交药品医嘱:', requestData);
      
      try {
        const response = await batchCreateMedicationOrders(requestData);
        if (response.success) {
          successCount += medicationOrders.length;
          results.push(`药品医嘱: ${medicationOrders.length}条成功`);
        } else {
          errorMessages.push(`药品医嘱失败: ${response.message}`);
          if (response.errors) errorMessages.push(...response.errors);
        }
      } catch (error) {
        errorMessages.push(`药品医嘱提交异常: ${error.message}`);
      }
    }

    // TODO：检查是否正确调用检查医嘱的API和数据结构

    // 🔍 提交检查医嘱
    if (inspectionOrders.length > 0) {
      const requestData = {
        patientId: selectedPatient.value?.id,
        doctorId: currentUser.value.staffId,
        orders: inspectionOrders.map(order => ({
          inspectionType: order.inspectionType,
          targetOrgan: order.targetOrgan,
          urgency: order.urgency,
          contrast: order.contrast,
          scheduledTime: toBeijingTimeISO(order.scheduledTime),
          clinicalInfo: order.clinicalInfo
        }))
      };

      console.log('🔍 提交检查医嘱:', requestData);
      
      try {
        const response = await batchCreateInspectionOrders(requestData);
        if (response.success) {
          successCount += inspectionOrders.length;
          results.push(`检查医嘱: ${inspectionOrders.length}条成功`);
        } else {
          errorMessages.push(`检查医嘱失败: ${response.message}`);
          if (response.errors) errorMessages.push(...response.errors);
        }
      } catch (error) {
        errorMessages.push(`检查医嘱提交异常: ${error.message}`);
      }
    }

    // 🔪 提交手术医嘱
    if (surgicalOrders.length > 0) {
      const requestData = {
        patientId: selectedPatient.value?.id,
        doctorId: currentUser.value.staffId,
        orders: surgicalOrders.map(order => ({
          surgeryName: order.surgeryName,
          surgeryType: order.surgeryType,
          anesthesiaMethod: order.anesthesiaMethod,
          surgeonId: order.surgeonId,
          assistantIds: order.assistantIds,
          scheduledTime: toBeijingTimeISO(order.scheduledTime),
          estimatedDuration: order.estimatedDuration,
          operatingRoom: order.operatingRoom
        }))
      };

      console.log('🔪 提交手术医嘱:', requestData);
      
      try {
        const response = await batchCreateSurgicalOrders(requestData);
        if (response.success) {
          successCount += surgicalOrders.length;
          results.push(`手术医嘱: ${surgicalOrders.length}条成功`);
        } else {
          errorMessages.push(`手术医嘱失败: ${response.message}`);
          if (response.errors) errorMessages.push(...response.errors);
        }
      } catch (error) {
        errorMessages.push(`手术医嘱提交异常: ${error.message}`);
      }
    }

    // ⚙️ 提交操作医嘱
    if (operationOrders.length > 0) {
      const requestData = {
        patientId: selectedPatient.value?.id,
        doctorId: currentUser.value.staffId,
        orders: operationOrders.map(order => ({
          operationCode: order.operationCode,
          operationName: order.operationName,
          targetSite: order.targetSite || null,
          scheduledTime: toBeijingTimeISO(order.scheduledTime),
          remarks: order.remarks || null
        }))
      };

      console.log('⚙️ 提交操作医嘱:', requestData);
      
      try {
        const response = await batchCreateOperationOrders(requestData);
        if (response.success) {
          successCount += operationOrders.length;
          results.push(`操作医嘱: ${operationOrders.length}条成功`);
        } else {
          errorMessages.push(`操作医嘱失败: ${response.message}`);
          if (response.errors) errorMessages.push(...response.errors);
        }
      } catch (error) {
        errorMessages.push(`操作医嘱提交异常: ${error.message}`);
      }
    }

    // 📢 显示结果
    if (errorMessages.length === 0) {
      ElMessage.success(`✅ 成功提交 ${successCount} 条医嘱\n${results.join('\n')}`);
      orderCart.value = [];
      expandedOrders.value = [];
    } else {
      const successMsg = successCount > 0 ? `成功 ${successCount} 条, ` : '';
      ElMessage.warning(`${successMsg}失败 ${errorMessages.length} 项\n${errorMessages.slice(0, 3).join('\n')}`);
      // 移除成功的医嘱
      if (successCount > 0) {
        orderCart.value = orderCart.value.filter(order => {
          const type = order.orderType || 'MedicationOrder';
          if (type === 'MedicationOrder' && medicationOrders.length > 0) return false;
          if (type === 'InspectionOrder' && inspectionOrders.length > 0) return false;
          if (type === 'SurgicalOrder' && surgicalOrders.length > 0) return false;
          if (type === 'OperationOrder' && operationOrders.length > 0) return false;
          return true;
        });
      }
    }
  } catch (error) {
    console.error('提交失败:', error);
    ElMessage.error('提交失败: ' + (error.response?.data?.message || error.message));
  } finally {
    submitting.value = false;
  }
};

// 辅助函数
const disablePastDates = (time) => {
  return time.getTime() < Date.now() - 24 * 60 * 60 * 1000;
};

const disablePastTime = (date) => {
  const now = new Date();
  const selectedDate = new Date(date);
  
  // 如果选择的是今天，禁用过去的时间
  if (selectedDate.toDateString() === now.toDateString()) {
    return {
      disabledHours: () => {
        const hours = [];
        for (let i = 0; i < now.getHours(); i++) {
          hours.push(i);
        }
        return hours;
      },
      disabledMinutes: (hour) => {
        if (hour === now.getHours()) {
          const minutes = [];
          for (let i = 0; i <= now.getMinutes(); i++) {
            minutes.push(i);
          }
          return minutes;
        }
        return [];
      }
    };
  }
  return {};
};

const disableTimesBeforeStart = (date) => {
  if (!currentOrder.startTime) return {};
  
  const startTime = new Date(currentOrder.startTime);
  const selectedDate = new Date(date);
  
  // 如果选择的日期与开始日期是同一天，禁用开始时间之前的时间
  if (selectedDate.toDateString() === startTime.toDateString()) {
    return {
      disabledHours: () => {
        const hours = [];
        for (let i = 0; i < startTime.getHours(); i++) {
          hours.push(i);
        }
        return hours;
      },
      disabledMinutes: (hour) => {
        if (hour === startTime.getHours()) {
          const minutes = [];
          for (let i = 0; i <= startTime.getMinutes(); i++) {
            minutes.push(i);
          }
          return minutes;
        }
        return [];
      }
    };
  }
  return {};
};

const formatTime = (timeSpan) => {
  if (!timeSpan) return '';
  // timeSpan 格式: "07:00:00" (UTC时间)
  // 🔥 需要转换为北京时间（+8小时）显示
  const parts = timeSpan.split(':');
  let hours = parseInt(parts[0]);
  const minutes = parts[1];
  
  // UTC转北京时间：+8小时
  hours = (hours + 8) % 24;
  
  // 格式化为两位数
  const hoursStr = String(hours).padStart(2, '0');
  
  return `${hoursStr}:${minutes}`;
};

const getDrugName = (id) => {
  return drugDict.value.find(d => d.id === id)?.genericName || id;
};

const formatDateTime = (datetime) => {
  if (!datetime) return '';
  const date = new Date(datetime);
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  const hours = String(date.getHours()).padStart(2, '0');
  const minutes = String(date.getMinutes()).padStart(2, '0');
  return `${year}-${month}-${day} ${hours}:${minutes}`;
};

const formatDate = (datetime) => {
  if (!datetime) return '';
  const date = new Date(datetime);
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
};

// 加载患者列表的函数（根据当前医生的科室过滤）
const loadPatientList = async () => {
  try {
    // 获取当前医生的科室代码
    const deptCode = currentUser.value.deptCode;
    
    if (!deptCode) {
      ElMessage.warning('未获取到科室信息，将显示所有患者');
    }
    
    // 调用API，传入科室ID参数
    const patients = await getPatientList(deptCode);
    patientList.value = patients;
    
    // 如果有患者，默认选择第一个
    if (patients.length > 0 && !selectedPatient.value) {
      selectedPatient.value = patients[0];
    }
    
    console.log('患者列表加载成功:', patients.length, '科室:', deptCode);
  } catch (error) {
    console.error('加载患者列表失败:', error);
    ElMessage.error('加载患者列表失败: ' + (error.response?.data?.message || error.message));
    // 失败后使用空数组
    patientList.value = [];
  }
};

const getStrategyLabel = (strategy) => {
  const allStrategies = [...strategyConfig.temporary, ...strategyConfig.longTerm];
  const found = allStrategies.find(s => s.value === strategy);
  return found ? found.label : strategy;
};

const getRouteName = (routeId) => {
  const routes = {
    1: '口服', 10: '肌肉注射', 11: '皮下注射', 12: '皮内注射',
    20: '静脉滴注', 21: '静脉推注'
  };
  return routes[routeId] || routeId;
};

// getFreqDescription 已移除，改用 getStrategyLabel

const getOrderSummary = (order) => {
  const drugNames = order.items.map(i => getDrugName(i.drugId)).join('+');
  const strategyLabel = getStrategyLabel(order.timingStrategy);
  return `${drugNames} (${strategyLabel})`;
};

const getStrategyDescription = (order) => {
  const strategy = order.timingStrategy?.toUpperCase();
  switch (strategy) {
    case 'IMMEDIATE':
      return '立即执行';
    case 'SPECIFIC':
      return `指定时间: ${formatDateTime(order.startTime)}`;
    case 'CYCLIC':
      return `周期执行: 每${order.intervalHours}小时一次`;
    case 'SLOTS':
      const slots = timeSlotDict.value.filter(s => (order.smartSlotsMask & s.id) !== 0);
      const slotNames = slots.map(s => s.slotName).join('、');
      return `时段执行: ${slotNames}`;
    default:
      return getStrategyLabel(order.timingStrategy);
  }
};

// 页面初始化，加载所有基础数据
onMounted(async () => {
  console.log('开始加载基础数据...');
  
  try {
    // 并行加载所有基础数据
    const [drugsResponse, timeSlotsResponse] = await Promise.all([
      getDrugList({ pageSize: 500 }), // 加载所有药品（前500个）
      getTimeSlots()
    ]);
    
    // 药品字典
    if (drugsResponse && drugsResponse.items) {
      drugDict.value = drugsResponse.items;
      console.log('药品字典加载成功:', drugsResponse.items.length);
    }
    
    // 时段配置
    if (timeSlotsResponse) {
      timeSlotDict.value = timeSlotsResponse;
      console.log('时段配置加载成功:', timeSlotsResponse.length);
    }
    
    // 加载患者列表
    await loadPatientList();
    
    ElMessage.success('基础数据加载完成');
  } catch (error) {
    console.error('加载基础数据失败:', error);
    ElMessage.error('加载基础数据失败，部分功能可能不可用');
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

/* ==================== 操作医嘱表单样式 ==================== */
.operation-form .section-header {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 15px;
  font-size: 1rem;
  font-weight: 600;
  color: var(--text-primary);
}

.operation-form .form-row {
  margin-bottom: 15px;
}

.operation-form .tip-text {
  display: inline-block;
  margin-left: 10px;
  color: var(--text-secondary);
  font-size: 0.85rem;
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