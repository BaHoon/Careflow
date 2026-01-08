namespace CareFlow.Application.DTOs.Admin;

/// <summary>
/// 人员信息DTO
/// </summary>
public class StaffDto
{
    /// <summary>
    /// 员工ID
    /// </summary>
    public string StaffId { get; set; } = string.Empty;
    
    /// <summary>
    /// 工号
    /// </summary>
    public string EmployeeNumber { get; set; } = string.Empty;
    
    /// <summary>
    /// 姓名
    /// </summary>
    public string FullName { get; set; } = string.Empty;
    
    /// <summary>
    /// 角色类型 (Doctor/Nurse/Admin)
    /// </summary>
    public string Role { get; set; } = string.Empty;
    
    /// <summary>
    /// 科室代码
    /// </summary>
    public string DeptCode { get; set; } = string.Empty;
    
    /// <summary>
    /// 科室名称
    /// </summary>
    public string DeptName { get; set; } = string.Empty;
    
    /// <summary>
    /// 病区ID（护士专用）
    /// </summary>
    public string? WardId { get; set; }
    
    /// <summary>
    /// 医生职称（医生专用）
    /// </summary>
    public string? Title { get; set; }
    
    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsActive { get; set; }
    
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// 创建人员请求DTO
/// </summary>
public class CreateStaffRequestDto
{
    /// <summary>
    /// 员工ID
    /// </summary>
    public string StaffId { get; set; } = string.Empty;
    
    /// <summary>
    /// 姓名
    /// </summary>
    public string FullName { get; set; } = string.Empty;
    
    /// <summary>
    /// 角色类型 (Doctor/Nurse/Admin)
    /// </summary>
    public string Role { get; set; } = string.Empty;
    
    /// <summary>
    /// 科室代码
    /// </summary>
    public string DeptCode { get; set; } = string.Empty;
    
    /// <summary>
    /// 身份证号
    /// </summary>
    public string IdCard { get; set; } = string.Empty;
    
    /// <summary>
    /// 电话号码
    /// </summary>
    public string Phone { get; set; } = string.Empty;
}

/// <summary>
/// 重置密码请求DTO
/// </summary>
public class ResetPasswordRequestDto
{
    /// <summary>
    /// 员工ID
    /// </summary>
    public string StaffId { get; set; } = string.Empty;
    
    /// <summary>
    /// 新密码
    /// </summary>
    public string NewPassword { get; set; } = string.Empty;
}

/// <summary>
/// 更新员工请求DTO
/// </summary>
public class UpdateStaffRequestDto
{
    /// <summary>
    /// 员工ID
    /// </summary>
    public string StaffId { get; set; } = string.Empty;
    
    /// <summary>
    /// 科室代码
    /// </summary>
    public string DeptCode { get; set; } = string.Empty;
}

/// <summary>
/// 人员列表查询请求DTO
/// </summary>
public class QueryStaffListRequestDto
{
    /// <summary>
    /// 搜索关键词（姓名或员工ID）
    /// </summary>
    public string? SearchKeyword { get; set; }
    
    /// <summary>
    /// 角色筛选
    /// </summary>
    public string? Role { get; set; }
    
    /// <summary>
    /// 科室筛选
    /// </summary>
    public string? DeptCode { get; set; }
    
    /// <summary>
    /// 页码
    /// </summary>
    public int PageNumber { get; set; } = 1;
    
    /// <summary>
    /// 每页数量
    /// </summary>
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// 人员列表查询响应DTO
/// </summary>
public class QueryStaffListResponseDto
{
    /// <summary>
    /// 人员列表
    /// </summary>
    public List<StaffDto> StaffList { get; set; } = new();
    
    /// <summary>
    /// 总记录数
    /// </summary>
    public int TotalCount { get; set; }
    
    /// <summary>
    /// 当前页码
    /// </summary>
    public int PageNumber { get; set; }
    
    /// <summary>
    /// 每页数量
    /// </summary>
    public int PageSize { get; set; }
}
