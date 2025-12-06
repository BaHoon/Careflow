using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CareFlow.Application.DTOs.Auth;
using CareFlow.Core.Models.Organization; // 引用新的 Staff 类所在的命名空间
using CareFlow.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace CareFlow.Application.Services;

public class AuthService
{
    // 泛型改为 <Staff, string>，因为现在的实体是 Staff，主键(StaffId)是 string 类型
    private readonly IRepository<Staff, string> _staffRepo;
    private readonly IConfiguration _configuration;

    public AuthService(IRepository<Staff, string> staffRepo, IConfiguration configuration)
    {
        _staffRepo = staffRepo;
        _configuration = configuration;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginDto dto)
    {
        // 1. 查找用户
        // 你的 Staff 类中，工号对应的字段是 StaffId
        // 前端传来的 dto.EmployeeNumber 对应数据库里的 StaffId
        var user = await _staffRepo.GetAsync(u => u.Id == dto.EmployeeNumber);

        if (user == null)
            throw new Exception("用户不存在");

        // 1.1 检查账号状态 (新加的逻辑，因为你的数据库有这个字段)
        if (!user.IsActive)
            throw new Exception("账号已被禁用，请联系管理员");

        // 2. 验证密码
        string inputHash = GenerateSimpleHash(dto.Password);
        
        // 注意：数据库里存的字段叫 PasswordHash
        if (user.PasswordHash != inputHash)
            throw new Exception("密码错误");

        // 3. 生成 JWT Token
        var token = GenerateJwtToken(user);

        return new LoginResponseDto
        {
            Token = token,
            FullName = user.Name,       // 对应 Staff.Name
            Role = user.RoleType        // 对应 Staff.RoleType (存的是字符串 "Doctor"/"Nurse")
        };
    }

    // 哈希算法保持不变，确保和生成种子数据时的一致
    private string GenerateSimpleHash(string password)
    {
        using (var sha256 = SHA256.Create())
        {
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }
    }

    private string GenerateJwtToken(Staff user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            // Sub (Subject) 存唯一标识，这里用工号
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            
            // 自定义 Claim，方便前端获取
            new Claim("id", user.Id), 
            new Claim("name", user.Name),
            new Claim("role", user.RoleType), // 把角色放进 Token，方便前端做权限控制
            new Claim("dept", user.DeptCode)  // 把科室代码也放进去，前端可能用到
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddDays(1), // Token 有效期1天
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}