namespace CareFlow.Application.DTOs.Auth;

public class LoginDto
{
    //账号
    public string EmployeeNumber { get; set; } = string.Empty;
    //密码
    public string Password { get; set; } = string.Empty;
}

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty; // 登录成功后发给前端的“通行证”（加密字符串）
    public string StaffId { get; set; } = string.Empty; // 员工ID
    public string FullName { get; set; } = string.Empty; // 返回姓名，用于在右上角显示 "欢迎，张主任"
    public string Role { get; set; } = string.Empty; // 返回角色，用于前端控制菜单个性化显示
    public string DeptCode { get; set; } = string.Empty; // 科室代码
}