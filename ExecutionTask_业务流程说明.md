# ExecutionTask 业务流程说明

## 概述
根据任务的 `TaskCategory` 字段，ExecutionTask 在执行时有不同的状态转换流程。当任务状态为 **就绪(2)、待执行(3)、执行中(4)** 时，会在"我的任务"面板中显示。

---

## 任务类别与状态转换

### 1. **Immediate（即刻执行）** ✅ 已实现
**特点**：一次性、立即完成的任务

**按钮显示**：
- 状态 Pending(3) → 显示"**完成任务**"按钮

**执行流程**：
```
Pending(3) → 点击"完成任务" → 显示任务详情 → 再点一下"确认完成" → Completed(5)
```

**示例**：打针、注射、外用药膏涂抹等

---

### 2. **Duration（持续任务）** ✅ 已实现
**特点**：需要启动后才能结束，执行过程持续一段时间

**按钮显示**：
- 状态 Pending(3) → 显示"**完成任务**"按钮
- 状态 InProgress(4) → 显示"**结束任务**"按钮

**执行流程**：
```
Pending(3) 
  → 点击"完成任务" 
  → 显示任务详情 
  → 再点一下"确认开始" 
  → InProgress(4)
  → 点击"结束任务" 
  → 显示任务详情 
  → 再点一下"确认完成" 
  → Completed(5)
```

**示例**：静脉滴注、持续输液、氧疗等

---

### 3. **ResultPending（结果待定）** ✅ 已实现
**特点**：类似Duration，但完成时需要录入执行结果

**按钮显示**：
- 状态 Pending(3) → 显示"**完成任务**"按钮
- 状态 InProgress(4) → 显示"**结束任务（需录入结果）**"按钮

**执行流程**：
```
Pending(3) 
  → 点击"完成任务" 
  → 显示任务详情 
  → 再点一下"确认开始" 
  → InProgress(4)
  → 点击"结束任务（需录入结果）" 
  → 显示任务详情 
  → 在文本框中录入执行结果（JSON或文本格式） 
  → 点"确认完成" 
  → Completed(5) + ResultPayload
```

**示例**：皮肤测试、生命体征采集（待结果确认）等

---

### 4. **DataCollection（数据采集）** ⏳ TODO
**特点**：待定

**当前状态**：暂未实现，返回错误提示

---

### 5. **Verification（核对验证）** ⏳ TODO
**特点**：待定

**当前状态**：暂未实现，返回错误提示

---

### 6. **ApplicationWithPrint（申请打印）** ⏳ TODO
**特点**：待定

**当前状态**：暂未实现，返回错误提示

---

## 技术实现细节

### 后端 (NursingController.cs)

#### CompleteExecutionTask API
**端点**：`POST /api/nursing/execution-tasks/{id}/complete`

**请求体**：
```json
{
  "nurseId": "nurse001",  // 支持工号或简码
  "resultPayload": "{...}"  // 可选，ResultPending类别时必填
}
```

**状态转换逻辑**：
```csharp
if (category == Immediate) {
  // 状态必须为 Pending 或 AppliedConfirmed
  // 直接转换：3 → 5 或 2 → 5
  targetStatus = Completed(5);
}
else if (category == Duration) {
  if (status == Pending || status == AppliedConfirmed) {
    targetStatus = InProgress(4);
  }
  else if (status == InProgress) {
    targetStatus = Completed(5);
  }
}
else if (category == ResultPending) {
  if (status == Pending || status == AppliedConfirmed) {
    targetStatus = InProgress(4);
  }
  else if (status == InProgress) {
    // 必须提供 resultPayload
    targetStatus = Completed(5);
  }
}
else {
  return BadRequest("暂未实现");  // TODO
}
```

**响应示例**：
```json
{
  "message": "已完成",
  "taskId": 123,
  "category": "Immediate",
  "status": 5,
  "executorName": "王护士",
  "nextAction": "任务已完成"
}
```

### 前端 (TaskItem.vue)

#### 按钮显示逻辑

**getCompletionButtonLabel(category, isFinishing)**
- `category`：任务类别
- `isFinishing`：是否是最后一个按钮（结束任务）

**示例**：
- Immediate, isFinishing=false → "完成任务"
- Duration, isFinishing=false → "完成任务"
- Duration, isFinishing=true → "结束任务"
- ResultPending, isFinishing=true → "结束任务（需录入结果）"

#### 事件处理

**handleStartCompletion()**
- 第一阶段：显示任务详情确认对话框
- Immediate：直接调 CompleteExecutionTask (3→5)
- Duration/ResultPending：调 CompleteExecutionTask (3→4)

**handleFinishTask()**
- 第二阶段（仅限 Duration/ResultPending）
- ResultPending：弹出文本框录入结果，然后调 CompleteExecutionTask (4→5)
- Duration：直接确认，然后调 CompleteExecutionTask (4→5)

---

## 数据流向

### DataPayload 示例

**药品医嘱 (MEDICATION_ADMINISTRATION)**：
```json
{
  "taskType": "MEDICATION_ADMINISTRATION",
  "title": "口服阿司匹林 100mg",
  "drugName": "阿司匹林片",
  "description": "头孢曲松钠(罗氏芬) 1.0g/瓶",
  "isChecklist": true,
  "items": [
    {
      "id": 1,
      "text": "核对药品",
      "isChecked": false,
      "required": true
    }
  ],
  "medicationInfo": {
    "drugName": "阿司匹林片",
    "specification": "100mg/片",
    "dosage": "100mg",
    "route": "PO",
    "frequency": "一日一次"
  }
}
```

### ResultPayload 示例

**完成时录入的结果**（ResultPending类别）：
```json
{
  "result": "阴性",
  "note": "皮试 (-)",
  "timestamp": "2025-12-24T14:13:40Z",
  "recordedBy": "赵护士"
}
```

或自由格式文本：
```
患者皮肤完整，无破损，备皮完成。
```

---

## UI 演示

### Immediate 类别流程
```
[待执行] → 点击"完成任务" 
  ↓ (弹对话框显示任务详情)
  → 点"确认完成" 
  ↓ (API调用)
  → [已完成]
```

### Duration 类别流程
```
[待执行] → 点击"完成任务" 
  ↓ (弹对话框显示任务详情)
  → 点"确认开始" 
  ↓ (API调用)
  → [执行中] 
  → 点击"结束任务" 
  ↓ (弹对话框显示任务详情)
  → 点"确认完成" 
  ↓ (API调用)
  → [已完成]
```

### ResultPending 类别流程
```
[待执行] → 点击"完成任务" 
  ↓ (弹对话框显示任务详情)
  → 点"确认开始" 
  ↓ (API调用)
  → [执行中] 
  → 点击"结束任务（需录入结果）" 
  ↓ (弹对话框显示任务详情+结果输入框)
  → 在文本框输入结果 
  → 点"确认完成" 
  ↓ (API调用)
  → [已完成]
```

---

## 待实现事项（TODO）

- [ ] DataCollection 类别的具体流程定义
- [ ] Verification 类别的具体流程定义
- [ ] ApplicationWithPrint 类别的具体流程定义
- [ ] 支持不同科室的自定义流程配置
- [ ] 任务超时提醒和自动处理规则
