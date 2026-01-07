using CareFlow.Application.DTOs.Admin;
using CareFlow.Application.Interfaces;
using CareFlow.Core.Interfaces;
using CareFlow.Core.Models.Organization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace CareFlow.Application.Services.Admin;

/// <summary>
/// 人员管理服务
/// </summary>
public class StaffManagementService
{
    private readonly IRepository<Staff, string> _staffRepository;
    private readonly IRepository<Doctor, string> _doctorRepository;
    private readonly IRepository<Nurse, string> _nurseRepository;
    private readonly IRepository<Department, string> _departmentRepository;
    private readonly ISystemLogService _systemLogService;
    private readonly ILogger<StaffManagementService> _logger;

    public StaffManagementService(
        IRepository<Staff, string> staffRepository,
        IRepository<Doctor, string> doctorRepository,
        IRepository<Nurse, string> nurseRepository,
        IRepository<Department, string> departmentRepository,
        ISystemLogService systemLogService,
        ILogger<StaffManagementService> logger)
    {
        _staffRepository = staffRepository;
        _doctorRepository = doctorRepository;
        _nurseRepository = nurseRepository;
        _departmentRepository = departmentRepository;
        _systemLogService = systemLogService;
        _logger = logger;
    }

    /// <summary>
    /// 查询人员列表
    /// </summary>
    public async Task<QueryStaffListResponseDto> QueryStaffListAsync(QueryStaffListRequestDto request)
    {
        _logger.LogInformation("查询人员列表，筛选条件: {@Request}", request);

        // 获取所有人员（包括医生、护士等）
        var query = _staffRepository.GetQueryable()
            .Include(s => s.Department)
            .AsQueryable();

        // 搜索关键词
        if (!string.IsNullOrEmpty(request.SearchKeyword))
        {
            query = query.Where(s => 
                s.Name.Contains(request.SearchKeyword) || 
                s.Id.Contains(request.SearchKeyword) ||
                s.EmployeeNumber.Contains(request.SearchKeyword));
        }

        // 角色筛选
        if (!string.IsNullOrEmpty(request.Role))
        {
            query = query.Where(s => s.RoleType == request.Role);
        }

        // 科室筛选
        if (!string.IsNullOrEmpty(request.DeptCode))
        {
            query = query.Where(s => s.DeptCode == request.DeptCode);
        }

        // 总数
        var totalCount = await query.CountAsync();

        // 排序和分页
        var staffList = await query
            .OrderByDescending(s => s.CreateTime)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        // 如果是查询医生，需要获取医生的职称信息
        var doctorIds = staffList
            .Where(s => s.RoleType == "Doctor")
            .Select(s => s.Id)
            .ToList();
        
        var doctors = new Dictionary<string, string>();
        if (doctorIds.Any())
        {
            var doctorList = await _doctorRepository.GetQueryable()
                .Where(d => doctorIds.Contains(d.Id))
                .ToListAsync();
            
            foreach (var doctor in doctorList)
            {
                doctors[doctor.Id] = doctor.Title;
            }
        }

        // 映射到DTO
        var staffDtos = staffList.Select(s => new StaffDto
        {
            StaffId = s.Id,
            EmployeeNumber = s.EmployeeNumber,
            FullName = s.Name,
            Role = s.RoleType,
            DeptCode = s.DeptCode,
            DeptName = s.Department?.DeptName ?? "",
            Title = s.RoleType == "Doctor" && doctors.ContainsKey(s.Id) ? doctors[s.Id] : null,
            IsActive = s.IsActive,
            CreatedAt = s.CreateTime
        }).ToList();

        return new QueryStaffListResponseDto
        {
            StaffList = staffDtos,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }

    /// <summary>
    /// 创建新人员
    /// </summary>
    public async Task<StaffDto> CreateStaffAsync(CreateStaffRequestDto request, string? operatorId = null, string? operatorName = null, string? ipAddress = null)
    {
        // 检查员工ID是否已存在
        var existingStaff = await _staffRepository.GetByIdAsync(request.StaffId);
        if (existingStaff != null)
        {
            throw new InvalidOperationException($"员工ID {request.StaffId} 已存在");
        }

        // 验证员工ID前缀是否与角色匹配
        var staffIdUpper = request.StaffId.ToUpper();
        switch (request.Role)
        {
            case "Doctor":
                if (!staffIdUpper.StartsWith("D"))
                {
                    throw new InvalidOperationException("医生的员工ID必须以'D'开头");
                }
                break;
            case "Nurse":
                if (!staffIdUpper.StartsWith("N"))
                {
                    throw new InvalidOperationException("护士的员工ID必须以'N'开头");
                }
                break;
            case "Admin":
                if (!staffIdUpper.StartsWith("A"))
                {
                    throw new InvalidOperationException("管理员的员工ID必须以'A'开头");
                }
                break;
            default:
                throw new ArgumentException($"不支持的角色类型: {request.Role}");
        }

        // 验证科室是否存在
        var department = await _departmentRepository.GetByIdAsync(request.DeptCode);
        if (department == null)
        {
            throw new InvalidOperationException($"科室代码 {request.DeptCode} 不存在");
        }

        // 默认密码为 123456，使用 SHA256 加密
        var passwordHash = HashPassword("123456");

        // 根据 ID 生成 EmployeeNumber
        // ID格式: D001 → doc001, N123 → nurse123, A005 → admin005
        string employeeNumber;
        string idNumber = request.StaffId.Substring(1); // 提取数字部分，如 D050 → 050
        
        switch (request.Role)
        {
            case "Doctor":
                employeeNumber = $"doc{idNumber.ToLower()}";
                break;
            case "Nurse":
                employeeNumber = $"nurse{idNumber.ToLower()}";
                break;
            case "Admin":
                employeeNumber = $"admin{idNumber.ToLower()}";
                break;
            default:
                throw new ArgumentException($"不支持的角色类型: {request.Role}");
        }

        Staff newStaff;
        
        // 根据角色类型创建不同的实体
        switch (request.Role)
        {
            case "Doctor":
                newStaff = new Doctor
                {
                    Id = request.StaffId,
                    EmployeeNumber = employeeNumber,
                    Name = request.FullName,
                    IdCard = request.IdCard,
                    Phone = request.Phone,
                    PasswordHash = passwordHash,
                    RoleType = "Doctor",
                    DeptCode = request.DeptCode,
                    IsActive = true,
                    Title = "医师",
                    PrescriptionAuthLevel = "普通"
                };
                await _doctorRepository.AddAsync((Doctor)newStaff);
                break;
                
            case "Nurse":
                newStaff = new Nurse
                {
                    Id = request.StaffId,
                    EmployeeNumber = employeeNumber,
                    Name = request.FullName,
                    IdCard = request.IdCard,
                    Phone = request.Phone,
                    PasswordHash = passwordHash,
                    RoleType = "Nurse",
                    DeptCode = request.DeptCode,
                    IsActive = true,
                    NurseRank = "护士"
                };
                await _nurseRepository.AddAsync((Nurse)newStaff);
                break;
                
            case "Admin":
                newStaff = new Staff
                {
                    Id = request.StaffId,
                    EmployeeNumber = employeeNumber,
                    Name = request.FullName,
                    IdCard = request.IdCard,
                    Phone = request.Phone,
                    PasswordHash = passwordHash,
                    RoleType = "Admin",
                    DeptCode = request.DeptCode,
                    IsActive = true
                };
                await _staffRepository.AddAsync(newStaff);
                break;
                
            default:
                throw new ArgumentException($"不支持的角色类型: {request.Role}");
        }

        _logger.LogInformation("成功创建人员: {StaffId}, 角色: {Role}", request.StaffId, request.Role);

        // 记录系统日志
        await _systemLogService.LogAccountOperationAsync(
            operationType: "AccountCreated",
            operatorId: null,
            operatorName: operatorName,
            targetAccountId: 0,
            details: $"创建人员: {request.FullName} ({request.StaffId}), 角色: {request.Role}, 科室: {request.DeptCode}",
            ipAddress: ipAddress
        );

        // 返回创建的人员信息
        return new StaffDto
        {
            StaffId = newStaff.Id,
            EmployeeNumber = newStaff.EmployeeNumber,
            FullName = newStaff.Name,
            Role = newStaff.RoleType,
            DeptCode = newStaff.DeptCode,
            DeptName = department.DeptName,
            IsActive = newStaff.IsActive,
            CreatedAt = newStaff.CreateTime
        };
    }

    /// <summary>
    /// 重置人员密码
    /// </summary>
    public async Task ResetPasswordAsync(ResetPasswordRequestDto request, string? operatorId = null, string? operatorName = null, string? ipAddress = null)
    {
        // 查找人员
        var staff = await _staffRepository.GetByIdAsync(request.StaffId);
        if (staff == null)
        {
            throw new InvalidOperationException($"员工ID {request.StaffId} 不存在");
        }

        // 加密新密码
        var newPasswordHash = HashPassword(request.NewPassword);
        
        // 更新密码
        staff.PasswordHash = newPasswordHash;
        await _staffRepository.UpdateAsync(staff);
        
        _logger.LogInformation("成功重置人员密码: {StaffId}", request.StaffId);

        // 记录系统日志
        await _systemLogService.LogAccountOperationAsync(
            operationType: "PasswordChanged",
            operatorId: null,
            operatorName: operatorName,
            targetAccountId: 0,
            details: $"重置密码: {staff.Name} ({request.StaffId})",
            ipAddress: ipAddress
        );
    }

    /// <summary>
    /// 更新员工信息
    /// </summary>
    public async Task<StaffDto> UpdateStaffAsync(UpdateStaffRequestDto request, string? operatorId = null, string? operatorName = null, string? ipAddress = null)
    {
        // 查找员工
        var staff = await _staffRepository.GetQueryable()
            .Include(s => s.Department)
            .FirstOrDefaultAsync(s => s.Id == request.StaffId);
            
        if (staff == null)
        {
            throw new InvalidOperationException($"员工ID {request.StaffId} 不存在");
        }

        // 验证科室是否存在
        var department = await _departmentRepository.GetByIdAsync(request.DeptCode);
        if (department == null)
        {
            throw new InvalidOperationException($"科室代码 {request.DeptCode} 不存在");
        }

        var oldDeptCode = staff.DeptCode;
        // 更新科室
        staff.DeptCode = request.DeptCode;
        await _staffRepository.UpdateAsync(staff);
        
        _logger.LogInformation("成功更新员工信息: {StaffId}", request.StaffId);

        // 记录系统日志
        await _systemLogService.LogAccountOperationAsync(
            operationType: "AccountModified",
            operatorId: null,
            operatorName: operatorName,
            targetAccountId: 0,
            details: $"修改人员: {staff.Name} ({request.StaffId}), 科室从 {oldDeptCode} 更改为 {request.DeptCode}",
            ipAddress: ipAddress
        );

        // 返回更新后的人员信息
        return new StaffDto
        {
            StaffId = staff.Id,
            EmployeeNumber = staff.EmployeeNumber,
            FullName = staff.Name,
            Role = staff.RoleType,
            DeptCode = staff.DeptCode,
            DeptName = department.DeptName,
            IsActive = staff.IsActive,
            CreatedAt = staff.CreateTime
        };
    }

    /// <summary>
    /// 删除员工
    /// </summary>
    public async Task DeleteStaffAsync(string staffId, string? operatorId = null, string? operatorName = null, string? ipAddress = null)
    {
        // 查找员工
        var staff = await _staffRepository.GetByIdAsync(staffId);
        if (staff == null)
        {
            throw new InvalidOperationException($"员工ID {staffId} 不存在");
        }

        var staffName = staff.Name;
        var staffRole = staff.RoleType;

        // 删除员工（根据角色类型使用对应的repository）
        switch (staff.RoleType)
        {
            case "Doctor":
                var doctor = await _doctorRepository.GetByIdAsync(staffId);
                if (doctor != null)
                {
                    await _doctorRepository.DeleteAsync(doctor);
                }
                break;
            case "Nurse":
                var nurse = await _nurseRepository.GetByIdAsync(staffId);
                if (nurse != null)
                {
                    await _nurseRepository.DeleteAsync(nurse);
                }
                break;
            case "Admin":
                await _staffRepository.DeleteAsync(staff);
                break;
            default:
                await _staffRepository.DeleteAsync(staff);
                break;
        }
        
        _logger.LogInformation("成功删除员工: {StaffId}", staffId);

        // 记录系统日志
        await _systemLogService.LogAccountOperationAsync(
            operationType: "AccountDeleted",
            operatorId: null,
            operatorName: operatorName,
            targetAccountId: 0,
            details: $"删除人员: {staffName} ({staffId}), 角色: {staffRole}",
            ipAddress: ipAddress
        );
    }

    /// <summary>
    /// 密码哈希方法（与 AuthService 保持一致）
    /// </summary>
    private static string HashPassword(string password)
    {
        using (var sha256 = SHA256.Create())
        {
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }
    }
}
