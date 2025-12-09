# 药品医嘱任务拆分DataPayload格式说明

## 新的DataPayload结构

现在每个ExecutionTask的DataPayload字段都会生成如下标准化格式：

```json
{
  "TaskType": "MEDICATION_ADMINISTRATION",
  "Title": "药品给药核对",
  "Description": "阿莫西林胶囊(阿莫西林胶囊) 250mg - 口服 - 每日三次，执行时间：2025-12-09 14:30，指定时间执行",
  "IsChecklist": true,
  "Items": [
    {
      "id": 1,
      "text": "核对患者身份：P001",
      "isChecked": false,
      "required": true
    },
    {
      "id": 2,
      "text": "核对药品信息：阿莫西林胶囊(阿莫西林胶囊) 250mg",
      "isChecked": false,
      "required": true
    },
    {
      "id": 3,
      "text": "核对给药剂量：250mg",
      "isChecked": false,
      "required": true
    },
    {
      "id": 4,
      "text": "核对给药途径：口服",
      "isChecked": false,
      "required": true
    },
    {
      "id": 5,
      "text": "执行给药操作",
      "isChecked": false,
      "required": true
    },
    {
      "id": 6,
      "text": "观察患者用药后反应",
      "isChecked": false,
      "required": true
    },
    {
      "id": 7,
      "text": "记录执行时间和结果",
      "isChecked": false,
      "required": true
    }
  ],
  "MedicationInfo": {
    "DrugId": "DRUG001",
    "Dosage": "250mg",
    "UsageRoute": "口服",
    "FreqCode": "TID",
    "ExecutionTime": "2025-12-09 14:30:00",
    "SlotName": "下午时段"
  }
}
```

## 主要改进

1. **标准化结构**: 所有ExecutionTask都使用相同的JSON结构格式
2. **丰富的描述信息**: Description字段包含药品+使用方式+使用时间的完整描述
3. **护士清单**: Items数组提供了给药过程中需要核对的清单项
4. **元数据信息**: MedicationInfo对象包含了任务相关的所有医嘱信息

## 不同时间策略的描述差异

### 立即执行 (IMMEDIATE)
- Description包含"立即执行"标识
- ExecutionTime为当前时间

### 指定时间 (SPECIFIC) 
- Description包含"指定时间执行"标识
- ExecutionTime为医嘱中设定的具体时间

### 周期性执行 (CYCLIC)
- Description包含"每日执行"或"每X天执行"标识
- 根据IntervalDays生成相应描述

### 时段执行 (SLOTS)
- Description包含时段名称（如"晨间给药"、"午间给药"等）
- SlotName字段包含具体的时段信息

## 频次代码中文映射

- ONCE: "单次给药"
- QD: "每日一次" 
- BID: "每日两次"
- TID: "每日三次"
- QID: "每日四次"
- Q6H: "每6小时一次"
- Q8H: "每8小时一次"
- Q12H: "每12小时一次"
- PRN: "需要时给药"
- CONT: "持续给药"