using CareFlow.Core.Models;
using CareFlow.Core.Models.Organization;
using CareFlow.Core.Enums;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace CareFlow.Infrastructure.Data
{
    public static class DbInitializer
    {
        // 辅助方法：计算 SHA256 哈希值并转换为 Base64 字符串（与 AuthService 保持一致）
        private static string HashPassword(string password)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // 计算哈希
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));

                // 转换为 Base64 字符串，与 AuthService.GenerateSimpleHash 保持一致
                return Convert.ToBase64String(bytes);
            }
        }

        public static void Initialize(ApplicationDbContext context)
        {
            // 确保数据库已创建
            context.Database.EnsureCreated();

            // 【开关】决定是否要强制清空数据
            // 注意：如果之前报错导致部分数据插入成功，建议保持 true 运行一次以清理脏数据
            bool forceReset = true; 

            if (forceReset)
            {
                ClearAllData(context);
            }

            // 1. 检查是否已有数据 (如果 forceReset 为 true，这里通常是空的，除非 Clear 失败)
            if (context.Departments.Any())
            {
                return;   // 数据库已播种，直接返回
            }

            // 2. 预置数据
            
            // --- 预置科室数据 (Department) ---
            // 【修正】添加了 Location 字段，解决 "null value in column Location" 错误
            var departments = new Department[]
            {
                new Department { DeptId = "IM", DeptName = "内科", Location = "住院部A栋3楼" },
                new Department { DeptId = "SUR", DeptName = "外科", Location = "住院部A栋4楼" },
                new Department { DeptId = "PED", DeptName = "儿科", Location = "住院部B栋2楼" },
                new Department { DeptId = "ADM", DeptName = "行政", Location = "行政楼101室" }
            };
            context.Departments.AddRange(departments);
            context.SaveChanges(); // 保存科室

            // --- 预置员工数据 (Staff/Doctor/Nurse) ---
            
            // 默认密码为 "123456"
            string defaultHashedPassword = HashPassword("123456");
            // string defaultHashedPassword = "123456";

            // 医生数据
            var doctors = new Doctor[]
            {
                new Doctor
                {
                    Id = "D001",
                    EmployeeNumber = "doc001",
                    PasswordHash = defaultHashedPassword,
                    Name = "张医生",
                    IdCard = "110100200001010001",
                    Phone = "13912340001",
                    RoleType = "Doctor",
                    IsActive = true,
                    DeptCode = "IM",
                    Title = DoctorTitle.Chief.ToString(), // 主任
                    PrescriptionAuthLevel = "High"
                },
                new Doctor
                {
                    Id = "D002",
                    EmployeeNumber = "doc002",
                    PasswordHash = defaultHashedPassword,
                    Name = "李医生",
                    IdCard = "110100200001010002",
                    Phone = "13912340002",
                    RoleType = "Doctor",
                    IsActive = true,
                    DeptCode = "IM",
                    Title = DoctorTitle.Attending.ToString(), // 主治
                    PrescriptionAuthLevel = "Medium"
                }
            };
            
            // 护士数据
            var nurses = new Nurse[]
            {
                new Nurse
                {
                    Id = "N001",
                    EmployeeNumber = "nurse001",
                    PasswordHash = defaultHashedPassword,
                    Name = "王护士",
                    IdCard = "110100200001010003",
                    Phone = "13912340003",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "SUR",
                    NurseRank = NurseRank.HeadNurse.ToString() // 护士长
                },
                new Nurse
                {
                    Id = "N002",
                    EmployeeNumber = "nurse002",
                    PasswordHash = defaultHashedPassword,
                    Name = "赵护士",
                    IdCard = "110100200001010004",
                    Phone = "13912340004",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "SUR",
                    NurseRank = NurseRank.RegularNurse.ToString() // 普通护士
                }
            };

            // 行政人员数据 (Staff)
            var adminStaffs = new Staff[]
            {
                new Staff
                {
                    Id = "A001",
                    EmployeeNumber = "admin001", // 【建议】添加工号，保持一致性
                    PasswordHash = defaultHashedPassword,
                    Name = "刘管理员",
                    IdCard = "110100200001010005",
                    Phone = "13912340005",
                    RoleType = "Admin",
                    IsActive = true,
                    DeptCode = "ADM"
                }
            };
            
            // 将所有员工添加到数据库
            context.Doctors.AddRange(doctors);
            context.Nurses.AddRange(nurses);
            context.Staffs.AddRange(adminStaffs);
            
            context.SaveChanges();
        }

        private static void ClearAllData(ApplicationDbContext context)
        {
            // 注意：表名需要与数据库实际生成的表名一致
            // 如果 EF Core 使用了 Table-Per-Type (TPT)，Doctors 和 Nurses 会有单独的表
            // 必须使用 CASCADE 来处理外键约束
            var sql = @"
                TRUNCATE TABLE ""Departments"", ""Staffs"", ""Doctors"", ""Nurses"", ""Patients"", ""MedicalOrders"" RESTART IDENTITY CASCADE;
            ";

            try 
            {
                context.Database.ExecuteSqlRaw(sql);
            }
            catch (Exception ex)
            {
                // 如果是第一次运行，表可能还不存在，忽略此错误或仅记录警告
                // Console.WriteLine("清空数据提示 (如果是首次运行可忽略): " + ex.Message);
            }
        }
    }
}