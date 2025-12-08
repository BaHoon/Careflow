using CareFlow.Core.Enums;
using CareFlow.Core.Models;
using CareFlow.Core.Models.Medical;
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

        private static DateTime UtcDate(int year, int month, int day, int hour = 0, int minute = 0)
        {
            return new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Utc);
        }

        public static void Initialize(ApplicationDbContext context)
        {
            // 删除现有数据库并重新创建（由于模型结构改变较大）
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            // 1. 检查是否已有数据（重新创建后应该是空的）
            if (context.Departments.Any())
            {
                return;   // 数据库已播种，直接返回
            }

            // 2. 预置数据
            
            // --- 预置科室数据 (Department) ---
            // 【修正】添加了 Location 字段，解决 "null value in column Location" 错误
            var departments = new Department[]
            {
                new Department { Id = "IM", DeptName = "内科", Location = "住院部A栋3楼" },
                new Department { Id = "SUR", DeptName = "外科", Location = "住院部A栋4楼" },
                new Department { Id = "PED", DeptName = "儿科", Location = "住院部B栋2楼" },
                new Department { Id = "ADM", DeptName = "行政", Location = "行政楼101室" }
            };
            context.Departments.AddRange(departments);
            context.SaveChanges(); // 保存科室

            // --- 预置时间槽位数据 (HospitalTimeSlot) ---
            // 使用2的幂次作为SlotId，便于位掩码操作和组合使用
            var timeSlots = new HospitalTimeSlot[]
            {
                // 基础时间槽位 - 餐食相关
                new HospitalTimeSlot { Id = 1, SlotCode = "PRE_BREAKFAST", SlotName = "早餐前", DefaultTime = new TimeSpan(7, 0, 0), OffsetMinutes = 15 },
                new HospitalTimeSlot { Id = 2, SlotCode = "POST_BREAKFAST", SlotName = "早餐后", DefaultTime = new TimeSpan(8, 30, 0), OffsetMinutes = 30 },
                new HospitalTimeSlot { Id = 4, SlotCode = "PRE_LUNCH", SlotName = "午餐前", DefaultTime = new TimeSpan(11, 30, 0), OffsetMinutes = 15 },
                new HospitalTimeSlot { Id = 8, SlotCode = "POST_LUNCH", SlotName = "午餐后", DefaultTime = new TimeSpan(13, 0, 0), OffsetMinutes = 30 },
                new HospitalTimeSlot { Id = 16, SlotCode = "PRE_DINNER", SlotName = "晚餐前", DefaultTime = new TimeSpan(17, 30, 0), OffsetMinutes = 15 },
                new HospitalTimeSlot { Id = 32, SlotCode = "POST_DINNER", SlotName = "晚餐后", DefaultTime = new TimeSpan(19, 0, 0), OffsetMinutes = 30 },
                new HospitalTimeSlot { Id = 64, SlotCode = "BEDTIME", SlotName = "睡前", DefaultTime = new TimeSpan(21, 0, 0), OffsetMinutes = 30 },
                new HospitalTimeSlot { Id = 128, SlotCode = "MIDNIGHT", SlotName = "夜间", DefaultTime = new TimeSpan(0, 0, 0), OffsetMinutes = 60 },

                // 扩展时间槽位 - 特殊用途
                new HospitalTimeSlot { Id = 256, SlotCode = "EARLY_MORNING", SlotName = "清晨", DefaultTime = new TimeSpan(6, 0, 0), OffsetMinutes = 30 },
                new HospitalTimeSlot { Id = 512, SlotCode = "MORNING", SlotName = "上午", DefaultTime = new TimeSpan(9, 0, 0), OffsetMinutes = 60 },
                new HospitalTimeSlot { Id = 1024, SlotCode = "NOON", SlotName = "中午", DefaultTime = new TimeSpan(12, 0, 0), OffsetMinutes = 30 },
                new HospitalTimeSlot { Id = 2048, SlotCode = "AFTERNOON", SlotName = "下午", DefaultTime = new TimeSpan(15, 0, 0), OffsetMinutes = 60 },
                new HospitalTimeSlot { Id = 4096, SlotCode = "EVENING", SlotName = "傍晚", DefaultTime = new TimeSpan(18, 0, 0), OffsetMinutes = 30 },
                new HospitalTimeSlot { Id = 8192, SlotCode = "NIGHT", SlotName = "夜晚", DefaultTime = new TimeSpan(22, 0, 0), OffsetMinutes = 60 },
                new HospitalTimeSlot { Id = 16384, SlotCode = "LATE_NIGHT", SlotName = "深夜", DefaultTime = new TimeSpan(2, 0, 0), OffsetMinutes = 60 },
                new HospitalTimeSlot { Id = 32768, SlotCode = "DAWN", SlotName = "黎明", DefaultTime = new TimeSpan(4, 0, 0), OffsetMinutes = 30 },

                // 特殊医疗时段
                new HospitalTimeSlot { Id = 65536, SlotCode = "EMERGENCY", SlotName = "紧急", DefaultTime = new TimeSpan(0, 0, 0), OffsetMinutes = 0 },
                new HospitalTimeSlot { Id = 131072, SlotCode = "STAT", SlotName = "立即", DefaultTime = new TimeSpan(0, 0, 0), OffsetMinutes = 0 },
                new HospitalTimeSlot { Id = 262144, SlotCode = "PRN", SlotName = "必要时", DefaultTime = new TimeSpan(0, 0, 0), OffsetMinutes = 0 },
                
                // 监护时段
                new HospitalTimeSlot { Id = 524288, SlotCode = "CONTINUOUS", SlotName = "持续", DefaultTime = new TimeSpan(0, 0, 0), OffsetMinutes = 0 }
            };
            context.HospitalTimeSlots.AddRange(timeSlots);
            context.SaveChanges(); // 保存时间槽位

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
                new Ward { Id = "IM-W01", DepartmentId = "IM" },  // 内科病房1
                new Ward { Id = "IM-W02", DepartmentId = "IM" },  // 内科病房2
                new Ward { Id = "SUR-W01", DepartmentId = "SUR" }, // 外科病房1
                new Ward { Id = "PED-W01", DepartmentId = "PED" }  // 儿科病房1
            };
            context.Wards.AddRange(wards);
            context.SaveChanges();

            var beds = new Bed[]
            {
                // 内科床位
                new Bed { Id = "IM-W01-001", WardId = "IM-W01", Status = "占用" },
                new Bed { Id = "IM-W01-002", WardId = "IM-W01", Status = "占用" },
                new Bed { Id = "IM-W01-003", WardId = "IM-W01", Status = "空闲" },
                new Bed { Id = "IM-W02-001", WardId = "IM-W02", Status = "占用" },
                
                // 外科床位
                new Bed { Id = "SUR-W01-001", WardId = "SUR-W01", Status = "占用" },
                new Bed { Id = "SUR-W01-002", WardId = "SUR-W01", Status = "占用" },
                
                // 儿科床位
                new Bed { Id = "PED-W01-001", WardId = "PED-W01", Status = "占用" }
            };
            context.Beds.AddRange(beds);
            context.SaveChanges();

            // --- 预置患者数据 ---
            var patients = new Patient[]
            {
                new Patient
                {
                    Id = "P001",
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
                    Id = "P002",
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
                    Id = "P003",
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
                    Id = "P004",
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
                    Id = "P005",
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
                    Id = "P006",
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

            // --- 预置药品数据 (Drug) ---
            var drugs = new Drug[]
            {
                // 常用内科药品
                new Drug
                {
                    Id = "DRUG001",
                    GenericName = "阿司匹林片",
                    TradeName = "拜阿司匹林",
                    Manufacturer = "拜耳医药保健有限公司",
                    Specification = "100mg/片",
                    DosageForm = "片剂",
                    PackageSpec = "30片/盒",
                    AtcCode = "B01AC06",
                    Category = "抗血栓药物",
                    AdministrationRoute = "口服",
                    UnitPrice = 0.85m,
                    PriceUnit = "片",
                    IsPrescriptionOnly = false,
                    IsNarcotic = false,
                    IsPsychotropic = false,
                    IsAntibiotic = false,
                    Status = "Active",
                    Indications = "预防血栓形成，缓解轻至中度疼痛",
                    Contraindications = "活动性消化道出血，血友病",
                    DosageInstructions = "成人一次100mg，一日1-2次，餐后服用",
                    SideEffects = "胃肠道反应，出血",
                    StorageConditions = "密封，在干燥处保存",
                    ShelfLifeMonths = 36,
                    ApprovalNumber = "国药准字H20013062",
                    Remarks = "小剂量用于心脑血管疾病预防"
                },
                
                new Drug
                {
                    Id = "DRUG002",
                    GenericName = "0.9%氯化钠注射液",
                    TradeName = "生理盐水",
                    Manufacturer = "石家庄四药有限公司",
                    Specification = "250ml/袋",
                    DosageForm = "注射液",
                    PackageSpec = "250ml/袋",
                    AtcCode = "B05BB01",
                    Category = "电解质溶液",
                    AdministrationRoute = "静脉滴注",
                    UnitPrice = 2.50m,
                    PriceUnit = "袋",
                    IsPrescriptionOnly = false,
                    IsNarcotic = false,
                    IsPsychotropic = false,
                    IsAntibiotic = false,
                    Status = "Active",
                    Indications = "各种原因所致的失水，包括低渗性、等渗性和高渗性失水",
                    Contraindications = "高钠血症，水肿",
                    DosageInstructions = "静脉滴注，用量根据患者年龄、体重及临床症状而定",
                    SideEffects = "水钠潴留",
                    StorageConditions = "密闭保存",
                    ShelfLifeMonths = 24,
                    ApprovalNumber = "国药准字H13022396",
                    Remarks = "基础输液制剂"
                },
                
                new Drug
                {
                    Id = "DRUG003",
                    GenericName = "精蛋白锌重组人胰岛素注射液",
                    TradeName = "优泌林N",
                    Manufacturer = "礼来苏州制药有限公司",
                    Specification = "100IU/ml，3ml/支",
                    DosageForm = "注射液",
                    PackageSpec = "3ml×5支/盒",
                    AtcCode = "A10AC01",
                    Category = "胰岛素及其类似物",
                    AdministrationRoute = "皮下注射",
                    UnitPrice = 68.50m,
                    PriceUnit = "支",
                    IsPrescriptionOnly = true,
                    IsNarcotic = false,
                    IsPsychotropic = false,
                    IsAntibiotic = false,
                    Status = "Active",
                    Indications = "糖尿病",
                    Contraindications = "低血糖症，对胰岛素过敏",
                    DosageInstructions = "皮下注射，剂量需个体化调整",
                    SideEffects = "低血糖，注射部位反应",
                    StorageConditions = "2-8℃冷藏保存",
                    ShelfLifeMonths = 30,
                    ApprovalNumber = "国药准字J20140052",
                    Remarks = "中效胰岛素，需冷藏保存"
                },
                
                new Drug
                {
                    Id = "OXYGEN001",
                    GenericName = "医用氧",
                    TradeName = "医用氧",
                    Manufacturer = "本地医用气体公司",
                    Specification = "99.5%",
                    DosageForm = "气体",
                    PackageSpec = "40L钢瓶",
                    AtcCode = "V03AN01",
                    Category = "医用气体",
                    AdministrationRoute = "吸入",
                    UnitPrice = 0.08m,
                    PriceUnit = "L",
                    IsPrescriptionOnly = false,
                    IsNarcotic = false,
                    IsPsychotropic = false,
                    IsAntibiotic = false,
                    Status = "Active",
                    Indications = "各种缺氧状态",
                    Contraindications = "无绝对禁忌症",
                    DosageInstructions = "根据病情调节流量，一般1-4L/min",
                    SideEffects = "长期高浓度吸氧可能导致氧中毒",
                    StorageConditions = "避免高温，远离火源",
                    ShelfLifeMonths = 60,
                    ApprovalNumber = "医用气体许可证",
                    Remarks = "按升计价，持续供应"
                },
                
                new Drug
                {
                    Id = "DRUG004",
                    GenericName = "红霉素眼膏",
                    TradeName = "红霉素眼膏",
                    Manufacturer = "上海通用药业股份有限公司",
                    Specification = "0.5%，2g/支",
                    DosageForm = "眼膏",
                    PackageSpec = "2g/支",
                    AtcCode = "S01AA17",
                    Category = "眼科用抗生素",
                    AdministrationRoute = "外用",
                    UnitPrice = 2.80m,
                    PriceUnit = "支",
                    IsPrescriptionOnly = false,
                    IsNarcotic = false,
                    IsPsychotropic = false,
                    IsAntibiotic = true,
                    Status = "Active",
                    Indications = "细菌性结膜炎，睑缘炎，麦粒肿",
                    Contraindications = "对红霉素过敏者",
                    DosageInstructions = "涂于眼睑内，一日2-3次",
                    SideEffects = "局部刺激症状",
                    StorageConditions = "密闭，在阴凉处保存",
                    ShelfLifeMonths = 24,
                    ApprovalNumber = "国药准字H31020675",
                    Remarks = "眼科外用抗生素"
                },
                
                new Drug
                {
                    Id = "DRUG005",
                    GenericName = "阿莫西林胶囊",
                    TradeName = "阿莫仙",
                    Manufacturer = "联邦制药（内蒙古）有限公司",
                    Specification = "0.25g/粒",
                    DosageForm = "胶囊",
                    PackageSpec = "24粒/盒",
                    AtcCode = "J01CA04",
                    Category = "β-内酰胺类抗生素",
                    AdministrationRoute = "口服",
                    UnitPrice = 0.45m,
                    PriceUnit = "粒",
                    IsPrescriptionOnly = true,
                    IsNarcotic = false,
                    IsPsychotropic = false,
                    IsAntibiotic = true,
                    Status = "Active",
                    Indications = "敏感细菌所致呼吸道、泌尿道等感染",
                    Contraindications = "青霉素过敏史",
                    DosageInstructions = "成人一次0.5g，一日3-4次，饭前服用",
                    SideEffects = "过敏反应，胃肠道反应",
                    StorageConditions = "密封保存",
                    ShelfLifeMonths = 24,
                    ApprovalNumber = "国药准字H20033564",
                    Remarks = "常用β-内酰胺类抗生素"
                },
                
                new Drug
                {
                    Id = "DRUG006",
                    GenericName = "盐酸哌替啶注射液",
                    TradeName = "盐酸哌替啶注射液",
                    Manufacturer = "西南药业股份有限公司",
                    Specification = "50mg/ml，1ml/支",
                    DosageForm = "注射液",
                    PackageSpec = "1ml×10支/盒",
                    AtcCode = "N02AB02",
                    Category = "阿片类镇痛药",
                    AdministrationRoute = "肌肉注射",
                    UnitPrice = 1.20m,
                    PriceUnit = "支",
                    IsPrescriptionOnly = true,
                    IsNarcotic = true,
                    IsPsychotropic = false,
                    IsAntibiotic = false,
                    Status = "Active",
                    Indications = "中等程度以上疼痛",
                    Contraindications = "呼吸抑制，颅内压增高",
                    DosageInstructions = "肌注一次50-100mg，必要时4小时重复给药",
                    SideEffects = "呼吸抑制，成瘾性",
                    StorageConditions = "密闭，在凉暗处保存",
                    ShelfLifeMonths = 24,
                    ApprovalNumber = "国药准字H50020007",
                    Remarks = "第二类精神药品，需严格管理"
                },
                
                // 儿科专用药品
                new Drug
                {
                    Id = "DRUG007",
                    GenericName = "小儿退热栓",
                    TradeName = "小儿退热栓",
                    Manufacturer = "华润三九医药股份有限公司",
                    Specification = "0.1g/粒",
                    DosageForm = "栓剂",
                    PackageSpec = "6粒/盒",
                    AtcCode = "N02BE01",
                    Category = "解热镇痛药",
                    AdministrationRoute = "直肠给药",
                    UnitPrice = 1.50m,
                    PriceUnit = "粒",
                    IsPrescriptionOnly = false,
                    IsNarcotic = false,
                    IsPsychotropic = false,
                    IsAntibiotic = false,
                    Status = "Active",
                    Indications = "小儿发热",
                    Contraindications = "对对乙酰氨基酚过敏",
                    DosageInstructions = "1-3岁儿童一次1粒，一日1-2次",
                    SideEffects = "个别患儿可出现皮疹",
                    StorageConditions = "密闭，在阴凉处保存",
                    ShelfLifeMonths = 24,
                    ApprovalNumber = "国药准字H20057229",
                    Remarks = "儿科专用退热药"
                },
                
                // 外科手术用药
                new Drug
                {
                    Id = "DRUG008",
                    GenericName = "头孢曲松钠",
                    TradeName = "罗氏芬",
                    Manufacturer = "上海罗氏制药有限公司",
                    Specification = "1.0g/瓶",
                    DosageForm = "粉针剂",
                    PackageSpec = "1.0g/瓶",
                    AtcCode = "J01DD04",
                    Category = "第三代头孢菌素",
                    AdministrationRoute = "静脉注射",
                    UnitPrice = 15.60m,
                    PriceUnit = "瓶",
                    IsPrescriptionOnly = true,
                    IsNarcotic = false,
                    IsPsychotropic = false,
                    IsAntibiotic = true,
                    Status = "Active",
                    Indications = "敏感菌所致严重感染",
                    Contraindications = "对头孢菌素过敏",
                    DosageInstructions = "成人一日1-2g，分1-2次静脉给药",
                    SideEffects = "过敏反应，胃肠道反应",
                    StorageConditions = "遮光，密闭保存",
                    ShelfLifeMonths = 24,
                    ApprovalNumber = "国药准字H20020066",
                    Remarks = "第三代头孢菌素，广谱抗生素"
                }
            };
            context.Drugs.AddRange(drugs);
            context.SaveChanges(); // 保存药品数据

            // --- 预置各种类型的医疗医嘱 ---
            var currentTime = DateTime.UtcNow;

            // 1. 药品医嘱 (MedicationOrder) - 各种类型的用药医嘱
            var medicationOrders = new MedicationOrder[]
            {
                // 口服药物 - 长期医嘱 (BID - 每日2次)
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
                    TimingStrategy = "SLOTS",
                    SmartSlotsMask = 2 | 32, // 早餐后(2) + 晚餐后(32) = 34
                    IntervalDays = 1
                },
                
                // 静脉滴注 - 立即执行
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
                    SmartSlotsMask = 131072, // 立即(STAT)
                    IntervalDays = 0
                },
                
                // 胰岛素 - 餐前注射 (TID - 每日3次)
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
                    SmartSlotsMask = 1 | 4 | 16, // 早餐前(1) + 午餐前(4) + 晚餐前(16) = 21
                    IntervalDays = 1
                },
                
                // 吸氧 - 持续治疗
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
                    SmartSlotsMask = 524288, // 持续(CONTINUOUS)
                    IntervalDays = 1
                },
                
                // 外用药膏 - 早晚使用
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
                    SmartSlotsMask = 512 | 64, // 上午(512) + 睡前(64) = 576
                    IntervalDays = 1
                },
                
                // 抗生素 - 每日4次 (QID)
                new MedicationOrder
                {
                    PatientId = "P005",
                    DoctorId = "D001",
                    CreateTime = currentTime.AddDays(-1),
                    PlantEndTime = currentTime.AddDays(7),
                    OrderType = "MedicationOrder",
                    Status = "Accepted",
                    IsLongTerm = true,
                    DrugId = "DRUG005",
                    Dosage = "500mg",
                    UsageRoute = "口服",
                    IsDynamicUsage = false,
                    FreqCode = "QID",
                    StartTime = currentTime.AddDays(-1),
                    TimingStrategy = "SLOTS",
                    SmartSlotsMask = 256 | 1024 | 4096 | 8192, // 清晨(256) + 中午(1024) + 傍晚(4096) + 夜晚(8192) = 13568
                    IntervalDays = 1
                },
                
                // 镇痛药 - 必要时使用 (PRN)
                new MedicationOrder
                {
                    PatientId = "P006",
                    DoctorId = "D002",
                    CreateTime = currentTime.AddDays(-1),
                    PlantEndTime = currentTime.AddDays(3),
                    OrderType = "MedicationOrder",
                    Status = "Accepted",
                    IsLongTerm = false,
                    DrugId = "DRUG006",
                    Dosage = "50mg",
                    UsageRoute = "肌肉注射",
                    IsDynamicUsage = true,
                    FreqCode = "PRN",
                    StartTime = currentTime.AddDays(-1),
                    TimingStrategy = "SPECIFIC",
                    SmartSlotsMask = 262144, // 必要时(PRN)
                    IntervalDays = 0,
                    SpecificExecutionTime = null // PRN类型不设定具体时间
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
    }
}