using CareFlow.Core.Enums;
using CareFlow.Core.Models.Medical;
using CareFlow.Core.Models.Nursing;
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
                    Age = 34, Weight = 70.5f, Status = "Active", PhoneNumber = "13800138001",
                    NursingGrade = NursingGrade.Grade2, BedId = "IM-W01-001", AttendingDoctorId = "D001"
                },
                new Patient
                {
                    Id = "P002", Name = "李四", Gender = "女", IdCard = "110100198505050002",
                    DateOfBirth = new DateTime(1985, 5, 5, 0, 0, 0, DateTimeKind.Utc),
                    Age = 39, Weight = 58.0f, Status = "Active", PhoneNumber = "13800138002",
                    NursingGrade = NursingGrade.Special, BedId = "IM-W01-002", AttendingDoctorId = "D001"
                },
                new Patient
                {
                    Id = "P003", Name = "王五", Gender = "男", IdCard = "110100197803030003",
                    DateOfBirth = new DateTime(1978, 3, 3, 0, 0, 0, DateTimeKind.Utc),
                    Age = 46, Weight = 75.2f, Status = "Active", PhoneNumber = "13800138003",
                    NursingGrade = NursingGrade.Grade3, BedId = "SUR-W01-001", AttendingDoctorId = "D002"
                },
                new Patient
                {
                    Id = "P004", Name = "赵六", Gender = "女", IdCard = "110100199212120004",
                    DateOfBirth = new DateTime(1992, 12, 12, 0, 0, 0, DateTimeKind.Utc),
                    Age = 32, Weight = 62.8f, Status = "Active", PhoneNumber = "13800138004",
                    NursingGrade = NursingGrade.Grade2, BedId = "SUR-W01-002", AttendingDoctorId = "D002"
                },
                new Patient
                {
                    Id = "P005", Name = "钱七", Gender = "男", IdCard = "110100201501010005",
                    DateOfBirth = new DateTime(2015, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    Age = 9, Weight = 28.5f, Status = "Active", PhoneNumber = "13800138005",
                    NursingGrade = NursingGrade.Grade1, BedId = "PED-W01-001", AttendingDoctorId = "D001"
                },
                new Patient
                {
                    Id = "P006", Name = "孙八", Gender = "男", IdCard = "110100196802020006",
                    DateOfBirth = new DateTime(1968, 2, 2, 0, 0, 0, DateTimeKind.Utc),
                    Age = 56, Weight = 80.1f, Status = "Active", PhoneNumber = "13800138006",
                    NursingGrade = NursingGrade.Grade3, BedId = "IM-W02-001", AttendingDoctorId = "D002"
                }
            };
            context.Patients.AddRange(patients);
            context.SaveChanges();

            // --- 预置各种类型的医疗医嘱 ---
            var currentTime = DateTime.UtcNow;

            // 1. 药品医嘱 (MedicationOrder)
            // 1. 药品医嘱 (MedicationOrder)
            // 提示：EF Core 会自动将 Items 列表中的子项插入 MedicationOrderItems 表，并自动关联 ID
            var medicationOrders = new List<MedicationOrder>
            {
                // P001: 阿司匹林 - 长期口服 (SLOTS策略)
                new MedicationOrder
                {
                    PatientId = "P001", DoctorId = "D001", NurseId = "N001",
                    CreateTime = currentTime.AddDays(-2), PlantEndTime = currentTime.AddDays(2),
                    OrderType = "MedicationOrder", Status = OrderStatus.Accepted, IsLongTerm = true,
                    UsageRoute = UsageRoute.PO,
                    IsDynamicUsage = false,
                    IntervalHours = null, // SLOTS策略不需要
                    StartTime = currentTime.AddDays(-2),
                    TimingStrategy = "SLOTS",
                    SmartSlotsMask = 2 | 32, // 早餐后 + 晚餐后
                    IntervalDays = 1,
                    Items = new List<MedicationOrderItem>
                    {
                        new MedicationOrderItem { DrugId = "DRUG001", Dosage = "100mg", Note = "餐后服用" }
                    }
                },

                // P001: 生理盐水 - 临时静脉滴注 (IMMEDIATE策略)
                new MedicationOrder
                {
                    PatientId = "P001", DoctorId = "D001", NurseId = "N002",
                    CreateTime = currentTime.AddHours(-1), PlantEndTime = currentTime.AddHours(1),
                    OrderType = "MedicationOrder", Status = OrderStatus.InProgress, IsLongTerm = false,
                    UsageRoute = UsageRoute.IVGTT,
                    IsDynamicUsage = false,
                    IntervalHours = null, // IMMEDIATE策略不需要
                    StartTime = currentTime.AddHours(-1),
                    TimingStrategy = "IMMEDIATE",
                    SmartSlotsMask = 0, // IMMEDIATE策略不依赖时段
                    IntervalDays = 0,
                    Items = new List<MedicationOrderItem>
                    {
                        new MedicationOrderItem { DrugId = "DRUG002", Dosage = "250ml", Note = "缓慢滴注" }
                    }
                },

                // P002: 胰岛素 - 长期皮下注射 (SLOTS策略)
                new MedicationOrder
                {
                    PatientId = "P002", DoctorId = "D001", NurseId = "N001",
                    CreateTime = currentTime.AddDays(-1), PlantEndTime = currentTime.AddDays(2),
                    OrderType = "MedicationOrder", Status = OrderStatus.Accepted, IsLongTerm = true,
                    UsageRoute = UsageRoute.SC,
                    IsDynamicUsage = false,
                    IntervalHours = null, // SLOTS策略不需要
                    StartTime = currentTime.AddDays(-1),
                    TimingStrategy = "SLOTS",
                    SmartSlotsMask = 1 | 4 | 16, // 早中晚餐前
                    IntervalDays = 1,
                    Items = new List<MedicationOrderItem>
                    {
                        new MedicationOrderItem { DrugId = "DRUG003", Dosage = "8单位", Note = "餐前15分钟" }
                    }
                },

                // P003: 头孢曲松钠 - 静脉注射抗感染治疗 (CYCLIC策略，每8小时一次)
                new MedicationOrder
                {
                    PatientId = "P003", DoctorId = "D002", NurseId = "N002",
                    CreateTime = currentTime.AddDays(-1), PlantEndTime = currentTime.AddDays(1),
                    OrderType = "MedicationOrder", Status = OrderStatus.Accepted, IsLongTerm = true,
                    UsageRoute = UsageRoute.IVP, // 静脉推注
                    IsDynamicUsage = false,
                    IntervalHours = 8m, // 每8小时给药一次
                    StartTime = currentTime.AddDays(-1).Date.AddHours(8), // 从早上8点开始
                    TimingStrategy = "CYCLIC",
                    SmartSlotsMask = 0, // CYCLIC策略不依赖时段
                    IntervalDays = 1,
                    Items = new List<MedicationOrderItem>
                    {
                        new MedicationOrderItem { DrugId = "DRUG008", Dosage = "1.0g", Note = "溶于100ml生理盐水缓慢静推" }
                    }
                },

                // P004: 红霉素眼膏 - 外用 (SLOTS策略)
                new MedicationOrder
                {
                    PatientId = "P004", DoctorId = "D002",
                    CreateTime = currentTime.AddDays(-1), PlantEndTime = currentTime.AddDays(2),
                    OrderType = "MedicationOrder", Status = OrderStatus.Accepted, IsLongTerm = true,
                    UsageRoute = UsageRoute.Topical,
                    IsDynamicUsage = false,
                    IntervalHours = null, // SLOTS策略不需要
                    StartTime = currentTime.AddDays(-1),
                    TimingStrategy = "SLOTS",
                    SmartSlotsMask = 2 | 64, // 早餐后 + 睡前
                    IntervalDays = 1,
                    Items = new List<MedicationOrderItem>
                    {
                        new MedicationOrderItem { DrugId = "DRUG004", Dosage = "适量", Note = "薄层涂抹" }
                    }
                },

                // *** 重点演示：P005: 混合静脉滴注 (多药混合：盐水 + 头孢) ***
                // SLOTS策略
                new MedicationOrder
                {
                    PatientId = "P005", DoctorId = "D001",
                    CreateTime = currentTime.AddDays(-1), PlantEndTime = currentTime.AddDays(1),
                    OrderType = "MedicationOrder", Status = OrderStatus.Accepted, IsLongTerm = true,
                    UsageRoute = UsageRoute.IVGTT,
                    IsDynamicUsage = false,
                    IntervalHours = null, // SLOTS策略不需要
                    StartTime = currentTime.AddDays(-1),
                    TimingStrategy = "SLOTS",
                    SmartSlotsMask = 2, // 早餐后
                    IntervalDays = 1,
                    Items = new List<MedicationOrderItem>
                    {
                        // 第一味药：溶媒（生理盐水）
                        new MedicationOrderItem { DrugId = "DRUG002", Dosage = "100ml", Note = "溶媒" },
                        // 第二味药：主药（头孢曲松钠）
                        new MedicationOrderItem { DrugId = "DRUG008", Dosage = "2.0g", Note = "皮试阴性" }
                    }
                },

                // P006: 杜冷丁 - 肌肉注射 (SPECIFIC策略, 指定时间给药)
                new MedicationOrder
                {
                    PatientId = "P006", DoctorId = "D002",
                    CreateTime = currentTime.AddDays(-1), PlantEndTime = currentTime.AddDays(3),
                    OrderType = "MedicationOrder", Status = OrderStatus.Accepted, IsLongTerm = false,
                    UsageRoute = UsageRoute.IM,
                    IsDynamicUsage = true,
                    IntervalHours = null, // SPECIFIC策略不需要
                    StartTime = currentTime.AddHours(2), // SPECIFIC策略：唯一执行时间
                    TimingStrategy = "SPECIFIC",
                    SmartSlotsMask = 0, // SPECIFIC策略不依赖时段
                    IntervalDays = 0,
                    Items = new List<MedicationOrderItem>
                    {
                        new MedicationOrderItem { DrugId = "DRUG006", Dosage = "50mg", Note = "剧烈疼痛时使用" }
                    }
                }
            };

            // 注意：这里不需要再分别保存 Items，EF Core 会一次性保存整个对象图
            context.MedicationOrders.AddRange(medicationOrders);
            context.SaveChanges();

            // 2. 操作医嘱 (OperationOrder)
            var operationOrders = new OperationOrder[]
            {
                new OperationOrder
                {
                    PatientId = "P001", DoctorId = "D001", NurseId = "N001",
                    CreateTime = currentTime.AddDays(-1), PlantEndTime = currentTime.AddDays(3),
                    OrderType = "OperationOrder", Status = OrderStatus.Accepted, IsLongTerm = true,
                    OpId = "OP001", Normal = true, FrequencyType = "每天", FrequencyValue = "3次"
                },
                new OperationOrder
                {
                    PatientId = "P003", DoctorId = "D002", NurseId = "N002",
                    CreateTime = currentTime.AddDays(-1), PlantEndTime = currentTime.AddDays(2),
                    OrderType = "OperationOrder", Status = OrderStatus.Accepted, IsLongTerm = true,
                    OpId = "OP002", Normal = true, FrequencyType = "持续", FrequencyValue = "24小时"
                },
                new OperationOrder
                {
                    PatientId = "P004", DoctorId = "D002", NurseId = "N001",
                    CreateTime = currentTime.AddHours(-2), PlantEndTime = currentTime.AddHours(1),
                    OrderType = "OperationOrder", Status = OrderStatus.InProgress, IsLongTerm = false,
                    OpId = "OP003", Normal = true, FrequencyType = "一次性", FrequencyValue = "1次"
                }
            };
            context.OperationOrders.AddRange(operationOrders);
            
            // 3. 检查医嘱 (InspectionOrder)
            var inspectionOrders = new InspectionOrder[]
            {
                new InspectionOrder
                {
                    PatientId = "P002", DoctorId = "D001",
                    CreateTime = currentTime.AddHours(-4), PlantEndTime = currentTime.AddHours(8),
                    OrderType = "InspectionOrder", Status = OrderStatus.PendingReceive, IsLongTerm = false,
                    ItemCode = "CT001", RisLisId = "RIS202412060001", Location = "影像科",
                    AppointmentTime = currentTime.AddHours(4), AppointmentPlace = "CT室1",
                    Precautions = "检查前4小时禁食", ReportId = "",
                    InspectionStatus = InspectionOrderStatus.Pending,
                    Source = InspectionSource.RIS
                },
                new InspectionOrder
                {
                    PatientId = "P005", DoctorId = "D001",
                    CreateTime = currentTime.AddHours(-1), PlantEndTime = currentTime.AddHours(6),
                    OrderType = "InspectionOrder", Status = OrderStatus.Accepted, IsLongTerm = false,
                    ItemCode = "LAB001", RisLisId = "LIS202412060001", Location = "检验科",
                    AppointmentTime = currentTime.AddHours(2), AppointmentPlace = "采血室",
                    Precautions = "空腹采血", ReportId = "",
                    InspectionStatus = InspectionOrderStatus.Pending,
                    Source = InspectionSource.LIS
                },
                new InspectionOrder
                {
                    PatientId = "P006", DoctorId = "D002",
                    CreateTime = currentTime.AddDays(-1), PlantEndTime = currentTime.AddHours(-2),
                    EndTime = currentTime.AddHours(-2),
                    OrderType = "InspectionOrder", Status = OrderStatus.Completed, IsLongTerm = false,
                    ItemCode = "ECG001", RisLisId = "RIS202412050001", Location = "心电图室",
                    AppointmentTime = currentTime.AddHours(-4), AppointmentPlace = "心电图室1",
                    Precautions = "检查时保持安静", CheckStartTime = currentTime.AddHours(-4),
                    CheckEndTime = currentTime.AddHours(-3.5), ReportPendingTime = currentTime.AddHours(-3),
                    ReportTime = currentTime.AddHours(-2), ReportId = "ECG202412050001",
                    InspectionStatus = InspectionOrderStatus.ReportCompleted,
                    Source = InspectionSource.RIS
                }
            };
            context.InspectionOrders.AddRange(inspectionOrders);
            
            // 4. 手术医嘱 (SurgicalOrder)
            var surgicalOrders = new SurgicalOrder[]
            {
                new SurgicalOrder
                {
                    PatientId = "P003", DoctorId = "D002",
                    CreateTime = currentTime.AddHours(-6), PlantEndTime = currentTime.AddHours(8),
                    OrderType = "SurgicalOrder", Status = OrderStatus.Accepted, IsLongTerm = false,
                    SurgeryName = "腹腔镜阑尾切除术", ScheduleTime = currentTime.AddHours(6),
                    AnesthesiaType = "全身麻醉", IncisionSite = "脐部及右下腹", SurgeonId = "D002",
                    RequiredTalk = "[\"术前禁食水宣教\", \"术前饰品摘取\", \"更换病号服\"]",
                    RequiredOperation = "[\"手术区域备皮\", \"留置导尿管\", \"建立静脉通路\"]",
                    PrepProgress = 0.6f, PrepStatus = "术前准备中",
                    Items = new List<MedicationOrderItem>
                    {
                        new MedicationOrderItem { DrugId = "DRUG001", Dosage = "2支", Note = "术中用药" },
                        new MedicationOrderItem { DrugId = "DRUG002", Dosage = "5袋", Note = "术中补液" },
                        new MedicationOrderItem { DrugId = "DRUG003", Dosage = "1支", Note = "术后镇痛" }
                    }
                },
                new SurgicalOrder
                {
                    PatientId = "P004", DoctorId = "D002",
                    CreateTime = currentTime.AddDays(-1), PlantEndTime = currentTime.AddDays(1),
                    OrderType = "SurgicalOrder", Status = OrderStatus.PendingReceive, IsLongTerm = false,
                    SurgeryName = "腹腔镜胆囊切除术", ScheduleTime = currentTime.AddDays(1).AddHours(2),
                    AnesthesiaType = "全身麻醉", IncisionSite = "脐部及上腹部", SurgeonId = "D002",
                    RequiredTalk = "[\"术前禁食水宣教\"]",
                    RequiredOperation = "[\"交叉配血\",\"手术区域备皮\", \"建立静脉通路\"]",
                    PrepProgress = 0.2f, PrepStatus = "等待术前评估",
                    Items = new List<MedicationOrderItem>
                    {
                        new MedicationOrderItem { DrugId = "DRUG005", Dosage = "3粒", Note = "术前预防感染" },
                        new MedicationOrderItem { DrugId = "DRUG002", Dosage = "10袋", Note = "术中补液" }
                    }
                },
                new SurgicalOrder
                {
                    PatientId = "P006", DoctorId = "D002",
                    CreateTime = currentTime.AddHours(-2), PlantEndTime = currentTime.AddHours(4),
                    OrderType = "SurgicalOrder", Status = OrderStatus.Accepted, IsLongTerm = false,
                    SurgeryName = "左股骨干骨折切开复位内固定术", ScheduleTime = currentTime.AddHours(2),
                    AnesthesiaType = "腰硬联合麻醉", IncisionSite = "左大腿外侧", SurgeonId = "D002",
                    RequiredTalk = "[\"术前禁食水宣教\", \"术前体位指导\"]",
                    RequiredOperation = "[\"手术区域备皮\", \"留置导尿管\", \"术前抗生素皮试\"]",
                    PrepProgress = 0.8f, PrepStatus = "急诊准备中",
                    Items = new List<MedicationOrderItem>
                    {
                        new MedicationOrderItem { DrugId = "DRUG008", Dosage = "2瓶", Note = "抗感染" },
                        new MedicationOrderItem { DrugId = "DRUG002", Dosage = "500ml", Note = "术中补液" }
                    }
                }
            };
            context.SurgicalOrders.AddRange(surgicalOrders);

            var medicationOrders2 = new List<MedicationOrder>{
                new MedicationOrder
                {
                    PatientId = "P003", DoctorId = "D002", NurseId = "N002",
                    CreateTime = currentTime, PlantEndTime = currentTime.AddDays(1),
                    OrderType = "MedicationOrder", Status = OrderStatus.PendingReceive, IsLongTerm = true,  // 修改为Accepted，因为已生成ExecutionTask
                    UsageRoute = UsageRoute.IVP, // 静脉推注
                    IsDynamicUsage = false,
                    IntervalHours = 8m, // 每8小时给药一次
                    StartTime = currentTime.Date.AddHours(8), // 从早上8点开始
                    TimingStrategy = "CYCLIC",
                    SmartSlotsMask = 0, // CYCLIC策略不依赖时段
                    IntervalDays = 1,
                    Items = new List<MedicationOrderItem>
                    {
                        new MedicationOrderItem { DrugId = "DRUG008", Dosage = "1.0g", Note = "溶于100ml生理盐水缓慢静推" }
                    }
                }
            };

            context.MedicationOrders.AddRange(medicationOrders2);
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
            
            // 护士排班表数据 - 安排不同科室护士的排班
            var nurseRosters = new NurseRoster[]
            {
                // 外科护士排班 (SUR - Ward SUR-W01)
                new NurseRoster
                {
                    StaffId = "N001", // 王护士 (护士长)
                    WardId = "SUR-W01",  // 外科病区
                    ShiftId = "DAY",
                    WorkDate = DateOnly.FromDateTime(DateTime.Today),
                    Status = "Scheduled"
                },
                new NurseRoster
                {
                    StaffId = "N002", // 赵护士
                    WardId = "SUR-W01",  // 外科病区
                    ShiftId = "EVENING",
                    WorkDate = DateOnly.FromDateTime(DateTime.Today),
                    Status = "Scheduled"
                },
                new NurseRoster
                {
                    StaffId = "N008", // 周护士
                    WardId = "SUR-W01",  // 外科病区
                    ShiftId = "NIGHT",
                    WorkDate = DateOnly.FromDateTime(DateTime.Today),
                    Status = "Scheduled"
                },
                
                // 内科护士排班 (IM - Ward IM-W01)
                new NurseRoster
                {
                    StaffId = "N004", // 张护士 (责任组长)
                    WardId = "IM-W01",  // 内科一病区
                    ShiftId = "DAY",
                    WorkDate = DateOnly.FromDateTime(DateTime.Today),
                    Status = "Scheduled"
                },
                new NurseRoster
                {
                    StaffId = "N003", // 李护士
                    WardId = "IM-W01",  // 内科一病区
                    ShiftId = "EVENING",
                    WorkDate = DateOnly.FromDateTime(DateTime.Today),
                    Status = "Scheduled"
                },
                new NurseRoster
                {
                    StaffId = "N007", // 吴护士
                    WardId = "IM-W02",  // 内科二病区
                    ShiftId = "NIGHT",
                    WorkDate = DateOnly.FromDateTime(DateTime.Today),
                    Status = "Scheduled"
                },
                
                // 儿科护士排班 (PED - Ward PED-W01)
                new NurseRoster
                {
                    StaffId = "N006", // 刘护士 (责任组长)
                    WardId = "PED-W01",  // 儿科病区
                    ShiftId = "DAY",
                    WorkDate = DateOnly.FromDateTime(DateTime.Today),
                    Status = "Scheduled"
                },
                new NurseRoster
                {
                    StaffId = "N005", // 陈护士
                    WardId = "PED-W01",  // 儿科病区
                    ShiftId = "EVENING",
                    WorkDate = DateOnly.FromDateTime(DateTime.Today),
                    Status = "Scheduled"
                },
                
                // 明天的排班安排 (部分轮换)
                new NurseRoster
                {
                    StaffId = "N002", // 赵护士
                    WardId = "SUR-W01",  // 外科病区
                    ShiftId = "DAY",
                    WorkDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                    Status = "Scheduled"
                },
                new NurseRoster
                {
                    StaffId = "N001", // 王护士 (护士长)
                    WardId = "SUR-W01",  // 外科病区
                    ShiftId = "EVENING",
                    WorkDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                    Status = "Scheduled"
                },
                new NurseRoster
                {
                    StaffId = "N003", // 李护士
                    WardId = "IM-W02",  // 内科二病区
                    ShiftId = "DAY",
                    WorkDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                    Status = "Scheduled"
                },
                new NurseRoster
                {
                    StaffId = "N005", // 陈护士
                    WardId = "PED-W01",  // 儿科病区
                    ShiftId = "DAY",
                    WorkDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                    Status = "Scheduled"
                }
            };
            context.NurseRosters.AddRange(nurseRosters);
            context.SaveChanges(); // 保存排班数据
            
            // // --- 执行任务数据 (ExecutionTask) ---
            // var executionTasks = new CareFlow.Core.Models.Nursing.ExecutionTask[]
            // {
            //     // P001 的药品执行任务 (阿司匹林 - 今日已完成)
            //     new CareFlow.Core.Models.Nursing.ExecutionTask
            //     {
            //         MedicalOrderId = medicationOrders[0].Id,
            //         PatientId = "P001",
            //         Category = TaskCategory.Immediate,
            //         PlannedStartTime = currentTime.Date.AddHours(0).AddMinutes(30), // UTC 00:30 (北京 08:30 早餐后)
            //         ActualStartTime = currentTime.Date.AddHours(0).AddMinutes(32),
            //         ExecutorStaffId = "N003",
            //         ActualEndTime = currentTime.Date.AddHours(0).AddMinutes(35),
            //         CompleterNurseId = "N003",
            //         Status = ExecutionTaskStatus.Completed,
            //         DataPayload = "{\"taskType\":\"Medication\",\"title\":\"口服阿司匹林 100mg\",\"drugName\":\"阿司匹林片\"}",
            //         ResultPayload = "{\"note\":\"患者已服药，无不适\"}"
            //     },
                
            //     // P001 的静脉滴注任务 (生理盐水 - 正在执行)
            //     new CareFlow.Core.Models.Nursing.ExecutionTask
            //     {
            //         MedicalOrderId = medicationOrders[1].Id,
            //         PatientId = "P001",
            //         Category = TaskCategory.Duration,
            //         PlannedStartTime = currentTime.AddHours(-1),
            //         ActualStartTime = currentTime.AddHours(-0.5),
            //         ExecutorStaffId = "N003",
            //         Status = ExecutionTaskStatus.InProgress,
            //         DataPayload = "{\"taskType\":\"IVGTT\",\"title\":\"静脉滴注0.9%氯化钠注射液 250ml\",\"drugName\":\"生理盐水\"}",
            //         ResultPayload = null
            //     },
                
            //     // P002 的胰岛素注射任务 (今日早餐前 - 已完成)
            //     new CareFlow.Core.Models.Nursing.ExecutionTask
            //     {
            //         MedicalOrderId = medicationOrders[2].Id,
            //         PatientId = "P002",
            //         Category = TaskCategory.Immediate,
            //         PlannedStartTime = currentTime.Date.AddHours(23), // UTC 前一天 23:00 (北京 07:00 早餐前)
            //         ActualStartTime = currentTime.Date.AddHours(23).AddMinutes(5),
            //         ExecutorStaffId = "N004",
            //         ActualEndTime = currentTime.Date.AddHours(23).AddMinutes(10),
            //         CompleterNurseId = "N004",
            //         Status = ExecutionTaskStatus.Completed,
            //         DataPayload = "{\"taskType\":\"Medication\",\"title\":\"皮下注射胰岛素 8单位\",\"drugName\":\"精蛋白锌重组人胰岛素\"}",
            //         ResultPayload = "{\"note\":\"注射部位：腹部，患者血糖监测正常\"}"
            //     },
                
            //     // P002 的胰岛素注射任务 (今日午餐前 - 待执行)
            //     new CareFlow.Core.Models.Nursing.ExecutionTask
            //     {
            //         MedicalOrderId = medicationOrders[2].Id,
            //         PatientId = "P002",
            //         Category = TaskCategory.Immediate,
            //         PlannedStartTime = currentTime.Date.AddHours(3).AddMinutes(30), // UTC 03:30 (北京 11:30 午餐前)
            //         Status = ExecutionTaskStatus.Pending,
            //         DataPayload = "{\"taskType\":\"Medication\",\"title\":\"皮下注射胰岛素 8单位\",\"drugName\":\"精蛋白锌重组人胰岛素\"}"
            //     },
                
            //     // P003 的头孢曲松钠任务 (今日第二次给药 - 待执行)
            //     new CareFlow.Core.Models.Nursing.ExecutionTask
            //     {
            //         MedicalOrderId = medicationOrders[3].Id,
            //         PatientId = "P003",
            //         Category = TaskCategory.Immediate,
            //         PlannedStartTime = currentTime.Date.AddHours(8), // UTC 08:00 (北京 16:00)
            //         Status = ExecutionTaskStatus.Pending,
            //         DataPayload = "{\"taskType\":\"Medication\",\"title\":\"静脉推注头孢曲松钠 1.0g\",\"drugName\":\"头孢曲松钠\",\"note\":\"皮试阴性\"}"
            //     },
                
            //     // P001 的生命体征采集任务 (今日早晨 - 已完成)
            //     new CareFlow.Core.Models.Nursing.ExecutionTask
            //     {
            //         MedicalOrderId = operationOrders[0].Id,
            //         PatientId = "P001",
            //         Category = TaskCategory.DataCollection,
            //         PlannedStartTime = currentTime.Date.AddHours(0), // UTC 00:00 (北京 08:00)
            //         ActualStartTime = currentTime.Date.AddHours(0).AddMinutes(10),
            //         ExecutorStaffId = "N003",
            //         ActualEndTime = currentTime.Date.AddHours(0).AddMinutes(15),
            //         CompleterNurseId = "N003",
            //         Status = ExecutionTaskStatus.Completed,
            //         DataPayload = "{\"taskType\":\"VitalSigns\",\"title\":\"生命体征测量\"}",
            //         ResultPayload = "{\"temperature\":36.5,\"pulse\":78,\"respiration\":18,\"systolic\":120,\"diastolic\":80}"
            //     },
                
            //     // P002 的生命体征采集任务 (今日早晨 - 已完成，体温异常)
            //     new CareFlow.Core.Models.Nursing.ExecutionTask
            //     {
            //         MedicalOrderId = operationOrders[0].Id,
            //         PatientId = "P002",
            //         Category = TaskCategory.DataCollection,
            //         PlannedStartTime = currentTime.Date.AddHours(0), // UTC 00:00 (北京 08:00)
            //         ActualStartTime = currentTime.Date.AddHours(0).AddMinutes(5),
            //         ExecutorStaffId = "N004",
            //         ActualEndTime = currentTime.Date.AddHours(0).AddMinutes(12),
            //         CompleterNurseId = "N004",
            //         Status = ExecutionTaskStatus.Completed,
            //         DataPayload = "{\"taskType\":\"VitalSigns\",\"title\":\"生命体征测量\"}",
            //         ResultPayload = "{\"temperature\":38.2,\"pulse\":92,\"respiration\":20,\"systolic\":135,\"diastolic\":85,\"note\":\"体温异常，已通知医生\"}"
            //     },
                
            //     // P003 的手术准备任务 (手术区域备皮 - 已完成)
            //     new CareFlow.Core.Models.Nursing.ExecutionTask
            //     {
            //         MedicalOrderId = surgicalOrders[0].Id,
            //         PatientId = "P003",
            //         Category = TaskCategory.Verification,
            //         PlannedStartTime = currentTime.AddHours(-2),
            //         ActualStartTime = currentTime.AddHours(-1.8),
            //         ExecutorStaffId = "N001",
            //         ActualEndTime = currentTime.AddHours(-1.6),
            //         CompleterNurseId = "N001",
            //         Status = ExecutionTaskStatus.Completed,
            //         DataPayload = "{\"taskType\":\"SurgicalPrep\",\"title\":\"手术区域备皮\",\"surgeryName\":\"腹腔镜阑尾切除术\"}",
            //         ResultPayload = "{\"note\":\"备皮完成，皮肤完整无破损\"}"
            //     },
                
            //     // P003 的手术准备任务 (建立静脉通路 - 已完成)
            //     new CareFlow.Core.Models.Nursing.ExecutionTask
            //     {
            //         MedicalOrderId = surgicalOrders[0].Id,
            //         PatientId = "P003",
            //         Category = TaskCategory.Verification,
            //         PlannedStartTime = currentTime.AddHours(-1),
            //         ActualStartTime = currentTime.AddHours(-0.8),
            //         ExecutorStaffId = "N002",
            //         ActualEndTime = currentTime.AddHours(-0.7),
            //         CompleterNurseId = "N002",
            //         Status = ExecutionTaskStatus.Completed,
            //         DataPayload = "{\"taskType\":\"SurgicalPrep\",\"title\":\"建立静脉通路\",\"surgeryName\":\"腹腔镜阑尾切除术\"}",
            //         ResultPayload = "{\"note\":\"右手背静脉留置针18G，回血良好\"}"
            //     },
                
            //     // P003 的手术准备任务 (留置导尿管 - 待执行)
            //     new CareFlow.Core.Models.Nursing.ExecutionTask
            //     {
            //         MedicalOrderId = surgicalOrders[0].Id,
            //         PatientId = "P003",
            //         Category = TaskCategory.Verification,
            //         PlannedStartTime = currentTime.AddMinutes(30),
            //         Status = ExecutionTaskStatus.Pending,
            //         DataPayload = "{\"taskType\":\"SurgicalPrep\",\"title\":\"留置导尿管\",\"surgeryName\":\"腹腔镜阑尾切除术\"}"
            //     },
                
            //     // P006 的手术准备任务 (术前抗生素皮试 - 已完成)
            //     new CareFlow.Core.Models.Nursing.ExecutionTask
            //     {
            //         MedicalOrderId = surgicalOrders[2].Id,
            //         PatientId = "P006",
            //         Category = TaskCategory.ResultPending,
            //         PlannedStartTime = currentTime.AddHours(-1.5),
            //         ActualStartTime = currentTime.AddHours(-1.4),
            //         ExecutorStaffId = "N007",
            //         ActualEndTime = currentTime.AddHours(-1.2),
            //         CompleterNurseId = "N007",
            //         Status = ExecutionTaskStatus.Completed,
            //         DataPayload = "{\"taskType\":\"SkinTest\",\"title\":\"头孢曲松钠皮试\",\"drugName\":\"头孢曲松钠\"}",
            //         ResultPayload = "{\"result\":\"阴性\",\"note\":\"皮试 (-)\"}"
            //     },
                
            //     // P005 的混合静脉滴注任务 (头孢+盐水 - 今日早餐后，待执行)
            //     new CareFlow.Core.Models.Nursing.ExecutionTask
            //     {
            //         MedicalOrderId = medicationOrders[5].Id,
            //         PatientId = "P005",
            //         Category = TaskCategory.Duration,
            //         PlannedStartTime = currentTime.Date.AddHours(0).AddMinutes(30), // UTC 00:30 (北京 08:30)
            //         Status = ExecutionTaskStatus.Pending,
            //         DataPayload = "{\"taskType\":\"IVGTT\",\"title\":\"静脉滴注：盐水100ml+头孢曲松钠2.0g\",\"drugName\":\"混合液\",\"note\":\"皮试阴性\"}"
            //     },
                
            //     // P004 的眼膏涂抹任务 (今日早餐后 - 已完成)
            //     new CareFlow.Core.Models.Nursing.ExecutionTask
            //     {
            //         MedicalOrderId = medicationOrders[4].Id,
            //         PatientId = "P004",
            //         Category = TaskCategory.Immediate,
            //         PlannedStartTime = currentTime.Date.AddHours(0).AddMinutes(30), // UTC 00:30 (北京 08:30)
            //         ActualStartTime = currentTime.Date.AddHours(0).AddMinutes(40),
            //         ExecutorStaffId = "N002",
            //         ActualEndTime = currentTime.Date.AddHours(0).AddMinutes(42),
            //         CompleterNurseId = "N002",
            //         Status = ExecutionTaskStatus.Completed,
            //         DataPayload = "{\"taskType\":\"Medication\",\"title\":\"外用红霉素眼膏\",\"drugName\":\"红霉素眼膏\"}",
            //         ResultPayload = "{\"note\":\"双眼睑内薄层涂抹\"}"
            //     },
                
            //     // P001 的超时任务 (昨日晚餐后阿司匹林 - 未完成，超时)
            //     new CareFlow.Core.Models.Nursing.ExecutionTask
            //     {
            //         MedicalOrderId = medicationOrders[0].Id,
            //         PatientId = "P001",
            //         Category = TaskCategory.Immediate,
            //         PlannedStartTime = currentTime.AddDays(-1).Date.AddHours(11), // 昨日 UTC 11:00 (北京 19:00)
            //         Status = ExecutionTaskStatus.Incomplete,
            //         DataPayload = "{\"taskType\":\"Medication\",\"title\":\"口服阿司匹林 100mg\",\"drugName\":\"阿司匹林片\"}",
            //         ExceptionReason = "患者拒绝服药，已记录"
            //     },
                
            //     // P002 的临期任务 (今日晚餐前胰岛素 - 即将到期)
            //     new CareFlow.Core.Models.Nursing.ExecutionTask
            //     {
            //         MedicalOrderId = medicationOrders[2].Id,
            //         PatientId = "P002",
            //         Category = TaskCategory.Immediate,
            //         PlannedStartTime = currentTime.Date.AddHours(9).AddMinutes(30), // UTC 09:30 (北京 17:30)
            //         Status = ExecutionTaskStatus.Pending,
            //         DataPayload = "{\"taskType\":\"Medication\",\"title\":\"皮下注射胰岛素 8单位\",\"drugName\":\"精蛋白锌重组人胰岛素\"}"
            //     }
            // };
            // context.ExecutionTasks.AddRange(executionTasks);
            
            // 最后的保存
            context.SaveChanges();
        }
    }
}