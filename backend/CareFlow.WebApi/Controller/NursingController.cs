using CareFlow.Application.Interfaces;
using CareFlow.Application.DTOs.Nursing; // 引用你新定义的 DTO
using CareFlow.Application.Services.Scheduling;
using CareFlow.Application.Common;
using CareFlow.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using CareFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;
using CareFlow.Core.Models.Nursing;
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
        private readonly IBarcodeMatchingService _barcodeMatchingService;
        private readonly IBarcodeService _barcodeService;

        // 构造函数注入服务
        public NursingController(
            IVitalSignService vitalSignService, 
            DailyTaskGeneratorService taskGenerator,
            ApplicationDbContext context,
            TaskDelayCalculator delayCalculator,
            IBarcodeMatchingService barcodeMatchingService,
            IBarcodeService barcodeService)
        {
            _vitalSignService = vitalSignService;
            _taskGenerator = taskGenerator;
            _context = context;
            _delayCalculator = delayCalculator;
            _barcodeMatchingService = barcodeMatchingService;
            _barcodeService = barcodeService;
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
        /// [护士端] 添加护理记录补充说明
        /// </summary>
        /// <param name="dto">补充说明数据</param>
        /// <returns></returns>
        [HttpPost("tasks/supplement")]
        public async Task<IActionResult> AddSupplement([FromBody] AddSupplementDto dto)
        {
            try
            {
                var result = await _vitalSignService.AddSupplementAsync(dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "添加补充说明失败", error = ex.Message });
            }
        }

        /// <summary>
        /// [护士端] 获取护理记录的补充说明列表
        /// </summary>
        /// <param name="taskId">护理任务ID</param>
        /// <returns></returns>
        [HttpGet("tasks/{taskId}/supplements")]
        public async Task<IActionResult> GetSupplements(long taskId)
        {
            try
            {
                var supplements = await _vitalSignService.GetSupplementsAsync(taskId);
                return Ok(supplements);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "获取补充说明失败", error = ex.Message });
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

                // 获取护士信息 - 支持通过 Id(简码如N001) 或 EmployeeNumber(工号如nurse001) 查询
                var nurse = await _context.Nurses
                    .Include(n => n.Department)
                    .FirstOrDefaultAsync(n => n.Id == nurseId || n.EmployeeNumber == nurseId);

                if (nurse == null)
                {
                    return NotFound(new { message = "护士不存在" });
                }
                
                // 使用护士的 Id(简码) 进行后续查询
                var nurseStaffId = nurse.Id;

                // 获取该科室下所有病区的床位ID
                var bedIds = await _context.Beds
                    .Include(b => b.Ward)
                    .Where(b => b.Ward.DepartmentId == nurse.DeptCode)
                    .Select(b => b.Id)
                    .ToListAsync();

                Console.WriteLine($"🔍 查询护士 {nurse.Name}(ID:{nurse.Id}, DeptCode:{nurse.DeptCode}) 的任务");
                Console.WriteLine($"📋 查询范围 UTC: {startOfDay} 到 {endOfDay}");
                Console.WriteLine($"🛏️  该科室床位数: {bedIds.Count}, 床位ID: {string.Join(",", bedIds)}");

                var currentTime = DateTime.UtcNow;
                var allTasks = new List<NurseTaskDto>();

                // 1. 查询护理任务 (NursingTask) - 只查询分配给当前护士的任务
                var nursingTasksQuery = _context.NursingTasks
                    .Include(nt => nt.Patient)
                    .Where(nt => nt.ScheduledTime >= startOfDay &&
                                 nt.ScheduledTime < endOfDay &&
                                 nt.AssignedNurseId == nurseStaffId && // 使用简码进行查询
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
                // 只查询分配给当前护士的任务 (AssignedNurseId == nurseStaffId)，与护理任务保持一致
                var executionTasksQuery = _context.ExecutionTasks
                    .Include(et => et.Patient)
                    .Include(et => et.MedicalOrder)
                    .Where(et => et.PlannedStartTime >= startOfDay &&
                                 et.PlannedStartTime < endOfDay &&
                                 et.AssignedNurseId == nurseStaffId && // 使用简码进行查询
                                 bedIds.Contains(et.Patient.BedId));

                // 如果没有指定状态筛选，默认只返回需要显示的状态
                if (status.HasValue)
                {
                    executionTasksQuery = executionTasksQuery.Where(et => et.Status == status);
                }
                else
                {
                    // 默认只显示：AppliedConfirmed(2)、Pending(3)、InProgress(4)、Completed(5)
                    executionTasksQuery = executionTasksQuery.Where(et => 
                        et.Status == ExecutionTaskStatus.AppliedConfirmed ||
                        et.Status == ExecutionTaskStatus.Pending ||
                        et.Status == ExecutionTaskStatus.InProgress ||
                        et.Status == ExecutionTaskStatus.Completed
                    );
                }

                var executionTasks = await executionTasksQuery.ToListAsync();

                Console.WriteLine($"✅ 查询到 {nursingTasks.Count} 个护理任务，{executionTasks.Count} 个执行任务");
                if (executionTasks.Count == 0)
                {
                    Console.WriteLine($"⚠️  没有找到执行任务，检查查询条件:");
                    Console.WriteLine($"   - AssignedNurseId == {nurseStaffId}");
                    Console.WriteLine($"   - bedIds: {string.Join(",", bedIds)}");
                    Console.WriteLine($"   - PlannedStartTime 范围: {startOfDay} 到 {endOfDay}");
                }

                foreach (var task in executionTasks)
                {
                    var delayStatus = _delayCalculator.CalculateExecutionTaskDelay(task, currentTime);
                    
                    // 获取责任护士信息
                    string? assignedNurseName = null;
                    if (!string.IsNullOrEmpty(task.AssignedNurseId))
                    {
                        var assignedNurse = await _context.Nurses
                            .FirstOrDefaultAsync(n => n.Id == task.AssignedNurseId);
                        assignedNurseName = assignedNurse?.Name;
                    }
                    
                    // 获取实际执行护士信息（如果已有执行人）
                    string? executorNurseName = null;
                    if (!string.IsNullOrEmpty(task.ExecutorStaffId))
                    {
                        var executorNurse = await _context.Nurses
                            .FirstOrDefaultAsync(n => n.Id == task.ExecutorStaffId);
                        executorNurseName = executorNurse?.Name;
                    }
                    
                    // 提取医嘱类型和标题信息（从DataPayload或MedicalOrder）
                    string orderTypeName = "执行任务";
                    string taskTitle = task.Category.ToString();
                    
                    if (task.MedicalOrder != null)
                    {
                        // 根据医嘱类型确定显示名称
                        orderTypeName = task.MedicalOrder.OrderType switch
                        {
                            "MedicationOrder" => "药品医嘱",
                            "SurgicalOrder" => "手术医嘱",
                            "InspectionOrder" => "检查医嘱",
                            "OperationOrder" => "操作医嘱",
                            _ => "医嘱任务"
                        };
                        
                        // 尝试从DataPayload解析标题
                        if (!string.IsNullOrEmpty(task.DataPayload))
                        {
                            try
                            {
                                var payloadData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(task.DataPayload);
                                if (payloadData != null && payloadData.ContainsKey("title"))
                                {
                                    taskTitle = payloadData["title"].ToString() ?? taskTitle;
                                }
                            }
                            catch
                            {
                                // 解析失败时保持默认值
                            }
                        }
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
                        AssignedNurseId = task.AssignedNurseId, // 使用责任护士
                        AssignedNurseName = assignedNurseName,
                        ExecutorNurseId = task.ExecutorStaffId,  // 添加实际执行护士
                        ExecutorNurseName = executorNurseName,    // 添加实际执行护士名称
                        OrderTypeName = orderTypeName,            // 医嘱类型名称
                        TaskTitle = taskTitle,                    // 任务标题
                        
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
                    nurseId = nurseStaffId, // 返回护士简码
                    employeeNumber = nurse.EmployeeNumber, // 返回工号
                    nurseName = nurse.Name, // 返回护士姓名
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
                            
                            resultPayload = System.Text.Json.JsonSerializer.Serialize(resultData, JsonConfig.DefaultOptions);
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

        // ==================== ExecutionTask 操作接口 ====================

        /// <summary>
        /// [护士端] 开始执行任务
        /// </summary>
        [HttpPost("execution-tasks/{id}/start")]
        public async Task<IActionResult> StartExecutionTask(long id, [FromBody] StartExecutionTaskDto dto)
        {
            try
            {
                // 获取护士信息（支持工号或简码）
                var nurse = await _context.Nurses
                    .FirstOrDefaultAsync(n => n.Id == dto.NurseId || n.EmployeeNumber == dto.NurseId);
                
                if (nurse == null)
                {
                    return NotFound(new { message = "护士不存在" });
                }
                
                var nurseStaffId = nurse.Id;

                // 查询任务并加锁（使用 EF Core 的乐观并发）
                var task = await _context.ExecutionTasks
                    .Include(t => t.Patient)
                    .Include(t => t.MedicalOrder)
                        .ThenInclude(m => m.Items)
                            .ThenInclude(i => i.Drug)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (task == null)
                {
                    return NotFound(new { message = "任务不存在" });
                }

                // 状态校验：只能从 AppliedConfirmed 或 Pending 状态开始
                if (task.Status != ExecutionTaskStatus.AppliedConfirmed && 
                    task.Status != ExecutionTaskStatus.Pending)
                {
                    return BadRequest(new { message = $"任务状态不允许开始执行，当前状态: {task.Status}" });
                }

                // 更新任务状态
                task.ExecutorStaffId = nurseStaffId;
                task.ActualStartTime = DateTime.UtcNow;
                task.Status = ExecutionTaskStatus.InProgress;
                task.LastModifiedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "任务已开始",
                    taskId = task.Id,
                    status = task.Status,
                    actualStartTime = task.ActualStartTime,
                    executorName = nurse.Name
                });
            }
            catch (DbUpdateConcurrencyException)
            {
                return Conflict(new { message = "任务状态已被其他操作修改，请刷新后重试" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "开始任务失败", error = ex.Message });
            }
        }

        /// <summary>
        /// [护士端] 完成/结束执行任务
        /// 业务流程：
        /// 1. Immediate(即刻执行)：  Pending(3) → Completed(5)，一次点击确认完成
        /// 2. Duration(持续任务)：    Pending(3) → InProgress(4)，然后 InProgress(4) → Completed(5)
        /// 3. ResultPending(结果待定)：Pending(3) → InProgress(4)，然后 InProgress(4) → Completed(5) + ResultPayload
        /// 4. 其他类别：TODO - 暂未实现
        /// </summary>
        [HttpPost("execution-tasks/{id}/complete")]
        public async Task<IActionResult> CompleteExecutionTask(long id, [FromBody] CompleteExecutionTaskDto dto)
        {
            try
            {
                // 调试日志
                Console.WriteLine($"[CompleteExecutionTask] 开始处理 - TaskId: {id}, NurseId: {dto.NurseId}");
                
                // 获取护士信息 - 先加载到内存再过滤（避免 ToString() 在 SQL 中不被支持）
                var nurse = await _context.Nurses.ToListAsync();
                var foundNurse = nurse.FirstOrDefault(n => 
                    n.Id == dto.NurseId || 
                    n.EmployeeNumber == dto.NurseId ||
                    n.IdCard == dto.NurseId ||
                    n.Name == dto.NurseId);
                
                if (foundNurse == null)
                {
                    // 如果没找到护士，返回更详细的错误信息
                    Console.WriteLine($"[CompleteExecutionTask] 护士未找到 - NurseId: {dto.NurseId}");
                    return NotFound(new { 
                        message = $"护士不存在，请确认护士ID或员工号: {dto.NurseId}",
                        nurseIdUsed = dto.NurseId
                    });
                }
                
                Console.WriteLine($"[CompleteExecutionTask] 护士已找到 - Id: {foundNurse.Id}, Name: {foundNurse.Name}");
                
                var nurseStaffId = foundNurse.Id;

                // 查询任务
                var task = await _context.ExecutionTasks
                    .Include(t => t.Patient)
                    .Include(t => t.MedicalOrder)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (task == null)
                {
                    return NotFound(new { message = "任务不存在" });
                }

                // 根据任务类别和当前状态决定转换路径
                ExecutionTaskStatus targetStatus;
                string actionDescription;

                // ==================== Immediate 类别 ====================
                if (task.Category == TaskCategory.Immediate)
                {
                    // 从 Pending(3) 直接到 Completed(5)
                    if (task.Status != ExecutionTaskStatus.Pending && 
                        task.Status != ExecutionTaskStatus.AppliedConfirmed)
                    {
                        return BadRequest(new { message = $"Immediate 任务只能从待执行或已确认状态完成，当前状态: {task.Status}" });
                    }

                    targetStatus = ExecutionTaskStatus.Completed;
                    actionDescription = "已完成";
                }
                // ==================== Duration 类别 ====================
                else if (task.Category == TaskCategory.Duration)
                {
                    // 从 Pending(3) 到 InProgress(4)，或从 InProgress(4) 到 Completed(5)
                    if (task.Status == ExecutionTaskStatus.Pending || 
                        task.Status == ExecutionTaskStatus.AppliedConfirmed)
                    {
                        targetStatus = ExecutionTaskStatus.InProgress;
                        actionDescription = "已开始执行，待结束";
                    }
                    else if (task.Status == ExecutionTaskStatus.InProgress)
                    {
                        targetStatus = ExecutionTaskStatus.Completed;
                        actionDescription = "已结束执行";
                    }
                    else
                    {
                        return BadRequest(new { message = $"Duration 任务状态不允许完成，当前状态: {task.Status}" });
                    }
                }
                // ==================== ResultPending 类别 ====================
                else if (task.Category == TaskCategory.ResultPending)
                {
                    // 从 Pending(3) 到 InProgress(4)，或从 InProgress(4) 到 Completed(5)（需要 ResultPayload）
                    if (task.Status == ExecutionTaskStatus.Pending || 
                        task.Status == ExecutionTaskStatus.AppliedConfirmed)
                    {
                        targetStatus = ExecutionTaskStatus.InProgress;
                        actionDescription = "已开始执行，待录入结果";
                    }
                    else if (task.Status == ExecutionTaskStatus.InProgress)
                    {
                        // 需要验证 ResultPayload
                        if (string.IsNullOrEmpty(dto.ResultPayload))
                        {
                            return BadRequest(new { message = "ResultPending 类别的任务完成时必须提供执行结果" });
                        }
                        
                        targetStatus = ExecutionTaskStatus.Completed;
                        actionDescription = "已完成并录入结果";
                    }
                    else
                    {
                        return BadRequest(new { message = $"ResultPending 任务状态不允许完成，当前状态: {task.Status}" });
                    }
                }
                // ==================== Verification 类别（核对类） ====================
                else if (task.Category == TaskCategory.Verification)
                {
                    // 从 Pending(3) 直接到 Completed(5)，一步完成
                    if (task.Status != ExecutionTaskStatus.Pending && 
                        task.Status != ExecutionTaskStatus.AppliedConfirmed)
                    {
                        return BadRequest(new { message = $"Verification 任务只能从待执行或已确认状态完成，当前状态: {task.Status}" });
                    }

                    targetStatus = ExecutionTaskStatus.Completed;
                    actionDescription = "核对已完成";
                }
                // ==================== 其他类别（暂未实现） ====================
                else
                {
                    // TODO: DataCollection, ApplicationWithPrint 的具体流程待定义
                    return BadRequest(new { message = $"任务类别 {task.Category} 的完成流程暂未实现，请联系管理员" });
                }

                // 首次开始执行任务时，设置执行者和开始时间
                if (task.Status == ExecutionTaskStatus.Pending || 
                    task.Status == ExecutionTaskStatus.AppliedConfirmed)
                {
                    task.ExecutorStaffId = nurseStaffId;
                    task.ActualStartTime = DateTime.UtcNow;
                }

                // 更新任务信息 - 处理备注
                if (!string.IsNullOrEmpty(dto.ResultPayload))
                {
                    // 对于 Duration 和 ResultPending，如果是第二次调用，需要追加备注
                    if ((task.Category == TaskCategory.Duration || task.Category == TaskCategory.ResultPending || task.Category == TaskCategory.Verification) &&
                        targetStatus == ExecutionTaskStatus.Completed &&
                        !string.IsNullOrEmpty(task.ResultPayload))
                    {
                        // 已经有备注，追加新的
                        task.ResultPayload = task.ResultPayload + "\n" + dto.ResultPayload;
                    }
                    else
                    {
                        // 第一次调用或覆盖
                        task.ResultPayload = dto.ResultPayload;
                    }
                }

                // 如果转换到 Completed 状态，设置完成信息
                if (targetStatus == ExecutionTaskStatus.Completed)
                {
                    task.ActualEndTime = DateTime.UtcNow;
                    task.CompleterNurseId = nurseStaffId;
                }

                task.Status = targetStatus;
                task.LastModifiedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // ==================== 检查医嘱是否所有任务都完成了 ====================
                // 如果当前任务完成了，检查该医嘱下的所有任务是否都完成
                if (targetStatus == ExecutionTaskStatus.Completed && task.MedicalOrderId > 0)
                {
                    var medicalOrderId = task.MedicalOrderId;
                    
                    // 获取该医嘱下所有的执行任务
                    var allTasksForOrder = await _context.ExecutionTasks
                        .Where(t => t.MedicalOrderId == medicalOrderId)
                        .ToListAsync();

                    // 检查是否所有任务都完成了（状态为 Completed）
                    var allCompleted = allTasksForOrder.Count > 0 && 
                                       allTasksForOrder.All(t => t.Status == ExecutionTaskStatus.Completed);

                    if (allCompleted)
                    {
                        // 获取医嘱对象，更新其状态（支持所有继承自 MedicalOrder 的类型）
                        var medicalOrder = await _context.Set<CareFlow.Core.Models.Medical.MedicalOrder>()
                            .FirstOrDefaultAsync(o => o.Id == medicalOrderId);
                        
                        if (medicalOrder != null && 
                            medicalOrder.Status != OrderStatus.Completed && 
                            medicalOrder.Status != OrderStatus.Stopped && 
                            medicalOrder.Status != OrderStatus.Cancelled)
                        {
                            medicalOrder.Status = OrderStatus.Completed;
                            medicalOrder.CompletedAt = DateTime.UtcNow;
                            await _context.SaveChangesAsync();
                            
                            Console.WriteLine($"[CompleteExecutionTask] 医嘱 {medicalOrderId} 下所有任务已完成，医嘱状态已更新为 Completed");
                        }
                    }
                }

                return Ok(new
                {
                    message = actionDescription,
                    taskId = task.Id,
                    category = task.Category.ToString(),
                    status = task.Status,
                    actualStartTime = task.ActualStartTime,
                    actualEndTime = task.ActualEndTime,
                    executorName = foundNurse.Name,
                    nextAction = targetStatus == ExecutionTaskStatus.InProgress 
                        ? (task.Category == TaskCategory.ResultPending ? "请点击\"结束任务\"并录入执行结果" : "请点击\"结束任务\"") 
                        : "任务已完成"
                });
            }
            catch (DbUpdateConcurrencyException)
            {
                return Conflict(new { message = "任务状态已被其他操作修改，请刷新后重试" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CompleteExecutionTask] 异常发生: {ex.GetType().Name}");
                Console.WriteLine($"[CompleteExecutionTask] 错误消息: {ex.Message}");
                Console.WriteLine($"[CompleteExecutionTask] 堆栈跟踪: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"[CompleteExecutionTask] 内部异常: {ex.InnerException.Message}");
                }
                return StatusCode(500, new { message = "完成任务失败", error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        /// <summary>
        /// [护士端] 取消执行任务
        /// </summary>
        [HttpPost("execution-tasks/{id}/cancel")]
        public async Task<IActionResult> CancelExecutionTask(long id, [FromBody] CancelExecutionTaskDto dto)
        {
            try
            {
                // 获取护士信息
                var nurse = await _context.Nurses
                    .FirstOrDefaultAsync(n => n.Id == dto.NurseId || n.EmployeeNumber == dto.NurseId);
                
                if (nurse == null)
                {
                    return NotFound(new { message = "护士不存在" });
                }

                // 查询任务
                var task = await _context.ExecutionTasks
                    .Include(t => t.Patient)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (task == null)
                {
                    return NotFound(new { message = "任务不存在" });
                }

                // 状态校验：不能取消已完成或已停止的任务
                if (task.Status == ExecutionTaskStatus.Completed || 
                    task.Status == ExecutionTaskStatus.Stopped)
                {
                    return BadRequest(new { message = $"任务状态不允许取消，当前状态: {task.Status}" });
                }

                // 验证取消理由
                if (string.IsNullOrWhiteSpace(dto.CancelReason))
                {
                    return BadRequest(new { message = "请填写取消理由" });
                }

                // 更新任务状态
                task.Status = ExecutionTaskStatus.Stopped;
                task.ExceptionReason = dto.CancelReason;
                task.LastModifiedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "任务已取消",
                    taskId = task.Id,
                    status = task.Status,
                    cancelReason = task.ExceptionReason
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "取消任务失败", error = ex.Message });
            }
        }

        /// <summary>
        /// [护士端] 获取执行任务详情（用于任务扫码）
        /// </summary>
        [HttpGet("execution-tasks/{id}")]
        public async Task<IActionResult> GetExecutionTaskDetail(long id)
        {
            try
            {
                var task = await _context.ExecutionTasks
                    .Include(t => t.Patient)
                    .Include(t => t.MedicalOrder)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (task == null)
                {
                    return NotFound(new { message = "任务不存在" });
                }

                // 构建返回的任务信息
                var taskInfo = new
                {
                    id = task.Id,
                    patientId = task.PatientId,
                    patientName = task.Patient?.Name,
                    bedId = task.Patient?.BedId,
                    category = (int)task.Category,
                    categoryName = GetTaskCategoryName(task.Category),
                    status = task.Status.ToString(),
                    plannedStartTime = task.PlannedStartTime,
                    actualStartTime = task.ActualStartTime,
                    medicalOrderId = task.MedicalOrderId,
                    executorStaffId = task.ExecutorStaffId,
                    resultPayload = task.ResultPayload,
                    drugs = GetTaskDrugs(task) // 用于核对类任务
                };

                return Ok(taskInfo);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "获取任务详情失败", error = ex.Message });
            }
        }

        /// <summary>
        /// [护士端] 上传任务条形码图片进行识别
        /// </summary>
        [HttpPost("barcode/recognize-task")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> RecognizeTaskBarcode(IFormFile taskBarcodeImage)
        {
            try
            {
                if (taskBarcodeImage == null || taskBarcodeImage.Length == 0)
                {
                    return BadRequest(new { message = "请上传条形码图片", success = false });
                }

                using (var stream = taskBarcodeImage.OpenReadStream())
                {
                    // 调用IBarcodeService识别条形码
                    var recognitionResult = _barcodeService.RecognizeBarcode(stream);
                    
                    if (recognitionResult == null)
                    {
                        return BadRequest(new 
                        { 
                            message = "条形码识别失败，无法解析条形码内容",
                            success = false,
                            taskId = 0
                        });
                    }

                    // 对于护士端的任务扫码，期望条形码中包含的是ExecutionTask ID
                    if (!long.TryParse(recognitionResult.RecordId, out var taskId))
                    {
                        return BadRequest(new 
                        { 
                            message = $"条形码识别成功，但内容不是有效的任务ID: {recognitionResult.RecordId}",
                            success = false,
                            taskId = 0,
                            decodedValue = recognitionResult.RecordId
                        });
                    }

                    // 验证任务是否存在
                    var executionTask = await _context.ExecutionTasks
                        .Include(t => t.Patient)
                        .FirstOrDefaultAsync(t => t.Id == taskId);

                    if (executionTask == null)
                    {
                        return NotFound(new 
                        { 
                            message = $"任务ID {taskId} 不存在",
                            success = false,
                            taskId = 0
                        });
                    }

                    return Ok(new 
                    { 
                        message = "条形码识别成功",
                        success = true,
                        taskId = taskId,
                        patientName = executionTask.Patient?.Name,
                        category = executionTask.Category,
                        status = executionTask.Status
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new 
                { 
                    message = $"条形码识别异常: {ex.Message}",
                    success = false,
                    taskId = 0
                });
            }
        }

        /// <summary>
        /// [护士端] 验证任务和患者条形码是否匹配
        /// </summary>
        [HttpPost("barcode/validate-patient")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> ValidatePatientBarcode(long taskId, IFormFile taskBarcodeImage, IFormFile patientBarcodeImage)
        {
            try
            {
                if (taskBarcodeImage == null || patientBarcodeImage == null)
                {
                    return BadRequest(new { message = "缺少条形码图片", success = false });
                }

                var task = await _context.ExecutionTasks
                    .Include(t => t.Patient)
                    .Include(t => t.MedicalOrder)
                        .ThenInclude(m => m.Items)
                    .FirstOrDefaultAsync(t => t.Id == taskId);

                if (task == null)
                {
                    return NotFound(new { message = "任务不存在", success = false });
                }

                // 使用IBarcodeService识别两张条形码图片
                using (var taskStream = taskBarcodeImage.OpenReadStream())
                using (var patientStream = patientBarcodeImage.OpenReadStream())
                {
                    try
                    {
                        var taskRecognition = _barcodeService.RecognizeBarcode(taskStream);
                        var patientRecognition = _barcodeService.RecognizeBarcode(patientStream);

                        if (taskRecognition == null || patientRecognition == null)
                        {
                            return BadRequest(new 
                            { 
                                success = false,
                                isMatched = false,
                                message = "条形码识别失败，无法解析条形码内容",
                                taskId = task.Id
                            });
                        }

                        // 验证任务ID是否匹配
                        if (!long.TryParse(taskRecognition.RecordId, out var decodedTaskId) || decodedTaskId != taskId)
                        {
                            return BadRequest(new 
                            { 
                                success = false,
                                isMatched = false,
                                message = "任务条形码不匹配",
                                taskId = task.Id
                            });
                        }

                        // 验证患者ID是否匹配
                        if (patientRecognition.RecordId != task.PatientId)
                        {
                            return BadRequest(new 
                            { 
                                success = false,
                                isMatched = false,
                                message = $"患者条形码不匹配，扫描的患者ID: {patientRecognition.RecordId}，任务患者ID: {task.PatientId}",
                                taskId = task.Id
                            });
                        }

                        // 验证成功
                        return Ok(new 
                        { 
                            success = true,
                            isMatched = true,
                            message = "患者验证成功",
                            taskId = task.Id,
                            patientId = task.PatientId,
                            patientName = task.Patient?.Name
                        });
                    }
                    catch (Exception ex)
                    {
                        return BadRequest(new 
                        { 
                            success = false,
                            isMatched = false,
                            message = $"条形码识别异常: {ex.Message}",
                            taskId = task.Id
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"患者条形码验证异常: {ex.Message}", success = false });
            }
        }

        /// <summary>
        /// [护士端] 验证任务和药品条形码是否匹配
        /// </summary>
        [HttpPost("barcode/validate-drug")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> ValidateDrugBarcode(long taskId, IFormFile taskBarcodeImage, IFormFile drugBarcodeImage)
        {
            try
            {
                if (taskBarcodeImage == null || drugBarcodeImage == null)
                {
                    return BadRequest(new { message = "缺少条形码图片", success = false });
                }

                var task = await _context.ExecutionTasks
                    .Include(t => t.Patient)
                    .Include(t => t.MedicalOrder)
                        .ThenInclude(m => m.Items)
                            .ThenInclude(i => i.Drug)
                    .FirstOrDefaultAsync(t => t.Id == taskId);

                if (task == null)
                {
                    return NotFound(new { message = "任务不存在", success = false });
                }

                // 使用IBarcodeService识别药品条形码
                using (var taskStream = taskBarcodeImage.OpenReadStream())
                using (var drugStream = drugBarcodeImage.OpenReadStream())
                {
                    try
                    {
                        var taskRecognition = _barcodeService.RecognizeBarcode(taskStream);
                        var drugRecognition = _barcodeService.RecognizeBarcode(drugStream);

                        if (taskRecognition == null || drugRecognition == null)
                        {
                            return BadRequest(new 
                            { 
                                success = false,
                                isMatched = false,
                                message = "条形码识别失败，无法解析条形码内容",
                                taskId = task.Id
                            });
                        }

                        // 验证任务ID是否匹配
                        if (!long.TryParse(taskRecognition.RecordId, out var decodedTaskId) || decodedTaskId != taskId)
                        {
                            return BadRequest(new 
                            { 
                                success = false,
                                isMatched = false,
                                message = "任务条形码不匹配",
                                taskId = task.Id
                            });
                        }

                        // 解析期望药品清单（从 DataPayload 或 MedicalOrder.Items）
                        var expectedDrugIds = new List<string>();
                        var expectedDrugs = new List<object>();

                        // 1) 尝试从 DataPayload 中读取 MedicationInfo.Items 或 Items 中的 drugId
                        try
                        {
                            if (!string.IsNullOrEmpty(task.DataPayload))
                            {
                                using var doc = JsonDocument.Parse(task.DataPayload);
                                var root = doc.RootElement;

                                // MedicationInfo.Items (生成任务时采用此结构)
                                if (root.TryGetProperty("MedicationInfo", out var medInfo))
                                {
                                    if (medInfo.ValueKind == JsonValueKind.Object && medInfo.TryGetProperty("Items", out var medItems) && medItems.ValueKind == JsonValueKind.Array)
                                    {
                                        foreach (var it in medItems.EnumerateArray())
                                        {
                                            string? drugId = null;
                                            string? drugName = null;
                                            if (it.TryGetProperty("DrugId", out var d1)) drugId = d1.GetString();
                                            if (it.TryGetProperty("drugId", out var d2) && drugId == null) drugId = d2.GetString();
                                            if (it.TryGetProperty("DrugName", out var dn1)) drugName = dn1.GetString();
                                            if (it.TryGetProperty("drugName", out var dn2) && drugName == null) drugName = dn2.GetString();
                                            if (!string.IsNullOrEmpty(drugId))
                                            {
                                                expectedDrugIds.Add(drugId!);
                                                expectedDrugs.Add(new { drugId, drugName });
                                            }
                                        }
                                    }
                                }

                                // root-level Items (备用)
                                if (!expectedDrugIds.Any() && root.TryGetProperty("Items", out var itemsEl) && itemsEl.ValueKind == JsonValueKind.Array)
                                {
                                    foreach (var it in itemsEl.EnumerateArray())
                                    {
                                        string? drugId = null;
                                        string? drugName = null;
                                        if (it.TryGetProperty("DrugId", out var d1)) drugId = d1.GetString();
                                        if (it.TryGetProperty("drugId", out var d2) && drugId == null) drugId = d2.GetString();
                                        if (it.TryGetProperty("DrugName", out var dn1)) drugName = dn1.GetString();
                                        if (it.TryGetProperty("drugName", out var dn2) && drugName == null) drugName = dn2.GetString();
                                        if (!string.IsNullOrEmpty(drugId))
                                        {
                                            expectedDrugIds.Add(drugId!);
                                            expectedDrugs.Add(new { drugId, drugName });
                                        }
                                    }
                                }
                            }
                        }
                        catch
                        {
                            // 忽略解析错误，后续会尝试从 MedicalOrder.Items 读取
                        }

                        // 2) 如果仍然没有，尝试从关联的 MedicalOrder.Items 获取 DrugId
                        if (!expectedDrugIds.Any() && task.MedicalOrder != null && task.MedicalOrder.Items != null)
                        {
                            foreach (var it in task.MedicalOrder.Items)
                            {
                                if (!string.IsNullOrEmpty(it.DrugId))
                                {
                                    expectedDrugIds.Add(it.DrugId);
                                    expectedDrugs.Add(new { drugId = it.DrugId, drugName = it.Drug?.GenericName ?? it.Drug?.TradeName });
                                }
                            }
                        }

                        var expectedCount = expectedDrugIds.Count;

                        // 3) 读取并更新 ResultPayload 中的已扫描列表（字段名: scannedDrugIds）
                        var scanned = new List<string>();
                        try
                        {
                            if (!string.IsNullOrEmpty(task.ResultPayload))
                            {
                                using var doc = JsonDocument.Parse(task.ResultPayload);
                                var root = doc.RootElement;
                                if (root.TryGetProperty("scannedDrugIds", out var scannedEl) && scannedEl.ValueKind == JsonValueKind.Array)
                                {
                                    foreach (var s in scannedEl.EnumerateArray())
                                    {
                                        var v = s.GetString();
                                        if (!string.IsNullOrEmpty(v)) scanned.Add(v!);
                                    }
                                }
                            }
                        }
                        catch
                        {
                            // ignore
                        }

                        var scannedDrugId = drugRecognition.RecordId;

                        // 已经扫描过
                        if (scanned.Contains(scannedDrugId))
                        {
                            var progressPercent = expectedCount == 0 ? 0 : (int)Math.Round((double)scanned.Count / expectedCount * 100);
                            return Ok(new
                            {
                                success = true,
                                isMatched = expectedDrugIds.Contains(scannedDrugId),
                                message = "该药品已扫描",
                                taskId = task.Id,
                                scannedDrugId,
                                scannedCount = scanned.Count,
                                expectedCount,
                                progress = progressPercent
                            });
                        }

                        // 如果期望清单为空，则无法验证，只记录扫描到的条码并返回
                        if (expectedCount == 0)
                        {
                            scanned.Add(scannedDrugId);
                        }
                        else
                        {
                            // 验证是否在期望清单中
                            if (expectedDrugIds.Contains(scannedDrugId))
                            {
                                scanned.Add(scannedDrugId);
                            }
                            else
                            {
                                return BadRequest(new
                                {
                                    success = false,
                                    isMatched = false,
                                    message = $"扫描的药品条码不在期望清单中: {scannedDrugId}",
                                    taskId = task.Id
                                });
                            }
                        }

                        // 将更新后的 scanned 列表写回 ResultPayload
                        try
                        {
                            var newResult = new Dictionary<string, object?>();
                            newResult["scannedDrugIds"] = scanned;
                            // 保留其他可能的结果字段? 这里只覆盖/设置扫描列表
                            task.ResultPayload = JsonSerializer.Serialize(newResult);
                            task.LastModifiedAt = DateTime.UtcNow;
                            await _context.SaveChangesAsync();
                        }
                        catch (Exception ex)
                        {
                            _ = ex; // 忽略保存错误，但应记录在日志中（此处简化）
                        }

                        var progress = expectedCount == 0 ? 100 : (int)Math.Round((double)scanned.Count / expectedCount * 100);

                        return Ok(new
                        {
                            success = true,
                            isMatched = true,
                            message = "药品验证成功",
                            taskId = task.Id,
                            scannedDrugId = scannedDrugId,
                            scannedCount = scanned.Count,
                            expectedCount,
                            progress
                        });
                    }
                    catch (Exception ex)
                    {
                        return BadRequest(new 
                        { 
                            success = false,
                            isMatched = false,
                            message = $"条形码识别异常: {ex.Message}",
                            taskId = task.Id
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"药品条形码验证异常: {ex.Message}", success = false });
            }
        }

        /// <summary>
        /// [护士端] 更新执行任务状态（用于任务扫码）
        /// </summary>
        [HttpPost("execution-tasks/{id}/update-status")]
        public async Task<IActionResult> UpdateExecutionTaskStatus(long id, [FromBody] UpdateExecutionTaskStatusDto dto)
        {
            try
            {
                // 获取护士信息 - 更灵活的查询方式
                var nurse = await _context.Nurses
                    .FirstOrDefaultAsync(n => 
                        n.Id.ToString() == dto.NurseId || 
                        n.EmployeeNumber == dto.NurseId ||
                        n.IdCard == dto.NurseId ||
                        n.Name == dto.NurseId);
                
                if (nurse == null)
                {
                    return NotFound(new { 
                        message = $"护士不存在，请确认护士ID或员工号: {dto.NurseId}",
                        nurseIdUsed = dto.NurseId
                    });
                }

                // 查询任务
                var task = await _context.ExecutionTasks
                    .Include(t => t.Patient)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (task == null)
                {
                    return NotFound(new { message = "任务不存在" });
                }

                // 解析目标状态
                if (!Enum.TryParse<ExecutionTaskStatus>(dto.Status, out var targetStatus))
                {
                    return BadRequest(new { message = $"无效的状态: {dto.Status}" });
                }

                // 更新任务状态和执行者信息
                task.ExecutorStaffId = nurse.Id;
                task.ActualStartTime ??= DateTime.UtcNow;
                task.Status = targetStatus;
                task.LastModifiedAt = DateTime.UtcNow;

                // 如果提供了结果，更新结果字段
                if (!string.IsNullOrEmpty(dto.ResultPayload))
                {
                    task.ResultPayload = dto.ResultPayload;
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "任务状态已更新",
                    taskId = task.Id,
                    status = task.Status,
                    actualStartTime = task.ActualStartTime
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "更新任务状态失败", error = ex.Message });
            }
        }

        /// <summary>
        /// 获取任务相关的药品列表（用于Verification类任务）
        /// </summary>
        private List<dynamic> GetTaskDrugs(ExecutionTask task)
        {
            var drugs = new List<dynamic>();

            // 尝试从 DataPayload 中解析期望药品
            var expectedDrugIds = new List<string>();
            var expectedDrugNames = new Dictionary<string, string?>();

            try
            {
                if (!string.IsNullOrEmpty(task.DataPayload))
                {
                    Console.WriteLine($"[GetTaskDrugs] 尝试从 DataPayload 读取药品 (TaskId: {task.Id})");
                    using var doc = JsonDocument.Parse(task.DataPayload);
                    var root = doc.RootElement;

                    // 尝试方式1：MedicationInfo.Items（给药任务格式）
                    if (root.TryGetProperty("MedicationInfo", out var medInfo) && medInfo.ValueKind == JsonValueKind.Object)
                    {
                        Console.WriteLine($"[GetTaskDrugs] 找到 MedicationInfo");
                        if (medInfo.TryGetProperty("Items", out var medItems) && medItems.ValueKind == JsonValueKind.Array)
                        {
                            Console.WriteLine($"[GetTaskDrugs] 找到 MedicationInfo.Items，项数: {medItems.GetArrayLength()}");
                            foreach (var it in medItems.EnumerateArray())
                            {
                                string? did = null;
                                string? dname = null;
                                if (it.TryGetProperty("DrugId", out var d1)) did = d1.GetString();
                                if (it.TryGetProperty("drugId", out var d2) && did == null) did = d2.GetString();
                                if (it.TryGetProperty("DrugName", out var dn1)) dname = dn1.GetString();
                                if (it.TryGetProperty("drugName", out var dn2) && dname == null) dname = dn2.GetString();
                                if (!string.IsNullOrEmpty(did))
                                {
                                    expectedDrugIds.Add(did!);
                                    expectedDrugNames[did!] = dname;
                                    Console.WriteLine($"[GetTaskDrugs] 提取药品: {did} - {dname}");
                                }
                            }
                        }
                    }

                    // 尝试方式2：medications 数组（取药任务格式）
                    if (!expectedDrugIds.Any() && root.TryGetProperty("medications", out var medications) && medications.ValueKind == JsonValueKind.Array)
                    {
                        Console.WriteLine($"[GetTaskDrugs] 找到 medications 数组，项数: {medications.GetArrayLength()}");
                        foreach (var med in medications.EnumerateArray())
                        {
                            string? did = null;
                            string? dname = null;
                            if (med.TryGetProperty("drugId", out var d1)) did = d1.GetString();
                            if (med.TryGetProperty("DrugId", out var d2) && did == null) did = d2.GetString();
                            if (med.TryGetProperty("drugName", out var dn1)) dname = dn1.GetString();
                            if (med.TryGetProperty("DrugName", out var dn2) && dname == null) dname = dn2.GetString();
                            if (!string.IsNullOrEmpty(did))
                            {
                                expectedDrugIds.Add(did!);
                                expectedDrugNames[did!] = dname;
                                Console.WriteLine($"[GetTaskDrugs] 提取药品: {did} - {dname}");
                            }
                        }
                    }

                    // 尝试方式3：root-level Items（核对清单格式，items 中的对象包含 drugId）
                    if (!expectedDrugIds.Any() && root.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array)
                    {
                        Console.WriteLine($"[GetTaskDrugs] 找到 root-level items，项数: {items.GetArrayLength()}");
                        foreach (var it in items.EnumerateArray())
                        {
                            string? did = null;
                            string? dname = null;
                            // 检查 items 中的 drugId 字段（取药清单中有）
                            if (it.TryGetProperty("drugId", out var d1)) did = d1.GetString();
                            if (it.TryGetProperty("DrugId", out var d2) && did == null) did = d2.GetString();
                            // drugName 可能需要从 text 字段中提取
                            if (it.TryGetProperty("text", out var textEl))
                            {
                                var text = textEl.GetString();
                                // text 格式: "核对药品：药品名称 剂量" - 提取"核对药品："后的部分
                                if (text != null && text.Contains("核对药品："))
                                {
                                    dname = text.Substring(text.IndexOf("核对药品：") + 5).Trim();
                                }
                            }
                            if (string.IsNullOrEmpty(dname))
                            {
                                if (it.TryGetProperty("drugName", out var dn1)) dname = dn1.GetString();
                                if (it.TryGetProperty("DrugName", out var dn2) && dname == null) dname = dn2.GetString();
                            }
                            if (!string.IsNullOrEmpty(did))
                            {
                                expectedDrugIds.Add(did!);
                                expectedDrugNames[did!] = dname;
                                Console.WriteLine($"[GetTaskDrugs] 提取药品: {did} - {dname}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GetTaskDrugs] 解析 DataPayload 失败: {ex.Message}");
                // ignore
            }

            // 如果仍无，从关联的 MedicalOrder.Items 中读取（需要传入时包含 MedicalOrder）
            if (!expectedDrugIds.Any() && task.MedicalOrder != null && task.MedicalOrder.Items != null)
            {
                Console.WriteLine($"[GetTaskDrugs] DataPayload 中未找到药品，尝试从 MedicalOrder.Items 读取 (共 {task.MedicalOrder.Items.Count()} 项)");
                foreach (var it in task.MedicalOrder.Items)
                {
                    if (!string.IsNullOrEmpty(it.DrugId))
                    {
                        expectedDrugIds.Add(it.DrugId);
                        expectedDrugNames[it.DrugId] = it.Drug?.GenericName ?? it.Drug?.TradeName;
                        Console.WriteLine($"[GetTaskDrugs] 提取药品: {it.DrugId} - {it.Drug?.GenericName ?? it.Drug?.TradeName}");
                    }
                }
            }

            if (expectedDrugIds.Any())
            {
                Console.WriteLine($"[GetTaskDrugs] 成功获取 {expectedDrugIds.Count} 个期望药品");
            }
            else
            {
                Console.WriteLine($"[GetTaskDrugs] 未能获取期望药品清单 (TaskId: {task.Id})");
            }

            // 读取已扫描的列表
            var scanned = new HashSet<string>();
            try
            {
                if (!string.IsNullOrEmpty(task.ResultPayload))
                {
                    using var doc = JsonDocument.Parse(task.ResultPayload);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("scannedDrugIds", out var scannedEl) && scannedEl.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var s in scannedEl.EnumerateArray())
                        {
                            var v = s.GetString();
                            if (!string.IsNullOrEmpty(v)) scanned.Add(v!);
                        }
                    }
                }
            }
            catch
            {
                // ignore
            }

            // 构建返回对象
            foreach (var did in expectedDrugIds)
            {
                drugs.Add(new {
                    drugId = did,
                    drugName = expectedDrugNames.ContainsKey(did) ? expectedDrugNames[did] : null,
                    required = true,
                    scanned = scanned.Contains(did)
                });
            }

            return drugs;
        }

        /// <summary>
        /// 获取任务类别的显示名称
        /// </summary>
        private string GetTaskCategoryName(TaskCategory category)
        {
            return category switch
            {
                TaskCategory.Immediate => "立即执行",
                TaskCategory.Duration => "持续执行",
                TaskCategory.ResultPending => "结果等待",
                TaskCategory.DataCollection => "护理记录",
                TaskCategory.Verification => "核对",
                TaskCategory.ApplicationWithPrint => "申请打印",
                _ => "未知"
            };
        }

        /// <summary>
        /// 判断任务类别是否需要 ResultPayload
        /// TODO: 待定义 DataCollection, Verification, ApplicationWithPrint 的流程
        /// </summary>
        private bool RequiresResultPayload(TaskCategory category)
        {
            return category == TaskCategory.ResultPending || 
                   category == TaskCategory.DataCollection ||
                   category == TaskCategory.Verification;
        }
    }
}
