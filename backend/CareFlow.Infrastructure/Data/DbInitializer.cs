using CareFlow.Core.Models;
using CareFlow.Core.Enums;
using System.Security.Cryptography;
using System.Text;

namespace CareFlow.Infrastructure.Data
{
    public static class DbInitializer
    {
        public static void Initialize(ApplicationDbContext context)
        {
            // 确保数据库已创建
            context.Database.EnsureCreated();

            // 1. 检查是否已有数据
            if (context.Departments.Any())
            {
                return;   // 数据库已播种，直接返回
            }

            // ==========================================
            // 2. 预置基础数据：科室 (Departments)
            // ==========================================
            var depts = new Department[]
            {
                new Department { Id = 1, DeptName = "心内科", IsClinical = true },
                new Department { Id = 2, DeptName = "急诊科", IsClinical = true },
                new Department { Id = 3, DeptName = "骨科", IsClinical = true },
                new Department { Id = 4, DeptName = "药房", IsClinical = false },
                new Department { Id = 5, DeptName = "行政部", IsClinical = false }
            };
            
            context.Departments.AddRange(depts);
            context.SaveChanges();

            // ==========================================
            // 3. 预置空间数据：病房与床位 (Rooms & Beds)
            // ==========================================
            var rooms = new Room[]
            {
                new Room 
                { 
                    // Id = 101, 
                    DeptId = 1, 
                    Building = "住院楼A", 
                    Floor = 10, 
                    RoomNumber = "1001", 
                    GenderConstraint = RoomGenderType.Mixed, 
                    Type = RoomType.General          
                },
                new Room 
                { 
                    // Id = 102, 
                    DeptId = 1, 
                    Building = "住院楼A", 
                    Floor = 10, 
                    RoomNumber = "1002", 
                    GenderConstraint = RoomGenderType.MaleOnly, 
                    Type = RoomType.ICU 
                }
            };
            context.Rooms.AddRange(rooms);
            context.SaveChanges();

            // 获取生成的 Room 对象以拿到 ID
            var room1 = context.Rooms.FirstOrDefault(r => r.RoomNumber == "1001");
            var room2 = context.Rooms.FirstOrDefault(r => r.RoomNumber == "1002");

            if (room1 != null && room2 != null)
            {
                var beds = new Bed[]
                {
                    // 1001房 3张床
                    new Bed { RoomId = room1.Id, BedLabel = "1001-1", Status = BedStatus.Available, PricePerDay = 50m },
                    new Bed { RoomId = room1.Id, BedLabel = "1001-2", Status = BedStatus.Available, PricePerDay = 50m },
                    new Bed { RoomId = room1.Id, BedLabel = "1001-3", Status = BedStatus.Occupied, PricePerDay = 50m },
                    // 1002房 (ICU) 1张床
                    new Bed { RoomId = room2.Id, BedLabel = "1002-1", Status = BedStatus.Available, PricePerDay = 200m }
                };
                context.Beds.AddRange(beds);
                context.SaveChanges();
            }

            // ==========================================
            // 4. 预置医疗数据：药品字典 (Medications)
            // ==========================================
            var drugs = new Medication[]
            {
                new Medication { DrugName = "阿莫西林胶囊", TradeName = "阿莫仙", Category = DrugCategory.Oral, Specification = "0.25g*24粒", Price = 15.50m, StockQuantity = 1000 },
                new Medication { DrugName = "0.9%氯化钠注射液", TradeName = "生理盐水", Category = DrugCategory.Injection, Specification = "250ml", Price = 5.00m, StockQuantity = 500 },
                new Medication { DrugName = "布洛芬缓释胶囊", TradeName = "芬必得", Category = DrugCategory.Oral, Specification = "0.3g*20粒", Price = 22.00m, StockQuantity = 200 },
                new Medication { DrugName = "头孢曲松钠", TradeName = "罗氏芬", Category = DrugCategory.Injection, Specification = "1.0g", Price = 45.00m, StockQuantity = 300 }
            };
            context.Medications.AddRange(drugs);
            context.SaveChanges();

            // ==========================================
            // 5. 预置人员账号 (Staff)
            // ==========================================
            string mockHash = GenerateSimpleHash("123456"); 

            // A. 管理员 (Admin)
            var admin = new StaffBase
            {
                EmployeeNumber = "admin001",
                PasswordHash = mockHash,
                FullName = "系统管理员",
                StaffType = StaffType.Admin, //
                DeptId = 5, 
                IsActive = true,
                CreateTime = DateTime.UtcNow
            };
            context.Staffs.Add(admin);

            // B. 医生 (Doctor - 主任)
            var doctor1 = new Doctor
            {
                EmployeeNumber = "doc001",
                PasswordHash = mockHash,
                FullName = "张主任",
                StaffType = StaffType.Doctor, //
                DeptId = 1, 
                IsActive = true,
                CreateTime = DateTime.UtcNow,
                Title = DoctorTitle.Chief, //
                PrescriptionLevel = 3,
                Specialty = "冠心病介入治疗"
            };
            context.Doctors.Add(doctor1);

            // C. 医生 (Doctor - 住院医)
            var doctor2 = new Doctor
            {
                EmployeeNumber = "doc002",
                PasswordHash = mockHash,
                FullName = "王小医",
                StaffType = StaffType.Doctor,
                DeptId = 1, 
                IsActive = true,
                CreateTime = DateTime.UtcNow,
                Title = DoctorTitle.Resident, //
                PrescriptionLevel = 1,
                Specialty = "心内科基础"
            };
            context.Doctors.Add(doctor2);

            // D. 护士 (Nurse - 护士长)
            var nurse1 = new Nurse
            {
                EmployeeNumber = "nurse001",
                PasswordHash = mockHash,
                FullName = "李护士长",
                StaffType = StaffType.Nurse, //
                DeptId = 1, 
                IsActive = true,
                CreateTime = DateTime.UtcNow,
                Rank = NurseRank.HeadNurse, //
                PrimaryView = "护理管理"
            };
            context.Nurses.Add(nurse1);

            // E. 护士 (Nurse - 普通护士)
            var nurse2 = new Nurse
            {
                EmployeeNumber = "nurse002",
                PasswordHash = mockHash,
                FullName = "赵护士",
                StaffType = StaffType.Nurse,
                DeptId = 1, 
                IsActive = true,
                CreateTime = DateTime.UtcNow,
                Rank = NurseRank.RegularNurse, //
                PrimaryView = "配药"
            };
            context.Nurses.Add(nurse2);

            context.SaveChanges();
        }

        private static string GenerateSimpleHash(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(bytes);
            }
        }
    }
}