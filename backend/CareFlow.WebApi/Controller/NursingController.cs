using CareFlow.Application.Interfaces;
using CareFlow.Application.DTOs.Nursing; // 引用你新定义的 DTO
using CareFlow.Application.Services.Nursing; // 引用 Service
using CareFlow.Application.Services.Scheduling;
using Microsoft.AspNetCore.Mvc;
using CareFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;
using CareFlow.Core.Models.Medical;

namespace CareFlow.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NursingController : ControllerBase
    {
        private readonly IVitalSignService _vitalSignService;
        private readonly NursingTaskGenerator _taskGenerator;
        private readonly ApplicationDbContext _context;
        private readonly TaskDelayCalculator _delayCalculator;

        // 构造函数注入服务
        public NursingController(
            IVitalSignService vitalSignService, 
            NursingTaskGenerator taskGenerator,
            ApplicationDbContext context,
            TaskDelayCalculator delayCalculator)
        {
            _vitalSignService = vitalSignService;
            _taskGenerator = taskGenerator;
            _context = context;
            _delayCalculator = delayCalculator;
        }

        /// <summary>
        /// [管理端/定时任务] 生成今日护理任务
        /// </summary>
        /// <param name="deptId">科室ID (如 DEPT001)</param>
        /// <returns></returns>
        [HttpPost("tasks/generate")]
        public async Task<IActionResult> GenerateDailyTasks(string deptId)
        {
            try
            {
                // 生成今天的任务
                var today = DateOnly.FromDateTime(DateTime.Now);
                
                await _taskGenerator.GenerateDailyTasksAsync(deptId, today);
                
                return Ok(new { message = $"科室 {deptId} 的 {today} 护理任务已生成" });
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
            if (dto == null) return BadRequest("提交数据不能为空");

            try
            {
                await _vitalSignService.SubmitVitalSignsAsync(dto);
                return Ok(new { message = "执行成功，数据已录入，任务状态已更新" });
            }
            catch (Exception ex)
            {
                // 生产环境建议记录日志
                return StatusCode(500, new { message = "提交失败", error = ex.Message });
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
                                 (so.Status == "Accepted" || so.Status == "PendingReview"))
                    .Select(so => so.PatientId)
                    .Distinct()
                    .ToListAsync();

                // 批量查询待执行任务
                var pendingTasks = await _context.ExecutionTasks
                    .Where(et => patientIds.Contains(et.PatientId) &&
                                 et.Status == "Pending")
                    .GroupBy(et => et.PatientId)
                    .Select(g => new { PatientId = g.Key, Count = g.Count() })
                    .ToListAsync();

                // 批量查询超时任务
                var overdueTasks = await _context.ExecutionTasks
                    .Where(et => patientIds.Contains(et.PatientId) &&
                                 et.Status == "Pending" &&
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
                                 (so.Status == "Accepted" || so.Status == "PendingReview"))
                    .Select(so => so.PatientId)
                    .Distinct()
                    .ToListAsync();

                // 批量查询待执行任务
                var pendingTasks = await _context.ExecutionTasks
                    .Where(et => patientIds.Contains(et.PatientId) && et.Status == "Pending")
                    .GroupBy(et => et.PatientId)
                    .Select(g => new { PatientId = g.Key, Count = g.Count() })
                    .ToListAsync();

                // 批量查询超时任务
                var overdueTasks = await _context.ExecutionTasks
                    .Where(et => patientIds.Contains(et.PatientId) &&
                                 et.Status == "Pending" &&
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
            string? status = null)
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

                // 1. 查询护理任务 (NursingTask)
                var nursingTasksQuery = _context.NursingTasks
                    .Include(nt => nt.Patient)
                    .Where(nt => nt.ScheduledTime >= startOfDay &&
                                 nt.ScheduledTime < endOfDay &&
                                 bedIds.Contains(nt.Patient.BedId));

                if (!string.IsNullOrEmpty(status))
                {
                    nursingTasksQuery = nursingTasksQuery.Where(nt => nt.Status == status);
                }

                var nursingTasks = await nursingTasksQuery.ToListAsync();

                foreach (var task in nursingTasks)
                {
                    var delayStatus = _delayCalculator.CalculateNursingTaskDelay(task, currentTime);
                    
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
                        
                        // 延迟状态字段
                        DelayMinutes = delayStatus.DelayMinutes,
                        AllowedDelayMinutes = delayStatus.AllowedDelayMinutes,
                        ExcessDelayMinutes = delayStatus.ExcessDelayMinutes,
                        SeverityLevel = delayStatus.SeverityLevel,
                        
                        IsOverdue = task.Status == "Pending" && delayStatus.ExcessDelayMinutes > 0,
                        IsDueSoon = task.Status == "Pending" && 
                                    task.ScheduledTime >= currentTime && 
                                    task.ScheduledTime <= currentTime.AddMinutes(30)
                    });
                }

                // 2. 查询医嘱执行任务 (ExecutionTask)
                var executionTasksQuery = _context.ExecutionTasks
                    .Include(et => et.Patient)
                    .Include(et => et.MedicalOrder)
                    .Where(et => et.PlannedStartTime >= startOfDay &&
                                 et.PlannedStartTime < endOfDay &&
                                 bedIds.Contains(et.Patient.BedId));

                if (!string.IsNullOrEmpty(status))
                {
                    executionTasksQuery = executionTasksQuery.Where(et => et.Status == status);
                }

                var executionTasks = await executionTasksQuery.ToListAsync();

                foreach (var task in executionTasks)
                {
                    var delayStatus = _delayCalculator.CalculateExecutionTaskDelay(task, currentTime);
                    
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
                        
                        // 延迟状态字段
                        DelayMinutes = delayStatus.DelayMinutes,
                        AllowedDelayMinutes = delayStatus.AllowedDelayMinutes,
                        ExcessDelayMinutes = delayStatus.ExcessDelayMinutes,
                        SeverityLevel = delayStatus.SeverityLevel,
                        
                        IsOverdue = task.Status == "Pending" && delayStatus.ExcessDelayMinutes > 0,
                        IsDueSoon = task.Status == "Pending" && 
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
                    nursingTaskCount = nursingTasks.Count,
                    executionTaskCount = executionTasks.Count,
                    overdueCount = sortedTasks.Count(t => t.IsOverdue),
                    dueSoonCount = sortedTasks.Count(t => t.IsDueSoon),
                    pendingCount = sortedTasks.Count(t => t.Status == "Pending"),
                    completedCount = sortedTasks.Count(t => t.Status == "Completed")
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "获取任务列表失败", error = ex.Message });
            }
        }
    }
}