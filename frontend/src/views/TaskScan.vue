<template>
  <div class="task-scan">
    <div class="page-header">
      <h2>任务扫码执行</h2>
    </div>

    <div class="scan-container">
      <!-- 左侧：扫码面板 -->
      <div class="scan-panel">
        <div class="step-indicator">
          <div v-for="(s, i) in ['上传任务码', '上传患者/药品码', '完成任务']" :key="i" class="step">
            <div :class="['step-circle', { active: currentStep === i, completed: currentStep > i }]">{{ i + 1 }}</div>
            <div class="step-label">{{ s }}</div>
          </div>
        </div>

        <div class="step-content">
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
            <h3>{{ currentTask.category === 5 ? '📦 核对药品' : '👤 上传患者码' }}</h3>
            <p class="step-desc">{{ currentTask.category === 5 ? '逐个上传药品条形码' : '上传患者条形码验证匹配' }}</p>
            
            <div class="current-task">{{ currentTask.patientName }} - {{ getCategoryName(currentTask.category) }}</div>

            <!-- 药品清单（仅在核对药品时显示） -->
            <div v-if="currentTask.category === 5 && currentTask.drugs" class="drug-list-panel">
              <h4>📋 期望核对的药品清单</h4>
              <div class="drug-list">
                <div v-for="(drug, idx) in currentTask.drugs" :key="idx" :class="['drug-item', getDrugStatus(drug)]">
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

        <!-- 操作按钮 -->
        <div class="action-btns">
          <el-button v-if="currentStep > 0" @click="goBack">← 返回</el-button>
          <el-button v-if="currentStep === 2" type="success" @click="finish">完成任务 ✓</el-button>
        </div>
      </div>

      <!-- 右侧：历史记录 -->
      <div class="history-panel">
        <h3>📝 执行历史</h3>
        <div v-if="history.length > 0" class="history-list">
          <div v-for="t in history" :key="t.id" class="history-item">
            <div class="h-header">
              <span>#{{ t.id }}</span>
              <span class="status">✓ 已完成</span>
            </div>
            <div>{{ t.patientName }}</div>
            <small>{{ formatTime(t.time) }}</small>
          </div>
        </div>
        <div v-else class="empty">暂无记录</div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue';
import { ElMessage } from 'element-plus';
import * as api from '../api/executionTask';
import { ElMessageBox } from 'element-plus';

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
const history = ref([]);
const remarks = ref('');

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
      currentTask.value = taskDetail;
      ElMessage.success('任务信息已加载（手动输入）');
    } else {
      // 使用识别出来的taskId获取任务详情
      const taskDetail = await api.getExecutionTaskDetail(result.taskId);
      currentTask.value = taskDetail;
      ElMessage.success('任务信息已加载（自动识别）');
    }
    
    if (currentTask.value.category === 5) {
      totalCount.value = currentTask.value.drugs?.length || 0;
      // 从后端返回的 drugs 列表中统计已扫描项
      confirmedCount.value = (currentTask.value.drugs || []).filter(d => d.scanned).length;
      if (totalCount.value === 0) {
        message.value = { type: 'warning', text: '未能从任务中读取期望药品清单，扫码将仅记录条码' };
      }
      // 前端调试：打印任务信息与清单
      console.log('[TaskScan] 任务加载完成:', {
        taskId: currentTask.value.id,
        category: currentTask.value.category,
        totalDrugs: totalCount.value,
        drugs: currentTask.value.drugs,
        dataPayload: currentTask.value.dataPayload
      });
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
        // 从后端结果更新已确认数和总数（后端返回 scannedCount/expectedCount/progress）
        if (typeof result.scannedCount === 'number') {
          confirmedCount.value = result.scannedCount;
        } else {
          // 回退：重新拉取任务详情
          const refreshed = await api.getExecutionTaskDetail(currentTask.value.id);
          confirmedCount.value = (refreshed.drugs || []).filter(d => d.scanned).length;
        }

        if (typeof result.expectedCount === 'number') {
          totalCount.value = result.expectedCount;
        }

        message.value = { type: 'success', text: `✓ 药品已核对 （${confirmedCount.value}/${totalCount.value}）` };

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

  try {
    const msg = ElMessage.info({ message: '完成任务中...', duration: 0 });
    
    // 获取当前登录护士的信息
    let nurseId = null;
    
    if (currentUser.value && currentUser.value.staffId) {
      nurseId = String(currentUser.value.staffId);
      console.log('[finish] 使用登录护士ID:', nurseId, '护士姓名:', currentUser.value.fullName);
    } else {
      msg.close();
      console.error('[finish] 无法获取登录用户信息，currentUser:', currentUser.value);
      ElMessage.error('无法获取登录用户信息，请重新登录');
      return;
    }
    
    // 根据任务类别决定调用策略
    const category = currentTask.value.category;
    const currentStatus = currentTask.value.status;
    let resultPayload = null;
    
    if (category === 1 || category === 4) {
      // Immediate：一次完成（Pending → Completed）
      if (remarks.value) {
        resultPayload = remarks.value;
      }
      await api.completeExecutionTask(currentTask.value.id, nurseId, resultPayload);
    } else if (category === 5) {
      // Verification(核对药品)：一次完成（Pending → Completed）
      // 所有药品已核对完毕，直接完成任务
      if (remarks.value) {
        resultPayload = `核对备注：${remarks.value}`;
      }
      await api.completeExecutionTask(currentTask.value.id, nurseId, resultPayload);
      
      msg.close();
      ElMessage.success(`任务已由 ${currentUser.value.fullName} 完成！`);
      
      // 添加到历史
      history.value.unshift({
        id: currentTask.value.id,
        patientName: currentTask.value.patientName,
        time: new Date()
      });

      // 重置
      reset();
      return;
    } else if (category === 2 || category === 3) {
      // Duration、ResultPending：两步完成
      
      if (currentStatus === 3 || currentStatus === 'Pending') {
        // 第一次调用：Pending → InProgress
        // 备注格式：开始备注：[内容]
        if (remarks.value) {
          resultPayload = `开始备注：${remarks.value}`;
        }
        await api.completeExecutionTask(currentTask.value.id, nurseId, resultPayload);
        
        msg.close();
        ElMessage.success(`任务已开始执行，请再扫一次以完成任务`);
      } else if (currentStatus === 4 || currentStatus === 'InProgress') {
        // 第二次调用：InProgress → Completed
        // 需要先获取现有的备注，然后追加
        // 这里我们假设备注已经在服务器保存了，我们就追加新的
        if (remarks.value) {
          resultPayload = `结束备注：${remarks.value}`;
        }
        await api.completeExecutionTask(currentTask.value.id, nurseId, resultPayload);
        
        msg.close();
        ElMessage.success(`任务已由 ${currentUser.value.fullName} 完成！`);
        
        // 添加到历史
        history.value.unshift({
          id: currentTask.value.id,
          patientName: currentTask.value.patientName,
          time: new Date()
        });

        // 重置
        reset();
        return;
      }
    }
    // 如果是第一次调用（Pending→InProgress），不清空数据，重置为第0步让护士再扫一次
    if ((category === 2 || category === 3) && (currentStatus === 3 || currentStatus === 'Pending')) {
      currentStep.value = 0;
      remarks.value = '';
      taskPreview.value = '';
      secondPreview.value = '';
      message.value = null;
      return;
    }
    
    msg.close();
    ElMessage.success(`任务已由 ${currentUser.value.fullName} 完成！`);
    
    // 添加到历史
    history.value.unshift({
      id: currentTask.value.id,
      patientName: currentTask.value.patientName,
      time: new Date()
    });

    // 重置
    reset();
  } catch (err) {
    ElMessage.error('完成失败: ' + err.message);
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

// 辅助函数
const getCategoryName = (cat) => {
  const names = { 1: '立即执行', 2: '持续执行', 3: '结果待收集', 5: '核对药品' };
  return names[cat] || '其他';
};

const formatTime = (dt) => {
  if (!dt) return '-';
  return new Date(dt).toLocaleString('zh-CN');
};

/**
 * 获取药品的状态类名（用于高亮样式）
 * @param {Object} drug - 药品对象
 * @returns {String} 状态类名：'scanned' 或 'unscanned'
 */
const getDrugStatus = (drug) => {
  return drug && drug.scanned ? 'scanned' : 'unscanned';
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
  grid-template-columns: 1fr 320px;
  gap: 20px;
}

.scan-panel {
  background: white;
  border-radius: 8px;
  padding: 30px;
  box-shadow: 0 2px 12px rgba(0,0,0,0.08);
}

.step-indicator {
  display: flex;
  gap: 40px;
  margin-bottom: 40px;
  justify-content: center;
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

.history-panel {
  background: white;
  border-radius: 8px;
  padding: 20px;
  box-shadow: 0 2px 12px rgba(0,0,0,0.08);
  height: fit-content;
  position: sticky;
  top: 20px;
}

.history-panel h3 {
  margin: 0 0 15px 0;
  font-size: 1.1rem;
  color: #303133;
}

.history-list {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.history-item {
  padding: 10px;
  background: #f5f7fa;
  border-radius: 6px;
  font-size: 0.9rem;
  border-left: 3px solid #dcdfe6;
}

.h-header {
  display: flex;
  justify-content: space-between;
  margin-bottom: 5px;
}

.h-header span:first-child {
  font-weight: 600;
  color: #303133;
}

.status {
  color: #67c23a;
  font-size: 0.85rem;
  font-weight: 600;
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
  
  .history-panel {
    position: static;
  }
}
</style>
