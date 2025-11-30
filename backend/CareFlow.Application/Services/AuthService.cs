using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CareFlow.Application.DTOs.Auth;
using CareFlow.Core.Models;
using CareFlow.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace CareFlow.Application.Services;

public class AuthService
{
private readonly IRepository<StaffBase, Guid> _staffRepo;
    private readonly IConfiguration _configuration;

    public AuthService(IRepository<StaffBase, Guid> staffRepo, IConfiguration configuration)
    {
        _staffRepo = staffRepo;
        _configuration = configuration;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginDto dto)
    {
        // 1. 查找用户
        var user = await _staffRepo.GetAsync(u => u.EmployeeNumber == dto.EmployeeNumber);

        if (user == null)
            throw new Exception("用户不存在");

        // 2. 验证密码 (必须使用与 DbInitializer 相同的哈希逻辑)
        string inputHash = GenerateSimpleHash(dto.Password);
        if (user.PasswordHash != inputHash)
            throw new Exception("密码错误");

        // 3. 生成 JWT Token
        var token = GenerateJwtToken(user);

        return new LoginResponseDto
        {
            Token = token,
            FullName = user.FullName,
            Role = user.StaffType.ToString()
        };
    }

    // 必须与 DbInitializer 中的逻辑完全一致
    private string GenerateSimpleHash(string password)
    {
        using (var sha256 = SHA256.Create())
        {
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }
    }

    private string GenerateJwtToken(StaffBase user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.EmployeeNumber),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("id", user.Id.ToString()),
            new Claim("role", user.StaffType.ToString()) // 将角色放入 Token
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