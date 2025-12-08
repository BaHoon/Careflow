# MedicationOrder 拆分为 ExecutionTask 功能实现

## 📋 功能概述

本功能实现了将medication order (医嘱)自动拆分为多个execution task (执行任务)的完整解决方案。支持4种时间策略：立即执行、指定时间、周期性执行和时段执行。

## 🎯 核心组件

### 1. 服务接口
- **文件**: `CareFlow.Core/Interfaces/IMedicationOrderTaskService.cs`
- **功能**: 定义核心服务方法

### 2. 服务实现
- **文件**: `CareFlow.Application/Services/MedicationOrderTaskService.cs`
- **功能**: 实现具体的任务生成逻辑

### 3. Web API 控制器
- **文件**: `CareFlow.WebApi/Controllers/MedicationOrderTaskController.cs`
- **功能**: 提供HTTP接口进行测试和调用

### 4. 依赖注入配置
- **文件**: `CareFlow.Application/Extensions/ServiceCollectionExtensions.cs`
- **功能**: 注册服务到DI容器

## 🚀 API 接口说明

### 1. 生成执行任务
```
POST /api/MedicationOrderTask/generate
Content-Type: application/json

{
    "medicationOrderId": 1
}
```

### 2. 回滚未执行任务
```
POST /api/MedicationOrderTask/{orderId}/rollback
Content-Type: application/json

"医嘱停止"
```

### 3. 刷新执行任务
```
POST /api/MedicationOrderTask/{orderId}/refresh
```

## 🔧 时间策略详解

### IMMEDIATE (立即执行)
- 生成1个任务，计划开始时间为当前时间
- 适用于临时医嘱、急诊用药

### SPECIFIC (指定时间)
- 生成1个任务，在SpecificExecutionTime时执行
- 适用于特定时间点的用药

### CYCLIC (周期性执行)
- 根据FreqCode(BID/TID/QID)和IntervalDays生成多个任务
- 按每日频次在指定日期范围内平均分布

### SLOTS (时段执行)
- 根据SmartSlotsMask位掩码匹配HospitalTimeSlot
- 在每个匹配的时间槽位生成执行任务

## 📊 位掩码示例

在测试数据中，SmartSlotsMask使用位运算组合时间槽位：

```csharp
// 早餐后 + 晚餐后
SmartSlotsMask = 2 | 32, // 34

// 早餐前 + 午餐前 + 晚餐前  
SmartSlotsMask = 1 | 4 | 16, // 21

// 上午 + 睡前
SmartSlotsMask = 512 | 64, // 576
```

## 🔄 状态管理

### ExecutionTask 状态流转
```
Pending → Running → Completed
   ↓         ↓
Cancelled ← Cancelled
```

### 回滚机制
- 只回滚状态为"Pending"且未开始执行(ActualStartTime=null)的任务
- 回滚后状态变为"Cancelled"，记录回滚原因

## 🧪 测试方法

### 1. 使用Swagger UI
1. 启动项目，访问 `/swagger`
2. 找到 `MedicationOrderTask` 控制器
3. 测试 `generate` 接口，传入已有的医嘱ID

### 2. 使用测试数据
项目初始化时已创建了多种类型的medication order测试数据，包括：
- 口服药物 - 长期医嘱 (BID)
- 静脉滴注 - 立即执行  
- 胰岛素 - 餐前注射 (TID)
- 吸氧 - 持续治疗
- 外用药膏 - 早晚使用
- 抗生素 - 每日4次 (QID)
- 镇痛药 - 必要时使用 (PRN)

### 3. 数据库验证
生成任务后，可以在数据库中查看 `ExecutionTasks` 表的数据：

```sql
SELECT * FROM "ExecutionTasks" WHERE "MedicalOrderId" = 1;
```

## 🛠️ 扩展建议

1. **并发控制**: 建议为ExecutionTask添加乐观锁版本号字段
2. **通知机制**: 可添加任务生成后的消息队列通知
3. **任务调度**: 可集成Quartz.NET等定时任务框架
4. **审计日志**: 增加详细的操作日志记录
5. **性能优化**: 对大量任务的批量操作进行优化