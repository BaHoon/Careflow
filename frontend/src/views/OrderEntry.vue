<template>
  <div class="order-container">
    <main class="order-layout">
      <header class="patient-context" v-if="selectedPatient">
        <div class="patient-badge">{{ selectedPatient.bedId }}</div>
        <div class="patient-info">
          <span class="name">{{ selectedPatient.name }}</span>
          <span class="detail">{{ selectedPatient.gender }} | {{ selectedPatient.age }}岁 | {{ selectedPatient.weight }}kg</span>
          <span class="tag">{{ getGradeText(selectedPatient.nursingGrade) }}</span>
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
                      :disabled-date="currentOrder.isLongTerm ? disableEndDateBeforeStart : disablePastDates"
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
                      placeholder="搜索药品名称/简拼"
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
                    >
                      <template #append v-if="item.drugId">
                        {{ getDrugUnit(item.drugId) }}
                      </template>
                    </el-input>
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
                      <el-option label="口服 (PO)" :value="1" />
                      <el-option label="外用/涂抹 (Topical)" :value="2" />
                      <el-option label="肌内注射 (IM)" :value="10" />
                      <el-option label="皮下注射 (SC)" :value="11" />
                      <el-option label="静脉推注 (IV Push)" :value="12" />
                      <el-option label="静脉滴注 (IV Drip)" :value="20" />
                      <el-option label="皮试 (Skin Test)" :value="30" />
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

<!-- 检查医嘱表单 - 完善版 -->
                    <div v-else-if="activeType === 'InspectionOrder'" class="inspection-form">
                      <!-- 检查类别选择 -->
                      <div class="form-section">
                        <div class="section-header">
                          <i class="el-icon-folder-opened"></i>
                          <span>检查类别</span>
                        </div>
                        
                        <div class="form-row">
                          <label class="required">检查大类</label>
                          <el-radio-group v-model="inspectionOrder.category" @change="handleCategoryChange">
                            <el-radio-button label="LAB">化验检查</el-radio-button>
                            <el-radio-button label="IMAGING">影像检查</el-radio-button>
                            <el-radio-button label="FUNCTION">功能检查</el-radio-button>
                            <el-radio-button label="ENDOSCOPY">内窥镜检查</el-radio-button>
                            <el-radio-button label="PATHOLOGY">病理检查</el-radio-button>
                          </el-radio-group>
                        </div>
                      </div>

                      <!-- 检查项目选择 -->
                      <div class="form-section" v-if="inspectionOrder.category">
                        <div class="section-header">
                          <i class="el-icon-document-checked"></i>
                          <span>检查项目</span>
                          <span class="section-tip">（可多选，每项单独开具一条医嘱）</span>
                        </div>

                        <!-- 化验检查项目 -->
                        <div v-if="inspectionOrder.category === 'LAB'">
                          <div class="inspection-group">
                            <div class="group-title">血液检验</div>
                            <el-checkbox-group v-model="inspectionOrder.selectedItems">
                              <el-checkbox label="LAB_BLOOD_ROUTINE">血常规</el-checkbox>
                              <el-checkbox label="LAB_BLOOD_BIOCHEM">生化全套</el-checkbox>
                              <el-checkbox label="LAB_BLOOD_GLUCOSE">血糖</el-checkbox>
                              <el-checkbox label="LAB_BLOOD_LIPID">血脂四项</el-checkbox>
                              <el-checkbox label="LAB_LIVER_FUNCTION">肝功能</el-checkbox>
                              <el-checkbox label="LAB_KIDNEY_FUNCTION">肾功能</el-checkbox>
                              <el-checkbox label="LAB_ELECTROLYTE">电解质</el-checkbox>
                              <el-checkbox label="LAB_COAGULATION">凝血功能</el-checkbox>
                              <el-checkbox label="LAB_BLOOD_GAS">血气分析</el-checkbox>
                              <el-checkbox label="LAB_THYROID">甲状腺功能</el-checkbox>
                              <el-checkbox label="LAB_CARDIAC_MARKER">心肌标志物</el-checkbox>
                              <el-checkbox label="LAB_TUMOR_MARKER">肿瘤标志物</el-checkbox>
                              <el-checkbox label="LAB_BLOOD_OTHER">其他</el-checkbox>
                            </el-checkbox-group>
                            <el-input
                              v-if="inspectionOrder.selectedItems.includes('LAB_BLOOD_OTHER')"
                              v-model="inspectionOrder.customItems.LAB_BLOOD_OTHER"
                              placeholder="请输入其他血液检验项目"
                              style="margin-top: 10px;"
                            />
                          </div>

                          <div class="inspection-group">
                            <div class="group-title">体液检验</div>
                            <el-checkbox-group v-model="inspectionOrder.selectedItems">
                              <el-checkbox label="LAB_URINE_ROUTINE">尿常规</el-checkbox>
                              <el-checkbox label="LAB_STOOL_ROUTINE">大便常规</el-checkbox>
                              <el-checkbox label="LAB_STOOL_OB">大便隐血</el-checkbox>
                              <el-checkbox label="LAB_SPUTUM">痰培养+药敏</el-checkbox>
                              <el-checkbox label="LAB_FLUID_OTHER">其他</el-checkbox>
                            </el-checkbox-group>
                            <el-input
                              v-if="inspectionOrder.selectedItems.includes('LAB_FLUID_OTHER')"
                              v-model="inspectionOrder.customItems.LAB_FLUID_OTHER"
                              placeholder="请输入其他体液检验项目"
                              style="margin-top: 10px;"
                            />
                          </div>

                          <div class="inspection-group">
                            <div class="group-title">免疫检验</div>
                            <el-checkbox-group v-model="inspectionOrder.selectedItems">
                              <el-checkbox label="LAB_HBV">乙肝五项</el-checkbox>
                              <el-checkbox label="LAB_HIV">HIV抗体</el-checkbox>
                              <el-checkbox label="LAB_SYPHILIS">梅毒抗体</el-checkbox>
                              <el-checkbox label="LAB_HCV">丙肝抗体</el-checkbox>
                              <el-checkbox label="LAB_CRP">C反应蛋白</el-checkbox>
                              <el-checkbox label="LAB_RF">类风湿因子</el-checkbox>
                              <el-checkbox label="LAB_IMMUNE_OTHER">其他</el-checkbox>
                            </el-checkbox-group>
                            <el-input
                              v-if="inspectionOrder.selectedItems.includes('LAB_IMMUNE_OTHER')"
                              v-model="inspectionOrder.customItems.LAB_IMMUNE_OTHER"
                              placeholder="请输入其他免疫检验项目"
                              style="margin-top: 10px;"
                            />
                          </div>
                        </div>

                        <!-- 影像检查项目 -->
                        <div v-if="inspectionOrder.category === 'IMAGING'">
                          <div class="inspection-group">
                            <div class="group-title">X线检查</div>
                            <el-checkbox-group v-model="inspectionOrder.selectedItems">
                              <el-checkbox label="XRAY_CHEST">胸部X线</el-checkbox>
                              <el-checkbox label="XRAY_ABDOMEN">腹部X线</el-checkbox>
                              <el-checkbox label="XRAY_SPINE">脊柱X线</el-checkbox>
                              <el-checkbox label="XRAY_LIMB">四肢X线</el-checkbox>
                              <el-checkbox label="XRAY_OTHER">其他</el-checkbox>
                            </el-checkbox-group>
                            <el-input
                              v-if="inspectionOrder.selectedItems.includes('XRAY_OTHER')"
                              v-model="inspectionOrder.customItems.XRAY_OTHER"
                              placeholder="请输入其他X线检查项目"
                              style="margin-top: 10px;"
                            />
                          </div>

                          <div class="inspection-group">
                            <div class="group-title">CT检查</div>
                            <el-checkbox-group v-model="inspectionOrder.selectedItems">
                              <el-checkbox label="CT_HEAD">头颅CT</el-checkbox>
                              <el-checkbox label="CT_CHEST">胸部CT</el-checkbox>
                              <el-checkbox label="CT_ABDOMEN">腹部CT</el-checkbox>
                              <el-checkbox label="CT_PELVIS">盆腔CT</el-checkbox>
                              <el-checkbox label="CT_SPINE">脊柱CT</el-checkbox>
                              <el-checkbox label="CT_CTA">CT血管造影</el-checkbox>
                              <el-checkbox label="CT_OTHER">其他</el-checkbox>
                            </el-checkbox-group>
                            <el-input
                              v-if="inspectionOrder.selectedItems.includes('CT_OTHER')"
                              v-model="inspectionOrder.customItems.CT_OTHER"
                              placeholder="请输入其他CT检查项目"
                              style="margin-top: 10px;"
                            />
                          </div>

                          <div class="inspection-group">
                            <div class="group-title">MRI检查</div>
                            <el-checkbox-group v-model="inspectionOrder.selectedItems">
                              <el-checkbox label="MRI_HEAD">头颅MRI</el-checkbox>
                              <el-checkbox label="MRI_SPINE">脊柱MRI</el-checkbox>
                              <el-checkbox label="MRI_JOINT">关节MRI</el-checkbox>
                              <el-checkbox label="MRI_ABDOMEN">腹部MRI</el-checkbox>
                              <el-checkbox label="MRI_MRA">磁共振血管造影</el-checkbox>
                              <el-checkbox label="MRI_OTHER">其他</el-checkbox>
                            </el-checkbox-group>
                            <el-input
                              v-if="inspectionOrder.selectedItems.includes('MRI_OTHER')"
                              v-model="inspectionOrder.customItems.MRI_OTHER"
                              placeholder="请输入其他MRI检查项目"
                              style="margin-top: 10px;"
                            />
                          </div>

                          <div class="inspection-group">
                            <div class="group-title">超声检查</div>
                            <el-checkbox-group v-model="inspectionOrder.selectedItems">
                              <el-checkbox label="US_ABDOMEN">腹部超声</el-checkbox>
                              <el-checkbox label="US_CARDIAC">心脏超声</el-checkbox>
                              <el-checkbox label="US_THYROID">甲状腺超声</el-checkbox>
                              <el-checkbox label="US_BREAST">乳腺超声</el-checkbox>
                              <el-checkbox label="US_VASCULAR">血管超声</el-checkbox>
                              <el-checkbox label="US_OBSTETRIC">产科超声</el-checkbox>
                              <el-checkbox label="US_OTHER">其他</el-checkbox>
                            </el-checkbox-group>
                            <el-input
                              v-if="inspectionOrder.selectedItems.includes('US_OTHER')"
                              v-model="inspectionOrder.customItems.US_OTHER"
                              placeholder="请输入其他超声检查项目"
                              style="margin-top: 10px;"
                            />
                          </div>

                          <div class="form-row" style="margin-top: 15px;">
                            <label>检查部位</label>
                            <el-input
                              v-model="inspectionOrder.location"
                              placeholder="请输入具体检查部位，如：头部、胸部、腹部、左膝关节等"
                            />
                          </div>

                          <div class="form-row">
                            <label>对比剂使用</label>
                            <el-radio-group v-model="inspectionOrder.contrastAgent">
                              <el-radio label="NONE">不使用</el-radio>
                              <el-radio label="PLAIN">平扫</el-radio>
                              <el-radio label="ENHANCED">增强扫描</el-radio>
                              <el-radio label="PLAIN_ENHANCED">平扫+增强</el-radio>
                            </el-radio-group>
                          </div>
                        </div>

                        <!-- 功能检查项目 -->
                        <div v-if="inspectionOrder.category === 'FUNCTION'">
                          <div class="inspection-group">
                            <el-checkbox-group v-model="inspectionOrder.selectedItems">
                              <el-checkbox label="ECG">常规心电图</el-checkbox>
                              <el-checkbox label="ECG_24H">24小时动态心电图</el-checkbox>
                              <el-checkbox label="EXERCISE_ECG">运动心电图</el-checkbox>
                              <el-checkbox label="EEG">脑电图</el-checkbox>
                              <el-checkbox label="EMG">肌电图</el-checkbox>
                              <el-checkbox label="PFT">肺功能检查</el-checkbox>
                              <el-checkbox label="ABPM">24小时动态血压</el-checkbox>
                              <el-checkbox label="TCD">经颅多普勒</el-checkbox>
                              <el-checkbox label="SLEEP_MONITOR">睡眠监测</el-checkbox>
                              <el-checkbox label="FUNCTION_OTHER">其他</el-checkbox>
                            </el-checkbox-group>
                            <el-input
                              v-if="inspectionOrder.selectedItems.includes('FUNCTION_OTHER')"
                              v-model="inspectionOrder.customItems.FUNCTION_OTHER"
                              placeholder="请输入其他功能检查项目"
                              style="margin-top: 10px;"
                            />
                          </div>
                        </div>

                        <!-- 内窥镜检查项目 -->
                        <div v-if="inspectionOrder.category === 'ENDOSCOPY'">
                          <div class="inspection-group">
                            <el-checkbox-group v-model="inspectionOrder.selectedItems">
                              <el-checkbox label="ENDO_GASTROSCOPY">胃镜检查</el-checkbox>
                              <el-checkbox label="ENDO_COLONOSCOPY">肠镜检查</el-checkbox>
                              <el-checkbox label="ENDO_BRONCHOSCOPY">支气管镜</el-checkbox>
                              <el-checkbox label="ENDO_LARYNGOSCOPY">喉镜检查</el-checkbox>
                              <el-checkbox label="ENDO_CYSTOSCOPY">膀胱镜检查</el-checkbox>
                              <el-checkbox label="ENDO_HYSTEROSCOPY">宫腔镜检查</el-checkbox>
                              <el-checkbox label="ENDO_ARTHROSCOPY">关节镜检查</el-checkbox>
                              <el-checkbox label="ENDO_OTHER">其他</el-checkbox>
                            </el-checkbox-group>
                            <el-input
                              v-if="inspectionOrder.selectedItems.includes('ENDO_OTHER')"
                              v-model="inspectionOrder.customItems.ENDO_OTHER"
                              placeholder="请输入其他内窥镜检查项目"
                              style="margin-top: 10px;"
                            />
                          </div>

                          <div class="form-row" style="margin-top: 15px;">
                            <label>麻醉方式</label>
                            <el-radio-group v-model="inspectionOrder.anesthesiaType">
                              <el-radio label="NONE">不麻醉</el-radio>
                              <el-radio label="LOCAL">局部麻醉</el-radio>
                              <el-radio label="CONSCIOUS_SEDATION">清醒镇静</el-radio>
                              <el-radio label="GENERAL">全身麻醉</el-radio>
                            </el-radio-group>
                          </div>
                        </div>

                        <!-- 病理检查项目 -->
                        <div v-if="inspectionOrder.category === 'PATHOLOGY'">
                          <div class="inspection-group">
                            <el-checkbox-group v-model="inspectionOrder.selectedItems">
                              <el-checkbox label="PATH_BIOPSY">组织活检</el-checkbox>
                              <el-checkbox label="PATH_CYTOLOGY">细胞学检查</el-checkbox>
                              <el-checkbox label="PATH_FROZEN">冰冻切片</el-checkbox>
                              <el-checkbox label="PATH_IMMUNOHISTO">免疫组化</el-checkbox>
                              <el-checkbox label="PATH_MOLECULAR">分子病理</el-checkbox>
                              <el-checkbox label="PATH_OTHER">其他</el-checkbox>
                            </el-checkbox-group>
                            <el-input
                              v-if="inspectionOrder.selectedItems.includes('PATH_OTHER')"
                              v-model="inspectionOrder.customItems.PATH_OTHER"
                              placeholder="请输入其他病理检查项目"
                              style="margin-top: 10px;"
                            />
                          </div>

                          <div class="form-row" style="margin-top: 15px;">
                            <label>标本来源</label>
                            <el-input
                              v-model="inspectionOrder.specimenSource"
                              placeholder="请输入标本来源部位"
                            />
                          </div>
                        </div>
                      </div>

                      <!-- 附加信息 -->
                      <div class="form-section" v-if="inspectionOrder.selectedItems.length > 0">
                        <div class="section-header">
                          <i class="el-icon-warning-outline"></i>
                          <span>附加信息</span>
                        </div>

                        <div class="form-row">
                          <label class="required">临床诊断</label>
                          <el-input
                            v-model="inspectionOrder.clinicalDiagnosis"
                            placeholder="请输入临床诊断，如：高血压、糖尿病等"
                            maxlength="200"
                            show-word-limit
                          />
                        </div>

                        <div class="form-row">
                          <label>检查目的</label>
                          <el-radio-group v-model="inspectionOrder.purpose">
                            <el-radio label="SCREENING">筛查</el-radio>
                            <el-radio label="DIAGNOSIS">诊断</el-radio>
                            <el-radio label="RECHECK">复查</el-radio>
                            <el-radio label="PREOP">术前检查</el-radio>
                          </el-radio-group>
                        </div>

                        <div class="form-row">
                          <label>备注</label>
                          <el-input
                            v-model="inspectionOrder.remarks"
                            type="textarea"
                            :rows="2"
                            placeholder="其他需要说明的事项（检查注意事项将在预约确认后返回）"
                            maxlength="500"
                            show-word-limit
                          />
                        </div>
                      </div>
                    </div>

            <!-- 手术医嘱表单 -->
            <div v-else-if="activeType === 'SurgicalOrder'" class="surgical-form">
              <!-- 手术基本信息 -->
              <div class="form-section">
                <div class="section-header">
                  <i class="el-icon-scissors"></i>
                  <span>手术基本信息</span>
                </div>
                
                <div class="form-row">
                  <label class="required">手术名称：</label>
                  <el-input 
                    v-model="surgicalOrder.surgeryName"
                    placeholder="请输入手术名称，如：阑尾切除术、胆囊切除术"
                    clearable
                  />
                </div>

                <div class="form-row">
                  <label class="required">麻醉方式：</label>
                  <el-select 
                    v-model="surgicalOrder.anesthesiaType"
                    placeholder="请选择麻醉方式"
                    style="width: 100%"
                  >
                    <el-option
                      v-for="item in anesthesiaOptions"
                      :key="item.value"
                      :label="item.label"
                      :value="item.value"
                    />
                  </el-select>
                </div>

                <div class="form-row">
                  <label class="required">切口部位：</label>
                  <el-input 
                    v-model="surgicalOrder.incisionSite"
                    placeholder="请输入切口部位，如：右下腹、脐部"
                    clearable
                  />
                </div>
              </div>

              <!-- 医生信息 -->
              <div class="form-section">
                <div class="section-header">
                  <i class="el-icon-user"></i>
                  <span>医生信息</span>
                </div>
                
                <div class="form-row">
                  <label class="required">主刀医生：</label>
                  <el-select 
                    v-model="surgicalOrder.surgeonId"
                    placeholder="请选择主刀医生"
                    filterable
                    style="width: 100%"
                  >
                    <el-option
                      v-for="doctor in doctorList"
                      :key="doctor.staffId"
                      :label="`${doctor.name} (${doctor.title || '医师'})`"
                      :value="doctor.staffId"
                    />
                  </el-select>
                </div>
              </div>

              <!-- 术前准备 -->
              <div class="form-section">
                <div class="section-header">
                  <i class="el-icon-document-checked"></i>
                  <span>术前准备</span>
                </div>
                
                <div class="form-row">
                  <label>术前宣讲：</label>
                  <div class="custom-multi-select">
                    <el-checkbox-group v-model="surgicalOrder.requiredTalk" class="checkbox-grid">
                      <el-checkbox
                        v-for="item in talkOptions"
                        :key="item.value"
                        :label="item.value"
                      >
                        {{ item.label }}
                      </el-checkbox>
                    </el-checkbox-group>
                    <div class="custom-input-row">
                      <el-input
                        v-model="customTalkInput"
                        placeholder="输入其他术前宣讲事项，按回车添加"
                        @keyup.enter="addCustomTalk"
                        clearable
                        style="flex: 1"
                      >
                        <template #append>
                          <el-button @click="addCustomTalk" :disabled="!customTalkInput.trim()">添加</el-button>
                        </template>
                      </el-input>
                    </div>
                    <div v-if="customTalkItems.length" class="custom-tags">
                      <el-tag
                        v-for="item in customTalkItems"
                        :key="item"
                        closable
                        @close="removeCustomTalk(item)"
                        type="info"
                      >
                        {{ item }}
                      </el-tag>
                    </div>
                  </div>
                </div>

                <div class="form-row">
                  <label>术前操作：</label>
                  <div class="custom-multi-select">
                    <el-checkbox-group v-model="surgicalOrder.requiredOperation" class="checkbox-grid">
                      <el-checkbox
                        v-for="item in preoperativeOperationOptions"
                        :key="item.value"
                        :label="item.value"
                      >
                        {{ item.label }}
                      </el-checkbox>
                    </el-checkbox-group>
                    <div class="custom-input-row">
                      <el-input
                        v-model="customOperationInput"
                        placeholder="输入其他术前操作，按回车添加"
                        @keyup.enter="addCustomOperation"
                        clearable
                        style="flex: 1"
                      >
                        <template #append>
                          <el-button @click="addCustomOperation" :disabled="!customOperationInput.trim()">添加</el-button>
                        </template>
                      </el-input>
                    </div>
                    <div v-if="customOperationItems.length" class="custom-tags">
                      <el-tag
                        v-for="item in customOperationItems"
                        :key="item"
                        closable
                        @close="removeCustomOperation(item)"
                        type="info"
                      >
                        {{ item }}
                      </el-tag>
                    </div>
                  </div>
                </div>
              </div>

              <!-- 手术药品 -->
              <div class="form-section">
                <div class="section-header">
                  <i class="el-icon-medicine-box"></i>
                  <span>手术药品</span>
                  <span style="color: red; margin-left: 4px;">*</span>
                </div>
                <div class="drug-group-box">
                  <div class="drug-group-header">
                    <span>手术药品配置</span>
                    <button @click="addSurgicalItem" class="btn-icon-text">
                      + 添加药品
                    </button>
                  </div>
                  <div v-for="(item, index) in surgicalOrder.items" :key="index" class="drug-item-row">
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
                    >
                      <template #append v-if="item.drugId">
                        {{ getDrugUnit(item.drugId) }}
                      </template>
                    </el-input>
                    <el-input 
                      v-model="item.note" 
                      placeholder="备注 (可选)" 
                      class="note-input"
                      style="width: 140px"
                    />
                    <button 
                      @click="removeSurgicalItem(index)" 
                      class="btn-icon-danger"
                      :disabled="surgicalOrder.items.length === 1"
                    >
                      ×
                    </button>
                  </div>
                </div>
              </div>

              <!-- 手术安排 -->
              <div class="form-section">
                <div class="section-header">
                  <i class="el-icon-time"></i>
                  <span>手术安排</span>
                </div>
                
                <div class="form-row">
                  <label class="required">手术时间：</label>
                  <el-date-picker
                    v-model="surgicalOrder.scheduleTime"
                    type="datetime"
                    placeholder="选择手术日期和时间"
                    format="YYYY-MM-DD HH:mm"
                    value-format="YYYY-MM-DDTHH:mm:ss"
                    :disabled-date="disablePastDates"
                  />
                </div>
              </div>

              <!-- 备注信息 -->
              <div class="form-section">
                <div class="form-row">
                  <label>备注：</label>
                  <el-input 
                    v-model="surgicalOrder.remarks"
                    type="textarea"
                    :rows="3"
                    placeholder="填写术前准备、注意事项等"
                    maxlength="300"
                    show-word-limit
                  />
                </div>
              </div>
            </div>

            <!-- 操作医嘱表单 -->
            <div v-else-if="activeType === 'OperationOrder'" class="operation-form">
              <!-- 步骤1：医嘱基本信息 -->
              <div class="form-section">
                <div class="section-header">
                  <i class="el-icon-document-checked"></i>
                  <span>医嘱基本信息</span>
                </div>
                <div class="form-row">
                  <label class="required">医嘱类型：</label>
                  <el-radio-group v-model="operationOrder.isLongTerm" @change="onOperationOrderTypeChange">
                    <el-radio-button :label="true">
                      <i class="el-icon-time"></i> 长期医嘱
                    </el-radio-button>
                    <el-radio-button :label="false">
                      <i class="el-icon-lightning"></i> 临时医嘱
                    </el-radio-button>
                  </el-radio-group>
                  <span class="tip-text">{{ operationOrder.isLongTerm ? '长期医嘱需配置执行周期' : '临时医嘱为单次执行' }}</span>
                </div>
              </div>

              <!-- 步骤2：操作信息 -->
              <div class="form-section">
                <div class="section-header">
                  <i class="el-icon-setting"></i>
                  <span>操作信息</span>
                </div>
                
                <div class="form-row">
                  <label class="required">操作项目：</label>
                  <el-select 
                    v-model="operationOrder.operationName" 
                    filterable
                    placeholder="搜索操作名称，如：更换引流袋、导尿、血糖监测等"
                    @change="onOperationNameChange"
                    style="width: 100%"
                  >
                    <el-option
                      v-for="op in operationOptions"
                      :key="op.opId"
                      :label="op.name"
                      :value="op.name"
                    >
                      <div>
                        <div style="font-weight: 600;">{{ op.name }}</div>
                        <div style="color: #666; font-size: 12px;">类型：{{ getCategoryLabel(op.category) }}</div>
                      </div>
                    </el-option>
                  </el-select>
                </div>

                <div class="form-row">
                  <label>操作部位：</label>
                  <el-input 
                    v-model="operationOrder.operationSite"
                    placeholder="请输入操作部位，如：左臂、腹部、背部等（可选）"
                    clearable
                  />
                </div>

                <div class="form-row" style="display: none;">
                  <label>正常/异常：</label>
                  <el-radio-group v-model="operationOrder.normal">
                    <el-radio :label="true">正常</el-radio>
                    <el-radio :label="false">异常</el-radio>
                  </el-radio-group>
                </div>
              </div>

              <!-- 步骤3：执行时间策略（参照药品医嘱） -->
              <div class="form-section">
                <div class="section-header">
                  <i class="el-icon-time"></i>
                  <span>执行时间策略</span>
                </div>

                <!-- 临时医嘱策略 -->
                <div class="form-row" v-if="!operationOrder.isLongTerm">
                  <label class="required">执行时间：</label>
                  <el-radio-group v-model="operationOrder.timingStrategy" @change="onOperationStrategyChange">
                    <el-radio label="IMMEDIATE">
                      <i class="el-icon-video-play"></i> 立即执行
                    </el-radio>
                    <el-radio label="SPECIFIC">
                      <i class="el-icon-alarm-clock"></i> 指定时间
                    </el-radio>
                  </el-radio-group>
                </div>

                <!-- 长期医嘱策略 -->
                <div class="form-row" v-if="operationOrder.isLongTerm">
                  <label class="required">执行策略：</label>
                  <el-radio-group v-model="operationOrder.timingStrategy" @change="onOperationStrategyChange">
                    <el-radio label="SLOTS">
                      <i class="el-icon-clock"></i> 按时段执行 (如早餐前、午餐后)
                    </el-radio>
                    <el-radio label="CYCLIC">
                      <i class="el-icon-refresh"></i> 固定间隔执行 (如每6小时一次)
                    </el-radio>
                  </el-radio-group>
                </div>

                <!-- 策略配置区域 -->
                <div class="strategy-config">
                  <!-- IMMEDIATE策略：显示开始执行时间 -->
                  <div class="form-row" v-if="operationOrder.timingStrategy === 'IMMEDIATE'">
                    <label class="required">开始执行时间：</label>
                    <el-date-picker 
                      v-model="operationOrder.startTime"
                      type="datetime"
                      placeholder="立即执行时间"
                      :disabled="true"
                      format="YYYY-MM-DD HH:mm"
                      value-format="YYYY-MM-DDTHH:mm:ss"
                      style="width: 280px"
                    />
                    <span class="tip-text">立即执行，时间不可修改</span>
                  </div>

                  <!-- SPECIFIC策略：日期时间选择器 -->
                  <div class="form-row" v-if="operationOrder.timingStrategy === 'SPECIFIC'">
                    <label class="required">指定执行时间：</label>
                    <el-date-picker 
                      v-model="operationOrder.startTime"
                      type="datetime"
                      placeholder="选择具体日期和时间"
                      :disabled-date="disablePastDates"
                      format="YYYY-MM-DD HH:mm"
                      value-format="YYYY-MM-DDTHH:mm:ss"
                      style="width: 280px"
                    />
                  </div>

                  <!-- CYCLIC策略：开始时间 + 间隔小时 + 间隔天数 -->
                  <div v-if="operationOrder.timingStrategy === 'CYCLIC'">
                    <div class="form-row">
                      <label class="required">首次执行时间：</label>
                      <el-date-picker 
                        v-model="operationOrder.startTime"
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
                        v-model="operationOrder.intervalHours" 
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
                        v-model="operationOrder.intervalDays" 
                        :min="1" 
                        :max="30"
                        placeholder="间隔天数"
                        style="width: 150px"
                      />
                      <span class="tip-text">1=每天执行，2=隔天执行（通常设为1）</span>
                    </div>
                  </div>

                  <!-- SLOTS策略：开始执行时间 + 时段选择 -->
                  <div v-if="operationOrder.timingStrategy === 'SLOTS'">
                    <div class="form-row">
                      <label class="required">开始执行时间：</label>
                      <el-date-picker 
                        v-model="operationOrder.startTime"
                        type="datetime"
                        placeholder="选择开始执行时间"
                        :disabled-date="disablePastDates"
                        format="YYYY-MM-DD HH:mm"
                        value-format="YYYY-MM-DDTHH:mm:ss"
                        style="width: 280px"
                      />
                      <span class="tip-text">从什么时间开始按时段执行</span>
                    </div>
                    <div class="form-row">
                      <label class="required">执行时段：</label>
                      <div class="time-slots-selector" style="margin-top: 10px;">
                        <div class="slot-category">
                          <div class="category-title">🍽️ 三餐前后及睡前</div>
                          <div class="slots-grid">
                            <div v-for="slot in allTimeSlots" :key="slot.id" 
                                 :class="['slot-tag', { selected: isOperationSlotSelected(slot.id) }]"
                                 @click="toggleOperationSlot(slot.id)">
                              <i class="el-icon-check" v-if="isOperationSlotSelected(slot.id)"></i>
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
                        v-model="operationOrder.intervalDays"
                        :min="1"
                        :max="30"
                        placeholder="间隔天数" 
                        style="width: 150px"
                      />
                      <span class="tip-text">1=每天执行，2=隔天执行，依此类推</span>
                    </div>
                    <div class="freq-reminder" v-if="operationOrder.smartSlotsMask > 0">
                      <i class="el-icon-info"></i> 
                      已选择 <strong>{{ getSelectedOperationSlotsCount() }}</strong> 个时段，每 <strong>{{ operationOrder.intervalDays }}</strong> 天执行 <strong>{{ getSelectedOperationSlotsCount() }}</strong> 次
                    </div>
                  </div>

                  <!-- 医嘱结束时间（所有策略都需要，SPECIFIC策略下自动同步，不显示） -->
                  <div class="form-row" v-if="operationOrder.timingStrategy !== 'SPECIFIC'">
                    <label class="required">{{ operationOrder.isLongTerm ? '医嘱结束时间' : '医嘱开始时间' }}：</label>
                    <el-date-picker 
                      v-model="operationOrder.plantEndTime"
                      type="datetime"
                      :placeholder="operationOrder.isLongTerm ? '选择医嘱结束时间' : '选择医嘱开始时间'"
                      :disabled="operationOrder.timingStrategy === 'IMMEDIATE'"
                      :disabled-date="operationOrder.isLongTerm ? disableEndDateBeforeStart : disablePastDates"
                      :disabled-time="operationOrder.isLongTerm ? disableTimesBeforeStart : undefined"
                      format="YYYY-MM-DD HH:mm"
                      value-format="YYYY-MM-DDTHH:mm:ss"
                      style="width: 280px"
                    />
                    <span class="tip-text" v-if="operationOrder.timingStrategy === 'IMMEDIATE'">立即执行，时间不可修改</span>
                    <span class="tip-text" v-else-if="operationOrder.isLongTerm">不能早于开始执行时间</span>
                  </div>
                  <!-- SPECIFIC策略下，结束时间自动与开始时间同步，显示提示 -->
                  <div class="form-row" v-if="operationOrder.timingStrategy === 'SPECIFIC' && operationOrder.startTime">
                    <label>医嘱结束时间：</label>
                    <el-date-picker 
                      v-model="operationOrder.plantEndTime"
                      type="datetime"
                      placeholder="自动与开始时间同步"
                      :disabled="true"
                      format="YYYY-MM-DD HH:mm"
                      value-format="YYYY-MM-DDTHH:mm:ss"
                      style="width: 280px"
                    />
                    <span class="tip-text">指定时间策略下，结束时间自动与开始时间相同（单次执行）</span>
                  </div>
                </div>
              </div>

              <!-- 步骤4：执行要求 -->
              <div class="form-section">
                <div class="section-header">
                  <i class="el-icon-box"></i>
                  <span>执行要求</span>
                </div>
                
                <!-- 准备物品列表（根据操作类型自动显示，可编辑） -->
                <div class="form-row" v-if="operationOrder.requiresPreparation">
                  <label>准备物品：</label>
                  <div class="preparation-items-container">
                    <div class="preparation-input-row">
                      <el-input
                        v-model="preparationItemInput"
                        placeholder="输入准备物品名称，按回车或点击添加"
                        @keyup.enter="addPreparationItem"
                        clearable
                        style="flex: 1"
                      >
                        <template #append>
                          <el-button @click="addPreparationItem" :disabled="!preparationItemInput.trim()">添加</el-button>
                        </template>
                      </el-input>
                    </div>
                    <div v-if="operationOrder.preparationItems.length > 0" class="preparation-tags">
                      <el-tag
                        v-for="(item, index) in operationOrder.preparationItems"
                        :key="index"
                        closable
                        @close="removePreparationItem(index)"
                        type="info"
                        style="margin-right: 8px; margin-bottom: 8px;"
                      >
                        {{ item }}
                      </el-tag>
                    </div>
                    <div v-else class="tip-text" style="margin-top: 8px;">
                      <i class="el-icon-warning"></i> 请至少添加一个准备物品
                    </div>
                  </div>
                </div>
                <div v-else class="form-row">
                  <label>准备物品：</label>
                  <span class="tip-text">该操作无需准备物品</span>
                </div>

                <!-- 预期执行时长（持续类操作或需要持续时间的ResultPending类操作显示） -->
                <div class="form-row" v-if="shouldShowDurationInput()">
                  <label>预期执行时长（分钟）：</label>
                  <el-input-number 
                    v-model="operationOrder.expectedDurationMinutes"
                    :min="1"
                    :max="1440"
                    placeholder="预期执行时长"
                    style="width: 150px"
                  />
                  <span class="tip-text">持续类操作建议设置预期时长，可根据实际情况修改</span>
                </div>
              </div>

              <!-- 步骤5：任务配置（自动判定，仅显示） -->
              <div class="form-section" v-if="operationOrder.requiresResult">
                <div class="section-header">
                  <i class="el-icon-setting"></i>
                  <span>任务配置</span>
                </div>
                
                <div class="form-row">
                  <label>是否需要记录结果：</label>
                  <el-tag type="success">需要</el-tag>
                  <span class="tip-text">该操作需要录入执行结果（如血糖值、血压值等）</span>
                </div>
              </div>

              <!-- 步骤6：医嘱备注 -->
              <div class="form-section">
                <div class="form-row">
                  <label>医嘱备注：</label>
                  <el-input 
                    v-model="operationOrder.remarks"
                    type="textarea"
                    :rows="2"
                    placeholder="可填写特殊嘱托、注意事项等"
                    maxlength="200"
                    show-word-limit
                  />
                </div>
              </div>
            </div>

            <!-- 护理等级修改表单 -->
            <div v-else-if="activeType === 'NursingGrade'" class="nursing-grade-form">
              <div class="form-section">
                <div class="section-header">
                  <i class="el-icon-user"></i>
                  <span>护理等级修改</span>
                </div>
                
                <div class="current-grade-display" v-if="selectedPatient">
                  <div class="info-row">
                    <span class="label">当前护理等级：</span>
                    <el-tag :type="getGradeTagType(selectedPatient.nursingGrade)" size="large">
                      {{ getGradeText(selectedPatient.nursingGrade) }}
                    </el-tag>
                  </div>
                </div>

                <div class="form-row">
                  <label class="required">新护理等级：</label>
                  <el-radio-group v-model="nursingGradeForm.newGrade" size="large">
                    <el-radio-button :label="0">
                      <i class="el-icon-star-on"></i> 特级护理
                    </el-radio-button>
                    <el-radio-button :label="1">
                      <i class="el-icon-medal"></i> 一级护理
                    </el-radio-button>
                    <el-radio-button :label="2">
                      <i class="el-icon-user"></i> 二级护理
                    </el-radio-button>
                    <el-radio-button :label="3">
                      <i class="el-icon-s-custom"></i> 三级护理
                    </el-radio-button>
                  </el-radio-group>
                </div>

                <div class="form-row">
                  <label>修改原因：</label>
                  <el-input
                    v-model="nursingGradeForm.reason"
                    type="textarea"
                    :rows="3"
                    placeholder="请输入护理等级修改原因（可选）"
                    maxlength="200"
                    show-word-limit
                  />
                </div>

                <div class="form-actions" style="margin-top: 30px;">
                  <el-button @click="resetNursingGradeForm" size="large">
                    <i class="el-icon-refresh-left"></i> 重置
                  </el-button>
                  <el-button 
                    type="primary" 
                    @click="submitNursingGrade" 
                    size="large"
                    :disabled="!isNursingGradeFormValid"
                  >
                    <i class="el-icon-check"></i> 确认修改
                  </el-button>
                </div>
              </div>
            </div>

            <!-- 出院医嘱表单 -->
            <div v-else-if="activeType === 'DischargeOrder'" class="discharge-form">
              <div class="form-section">
                <div class="section-header">
                  <i class="el-icon-success"></i>
                  <span>出院基本信息</span>
                </div>

                <div class="form-row">
                  <label class="required">出院类型：</label>
                  <el-radio-group v-model="dischargeOrder.dischargeType" size="large">
                    <el-radio-button :label="1">
                      <i class="el-icon-circle-check"></i> 治愈出院
                    </el-radio-button>
                    <el-radio-button :label="2">
                      <i class="el-icon-success"></i> 好转出院
                    </el-radio-button>
                    <el-radio-button :label="99">
                      <i class="el-icon-more"></i> 其他
                    </el-radio-button>
                  </el-radio-group>
                </div>

                <div class="form-row">
                  <label class="required">出院时间：</label>
                  <el-date-picker 
                    v-model="dischargeOrder.dischargeTime"
                    type="datetime"
                    placeholder="选择出院时间"
                    :disabled-date="disablePastDates"
                    format="YYYY-MM-DD HH:mm"
                    value-format="YYYY-MM-DDTHH:mm:ss"
                    style="width: 280px"
                  />
                  <span class="tip-text">建议提前设置，以便安排后续工作</span>
                </div>

                <div class="form-row">
                  <label class="required">出院诊断：</label>
                  <el-input
                    v-model="dischargeOrder.dischargeDiagnosis"
                    type="textarea"
                    :rows="2"
                    placeholder="请输入出院诊断信息"
                    maxlength="500"
                    show-word-limit
                  />
                </div>

                <div class="form-row">
                  <label>出院医嘱：</label>
                  <el-input
                    v-model="dischargeOrder.dischargeInstructions"
                    type="textarea"
                    :rows="3"
                    placeholder="请输入出院医嘱（如：注意休息、定期复查等）"
                    maxlength="1000"
                    show-word-limit
                  />
                </div>
              </div>

              <!-- 出院带回药品 -->
              <div class="form-section">
                <div class="section-header">
                  <i class="el-icon-medicine-box"></i>
                  <span>出院带回药品（可选）</span>
                </div>
                <div class="drug-group-box">
                  <div class="drug-group-header">
                    <span>患者出院携带的药品</span>
                    <button @click="addDischargeDrug" class="btn-icon-text">
                      + 添加药品
                    </button>
                  </div>
                  <div v-if="dischargeOrder.items.length === 0" class="empty-hint">
                    <i class="el-icon-info"></i> 无需带回药品可跳过此部分
                  </div>
                  <div v-for="(item, index) in dischargeOrder.items" :key="index" class="drug-item-row">
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
                    >
                      <template #append v-if="item.drugId">
                        {{ getDrugUnit(item.drugId) }}
                      </template>
                    </el-input>
                    <el-input 
                      v-model="item.note" 
                      placeholder="备注 (可选)" 
                      class="note-input"
                      style="width: 140px"
                    />
                    <button 
                      @click="removeDischargeDrug(index)" 
                      class="btn-icon-danger"
                    >
                      ×
                    </button>
                  </div>
                </div>
              </div>

              <!-- 用药指导 -->
              <div class="form-section">
                <div class="section-header">
                  <i class="el-icon-document"></i>
                  <span>用药指导</span>
                </div>
                <div class="form-row">
                  <label>用药说明：</label>
                  <el-input
                    v-model="dischargeOrder.medicationInstructions"
                    type="textarea"
                    :rows="3"
                    placeholder="请输入出院后用药指导（如：服药方法、注意事项等）"
                    maxlength="1000"
                    show-word-limit
                  />
                </div>
              </div>

              <!-- 随访安排 -->
              <div class="form-section">
                <div class="section-header">
                  <i class="el-icon-date"></i>
                  <span>随访安排</span>
                </div>
                <div class="form-row">
                  <label>是否需要随访：</label>
                  <el-switch 
                    v-model="dischargeOrder.requiresFollowUp"
                    active-text="需要"
                    inactive-text="不需要"
                  />
                </div>
                <div class="form-row" v-if="dischargeOrder.requiresFollowUp">
                  <label>随访日期：</label>
                  <el-date-picker 
                    v-model="dischargeOrder.followUpDate"
                    type="date"
                    placeholder="选择随访日期"
                    :disabled-date="disableFollowUpPastDates"
                    format="YYYY-MM-DD"
                    value-format="YYYY-MM-DDTHH:mm:ss"
                    style="width: 280px"
                  />
                </div>
              </div>
            </div>

            <!-- 其他未知类型的占位符 -->
            <div v-else class="placeholder-form">
              正在开发 {{ activeType }} 的详细表单...
            </div>

            <div class="form-actions" v-if="activeType !== 'NursingGrade'">
              <button @click="clearForm" class="btn-default">
                <i class="el-icon-refresh-left"></i> 清空表单
              </button>
              <button @click="addToCart" class="btn-primary" :disabled="!isFormValid || !canAddToCart" :title="!canAddToCart ? '患者处于待出院状态，不允许暂存医嘱' : ''">
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
                  <el-tag :type="getOrderTagType(o)" size="small">
                    {{ getOrderTypeLabel(o) }}
                  </el-tag>
                  <span class="order-title">{{ getOrderSummary(o) }}</span>
                  <button @click="toggleOrderDetail(idx)" class="btn-detail">
                    {{ expandedOrders.includes(idx) ? '▲' : '▼' }}
                  </button>
                  <button @click="removeFromCart(idx)" class="btn-mini-danger">
                    ×
                  </button>
                </div>
                
                <!-- 基本信息（始终显示） - 仅药品医嘱显示 -->
                <div v-if="o.orderType === 'MedicationOrder'" class="order-basic-info">
                  <span class="info-item">{{ getRouteName(o.usageRoute) }}</span>
                </div>
                <!-- 检查医嘱基本信息 -->
                <div class="order-basic-info" v-else-if="o.orderType === 'InspectionOrder'">
                  <!-- 检查医嘱无需显示用药途径 -->
                </div>
                
                <!-- 手术医嘱基本信息 -->
                <div v-else-if="o.orderType === 'SurgicalOrder'" class="order-basic-info">
                  <span class="info-item">🕐 {{ formatDateTime(o.scheduleTime) }}</span>
                  <span class="info-item">💉 {{ o.anesthesiaType }}</span>
                </div>
                
                <!-- 操作医嘱基本信息 -->
                <div v-else-if="o.orderType === 'OperationOrder'" class="order-basic-info">
                  <span class="info-item">{{ getStrategyLabel(o.timingStrategy) }}</span>
                  <span v-if="o.operationSite" class="info-item">📍 {{ o.operationSite }}</span>
                </div>

                <!-- 出院医嘱基本信息 -->
                <div v-else-if="o.orderType === 'DischargeOrder'" class="order-basic-info">
                  <span class="info-item">🕐 {{ formatDateTime(o.dischargeTime) }}</span>
                  <span class="info-item" v-if="o.items && o.items.length > 0">💊 携带{{ o.items.length }}种药品</span>
                </div>

                <div class="order-basic-info" v-else>
                  <span class="info-item">{{ getRouteName(o.usageRoute) }}</span>
                </div>

                <!-- 详细信息（可展开） -->
                <div v-show="expandedOrders.includes(idx)" class="order-detail-expand">
                  <!-- 操作医嘱详细信息 -->
                  <template v-if="o.orderType === 'OperationOrder'">
                    <div class="detail-section" v-if="o.operationSite">
                      <div class="detail-label">操作部位：</div>
                      <div class="detail-value">{{ o.operationSite }}</div>
                    </div>
                    <div class="detail-section">
                      <div class="detail-label">时间策略：</div>
                      <div class="detail-value">{{ getStrategyLabel(o.timingStrategy) }}</div>
                    </div>
                    <div class="detail-section" v-if="o.startTime">
                      <div class="detail-label">开始时间：</div>
                      <div class="detail-value">{{ formatDateTime(o.startTime) }}</div>
                    </div>
                    <div class="detail-section" v-if="o.plantEndTime">
                      <div class="detail-label">结束时间：</div>
                      <div class="detail-value">{{ formatDateTime(o.plantEndTime) }}</div>
                    </div>
                    <div class="detail-section" v-if="o.timingStrategy === 'CYCLIC' && o.intervalHours">
                      <div class="detail-label">间隔：</div>
                      <div class="detail-value">每 {{ o.intervalHours }} 小时</div>
                    </div>
                    <div class="detail-section" v-if="o.timingStrategy === 'SLOTS' && o.smartSlotsMask > 0">
                      <div class="detail-label">执行时段：</div>
                      <div class="detail-value">{{ getSelectedOperationSlotsText(o.smartSlotsMask) }}</div>
                    </div>
                    <div class="detail-section" v-if="o.requiresPreparation && o.preparationItems && o.preparationItems.length > 0">
                      <div class="detail-label">准备物品：</div>
                      <div class="detail-value">{{ o.preparationItems.join('、') }}</div>
                    </div>
                    <div class="detail-section" v-if="o.expectedDurationMinutes">
                      <div class="detail-label">预期时长：</div>
                      <div class="detail-value">{{ o.expectedDurationMinutes }} 分钟</div>
                    </div>
                    <div class="detail-section" v-if="o.remarks">
                      <div class="detail-label">备注：</div>
                      <div class="detail-value">{{ o.remarks }}</div>
                    </div>
                  </template>
                  
                  <!-- 检查医嘱详细信息 -->
                  <template v-else-if="o.orderType === 'InspectionOrder'">
                    <div class="detail-section">
                      <div class="detail-label">检查项目：</div>
                      <div class="detail-value">{{ o.itemName || o.itemCode }}</div>
                    </div>
                    
                    <div class="detail-section" v-if="o.remarks">
                      <div class="detail-label">备注：</div>
                      <div class="detail-value">{{ o.remarks }}</div>
                    </div>
                  </template>
                  
                  <!-- 药品医嘱详细信息 -->
                  <template v-else-if="o.orderType === 'MedicationOrder'">
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
                  </template>
                  <!-- 手术医嘱详细信息 --> 
                  <template v-else-if="o.orderType === 'SurgicalOrder'">
                    <div class="detail-section">
                      <div class="detail-label">切口部位：</div>
                      <div class="detail-value">{{ o.incisionSite }}</div>
                    </div>
                    <div class="detail-section">
                      <div class="detail-label">主刀医生：</div>
                      <div class="detail-value">{{ o.surgeonId }}</div>
                    </div>
                    <div v-if="o.remarks" class="detail-section">
                      <div class="detail-label">备注：</div>
                      <div class="detail-value">{{ o.remarks }}</div>
                    </div>
                  </template> 

                  <!-- 出院医嘱详细信息 -->
                  <template v-else-if="o.orderType === 'DischargeOrder'">
                    <div class="detail-section">
                      <div class="detail-label">出院诊断：</div>
                      <div class="detail-value">{{ o.dischargeDiagnosis }}</div>
                    </div>
                    <div class="detail-section" v-if="o.dischargeInstructions">
                      <div class="detail-label">出院医嘱：</div>
                      <div class="detail-value">{{ o.dischargeInstructions }}</div>
                    </div>
                    <div class="detail-section" v-if="o.items && o.items.length > 0">
                      <div class="detail-label">携带药品：</div>
                      <div v-for="(item, i) in o.items" :key="i" class="detail-value">
                        {{ i + 1 }}. {{ getDrugName(item.drugId) }} {{ item.dosage }}
                        <span v-if="item.note" class="note-text">({{ item.note }})</span>
                      </div>
                    </div>
                    <div class="detail-section" v-if="o.requiresFollowUp">
                      <div class="detail-label">随访安排：</div>
                      <div class="detail-value">{{ formatDateTime(o.followUpDate) }}</div>
                    </div>
                  </template>

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
import { ElMessage, ElMessageBox, ElLoading } from 'element-plus';
import { getPatientList } from '../api/patient';
import { getDrugList } from '../api/drug';
import { getTimeSlots } from '../api/hospitalConfig';
import { batchCreateMedicationOrders } from '../api/medicationOrder';
import { batchCreateInspectionOrders } from '../api/inspectionOrder';
import { batchCreateSurgicalOrders } from '../api/surgicalOrder';
import { batchCreateOperationOrders } from '../api/operationOrder';
import { batchCreateDischargeOrders, validateDischargeOrderCreation } from '../api/dischargeOrder';
import { queryStaffList } from '../api/admin';
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
  { label: '护理操作', val: 'OperationOrder' },
  { label: '护理等级', val: 'NursingGrade' },
  { label: '出院医嘱', val: 'DischargeOrder' }
];

// 检查医嘱的响应式数据 - 完善版
const inspectionOrder = reactive({
  category: '',              // 检查大类：LAB/IMAGING/FUNCTION/ENDOSCOPY/PATHOLOGY
  selectedItems: [],         // 已选择的检查项目列表（多选）
  customItems: {},           // 自定义"其他"项目的内容
  
  // 影像检查相关
  location: '',              // 检查部位
  contrastAgent: 'NONE',     // 对比剂使用
  
  // 内窥镜检查相关
  anesthesiaType: 'NONE',    // 麻醉方式
  
  // 病理检查相关
  specimenSource: '',        // 标本来源
  
  // 通用字段
  clinicalDiagnosis: '',     // 临床诊断（必填）
  purpose: 'DIAGNOSIS',      // 检查目的
  remarks: ''                // 备注（注意事项将在预约确认后返回）
});

// 手术医嘱的响应式数据
const surgicalOrder = reactive({
  surgeryName: '',           // 手术名称
  anesthesiaType: '',        // 麻醉方式
  incisionSite: '',          // 切口部位
  surgeonId: '',             // 主刀医生ID
  scheduleTime: null,        // 手术时间
  requiredTalk: [],          // 术前宣讲（多选）
  requiredOperation: [],     // 术前操作（多选）
  items: [{ drugId: '', dosage: '', note: '' }],  // 手术药品
  remarks: ''                // 备注
});

// 出院医嘱的响应式数据
const dischargeOrder = reactive({
  dischargeType: 2,          // 出院类型：1-治愈 2-好转 99-其他
  dischargeTime: null,       // 出院时间
  dischargeDiagnosis: '',    // 出院诊断
  dischargeInstructions: '', // 出院医嘱
  medicationInstructions: '',// 用药指导
  requiresFollowUp: false,   // 是否需要随访
  followUpDate: null,        // 随访日期
  items: []                  // 出院带回药品（可选）
});

// 操作医嘱的响应式数据
// 参考DTO: DTOs/OperationOrders/BatchCreateOperationOrderDto.cs
const operationOrder = reactive({
  // === 基础信息 ===
  isLongTerm: true,              // 医嘱类型：true=长期，false=临时
  plantEndTime: null,            // 计划结束时间（必填，DateTime）
  remarks: '',                   // 备注（可选，string）
  
  // === 操作信息 ===
  opId: '',                      // 操作代码（必填，如 "OP001"）
  operationName: '',             // 操作名称（必填，如 "更换引流袋"）
  operationSite: '',             // 操作部位（可选，如 "左臂"、"腹部"）
  normal: true,                  // 正常/异常标识（默认true，boolean）
  
  // === 时间策略（参照药品医嘱） ===
  timingStrategy: '',            // 'IMMEDIATE' | 'SPECIFIC' | 'CYCLIC' | 'SLOTS'
  startTime: null,               // DateTime? - SPECIFIC/CYCLIC/SLOTS 需要
  intervalHours: null,           // decimal? - 仅 CYCLIC 使用（如 6, 8, 12）
  intervalDays: 1,               // int - CYCLIC/SLOTS 使用（默认1=每天）
  smartSlotsMask: 0,             // int - 仅 SLOTS 使用（位掩码，如 2|32=早餐后+晚餐后）
  
  // === 执行要求 ===
  requiresPreparation: false,    // 是否需要准备物品（boolean）
  preparationItems: [],          // 准备物品列表（string[]，如 ["引流袋", "无菌手套"]）
  
  // === 任务配置 ===
  expectedDurationMinutes: null, // 预期执行时长（分钟，可选，int?）
  requiresResult: false,         // 是否需要记录结果（boolean）
  resultTemplate: null           // 结果模板（对象，可选，用于ResultPending类任务）
});

// 常用操作代码选项（参照后端 OperationOrderService.OperationNameMap）
const operationOptions = [
  // --- 一、呼吸道管理类 (高频治疗) ---
  { opId: 'OP001', name: '持续低流量吸氧', category: 'Duration', needsPreparation: true, preparationItems: ['氧气流量表', '湿化瓶', '灭菌注射用水', '鼻导管/吸氧面罩', '棉签'], needsResult: false, defaultDurationMinutes: 1440 },
  { opId: 'OP002', name: '雾化吸入治疗', category: 'Duration', needsPreparation: true, preparationItems: ['雾化器', '雾化面罩/咬嘴', '药液', '生理盐水'], needsResult: false, defaultDurationMinutes: 15 },
  { opId: 'OP003', name: '经口/鼻吸痰', category: 'Immediate', needsPreparation: true, preparationItems: ['负压吸引装置', '一次性吸痰管', '无菌手套', '生理盐水', '冲洗杯'], needsResult: false },
  { opId: 'OP004', name: '气管切开护理', category: 'Immediate', needsPreparation: true, preparationItems: ['气切护理包', '吸痰管', '无菌纱布', '系带', '过氧化氢/生理盐水'], needsResult: false },
  
  // --- 二、管路置入与维护类 (技术性操作) ---
  { opId: 'OP005', name: '留置胃管(鼻饲管置入)', category: 'Immediate', needsPreparation: true, preparationItems: ['一次性胃管', '50ml注射器', '治疗巾', '液状石蜡/润滑油', '胶布', '听诊器', '温开水'], needsResult: false },
  { opId: 'OP006', name: '胃肠减压护理', category: 'ResultPending', needsPreparation: true, preparationItems: ['负压引流器', '生理盐水', '冲洗注射器'], needsResult: true, defaultDurationMinutes: 1440 },
  { opId: 'OP007', name: '留置导尿术', category: 'Immediate', needsPreparation: true, preparationItems: ['一次性导尿包', '无菌手套', '尿袋', '碘伏', '润滑油', '胶布'], needsResult: false },
  { opId: 'OP008', name: '更换引流袋/尿袋', category: 'Immediate', needsPreparation: true, preparationItems: ['无菌引流袋/尿袋', '碘伏棉球', '弯盘', '血管钳'], needsResult: false },
  { opId: 'OP009', name: '膀胱冲洗', category: 'ResultPending', needsPreparation: true, preparationItems: ['膀胱冲洗器', '冲洗液(生理盐水)', '废液袋', '输液架'], needsResult: true, defaultDurationMinutes: 60 },
  { opId: 'OP010', name: '大量不保留灌肠', category: 'ResultPending', needsPreparation: true, preparationItems: ['灌肠袋/桶', '肛管', '润滑油', '灌肠液', '便盆', '一次性中单'], needsResult: true },
  
  // --- 三、静脉与标本采集类 (穿刺操作) ---
  { opId: 'OP011', name: '快速血糖监测(末梢)', category: 'ResultPending', needsPreparation: true, preparationItems: ['血糖仪', '血糖试纸', '采血笔', '75%酒精', '棉签', '锐器盒'], needsResult: true },
  { opId: 'OP012', name: '静脉留置针置管', category: 'Immediate', needsPreparation: true, preparationItems: ['静脉留置针', '透明敷贴', '止血带', '碘伏/酒精', '肝素帽', '冲管液'], needsResult: false },
  { opId: 'OP013', name: '静脉采血', category: 'Immediate', needsPreparation: true, preparationItems: ['真空采血管', '采血针', '止血带', '碘伏棉签', '锐器盒', '条码'], needsResult: false },
  { opId: 'OP014', name: '动脉血气采集', category: 'Immediate', needsPreparation: true, preparationItems: ['动脉血气针', '肝素钠', '无菌手套', '软木塞/橡皮胶', '棉球'], needsResult: false },
  { opId: 'OP015', name: '静脉输血护理', category: 'ResultPending', needsPreparation: true, preparationItems: ['输血器', '生理盐水', '无菌手套', '血液制品(需双人核对)'], needsResult: true, defaultDurationMinutes: 120 },
  
  // --- 四、伤口与皮肤护理类 ---
  { opId: 'OP016', name: '普通换药/敷料更换', category: 'Immediate', needsPreparation: true, preparationItems: ['无菌换药包', '棉球', '纱布', '胶带', '生理盐水', '碘伏'], needsResult: false },
  { opId: 'OP017', name: '造口护理', category: 'Immediate', needsPreparation: true, preparationItems: ['造口袋', '造口底盘', '造口剪', '量尺', '护肤粉', '防漏膏', '温水'], needsResult: false },
  { opId: 'OP018', name: '手术切口拆线', category: 'Immediate', needsPreparation: true, preparationItems: ['无菌拆线包(剪刀/镊子)', '碘伏', '敷料', '胶布'], needsResult: false },
  
  // --- 五、仪器监测与治疗类 ---
  { opId: 'OP019', name: '心电监护', category: 'Duration', needsPreparation: true, preparationItems: ['心电监护仪', '电极片', '酒精棉球'], needsResult: false, defaultDurationMinutes: 1440 },
  { opId: 'OP020', name: '微量泵/注射泵使用', category: 'Duration', needsPreparation: true, preparationItems: ['微量注射泵', '延长管', '50ml注射器', '药物标签'], needsResult: false, defaultDurationMinutes: 60 },
  { opId: 'OP021', name: '气压治疗(预防血栓)', category: 'Duration', needsPreparation: true, preparationItems: ['气压治疗仪', '腿套'], needsResult: false, defaultDurationMinutes: 30 }
];

// 准备物品输入
const preparationItemInput = ref('');

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

// 医生列表
const doctorList = ref([]);

// 麻醉方式选项
const anesthesiaOptions = [
  { value: '全身麻醉', label: '全身麻醉' },
  { value: '局部麻醉', label: '局部麻醉' },
  { value: '硬膜外麻醉', label: '硬膜外麻醉' },
  { value: '脊髓麻醉', label: '脊髓麻醉' },
  { value: '联合麻醉', label: '联合麻醉' }
];

// 术前宣讲选项
const talkOptions = [
  { value: '更换手术服', label: '更换手术服' },
  { value: '摘除配饰', label: '摘除配饰（首饰、手表等）' },
  { value: '术前禁食', label: '术前禁食禁饮' },
];

// 术前操作选项
const preoperativeOperationOptions = [
  { value: '备皮', label: '备皮' },
  { value: '皮肤清洁', label: '皮肤清洁' },
  { value: '手术位置标记', label: '手术位置标记' },
  { value: '胃管置入', label: '胃管置入' }
];

// 护理等级修改表单
const nursingGradeForm = reactive({
  newGrade: null,  // 新的护理等级 0-特级 1-一级 2-二级 3-三级
  reason: ''       // 修改原因
});

// 折叠状态
const leftCollapsed = ref(false);
const rightCollapsed = ref(false);

// 术前宣讲和术前操作的自定义输入
const customTalkInput = ref('');
const customOperationInput = ref('');
const customTalkItems = ref([]);
const customOperationItems = ref([]);

// 医嘱详情展开状态
const expandedOrders = ref([]);

// 检查患者是否可以暂存医嘱（待出院状态禁用）
const canAddToCart = computed(() => {
  if (!selectedPatient.value) return false;
  // PatientStatus.PendingDischarge = 2
  return selectedPatient.value.status !== 2;
});

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
    // 操作医嘱验证（参照药品类医嘱，必须选择病人）
    if (!selectedPatient.value) return false;
    if (!operationOrder.operationName) return false;
    if (!operationOrder.opId) return false; // OpId应该通过操作名称自动匹配
    if (!operationOrder.timingStrategy) return false;
    if (!operationOrder.plantEndTime) return false;

    const strategy = operationOrder.timingStrategy.toUpperCase();
    if (strategy === 'SPECIFIC' && !operationOrder.startTime) return false;
    if (strategy === 'CYCLIC' && (!operationOrder.startTime || !operationOrder.intervalHours || operationOrder.intervalHours <= 0)) return false;
    if (strategy === 'SLOTS' && (!operationOrder.startTime || operationOrder.smartSlotsMask <= 0)) return false;
    
    // 如果requiresPreparation为true，必须至少有一个准备物品
    if (operationOrder.requiresPreparation && operationOrder.preparationItems.length === 0) return false;
    
    return true;
  } else if (activeType.value === 'InspectionOrder') {
    // 检查医嘱验证 - 完善版
    if (!selectedPatient.value) return false;
    if (!inspectionOrder.category) return false;
    if (inspectionOrder.selectedItems.length === 0) return false;
    if (!inspectionOrder.clinicalDiagnosis) return false;
    return true;
  } else if (activeType.value === 'SurgicalOrder') {
    // 手术医嘱验证
    if (!surgicalOrder.surgeryName) return false;
    if (!surgicalOrder.anesthesiaType) return false;
    if (!surgicalOrder.incisionSite) return false;
    if (!surgicalOrder.surgeonId) return false;
    if (!surgicalOrder.scheduleTime) return false;
    return true;
  } else if (activeType.value === 'DischargeOrder') {
    // 出院医嘱验证
    if (!selectedPatient.value) return false;
    if (!dischargeOrder.dischargeType) return false;
    if (!dischargeOrder.dischargeTime) return false;
    if (!dischargeOrder.dischargeDiagnosis || !dischargeOrder.dischargeDiagnosis.trim()) return false;
    // 如果需要随访，则随访日期必填
    if (dischargeOrder.requiresFollowUp && !dischargeOrder.followUpDate) return false;
    // 如果有药品，则药品必须完整填写
    if (dischargeOrder.items.length > 0) {
      const hasInvalidItem = dischargeOrder.items.some(item => !item.drugId || !item.dosage);
      if (hasInvalidItem) return false;
    }
    return true;
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
      // 指定时间单次执行：不自动填充时间，由用户手动选择
      // currentOrder.startTime 和 plantEndTime 保持为 null，等待用户选择
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

// ==================== 操作医嘱相关函数 ====================

// 操作医嘱类型切换
const onOperationOrderTypeChange = (isLongTerm) => {
  operationOrder.isLongTerm = isLongTerm;
  
  // 重置策略选择
  operationOrder.timingStrategy = '';
  
  // 清空所有时间相关字段
  operationOrder.startTime = null;
  operationOrder.plantEndTime = null;
  operationOrder.intervalHours = null;
  operationOrder.intervalDays = 1;
  operationOrder.smartSlotsMask = 0;
};

// 操作名称选择处理（医生选择操作名称，系统自动匹配OpId）
// 注意：后端逻辑以OpId为主，OperationName是冗余字段，前端只是为了让医生更方便选择
const onOperationNameChange = (operationName) => {
  const selectedOp = operationOptions.find(op => op.name === operationName);
  if (selectedOp) {
    // 自动匹配操作代码（这是关键字段，后端逻辑都基于此）
    operationOrder.opId = selectedOp.opId;
    
    // 根据操作类型自动设置是否需要准备物品（基于opId对应的操作类型）
    if (selectedOp.needsPreparation !== undefined) {
      operationOrder.requiresPreparation = selectedOp.needsPreparation;
      // 如果不需要准备物品，清空准备物品列表
      if (!selectedOp.needsPreparation) {
        operationOrder.preparationItems = [];
      } else {
        // 如果需要准备物品，自动填充默认准备物品列表
        if (selectedOp.preparationItems && selectedOp.preparationItems.length > 0) {
          operationOrder.preparationItems = [...selectedOp.preparationItems];
        }
      }
    }
    
    // 根据操作类型自动设置是否需要记录结果（基于opId对应的操作类型）
    if (selectedOp.needsResult !== undefined) {
      operationOrder.requiresResult = selectedOp.needsResult;
    } else {
      // 如果没有明确设置，根据类别判断（操作类医嘱只有 ResultPending 需要结果）
      operationOrder.requiresResult = selectedOp.category === 'ResultPending';
    }
    
    // 持续类操作或需要持续时间的ResultPending类操作自动填充默认预期执行时长
    if (selectedOp.defaultDurationMinutes) {
      // 如果用户还没有设置过，则使用默认值
      if (!operationOrder.expectedDurationMinutes) {
        operationOrder.expectedDurationMinutes = selectedOp.defaultDurationMinutes;
      }
    } else if (!selectedOp.defaultDurationMinutes) {
      // 没有默认持续时间的操作清空预期执行时长
      operationOrder.expectedDurationMinutes = null;
    }
  } else {
    // 如果找不到匹配的操作，清空OpId
    operationOrder.opId = '';
    ElMessage.warning('未找到匹配的操作代码，请重新选择操作名称');
  }
};

// 获取操作类别标签
const getCategoryLabel = (category) => {
  const categoryMap = {
    'Immediate': '即刻类',
    'Duration': '持续类',
    'ResultPending': '结果类',
    'DataCollection': '数据收集类'
  };
  return categoryMap[category] || category;
};

// 获取当前选择的操作类别
const getSelectedOperationCategory = () => {
  if (!operationOrder.operationName) return null;
  const selectedOp = operationOptions.find(op => op.name === operationOrder.operationName);
  return selectedOp ? selectedOp.category : null;
};

// 判断是否应该显示预期持续时间输入框
const shouldShowDurationInput = () => {
  if (!operationOrder.operationName) return false;
  const selectedOp = operationOptions.find(op => op.name === operationOrder.operationName);
  if (!selectedOp) return false;
  // Duration类操作或有defaultDurationMinutes的操作都显示
  return selectedOp.category === 'Duration' || selectedOp.defaultDurationMinutes !== undefined;
};

// 操作医嘱时间策略切换
const onOperationStrategyChange = () => {
  const strategy = operationOrder.timingStrategy.toUpperCase();
  
  // 清空不适用字段
  if (strategy === 'IMMEDIATE') {
    operationOrder.intervalHours = null;
    operationOrder.smartSlotsMask = 0;
    // IMMEDIATE策略：开始时间和结束时间都为当前时间
    const immediateNow = new Date();
    operationOrder.startTime = getLocalISOString(immediateNow);
    operationOrder.plantEndTime = getLocalISOString(immediateNow);
  } else if (strategy === 'SPECIFIC') {
    operationOrder.intervalHours = null;
    operationOrder.smartSlotsMask = 0;
    operationOrder.intervalDays = 1;
    // SPECIFIC策略：不自动填充时间，由用户手动选择
    // operationOrder.startTime 和 plantEndTime 保持为 null，等待用户选择
  } else if (strategy === 'CYCLIC') {
    operationOrder.smartSlotsMask = 0;
    operationOrder.intervalDays = 1;
    // CYCLIC策略：默认每8小时，从当前时间开始
    const cyclicNow = new Date();
    operationOrder.startTime = getLocalISOString(cyclicNow);
    operationOrder.intervalHours = 8;
    const cyclicEnd = new Date();
    cyclicEnd.setDate(cyclicEnd.getDate() + 7); // 7天后
    operationOrder.plantEndTime = getLocalISOString(cyclicEnd);
  } else if (strategy === 'SLOTS') {
    operationOrder.intervalHours = null;
    operationOrder.intervalDays = 1;
    // SLOTS策略：默认从当前时间开始
    const slotsNow = new Date();
    operationOrder.startTime = getLocalISOString(slotsNow);
    const slotsEnd = new Date();
    slotsEnd.setDate(slotsEnd.getDate() + 7); // 7天后
    operationOrder.plantEndTime = getLocalISOString(slotsEnd);
  }
};

// 监听 SPECIFIC 策略的 startTime 变化，自动同步到 plantEndTime（操作医嘱）
watch(() => operationOrder.startTime, (newVal) => {
  if (operationOrder.timingStrategy === 'SPECIFIC' && newVal) {
    operationOrder.plantEndTime = newVal;
  }
});

// 操作医嘱时段操作
const toggleOperationSlot = (slotId) => {
  operationOrder.smartSlotsMask ^= slotId;
};

const isOperationSlotSelected = (slotId) => {
  return (operationOrder.smartSlotsMask & slotId) !== 0;
};

const getSelectedOperationSlotsCount = () => {
  let count = 0;
  let mask = operationOrder.smartSlotsMask;
  while (mask) {
    count += mask & 1;
    mask >>= 1;
  }
  return count;
};

// 获取已选时段的文本描述（用于待提交清单显示）
const getSelectedOperationSlotsText = (mask) => {
  if (!mask || mask === 0) return '未选择';
  
  const slotNames = [];
  if (mask & 1) slotNames.push('早餐前');
  if (mask & 2) slotNames.push('早餐后');
  if (mask & 4) slotNames.push('午餐前');
  if (mask & 8) slotNames.push('午餐后');
  if (mask & 16) slotNames.push('晚餐前');
  if (mask & 32) slotNames.push('晚餐后');
  if (mask & 64) slotNames.push('睡前');
  
  return slotNames.join('、');
};

// 准备物品管理
const addPreparationItem = () => {
  const item = preparationItemInput.value.trim();
  if (!item) {
    ElMessage.warning('请输入准备物品名称');
    return;
  }
  
  if (operationOrder.preparationItems.includes(item)) {
    ElMessage.warning('该物品已存在');
    return;
  }
  
  operationOrder.preparationItems.push(item);
  preparationItemInput.value = '';
  ElMessage.success(`已添加：${item}`);
};

const removePreparationItem = (index) => {
  const removed = operationOrder.preparationItems.splice(index, 1)[0];
  ElMessage.info({ message: `已移除：${removed}`, duration: 3000 });
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
  
  // 检查患者状态：待出院患者不允许开医嘱
  if (patient.status === 2) { // PatientStatus.PendingDischarge = 2
    ElMessageBox.alert(
      `患者 ${patient.name} (${patient.bedId}) 当前状态为"待出院"，不允许开具新医嘱。`,
      '温馨提示',
      {
        confirmButtonText: '我知道了',
        type: 'info',
        center: true
      }
    );
    return;
  }
  
  // 检查是否有未提交的数据（包括所有医嘱类型）
  const hasUnsubmittedData = 
    (activeType.value === 'MedicationOrder' && currentOrder.items.some(i => i.drugId && i.dosage)) ||
    (activeType.value === 'OperationOrder' && (operationOrder.opId || operationOrder.operationName)) ||
    (activeType.value === 'InspectionOrder' && inspectionOrder.selectedItems.length > 0) ||
    (activeType.value === 'SurgicalOrder' && surgicalOrder.surgeryName) ||
    orderCart.value.length > 0;
  
  if (hasUnsubmittedData) {
    if (confirm('切换患者将清空当前表单和待提交清单，是否继续？')) {
      selectedPatient.value = patient;
      clearForm();
      orderCart.value = [];
      expandedOrders.value = [];
      // ElMessage.success(`已切换至患者：${patient.name} (${patient.bedId})`);
    }
  } else {
    selectedPatient.value = patient;
    // ElMessage.success(`已切换至患者：${patient.name} (${patient.bedId})`);
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

// 手术药品增删
const addSurgicalItem = () => {
  surgicalOrder.items.push({ drugId: '', dosage: '', note: '' });
};

const removeSurgicalItem = (index) => {
  if (surgicalOrder.items.length > 1) {
    surgicalOrder.items.splice(index, 1);
  }
};

// 出院药品增删
const addDischargeDrug = () => {
  dischargeOrder.items.push({ drugId: '', dosage: '', note: '' });
};

const removeDischargeDrug = (index) => {
  dischargeOrder.items.splice(index, 1);
};

// 添加自定义术前宣讲
const addCustomTalk = () => {
  const value = customTalkInput.value.trim();
  if (value && !surgicalOrder.requiredTalk.includes(value) && !customTalkItems.value.includes(value)) {
    customTalkItems.value.push(value);
    surgicalOrder.requiredTalk.push(value);
    customTalkInput.value = '';
  }
};

// 移除自定义术前宣讲
const removeCustomTalk = (item) => {
  const index = customTalkItems.value.indexOf(item);
  if (index > -1) {
    customTalkItems.value.splice(index, 1);
  }
  const reqIndex = surgicalOrder.requiredTalk.indexOf(item);
  if (reqIndex > -1) {
    surgicalOrder.requiredTalk.splice(reqIndex, 1);
  }
};

// 添加自定义术前操作
const addCustomOperation = () => {
  const value = customOperationInput.value.trim();
  if (value && !surgicalOrder.requiredOperation.includes(value) && !customOperationItems.value.includes(value)) {
    customOperationItems.value.push(value);
    surgicalOrder.requiredOperation.push(value);
    customOperationInput.value = '';
  }
};

// 移除自定义术前操作
const removeCustomOperation = (item) => {
  const index = customOperationItems.value.indexOf(item);
  if (index > -1) {
    customOperationItems.value.splice(index, 1);
  }
  const reqIndex = surgicalOrder.requiredOperation.indexOf(item);
  if (reqIndex > -1) {
    surgicalOrder.requiredOperation.splice(reqIndex, 1);
  }
};

// 检查项目选择处理
// 检查类别切换时清空已选项目
const handleCategoryChange = () => {
  inspectionOrder.selectedItems = [];
  inspectionOrder.customItems = {};
  inspectionOrder.specimenType = '';
  inspectionOrder.samplingRequirements = [];
  inspectionOrder.location = '';
  inspectionOrder.contrastAgent = 'NONE';
  inspectionOrder.anesthesiaType = 'NONE';
  inspectionOrder.specimenSource = '';
};

// 获取检查项目的显示名称
const getInspectionItemName = (itemCode) => {
  const inspectionNames = {
    // 化验检查
    'LAB_BLOOD_ROUTINE': '血常规',
    'LAB_BLOOD_BIOCHEM': '生化全套',
    'LAB_BLOOD_GLUCOSE': '血糖',
    'LAB_BLOOD_LIPID': '血脂四项',
    'LAB_LIVER_FUNCTION': '肝功能',
    'LAB_KIDNEY_FUNCTION': '肾功能',
    'LAB_ELECTROLYTE': '电解质',
    'LAB_COAGULATION': '凝血功能',
    'LAB_BLOOD_GAS': '血气分析',
    'LAB_THYROID': '甲状腺功能',
    'LAB_CARDIAC_MARKER': '心肌标志物',
    'LAB_TUMOR_MARKER': '肿瘤标志物',
    'LAB_URINE_ROUTINE': '尿常规',
    'LAB_STOOL_ROUTINE': '大便常规',
    'LAB_STOOL_OB': '大便隐血',
    'LAB_SPUTUM': '痰培养+药敏',
    'LAB_HBV': '乙肝五项',
    'LAB_HIV': 'HIV抗体',
    'LAB_SYPHILIS': '梅毒抗体',
    'LAB_HCV': '丙肝抗体',
    'LAB_CRP': 'C反应蛋白',
    'LAB_RF': '类风湿因子',
    
    // X线检查
    'XRAY_CHEST': '胸部X线',
    'XRAY_ABDOMEN': '腹部X线',
    'XRAY_SPINE': '脊柱X线',
    'XRAY_LIMB': '四肢X线',
    
    // CT检查
    'CT_HEAD': '头颅CT',
    'CT_CHEST': '胸部CT',
    'CT_ABDOMEN': '腹部CT',
    'CT_PELVIS': '盆腔CT',
    'CT_SPINE': '脊柱CT',
    'CT_CTA': 'CT血管造影',
    
    // MRI检查
    'MRI_HEAD': '头颅MRI',
    'MRI_SPINE': '脊柱MRI',
    'MRI_JOINT': '关节MRI',
    'MRI_ABDOMEN': '腹部MRI',
    'MRI_MRA': '磁共振血管造影',
    
    // 超声检查
    'US_ABDOMEN': '腹部超声',
    'US_CARDIAC': '心脏超声',
    'US_THYROID': '甲状腺超声',
    'US_BREAST': '乳腺超声',
    'US_VASCULAR': '血管超声',
    'US_OBSTETRIC': '产科超声',
    
    // 功能检查
    'ECG': '常规心电图',
    'ECG_24H': '24小时动态心电图',
    'EXERCISE_ECG': '运动心电图',
    'EEG': '脑电图',
    'EMG': '肌电图',
    'PFT': '肺功能检查',
    'ABPM': '24小时动态血压',
    'TCD': '经颅多普勒',
    'SLEEP_MONITOR': '睡眠监测',
    
    // 内窥镜检查
    'ENDO_GASTROSCOPY': '胃镜检查',
    'ENDO_COLONOSCOPY': '肠镜检查',
    'ENDO_BRONCHOSCOPY': '支气管镜',
    'ENDO_LARYNGOSCOPY': '喉镜检查',
    'ENDO_CYSTOSCOPY': '膀胱镜检查',
    'ENDO_HYSTEROSCOPY': '宫腔镜检查',
    'ENDO_ARTHROSCOPY': '关节镜检查',
    
    // 病理检查
    'PATH_BIOPSY': '组织活检',
    'PATH_CYTOLOGY': '细胞学检查',
    'PATH_FROZEN': '冰冻切片',
    'PATH_IMMUNOHISTO': '免疫组化',
    'PATH_MOLECULAR': '分子病理'
  };
  
  // 如果是"其他"项目，使用自定义输入的内容
  if (itemCode.endsWith('_OTHER') && inspectionOrder.customItems[itemCode]) {
    return inspectionOrder.customItems[itemCode];
  }
  
  return inspectionNames[itemCode] || itemCode;
};

// 表单操作

// TODO: 清空表单时需根据医嘱类型清空对应的数据
const clearForm = () => {
  if (activeType.value === 'OperationOrder') {
    // 清空操作医嘱表单
    operationOrder.isLongTerm = true;
    operationOrder.operationName = ''; // 先清空操作名称
    operationOrder.opId = ''; // 操作代码会自动清空
    operationOrder.operationSite = '';
    operationOrder.normal = true;
    operationOrder.timingStrategy = '';
    operationOrder.startTime = null;
    operationOrder.plantEndTime = null;
    operationOrder.intervalHours = null;
    operationOrder.intervalDays = 1;
    operationOrder.smartSlotsMask = 0;
    operationOrder.requiresPreparation = false;
    operationOrder.preparationItems = [];
    operationOrder.expectedDurationMinutes = null;
    operationOrder.requiresResult = false;
    operationOrder.resultTemplate = null;
    operationOrder.remarks = '';
    preparationItemInput.value = '';
  } else if (activeType.value === 'InspectionOrder') {
    // 清空检查医嘱表单
    inspectionOrder.category = '';
    inspectionOrder.selectedItems = [];
    inspectionOrder.customItems = {};
    inspectionOrder.specimenType = '';
    inspectionOrder.samplingRequirements = [];
    inspectionOrder.location = '';
    inspectionOrder.contrastAgent = 'NONE';
    inspectionOrder.anesthesiaType = 'NONE';
    inspectionOrder.specimenSource = '';
    inspectionOrder.clinicalDiagnosis = '';
    inspectionOrder.purpose = 'DIAGNOSIS';
    inspectionOrder.remarks = '';
  } else if (activeType.value === 'SurgicalOrder') {
    // 清空手术医嘱表单
    surgicalOrder.surgeryName = '';
    surgicalOrder.anesthesiaType = '';
    surgicalOrder.incisionSite = '';
    surgicalOrder.surgeonId = '';
    surgicalOrder.scheduleTime = null;
    surgicalOrder.requiredTalk = [];
    surgicalOrder.requiredOperation = [];
    surgicalOrder.items = getDefaultSurgicalItems();
    surgicalOrder.remarks = '';
    // 清空自定义输入
    customTalkInput.value = '';
    customOperationInput.value = '';
    customTalkItems.value = [];
    customOperationItems.value = [];
  } else if (activeType.value === 'DischargeOrder') {
    // 清空出院医嘱表单
    dischargeOrder.dischargeType = 2;
    dischargeOrder.dischargeTime = null;
    dischargeOrder.dischargeDiagnosis = '';
    dischargeOrder.dischargeInstructions = '';
    dischargeOrder.medicationInstructions = '';
    dischargeOrder.requiresFollowUp = false;
    dischargeOrder.followUpDate = null;
    dischargeOrder.items = [];
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
const addToCart = async () => {
  if (!isFormValid.value) {
    ElMessage.warning('请完善必填项后再暂存');
    return;
  }
  
  // 根据医嘱类型暂存对应数据
  if (activeType.value === 'OperationOrder') {
    // 暂存操作医嘱（参照药品类医嘱，必须包含patientId）
    // 如果是长期医嘱，验证结束时间不能早于开始时间
    if (operationOrder.isLongTerm && operationOrder.startTime && operationOrder.plantEndTime) {
      const startTime = new Date(operationOrder.startTime);
      const endTime = new Date(operationOrder.plantEndTime);
      if (endTime.getTime() < startTime.getTime()) {
        ElMessage.error('长期医嘱的结束时间不能早于开始时间');
        return;
      }
    }
    
    const orderData = {
      orderType: 'OperationOrder',
      patientId: selectedPatient.value.id, // 添加patientId字段
      isLongTerm: operationOrder.isLongTerm,
      opId: operationOrder.opId,
      operationName: operationOrder.operationName,
      operationSite: operationOrder.operationSite || null,
      normal: operationOrder.normal,
      timingStrategy: operationOrder.timingStrategy,
      startTime: operationOrder.startTime,
      plantEndTime: operationOrder.plantEndTime,
      intervalHours: operationOrder.intervalHours,
      intervalDays: operationOrder.intervalDays,
      smartSlotsMask: operationOrder.smartSlotsMask,
      requiresPreparation: operationOrder.requiresPreparation,
      preparationItems: operationOrder.preparationItems.length > 0 ? [...operationOrder.preparationItems] : null,
      expectedDurationMinutes: operationOrder.expectedDurationMinutes,
      requiresResult: operationOrder.requiresResult,
      resultTemplate: operationOrder.resultTemplate,
      remarks: operationOrder.remarks || null
    };
    
    orderCart.value.push(orderData);
    ElMessage.success('操作医嘱已暂存到待提交清单');
    clearForm();
  } else if (activeType.value === 'InspectionOrder') {
    // 暂存检查医嘱 - 为每个选中的项目创建一条医嘱
    let hasError = false;
    
    inspectionOrder.selectedItems.forEach(itemCode => {
      // 如果是"其他"项目，检查是否填写了自定义内容
      if (itemCode.endsWith('_OTHER') && !inspectionOrder.customItems[itemCode]) {
        ElMessage.warning('请填写"其他"项目的具体内容');
        hasError = true;
        return;
      }
      
    orderCart.value.push({
      orderType: 'InspectionOrder',
        itemCode: itemCode,
        itemName: getInspectionItemName(itemCode),
        category: inspectionOrder.category,
        location: inspectionOrder.location,
        contrastAgent: inspectionOrder.contrastAgent,
        anesthesiaType: inspectionOrder.anesthesiaType,
        specimenSource: inspectionOrder.specimenSource,
        clinicalDiagnosis: inspectionOrder.clinicalDiagnosis,
        purpose: inspectionOrder.purpose,
        remarks: inspectionOrder.remarks,
      patientId: selectedPatient.value.id
    });
    });
    
    if (hasError) return;
  } else if (activeType.value === 'SurgicalOrder') {
    // 暂存手术医嘱
    orderCart.value.push({
      ...JSON.parse(JSON.stringify(surgicalOrder)),
      orderType: 'SurgicalOrder',
      patientId: selectedPatient.value.id
    });
  } else if (activeType.value === 'DischargeOrder') {
    // 出院医嘱需要先进行严格的前置验证
    await validateAndAddDischargeOrder();
    return;
  } else {
    // 暂存药品医嘱（原有逻辑）
    // 如果是长期医嘱，验证结束时间不能早于开始时间
    if (currentOrder.isLongTerm && currentOrder.startTime && currentOrder.plantEndTime) {
      const startTime = new Date(currentOrder.startTime);
      const endTime = new Date(currentOrder.plantEndTime);
      if (endTime.getTime() < startTime.getTime()) {
        ElMessage.error('长期医嘱的结束时间不能早于开始时间');
        return;
      }
    }
    
    orderCart.value.push({
      ...JSON.parse(JSON.stringify(currentOrder)),
      orderType: 'MedicationOrder',
      patientId: selectedPatient.value.id
    });
  }
  
  ElMessage.success('医嘱已暂存到待提交清单');
  clearForm();
};

// 验证并添加出院医嘱到暂存清单
const validateAndAddDischargeOrder = async () => {
  // 0. 检查购物车中是否已有出院医嘱
  const hasDischargeOrder = orderCart.value.some(o => o.orderType === 'DischargeOrder');
  if (hasDischargeOrder) {
    ElMessageBox.alert(
      `<div style="text-align: left;">
        <h3 style="color: #e6a23c; margin-bottom: 15px;">⚠️ 无法暂存出院医嘱</h3>
        <div style="padding: 12px; background: #fdf6ec; border-left: 4px solid #e6a23c; border-radius: 4px;">
          <div style="font-size: 14px; line-height: 1.8; color: #606266;">
            购物车中已存在出院医嘱，不允许再添加出院医嘱。
          </div>
          <div style="margin-top: 15px; padding: 10px; background: #fff3cd; border-radius: 4px;">
            <div style="font-size: 13px; line-height: 1.6; color: #856404;">
              <strong>💡 提示：</strong><br>
              • 每个患者只能有一条出院医嘱<br>
              • 如需修改，请先从购物车中删除现有出院医嘱
            </div>
          </div>
        </div>
      </div>`,
      '无法添加',
      {
        dangerouslyUseHTMLString: true,
        confirmButtonText: '我知道了',
        type: 'warning'
      }
    );
    return;
  }
  
  const loading = ElLoading.service({
    lock: true,
    text: '正在验证出院条件...',
    background: 'rgba(0, 0, 0, 0.7)'
  });

  try {
    // 1. 调用后端验证接口
    const validationResult = await validateDischargeOrderCreation(selectedPatient.value.id);
    
    const dischargeTime = new Date(dischargeOrder.dischargeTime);
    const earliestTime = validationResult.earliestDischargeTime ? new Date(validationResult.earliestDischargeTime) : null;
    
    // 2. 检查时间是否符合要求（这是强制条件）
    if (earliestTime && dischargeTime < earliestTime) {
      loading.close();
      
      // 构建时间不符合的错误提示
      let errorHtml = '<div style="text-align: left;">';
      errorHtml += '<h3 style="color: #f56c6c; margin-bottom: 15px;">❌ 出院时间不符合要求</h3>';
      
      errorHtml += '<div style="padding: 12px; background: #fef0f0; border-left: 4px solid #f56c6c; border-radius: 4px;">';
      errorHtml += '<div style="font-weight: bold; color: #f56c6c; margin-bottom: 8px;">⏰ 时间冲突：</div>';
      errorHtml += '<div style="font-size: 13px; line-height: 1.8;">';
      errorHtml += `<div>• 您设置的出院时间：<strong>${formatDateTime(dischargeTime)}</strong></div>`;
      errorHtml += `<div>• 最早允许出院时间：<strong style="color: #f56c6c;">${formatDateTime(earliestTime)}</strong></div>`;
      errorHtml += '<div style="margin-top: 10px; padding: 10px; background: white; border-radius: 4px; color: #f56c6c;">';
      errorHtml += '❌ 出院时间早于最早允许时间，必须重新设置！';
      errorHtml += '</div></div></div>';
      
      // 显示阻塞医嘱信息（如果有）
      if (validationResult.blockedOrders && validationResult.blockedOrders.length > 0) {
        errorHtml += '<div style="margin-top: 20px;"><strong style="color: #e6a23c;">存在 ' + validationResult.blockedOrders.length + ' 条阻塞医嘱：</strong></div>';
        errorHtml += '<ul style="margin: 5px 0; padding-left: 20px; max-height: 150px; overflow-y: auto;">';
        validationResult.blockedOrders.forEach(order => {
          const startTime = order.startTime ? formatDateTime(new Date(order.startTime)) : '未知';
          const endTime = order.endTime ? formatDateTime(new Date(order.endTime)) : '未知';
          errorHtml += `<li style="margin-bottom: 8px; font-size: 13px;">
            <div><strong>${order.summary}</strong></div>
            <div style="color: #909399; font-size: 12px;">状态: ${order.statusDisplay}</div>
            <div style="color: #909399; font-size: 12px;">时间: ${startTime} - ${endTime}</div>
          </li>`;
        });
        errorHtml += '</ul>';
      }
      
      // 显示待处理任务信息（如果有）
      if (validationResult.pendingStopOrders && validationResult.pendingStopOrders.length > 0) {
        errorHtml += '<div style="margin-top: 15px;"><strong style="color: #e6a23c;">待完成任务的医嘱：</strong></div>';
        errorHtml += '<ul style="margin: 5px 0; padding-left: 20px;">';
        validationResult.pendingStopOrders.forEach(order => {
          const taskTime = order.latestTaskPlannedTime ? formatDateTime(new Date(order.latestTaskPlannedTime)) : '未知';
          errorHtml += `<li style="margin-bottom: 8px; font-size: 13px;">
            <div><strong>${order.summary}</strong></div>
            <div style="color: #909399; font-size: 12px;">最晚任务时间: ${taskTime}</div>
          </li>`;
        });
        errorHtml += '</ul>';
      }
      
      errorHtml += '<div style="margin-top: 20px; padding: 12px; background: #f0f9ff; border-left: 4px solid #409eff; border-radius: 4px;">';
      errorHtml += '<div style="font-size: 13px; line-height: 1.6;">';
      errorHtml += '<strong style="color: #409eff;">💡 处理建议：</strong><br>';
      errorHtml += `1. 设置出院时间不早于 <strong>${formatDateTime(earliestTime)}</strong><br>`;
      errorHtml += '2. 或先完成所有未完成的任务<br>';
      errorHtml += '3. 或先签收所有待签收医嘱';
      errorHtml += '</div></div>';
      
      errorHtml += '</div>';
      
      // 使用MessageBox显示详细信息
      ElMessageBox.alert(errorHtml, '出院时间不符合要求', {
        dangerouslyUseHTMLString: true,
        confirmButtonText: '重新设置',
        type: 'error',
        customClass: 'discharge-validation-dialog',
        callback: () => {
          // 清空出院时间，保留其他数据
          dischargeOrder.dischargeTime = null;
          ElMessage.warning({
            message: `请设置出院时间不早于 ${formatDateTime(earliestTime)}`,
            duration: 5000,
            showClose: true
          });
        }
      });
      
      return;
    }
    
    // 3. 时间符合要求，但检查是否有阻塞医嘱（警告但允许继续）
    if (validationResult.blockedOrders && validationResult.blockedOrders.length > 0) {
      loading.close();
      
      // 构建警告提示
      let warningHtml = '<div style="text-align: left;">';
      warningHtml += '<h3 style="color: #e6a23c; margin-bottom: 15px;">⚠️ 存在阻塞医嘱警告</h3>';
      
      warningHtml += '<div style="padding: 12px; background: #fdf6ec; border-left: 4px solid #e6a23c; border-radius: 4px; margin-bottom: 15px;">';
      warningHtml += '<div style="font-size: 13px; line-height: 1.6; color: #606266;">';
      warningHtml += '检测到患者当前有未完成的医嘱或任务，虽然出院时间符合要求，但建议您先处理完这些医嘱后再开具出院医嘱。';
      warningHtml += '</div></div>';
      
      warningHtml += '<div style="margin-bottom: 15px;"><strong style="color: #f56c6c;">存在 ' + validationResult.blockedOrders.length + ' 条阻塞医嘱：</strong></div>';
      warningHtml += '<ul style="margin: 0; padding-left: 20px; max-height: 250px; overflow-y: auto; border: 1px solid #ebeef5; border-radius: 4px; padding: 10px; background: #fafafa;">';
      validationResult.blockedOrders.forEach(order => {
        const startTime = order.startTime ? formatDateTime(new Date(order.startTime)) : '未知';
        const endTime = order.endTime ? formatDateTime(new Date(order.endTime)) : '未知';
        warningHtml += `<li style="margin-bottom: 12px; padding-bottom: 12px; border-bottom: 1px solid #ebeef5;">
          <div style="margin-bottom: 4px;"><strong style="color: #303133;">${order.summary}</strong></div>
          <div style="color: #909399; font-size: 12px; line-height: 1.5;">状态: ${order.statusDisplay}</div>
          <div style="color: #909399; font-size: 12px; line-height: 1.5;">时间: ${startTime} - ${endTime}</div>
        </li>`;
      });
      warningHtml += '</ul>';
      
      // 显示待处理任务信息（如果有）
      if (validationResult.pendingStopOrders && validationResult.pendingStopOrders.length > 0) {
        warningHtml += '<div style="margin-top: 15px;"><strong style="color: #e6a23c;">待完成任务的医嘱：</strong></div>';
        warningHtml += '<ul style="margin: 5px 0; padding-left: 20px;">';
        validationResult.pendingStopOrders.forEach(order => {
          const taskTime = order.latestTaskPlannedTime ? formatDateTime(new Date(order.latestTaskPlannedTime)) : '未知';
          warningHtml += `<li style="margin-bottom: 8px; font-size: 13px;">
            <div><strong>${order.summary}</strong></div>
            <div style="color: #909399; font-size: 12px;">最晚任务时间: ${taskTime}</div>
          </li>`;
        });
        warningHtml += '</ul>';
      }
      
      warningHtml += '<div style="margin-top: 20px; padding: 12px; background: #fff3cd; border-left: 4px solid #e6a23c; border-radius: 4px;">';
      warningHtml += '<div style="font-size: 13px; line-height: 1.6; color: #856404;">';
      warningHtml += '<strong>⚠️ 温馨提示：</strong><br>';
      warningHtml += '• 出院时间符合要求，您可以继续开具出院医嘱<br>';
      warningHtml += '• 但建议先完成上述医嘱和任务，以确保患者安全出院';
      warningHtml += '</div></div>';
      
      warningHtml += '</div>';
      
      // 使用MessageBox显示警告并让医生确认
      ElMessageBox.confirm(warningHtml, '阻塞医嘱确认', {
        dangerouslyUseHTMLString: true,
        confirmButtonText: '我已确认，继续开具',
        cancelButtonText: '取消，先处理医嘱',
        type: 'warning',
        customClass: 'discharge-validation-dialog',
        distinguishCancelAndClose: true,
        closeOnClickModal: false
      }).then(() => {
        // 医生确认后，添加到暂存清单
        orderCart.value.push({
          ...JSON.parse(JSON.stringify(dischargeOrder)),
          orderType: 'DischargeOrder',
          patientId: selectedPatient.value.id,
          validationResult: validationResult // 保存验证结果供后续使用
        });
        
        ElMessage.success({
          message: '✅ 出院医嘱已暂存到待提交清单',
          duration: 3000,
          showClose: true
        });
        
        clearForm();
      }).catch((action) => {
        // 用户取消
        if (action === 'cancel') {
          ElMessage.info({ message: '已取消，请先处理阻塞医嘱', duration: 3000 });
        }
      });
      
      return;
    }
    
    // 4. 没有任何问题，直接添加到暂存清单
    loading.close();
    
    orderCart.value.push({
      ...JSON.parse(JSON.stringify(dischargeOrder)),
      orderType: 'DischargeOrder',
      patientId: selectedPatient.value.id,
      validationResult: validationResult // 保存验证结果供后续使用
    });
    
    ElMessage.success({
      message: '✅ 出院医嘱验证通过，已暂存到待提交清单',
      duration: 3000,
      showClose: true
    });
    
    clearForm();
    
  } catch (error) {
    loading.close();
    console.error('验证出院医嘱失败:', error);
    
    ElMessageBox.alert(
      `<div style="text-align: left;">
        <div style="margin-bottom: 10px;"><strong style="color: #f56c6c;">❌ 验证失败</strong></div>
        <div style="line-height: 1.8;">
          <div style="color: #909399;">调用验证接口时发生错误</div>
          <div style="margin-top: 10px; padding: 10px; background: #f5f5f5; border-radius: 4px; font-family: monospace; font-size: 12px; color: #f56c6c;">
            ${error.response?.data?.message || error.message || '未知错误'}
          </div>
        </div>
      </div>`,
      '系统错误',
      {
        dangerouslyUseHTMLString: true,
        confirmButtonText: '确定',
        type: 'error'
      }
    );
  }
};

const removeFromCart = (index) => {
  orderCart.value.splice(index, 1);
  ElMessage.info({ message: '已从清单中移除', duration: 3000 });
};

const clearCart = () => {
  orderCart.value = [];
  ElMessage.info({ message: '已清空待提交清单', duration: 3000 });
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
    const dischargeOrders = orderCart.value.filter(o => o.orderType === 'DischargeOrder');
    
    // 🚨 方案3核心逻辑：如果包含出院医嘱，特殊处理
    if (dischargeOrders.length > 0) {
      // 1. 如果有其他医嘱，先提示并提交其他医嘱
      const otherOrders = orderCart.value.filter(o => o.orderType !== 'DischargeOrder');
      if (otherOrders.length > 0) {
        // 1.1 显示确认弹窗（只有当购物车中不止出院医嘱时才显示）
        try {
          await ElMessageBox.confirm(
            `<div style="text-align: left; line-height: 1.8;">
              <div style="margin-bottom: 15px; padding: 12px; background: #fff3cd; border-left: 4px solid #e6a23c; border-radius: 4px;">
                <div style="font-weight: bold; color: #856404; margin-bottom: 8px;">📋 检测到购物车中包含出院医嘱</div>
                <div style="font-size: 13px; color: #856404;">
                  为确保医嘱提交顺序正确，系统将按以下顺序处理：
                </div>
              </div>
              <div style="padding: 15px; background: #f8f9fa; border-radius: 4px; margin-bottom: 15px;">
                <div style="font-size: 13px; line-height: 2;"><strong>第一步：</strong> 提交 <span style="color: #409eff; font-weight: bold;">${otherOrders.length}</span> 条其他医嘱</div>
                <div style="font-size: 13px; line-height: 2;"><strong>第二步：</strong> 再次验证出院条件</div>
                <div style="font-size: 13px; line-height: 2;"><strong>第三步：</strong> 提交 <span style="color: #f56c6c; font-weight: bold;">${dischargeOrders.length}</span> 条出院医嘱</div>
              </div>
              <div style="padding: 10px; background: #e7f3ff; border-radius: 4px; font-size: 12px; color: #0066cc;">
                💡 <strong>提示：</strong>此顺序可确保出院医嘱提交前的验证准确性
              </div>
            </div>`,
            '出院医嘱提交顺序确认',
            {
              dangerouslyUseHTMLString: true,
              confirmButtonText: '确认，按此顺序提交',
              cancelButtonText: '取消',
              type: 'warning',
              distinguishCancelAndClose: true,
              closeOnClickModal: false
            }
          );
        } catch {
          submitting.value = false;
          return; // 用户取消
        }
        
        // 1.1 先提交其他医嘱
        const otherOrdersResult = await submitNonDischargeOrders(
          medicationOrders, 
          inspectionOrders, 
          surgicalOrders, 
          operationOrders
        );
        
        if (!otherOrdersResult.success) {
          submitting.value = false;
          ElMessage.error(`其他医嘱提交失败：${otherOrdersResult.errorMessages.join(', ')}`);
          return;
        }
      }
      
      // 2. 提交出院医嘱前再次验证
      // 如果购物车中只有出院医嘱，则跳过阻塞医嘱的警告弹窗
      const skipBlockedWarning = otherOrders.length === 0;
      const dischargeResult = await submitDischargeOrdersWithValidation(dischargeOrders, skipBlockedWarning);
      
      if (dischargeResult.success) {
        ElMessage.success(`✅ 所有医嘱提交成功`);
        orderCart.value = [];
        expandedOrders.value = [];
      } else {
        // 部分成功的情况
        if (dischargeResult.partialSuccess) {
          ElMessage.warning(dischargeResult.message);
          // 只移除成功提交的医嘱
          orderCart.value = orderCart.value.filter(o => o.orderType === 'DischargeOrder');
        } else {
          ElMessage.error(dischargeResult.message);
        }
      }
      
      submitting.value = false;
      return;
    }

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
        PatientId: selectedPatient.value?.id,
        DoctorId: currentUser.value.staffId,
        Orders: inspectionOrders.map(order => {
          const orderData = {
            ItemCode: order.itemCode,
            ItemName: order.itemName  // 添加项目名称
          };
          
          // 添加备注
          if (order.remarks) {
            orderData.Remarks = order.remarks;
          }
          
          return orderData;
        })
      };

      console.log('🔍 提交检查医嘱:', requestData);
      console.log('🔍 检查医嘱数据详情:', JSON.stringify(requestData, null, 2));
      
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
        console.error('❌ 检查医嘱提交详细错误:', error);
        console.error('❌ 错误响应:', error.response?.data);
        errorMessages.push(`检查医嘱提交异常: ${error.response?.data?.message || error.message}`);
        if (error.response?.data?.errors) {
          errorMessages.push(...Object.values(error.response.data.errors).flat());
        }
      }
    }

    // 🔪 提交手术医嘱
    if (surgicalOrders.length > 0) {
      const requestData = {
        patientId: selectedPatient.value?.id,
        doctorId: currentUser.value.staffId,
        orders: surgicalOrders.map(order => ({
          surgeryName: order.surgeryName,
          anesthesiaType: order.anesthesiaType,
          incisionSite: order.incisionSite,
          surgeonId: order.surgeonId,
          scheduleTime: toBeijingTimeISO(order.scheduleTime),
          requiredTalk: order.requiredTalk || [],
          requiredOperation: order.requiredOperation || [],
          items: order.items || [],
          remarks: order.remarks
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
        PatientId: selectedPatient.value?.id,
        DoctorId: currentUser.value.staffId,
        Orders: operationOrders.map(order => ({
          IsLongTerm: order.isLongTerm,
          PlantEndTime: toBeijingTimeISO(order.plantEndTime),
          Remarks: order.remarks || null,
          OpId: order.opId,
          OperationName: order.operationName,
          OperationSite: order.operationSite || null,
          Normal: order.normal,
          TimingStrategy: order.timingStrategy?.toUpperCase(),
          StartTime: order.startTime ? toBeijingTimeISO(order.startTime) : null,
          IntervalHours: order.intervalHours || null,
          IntervalDays: order.intervalDays || 1,
          SmartSlotsMask: order.smartSlotsMask || 0,
          RequiresPreparation: order.requiresPreparation || false,
          PreparationItems: order.preparationItems && order.preparationItems.length > 0 ? order.preparationItems : null,
          ExpectedDurationMinutes: order.expectedDurationMinutes || null,
          RequiresResult: order.requiresResult || false,
          ResultTemplate: order.resultTemplate || null
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
        console.error('操作医嘱提交详细错误:', error);
        console.error('错误响应:', error.response?.data);
        errorMessages.push(`操作医嘱提交异常: ${error.response?.data?.message || error.message}`);
        if (error.response?.data?.errors) {
          errorMessages.push(...Object.values(error.response.data.errors).flat());
        }
      }
    }

    // 🏠 提交出院医嘱
    if (dischargeOrders.length > 0) {
      const requestData = {
        PatientId: selectedPatient.value?.id,
        DoctorId: currentUser.value.staffId,
        Orders: dischargeOrders.map(order => ({
          DischargeType: order.dischargeType,
          DischargeTime: toBeijingTimeISO(order.dischargeTime),
          DischargeDiagnosis: order.dischargeDiagnosis,
          DischargeInstructions: order.dischargeInstructions || '',
          MedicationInstructions: order.medicationInstructions || '',
          RequiresFollowUp: order.requiresFollowUp,
          FollowUpDate: order.followUpDate ? toBeijingTimeISO(order.followUpDate) : null,
          Items: order.items && order.items.length > 0 ? order.items.map(item => ({
            DrugId: item.drugId,
            Dosage: item.dosage,
            Note: item.note || ''
          })) : null
        }))
      };

      console.log('🏠 提交出院医嘱:', requestData);
      
      try {
        const response = await batchCreateDischargeOrders(requestData);
        if (response.success) {
          successCount += dischargeOrders.length;
          results.push(`出院医嘱: ${dischargeOrders.length}条成功`);
        } else {
          errorMessages.push(`出院医嘱失败: ${response.message}`);
          if (response.errors) errorMessages.push(...response.errors);
        }
      } catch (error) {
        errorMessages.push(`出院医嘱提交异常: ${error.message}`);
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
          if (type === 'DischargeOrder' && dischargeOrders.length > 0) return false;
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

// 🔧 辅助函数：提交非出院医嘱
const submitNonDischargeOrders = async (medicationOrders, inspectionOrders, surgicalOrders, operationOrders) => {
  const results = [];
  let successCount = 0;
  const errorMessages = [];

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

  // 🔍 提交检查医嘱
  if (inspectionOrders.length > 0) {
    const requestData = {
      PatientId: selectedPatient.value?.id,
      DoctorId: currentUser.value.staffId,
      Orders: inspectionOrders.map(order => {
        const orderData = {
          ItemCode: order.itemCode,
          ItemName: order.itemName
        };
        if (order.remarks) {
          orderData.Remarks = order.remarks;
        }
        return orderData;
      })
    };

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
      errorMessages.push(`检查医嘱提交异常: ${error.response?.data?.message || error.message}`);
      if (error.response?.data?.errors) {
        errorMessages.push(...Object.values(error.response.data.errors).flat());
      }
    }
  }

  // 🔪 提交手术医嘱
  if (surgicalOrders.length > 0) {
    const requestData = {
      patientId: selectedPatient.value?.id,
      doctorId: currentUser.value.staffId,
      orders: surgicalOrders.map(order => ({
        surgeryName: order.surgeryName,
        anesthesiaType: order.anesthesiaType,
        incisionSite: order.incisionSite,
        surgeonId: order.surgeonId,
        scheduleTime: toBeijingTimeISO(order.scheduleTime),
        requiredTalk: order.requiredTalk || [],
        requiredOperation: order.requiredOperation || [],
        items: order.items || [],
        remarks: order.remarks
      }))
    };

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
      PatientId: selectedPatient.value?.id,
      DoctorId: currentUser.value.staffId,
      Orders: operationOrders.map(order => ({
        IsLongTerm: order.isLongTerm,
        PlantEndTime: toBeijingTimeISO(order.plantEndTime),
        Remarks: order.remarks || null,
        OpId: order.opId,
        OperationName: order.operationName,
        OperationSite: order.operationSite || null,
        Normal: order.normal,
        TimingStrategy: order.timingStrategy?.toUpperCase(),
        StartTime: order.startTime ? toBeijingTimeISO(order.startTime) : null,
        IntervalHours: order.intervalHours || null,
        IntervalDays: order.intervalDays || 1,
        SmartSlotsMask: order.smartSlotsMask || 0,
        RequiresPreparation: order.requiresPreparation || false,
        PreparationItems: order.preparationItems && order.preparationItems.length > 0 ? order.preparationItems : null,
        ExpectedDurationMinutes: order.expectedDurationMinutes || null,
        RequiresResult: order.requiresResult || false,
        ResultTemplate: order.resultTemplate || null
      }))
    };

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
      errorMessages.push(`操作医嘱提交异常: ${error.response?.data?.message || error.message}`);
      if (error.response?.data?.errors) {
        errorMessages.push(...Object.values(error.response.data.errors).flat());
      }
    }
  }

  // 返回结果
  if (errorMessages.length === 0) {
    ElMessage.success(`✅ 成功提交其他医嘱 ${successCount} 条`);
    return { success: true, successCount, results };
  } else {
    return { success: false, errorMessages };
  }
};

// 🔧 辅助函数：验证并提交出院医嘱
const submitDischargeOrdersWithValidation = async (dischargeOrders, skipBlockedWarning = false) => {
  const loading = ElLoading.service({
    lock: true,
    text: '正在验证出院条件并提交...',
    background: 'rgba(0, 0, 0, 0.7)'
  });

  try {
    // 1. 对每个出院医嘱进行二次验证
    for (const order of dischargeOrders) {
      const validationResult = await validateDischargeOrderCreation(order.patientId);
      
      const dischargeTime = new Date(order.dischargeTime);
      const earliestTime = validationResult.earliestDischargeTime ? new Date(validationResult.earliestDischargeTime) : null;
      
      // 检查时间是否符合要求
      if (earliestTime && dischargeTime < earliestTime) {
        loading.close();
        
        // 构建时间不符合的错误提示（复用原有样式）
        let errorHtml = '<div style="text-align: left;">';
        errorHtml += '<h3 style="color: #f56c6c; margin-bottom: 15px;">❌ 出院条件已变化</h3>';
        
        errorHtml += '<div style="padding: 12px; background: #fef0f0; border-left: 4px solid #f56c6c; border-radius: 4px;">';
        errorHtml += '<div style="font-weight: bold; color: #f56c6c; margin-bottom: 8px;">⏰ 时间冲突：</div>';
        errorHtml += '<div style="font-size: 13px; line-height: 1.8;">';
        errorHtml += `<div>• 您之前设置的出院时间：<strong>${formatDateTime(dischargeTime)}</strong></div>`;
        errorHtml += `<div>• 当前最早允许出院时间：<strong style="color: #f56c6c;">${formatDateTime(earliestTime)}</strong></div>`;
        errorHtml += '<div style="margin-top: 10px; padding: 10px; background: white; border-radius: 4px; color: #f56c6c;">';
        errorHtml += '❌ 在提交其他医嘱后，出院条件已发生变化，出院时间不再符合要求！';
        errorHtml += '</div></div></div>';
        
        if (validationResult.blockedOrders && validationResult.blockedOrders.length > 0) {
          errorHtml += '<div style="margin-top: 20px;"><strong style="color: #e6a23c;">存在 ' + validationResult.blockedOrders.length + ' 条阻塞医嘱：</strong></div>';
          errorHtml += '<ul style="margin: 5px 0; padding-left: 20px; max-height: 150px; overflow-y: auto;">';
          validationResult.blockedOrders.forEach(blockedOrder => {
            const startTime = blockedOrder.startTime ? formatDateTime(new Date(blockedOrder.startTime)) : '未知';
            const endTime = blockedOrder.endTime ? formatDateTime(new Date(blockedOrder.endTime)) : '未知';
            errorHtml += `<li style="margin-bottom: 8px; font-size: 13px;">
              <div><strong>${blockedOrder.summary}</strong></div>
              <div style="color: #909399; font-size: 12px;">状态: ${blockedOrder.statusDisplay}</div>
              <div style="color: #909399; font-size: 12px;">时间: ${startTime} - ${endTime}</div>
            </li>`;
          });
          errorHtml += '</ul>';
        }
        
        errorHtml += '<div style="margin-top: 20px; padding: 12px; background: #f0f9ff; border-left: 4px solid #409eff; border-radius: 4px;">';
        errorHtml += '<div style="font-size: 13px; line-height: 1.6;">';
        errorHtml += '<strong style="color: #409eff;">💡 处理建议：</strong><br>';
        errorHtml += `1. 返回出院医嘱页面，重新设置出院时间不早于 <strong>${formatDateTime(earliestTime)}</strong><br>`;
        errorHtml += '2. 或先完成所有未完成的任务';
        errorHtml += '</div></div>';
        errorHtml += '</div>';
        
        ElMessageBox.alert(errorHtml, '出院医嘱验证失败', {
          dangerouslyUseHTMLString: true,
          confirmButtonText: '我知道了',
          type: 'error',
          customClass: 'discharge-validation-dialog'
        });
        
        return { success: false, message: '出院条件验证失败', partialSuccess: true };
      }
      
      // 检查是否有阻塞医嘱（警告但允许继续）
      // 如果 skipBlockedWarning 为 true，则跳过警告直接提交
      if (!skipBlockedWarning && validationResult.blockedOrders && validationResult.blockedOrders.length > 0) {
        loading.close();
        
        let warningHtml = '<div style="text-align: left;">';
        warningHtml += '<h3 style="color: #e6a23c; margin-bottom: 15px;">⚠️ 存在阻塞医嘱警告</h3>';
        warningHtml += '<div style="padding: 12px; background: #fdf6ec; border-left: 4px solid #e6a23c; border-radius: 4px; margin-bottom: 15px;">';
        warningHtml += '<div style="font-size: 13px; line-height: 1.6; color: #606266;">';
        warningHtml += '检测到患者当前仍有未完成的医嘱或任务，虽然出院时间符合要求，但建议您确认是否继续提交出院医嘱。';
        warningHtml += '</div></div>';
        warningHtml += '<div style="margin-bottom: 15px;"><strong style="color: #f56c6c;">存在 ' + validationResult.blockedOrders.length + ' 条阻塞医嘱：</strong></div>';
        warningHtml += '<ul style="margin: 0; padding-left: 20px; max-height: 250px; overflow-y: auto; border: 1px solid #ebeef5; border-radius: 4px; padding: 10px; background: #fafafa;">';
        validationResult.blockedOrders.forEach(blockedOrder => {
          const startTime = blockedOrder.startTime ? formatDateTime(new Date(blockedOrder.startTime)) : '未知';
          const endTime = blockedOrder.endTime ? formatDateTime(new Date(blockedOrder.endTime)) : '未知';
          warningHtml += `<li style="margin-bottom: 12px; padding-bottom: 12px; border-bottom: 1px solid #ebeef5;">
            <div style="margin-bottom: 4px;"><strong style="color: #303133;">${blockedOrder.summary}</strong></div>
            <div style="color: #909399; font-size: 12px; line-height: 1.5;">状态: ${blockedOrder.statusDisplay}</div>
            <div style="color: #909399; font-size: 12px; line-height: 1.5;">时间: ${startTime} - ${endTime}</div>
          </li>`;
        });
        warningHtml += '</ul>';
        warningHtml += '<div style="margin-top: 20px; padding: 12px; background: #fff3cd; border-left: 4px solid #e6a23c; border-radius: 4px;">';
        warningHtml += '<div style="font-size: 13px; line-height: 1.6; color: #856404;">';
        warningHtml += '<strong>⚠️ 温馨提示：</strong><br>';
        warningHtml += '• 出院时间符合要求，您可以继续提交出院医嘱<br>';
        warningHtml += '• 但建议先完成上述医嘱和任务，以确保患者安全出院';
        warningHtml += '</div></div>';
        warningHtml += '</div>';
        
        try {
          await ElMessageBox.confirm(warningHtml, '阻塞医嘱确认', {
            dangerouslyUseHTMLString: true,
            confirmButtonText: '我已确认，继续提交',
            cancelButtonText: '取消',
            type: 'warning',
            customClass: 'discharge-validation-dialog',
            distinguishCancelAndClose: true,
            closeOnClickModal: false
          });
        } catch {
          return { success: false, message: '用户取消提交', partialSuccess: true };
        }
        
        // 重新显示loading（关闭旧的，创建新的）
        loading.close();
        const newLoading = ElLoading.service({
          lock: true,
          text: '正在提交出院医嘱...',
          background: 'rgba(0, 0, 0, 0.7)'
        });
        
        // 继续提交流程，最后关闭newLoading
        try {
          const requestData = {
            PatientId: selectedPatient.value?.id,
            DoctorId: currentUser.value.staffId,
            Orders: dischargeOrders.map(order => ({
              DischargeType: order.dischargeType,
              DischargeTime: toBeijingTimeISO(order.dischargeTime),
              DischargeDiagnosis: order.dischargeDiagnosis,
              DischargeInstructions: order.dischargeInstructions || '',
              MedicationInstructions: order.medicationInstructions || '',
              RequiresFollowUp: order.requiresFollowUp,
              FollowUpDate: order.followUpDate ? toBeijingTimeISO(order.followUpDate) : null,
              Items: order.items && order.items.length > 0 ? order.items.map(item => ({
                DrugId: item.drugId,
                Dosage: item.dosage,
                Note: item.note || ''
              })) : null
            }))
          };

          const response = await batchCreateDischargeOrders(requestData);
          newLoading.close();
          
          if (response.success) {
            return { success: true };
          } else {
            ElMessage.error(`出院医嘱提交失败: ${response.message}`);
            return { success: false, message: response.message };
          }
        } catch (error) {
          newLoading.close();
          console.error('出院医嘱提交异常:', error);
          ElMessage.error(`出院医嘱提交异常: ${error.response?.data?.message || error.message}`);
          return { success: false, message: error.response?.data?.message || error.message };
        }
      }
    }
    
    // 2. 验证通过且没有阻塞医嘱，直接提交出院医嘱
    const requestData = {
      PatientId: selectedPatient.value?.id,
      DoctorId: currentUser.value.staffId,
      Orders: dischargeOrders.map(order => ({
        DischargeType: order.dischargeType,
        DischargeTime: toBeijingTimeISO(order.dischargeTime),
        DischargeDiagnosis: order.dischargeDiagnosis,
        DischargeInstructions: order.dischargeInstructions || '',
        MedicationInstructions: order.medicationInstructions || '',
        RequiresFollowUp: order.requiresFollowUp,
        FollowUpDate: order.followUpDate ? toBeijingTimeISO(order.followUpDate) : null,
        Items: order.items && order.items.length > 0 ? order.items.map(item => ({
          DrugId: item.drugId,
          Dosage: item.dosage,
          Note: item.note || ''
        })) : null
      }))
    };

    const response = await batchCreateDischargeOrders(requestData);
    loading.close();
    
    if (response.success) {
      return { success: true };
    } else {
      ElMessage.error(`出院医嘱提交失败: ${response.message}`);
      return { success: false, message: response.message };
    }
  } catch (error) {
    loading.close();
    console.error('出院医嘱提交异常:', error);
    ElMessage.error(`出院医嘱提交异常: ${error.response?.data?.message || error.message}`);
    return { success: false, message: error.response?.data?.message || error.message };
  }
};

// 辅助函数
const disablePastDates = (time) => {
  return time.getTime() < Date.now() - 24 * 60 * 60 * 1000;
};

// 禁用早于开始时间的日期（用于长期医嘱的结束时间选择）
const disableEndDateBeforeStart = (time) => {
  // 首先禁用过去的日期
  if (time.getTime() < Date.now() - 24 * 60 * 60 * 1000) {
    return true;
  }
  
  // 获取开始时间
  let startTimeValue = null;
  if (activeType.value === 'MedicationOrder') {
    startTimeValue = currentOrder.startTime;
  } else if (activeType.value === 'OperationOrder') {
    startTimeValue = operationOrder.startTime;
  }
  
  if (!startTimeValue) return false;
  
  const startTime = new Date(startTimeValue);
  // 将时间部分清零，只比较日期
  const startDate = new Date(startTime.getFullYear(), startTime.getMonth(), startTime.getDate());
  const selectedDate = new Date(time.getFullYear(), time.getMonth(), time.getDate());
  
  // 禁用早于开始日期的所有日期
  return selectedDate.getTime() < startDate.getTime();
};

// 随访日期不能早于出院时间
const disableFollowUpPastDates = (time) => {
  if (dischargeOrder.dischargeTime) {
    const dischargeDate = new Date(dischargeOrder.dischargeTime);
    return time.getTime() < dischargeDate.getTime();
  }
  return time.getTime() < Date.now();
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
  // 支持药品医嘱和操作医嘱
  let startTimeValue = null;
  if (activeType.value === 'MedicationOrder') {
    startTimeValue = currentOrder.startTime;
  } else if (activeType.value === 'OperationOrder') {
    startTimeValue = operationOrder.startTime;
  }
  
  if (!startTimeValue) return {};
  
  const startTime = new Date(startTimeValue);
  const selectedDate = new Date(date);
  
  // 将日期部分标准化为零点时间进行比较
  const startDateOnly = new Date(startTime.getFullYear(), startTime.getMonth(), startTime.getDate());
  const selectedDateOnly = new Date(selectedDate.getFullYear(), selectedDate.getMonth(), selectedDate.getDate());
  
  // 如果选择的日期早于开始日期，理论上不应该到这里（因为日期已被禁用）
  if (selectedDateOnly.getTime() < startDateOnly.getTime()) {
    // 禁用所有时间
    return {
      disabledHours: () => Array.from({ length: 24 }, (_, i) => i),
      disabledMinutes: () => Array.from({ length: 60 }, (_, i) => i)
    };
  }
  
  // 如果选择的日期与开始日期是同一天，禁用开始时间之前的时间
  if (selectedDateOnly.getTime() === startDateOnly.getTime()) {
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
  
  // 如果选择的日期晚于开始日期，不禁用任何时间
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

const getDrugUnit = (id) => {
  const drug = drugDict.value.find(d => d.id === id);
  return drug?.PriceUnit || drug?.priceUnit || '';
};

const formatDateTime = (datetime) => {
  if (!datetime) return '';
  try {
    let date;

    // 已经是 Date 对象，直接使用本地时间
    if (datetime instanceof Date) {
      date = datetime;
    } else if (typeof datetime === 'string') {
      // 字符串里已经带有时区信息（如 Z 或 +08:00），交给 Date 自己解析
      if (datetime.includes('Z') || datetime.includes('+')) {
        date = new Date(datetime);
      } else {
        // 表单里选出来的本地时间（如 2025-12-19T08:00:00），按本地时间解析，不再额外加 8 小时
        // 同时兼容空格分隔的形式（如 2025-12-19 08:00:00）
        const normalized = datetime.replace(' ', 'T');
        date = new Date(normalized);
      }
    } else {
      // 兜底：其它类型直接交给 Date 处理
      date = new Date(datetime);
    }

    if (isNaN(date.getTime())) {
      return datetime;
    }

    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    const hours = String(date.getHours()).padStart(2, '0');
    const minutes = String(date.getMinutes()).padStart(2, '0');
    return `${year}-${month}-${day} ${hours}:${minutes}`;
  } catch {
    return datetime;
  }
};

const formatDate = (datetime) => {
  if (!datetime) return '';
  const date = new Date(datetime);
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
};

const getOrderTagType = (order) => {
  if (order.orderType === 'SurgicalOrder') {
    return order.surgeryType === 'Emergency' ? 'danger' : 'success';
  }
  return order.isLongTerm ? 'primary' : 'warning';
};

const getOrderTypeLabel = (order) => {
  if (order.orderType === 'SurgicalOrder') {
    return '手术';
  } else if (order.orderType === 'InspectionOrder') {
    return '检查';
  } else if (order.orderType === 'OperationOrder') {
    return '操作';
  }
  return order.isLongTerm ? '长期' : '临时';
};

// 获取默认手术药品
const getDefaultSurgicalItems = () => {
  return [
    { drugId: '', dosage: '', note: '' }
  ];
};

// 初始化手术药品（空白行）
const initDefaultSurgicalDrugs = () => {
  surgicalOrder.items = getDefaultSurgicalItems();
};

// 加载医生列表
const loadDoctorList = async () => {
  try {
    // 获取当前医生的科室代码
    const deptCode = currentUser.value?.deptCode;
    
    if (!deptCode) {
      ElMessage.warning('未获取到科室信息，无法加载医生列表');
      doctorList.value = [];
      return;
    }
    
    // 调用API获取本科室的医生列表
    const response = await queryStaffList({
      role: 'Doctor',
      deptCode: deptCode,
      pageNumber: 1,
      pageSize: 100  // 获取足够多的医生，一般一个科室不会有超过100个医生
    });
    
    // 映射数据格式
    doctorList.value = response.staffList
      .filter(doctor => doctor.isActive)  // 只显示启用的医生
      .map(doctor => ({
        staffId: doctor.staffId,
        name: doctor.fullName,
        title: doctor.title || ''  // 使用后端返回的title字段
      }));
    
    console.log('医生列表加载成功:', doctorList.value.length, '科室:', deptCode);
    
    // 自动选择当前登录医生（如果存在）
    if (currentUser.value?.staffId) {
      const currentDoctor = doctorList.value.find(d => d.staffId === currentUser.value.staffId);
      if (currentDoctor) {
        surgicalOrder.surgeonId = currentUser.value.staffId;
      }
    }
  } catch (error) {
    console.error('加载医生列表失败:', error);
    ElMessage.error('加载医生列表失败: ' + (error.response?.data?.message || error.message));
    doctorList.value = [];
  }
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
    
    // 不再默认选择患者，让医生手动选择
    // if (patients.length > 0 && !selectedPatient.value) {
    //   selectedPatient.value = patients[0];
    // }
    
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
    1: '口服',
    2: '外用/涂抹',
    10: '肌内注射',
    11: '皮下注射',
    12: '静脉推注',
    20: '静脉滴注',
    30: '皮试'
  };
  return routes[routeId] || routeId;
};

// getFreqDescription 已移除，改用 getStrategyLabel

const getOrderSummary = (order) => {
  // 判断医嘱类型
  if (order.orderType === 'SurgicalOrder') {
    // 手术医嘱摘要
    return order.surgeryName;
  } 
  else if (order.orderType === 'InspectionOrder') {
    return order.itemName || order.itemCode || '检查';
  } 
  else if (order.orderType === 'OperationOrder') {

    // 操作医嘱摘要
    const strategyLabel = getStrategyLabel(order.imingStrategy);
    return `${order.operationName || order.opId} (${strategyLabel})`;
  } 
  else if (order.orderType === 'DischargeOrder') {
    // 出院医嘱摘要
    const typeNames = {
      1: '治愈出院',
      2: '好转出院',
      99: '其他出院'
    };
    return typeNames[order.dischargeType] || '出院';
  } 
  else {
    // 药品医嘱摘要
    const drugNames = order.items.map(i => getDrugName(i.drugId)).join('+');
    const strategyLabel = getStrategyLabel(order.timingStrategy);
    return `${drugNames} (${strategyLabel})`;
  }
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
    
    // 加载医生列表
    await loadDoctorList();
    
    // 初始化手术常用药品（在药品字典加载完成后）
    initDefaultSurgicalDrugs();
    
    ElMessage.success('基础数据加载完成');
  } catch (error) {
    console.error('加载基础数据失败:', error);
    ElMessage.error('加载基础数据失败，部分功能可能不可用');
  }
});

// ==================== 护理等级相关方法 ====================

// 护理等级文本
const getGradeText = (grade) => {
  const gradeMap = {
    0: '特级护理',
    1: '一级护理',
    2: '二级护理',
    3: '三级护理'
  };
  return gradeMap[grade] || '未知';
};

// 护理等级标签类型
const getGradeTagType = (grade) => {
  const typeMap = {
    0: 'danger',   // 特级-红色
    1: 'warning',  // 一级-橙色
    2: 'success',  // 二级-绿色
    3: 'info'      // 三级-灰色
  };
  return typeMap[grade] || 'info';
};

// 护理等级表单验证
const isNursingGradeFormValid = computed(() => {
  return nursingGradeForm.newGrade !== null && 
         selectedPatient.value &&
         nursingGradeForm.newGrade !== selectedPatient.value.nursingGrade;
});

// 重置护理等级表单
const resetNursingGradeForm = () => {
  nursingGradeForm.newGrade = selectedPatient.value?.nursingGrade || null;
  nursingGradeForm.reason = '';
};

// 提交护理等级修改
const submitNursingGrade = async () => {
  if (!selectedPatient.value) {
    ElMessage.warning('请先选择患者');
    return;
  }

  if (nursingGradeForm.newGrade === null) {
    ElMessage.warning('请选择新的护理等级');
    return;
  }

  if (nursingGradeForm.newGrade === selectedPatient.value.nursingGrade) {
    ElMessage.warning('新护理等级与当前等级相同');
    return;
  }

  try {
    submitting.value = true;
    
    console.log('提交护理等级修改:', {
      patientId: selectedPatient.value.id,
      newGrade: nursingGradeForm.newGrade,
      doctorId: currentUser.value.staffId
    });
    
    // 调用API更新护理等级
    const response = await fetch(`/api/patient/${selectedPatient.value.id}/nursing-grade`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        newGrade: nursingGradeForm.newGrade,
        reason: nursingGradeForm.reason || '',
        doctorId: currentUser.value.staffId
      })
    });

    console.log('API响应状态:', response.status);

    if (!response.ok) {
      let errorMessage = '更新失败';
      try {
        const error = await response.json();
        errorMessage = error.message || errorMessage;
      } catch (e) {
        const errorText = await response.text();
        console.error('API错误响应:', errorText);
        errorMessage = `服务器错误 (${response.status})`;
      }
      throw new Error(errorMessage);
    }

    const result = await response.json();
    console.log('更新成功:', result);
    
    ElMessage.success(`护理等级已从 ${getGradeText(selectedPatient.value.nursingGrade)} 修改为 ${getGradeText(nursingGradeForm.newGrade)}`);
    
    // 更新本地患者数据
    selectedPatient.value.nursingGrade = nursingGradeForm.newGrade;
    const patientInList = patientList.value.find(p => p.id === selectedPatient.value.id);
    if (patientInList) {
      patientInList.nursingGrade = nursingGradeForm.newGrade;
    }
    
    // 重置表单
    resetNursingGradeForm();
  } catch (error) {
    console.error('更新护理等级失败:', error);
    ElMessage.error('更新护理等级失败: ' + error.message);
  } finally {
    submitting.value = false;
  }
};

// 监听选中患者变化，重置护理等级表单
watch(selectedPatient, (newPatient) => {
  if (newPatient && activeType.value === 'NursingGrade') {
    resetNursingGradeForm();
  }
});

// 监听Tab切换，初始化护理等级表单
watch(activeType, (newType) => {
  if (newType === 'NursingGrade' && selectedPatient.value) {
    resetNursingGradeForm();
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

/* ==================== 主布局 ==================== */
.order-layout {
  padding: 20px;
  background: var(--bg-page);
  min-height: 100%;
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

/* 准备物品容器样式 */
.preparation-items-container {
  width: 100%;
}

.preparation-input-row {
  display: flex;
  gap: 8px;
  margin-bottom: 10px;
}

.preparation-tags {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  margin-top: 10px;
}

.preparation-tags .el-tag {
  margin: 0;
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

/* 手术必备物列表样式 */
.surgical-items-list {
  margin-bottom: 15px;
}

.item-row {
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

/* 自定义多选框样式 */
.custom-multi-select {
  width: 100%;
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.checkbox-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 10px;
  padding: 12px;
  background: var(--bg-secondary);
  border-radius: var(--radius-small);
}

.checkbox-grid :deep(.el-checkbox) {
  margin-right: 0;
}

.custom-input-row {
  display: flex;
  gap: 8px;
  align-items: center;
}

.custom-tags {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  padding: 8px 12px;
  background: var(--bg-secondary);
  border-radius: var(--radius-small);
  min-height: 36px;
}

.custom-tags .el-tag {
  margin: 0;
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
  padding: 4px 8px;
  border-radius: 4px;
  font-size: 0.75rem;
  font-weight: bold;
  display: inline-flex;
  align-items: center;
  margin-bottom: 8px;
  width: 170px;
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

/* ==================== 护理等级表单样式 ==================== */
.nursing-grade-form {
  padding: 20px;
}

.current-grade-display {
  background: linear-gradient(135deg, #f5f7fa 0%, #e8eef5 100%);
  border-radius: var(--radius-medium);
  padding: 20px;
  margin-bottom: 30px;
  border-left: 4px solid var(--primary-color);
}

.info-row {
  display: flex;
  align-items: center;
  gap: 15px;
  font-size: 1.1rem;
}

.info-row .label {
  font-weight: 600;
  color: var(--text-primary);
}

.nursing-grade-form .el-radio-group {
  display: flex;
  gap: 12px;
  flex-wrap: wrap;
}

.nursing-grade-form .el-radio-button {
  flex: 1;
  min-width: 140px;
}

.nursing-grade-form .form-actions {
  display: flex;
  justify-content: flex-end;
  gap: 15px;
  padding-top: 20px;
  border-top: 1px solid var(--border-color);
}

/* ==================== 响应式调整 ==================== */
@media (max-width: 1600px) {
  .patient-panel:not(.collapsed) {
    width: 250px;
  }
  
  .cart-panel:not(.collapsed) {
    width: 280px;
  }
}

@media (max-width: 1400px) {
  .patient-panel:not(.collapsed) {
    width: 250px;
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

/* 多选框全展示样式 */
.multi-select-full :deep(.el-select__tags) {
  flex-wrap: nowrap !important;
  overflow: hidden;
}

.multi-select-full :deep(.el-tag) {
  max-width: none !important;
  flex-shrink: 0;
}

.multi-select-full :deep(.el-select__tags-text) {
  white-space: nowrap;
}

/* ==================== 检查医嘱表单样式 ==================== */
.inspection-form {
  padding: 20px;
}

.inspection-group {
  margin-bottom: 20px;
  padding: 15px;
  background: #f8f9fa;
  border-radius: 8px;
  border: 1px solid #e9ecef;
}

.group-title {
  font-size: 0.95rem;
  font-weight: 600;
  color: #495057;
  margin-bottom: 12px;
  padding-bottom: 8px;
  border-bottom: 2px solid #dee2e6;
}

.inspection-group :deep(.el-checkbox-group) {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(180px, 1fr));
  gap: 10px;
}

.inspection-group :deep(.el-checkbox) {
  margin: 0;
  padding: 8px 12px;
  background: white;
  border: 1px solid #e1e4e8;
  border-radius: 6px;
  transition: all 0.2s;
}

.inspection-group :deep(.el-checkbox:hover) {
  background: #f0f7ff;
  border-color: #409eff;
}

.inspection-group :deep(.el-checkbox.is-checked) {
  background: linear-gradient(135deg, #e8f4ff 0%, #f0f8ff 100%);
  border-color: #409eff;
  box-shadow: 0 2px 4px rgba(64, 158, 255, 0.1);
}

.inspection-group :deep(.el-checkbox__label) {
  font-size: 0.9rem;
  color: #495057;
  padding-left: 8px;
}

.section-tip {
  font-size: 0.85rem;
  color: #6c757d;
  font-weight: normal;
  margin-left: 8px;
}

.form-row :deep(.el-radio-group) {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
}

.form-row :deep(.el-radio) {
  margin: 0;
  padding: 8px 16px;
  background: white;
  border: 1px solid #e1e4e8;
  border-radius: 6px;
  transition: all 0.2s;
}

.form-row :deep(.el-radio:hover) {
  background: #f0f7ff;
  border-color: #409eff;
}

.form-row :deep(.el-radio.is-checked) {
  background: linear-gradient(135deg, #e8f4ff 0%, #f0f8ff 100%);
  border-color: #409eff;
}

.form-row :deep(.el-radio-button__inner) {
  border-radius: 6px !important;
  padding: 10px 20px;
  font-weight: 500;
}

.form-row :deep(.el-checkbox-group .el-checkbox) {
  background: white;
  border: 1px solid #e1e4e8;
  border-radius: 6px;
  padding: 8px 12px;
  margin-right: 10px;
  margin-bottom: 10px;
  transition: all 0.2s;
}

.form-row :deep(.el-checkbox-group .el-checkbox:hover) {
  background: #f0f7ff;
  border-color: #409eff;
}

.form-row :deep(.el-checkbox-group .el-checkbox.is-checked) {
  background: linear-gradient(135deg, #e8f4ff 0%, #f0f8ff 100%);
  border-color: #409eff;
}

/* ==================== 出院验证弹窗样式 ==================== */
:deep(.discharge-validation-dialog) {
  max-width: 650px;
}

:deep(.discharge-validation-dialog .el-message-box__message) {
  max-height: 500px;
  overflow-y: auto;
}

:deep(.discharge-validation-dialog ul) {
  list-style-type: none;
}

:deep(.discharge-validation-dialog ul li) {
  border-bottom: 1px solid #ebeef5;
  padding-bottom: 8px;
}

:deep(.discharge-validation-dialog ul li:last-child) {
  border-bottom: none;
}

/* ==================== 响应式调整 ==================== */
@media (max-width: 1400px) {
  .main-content {
    grid-template-columns: 1fr 340px;
  }
  
  .inspection-group :deep(.el-checkbox-group) {
    grid-template-columns: repeat(auto-fill, minmax(150px, 1fr));
  }
}

@media (max-width: 1200px) {
  .main-content {
    grid-template-columns: 1fr;
  }
  
  .cart-area {
    max-height: 500px;
  }
  
  .inspection-group :deep(.el-checkbox-group) {
    grid-template-columns: repeat(auto-fill, minmax(140px, 1fr));
  }
}
</style>