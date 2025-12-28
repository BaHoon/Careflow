using CareFlow.Application.DTOs.Patient;
using CareFlow.Application.Interfaces;
using CareFlow.Core.Interfaces;
using CareFlow.Core.Enums;
using CareFlow.Core.Models.Organization;
using CareFlow.Core.Models.Space;
using CareFlow.Core.Models.Medical;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CareFlow.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PatientController : ControllerBase
{
    private readonly IRepository<Patient, string> _patientRepository;
    private readonly IRepository<Bed, string> _bedRepository;
    private readonly IRepository<DischargeOrder, long> _dischargeOrderRepository;
    private readonly IDischargeOrderService _dischargeOrderService;
    private readonly INurseAssignmentService _nurseAssignmentService;
    private readonly IRepository<Nurse, string> _nurseRepository;
    private readonly ILogger<PatientController> _logger;
    private readonly ICareFlowDbContext _dbContext;

    public PatientController(
        IRepository<Patient, string> patientRepository,
        IRepository<Bed, string> bedRepository,
        IRepository<DischargeOrder, long> dischargeOrderRepository,
        IDischargeOrderService dischargeOrderService,
        INurseAssignmentService nurseAssignmentService,
        IRepository<Nurse, string> nurseRepository,
        ILogger<PatientController> logger,
        ICareFlowDbContext dbContext)
    {
        _patientRepository = patientRepository;
        _bedRepository = bedRepository;
        _dischargeOrderRepository = dischargeOrderRepository;
        _dischargeOrderService = dischargeOrderService;
        _nurseAssignmentService = nurseAssignmentService;
        _nurseRepository = nurseRepository;
        _logger = logger;
        _dbContext = dbContext;
    }

    /// <summary>
    /// 获取患者列表
    /// </summary>
    /// <param name="departmentId">科室ID（可选）</param>
    /// <param name="wardId">病区ID（可选）</param>
    [HttpGet("list")]
    public async Task<ActionResult<List<PatientListDto>>> GetPatientList(
        [FromQuery] string? departmentId = null,
        [FromQuery] string? wardId = null)
    {
        try
        {
            _logger.LogInformation("获取患者列表，科室: {DepartmentId}, 病区: {WardId}", departmentId, wardId);

            var query = _patientRepository.GetQueryable()
                .Include(p => p.Bed)
                .ThenInclude(b => b.Ward)
                .ThenInclude(w => w.Department)
                .Include(p => p.AttendingDoctor)
                .Where(p => p.Status == PatientStatus.Hospitalized || p.Status == PatientStatus.PendingDischarge); // 排除已出院患者

            // 根据departmentId和wardId过滤
            if (!string.IsNullOrEmpty(wardId))
            {
                // 如果指定了病区，先检查病区是否属于指定科室（如果有科室参数）
                if (!string.IsNullOrEmpty(departmentId))
                {
                    // 验证病区是否属于该科室
                    var wardExists = await query
                        .AnyAsync(p => p.Bed.WardId == wardId && p.Bed.Ward.Department.Id == departmentId);
                    
                    if (!wardExists)
                    {
                        _logger.LogWarning("病区 {WardId} 不属于科室 {DepartmentId}", wardId, departmentId);
                        return BadRequest(new { message = $"病区 {wardId} 不属于科室 {departmentId}" });
                    }
                    
                    // 同时过滤科室和病区
                    query = query.Where(p => p.Bed.Ward.Department.Id == departmentId && p.Bed.WardId == wardId);
                }
                else
                {
                    // 只过滤病区
                    query = query.Where(p => p.Bed.WardId == wardId);
                }
            }
            else if (!string.IsNullOrEmpty(departmentId))
            {
                // 只指定了科室，返回该科室所有患者
                query = query.Where(p => p.Bed.Ward.Department.Id == departmentId);
            }

            var patients = await query.ToListAsync();

            var result = patients.Select(p => new PatientListDto
            {
                Id = p.Id,
                BedId = p.Bed?.Id ?? p.BedId,
                Name = p.Name,
                Gender = p.Gender,
                Age = p.Age,
                Weight = p.Weight,
                NursingGrade = p.NursingGrade,
                Department = p.Bed?.Ward?.Department?.DeptName ?? "未分配",
                Diagnosis = null, // TODO: 从就诊记录中获取
                Status = p.Status
            }).ToList();

            _logger.LogInformation("成功获取 {Count} 个患者", result.Count);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取患者列表失败");
            return StatusCode(500, new { message = "获取患者列表失败: " + ex.Message });
        }
    }

    /// <summary>
    /// 获取单个患者详情
    /// </summary>
    /// <param name="patientId">患者ID</param>
    [HttpGet("{patientId}")]
    public async Task<ActionResult<PatientDetailDto>> GetPatientDetail(string patientId)
    {
        try
        {
            _logger.LogInformation("获取患者详情，ID: {PatientId}", patientId);

            var patient = await _patientRepository.GetQueryable()
                .Include(p => p.Bed)
                .ThenInclude(b => b.Ward)
                .ThenInclude(w => w.Department)
                .Include(p => p.AttendingDoctor)
                .FirstOrDefaultAsync(p => p.Id == patientId);

            if (patient == null)
            {
                return NotFound(new { message = $"未找到ID为 {patientId} 的患者" });
            }

            var result = new PatientDetailDto
            {
                Id = patient.Id,
                BedId = patient.Bed?.Id ?? patient.BedId,
                Name = patient.Name,
                Gender = patient.Gender,
                Age = patient.Age,
                Weight = patient.Weight,
                NursingGrade = patient.NursingGrade,
                Department = patient.Bed?.Ward?.Department?.DeptName ?? "未分配",
                PhoneNumber = patient.PhoneNumber,
                IdCard = patient.IdCard,
                DateOfBirth = patient.DateOfBirth,
                Status = patient.Status,
                AttendingDoctorName = patient.AttendingDoctor?.Name,
                Diagnosis = null, // TODO: 从就诊记录中获取
                Allergies = null, // TODO: 从过敏史中获取
                MedicalHistory = null // TODO: 从病史中获取
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取患者详情失败，ID: {PatientId}", patientId);
            return StatusCode(500, new { message = "获取患者详情失败: " + ex.Message });
        }
    }

    /// <summary>
    /// 更新患者护理等级
    /// </summary>
    /// <param name="patientId">患者ID</param>
    /// <param name="request">更新请求</param>
    [HttpPut("{patientId}/nursing-grade")]
    public async Task<IActionResult> UpdateNursingGrade(
        string patientId,
        [FromBody] UpdateNursingGradeRequest request)
    {
        try
        {
            _logger.LogInformation("更新患者护理等级，患者ID: {PatientId}, 新等级: {NewGrade}", patientId, request.NewGrade);

            var patient = await _patientRepository.GetByIdAsync(patientId);
            if (patient == null)
            {
                return NotFound(new { message = $"未找到ID为 {patientId} 的患者" });
            }

            var oldGrade = patient.NursingGrade;
            patient.NursingGrade = (CareFlow.Core.Enums.NursingGrade)request.NewGrade;
            
            await _patientRepository.UpdateAsync(patient);

            _logger.LogInformation("患者 {PatientId} 护理等级已从 {OldGrade} 更新为 {NewGrade}，操作医生: {DoctorId}", 
                patientId, oldGrade, patient.NursingGrade, request.DoctorId);

            return Ok(new 
            { 
                message = "护理等级更新成功",
                data = new
                {
                    patientId = patient.Id,
                    oldGrade = (int)oldGrade,
                    newGrade = (int)patient.NursingGrade
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新患者护理等级失败，ID: {PatientId}", patientId);
            return StatusCode(500, new { message = "更新护理等级失败: " + ex.Message });
        }
    }

    // ==================== 患者管理接口 ====================

    /// <summary>
    /// 【患者管理】获取患者管理列表（支持筛选和搜索）
    /// </summary>
    /// <param name="status">患者状态筛选（可选）</param>
    /// <param name="keyword">搜索关键词（患者ID/身份证号/姓名）</param>
    /// <param name="departmentId">科室ID（可选）</param>
    /// <param name="wardId">病区ID（可选）</param>
    [HttpGet("management/list")]
    public async Task<ActionResult<List<PatientCardDto>>> GetPatientManagementList(
        [FromQuery] PatientStatus? status = null,
        [FromQuery] string? keyword = null,
        [FromQuery] string? departmentId = null,
        [FromQuery] string? wardId = null)
    {
        try
        {
            _logger.LogInformation("获取患者管理列表，状态: {Status}, 关键词: {Keyword}, 科室: {DepartmentId}, 病区: {WardId}", 
                status, keyword, departmentId, wardId);

            var query = _patientRepository.GetQueryable()
                .Include(p => p.Bed)
                .ThenInclude(b => b.Ward)
                .ThenInclude(w => w.Department)
                .Include(p => p.AttendingDoctor)
                .AsQueryable();

            // 状态筛选
            if (status.HasValue)
            {
                query = query.Where(p => p.Status == status.Value);
            }
            else
            {
                // 默认只显示在院和待出院患者，排除待入院和已出院
                query = query.Where(p => p.Status == PatientStatus.Hospitalized || p.Status == PatientStatus.PendingDischarge);
            }

            // 科室和病区筛选
            if (!string.IsNullOrEmpty(wardId))
            {
                query = query.Where(p => p.Bed.WardId == wardId);
                
                if (!string.IsNullOrEmpty(departmentId))
                {
                    query = query.Where(p => p.Bed.Ward.Department.Id == departmentId);
                }
            }
            else if (!string.IsNullOrEmpty(departmentId))
            {
                query = query.Where(p => p.Bed.Ward.Department.Id == departmentId);
            }

            // 关键词搜索（患者ID、身份证号、姓名）
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var searchKeyword = keyword.Trim();
                query = query.Where(p => 
                    p.Id.Contains(searchKeyword) || 
                    p.IdCard.Contains(searchKeyword) || 
                    p.Name.Contains(searchKeyword));
            }

            // 按病区和床号排序，保证顺序稳定
            var patients = await query
                .OrderBy(p => p.Bed.WardId)
                .ThenBy(p => p.BedId)
                .ToListAsync();

            // 获取责任护士信息（使用UTC时间，与数据库排班表一致）
            var now = DateTime.UtcNow;
            var patientNurseMap = new Dictionary<string, Nurse>();

            // 串行计算每位患者的责任护士ID (避免 DbContext 并发问题)
            var nurseIdResults = new List<(string PatientId, string? NurseId)>();
            foreach (var p in patients)
            {
                var nurseId = await _nurseAssignmentService.CalculateResponsibleNurseAsync(p.Id, now);
                nurseIdResults.Add((p.Id, nurseId));
            }
            
            // 获取所有涉及的护士ID并查询详情
            var nurseIds = nurseIdResults
                .Where(x => !string.IsNullOrEmpty(x.NurseId))
                .Select(x => x.NurseId!)
                .Distinct()
                .ToList();

            if (nurseIds.Any())
            {
                var nurses = await _nurseRepository.GetQueryable()
                    .Where(n => nurseIds.Contains(n.Id))
                    .ToListAsync();
                
                var nurseDict = nurses.ToDictionary(n => n.Id);
                
                // 建立患者ID到护士实体的映射
                foreach (var item in nurseIdResults)
                {
                    if (!string.IsNullOrEmpty(item.NurseId) && nurseDict.TryGetValue(item.NurseId, out var nurse))
                    {
                        patientNurseMap[item.PatientId] = nurse;
                    }
                }
            }

            var result = patients.Select(p => {
                var responsibleNurse = patientNurseMap.ContainsKey(p.Id) ? patientNurseMap[p.Id] : null;

                return new PatientCardDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Gender = p.Gender,
                    Age = p.Age,
                    BedId = p.Bed?.Id ?? p.BedId,
                    NursingGrade = p.NursingGrade,
                    Status = p.Status,
                    StatusDisplay = GetStatusDisplayName(p.Status),
                    NursingAnomalyStatus = p.NursingAnomalyStatus,
                    Department = p.Bed?.Ward?.Department?.DeptName ?? "未分配",
                    Ward = p.Bed?.Ward?.Id ?? "未分配",
                    ResponsibleDoctorId = p.AttendingDoctor?.Id,
                    ResponsibleDoctorName = p.AttendingDoctor?.Name,
                    ResponsibleDoctorPhone = p.AttendingDoctor?.Phone,
                    ResponsibleNurseId = responsibleNurse?.Id,
                    ResponsibleNurseName = responsibleNurse?.Name,
                    ResponsibleNursePhone = responsibleNurse?.Phone
                };
            }).ToList();

            _logger.LogInformation("成功获取 {Count} 个患者", result.Count);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取患者管理列表失败");
            return StatusCode(500, new { message = "获取患者管理列表失败: " + ex.Message });
        }
    }

    /// <summary>
    /// 【患者管理】获取患者完整信息
    /// </summary>
    /// <param name="patientId">患者ID</param>
    [HttpGet("management/{patientId}/full")]
    public async Task<ActionResult<PatientFullInfoDto>> GetPatientFullInfo(string patientId)
    {
        try
        {
            _logger.LogInformation("获取患者完整信息，ID: {PatientId}", patientId);

            var patient = await _patientRepository.GetQueryable()
                .Include(p => p.Bed)
                .ThenInclude(b => b.Ward)
                .ThenInclude(w => w.Department)
                .Include(p => p.AttendingDoctor)
                .FirstOrDefaultAsync(p => p.Id == patientId);

            if (patient == null)
            {
                _logger.LogWarning("未找到患者，ID: {PatientId}", patientId);
                return NotFound(new { message = $"未找到ID为 {patientId} 的患者" });
            }

            var result = new PatientFullInfoDto
            {
                // 基本信息（不可修改）
                Id = patient.Id,
                Name = patient.Name,
                IdCard = patient.IdCard,
                
                // 可修改字段
                Gender = patient.Gender,
                DateOfBirth = patient.DateOfBirth,
                Age = patient.Age,
                Height = patient.Height,
                Weight = patient.Weight,
                PhoneNumber = patient.PhoneNumber,
                OutpatientDiagnosis = patient.OutpatientDiagnosis,
                ScheduledAdmissionTime = patient.ScheduledAdmissionTime,
                ActualAdmissionTime = patient.ActualAdmissionTime,
                NursingGrade = patient.NursingGrade,
                
                // 异常状态
                NursingAnomalyStatus = patient.NursingAnomalyStatus,
                
                // 关联信息（只读）
                BedId = patient.Bed?.Id ?? patient.BedId,
                Department = patient.Bed?.Ward?.Department?.DeptName ?? "未分配",
                Ward = patient.Bed?.Ward?.Id ?? "未分配",
                AttendingDoctorName = patient.AttendingDoctor?.Name ?? "未分配",
                Status = patient.Status
            };

            _logger.LogInformation("成功获取患者完整信息，患者: {Name}", patient.Name);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取患者完整信息失败，ID: {PatientId}", patientId);
            return StatusCode(500, new { message = "获取患者完整信息失败: " + ex.Message });
        }
    }

    /// <summary>
    /// 【患者管理】更新患者信息
    /// </summary>
    /// <param name="patientId">患者ID</param>
    /// <param name="request">更新请求</param>
    [HttpPut("management/{patientId}")]
    public async Task<IActionResult> UpdatePatientInfo(
        string patientId,
        [FromBody] UpdatePatientInfoRequest request)
    {
        try
        {
            _logger.LogInformation("更新患者信息，患者ID: {PatientId}, 操作人: {OperatorId}", patientId, request.OperatorId);

            // 验证患者ID是否匹配
            if (request.PatientId != patientId)
            {
                return BadRequest(new { message = "请求体中的患者ID与URL参数不匹配" });
            }

            var patient = await _patientRepository.GetByIdAsync(patientId);
            if (patient == null)
            {
                _logger.LogWarning("未找到患者，ID: {PatientId}", patientId);
                return NotFound(new { message = $"未找到ID为 {patientId} 的患者" });
            }

            // 记录原始值用于日志
            var changes = new List<string>();

            // 只更新传入的字段（非null则更新）
            if (request.Height.HasValue && request.Height.Value != patient.Height)
            {
                changes.Add($"身高: {patient.Height}cm → {request.Height.Value}cm");
                patient.Height = request.Height.Value;
            }

            if (request.Weight.HasValue && request.Weight.Value != patient.Weight)
            {
                changes.Add($"体重: {patient.Weight}kg → {request.Weight.Value}kg");
                patient.Weight = request.Weight.Value;
            }

            if (!string.IsNullOrWhiteSpace(request.PhoneNumber) && request.PhoneNumber != patient.PhoneNumber)
            {
                changes.Add($"电话: {patient.PhoneNumber} → {request.PhoneNumber}");
                patient.PhoneNumber = request.PhoneNumber;
            }

            if (request.OutpatientDiagnosis != null && request.OutpatientDiagnosis != patient.OutpatientDiagnosis)
            {
                changes.Add($"门诊诊断: {patient.OutpatientDiagnosis ?? "无"} → {request.OutpatientDiagnosis}");
                patient.OutpatientDiagnosis = request.OutpatientDiagnosis;
            }

            if (request.ScheduledAdmissionTime.HasValue && request.ScheduledAdmissionTime != patient.ScheduledAdmissionTime)
            {
                changes.Add($"预约入院时间: {patient.ScheduledAdmissionTime} → {request.ScheduledAdmissionTime}");
                patient.ScheduledAdmissionTime = request.ScheduledAdmissionTime;
            }

            if (request.ActualAdmissionTime.HasValue && request.ActualAdmissionTime != patient.ActualAdmissionTime)
            {
                changes.Add($"实际入院时间: {patient.ActualAdmissionTime} → {request.ActualAdmissionTime}");
                patient.ActualAdmissionTime = request.ActualAdmissionTime;
            }

            if (request.NursingGrade.HasValue && request.NursingGrade.Value != patient.NursingGrade)
            {
                changes.Add($"护理级别: {patient.NursingGrade} → {request.NursingGrade.Value}");
                patient.NursingGrade = request.NursingGrade.Value;
            }

            if (changes.Count == 0)
            {
                return Ok(new { message = "没有字段需要更新" });
            }

            await _patientRepository.UpdateAsync(patient);

            _logger.LogInformation("患者信息更新成功，患者ID: {PatientId}, 操作人: {OperatorId} ({OperatorType}), 变更: {Changes}", 
                patientId, request.OperatorId, request.OperatorType, string.Join("; ", changes));

            return Ok(new 
            { 
                message = "患者信息更新成功",
                data = new
                {
                    patientId = patient.Id,
                    changesCount = changes.Count,
                    changes = changes
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新患者信息失败，ID: {PatientId}", patientId);
            return StatusCode(500, new { message = "更新患者信息失败: " + ex.Message });
        }
    }

    /// <summary>
    /// 【患者管理】出院前检查
    /// </summary>
    /// <param name="patientId">患者ID</param>
    [HttpGet("management/{patientId}/discharge-check")]
    public async Task<ActionResult<PatientDischargeCheckDto>> CheckPatientDischarge(string patientId)
    {
        try
        {
            _logger.LogInformation("执行出院前检查，患者ID: {PatientId}", patientId);

            // 验证患者是否存在
            var patient = await _patientRepository.GetByIdAsync(patientId);
            if (patient == null)
            {
                _logger.LogWarning("未找到患者，ID: {PatientId}", patientId);
                return NotFound(new { message = $"未找到ID为 {patientId} 的患者" });
            }

            // 验证患者状态
            if (patient.Status != PatientStatus.PendingDischarge)
            {
                return BadRequest(new 
                { 
                    message = $"患者当前状态为 {GetStatusDisplayName(patient.Status)}，只有待出院患者才能进行出院检查" 
                });
            }

            // 调用出院医嘱服务进行验证
            var validationResult = await _dischargeOrderService.ValidateDischargeOrderCreationAsync(patientId);

            var result = new PatientDischargeCheckDto
            {
                PatientId = patientId,
                CanDischarge = validationResult.CanCreateDischargeOrder && 
                               (validationResult.BlockedOrders == null || validationResult.BlockedOrders.Count == 0),
                Message = "",
                UnfinishedTasks = new List<UnfinishedTaskDto>()
            };

            // 处理检查结果
            if (validationResult.BlockedOrders != null && validationResult.BlockedOrders.Count > 0)
            {
                result.CanDischarge = false;
                result.Message = $"患者有 {validationResult.BlockedOrders.Count} 条未完成的医嘱，请先处理后再办理出院";
                
                result.UnfinishedTasks = validationResult.BlockedOrders.Select(order => new UnfinishedTaskDto
                {
                    OrderId = order.OrderId,
                    OrderType = order.OrderType,
                    OrderSummary = order.Summary,
                    Status = order.Status,
                    StatusDisplay = order.StatusDisplay,
                    UnfinishedTaskCount = ParseUnfinishedTaskCount(order.StatusDisplay),
                    LatestTaskTime = order.EndTime,
                    ItemName = order.ItemName,
                    OperationName = order.OperationName,
                    SurgeryName = order.SurgeryName,
                    MedicationOrderItems = order.MedicationOrderItems
                }).ToList();

                _logger.LogWarning("出院检查失败，患者ID: {PatientId}, 未完成医嘱数: {Count}", 
                    patientId, validationResult.BlockedOrders.Count);
            }
            else
            {
                result.CanDischarge = true;
                result.Message = "患者符合出院条件，可以办理出院";
                _logger.LogInformation("出院检查通过，患者ID: {PatientId}", patientId);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "出院前检查失败，患者ID: {PatientId}", patientId);
            return StatusCode(500, new { message = "出院前检查失败: " + ex.Message });
        }
    }

    /// <summary>
    /// 【患者管理】办理出院（TODO）
    /// </summary>
    /// <param name="patientId">患者ID</param>
    /// <param name="request">出院请求</param>
    [HttpPost("management/{patientId}/discharge")]
    public async Task<IActionResult> ProcessPatientDischarge(
        string patientId,
        [FromBody] ProcessDischargeRequest request)
    {
        try
        {
            _logger.LogInformation("办理患者出院，患者ID: {PatientId}, 操作人: {OperatorId}", patientId, request.OperatorId);

            // 验证患者ID是否匹配
            if (request.PatientId != patientId)
            {
                return BadRequest(new { message = "请求体中的患者ID与URL参数不匹配" });
            }

            var patient = await _patientRepository.GetByIdAsync(patientId);
            if (patient == null)
            {
                _logger.LogWarning("未找到患者，ID: {PatientId}", patientId);
                return NotFound(new { message = $"未找到ID为 {patientId} 的患者" });
            }

            // 验证患者状态
            if (patient.Status != PatientStatus.PendingDischarge)
            {
                _logger.LogWarning("患者状态不符合出院条件，患者ID: {PatientId}, 当前状态: {Status}", 
                    patientId, patient.Status);
                return BadRequest(new 
                { 
                    message = $"患者当前状态为 {GetStatusDisplayName(patient.Status)}，无法办理出院" 
                });
            }

            // ==================== 执行出院处理逻辑 ====================
            
            // 使用数据库事务确保数据一致性
            using var transaction = await _dbContext.BeginTransactionAsync();
            
            try
            {
                _logger.LogInformation("开始办理患者出院事务，患者ID: {PatientId}", patientId);
                
                // 1. 查找患者的出院医嘱
                var dischargeOrder = await _dischargeOrderRepository.GetQueryable()
                    .Where(o => o.PatientId == patientId && o.Status == OrderStatus.Accepted)
                    .OrderByDescending(o => o.CreateTime)
                    .FirstOrDefaultAsync();
                
                if (dischargeOrder == null)
                {
                    _logger.LogWarning("未找到患者的出院医嘱，患者ID: {PatientId}", patientId);
                    await transaction.RollbackAsync();
                    return BadRequest(new { message = "未找到患者的出院医嘱，无法办理出院" });
                }
                
                var dischargeTime = DateTime.UtcNow;
                
                // 2. 更新出院医嘱的确认信息
                dischargeOrder.DischargeConfirmedByNurseId = request.OperatorId;
                dischargeOrder.DischargeConfirmedAt = dischargeTime;
                
                _logger.LogInformation("更新出院医嘱确认信息，医嘱ID: {OrderId}, 护士ID: {NurseId}", 
                    dischargeOrder.Id, request.OperatorId);
                await _dischargeOrderRepository.UpdateAsync(dischargeOrder);
                
                // 3. 获取患者床位信息（用于释放床位）
                Bed? bed = null;
                if (!string.IsNullOrEmpty(patient.BedId))
                {
                    bed = await _bedRepository.GetByIdAsync(patient.BedId);
                    if (bed == null)
                    {
                        _logger.LogWarning("未找到患者所在床位，床位ID: {BedId}", patient.BedId);
                    }
                }
                
                // 4. 更新患者状态为已出院
                patient.Status = PatientStatus.Discharged;
                
                _logger.LogInformation("更新患者状态为已出院，患者ID: {PatientId}", patientId);
                await _patientRepository.UpdateAsync(patient);
                
                // 5. 释放床位（将床位状态改为空闲）
                if (bed != null)
                {
                    bed.Status = "空闲"; // 更新床位状态为空闲
                    
                    _logger.LogInformation("释放床位，床位ID: {BedId}, 病区: {WardId}", 
                        bed.Id, bed.WardId);
                    await _bedRepository.UpdateAsync(bed);
                }
                else if (!string.IsNullOrEmpty(patient.BedId))
                {
                    _logger.LogWarning("患者有床位ID但未找到对应床位记录，床位ID: {BedId}", patient.BedId);
                }
                
                // 6. 提交事务
                await transaction.CommitAsync();
                
                // 7. 记录操作日志（事务成功后）
                _logger.LogInformation(
                    "患者出院办理成功 - 患者ID: {PatientId}, 患者姓名: {PatientName}, " +
                    "出院医嘱ID: {DischargeOrderId}, 床位ID: {BedId}, " +
                    "确认护士ID: {NurseId}, 出院确认时间: {DischargeTime}, 备注: {Remarks}",
                    patient.Id,
                    patient.Name,
                    dischargeOrder.Id,
                    patient.BedId,
                    request.OperatorId,
                    dischargeTime,
                    request.Remarks ?? "无");
                
                // 8. 返回成功响应
                return Ok(new 
                { 
                    message = "出院办理成功",
                    data = new
                    {
                        patientId = patient.Id,
                        patientName = patient.Name,
                        dischargeOrderId = dischargeOrder.Id,
                        dischargeTime = dischargeTime,
                        confirmedByNurseId = request.OperatorId,
                        releasedBedId = bed?.Id
                    }
                });
            }
            catch (Exception ex)
            {
                // 回滚事务
                await transaction.RollbackAsync();
                
                _logger.LogError(ex, 
                    "出院办理事务失败，已回滚 - 患者ID: {PatientId}, 操作人: {OperatorId}", 
                    patientId, request.OperatorId);
                
                return StatusCode(500, new 
                { 
                    message = "出院办理失败，事务已回滚: " + ex.Message 
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "办理出院失败，患者ID: {PatientId}", patientId);
            return StatusCode(500, new { message = "办理出院失败: " + ex.Message });
        }
    }

    // ==================== 辅助方法 ====================

    /// <summary>
    /// 获取患者状态显示名称
    /// </summary>
    private string GetStatusDisplayName(PatientStatus status)
    {
        return status switch
        {
            PatientStatus.PendingAdmission => "待入院",
            PatientStatus.Hospitalized => "在院",
            PatientStatus.PendingDischarge => "待出院",
            PatientStatus.Discharged => "已出院",
            _ => "未知状态"
        };
    }

    /// <summary>
    /// 从状态显示文本中解析未完成任务数量
    /// </summary>
    private int ParseUnfinishedTaskCount(string statusDisplay)
    {
        // 解析类似 "进行中（有2个未完成任务）" 这样的文本
        if (string.IsNullOrEmpty(statusDisplay))
            return 0;

        var match = System.Text.RegularExpressions.Regex.Match(statusDisplay, @"有(\d+)个未完成任务");
        if (match.Success && int.TryParse(match.Groups[1].Value, out int count))
        {
            return count;
        }

        return 0;
    }
}

/// <summary>
/// 更新护理等级请求
/// </summary>
public class UpdateNursingGradeRequest
{
    /// <summary>
    /// 新的护理等级 (0=特级, 1=一级, 2=二级, 3=三级)
    /// </summary>
    public int NewGrade { get; set; }

    /// <summary>
    /// 修改原因
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// 操作医生ID
    /// </summary>
    public string DoctorId { get; set; } = string.Empty;
}