namespace CareFlow.Application.DTOs.Staff;

public class StaffResponseDto
{
    public Guid Id { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    
    // "Doctor" 或 "Nurse"
    public string Role { get; set; } = string.Empty; 
    
    // 科室名称，例如 "心内科"
    public string DeptName { get; set; } = string.Empty; 
    
    public bool IsActive { get; set; }

    // 核心展示字段：如果是医生显示“主任”，如果是护士显示“护士长”
    public string TitleOrRank { get; set; } = string.Empty; 
}