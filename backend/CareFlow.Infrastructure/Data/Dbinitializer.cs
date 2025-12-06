using CareFlow.Core.Models;
using CareFlow.Core.Models.Organization;
using CareFlow.Core.Models.Medical;
using CareFlow.Core.Models.Space;
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
                    EmployeeNumber = "admin001",
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

            // --- 预置病房和床位数据 ---
            var wards = new Ward[]
            {
                new Ward { WardId = "IM-W01", DeptId = "IM" },  // 内科病房1
                new Ward { WardId = "IM-W02", DeptId = "IM" },  // 内科病房2
                new Ward { WardId = "SUR-W01", DeptId = "SUR" }, // 外科病房1
                new Ward { WardId = "PED-W01", DeptId = "PED" }  // 儿科病房1
            };
            context.Wards.AddRange(wards);
            context.SaveChanges();

            var beds = new Bed[]
            {
                // 内科床位
                new Bed { BedId = "IM-W01-001", WardId = "IM-W01", Status = "占用" },
                new Bed { BedId = "IM-W01-002", WardId = "IM-W01", Status = "占用" },
                new Bed { BedId = "IM-W01-003", WardId = "IM-W01", Status = "空闲" },
                new Bed { BedId = "IM-W02-001", WardId = "IM-W02", Status = "占用" },
                
                // 外科床位
                new Bed { BedId = "SUR-W01-001", WardId = "SUR-W01", Status = "占用" },
                new Bed { BedId = "SUR-W01-002", WardId = "SUR-W01", Status = "占用" },
                
                // 儿科床位
                new Bed { BedId = "PED-W01-001", WardId = "PED-W01", Status = "占用" }
            };
            context.Beds.AddRange(beds);
            context.SaveChanges();

            // --- 预置患者数据 ---
            var patients = new Patient[]
            {
                new Patient
                {
                    PatientId = "P001",
                    Name = "张三",
                    Gender = "男",
                    IdCard = "110100199001010001",
                    DateOfBirth = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    Age = 34,
                    Weight = 70.5f,
                    Status = "在院",
                    PhoneNumber = "13800138001",
                    NursingGrade = 2,
                    BedId = "IM-W01-001",
                    AttendingDoctorId = "D001"
                },
                new Patient
                {
                    PatientId = "P002",
                    Name = "李四",
                    Gender = "女",
                    IdCard = "110100198505050002",
                    DateOfBirth = new DateTime(1985, 5, 5, 0, 0, 0, DateTimeKind.Utc),
                    Age = 39,
                    Weight = 58.0f,
                    Status = "在院",
                    PhoneNumber = "13800138002",
                    NursingGrade = 1,
                    BedId = "IM-W01-002",
                    AttendingDoctorId = "D001"
                },
                new Patient
                {
                    PatientId = "P003",
                    Name = "王五",
                    Gender = "男",
                    IdCard = "110100197803030003",
                    DateOfBirth = new DateTime(1978, 3, 3, 0, 0, 0, DateTimeKind.Utc),
                    Age = 46,
                    Weight = 75.2f,
                    Status = "在院",
                    PhoneNumber = "13800138003",
                    NursingGrade = 3,
                    BedId = "SUR-W01-001",
                    AttendingDoctorId = "D002"
                },
                new Patient
                {
                    PatientId = "P004",
                    Name = "赵六",
                    Gender = "女",
                    IdCard = "110100199212120004",
                    DateOfBirth = new DateTime(1992, 12, 12, 0, 0, 0, DateTimeKind.Utc),
                    Age = 32,
                    Weight = 62.8f,
                    Status = "在院",
                    PhoneNumber = "13800138004",
                    NursingGrade = 2,
                    BedId = "SUR-W01-002",
                    AttendingDoctorId = "D002"
                },
                new Patient
                {
                    PatientId = "P005",
                    Name = "钱七",
                    Gender = "男",
                    IdCard = "110100201501010005",
                    DateOfBirth = new DateTime(2015, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    Age = 9,
                    Weight = 28.5f,
                    Status = "在院",
                    PhoneNumber = "13800138005",
                    NursingGrade = 1,
                    BedId = "PED-W01-001",
                    AttendingDoctorId = "D001"
                },
                new Patient
                {
                    PatientId = "P006",
                    Name = "孙八",
                    Gender = "男",
                    IdCard = "110100196802020006",
                    DateOfBirth = new DateTime(1968, 2, 2, 0, 0, 0, DateTimeKind.Utc),
                    Age = 56,
                    Weight = 80.1f,
                    Status = "在院",
                    PhoneNumber = "13800138006",
                    NursingGrade = 3,
                    BedId = "IM-W02-001",
                    AttendingDoctorId = "D002"
                }
            };
            context.Patients.AddRange(patients);
            context.SaveChanges();

            // --- 预置各种类型的医疗医嘱 ---
            var currentTime = DateTime.UtcNow;

            // 1. 药品医嘱 (MedicationOrder) - 各种类型的用药医嘱
            var medicationOrders = new MedicationOrder[]
            {
                // 口服药物 - 长期医嘱
                new MedicationOrder
                {
                    PatientId = "P001",
                    DoctorId = "D001",
                    NurseId = "N001",
                    CreateTime = currentTime.AddDays(-2),
                    PlantEndTime = currentTime.AddDays(5),
                    OrderType = "MedicationOrder",
                    Status = "Accepted",
                    IsLongTerm = true,
                    DrugId = "DRUG001",
                    Dosage = "100mg",
                    UsageRoute = "口服",
                    IsDynamicUsage = false,
                    FreqCode = "BID",
                    StartTime = currentTime.AddDays(-2),
                    TimingStrategy = "CYCLIC",
                    SmartSlotsMask = 0b00100100, // 上午8点和下午8点
                    IntervalDays = 1
                },
                
                // 静脉滴注 - 临时医嘱
                new MedicationOrder
                {
                    PatientId = "P001",
                    DoctorId = "D001",
                    NurseId = "N002",
                    CreateTime = currentTime.AddHours(-3),
                    PlantEndTime = currentTime.AddHours(2),
                    OrderType = "MedicationOrder",
                    Status = "InProgress",
                    IsLongTerm = false,
                    DrugId = "DRUG002",
                    Dosage = "250ml",
                    UsageRoute = "静脉滴注",
                    IsDynamicUsage = false,
                    FreqCode = "ONCE",
                    StartTime = currentTime.AddHours(-3),
                    TimingStrategy = "IMMEDIATE",
                    SmartSlotsMask = 0,
                    IntervalDays = 0
                },
                
                // 胰岛素 - 餐前注射
                new MedicationOrder
                {
                    PatientId = "P002",
                    DoctorId = "D001",
                    NurseId = "N001",
                    CreateTime = currentTime.AddDays(-1),
                    PlantEndTime = currentTime.AddDays(7),
                    OrderType = "MedicationOrder",
                    Status = "Accepted",
                    IsLongTerm = true,
                    DrugId = "DRUG003",
                    Dosage = "8单位",
                    UsageRoute = "皮下注射",
                    IsDynamicUsage = false,
                    FreqCode = "TID",
                    StartTime = currentTime.AddDays(-1),
                    TimingStrategy = "SLOTS",
                    SmartSlotsMask = 0b01001001, // 餐前时段
                    IntervalDays = 1
                },
                
                // 吸氧 - 不定量医嘱
                new MedicationOrder
                {
                    PatientId = "P003",
                    DoctorId = "D002",
                    NurseId = "N002",
                    CreateTime = currentTime.AddDays(-1),
                    PlantEndTime = currentTime.AddDays(3),
                    OrderType = "MedicationOrder",
                    Status = "Accepted",
                    IsLongTerm = true,
                    DrugId = "OXYGEN001",
                    Dosage = "2L/min",
                    UsageRoute = "鼻导管吸氧",
                    IsDynamicUsage = true,
                    FreqCode = "CONT",
                    StartTime = currentTime.AddDays(-1),
                    TimingStrategy = "CYCLIC",
                    SmartSlotsMask = 0b11111111, // 全天
                    IntervalDays = 1
                },
                
                // 外用药膏
                new MedicationOrder
                {
                    PatientId = "P004",
                    DoctorId = "D002",
                    CreateTime = currentTime.AddDays(-1),
                    PlantEndTime = currentTime.AddDays(5),
                    OrderType = "MedicationOrder", 
                    Status = "Accepted",
                    IsLongTerm = true,
                    DrugId = "DRUG004",
                    Dosage = "适量",
                    UsageRoute = "外用涂抹",
                    IsDynamicUsage = false,
                    FreqCode = "BID",
                    StartTime = currentTime.AddDays(-1),
                    TimingStrategy = "SLOTS",
                    SmartSlotsMask = 0b10000001, // 早晚
                    IntervalDays = 1
                }
            };
            context.MedicationOrders.AddRange(medicationOrders);
            
            // 2. 操作医嘱 (OperationOrder)
            var operationOrders = new OperationOrder[]
            {
                // 测量血压
                new OperationOrder
                {
                    PatientId = "P001",
                    DoctorId = "D001",
                    NurseId = "N001",
                    CreateTime = currentTime.AddDays(-1),
                    PlantEndTime = currentTime.AddDays(3),
                    OrderType = "OperationOrder",
                    Status = "Accepted",
                    IsLongTerm = true,
                    OpId = "OP001",
                    Normal = true,
                    FrequencyType = "每天",
                    FrequencyValue = "3次"
                },
                
                // 心电监护
                new OperationOrder
                {
                    PatientId = "P003",
                    DoctorId = "D002",
                    NurseId = "N002",
                    CreateTime = currentTime.AddDays(-1),
                    PlantEndTime = currentTime.AddDays(2),
                    OrderType = "OperationOrder",
                    Status = "Accepted",
                    IsLongTerm = true,
                    OpId = "OP002",
                    Normal = true,
                    FrequencyType = "持续",
                    FrequencyValue = "24小时"
                },
                
                // 换药处理
                new OperationOrder
                {
                    PatientId = "P004",
                    DoctorId = "D002",
                    NurseId = "N001",
                    CreateTime = currentTime.AddHours(-2),
                    PlantEndTime = currentTime.AddHours(1),
                    OrderType = "OperationOrder",
                    Status = "InProgress",
                    IsLongTerm = false,
                    OpId = "OP003",
                    Normal = true,
                    FrequencyType = "一次性",
                    FrequencyValue = "1次"
                }
            };
            context.OperationOrders.AddRange(operationOrders);
            
            // 3. 检查医嘱 (InspectionOrder)
            var inspectionOrders = new InspectionOrder[]
            {
                // CT检查
                new InspectionOrder
                {
                    PatientId = "P002",
                    DoctorId = "D001",
                    CreateTime = currentTime.AddHours(-4),
                    PlantEndTime = currentTime.AddHours(8),
                    OrderType = "InspectionOrder",
                    Status = "PendingReview",
                    IsLongTerm = false,
                    ItemCode = "CT001",
                    RisLisId = "RIS202412060001",
                    Location = "影像科",
                    AppointmentTime = currentTime.AddHours(4),
                    AppointmentPlace = "CT室1",
                    Precautions = "检查前4小时禁食",
                    ReportId = ""
                },
                
                // 血常规检查
                new InspectionOrder
                {
                    PatientId = "P005",
                    DoctorId = "D001",
                    CreateTime = currentTime.AddHours(-1),
                    PlantEndTime = currentTime.AddHours(6),
                    OrderType = "InspectionOrder",
                    Status = "Accepted",
                    IsLongTerm = false,
                    ItemCode = "LAB001",
                    RisLisId = "LIS202412060001",
                    Location = "检验科",
                    AppointmentTime = currentTime.AddHours(2),
                    AppointmentPlace = "采血室",
                    Precautions = "空腹采血",
                    ReportId = ""
                },
                
                // 心电图检查 - 已完成
                new InspectionOrder
                {
                    PatientId = "P006",
                    DoctorId = "D002",
                    CreateTime = currentTime.AddDays(-1),
                    PlantEndTime = currentTime.AddHours(-2),
                    EndTime = currentTime.AddHours(-2),
                    OrderType = "InspectionOrder",
                    Status = "Completed",
                    IsLongTerm = false,
                    ItemCode = "ECG001",
                    RisLisId = "RIS202412050001",
                    Location = "心电图室",
                    AppointmentTime = currentTime.AddHours(-4),
                    AppointmentPlace = "心电图室1",
                    Precautions = "检查时保持安静",
                    CheckStartTime = currentTime.AddHours(-4),
                    CheckEndTime = currentTime.AddHours(-3.5),
                    BackToWardTime = currentTime.AddHours(-3),
                    ReportTime = currentTime.AddHours(-2),
                    ReportId = "ECG202412050001"
                }
            };
            context.InspectionOrders.AddRange(inspectionOrders);
            
            // 4. 手术医嘱 (SurgicalOrder)
            var surgicalOrders = new SurgicalOrder[]
            {
                // 阑尾切除术 - 准备中
                new SurgicalOrder
                {
                    PatientId = "P003",
                    DoctorId = "D002",
                    CreateTime = currentTime.AddHours(-6),
                    PlantEndTime = currentTime.AddHours(8),
                    OrderType = "SurgicalOrder",
                    Status = "Accepted",
                    IsLongTerm = false,
                    SurgeryName = "腹腔镜阑尾切除术",
                    ScheduleTime = currentTime.AddHours(6),
                    AnesthesiaType = "全身麻醉",
                    IncisionSite = "脐部及右下腹",
                    RequiredMeds = "[\"头孢曲松\", \"丙泊酚\", \"瑞芬太尼\"]",
                    NeedBloodPrep = false,
                    HasImplants = false,
                    PrepProgress = 0.6f,
                    PrepStatus = "术前准备中"
                },
                
                // 胆囊切除术 - 已排期
                new SurgicalOrder
                {
                    PatientId = "P004",
                    DoctorId = "D002",
                    CreateTime = currentTime.AddDays(-1),
                    PlantEndTime = currentTime.AddDays(1),
                    OrderType = "SurgicalOrder",
                    Status = "PendingReview",
                    IsLongTerm = false,
                    SurgeryName = "腹腔镜胆囊切除术",
                    ScheduleTime = currentTime.AddDays(1).AddHours(2),
                    AnesthesiaType = "全身麻醉",
                    IncisionSite = "脐部及上腹部",
                    RequiredMeds = "[\"头孢西丁\", \"丙泊酚\", \"舒芬太尼\"]",
                    NeedBloodPrep = true,
                    HasImplants = false,
                    PrepProgress = 0.2f,
                    PrepStatus = "等待术前评估"
                },
                
                // 骨折内固定术 - 紧急手术
                new SurgicalOrder
                {
                    PatientId = "P006",
                    DoctorId = "D002",
                    CreateTime = currentTime.AddHours(-2),
                    PlantEndTime = currentTime.AddHours(4),
                    OrderType = "SurgicalOrder",
                    Status = "Accepted",
                    IsLongTerm = false,
                    SurgeryName = "左股骨干骨折切开复位内固定术",
                    ScheduleTime = currentTime.AddHours(2),
                    AnesthesiaType = "腰硬联合麻醉",
                    IncisionSite = "左大腿外侧",
                    RequiredMeds = "[\"头孢唑林\", \"罗哌卡因\", \"吗啡\"]",
                    NeedBloodPrep = true,
                    HasImplants = true,
                    PrepProgress = 0.8f,
                    PrepStatus = "急诊准备中"
                }
            };
            context.SurgicalOrders.AddRange(surgicalOrders);
            
            context.SaveChanges();
        }

        private static void ClearAllData(ApplicationDbContext context)
        {
            // 注意：表名需要与数据库实际生成的表名一致
            // 如果 EF Core 使用了 Table-Per-Type (TPT)，Doctors 和 Nurses 会有单独的表
            // 必须使用 CASCADE 来处理外键约束
            var sql = @"
                TRUNCATE TABLE ""Departments"", ""Staffs"", ""Doctors"", ""Nurses"", ""Patients"", ""Wards"", ""Beds"", ""MedicalOrders"", ""MedicationOrders"", ""OperationOrders"", ""InspectionOrders"", ""SurgicalOrders"" RESTART IDENTITY CASCADE;
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