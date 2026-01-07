using CareFlow.Core.Enums;
using CareFlow.Core.Models.Medical;
using CareFlow.Core.Models.Nursing;
using CareFlow.Core.Models.Organization;
using CareFlow.Core.Models.Space;
using CareFlow.Core.Utils;
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
            // 删除现有数据库并重新创建（由于模型结构改变较大，这是最彻底的清理方式）
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            // 1. 检查是否已有数据（重新创建后应该是空的，这里做双重保险）
            if (context.Departments.Any())
            {
                return;   // 数据库已播种，直接返回
            }

            // ==========================================
            // 第一部分：基础数据（科室、时段、员工、床位）
            // ==========================================

            // --- 预置科室数据 (Department) ---
            var departments = new Department[]
            {
                new Department { Id = "IM", DeptName = "内科", Location = "住院部A栋3楼" },
                new Department { Id = "SUR", DeptName = "外科", Location = "住院部A栋4楼" },
                new Department { Id = "PED", DeptName = "儿科", Location = "住院部B栋2楼" },
                new Department { Id = "ADM", DeptName = "行政", Location = "行政楼101室" },
                new Department { Id = "CHK", DeptName = "检查站", Location = "检查站1楼" }
            };
            context.Departments.AddRange(departments);
            context.SaveChanges(); // 保存科室

            // --- 预置时间槽位数据 (HospitalTimeSlot) ---
            // ⚠️ 重要：时间使用 UTC 存储（北京时间 -8小时）
            // 例如：北京时间 07:00 = UTC 前一天 23:00
            var timeSlots = new HospitalTimeSlot[]
            {
                new HospitalTimeSlot { Id = 1, SlotCode = "PRE_BREAKFAST", SlotName = "早餐前", DefaultTime = new TimeSpan(23, 0, 0), OffsetMinutes = 15 },   // 北京 07:00 → UTC 前一天 23:00
                new HospitalTimeSlot { Id = 2, SlotCode = "POST_BREAKFAST", SlotName = "早餐后", DefaultTime = new TimeSpan(0, 30, 0), OffsetMinutes = 30 },    // 北京 08:30 → UTC 00:30
                new HospitalTimeSlot { Id = 4, SlotCode = "PRE_LUNCH", SlotName = "午餐前", DefaultTime = new TimeSpan(3, 30, 0), OffsetMinutes = 15 },        // 北京 11:30 → UTC 03:30
                new HospitalTimeSlot { Id = 8, SlotCode = "POST_LUNCH", SlotName = "午餐后", DefaultTime = new TimeSpan(5, 0, 0), OffsetMinutes = 30 },         // 北京 13:00 → UTC 05:00
                new HospitalTimeSlot { Id = 16, SlotCode = "PRE_DINNER", SlotName = "晚餐前", DefaultTime = new TimeSpan(9, 30, 0), OffsetMinutes = 15 },       // 北京 17:30 → UTC 09:30
                new HospitalTimeSlot { Id = 32, SlotCode = "POST_DINNER", SlotName = "晚餐后", DefaultTime = new TimeSpan(11, 0, 0), OffsetMinutes = 30 },      // 北京 19:00 → UTC 11:00
                new HospitalTimeSlot { Id = 64, SlotCode = "BEDTIME", SlotName = "睡前", DefaultTime = new TimeSpan(13, 0, 0), OffsetMinutes = 30 }             // 北京 21:00 → UTC 13:00
            };
            context.HospitalTimeSlots.AddRange(timeSlots);
            context.SaveChanges(); // 保存时间槽位

            // --- 预置员工数据 (Staff/Doctor/Nurse) ---
            string defaultHashedPassword = HashPassword("123456");

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
                    Title = DoctorTitle.Chief.ToString(),
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
                    DeptCode = "SUR",
                    Title = DoctorTitle.Chief.ToString(),
                    PrescriptionAuthLevel = "High"
                },
                new Doctor
                {
                    Id = "D003",
                    EmployeeNumber = "doc003",
                    PasswordHash = defaultHashedPassword,
                    Name = "王医生",
                    IdCard = "110100200001010014",
                    Phone = "13912340014",
                    RoleType = "Doctor",
                    IsActive = true,
                    DeptCode = "PED",
                    Title = DoctorTitle.Attending.ToString(),
                    PrescriptionAuthLevel = "Medium"
                },
                new Doctor
                {
                    Id = "D004",
                    EmployeeNumber = "doc004",
                    PasswordHash = defaultHashedPassword,
                    Name = "陈医生",
                    IdCard = "110100200001010015",
                    Phone = "13912340015",
                    RoleType = "Doctor",
                    IsActive = true,
                    DeptCode = "CHK",
                    Title = DoctorTitle.Attending.ToString(),
                    PrescriptionAuthLevel = "Medium"
                }
            };
            
            // 护士数据 - 扩展更多护士以支持排班表
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
                    NurseRank = NurseRank.HeadNurse.ToString()
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
                    NurseRank = NurseRank.RegularNurse.ToString()
                },
                new Nurse
                {
                    Id = "N003",
                    EmployeeNumber = "nurse003",
                    PasswordHash = defaultHashedPassword,
                    Name = "李护士",
                    IdCard = "110100200001010006",
                    Phone = "13912340006",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "IM",
                    NurseRank = NurseRank.RegularNurse.ToString()
                },
                new Nurse
                {
                    Id = "N004",
                    EmployeeNumber = "nurse004",
                    PasswordHash = defaultHashedPassword,
                    Name = "张护士",
                    IdCard = "110100200001010007",
                    Phone = "13912340007",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "IM",
                    NurseRank = NurseRank.TeamLeader.ToString()
                },
                new Nurse
                {
                    Id = "N005",
                    EmployeeNumber = "nurse005",
                    PasswordHash = defaultHashedPassword,
                    Name = "陈护士",
                    IdCard = "110100200001010008",
                    Phone = "13912340008",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "PED",
                    NurseRank = NurseRank.RegularNurse.ToString()
                },
                new Nurse
                {
                    Id = "N006",
                    EmployeeNumber = "nurse006",
                    PasswordHash = defaultHashedPassword,
                    Name = "刘护士",
                    IdCard = "110100200001010009",
                    Phone = "13912340009",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "PED",
                    NurseRank = NurseRank.TeamLeader.ToString()
                },
                new Nurse
                {
                    Id = "N007",
                    EmployeeNumber = "nurse007",
                    PasswordHash = defaultHashedPassword,
                    Name = "吴护士",
                    IdCard = "110100200001010010",
                    Phone = "13912340010",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "IM",
                    NurseRank = NurseRank.RegularNurse.ToString()
                },
                new Nurse
                {
                    Id = "N008",
                    EmployeeNumber = "nurse008",
                    PasswordHash = defaultHashedPassword,
                    Name = "周护士",
                    IdCard = "110100200001010011",
                    Phone = "13912340011",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "SUR",
                    NurseRank = NurseRank.RegularNurse.ToString()
                },
                new Nurse
                {
                    Id = "N009",
                    EmployeeNumber = "nurse009",
                    PasswordHash = defaultHashedPassword,
                    Name = "孙护士",
                    IdCard = "110100200001010012",
                    Phone = "13912340012",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "CHK",
                    NurseRank = NurseRank.RegularNurse.ToString()
                },
                new Nurse
                {
                    Id = "N010",
                    EmployeeNumber = "nurse010",
                    PasswordHash = defaultHashedPassword,
                    Name = "郑护士",
                    IdCard = "110100200001010013",
                    Phone = "13912340013",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "CHK",
                    NurseRank = NurseRank.TeamLeader.ToString()
                }
            };

            // 行政人员数据
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
            
            context.Doctors.AddRange(doctors);
            context.Nurses.AddRange(nurses);
            context.Staffs.AddRange(adminStaffs);
            context.SaveChanges();

            // --- 预置病房和床位数据 ---
            var wards = new Ward[]
            {
                new Ward { Id = "IM-W01", DepartmentId = "IM" },
                new Ward { Id = "IM-W02", DepartmentId = "IM" },
                new Ward { Id = "SUR-W01", DepartmentId = "SUR" },
                new Ward { Id = "PED-W01", DepartmentId = "PED" }
            };
            context.Wards.AddRange(wards);
            context.SaveChanges();

            var beds = new Bed[]
            {
                new Bed { Id = "IM-W01-001", WardId = "IM-W01", Status = "占用" },
                new Bed { Id = "IM-W01-002", WardId = "IM-W01", Status = "占用" },
                new Bed { Id = "IM-W01-003", WardId = "IM-W01", Status = "空闲" },
                new Bed { Id = "IM-W02-001", WardId = "IM-W02", Status = "占用" },
                new Bed { Id = "SUR-W01-001", WardId = "SUR-W01", Status = "占用" },
                new Bed { Id = "SUR-W01-002", WardId = "SUR-W01", Status = "占用" },
                new Bed { Id = "PED-W01-001", WardId = "PED-W01", Status = "占用" }
            };
            context.Beds.AddRange(beds);
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

            // ==========================================
            // 第二部分：业务数据（患者、医嘱）- 原来被隔绝的部分
            // ==========================================

            // --- 预置患者数据 ---
            var patients = new Patient[]
            {
                new Patient
                {
                    Id = "P001", Name = "张三", Gender = "男", IdCard = "110100199001010001",
                    DateOfBirth = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    Age = 34, Height = 175.0f, Weight = 70.5f, Status = PatientStatus.Hospitalized, PhoneNumber = "13800138001",
                    NursingGrade = NursingGrade.Grade2, BedId = "IM-W01-001", AttendingDoctorId = "D001",
                    OutpatientDiagnosis = "高血压2级，糖尿病",
                    ScheduledAdmissionTime = UtcDate(2024, 12, 1, 8, 0),
                    ActualAdmissionTime = UtcDate(2024, 12, 1, 9, 30)
                },
                new Patient
                {
                    Id = "P002", Name = "李四", Gender = "女", IdCard = "110100198505050002",
                    DateOfBirth = new DateTime(1985, 5, 5, 0, 0, 0, DateTimeKind.Utc),
                    Age = 39, Height = 162.0f, Weight = 58.0f, Status = PatientStatus.PendingDischarge, PhoneNumber = "13800138002",
                    NursingGrade = NursingGrade.Special, BedId = "IM-W01-002", AttendingDoctorId = "D001",
                    OutpatientDiagnosis = "急性心肌梗死",
                    ScheduledAdmissionTime = UtcDate(2024, 12, 5, 10, 0),
                    ActualAdmissionTime = UtcDate(2024, 12, 5, 10, 15)
                },
                new Patient
                {
                    Id = "P003", Name = "王五", Gender = "女", IdCard = "110100197803030003",
                    DateOfBirth = new DateTime(1978, 3, 3, 0, 0, 0, DateTimeKind.Utc),
                    Age = 44, Height = 168.0f, Weight = 64.2f, Status = PatientStatus.Hospitalized, PhoneNumber = "13800138003",
                    NursingGrade = NursingGrade.Grade3, BedId = "SUR-W01-001", AttendingDoctorId = "D002",
                    OutpatientDiagnosis = "阑尾炎",
                    ScheduledAdmissionTime = UtcDate(2024, 12, 10, 14, 0),
                    ActualAdmissionTime = UtcDate(2024, 12, 10, 14, 30)
                },
                new Patient
                {
                    Id = "P004", Name = "赵六", Gender = "女", IdCard = "110100199212120004",
                    DateOfBirth = new DateTime(1992, 12, 12, 0, 0, 0, DateTimeKind.Utc),
                    Age = 32, Height = 165.0f, Weight = 62.8f, Status = PatientStatus.Hospitalized, PhoneNumber = "13800138004",
                    NursingGrade = NursingGrade.Grade2, BedId = "SUR-W01-002", AttendingDoctorId = "D002",
                    OutpatientDiagnosis = "胆囊结石",
                    ScheduledAdmissionTime = UtcDate(2024, 12, 12, 9, 0),
                    ActualAdmissionTime = UtcDate(2024, 12, 12, 9, 45)
                },
                new Patient
                {
                    Id = "P005", Name = "钱七", Gender = "男", IdCard = "110100201501010005",
                    DateOfBirth = new DateTime(2015, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    Age = 9, Height = 135.0f, Weight = 28.5f, Status = PatientStatus.Hospitalized, PhoneNumber = "13800138005",
                    NursingGrade = NursingGrade.Grade1, BedId = "PED-W01-001", AttendingDoctorId = "D001",
                    OutpatientDiagnosis = "支气管肺炎",
                    ScheduledAdmissionTime = UtcDate(2024, 12, 15, 10, 0),
                    ActualAdmissionTime = UtcDate(2024, 12, 15, 10, 20)
                },
                new Patient
                {
                    Id = "P006", Name = "孙八", Gender = "男", IdCard = "110100196802020006",
                    DateOfBirth = new DateTime(1968, 2, 2, 0, 0, 0, DateTimeKind.Utc),
                    Age = 56, Height = 172.0f, Weight = 80.1f, Status = PatientStatus.Hospitalized, PhoneNumber = "13800138006",
                    NursingGrade = NursingGrade.Grade3, BedId = "IM-W02-001", AttendingDoctorId = "D002",
                    OutpatientDiagnosis = "慢性阻塞性肺病",
                    ScheduledAdmissionTime = UtcDate(2024, 12, 18, 8, 30),
                    ActualAdmissionTime = UtcDate(2024, 12, 18, 9, 0)
                },
                // ==================== 待入院患者（用于测试入院功能） ====================
                new Patient
                {
                    Id = "P007", Name = "周九", Gender = "男", IdCard = "110100199203150007",
                    DateOfBirth = new DateTime(1992, 3, 15, 0, 0, 0, DateTimeKind.Utc),
                    Age = 32, Height = 178.0f, Weight = 75.0f, Status = PatientStatus.PendingAdmission, PhoneNumber = "13800138007",
                    NursingGrade = NursingGrade.Grade2, BedId = null, AttendingDoctorId = "D001",  // BedId 改为 null
                    OutpatientDiagnosis = "慢性胃炎，胃溃疡",
                    ScheduledAdmissionTime = UtcDate(2024, 12, 20, 9, 0),
                    ActualAdmissionTime = null
                },
                new Patient
                {
                    Id = "P008", Name = "吴十", Gender = "女", IdCard = "110100198811200008",
                    DateOfBirth = new DateTime(1988, 11, 20, 0, 0, 0, DateTimeKind.Utc),
                    Age = 36, Height = 165.0f, Weight = 60.0f, Status = PatientStatus.PendingAdmission, PhoneNumber = "13800138008",
                    NursingGrade = NursingGrade.Grade1, BedId = null, AttendingDoctorId = "D001",  // BedId 改为 null
                    OutpatientDiagnosis = "心律失常，房颤",
                    ScheduledAdmissionTime = UtcDate(2024, 12, 21, 10, 0),
                    ActualAdmissionTime = null
                },
                new Patient
                {
                    Id = "P009", Name = "郑十一", Gender = "男", IdCard = "110100197504250009",
                    DateOfBirth = new DateTime(1975, 4, 25, 0, 0, 0, DateTimeKind.Utc),
                    Age = 49, Height = 170.0f, Weight = 68.5f, Status = PatientStatus.PendingAdmission, PhoneNumber = "13800138009",
                    NursingGrade = NursingGrade.Grade2, BedId = null, AttendingDoctorId = "D002",  // BedId 改为 null
                    OutpatientDiagnosis = "腰椎间盘突出症",
                    ScheduledAdmissionTime = UtcDate(2024, 12, 22, 14, 0),
                    ActualAdmissionTime = null
                }
            };
            context.Patients.AddRange(patients);
            context.SaveChanges();

          
            // context.MedicationOrders.AddRange(medicationOrders2);
            // --- 护士排班相关数据 ---
            // 班次类型数据
            // ⚠️ 重要：时间使用 UTC 存储（北京时间 -8小时）
            // 班次设计：白班08:00-16:00，晚班16:00-21:00，夜班21:00-08:00(次日) [北京时间]
            var shiftTypes = new CareFlow.Core.Models.Nursing.ShiftType[]
            {
                new CareFlow.Core.Models.Nursing.ShiftType
                {
                    Id = "DAY",
                    ShiftName = "白班",
                    StartTime = new TimeSpan(0, 0, 0),   // UTC 00:00 (北京时间 08:00)
                    EndTime = new TimeSpan(8, 0, 0)      // UTC 08:00 (北京时间 16:00)
                },
                new CareFlow.Core.Models.Nursing.ShiftType
                {
                    Id = "EVENING",
                    ShiftName = "晚班",
                    StartTime = new TimeSpan(8, 0, 0),   // UTC 08:00 (北京时间 16:00)
                    EndTime = new TimeSpan(13, 0, 0)     // UTC 13:00 (北京时间 21:00)
                },
                new CareFlow.Core.Models.Nursing.ShiftType
                {
                    Id = "NIGHT",
                    ShiftName = "夜班",
                    StartTime = new TimeSpan(13, 0, 0),  // UTC 13:00 (北京时间 21:00)
                    EndTime = new TimeSpan(0, 0, 0)      // UTC 00:00 (次日，北京时间 08:00)
                }
            };
            context.ShiftTypes.AddRange(shiftTypes);
            context.SaveChanges(); // 保存班次类型
            
            // 护士排班表数据 - 每个病区每个班次均分配护士
            // 注意：使用 DateTime.UtcNow 确保日期为 UTC 日期，与查询时的 UTC 时间一致
            var todayUtc = DateTime.UtcNow;
            var nurseRosters = new NurseRoster[]
            {
                // 内科一病区 IM-W01
                new NurseRoster { StaffId = "N004", WardId = "IM-W01", ShiftId = "DAY", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N003", WardId = "IM-W01", ShiftId = "EVENING", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N007", WardId = "IM-W01", ShiftId = "NIGHT", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },

                // 内科二病区 IM-W02
                new NurseRoster { StaffId = "N001", WardId = "IM-W02", ShiftId = "DAY", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N004", WardId = "IM-W02", ShiftId = "EVENING", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N003", WardId = "IM-W02", ShiftId = "NIGHT", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },

                // 外科病区 SUR-W01
                new NurseRoster { StaffId = "N001", WardId = "SUR-W01", ShiftId = "NIGHT", WorkDate = DateOnly.FromDateTime(todayUtc.AddDays(-1)), Status = "Scheduled" },
                new NurseRoster { StaffId = "N002", WardId = "SUR-W01", ShiftId = "DAY", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N008", WardId = "SUR-W01", ShiftId = "EVENING", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N001", WardId = "SUR-W01", ShiftId = "NIGHT", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },

                // 儿科病区 PED-W01
                new NurseRoster { StaffId = "N006", WardId = "PED-W01", ShiftId = "DAY", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N005", WardId = "PED-W01", ShiftId = "EVENING", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N010", WardId = "PED-W01", ShiftId = "NIGHT", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },

                // 备用/明日部分排班（保持原来的简单轮换示例）
                new NurseRoster { StaffId = "N002", WardId = "SUR-W01", ShiftId = "DAY", WorkDate = DateOnly.FromDateTime(todayUtc.AddDays(1)), Status = "Scheduled" },
                new NurseRoster { StaffId = "N001", WardId = "SUR-W01", ShiftId = "EVENING", WorkDate = DateOnly.FromDateTime(todayUtc.AddDays(1)), Status = "Scheduled" },
                new NurseRoster { StaffId = "N003", WardId = "IM-W02", ShiftId = "DAY", WorkDate = DateOnly.FromDateTime(todayUtc.AddDays(1)), Status = "Scheduled" },
                new NurseRoster { StaffId = "N005", WardId = "PED-W01", ShiftId = "DAY", WorkDate = DateOnly.FromDateTime(todayUtc.AddDays(1)), Status = "Scheduled" }
            };
            context.NurseRosters.AddRange(nurseRosters);
            context.SaveChanges(); // 保存排班数据
            
        }
    }
}