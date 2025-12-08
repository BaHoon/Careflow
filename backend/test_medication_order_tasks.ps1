# PowerShell 测试脚本 - MedicationOrder Task 功能验证

Write-Host "=== MedicationOrder 拆分为 ExecutionTask 功能测试 ===" -ForegroundColor Green

$baseUrl = "http://localhost:5181"
$headers = @{
    "Content-Type" = "application/json"
}

# 测试1: 为医嘱生成执行任务
Write-Host "`n[测试1] 为医嘱ID=1生成执行任务..." -ForegroundColor Yellow

$generateRequest = @{
    medicationOrderId = 1
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/MedicationOrderTask/generate" -Method POST -Body $generateRequest -Headers $headers
    Write-Host "生成任务成功!" -ForegroundColor Green
    Write-Host "任务数量: $($response.TaskCount)" -ForegroundColor Cyan
    Write-Host "消息: $($response.Message)" -ForegroundColor Cyan
    
    if ($response.Tasks.Count -gt 0) {
        Write-Host "前3个任务详情:" -ForegroundColor Cyan
        $response.Tasks[0..2] | ForEach-Object {
            Write-Host "  - 任务ID: $($_.Id), 计划时间: $($_.PlannedStartTime), 状态: $($_.Status)" -ForegroundColor White
        }
    }
}
catch {
    Write-Host "生成任务失败: $($_.Exception.Message)" -ForegroundColor Red
}

# 测试2: 为另一个医嘱生成任务
Write-Host "`n[测试2] 为医嘱ID=2生成执行任务..." -ForegroundColor Yellow

$generateRequest2 = @{
    medicationOrderId = 2
} | ConvertTo-Json

try {
    $response2 = Invoke-RestMethod -Uri "$baseUrl/api/MedicationOrderTask/generate" -Method POST -Body $generateRequest2 -Headers $headers
    Write-Host "生成任务成功!" -ForegroundColor Green
    Write-Host "任务数量: $($response2.TaskCount)" -ForegroundColor Cyan
    Write-Host "消息: $($response2.Message)" -ForegroundColor Cyan
}
catch {
    Write-Host "生成任务失败: $($_.Exception.Message)" -ForegroundColor Red
}

# 测试3: 回滚医嘱1的未执行任务
Write-Host "`n[测试3] 回滚医嘱ID=1的未执行任务..." -ForegroundColor Yellow

$rollbackReason = "测试回滚功能" | ConvertTo-Json

try {
    $rollbackResponse = Invoke-RestMethod -Uri "$baseUrl/api/MedicationOrderTask/1/rollback" -Method POST -Body $rollbackReason -Headers $headers
    Write-Host "回滚任务成功!" -ForegroundColor Green
    Write-Host "消息: $($rollbackResponse.Message)" -ForegroundColor Cyan
}
catch {
    Write-Host "回滚任务失败: $($_.Exception.Message)" -ForegroundColor Red
}

# 测试4: 刷新医嘱2的执行任务
Write-Host "`n[测试4] 刷新医嘱ID=2的执行任务..." -ForegroundColor Yellow

try {
    $refreshResponse = Invoke-RestMethod -Uri "$baseUrl/api/MedicationOrderTask/2/refresh" -Method POST -Headers $headers
    Write-Host "刷新任务成功!" -ForegroundColor Green
    Write-Host "消息: $($refreshResponse.Message)" -ForegroundColor Cyan
}
catch {
    Write-Host "刷新任务失败: $($_.Exception.Message)" -ForegroundColor Red
}

# 测试5: 测试不存在的医嘱ID
Write-Host "`n[测试5] 测试不存在的医嘱ID=999..." -ForegroundColor Yellow

$generateRequest3 = @{
    medicationOrderId = 999
} | ConvertTo-Json

try {
    $response3 = Invoke-RestMethod -Uri "$baseUrl/api/MedicationOrderTask/generate" -Method POST -Body $generateRequest3 -Headers $headers
    Write-Host "意外成功: $($response3.Message)" -ForegroundColor Yellow
}
catch {
    Write-Host "按预期失败（医嘱不存在）" -ForegroundColor Green
}

Write-Host "`n=== 测试完成 ===" -ForegroundColor Green
Write-Host "请查看Swagger UI以获取更多API详情: http://localhost:5181/swagger" -ForegroundColor Cyan