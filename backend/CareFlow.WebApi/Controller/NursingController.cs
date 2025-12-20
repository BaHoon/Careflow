using CareFlow.Application.Interfaces;
using CareFlow.Application.DTOs.Nursing; // 引用你新定义的 DTO
using CareFlow.Application.Services.Nursing; // 引用 Service
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

        // 构造函数注入服务
        public NursingController(
            IVitalSignService vitalSignService, 
            NursingTaskGenerator taskGenerator,
            ApplicationDbContext context)
        {
            _vitalSignService = vitalSignService;
            _taskGenerator = taskGenerator;
            _context = context;
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
        /// <param name="wardId">病区ID</param>
        /// <param name="departmentId">科室ID（可选，用于多病区查询）</param>
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

                // 查询床位信息
                var bedsQuery = _context.Beds
                    .Include(b => b.Ward)
                        .ThenInclude(w => w.Department)
                    .AsQueryable();

                // 根据筛选条件
                if (!string.IsNullOrEmpty(wardId))
                {
                    bedsQuery = bedsQuery.Where(b => b.WardId == wardId);
                }
                else if (!string.IsNullOrEmpty(departmentId))
                {
                    bedsQuery = bedsQuery.Where(b => b.Ward.DepartmentId == departmentId);
                }

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
        /// [护士端] 获取我的待办任务列表
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
                var targetDate = date ?? DateTime.UtcNow;
                var startOfDay = DateTime.SpecifyKind(targetDate.Date, DateTimeKind.Utc);
                var endOfDay = DateTime.SpecifyKind(startOfDay.AddDays(1), DateTimeKind.Utc);

                // 查询任务
                var tasksQuery = _context.ExecutionTasks
                    .Include(et => et.Patient)
                    .Where(et => et.PlannedStartTime >= startOfDay &&
                                 et.PlannedStartTime < endOfDay);

                // 可以根据排班表筛选护士负责的患者，这里简化处理
                // 实际应该根据 NurseRoster 和病区关系来筛选

                if (!string.IsNullOrEmpty(status))
                {
                    tasksQuery = tasksQuery.Where(et => et.Status == status);
                }

                var tasks = await tasksQuery
                    .OrderBy(et => et.PlannedStartTime)
                    .ToListAsync();

                var currentTime = DateTime.UtcNow;

                var nurseTasks = tasks.Select(task => new NurseTaskDto
                {
                    Id = task.Id,
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
                    IsOverdue = task.Status == "Pending" && task.PlannedStartTime < currentTime,
                    IsDueSoon = task.Status == "Pending" && 
                                task.PlannedStartTime >= currentTime && 
                                task.PlannedStartTime <= currentTime.AddMinutes(30)
                }).ToList();

                return Ok(new
                {
                    nurseId,
                    date = targetDate.Date,
                    tasks = nurseTasks,
                    totalCount = nurseTasks.Count,
                    overdueCount = nurseTasks.Count(t => t.IsOverdue),
                    dueSoonCount = nurseTasks.Count(t => t.IsDueSoon),
                    pendingCount = nurseTasks.Count(t => t.Status == "Pending"),
                    completedCount = nurseTasks.Count(t => t.Status == "Completed")
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "获取任务列表失败", error = ex.Message });
            }
        }
    }
}