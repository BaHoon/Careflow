using CareFlow.Application.Interfaces;
using CareFlow.Application.DTOs.Nursing; // 引用你新定义的 DTO
using CareFlow.Application.Services.Nursing; // 引用 Service
using CareFlow.Application.Services.Scheduling;
using Microsoft.AspNetCore.Mvc;
using CareFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;
using CareFlow.Core.Models.Medical;
using CareFlow.Core.Enums;

namespace CareFlow.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NursingController : ControllerBase
    {
        private readonly IVitalSignService _vitalSignService;
        private readonly DailyTaskGeneratorService _taskGenerator;
        private readonly ApplicationDbContext _context;
        private readonly TaskDelayCalculator _delayCalculator;

        // 构造函数注入服务
        public NursingController(
            IVitalSignService vitalSignService, 
            DailyTaskGeneratorService taskGenerator,
            ApplicationDbContext context,
            TaskDelayCalculator delayCalculator)
        {
            _vitalSignService = vitalSignService;
            _taskGenerator = taskGenerator;
            _context = context;
            _delayCalculator = delayCalculator;
        }

        /// <summary>
        /// [管理端/定时任务] 生成今日护理任务（为所有在院患者根据护理等级生成）
        /// </summary>
        /// <returns></returns>
        [HttpPost("tasks/generate")]
        public async Task<IActionResult> GenerateDailyTasks()
        {
            try
            {
                // 为所有在院患者生成今天的任务（根据护理等级）
                await _taskGenerator.GenerateTodayTasksAsync();
                
                return Ok(new { message = "今日护理任务已生成" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "生成任务失败", error = ex.Message });
            }
        }

        /// <summary>
        /// [护士端] 提交体征数据并完成任务
        /// </summary>
        /// <param name="dto">提交的数据包</param>
        /// <returns></returns>
        [HttpPost("tasks/submit")]
        public async Task<IActionResult> SubmitVitalSigns([FromBody] NursingTaskSubmissionDto dto)
        {
            Console.WriteLine($"📥 收到提交请求: TaskId={dto?.TaskId}, NurseId={dto?.CurrentNurseId}");
            
            if (dto == null) 
            {
                Console.WriteLine("❌ DTO为空");
                return BadRequest(new { message = "提交数据不能为空" });
            }

            // 验证必填字段
            if (dto.TaskId == 0)
            {
                Console.WriteLine("❌ TaskId为0");
                return BadRequest(new { message = "任务ID不能为空" });
            }

            if (string.IsNullOrEmpty(dto.CurrentNurseId))
            {
                Console.WriteLine("❌ CurrentNurseId为空");
                return BadRequest(new { message = "护士ID不能为空" });
            }

            try
            {
                Console.WriteLine($"✅ 开始保存护理记录...");
                await _vitalSignService.SubmitVitalSignsAsync(dto);
                Console.WriteLine($"✅ 护理记录保存成功");
                return Ok(new { message = "执行成功，数据已录入，任务状态已更新" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 保存失败: {ex.Message}");
                Console.WriteLine($"堆栈: {ex.StackTrace}");
                return StatusCode(500, new { message = "提交失败", error = ex.Message, details = ex.InnerException?.Message });
            }
        }

        /// <summary>
        /// [护士端] 取消护理任务
        /// </summary>
        /// <param name="taskId">任务ID</param>
        /// <param name="nurseId">护士ID</param>
        /// <param name="cancelReason">取消理由</param>
        /// <returns></returns>
        [HttpPost("tasks/{taskId}/cancel")]
        public async Task<IActionResult> CancelNursingTask(long taskId, [FromQuery] string nurseId, [FromQuery] string? cancelReason = null)
        {
            Console.WriteLine($"🔵 收到取消任务请求 - TaskId: {taskId}, NurseId: {nurseId}, Reason: {cancelReason}");
            
            if (string.IsNullOrEmpty(nurseId))
            {
                Console.WriteLine($"❌ 护士ID为空");
                return BadRequest(new { message = "护士ID不能为空" });
            }

            try
            {
                await _vitalSignService.CancelNursingTaskAsync(taskId, nurseId, cancelReason ?? "未填写取消理由");
                Console.WriteLine($"✅ 任务 {taskId} 取消成功");
                return Ok(new { message = "任务已取消" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 取消任务失败: {ex.Message}");
                Console.WriteLine($"堆栈: {ex.StackTrace}");
                return StatusCode(500, new { message = "取消任务失败", error = ex.Message });
            }
        }

        /// <summary>
        /// [护士端] 获取病区床位概览
        /// </summary>
        /// <param name="wardId">病区ID（可选）</param>
        /// <param name="departmentId">科室ID（可选，返回该科室所有病区）</param>
        /// <returns></returns>
        [HttpGet("ward-overview")]
        public async Task<IActionResult> GetWardOverview(string? wardId = null, string? departmentId = null)
        {
            try
            {
                // 如果两个都没有，返回错误
                if (string.IsNullOrEmpty(wardId) && string.IsNullOrEmpty(departmentId))
                {
                    return BadRequest("必须提供 wardId 或 departmentId");
                }

                // 如果传入了科室ID，返回该科室所有病区的分组数据
                if (!string.IsNullOrEmpty(departmentId))
                {
                    return await GetDepartmentOverview(departmentId);
                }

                // 查询单个病区的床位信息
                var bedsQuery = _context.Beds
                    .Include(b => b.Ward)
                        .ThenInclude(w => w.Department)
                    .Where(b => b.WardId == wardId)
                    .AsQueryable();

                var beds = await bedsQuery.OrderBy(b => b.Id).ToListAsync();

                if (!beds.Any())
                {
                    return NotFound("未找到床位信息");
                }

                var currentTime = DateTime.UtcNow;
                var today = DateOnly.FromDateTime(currentTime);

                // 查询床位对应的患者
                var bedIds = beds.Select(b => b.Id).ToList();
                var patients = await _context.Patients
                    .Include(p => p.AttendingDoctor)
                    .Where(p => bedIds.Contains(p.BedId))
                    .ToListAsync();

                // 创建床位-患者映射
                var bedPatientMap = patients.ToDictionary(p => p.BedId, p => p);

                // 获取所有患者ID
                var patientIds = patients.Select(p => p.Id).ToList();

                // 批量查询今日手术医嘱
                var todaySurgeries = await _context.SurgicalOrders
                    .Where(so => patientIds.Contains(so.PatientId) &&
                                 so.ScheduleTime.Date == currentTime.Date &&
                                 (so.Status == OrderStatus.Accepted || so.Status == OrderStatus.PendingReceive))
                    .Select(so => so.PatientId)
                    .Distinct()
                    .ToListAsync();

                // 批量查询待执行任务
                var pendingTasks = await _context.ExecutionTasks
                    .Where(et => patientIds.Contains(et.PatientId) &&
                                 et.Status == ExecutionTaskStatus.Pending)
                    .GroupBy(et => et.PatientId)
                    .Select(g => new { PatientId = g.Key, Count = g.Count() })
                    .ToListAsync();

                // 批量查询超时任务
                var overdueTasks = await _context.ExecutionTasks
                    .Where(et => patientIds.Contains(et.PatientId) &&
                                 et.Status == ExecutionTaskStatus.Pending &&
                                 et.PlannedStartTime < currentTime)
                    .GroupBy(et => et.PatientId)
                    .Select(g => new { PatientId = g.Key, Count = g.Count() })
                    .ToListAsync();

                // 批量查询体征异常（最近一次体温）
                // 简化查询：先获取所有异常记录，再在内存中过滤
                var recentTime = currentTime.AddHours(-24);
                var abnormalVitalSigns = await _context.VitalSignsRecords
                    .Where(vs => patientIds.Contains(vs.PatientId) &&
                                 vs.RecordTime >= recentTime &&
                                 (vs.Temperature < 36.0m || vs.Temperature > 38.0m))
                    .Select(vs => vs.PatientId)
                    .Distinct()
                    .ToListAsync();

                // 构建床位概览DTO
                var bedOverviews = beds.Select(bed => 
                {
                    var patient = bedPatientMap.ContainsKey(bed.Id) ? bedPatientMap[bed.Id] : null;
                    
                    return new BedOverviewDto
                    {
                        BedId = bed.Id,
                        BedStatus = bed.Status,
                        WardId = bed.WardId,
                        Patient = patient == null ? null : new PatientSummaryDto
                        {
                            Id = patient.Id,
                            Name = patient.Name,
                            Gender = patient.Gender,
                            Age = patient.Age,
                            NursingGrade = (int)patient.NursingGrade,
                            BedId = patient.BedId
                        },
                        StatusFlags = patient == null ? new BedStatusFlagsDto() : new BedStatusFlagsDto
                        {
                            HasSurgeryToday = todaySurgeries.Contains(patient.Id),
                            HasAbnormalVitalSign = abnormalVitalSigns.Contains(patient.Id),
                            HasNewOrder = false, // 可以根据实际业务逻辑实现
                            HasPendingTask = pendingTasks.Any(pt => pt.PatientId == patient.Id),
                            HasOverdueTask = overdueTasks.Any(ot => ot.PatientId == patient.Id)
                        }
                    };
                }).ToList();

                // 获取病区/科室信息
                var firstBed = beds.First();
                var ward = firstBed.Ward;
                
                if (ward == null)
                {
                    return StatusCode(500, new { message = "床位数据异常：缺少病区信息" });
                }
                
                var department = ward.Department;
                
                if (department == null)
                {
                    return StatusCode(500, new { message = "病区数据异常：缺少科室信息" });
                }

                var response = new WardOverviewResponseDto
                {
                    WardId = ward.Id,
                    WardName = ward.Id, // 可以添加 WardName 字段到模型
                    DepartmentId = department.Id,
                    DepartmentName = department.DeptName,
                    Beds = bedOverviews,
                    TotalBeds = beds.Count,
                    OccupiedBeds = beds.Count(b => b.Status == "占用"),
                    AvailableBeds = beds.Count(b => b.Status == "空闲")
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    message = "获取病区概览失败", 
                    error = ex.Message,
                    stackTrace = ex.StackTrace,
                    innerException = ex.InnerException?.Message
                });
            }
        }

        /// <summary>
        /// 获取科室所有病区的概览（内部辅助方法）
        /// </summary>
        private async Task<IActionResult> GetDepartmentOverview(string departmentId)
        {
            // 获取该科室下所有病区
            var wards = await _context.Wards
                .Include(w => w.Department)
                .Where(w => w.DepartmentId == departmentId)
                .ToListAsync();

            if (!wards.Any())
            {
                return NotFound(new { message = "该科室下没有病区" });
            }

            var wardOverviews = new List<Dictionary<string, object>>();
            int totalBedsCount = 0;
            int totalOccupiedCount = 0;
            int totalAvailableCount = 0;

            foreach (var ward in wards)
            {
                // 查询该病区的床位
                var beds = await _context.Beds
                    .Where(b => b.WardId == ward.Id)
                    .OrderBy(b => b.Id)
                    .ToListAsync();

                if (!beds.Any()) continue;

                var currentTime = DateTime.UtcNow;

                // 查询床位对应的患者
                var bedIds = beds.Select(b => b.Id).ToList();
                var patients = await _context.Patients
                    .Include(p => p.AttendingDoctor)
                    .Where(p => bedIds.Contains(p.BedId))
                    .ToListAsync();

                var bedPatientMap = patients.ToDictionary(p => p.BedId, p => p);
                var patientIds = patients.Select(p => p.Id).ToList();

                // 批量查询今日手术医嘱
                var todaySurgeries = await _context.SurgicalOrders
                    .Where(so => patientIds.Contains(so.PatientId) &&
                                 so.ScheduleTime.Date == currentTime.Date &&
                                 (so.Status == OrderStatus.Accepted || so.Status == OrderStatus.PendingReceive))
                    .Select(so => so.PatientId)
                    .Distinct()
                    .ToListAsync();

                // 批量查询待执行任务
                var pendingTasks = await _context.ExecutionTasks
                    .Where(et => patientIds.Contains(et.PatientId) && et.Status == ExecutionTaskStatus.Pending)
                    .GroupBy(et => et.PatientId)
                    .Select(g => new { PatientId = g.Key, Count = g.Count() })
                    .ToListAsync();

                // 批量查询超时任务
                var overdueTasks = await _context.ExecutionTasks
                    .Where(et => patientIds.Contains(et.PatientId) &&
                                 et.Status == ExecutionTaskStatus.Pending &&
                                 et.PlannedStartTime < currentTime)
                    .GroupBy(et => et.PatientId)
                    .Select(g => new { PatientId = g.Key, Count = g.Count() })
                    .ToListAsync();

                // 批量查询体征异常
                var recentTime = currentTime.AddHours(-24);
                var abnormalVitalSigns = await _context.VitalSignsRecords
                    .Where(vs => patientIds.Contains(vs.PatientId) &&
                                 vs.RecordTime >= recentTime &&
                                 (vs.Temperature < 36.0m || vs.Temperature > 38.0m))
                    .Select(vs => vs.PatientId)
                    .Distinct()
                    .ToListAsync();

                // 构建床位概览
                var bedOverviews = beds.Select(bed =>
                {
                    var patient = bedPatientMap.ContainsKey(bed.Id) ? bedPatientMap[bed.Id] : null;

                    return new BedOverviewDto
                    {
                        BedId = bed.Id,
                        BedStatus = bed.Status,
                        WardId = bed.WardId,
                        Patient = patient == null ? null : new PatientSummaryDto
                        {
                            Id = patient.Id,
                            Name = patient.Name,
                            Gender = patient.Gender,
                            Age = patient.Age,
                            NursingGrade = (int)patient.NursingGrade,
                            BedId = patient.BedId
                        },
                        StatusFlags = patient == null ? new BedStatusFlagsDto() : new BedStatusFlagsDto
                        {
                            HasSurgeryToday = todaySurgeries.Contains(patient.Id),
                            HasAbnormalVitalSign = abnormalVitalSigns.Contains(patient.Id),
                            HasNewOrder = false,
                            HasPendingTask = pendingTasks.Any(pt => pt.PatientId == patient.Id),
                            HasOverdueTask = overdueTasks.Any(ot => ot.PatientId == patient.Id)
                        }
                    };
                }).ToList();

                var wardBedCount = beds.Count;
                var wardOccupiedCount = beds.Count(b => b.Status == "占用");
                var wardAvailableCount = beds.Count(b => b.Status == "空闲");

                totalBedsCount += wardBedCount;
                totalOccupiedCount += wardOccupiedCount;
                totalAvailableCount += wardAvailableCount;

                wardOverviews.Add(new Dictionary<string, object>
                {
                    { "wardId", ward.Id },
                    { "wardName", ward.Id },
                    { "beds", bedOverviews },
                    { "totalBeds", wardBedCount },
                    { "occupiedBeds", wardOccupiedCount },
                    { "availableBeds", wardAvailableCount }
                });
            }

            var department = wards.First().Department;

            return Ok(new
            {
                departmentId = department.Id,
                departmentName = department.DeptName,
                wards = wardOverviews,
                totalBeds = totalBedsCount,
                occupiedBeds = totalOccupiedCount,
                availableBeds = totalAvailableCount
            });
        }

        /// <summary>
        /// [护士端] 获取我的待办任务列表（包含护理任务和医嘱执行任务）
        /// </summary>
        /// <param name="nurseId">护士ID</param>
        /// <param name="date">查询日期（可选，默认今天）</param>
        /// <param name="status">任务状态筛选（可选）</param>
        /// <returns></returns>
        [HttpGet("my-tasks")]
        public async Task<IActionResult> GetMyTasks(
            string nurseId, 
            DateTime? date = null, 
            ExecutionTaskStatus? status = null)
        {
            try
            {
                // 使用中国时区处理日期
                var chinaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("China Standard Time");
                var targetDate = date ?? TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, chinaTimeZone);
                
                // 获取当天中国时间的开始和结束（转换为UTC用于数据库查询）
                var chinaDateOnly = DateOnly.FromDateTime(targetDate);
                var chinaStartOfDay = chinaDateOnly.ToDateTime(TimeOnly.MinValue);
                var chinaEndOfDay = chinaDateOnly.AddDays(1).ToDateTime(TimeOnly.MinValue);
                
                // 转换为UTC时间（数据库存储的是UTC）
                var startOfDay = TimeZoneInfo.ConvertTimeToUtc(chinaStartOfDay, chinaTimeZone);
                var endOfDay = TimeZoneInfo.ConvertTimeToUtc(chinaEndOfDay, chinaTimeZone);

                // 获取护士所属科室
                var nurse = await _context.Nurses
                    .Include(n => n.Department)
                    .FirstOrDefaultAsync(n => n.Id == nurseId);

                if (nurse == null)
                {
                    return NotFound(new { message = "护士不存在" });
                }

                // 获取该科室下所有病区的床位ID
                var bedIds = await _context.Beds
                    .Include(b => b.Ward)
                    .Where(b => b.Ward.DepartmentId == nurse.DeptCode)
                    .Select(b => b.Id)
                    .ToListAsync();

                var currentTime = DateTime.UtcNow;
                var allTasks = new List<NurseTaskDto>();

                // 1. 查询护理任务 (NursingTask) - 只查询分配给当前护士的任务
                var nursingTasksQuery = _context.NursingTasks
                    .Include(nt => nt.Patient)
                    .Where(nt => nt.ScheduledTime >= startOfDay &&
                                 nt.ScheduledTime < endOfDay &&
                                 nt.AssignedNurseId == nurseId && // 只查询分配给当前护士的任务
                                 bedIds.Contains(nt.Patient.BedId));

                if (status.HasValue)
                {
                    nursingTasksQuery = nursingTasksQuery.Where(nt => nt.Status == status);
                }

                var nursingTasks = await nursingTasksQuery.ToListAsync();

                foreach (var task in nursingTasks)
                {
                    var delayStatus = _delayCalculator.CalculateNursingTaskDelay(task, currentTime);
                    
                    // 获取负责护士信息
                    string? assignedNurseName = null;
                    if (!string.IsNullOrEmpty(task.AssignedNurseId))
                    {
                        var assignedNurse = await _context.Nurses
                            .FirstOrDefaultAsync(n => n.Id == task.AssignedNurseId);
                        assignedNurseName = assignedNurse?.Name;
                    }
                    
                    // 获取实际执行护士信息
                    string? executorNurseName = null;
                    if (!string.IsNullOrEmpty(task.ExecutorNurseId))
                    {
                        var executorNurse = await _context.Nurses
                            .FirstOrDefaultAsync(n => n.Id == task.ExecutorNurseId);
                        executorNurseName = executorNurse?.Name;
                    }
                    
                    // 如果任务已完成，获取体征数据和护理笔记，并序列化为ResultPayload
                    string? resultPayload = null;
                    if (task.Status == ExecutionTaskStatus.Completed)
                    {
                        Console.WriteLine($"🔍 任务 {task.Id} 已完成，查询护理数据...");
                        
                        var vitalRecord = await _context.VitalSignsRecords
                            .FirstOrDefaultAsync(v => v.NursingTaskId == task.Id);
                        
                        Console.WriteLine($"  体征记录: {(vitalRecord != null ? "找到" : "未找到")}");
                        
                        var careNote = await _context.NursingCareNotes
                            .FirstOrDefaultAsync(n => n.NursingTaskId == task.Id);
                        
                        Console.WriteLine($"  护理笔记: {(careNote != null ? "找到" : "未找到")}");
                        
                        if (vitalRecord != null)
                        {
                            var resultData = new Dictionary<string, object?>
                            {
                                ["temperature"] = vitalRecord.Temperature,
                                ["tempType"] = vitalRecord.TempType,
                                ["pulse"] = vitalRecord.Pulse,
                                ["respiration"] = vitalRecord.Respiration,
                                ["sysBp"] = vitalRecord.SysBp,
                                ["diaBp"] = vitalRecord.DiaBp,
                                ["spo2"] = vitalRecord.Spo2,
                                ["painScore"] = vitalRecord.PainScore,
                                ["weight"] = vitalRecord.Weight > 0 ? vitalRecord.Weight : null,
                                ["intervention"] = !string.IsNullOrEmpty(vitalRecord.Intervention) ? vitalRecord.Intervention : null
                            };
                            
                            // 添加护理笔记数据（如果有）
                            if (careNote != null)
                            {
                                Console.WriteLine($"  添加护理笔记数据:");
                                Console.WriteLine($"    Consciousness: {careNote.Consciousness}");
                                Console.WriteLine($"    SkinCondition: {careNote.SkinCondition}");
                                Console.WriteLine($"    Content: {careNote.Content}");
                                Console.WriteLine($"    IntakeVolume: {careNote.IntakeVolume}");
                                Console.WriteLine($"    OutputVolume: {careNote.OutputVolume}");
                                
                                resultData["consciousness"] = careNote.Consciousness;
                                resultData["skinCondition"] = careNote.SkinCondition;
                                resultData["intakeVolume"] = careNote.IntakeVolume > 0 ? careNote.IntakeVolume : null;
                                resultData["intakeType"] = !string.IsNullOrEmpty(careNote.IntakeType) ? careNote.IntakeType : null;
                                resultData["outputVolume"] = careNote.OutputVolume > 0 ? careNote.OutputVolume : null;
                                resultData["outputType"] = !string.IsNullOrEmpty(careNote.OutputType) ? careNote.OutputType : null;
                                resultData["noteContent"] = !string.IsNullOrEmpty(careNote.Content) ? careNote.Content : null;
                                resultData["healthEducation"] = !string.IsNullOrEmpty(careNote.HealthEducation) ? careNote.HealthEducation : null;
                            }
                            
                            resultPayload = System.Text.Json.JsonSerializer.Serialize(resultData);
                            Console.WriteLine($"  序列化后的ResultPayload: {resultPayload}");
                        }
                    }
                    
                    Console.WriteLine($"📋 任务 {task.Id}: ExecutorNurseId={task.ExecutorNurseId}, ExecutorNurseName={executorNurseName}");
                    
                    allTasks.Add(new NurseTaskDto
                    {
                        Id = task.Id,
                        TaskSource = "NursingTask", // 标识任务来源
                        PatientId = task.PatientId,
                        PatientName = task.Patient?.Name ?? "未知",
                        BedId = task.Patient?.BedId ?? "未知",
                        Category = task.TaskType, // Routine, ReMeasure
                        PlannedStartTime = task.ScheduledTime,
                        ActualStartTime = task.ExecuteTime,
                        Status = task.Status,
                        AssignedNurseId = task.AssignedNurseId,
                        AssignedNurseName = assignedNurseName,
                        ExecutorNurseId = task.ExecutorNurseId,  // 添加实际执行护士
                        ExecutorNurseName = executorNurseName,    // 添加实际执行护士名称
                        ResultPayload = resultPayload,             // 添加护理数据
                        
                        // 延迟状态字段
                        DelayMinutes = delayStatus.DelayMinutes,
                        AllowedDelayMinutes = delayStatus.AllowedDelayMinutes,
                        ExcessDelayMinutes = delayStatus.ExcessDelayMinutes,
                        SeverityLevel = delayStatus.SeverityLevel,
                        
                        IsOverdue = task.Status == ExecutionTaskStatus.Pending && delayStatus.ExcessDelayMinutes > 0,
                        IsDueSoon = task.Status == ExecutionTaskStatus.Pending && 
                                    task.ScheduledTime >= currentTime && 
                                    task.ScheduledTime <= currentTime.AddMinutes(30)
                    });
                }

                // 2. 查询医嘱执行任务 (ExecutionTask)
                // 医嘱执行任务：查询该护士已经开始执行的任务 (ExecutorStaffId == nurseId)
                // 或者待执行的任务（任何护士都可以执行）TODO：这里后续需要修改权限逻辑
                var executionTasksQuery = _context.ExecutionTasks
                    .Include(et => et.Patient)
                    .Include(et => et.MedicalOrder)
                    .Where(et => et.PlannedStartTime >= startOfDay &&
                                 et.PlannedStartTime < endOfDay &&
                                 bedIds.Contains(et.Patient.BedId) &&
                                 (et.ExecutorStaffId == nurseId || et.ExecutorStaffId == null)); // 我执行的或待执行的

                if (status.HasValue)
                {
                    executionTasksQuery = executionTasksQuery.Where(et => et.Status == status);
                }

                var executionTasks = await executionTasksQuery.ToListAsync();

                foreach (var task in executionTasks)
                {
                    var delayStatus = _delayCalculator.CalculateExecutionTaskDelay(task, currentTime);
                    
                    // 获取执行护士信息（如果已有执行人）
                    string? executorNurseName = null;
                    if (!string.IsNullOrEmpty(task.ExecutorStaffId))
                    {
                        var executorNurse = await _context.Nurses
                            .FirstOrDefaultAsync(n => n.Id == task.ExecutorStaffId);
                        executorNurseName = executorNurse?.Name;
                    }
                    
                    allTasks.Add(new NurseTaskDto
                    {
                        Id = task.Id,
                        TaskSource = "ExecutionTask", // 标识任务来源
                        MedicalOrderId = task.MedicalOrderId,
                        PatientId = task.PatientId,
                        PatientName = task.Patient?.Name ?? "未知",
                        BedId = task.Patient?.BedId ?? "未知",
                        Category = task.Category.ToString(),
                        PlannedStartTime = task.PlannedStartTime,
                        ActualStartTime = task.ActualStartTime,
                        ActualEndTime = task.ActualEndTime,
                        Status = task.Status,
                        DataPayload = task.DataPayload,
                        ResultPayload = task.ResultPayload,
                        AssignedNurseId = task.ExecutorStaffId, // 医嘱执行任务使用ExecutorStaffId
                        AssignedNurseName = executorNurseName,
                        
                        // 延迟状态字段
                        DelayMinutes = delayStatus.DelayMinutes,
                        AllowedDelayMinutes = delayStatus.AllowedDelayMinutes,
                        ExcessDelayMinutes = delayStatus.ExcessDelayMinutes,
                        SeverityLevel = delayStatus.SeverityLevel,
                        
                        IsOverdue = task.Status == ExecutionTaskStatus.Pending && delayStatus.ExcessDelayMinutes > 0,
                        IsDueSoon = task.Status == ExecutionTaskStatus.Pending && 
                                    task.PlannedStartTime >= currentTime && 
                                    task.PlannedStartTime <= currentTime.AddMinutes(30)
                    });
                }

                // 按计划时间排序
                var sortedTasks = allTasks.OrderBy(t => t.PlannedStartTime).ToList();

                return Ok(new
                {
                    nurseId,
                    date = targetDate.Date,
                    tasks = sortedTasks,
                    totalCount = sortedTasks.Count,
                    overdueCount = sortedTasks.Count(t => t.IsOverdue),
                    dueSoonCount = sortedTasks.Count(t => t.IsDueSoon),
                    pendingCount = sortedTasks.Count(t => t.Status == ExecutionTaskStatus.Pending),
                    completedCount = sortedTasks.Count(t => t.Status == ExecutionTaskStatus.Completed)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "获取任务列表失败", error = ex.Message });
            }
        }

        /// <summary>
        /// [护士端] 获取指定患者的所有护理任务（护理记录功能使用）
        /// </summary>
        /// <param name="patientId">患者ID</param>
        /// <param name="date">查询日期（可选，默认今天）</param>
        /// <returns></returns>
        [HttpGet("patient-nursing-tasks")]
        public async Task<IActionResult> GetPatientNursingTasks(string patientId, DateTime? date = null)
        {
            try
            {
                // 使用中国时区处理日期
                var chinaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("China Standard Time");
                var targetDate = date ?? TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, chinaTimeZone);
                
                // 获取当天中国时间的开始和结束（转换为UTC用于数据库查询）
                var chinaDateOnly = DateOnly.FromDateTime(targetDate);
                var chinaStartOfDay = chinaDateOnly.ToDateTime(TimeOnly.MinValue);
                var chinaEndOfDay = chinaDateOnly.AddDays(1).ToDateTime(TimeOnly.MinValue);
                
                // 转换为UTC时间（数据库存储的是UTC）
                var startOfDay = TimeZoneInfo.ConvertTimeToUtc(chinaStartOfDay, chinaTimeZone);
                var endOfDay = TimeZoneInfo.ConvertTimeToUtc(chinaEndOfDay, chinaTimeZone);

                var currentTime = DateTime.UtcNow;

                // 查询该患者的所有护理任务
                var nursingTasks = await _context.NursingTasks
                    .Include(nt => nt.Patient)
                    .Where(nt => nt.PatientId == patientId &&
                                 nt.ScheduledTime >= startOfDay &&
                                 nt.ScheduledTime < endOfDay)
                    .OrderBy(nt => nt.ScheduledTime)
                    .ToListAsync();

                // 打印调试信息
                Console.WriteLine($"查询到患者 {patientId} 的任务数: {nursingTasks.Count}");
                foreach (var task in nursingTasks)
                {
                    Console.WriteLine($"  任务ID: {task.Id}, 时间: {task.ScheduledTime}, 负责人: {task.AssignedNurseId}");
                }

                var taskDtos = new List<NurseTaskDto>();

                foreach (var task in nursingTasks)
                {
                    var delayStatus = _delayCalculator.CalculateNursingTaskDelay(task, currentTime);
                    
                    // 获取负责护士信息
                    string? assignedNurseName = null;
                    if (!string.IsNullOrEmpty(task.AssignedNurseId))
                    {
                        var assignedNurse = await _context.Nurses
                            .FirstOrDefaultAsync(n => n.Id == task.AssignedNurseId);
                        assignedNurseName = assignedNurse?.Name;
                    }
                    
                    // 获取实际执行护士信息
                    string? executorNurseName = null;
                    if (!string.IsNullOrEmpty(task.ExecutorNurseId))
                    {
                        var executorNurse = await _context.Nurses
                            .FirstOrDefaultAsync(n => n.Id == task.ExecutorNurseId);
                        executorNurseName = executorNurse?.Name;
                    }
                    
                    // 如果任务已完成，获取体征数据和护理笔记，并序列化为ResultPayload
                    string? resultPayload = null;
                    if (task.Status == ExecutionTaskStatus.Completed)
                    {
                        var vitalRecord = await _context.VitalSignsRecords
                            .FirstOrDefaultAsync(v => v.NursingTaskId == task.Id);
                        
                        var careNote = await _context.NursingCareNotes
                            .FirstOrDefaultAsync(n => n.NursingTaskId == task.Id);
                        
                        if (vitalRecord != null)
                        {
                            var resultData = new Dictionary<string, object?>
                            {
                                ["temperature"] = vitalRecord.Temperature,
                                ["tempType"] = vitalRecord.TempType,
                                ["pulse"] = vitalRecord.Pulse,
                                ["respiration"] = vitalRecord.Respiration,
                                ["sysBp"] = vitalRecord.SysBp,
                                ["diaBp"] = vitalRecord.DiaBp,
                                ["spo2"] = vitalRecord.Spo2,
                                ["painScore"] = vitalRecord.PainScore,
                                ["weight"] = vitalRecord.Weight > 0 ? vitalRecord.Weight : null,
                                ["intervention"] = !string.IsNullOrEmpty(vitalRecord.Intervention) ? vitalRecord.Intervention : null
                            };
                            
                            // 添加护理笔记数据（如果有）
                            if (careNote != null)
                            {
                                resultData["consciousness"] = careNote.Consciousness;
                                resultData["skinCondition"] = careNote.SkinCondition;
                                resultData["intakeVolume"] = careNote.IntakeVolume > 0 ? careNote.IntakeVolume : null;
                                resultData["intakeType"] = !string.IsNullOrEmpty(careNote.IntakeType) ? careNote.IntakeType : null;
                                resultData["outputVolume"] = careNote.OutputVolume > 0 ? careNote.OutputVolume : null;
                                resultData["outputType"] = !string.IsNullOrEmpty(careNote.OutputType) ? careNote.OutputType : null;
                                resultData["noteContent"] = !string.IsNullOrEmpty(careNote.Content) ? careNote.Content : null;
                                resultData["healthEducation"] = !string.IsNullOrEmpty(careNote.HealthEducation) ? careNote.HealthEducation : null;
                            }
                            
                            resultPayload = System.Text.Json.JsonSerializer.Serialize(resultData);
                        }
                    }
                    
                    taskDtos.Add(new NurseTaskDto
                    {
                        Id = task.Id,
                        TaskSource = "NursingTask",
                        PatientId = task.PatientId,
                        PatientName = task.Patient?.Name ?? "未知",
                        BedId = task.Patient?.BedId ?? "未知",
                        Category = task.TaskType,
                        PlannedStartTime = task.ScheduledTime,
                        ActualStartTime = task.ExecuteTime,
                        Status = task.Status,
                        AssignedNurseId = task.AssignedNurseId,
                        AssignedNurseName = assignedNurseName,
                        ExecutorNurseId = task.ExecutorNurseId,
                        ExecutorNurseName = executorNurseName,
                        ResultPayload = resultPayload,
                        
                        // 延迟状态字段
                        DelayMinutes = delayStatus.DelayMinutes,
                        AllowedDelayMinutes = delayStatus.AllowedDelayMinutes,
                        ExcessDelayMinutes = delayStatus.ExcessDelayMinutes,
                        SeverityLevel = delayStatus.SeverityLevel,
                        
                        IsOverdue = task.Status == ExecutionTaskStatus.Pending && delayStatus.ExcessDelayMinutes > 0,
                        IsDueSoon = task.Status == ExecutionTaskStatus.Pending && 
                                    task.ScheduledTime >= currentTime && 
                                    task.ScheduledTime <= currentTime.AddMinutes(30)
                    });
                }

                return Ok(new
                {
                    patientId,
                    date = targetDate.Date,
                    tasks = taskDtos,
                    totalCount = taskDtos.Count,
                    pendingCount = taskDtos.Count(t => t.Status == ExecutionTaskStatus.Pending),
                    completedCount = taskDtos.Count(t => t.Status == ExecutionTaskStatus.Completed)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "获取患者护理任务失败", error = ex.Message });
            }
        }
    }
}