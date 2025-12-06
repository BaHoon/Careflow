using CareFlow.Core.Enums;
using CareFlow.Core.Models;
using CareFlow.Core.Models.Medical;
using CareFlow.Core.Models.Organization;
using CareFlow.Core.Models.Space;
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

        private static DateTime UtcDate(int year, int month, int day, int hour = 0, int minute = 0)
        {
            return new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Utc);
        }

        public static void Initialize(ApplicationDbContext context)
        {
            // 确保数据库已创建
            context.Database.EnsureCreated();

            var nowUtc = DateTime.UtcNow;
            var todayUtc = DateTime.SpecifyKind(nowUtc.Date, DateTimeKind.Utc);

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

            // --- 预置病区/床位 (Ward/Bed) ---
            var wards = new Ward[]
            {
                new Ward { WardId = "WARD_SURG_A", DeptId = "SUR", Department = departments.First(d => d.DeptId == "SUR") },
                new Ward { WardId = "WARD_IM_A", DeptId = "IM", Department = departments.First(d => d.DeptId == "IM") }
            };
            context.Wards.AddRange(wards);
            context.SaveChanges();

            var beds = new Bed[]
            {
                new Bed { BedId = "SUR-401-A", WardId = "WARD_SURG_A", Ward = wards.First(w => w.WardId == "WARD_SURG_A"), Status = "已占用" },
                new Bed { BedId = "SUR-401-B", WardId = "WARD_SURG_A", Ward = wards.First(w => w.WardId == "WARD_SURG_A"), Status = "空床" },
                new Bed { BedId = "IM-301-A", WardId = "WARD_IM_A", Ward = wards.First(w => w.WardId == "WARD_IM_A"), Status = "已占用" }
            };
            context.Beds.AddRange(beds);
            context.SaveChanges();

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

            // --- 预置患者数据 (Patient) ---
            var patients = new Patient[]
            {
                new Patient
                {
                    PatientId = "P001",
                    Name = "陈术前",
                    Gender = "F",
                    IdCard = "110100200001010101",
                    DateOfBirth = UtcDate(1990, 5, 12),
                    Age = 35,
                    Weight = 60,
                    Status = "住院中",
                    PhoneNumber = "13800000001",
                    NursingGrade = 2,
                    BedId = beds.First(b => b.BedId == "SUR-401-A").BedId,
                    Bed = beds.First(b => b.BedId == "SUR-401-A"),
                    AttendingDoctorId = doctors.First().Id,
                    AttendingDoctor = doctors.First()
                },
                new Patient
                {
                    PatientId = "P002",
                    Name = "周术备",
                    Gender = "M",
                    IdCard = "110100200001010202",
                    DateOfBirth = UtcDate(1985, 11, 23),
                    Age = 40,
                    Weight = 72,
                    Status = "术前待命",
                    PhoneNumber = "13800000002",
                    NursingGrade = 3,
                    BedId = beds.First(b => b.BedId == "IM-301-A").BedId,
                    Bed = beds.First(b => b.BedId == "IM-301-A"),
                    AttendingDoctorId = doctors.Last().Id,
                    AttendingDoctor = doctors.Last()
                }
            };
            context.Patients.AddRange(patients);
            context.SaveChanges();

            // --- 预置药品医嘱数据 (MedicationOrder) ---
            var medicationOrders = new MedicationOrder[]
            {
                new MedicationOrder
                {
                    PatientId = patients[0].PatientId,
                    Patient = patients[0],
                    DoctorId = doctors[0].Id,
                    Doctor = doctors[0],
                    NurseId = nurses[0].Id,
                    Nurse = nurses[0],
                    CreateTime = nowUtc.AddHours(-8),
                    PlantEndTime = nowUtc.AddHours(12),
                    EndTime = null,
                    OrderType = "药品",
                    Status = "执行中",
                    IsLongTerm = false,
                    DrugId = "DRUG-CTX",
                    Dosage = "1.0g",
                    UsageRoute = "静脉滴注",
                    IsDynamicUsage = false,
                    FreqCode = "Q8H",
                    StartTime = nowUtc.AddHours(-2),
                    TimingStrategy = "SPECIFIC",
                    SpecificExecutionTime = nowUtc.AddHours(2),
                    SmartSlotsMask = 0b0001,
                    IntervalDays = 1
                },
                new MedicationOrder
                {
                    PatientId = patients[1].PatientId,
                    Patient = patients[1],
                    DoctorId = doctors[1].Id,
                    Doctor = doctors[1],
                    NurseId = nurses[1].Id,
                    Nurse = nurses[1],
                    CreateTime = nowUtc.AddDays(-1),
                    PlantEndTime = nowUtc.AddDays(3),
                    EndTime = null,
                    OrderType = "药品",
                    Status = "待核对",
                    IsLongTerm = true,
                    DrugId = "DRUG-AMI",
                    Dosage = "5mg",
                    UsageRoute = "口服",
                    IsDynamicUsage = false,
                    FreqCode = "QD",
                    StartTime = nowUtc,
                    TimingStrategy = "CYCLIC",
                    SpecificExecutionTime = null,
                    SmartSlotsMask = 0b0010,
                    IntervalDays = 1
                }
            };
            context.MedicationOrders.AddRange(medicationOrders);
            context.SaveChanges();

            // --- 预置检查医嘱数据 (InspectionOrder) ---
            var inspectionOrders = new InspectionOrder[]
            {
                new InspectionOrder
                {
                    PatientId = patients[0].PatientId,
                    Patient = patients[0],
                    DoctorId = doctors[0].Id,
                    Doctor = doctors[0],
                    NurseId = nurses[0].Id,
                    Nurse = nurses[0],
                    CreateTime = nowUtc.AddHours(-4),
                    PlantEndTime = nowUtc.AddHours(4),
                    EndTime = null,
                    OrderType = "检查",
                    Status = "已预约",
                    IsLongTerm = false,
                    ItemCode = "CT-ABD",
                    RisLisId = "RIS20251206001",
                    Location = "影像中心",
                    AppointmentTime = nowUtc.AddHours(2),
                    AppointmentPlace = "CT室2",
                    Precautions = "检查前禁食4小时",
                    CheckStartTime = null,
                    CheckEndTime = null,
                    BackToWardTime = null,
                    ReportTime = null,
                    ReportId = "REP-CT-001"
                },
                new InspectionOrder
                {
                    PatientId = patients[1].PatientId,
                    Patient = patients[1],
                    DoctorId = doctors[1].Id,
                    Doctor = doctors[1],
                    NurseId = nurses[1].Id,
                    Nurse = nurses[1],
                    CreateTime = nowUtc.AddDays(-2),
                    PlantEndTime = nowUtc.AddDays(1),
                    EndTime = null,
                    OrderType = "检查",
                    Status = "报告回传",
                    IsLongTerm = false,
                    ItemCode = "LAB-COAG",
                    RisLisId = "LIS20251205011",
                    Location = "检验科",
                    AppointmentTime = nowUtc.AddHours(-6),
                    AppointmentPlace = "采血室A",
                    Precautions = "采血后压迫5分钟",
                    CheckStartTime = nowUtc.AddHours(-5),
                    CheckEndTime = nowUtc.AddHours(-4),
                    BackToWardTime = nowUtc.AddHours(-3.5),
                    ReportTime = nowUtc.AddHours(-1),
                    ReportId = "REP-LAB-011"
                }
            };
            context.InspectionOrders.AddRange(inspectionOrders);
            context.SaveChanges();

            // --- 预置操作/护理医嘱数据 (OperationOrder) ---
            var operationOrders = new OperationOrder[]
            {
                new OperationOrder
                {
                    PatientId = patients[0].PatientId,
                    Patient = patients[0],
                    DoctorId = doctors[0].Id,
                    Doctor = doctors[0],
                    NurseId = nurses[0].Id,
                    Nurse = nurses[0],
                    CreateTime = nowUtc.AddHours(-10),
                    PlantEndTime = nowUtc.AddHours(-2),
                    EndTime = nowUtc.AddHours(-1),
                    OrderType = "操作",
                    Status = "已完成",
                    IsLongTerm = false,
                    OpId = "OP-BATH",
                    Normal = true,
                    FrequencyType = "单次",
                    FrequencyValue = "1"
                },
                new OperationOrder
                {
                    PatientId = patients[1].PatientId,
                    Patient = patients[1],
                    DoctorId = doctors[1].Id,
                    Doctor = doctors[1],
                    NurseId = nurses[1].Id,
                    Nurse = nurses[1],
                    CreateTime = nowUtc.AddHours(-3),
                    PlantEndTime = nowUtc.AddHours(5),
                    EndTime = null,
                    OrderType = "操作",
                    Status = "执行中",
                    IsLongTerm = true,
                    OpId = "OP-TURN",
                    Normal = true,
                    FrequencyType = "每天",
                    FrequencyValue = "Q6H"
                }
            };
            context.OperationOrders.AddRange(operationOrders);
            context.SaveChanges();

            // --- 预置手术医嘱数据 (SurgicalOrder，字段内容使用中文描述) ---
            var surgicalOrders = new SurgicalOrder[]
            {
                new SurgicalOrder
                {
                    PatientId = patients[0].PatientId,
                    Patient = patients[0],
                    DoctorId = doctors[0].Id,
                    Doctor = doctors[0],
                    NurseId = nurses[0].Id,
                    Nurse = nurses[0],
                    CreateTime = nowUtc.AddHours(-6),
                    PlantEndTime = nowUtc.AddHours(6),
                    EndTime = null,
                    OrderType = "手术",
                    Status = "待执行",
                    IsLongTerm = false,
                    SurgeryName = "腹腔镜胆囊切除术",
                    ScheduleTime = todayUtc.AddHours(10),
                    AnesthesiaType = "硬膜外联合麻醉",
                    IncisionSite = "右上腹肋缘",
                    RequiredMeds = "{\"items\":[{\"name\":\"术前碘伏\",\"qty\":\"2瓶\"},{\"name\":\"0.9%氯化钠\",\"qty\":\"500ml\"}]}",
                    NeedBloodPrep = true,
                    HasImplants = false,
                    PrepProgress = 0.35f,
                    PrepStatus = "器械核对中"
                },
                new SurgicalOrder
                {
                    PatientId = patients[1].PatientId,
                    Patient = patients[1],
                    DoctorId = doctors[1].Id,
                    Doctor = doctors[1],
                    NurseId = nurses[1].Id,
                    Nurse = nurses[1],
                    CreateTime = nowUtc.AddDays(-1),
                    PlantEndTime = nowUtc.AddDays(1),
                    EndTime = null,
                    OrderType = "手术",
                    Status = "已排程",
                    IsLongTerm = false,
                    SurgeryName = "经皮肾镜结石取出术",
                    ScheduleTime = todayUtc.AddDays(1).AddHours(8),
                    AnesthesiaType = "全身麻醉",
                    IncisionSite = "左腰部穿刺点",
                    RequiredMeds = "{\"items\":[{\"name\":\"输尿管支架\",\"qty\":\"1套\"},{\"name\":\"止血纱布\",\"qty\":\"3包\"}]}",
                    NeedBloodPrep = false,
                    HasImplants = true,
                    PrepProgress = 0.6f,
                    PrepStatus = "材料确认完成"
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
                TRUNCATE TABLE ""Departments"", ""Staffs"", ""Doctors"", ""Nurses"", ""Patients"", ""Wards"", ""Beds"", ""MedicalOrders"" RESTART IDENTITY CASCADE;
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