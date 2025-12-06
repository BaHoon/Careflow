using CareFlow.Core.Models;
using CareFlow.Core.Models.Organization;
using CareFlow.Core.Enums;
using System.Security.Cryptography;
using System.Text;

namespace CareFlow.Infrastructure.Data
{
    public static class DbInitializer
    {
        // 辅助方法：计算 SHA256 哈希值并转换为十六进制字符串
        private static string HashPassword(string password)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // 计算哈希
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));

                // 转换为十六进制字符串
                // 对于较新的 .NET 版本（如 .NET 5+），可以使用 Convert.ToHexString(bytes)
                // 这里使用 StringBuilder 确保兼容性
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2")); // "x2" 表示两位十六进制，小写
                }
                return builder.ToString();
            }
        }

        public static void Initialize(ApplicationDbContext context)
        {
            // 确保数据库已创建
            context.Database.EnsureCreated();

            // 1. 检查是否已有数据
            if (context.Departments.Any())
            {
                return;   // 数据库已播种，直接返回
            }

            // 2. 预置数据
            
            // --- 预置科室数据 (Department) ---
            var departments = new Department[]
            {
                new Department { DeptId = "IM", DeptName = "内科" },
                new Department { DeptId = "SUR", DeptName = "外科" },
                new Department { DeptId = "PED", DeptName = "儿科" },
                new Department { DeptId = "ADM", DeptName = "行政" }
            };
            context.Departments.AddRange(departments);
            context.SaveChanges(); // 先保存科室，以便后续引用

            // --- 预置员工数据 (Staff/Doctor/Nurse) ---
            
            // 默认密码为 "123456"，计算其 SHA256 哈希值
            string defaultHashedPassword = HashPassword("123456");

            // 医生数据
            var doctors = new Doctor[]
            {
                new Doctor
                {
                    Id = "D001",
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

            // 行政人员数据 (Staff) - 假设行政人员使用 Staff 基类
            var adminStaffs = new Staff[]
            {
                new Staff
                {
                    Id = "A001",
                    PasswordHash = defaultHashedPassword,
                    Name = "刘管理员",
                    IdCard = "110100200001010005",
                    Phone = "13912340005",
                    RoleType = "Admin", // 假设 RoleType 为 Admin
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
    }
}