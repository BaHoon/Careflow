using CareFlow.Application.DTOs.Patient;
using CareFlow.Core.Interfaces;
using CareFlow.Core.Models.Organization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CareFlow.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PatientController : ControllerBase
{
    private readonly IRepository<Patient, string> _patientRepository;
    private readonly ILogger<PatientController> _logger;

    public PatientController(
        IRepository<Patient, string> patientRepository,
        ILogger<PatientController> logger)
    {
        _patientRepository = patientRepository;
        _logger = logger;
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
                .Where(p => p.Status != "Discharged"); // 排除已出院患者

            // 根据departmentId和wardId过滤
            if (!string.IsNullOrEmpty(wardId))
            {
                // 如果指定了病区，先检查病区是否属于指定科室（如果有科室参数）
                if (!string.IsNullOrEmpty(departmentId))
                {
                    // 验证病区是否属于该科室
                    var wardExists = await query
                        .AnyAsync(p => p.Bed.WardId == wardId && p.Bed.Ward.DepartmentId == departmentId);
                    
                    if (!wardExists)
                    {
                        _logger.LogWarning("病区 {WardId} 不属于科室 {DepartmentId}", wardId, departmentId);
                        return BadRequest(new { message = $"病区 {wardId} 不属于科室 {departmentId}" });
                    }
                    
                    // 同时过滤科室和病区
                    query = query.Where(p => p.Bed.Ward.DepartmentId == departmentId && p.Bed.WardId == wardId);
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
                query = query.Where(p => p.Bed.Ward.DepartmentId == departmentId);
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
                Diagnosis = null // TODO: 从就诊记录中获取
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
}
