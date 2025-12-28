<template>
  <div class="task-scan">
    <div class="page-header">
      <h2>任务扫码执行</h2>
    </div>

    <div class="scan-container">
      <!-- 左侧：扫码面板 -->
      <div class="scan-panel">
        <!-- 操作栏：历史标签 -->
        <div class="operation-bar">
          <div class="history-tabs">
            <div 
              :class="['tab', { active: !showHistory }]"
              @click="showHistory = false"
            >
              执行操作
            </div>
            <div 
              :class="['tab', { active: showHistory }]"
              @click="showHistory = true"
            >
              📝 执行历史 ({{ history.length }})
            </div>
          </div>
        </div>

        <div class="step-content">
          <!-- 显示执行操作 -->
          <div v-if="!showHistory">
            <!-- 步骤指示器 - 仅在执行操作中显示 -->
            <div class="step-indicator">
              <div v-for="(s, i) in ['上传任务码', '上传患者/药品码', '完成任务']" :key="i" class="step">
                <div :class="['step-circle', { active: currentStep === i, completed: currentStep > i }]">{{ i + 1 }}</div>
                <div class="step-label">{{ s }}</div>
              </div>
            </div>

            <!-- 步骤1：任务条形码 -->
            <div v-if="currentStep === 0">
            <h3>📷 上传任务条形码</h3>
            <p class="step-desc">请拍摄或上传任务条形码</p>
            
            <div class="upload-box">
              <input ref="taskInput" type="file" accept="image/*" @change="handleTaskUpload" style="display:none" />
              <div class="upload-area" @click="$refs.taskInput?.click()">
                <div style="font-size: 3rem">📷</div>
                <div>点击上传或拍摄</div>
                <small>支持 JPG、PNG、BMP</small>
              </div>
              <img v-if="taskPreview" :src="taskPreview" class="preview" />
            </div>

            <div v-if="currentTask" class="task-info">
              <h4>任务信息</h4>
              <p><strong>患者:</strong> {{ currentTask.patientName }}</p>
              <p><strong>类型:</strong> {{ getCategoryName(currentTask.category) }}</p>
              <p><strong>时间:</strong> {{ formatTime(currentTask.plannedStartTime) }}</p>
            </div>
          </div>

          <!-- 步骤2：患者或药品条形码 -->
          <div v-else-if="currentStep === 1">
            <h3>{{ currentTask.category === 5 ? '核对药品' : '👤 上传患者码' }}</h3>
            <p class="step-desc">{{ currentTask.category === 5 ? '逐个上传药品条形码' : '上传患者条形码验证匹配' }}</p>
            
            <div class="current-task">{{ currentTask.patientName }} - {{ getCategoryName(currentTask.category) }}</div>

            <!-- 药品清单（仅在核对药品时显示） -->
            <div v-if="currentTask.category === 5 && currentTask.drugs" class="drug-list-panel">
              <h4>📋 期望核对的药品清单</h4>
              <div class="drug-input-area">
                <el-input 
                  v-if="currentTask.category === 5"
                  v-model="drugInputValue"
                  placeholder="输入药品代码或扫描药品条形码"
                  @keyup.enter="handleDrugInput"
                  clearable
                >
                  <template #append>
                    <el-button @click="handleDrugInput" type="primary">核对</el-button>
                  </template>
                </el-input>
              </div>
              <div class="drug-list">
                <div v-for="(drug, idx) in currentTask.drugs" :key="idx" :class="['drug-item', getDrugStatus(drug)]" @click="toggleDrugStatus(drug)">
                  <div class="drug-status-icon">
                    <span v-if="drug.scanned" class="scanned-icon">✓</span>
                    <span v-else class="unscanned-icon">○</span>
                  </div>
                  <div class="drug-info">
                    <div class="drug-name">{{ drug.drugName || drug.drugId }}</div>
                    <div class="drug-id">{{ drug.drugId }}</div>
                  </div>
                </div>
              </div>
            </div>

            <div class="upload-box">
              <input ref="secondInput" type="file" accept="image/*" @change="handleSecondUpload" style="display:none" />
              <div class="upload-area" @click="$refs.secondInput?.click()">
                <div style="font-size: 3rem">📷</div>
                <div>点击上传或拍摄</div>
              </div>
              <img v-if="secondPreview" :src="secondPreview" class="preview" />
            </div>

            <!-- 进度条与统计 -->
            <div v-if="currentTask.category === 5" class="progress">
              <div class="progress-stats">
                <span>已核对: <strong class="count-scanned">{{ confirmedCount }}</strong></span>
                <span class="separator">/</span>
                <span>期望: <strong class="count-total">{{ totalCount }}</strong></span>
                <span v-if="totalCount === 0" class="warning-note">（未能读取清单）</span>
              </div>
              <el-progress 
                :percentage="totalCount > 0 ? Math.round((confirmedCount / totalCount) * 100) : 0" 
                :color="getProgressColor(confirmedCount, totalCount)"
              />
            </div>

            <!-- 消息提示（成功、警告、错误） -->
            <div v-if="message" :class="['msg', message.type]">
              <span v-if="message.type === 'error'" class="msg-icon">⚠️</span>
              <span v-else-if="message.type === 'success'" class="msg-icon">✓</span>
              <span v-else-if="message.type === 'warning'" class="msg-icon">ℹ️</span>
              {{ message.text }}
            </div>
          </div>

          <!-- 步骤3：结束任务 (仅类别2) -->
          <div v-else-if="currentStep === 2">
            <h3>✓ 完成任务确认</h3>
            <p class="step-desc">确认任务信息后点击完成</p>
            
            <div class="task-confirm">
              <h4>任务确认信息</h4>
              <p><strong>患者:</strong> {{ currentTask.patientName }}</p>
              <p><strong>任务类型:</strong> {{ getCategoryName(currentTask.category) }}</p>
              <p><strong>计划时间:</strong> {{ formatTime(currentTask.plannedStartTime) }}</p>
            </div>

            <div class="remark-box">
              <label for="remark">备注（可选）：</label>
              <el-input
                id="remark"
                v-model="remarks"
                type="textarea"
                placeholder="请输入执行过程中的备注信息"
                :rows="4"
              />
            </div>

            <div v-if="message" :class="['msg', message.type]">{{ message.text }}</div>
            </div>
          </div>

          <!-- 执行历史标签页 -->
          <div v-else class="history-content">
            <h3 style="margin-top: 0">📝 执行历史</h3>
            <div v-if="history.length > 0" class="history-list">
              <div v-for="t in history" :key="t.id" class="history-item">
                <div class="h-header">
                  <span class="task-id">#{{ t.id }}</span>
                  <span class="task-category">{{ t.categoryName }}</span>
                  <span class="status">✓ 已完成</span>
                </div>
                <div class="h-patient">
                  <span class="label">患者:</span>
                  <span class="value">{{ t.patientName }}</span>
                  <span v-if="t.bedId" class="bed-id">({{ t.bedId }}号床)</span>
                </div>
                <div class="h-executor">
                  <span class="label">执行人:</span>
                  <span class="value">{{ t.completedBy }}</span>
                </div>
                <div class="h-time">
                  <span class="label">完成时间:</span>
                  <span class="value">{{ formatHistoryTime(t.completedTime) }}</span>
                </div>
                <div v-if="t.remarks && t.remarks !== '无备注'" class="h-remarks">
                  <span class="label">备注:</span>
                  <span class="value">{{ t.remarks }}</span>
                </div>
              </div>
            </div>
            <div v-else class="empty">暂无记录</div>
            <div class="history-actions">
              <el-button @click="clearHistory" type="danger" text size="small">清空历史</el-button>
            </div>
          </div>
        </div>

        <!-- 操作按钮 -->
        <div class="action-btns">
          <el-button v-if="!showHistory && currentStep > 0" @click="goBack">← 返回</el-button>
          <el-button v-if="!showHistory && currentStep === 2" type="success" @click="finish">完成任务 ✓</el-button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, onBeforeUnmount, watch } from 'vue';
import { ElMessage } from 'element-plus';
import * as api from '../api/executionTask';
import { ElMessageBox } from 'element-plus';
import { useRoute } from 'vue-router';

// 获取当前登录用户信息
const getUserInfo = () => {
  try {
    const userInfoStr = localStorage.getItem('userInfo');
    if (userInfoStr) {
      return JSON.parse(userInfoStr);
    }
  } catch (error) {
    console.error('解析用户信息失败:', error);
  }
  return null;
};

const currentUser = ref(getUserInfo());

// 步骤和状态
const currentStep = ref(0);
const currentTask = ref(null);
const showHistory = ref(false);
const remarks = ref('');
const drugInputValue = ref(''); // 药品输入框

// 从 localStorage 加载执行历史
const loadHistoryFromStorage = () => {
  try {
    const stored = localStorage.getItem('taskScanHistory');
    return stored ? JSON.parse(stored) : [];
  } catch (error) {
    console.error('加载执行历史失败:', error);
    return [];
  }
};

// 直接初始化 history - 使用响应式的方式确保能正确更新
const history = ref(loadHistoryFromStorage());

// 文件输入和预览
const taskInput = ref(null);
const secondInput = ref(null);
const endInput = ref(null);

const taskPreview = ref('');
const secondPreview = ref('');
const endPreview = ref('');

// 文件对象
let taskFile = null;
let secondFile = null;
let endFile = null;

// 状态
const confirmedCount = ref(0);
const totalCount = ref(0);
const message = ref(null);

// 处理任务条形码上传
const handleTaskUpload = async (e) => {
  const file = e.target.files?.[0];
  if (!file) return;

  try {
    const msg = ElMessage.info({ message: '识别条形码中...', duration: 0 });
    taskFile = file;

    // 显示预览
    const reader = new FileReader();
    reader.onload = r => taskPreview.value = r.target?.result;
    reader.readAsDataURL(file);

    // 调用后端的条形码识别接口
    const result = await api.recognizeTaskBarcode(file);
    msg.close();
    
    // 后端识别失败或没有返回有效的taskId，提示用户手动输入
    if (!result || !result.taskId || result.taskId === 0) {
      // 使用promise调用，允许用户手动输入taskId
      const taskId = await ElMessageBox.prompt(
        '条形码自动识别失败，请手动输入任务ID',
        '输入任务ID',
        {
          confirmButtonText: '确定',
          cancelButtonText: '取消',
          inputType: 'number',
        }
      ).then(({ value }) => {
        return parseInt(value);
      }).catch(() => {
        ElMessage.info('已取消');
        return null;
      });
      
      if (!taskId) {
        taskPreview.value = '';
        taskFile = null;
        return;
      }
      
      // 使用手动输入的taskId获取任务详情
      const taskDetail = await api.getExecutionTaskDetail(taskId);
      
      // 检查任务状态是否已完成
      if (taskDetail.status === 'Completed' || taskDetail.status === 5) {
        ElMessage.error('该任务已完成，无法重复执行');
        taskPreview.value = '';
        taskFile = null;
        return;
      }
      
      currentTask.value = taskDetail;
      ElMessage.success('任务信息已加载（手动输入）');
    } else {
      // 使用识别出来的taskId获取任务详情
      const taskDetail = await api.getExecutionTaskDetail(result.taskId);
      
      // 检查任务状态是否已完成
      if (taskDetail.status === 'Completed' || taskDetail.status === 5) {
        ElMessage.error('该任务已完成，无法重复执行');
        taskPreview.value = '';
        taskFile = null;
        return;
      }
      
      currentTask.value = taskDetail;
      ElMessage.success('任务信息已加载（自动识别）');
    }
    
    if (currentTask.value.category === 5) {
      totalCount.value = currentTask.value.drugs?.length || 0;
      // 重置所有药品的 scanned 状态为 false（每次新任务都从头开始）
      if (currentTask.value.drugs) {
        currentTask.value.drugs.forEach(drug => {
          drug.scanned = false;
        });
      }
      confirmedCount.value = 0; // 重置已核对计数
      if (totalCount.value === 0) {
        message.value = { type: 'warning', text: '未能从任务中读取期望药品清单，扫码将仅记录条码' };
      }
    }
    
    // 任务加载成功后自动进入第2步
    setTimeout(() => nextStep(), 1000);
  } catch (err) {
    if (err.message !== '已取消') {
      ElMessage.error('处理条形码失败: ' + err.message);
    }
  }
};

// 处理患者/药品条形码上传
const handleSecondUpload = async (e) => {
  const file = e.target.files?.[0];
  if (!file) return;

  try {
    const msg = ElMessage.info({ message: '验证中...', duration: 0 });
    secondFile = file;

    const reader = new FileReader();
    reader.onload = r => secondPreview.value = r.target?.result;
    reader.readAsDataURL(file);

    // 调用后端验证API
    let result;
    if (currentTask.value.category === 5) {
      // 药品验证
      result = await api.validateDrugBarcodeImage(currentTask.value.id, taskFile, file);
    } else {
      // 患者验证
      result = await api.validatePatientBarcodeImage(currentTask.value.id, taskFile, file);
    }
    
    msg.close();

    if (result.isMatched) {
      if (currentTask.value.category === 5) {
        // 如果后端识别出了药品 ID，自动在前端打钩对应的药品
        if (result.scannedDrugId) {
          const targetDrug = currentTask.value.drugs?.find(d => d.drugId === result.scannedDrugId);
          if (targetDrug && !targetDrug.scanned) {
            targetDrug.scanned = true;
            confirmedCount.value++;
          }
        }
        
        // 从后端结果更新已确认数和总数（后端返回 scannedCount/expectedCount/progress）
        if (typeof result.scannedCount === 'number') {
          confirmedCount.value = result.scannedCount;
        }

        if (typeof result.expectedCount === 'number') {
          totalCount.value = result.expectedCount;
        }

        message.value = { type: 'success', text: `药品已核对 （${confirmedCount.value}/${totalCount.value}）` };

        // 如果后端返回 progress 并且完成
        const progress = typeof result.progress === 'number' ? result.progress : (totalCount.value > 0 ? Math.round((confirmedCount.value / totalCount.value) * 100) : 0);
        if (progress >= 100 && totalCount.value > 0) {
          ElMessage.success('所有药品已核对');
          setTimeout(() => nextStep(), 1500);
        } else {
          // 保持在当前步骤，清除预览便于下一次扫描
          secondPreview.value = '';
        }
      } else {
        // 患者验证成功
        message.value = { type: 'success', text: '✓ 患者验证成功' };
        ElMessage.success('进入完成步骤');
        setTimeout(() => nextStep(), 1500);
      }
    } else {
      // 显示后端返回的详细错误（例如扫描条码不在期望清单）
      const txt = result && result.message ? result.message : '条形码不匹配';
      message.value = { type: 'error', text: `✗ 验证失败: ${txt}` };
      ElMessage.error(txt);
      secondPreview.value = '';
    }
  } catch (err) {
    message.value = { type: 'error', text: '✗ 验证失败' };
    ElMessage.error('验证失败: ' + err.message);
  }
};

// 处理结束上传
const handleEndUpload = async (e) => {
  const file = e.target.files?.[0];
  if (!file) return;

  try {
    const msg = ElMessage.info({ message: '验证中...', duration: 0 });
    endFile = file;

    const reader = new FileReader();
    reader.onload = r => endPreview.value = r.target?.result;
    reader.readAsDataURL(file);

    msg.close();
    message.value = { type: 'success', text: '✓ 已确认' };
    // 不再自动完成，让用户手动点击"完成任务"按钮
  } catch (err) {
    message.value = { type: 'error', text: '✗ 错误' };
    ElMessage.error('处理失败: ' + err.message);
  }
};

// 流程控制
const nextStep = () => {
  currentStep.value++;
  message.value = null;
};

const goBack = () => {
  if (currentStep.value > 0) {
    currentStep.value--;
    message.value = null;
  }
};

const finish = async () => {
  if (!currentTask.value) return;

  let msg = null;
  try {
    msg = ElMessage.info({ message: '完成任务中...', duration: 0 });
    
    // 获取当前登录护士的信息
    let nurseId = null;
    
    if (currentUser.value && currentUser.value.staffId) {
      nurseId = String(currentUser.value.staffId);
    } else {
      msg?.close();
      ElMessage.error('无法获取登录用户信息，请重新登录');
      return;
    }
    
    // 根据任务类别决定调用策略
    const category = currentTask.value.category;
    const currentStatus = currentTask.value.status;
    let resultPayload = null;
    
    if (category === 1 || category === 4) {
      // Immediate、DataCollection：一次完成（Pending → Completed）
      if (remarks.value) {
        resultPayload = remarks.value;
      }
      await api.completeExecutionTask(currentTask.value.id, nurseId, resultPayload);
      
      msg?.close();
      ElMessage.success(`任务已由 ${currentUser.value.fullName} 完成！`);
      
      // 添加到历史
      addToHistory(currentTask.value);

      // 重置
      reset();
      return;
    } else if (category === 5) {
      // Verification(核对药品)：一次完成（Pending → Completed）
      // 所有药品已核对完毕，直接完成任务
      if (remarks.value) {
        resultPayload = `核对备注：${remarks.value}`;
      }
      await api.completeExecutionTask(currentTask.value.id, nurseId, resultPayload);
      
      msg?.close();
      ElMessage.success(`任务已由 ${currentUser.value.fullName} 完成！`);
      
      // 添加到历史
      addToHistory(currentTask.value);

      // 重置
      reset();
      return;
    } else if (category === 2 || category === 3) {
      // Duration、ResultPending：两步完成
      
      if (currentStatus === 3 || currentStatus === 'Pending') {
        // 第一次调用：Pending → InProgress
        // 备注格式：开始备注：[内容]
        if (remarks.value) {
          resultPayload = `开始备注：${remarks.value}.`;
        }
        await api.completeExecutionTask(currentTask.value.id, nurseId, resultPayload);
        
        msg?.close();
        ElMessage.success(`任务已开始执行，请再扫一次以完成任务`);
        
        // 重置为第0步让护士再扫一次
        currentStep.value = 0;
        remarks.value = '';
        taskPreview.value = '';
        secondPreview.value = '';
        message.value = null;
        return;
      } else if (currentStatus === 4 || currentStatus === 'InProgress') {
        // 第二次调用：InProgress → Completed
        // 后端会自动合并为：开始备注：内容1.结束备注：内容2.的格式
        if (remarks.value) {
          resultPayload = remarks.value;
        }
        await api.completeExecutionTask(currentTask.value.id, nurseId, resultPayload);
        
        msg?.close();
        ElMessage.success(`任务已由 ${currentUser.value.fullName} 完成！`);
        
        // 添加到历史
        addToHistory(currentTask.value);

        // 重置
        reset();
        return;
      }
    }
    
    msg?.close();
    ElMessage.success(`任务已由 ${currentUser.value.fullName} 完成！`);
    
    // 添加到历史
    addToHistory(currentTask.value);

    // 重置
    reset();
  } catch (err) {
    msg?.close();
    ElMessage.error('完成失败: ' + (err.message || '未知错误'));
  }
};

const reset = () => {
  currentStep.value = 0;
  currentTask.value = null;
  taskFile = null;
  secondFile = null;
  endFile = null;
  taskPreview.value = '';
  secondPreview.value = '';
  endPreview.value = '';
  message.value = null;
  remarks.value = '';
  confirmedCount.value = 0;
  totalCount.value = 0;
};

// 保存历史到 localStorage
const saveHistoryToStorage = () => {
  try {
    localStorage.setItem('taskScanHistory', JSON.stringify(history.value));
  } catch (error) {
    console.error('保存执行历史失败:', error);
  }
};

// 添加历史记录
const addToHistory = (task) => {
  history.value.unshift({
    id: task.id,
    patientName: task.patientName,
    bedId: task.bedId,
    category: task.category,
    categoryName: getCategoryName(task.category),
    completedTime: new Date().toISOString(),
    completedBy: currentUser.value?.fullName || '未知',
    remarks: remarks.value || '无备注'
  });
  saveHistoryToStorage();
};

// 清空历史
const clearHistory = () => {
  ElMessageBox.confirm(
    '确定要清空所有执行历史吗？此操作无法撤销。',
    '警告',
    {
      confirmButtonText: '确定清空',
      cancelButtonText: '取消',
      type: 'warning',
    }
  )
  .then(() => {
    history.value = [];
    saveHistoryToStorage();
    ElMessage.success('执行历史已清空');
  })
  .catch(() => {});
};

// 辅助函数
const getCategoryName = (cat) => {
  const names = { 1: '立即执行', 2: '持续执行', 3: '结果待收集', 5: '核对药品' };
  return names[cat] || '其他';
};

const formatTime = (dt) => {
  if (!dt) return '-';
  return new Date(dt).toLocaleString('zh-CN');
};

// 格式化历史记录时间（只显示时间，不显示日期）
const formatHistoryTime = (dt) => {
  if (!dt) return '-';
  try {
    const date = new Date(dt);
    return date.toLocaleString('zh-CN', {
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit'
    });
  } catch {
    return dt;
  }
};

/**
 * 获取药品的状态类名（用于高亮样式）
 * @param {Object} drug - 药品对象
 * @returns {String} 状态类名：'scanned' 或 'unscanned'
 */
const getDrugStatus = (drug) => {
  return drug && drug.scanned ? 'scanned' : 'unscanned';
};

// 处理药品输入（直接输入药品代码打钩）
const handleDrugInput = async () => {
  const drugCode = drugInputValue.value.trim();
  if (!drugCode) return;

  try {
    // 在当前任务的药品清单中查找匹配的药品
    const foundDrug = currentTask.value.drugs?.find(d => 
      d.drugId === drugCode || 
      (d.drugName && d.drugName.includes(drugCode))
    );

    if (foundDrug) {
      if (foundDrug.scanned) {
        ElMessage.warning('该药品已经核对过了');
      } else {
        // 标记为已扫描
        foundDrug.scanned = true;
        confirmedCount.value++;
        ElMessage.success(`已核对: ${foundDrug.drugName || foundDrug.drugId}`);

        // 清空输入框
        drugInputValue.value = '';

        // 检查是否完成
        if (confirmedCount.value >= totalCount.value && totalCount.value > 0) {
          ElMessage.success('所有药品已核对完毕！');
          setTimeout(() => nextStep(), 1000);
        }
      }
    } else {
      ElMessage.error(`未找到药品：${drugCode}`);
    }
  } catch (err) {
    ElMessage.error('核对失败: ' + err.message);
  }
};

// 切换药品的核对状态（点击打钩/取消）
const toggleDrugStatus = (drug) => {
  if (!drug) return;
  
  if (drug.scanned) {
    // 已核对 -> 取消核对
    drug.scanned = false;
    confirmedCount.value--;
    ElMessage.info(`已取消: ${drug.drugName || drug.drugId}`);
  } else {
    // 未核对 -> 标记为已核对
    drug.scanned = true;
    confirmedCount.value++;
    ElMessage.success(`已核对: ${drug.drugName || drug.drugId}`);

    // 检查是否完成
    if (confirmedCount.value >= totalCount.value && totalCount.value > 0) {
      ElMessage.success('所有药品已核对完毕！');
      setTimeout(() => nextStep(), 1000);
    }
  }
};

/**
 * 根据进度计算进度条颜色
 * @param {Number} scanned - 已扫数
 * @param {Number} total - 总数
 * @returns {String} 颜色值
 */
const getProgressColor = (scanned, total) => {
  if (total === 0) return '#E6A23C'; // 黄色：无清单
  const percent = Math.round((scanned / total) * 100);
  if (percent === 100) return '#67C23A'; // 绿色：完成
  if (percent >= 50) return '#409EFF'; // 蓝色：进行中
  return '#F56C6C'; // 红色：开始阶段
};

// 组件挂载时的初始化
onMounted(() => {
  // 重新加载历史以确保获取最新的 localStorage 数据
  refreshHistory();
});

// 刷新历史数据
const refreshHistory = () => {
  const freshHistory = loadHistoryFromStorage();
  // 清空并重新赋值
  history.value.length = 0;
  history.value.push(...freshHistory);
};

// 监听路由变化，当返回到此页面时重新加载历史
const route = useRoute();
watch(() => route.path, (newPath) => {
  if (newPath.includes('task-scan')) {
    refreshHistory();
  }
});

// 组件卸载前保存历史
onBeforeUnmount(() => {
  saveHistoryToStorage();
});
</script>

<style scoped>
.task-scan {
  padding: 20px;
  background: #f4f7f9;
  min-height: calc(100vh - 60px);
}

.page-header h2 {
  margin: 0 0 20px 0;
  font-size: 1.8rem;
  color: #303133;
}

.scan-container {
  display: grid;
  grid-template-columns: 1fr;
  gap: 20px;
}

.scan-panel {
  background: white;
  border-radius: 8px;
  padding: 30px;
  box-shadow: 0 2px 12px rgba(0,0,0,0.08);
}

.operation-bar {
  margin-bottom: 30px;
  display: flex;
  flex-direction: column;
  gap: 0;
}

.history-tabs {
  display: flex;
  gap: 0;
  border-bottom: 2px solid #dcdfe6;
}

.history-tabs .tab {
  padding: 12px 24px;
  cursor: pointer;
  border: none;
  background: none;
  color: #909399;
  font-size: 1rem;
  font-weight: 500;
  position: relative;
  transition: all 0.3s;
  border-bottom: 2px solid transparent;
  margin-bottom: -2px;
}

.history-tabs .tab:hover {
  color: #606266;
}

.history-tabs .tab.active {
  color: #409eff;
  border-bottom-color: #409eff;
}

.history-content {
  min-height: 400px;
}

.history-actions {
  margin-top: 20px;
  padding-top: 20px;
  border-top: 1px solid #dcdfe6;
}

.step-indicator {
  display: flex;
  gap: 40px;
  justify-content: center;
  flex-wrap: wrap;
  margin-bottom: 30px;
}

.step {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 10px;
}

.step-circle {
  width: 50px;
  height: 50px;
  border-radius: 50%;
  border: 3px solid #dcdfe6;
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: bold;
  font-size: 1.2rem;
  color: #909399;
  background: white;
  transition: all 0.3s;
}

.step-circle.active {
  border-color: #409eff;
  color: white;
  background: #409eff;
}

.step-circle.completed {
  border-color: #67c23a;
  color: white;
  background: #67c23a;
}

.step-label {
  font-size: 0.9rem;
  color: #606266;
  text-align: center;
}

.step-content {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.step-content h3 {
  margin: 0;
  font-size: 1.5rem;
  color: #303133;
}

.step-desc {
  margin: 0;
  color: #909399;
  font-size: 0.95rem;
}

.upload-box {
  display: flex;
  flex-direction: column;
  gap: 15px;
}

.upload-area {
  border: 2px dashed #dcdfe6;
  border-radius: 8px;
  padding: 40px 20px;
  text-align: center;
  cursor: pointer;
  transition: all 0.3s;
  background: #fafafa;
}

.upload-area:hover {
  border-color: #409eff;
  background: #f5f7fa;
}

.upload-area div:nth-child(2) {
  font-weight: 600;
  color: #303133;
  margin: 10px 0 5px 0;
}

.upload-area small {
  color: #909399;
}

.preview {
  max-width: 150px;
  max-height: 150px;
  border-radius: 6px;
  border: 1px solid #dcdfe6;
}

.task-info {
  background: #f5f7fa;
  padding: 15px;
  border-radius: 6px;
  border-left: 4px solid #409eff;
}

.task-info h4 {
  margin: 0 0 10px 0;
  color: #303133;
}

.task-info p {
  margin: 5px 0;
  font-size: 0.95rem;
}

.current-task {
  background: #e8f4ff;
  padding: 12px 16px;
  border-radius: 6px;
  border-left: 4px solid #409eff;
  color: #303133;
  font-weight: 500;
}

.task-confirm {
  background: #f5f7fa;
  padding: 15px;
  border-radius: 6px;
  border-left: 4px solid #67c23a;
  margin-bottom: 20px;
}

.task-confirm h4 {
  margin: 0 0 10px 0;
  color: #303133;
}

.task-confirm p {
  margin: 8px 0;
  font-size: 0.95rem;
  color: #606266;
}

.remark-box {
  margin-bottom: 20px;
}

.remark-box label {
  display: block;
  margin-bottom: 8px;
  color: #303133;
  font-weight: 500;
}

.progress {
  background: #f5f7fa;
  padding: 15px;
  border-radius: 6px;
}

.progress-stats {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 10px;
  font-size: 14px;
}

.progress-stats .count-scanned {
  color: #67c23a;
  font-size: 1.1em;
}

.progress-stats .count-total {
  color: #606266;
  font-size: 1.1em;
}

.progress-stats .separator {
  color: #c0c4cc;
}

.progress-stats .warning-note {
  color: #e6a23c;
  font-size: 12px;
  font-weight: normal;
}

.msg {
  padding: 12px;
  border-radius: 6px;
  text-align: center;
  font-weight: 500;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
}

.msg.success {
  background: #f0f9ff;
  color: #67c23a;
  border: 1px solid #67c23a;
}

.msg.error {
  background: #fef0f0;
  color: #f56c6c;
  border: 1px solid #f56c6c;
}

.msg.warning {
  background: #fdf6ec;
  color: #e6a23c;
  border: 1px solid #e6a23c;
}

.msg-icon {
  font-size: 1.2em;
}

/* 药品清单样式 */
.drug-list-panel {
  background: #fafbfc;
  border: 1px solid #dcdfe6;
  border-radius: 6px;
  padding: 15px;
  margin-bottom: 20px;
}

.drug-list-panel h4 {
  margin: 0 0 12px 0;
  font-size: 14px;
  color: #303133;
  font-weight: 600;
}

.drug-input-area {
  margin-bottom: 15px;
}

.drug-list {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
  gap: 10px;
}

.drug-item {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 10px;
  border-radius: 4px;
  border: 1px solid #e4e7eb;
  background: white;
  transition: all 0.3s;
  cursor: pointer; /* 显示可点击 */
}

.drug-item:hover {
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
  transform: translateY(-2px);
}

.drug-item.scanned {
  background: #f0f9ff;
  border-color: #67c23a;
}

.drug-item.unscanned {
  background: #fafbfc;
  border-color: #dcdfe6;
}

.drug-item.scanned:hover {
  box-shadow: 0 2px 8px rgba(103, 194, 58, 0.15);
}

.drug-item.unscanned:hover {
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.drug-status-icon {
  flex-shrink: 0;
  width: 28px;
  height: 28px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 50%;
  font-weight: bold;
  font-size: 1.2em;
}

.drug-item.scanned .drug-status-icon {
  background: #67c23a;
  color: white;
}

.drug-item.unscanned .drug-status-icon {
  background: #e4e7eb;
  color: #909399;
}

.scanned-icon {
  display: inline-block;
}

.unscanned-icon {
  display: inline-block;
}

.drug-info {
  flex: 1;
  min-width: 0;
}

.drug-name {
  font-size: 13px;
  font-weight: 500;
  color: #303133;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.drug-id {
  font-size: 12px;
  color: #909399;
  margin-top: 2px;
}

.action-btns {
  display: flex;
  gap: 12px;
  margin-top: 20px;
  justify-content: center;
}

.history-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.history-item {
  padding: 12px;
  background: white;
  border-radius: 6px;
  border: 1px solid #e0e6f2;
  border-left: 4px solid #409eff;
  box-shadow: 0 1px 4px rgba(0, 0, 0, 0.05);
  transition: all 0.3s;
}

.history-item:hover {
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
  border-left-color: #67c23a;
}

.h-header {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 10px;
  padding-bottom: 10px;
  border-bottom: 1px solid #ebeef5;
}

.task-id {
  font-weight: 600;
  color: #303133;
  font-size: 14px;
}

.task-category {
  background: #e8f4ff;
  color: #409eff;
  padding: 2px 8px;
  border-radius: 3px;
  font-size: 12px;
  font-weight: 500;
}

.status {
  color: #67c23a;
  font-size: 12px;
  font-weight: 600;
  margin-left: auto;
}

.h-patient,
.h-executor,
.h-time,
.h-remarks {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-top: 6px;
  font-size: 13px;
}

.h-patient .label,
.h-executor .label,
.h-time .label,
.h-remarks .label {
  color: #909399;
  font-weight: 500;
  min-width: 60px;
}

.h-patient .value,
.h-executor .value,
.h-time .value,
.h-remarks .value {
  color: #303133;
  flex: 1;
}

.bed-id {
  color: #909399;
  font-size: 12px;
  margin-left: 4px;
}

.empty {
  text-align: center;
  color: #909399;
  padding: 30px 20px;
}

@media (max-width: 1200px) {
  .scan-container {
    grid-template-columns: 1fr;
  }
}
</style>
