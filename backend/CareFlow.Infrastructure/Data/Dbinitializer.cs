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
            // 修改说明：移除大内科/大外科，细分为二级专科
            var departments = new Department[]
            {
                // === 内科系统 ===
                new Department { Id = "CARD", DeptName = "心血管内科", Location = "住院部A栋3楼" },
                new Department { Id = "RESP", DeptName = "呼吸内科", Location = "住院部A栋4楼" },
                new Department { Id = "GAST", DeptName = "消化内科", Location = "住院部A栋5楼" },
                new Department { Id = "NEUR", DeptName = "神经内科", Location = "住院部A栋6楼" },
                new Department { Id = "NEPH", DeptName = "肾内科", Location = "住院部A栋7楼" },
                new Department { Id = "ENDO", DeptName = "内分泌科", Location = "住院部A栋8楼" },
                new Department { Id = "HEMA", DeptName = "血液内科", Location = "住院部A栋9楼" },

                // === 外科系统 ===
                new Department { Id = "GEN_SUR", DeptName = "普通外科", Location = "住院部B栋3楼" }, 
                new Department { Id = "ORTH", DeptName = "骨科", Location = "住院部B栋4楼" },
                new Department { Id = "NSUR", DeptName = "神经外科", Location = "住院部B栋5楼" },
                new Department { Id = "CT_SUR", DeptName = "心胸外科", Location = "住院部B栋6楼" },
                new Department { Id = "UROL", DeptName = "泌尿外科", Location = "住院部B栋7楼" },
                new Department { Id = "BURN", DeptName = "烧伤科", Location = "住院部B栋8楼" },

                // === 妇产科系统 ===
                new Department { Id = "GYN", DeptName = "妇科", Location = "住院部C栋3楼" },
                new Department { Id = "OBS", DeptName = "产科", Location = "住院部C栋4楼" },

                // === 其他保留科室 ===
                new Department { Id = "PED", DeptName = "儿科", Location = "住院部B栋2楼" },
                new Department { Id = "ADM", DeptName = "行政", Location = "行政楼101室" },
                new Department { Id = "CHK", DeptName = "检查站", Location = "检查站1楼" },
            };
            context.Departments.AddRange(departments);
            context.SaveChanges();

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

            // 医生数据 - 按照科室顺序，每个科室一个医生
            var doctors = new Doctor[]
            {
                // === 内科系统 ===
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
                    DeptCode = "CARD",
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
                    DeptCode = "RESP",
                    Title = DoctorTitle.Chief.ToString(),
                    PrescriptionAuthLevel = "High"
                },
                new Doctor
                {
                    Id = "D003",
                    EmployeeNumber = "doc003",
                    PasswordHash = defaultHashedPassword,
                    Name = "王医生",
                    IdCard = "110100200001010003",
                    Phone = "13912340003",
                    RoleType = "Doctor",
                    IsActive = true,
                    DeptCode = "GAST",
                    Title = DoctorTitle.Chief.ToString(),
                    PrescriptionAuthLevel = "High"
                },
                new Doctor
                {
                    Id = "D004",
                    EmployeeNumber = "doc004",
                    PasswordHash = defaultHashedPassword,
                    Name = "赵医生",
                    IdCard = "110100200001010004",
                    Phone = "13912340004",
                    RoleType = "Doctor",
                    IsActive = true,
                    DeptCode = "NEUR",
                    Title = DoctorTitle.Chief.ToString(),
                    PrescriptionAuthLevel = "High"
                },
                new Doctor
                {
                    Id = "D005",
                    EmployeeNumber = "doc005",
                    PasswordHash = defaultHashedPassword,
                    Name = "刘医生",
                    IdCard = "110100200001010005",
                    Phone = "13912340005",
                    RoleType = "Doctor",
                    IsActive = true,
                    DeptCode = "NEPH",
                    Title = DoctorTitle.Chief.ToString(),
                    PrescriptionAuthLevel = "High"
                },
                new Doctor
                {
                    Id = "D006",
                    EmployeeNumber = "doc006",
                    PasswordHash = defaultHashedPassword,
                    Name = "杨医生",
                    IdCard = "110100200001010006",
                    Phone = "13912340006",
                    RoleType = "Doctor",
                    IsActive = true,
                    DeptCode = "ENDO",
                    Title = DoctorTitle.Chief.ToString(),
                    PrescriptionAuthLevel = "High"
                },
                new Doctor
                {
                    Id = "D007",
                    EmployeeNumber = "doc007",
                    PasswordHash = defaultHashedPassword,
                    Name = "陈医生",
                    IdCard = "110100200001010007",
                    Phone = "13912340007",
                    RoleType = "Doctor",
                    IsActive = true,
                    DeptCode = "HEMA",
                    Title = DoctorTitle.Chief.ToString(),
                    PrescriptionAuthLevel = "High"
                },
                // === 外科系统 ===
                new Doctor
                {
                    Id = "D008",
                    EmployeeNumber = "doc008",
                    PasswordHash = defaultHashedPassword,
                    Name = "周医生",
                    IdCard = "110100200001010008",
                    Phone = "13912340008",
                    RoleType = "Doctor",
                    IsActive = true,
                    DeptCode = "GEN_SUR",
                    Title = DoctorTitle.Chief.ToString(),
                    PrescriptionAuthLevel = "High"
                },
                new Doctor
                {
                    Id = "D009",
                    EmployeeNumber = "doc009",
                    PasswordHash = defaultHashedPassword,
                    Name = "吴医生",
                    IdCard = "110100200001010009",
                    Phone = "13912340009",
                    RoleType = "Doctor",
                    IsActive = true,
                    DeptCode = "ORTH",
                    Title = DoctorTitle.Chief.ToString(),
                    PrescriptionAuthLevel = "High"
                },
                new Doctor
                {
                    Id = "D010",
                    EmployeeNumber = "doc010",
                    PasswordHash = defaultHashedPassword,
                    Name = "徐医生",
                    IdCard = "110100200001010010",
                    Phone = "13912340010",
                    RoleType = "Doctor",
                    IsActive = true,
                    DeptCode = "NSUR",
                    Title = DoctorTitle.Chief.ToString(),
                    PrescriptionAuthLevel = "High"
                },
                new Doctor
                {
                    Id = "D011",
                    EmployeeNumber = "doc011",
                    PasswordHash = defaultHashedPassword,
                    Name = "孙医生",
                    IdCard = "110100200001010011",
                    Phone = "13912340011",
                    RoleType = "Doctor",
                    IsActive = true,
                    DeptCode = "CT_SUR",
                    Title = DoctorTitle.Chief.ToString(),
                    PrescriptionAuthLevel = "High"
                },
                new Doctor
                {
                    Id = "D012",
                    EmployeeNumber = "doc012",
                    PasswordHash = defaultHashedPassword,
                    Name = "马医生",
                    IdCard = "110100200001010012",
                    Phone = "13912340012",
                    RoleType = "Doctor",
                    IsActive = true,
                    DeptCode = "UROL",
                    Title = DoctorTitle.Chief.ToString(),
                    PrescriptionAuthLevel = "High"
                },
                new Doctor
                {
                    Id = "D013",
                    EmployeeNumber = "doc013",
                    PasswordHash = defaultHashedPassword,
                    Name = "朱医生",
                    IdCard = "110100200001010013",
                    Phone = "13912340013",
                    RoleType = "Doctor",
                    IsActive = true,
                    DeptCode = "BURN",
                    Title = DoctorTitle.Chief.ToString(),
                    PrescriptionAuthLevel = "High"
                },
                // === 妇产科系统 ===
                new Doctor
                {
                    Id = "D014",
                    EmployeeNumber = "doc014",
                    PasswordHash = defaultHashedPassword,
                    Name = "胡医生",
                    IdCard = "110100200001010014",
                    Phone = "13912340014",
                    RoleType = "Doctor",
                    IsActive = true,
                    DeptCode = "GYN",
                    Title = DoctorTitle.Chief.ToString(),
                    PrescriptionAuthLevel = "High"
                },
                new Doctor
                {
                    Id = "D015",
                    EmployeeNumber = "doc015",
                    PasswordHash = defaultHashedPassword,
                    Name = "林医生",
                    IdCard = "110100200001010015",
                    Phone = "13912340015",
                    RoleType = "Doctor",
                    IsActive = true,
                    DeptCode = "OBS",
                    Title = DoctorTitle.Chief.ToString(),
                    PrescriptionAuthLevel = "High"
                },
                // === 其他保留科室 ===
                new Doctor
                {
                    Id = "D016",
                    EmployeeNumber = "doc016",
                    PasswordHash = defaultHashedPassword,
                    Name = "郭医生",
                    IdCard = "110100200001010016",
                    Phone = "13912340016",
                    RoleType = "Doctor",
                    IsActive = true,
                    DeptCode = "PED",
                    Title = DoctorTitle.Chief.ToString(),
                    PrescriptionAuthLevel = "High"
                }
            };
            
            // 护士数据 - 按照科室顺序，每个病区3个护士
            var nurses = new Nurse[]
            {
                // === 心血管内科 CARD 病区护士 ===
                new Nurse
                {
                    Id = "N001",
                    EmployeeNumber = "nurse001",
                    PasswordHash = defaultHashedPassword,
                    Name = "梁护士",
                    IdCard = "110100200001010003",
                    Phone = "13912340003",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "CARD",
                    NurseRank = NurseRank.HeadNurse.ToString()
                },
                new Nurse
                {
                    Id = "N002",
                    EmployeeNumber = "nurse002",
                    PasswordHash = defaultHashedPassword,
                    Name = "谢护士",
                    IdCard = "110100200001010004",
                    Phone = "13912340004",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "CARD",
                    NurseRank = NurseRank.TeamLeader.ToString()
                },
                new Nurse
                {
                    Id = "N003",
                    EmployeeNumber = "nurse003",
                    PasswordHash = defaultHashedPassword,
                    Name = "宋护士",
                    IdCard = "110100200001010005",
                    Phone = "13912340005",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "CARD",
                    NurseRank = NurseRank.RegularNurse.ToString()
                },
                new Nurse
                {
                    Id = "N004",
                    EmployeeNumber = "nurse004",
                    PasswordHash = defaultHashedPassword,
                    Name = "唐护士",
                    IdCard = "110100200001010006",
                    Phone = "13912340006",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "CARD",
                    NurseRank = NurseRank.HeadNurse.ToString()
                },
                new Nurse
                {
                    Id = "N005",
                    EmployeeNumber = "nurse005",
                    PasswordHash = defaultHashedPassword,
                    Name = "许护士",
                    IdCard = "110100200001010007",
                    Phone = "13912340007",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "CARD",
                    NurseRank = NurseRank.TeamLeader.ToString()
                },
                new Nurse
                {
                    Id = "N006",
                    EmployeeNumber = "nurse006",
                    PasswordHash = defaultHashedPassword,
                    Name = "韩护士",
                    IdCard = "110100200001010008",
                    Phone = "13912340008",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "CARD",
                    NurseRank = NurseRank.RegularNurse.ToString()
                },
                // === 呼吸内科 RESP 病区护士 ===
                new Nurse
                {
                    Id = "N007",
                    EmployeeNumber = "nurse007",
                    PasswordHash = defaultHashedPassword,
                    Name = "冯护士",
                    IdCard = "110100200001010009",
                    Phone = "13912340009",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "RESP",
                    NurseRank = NurseRank.HeadNurse.ToString()
                },
                new Nurse
                {
                    Id = "N008",
                    EmployeeNumber = "nurse008",
                    PasswordHash = defaultHashedPassword,
                    Name = "邓护士",
                    IdCard = "110100200001010010",
                    Phone = "13912340010",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "RESP",
                    NurseRank = NurseRank.TeamLeader.ToString()
                },
                new Nurse
                {
                    Id = "N009",
                    EmployeeNumber = "nurse009",
                    PasswordHash = defaultHashedPassword,
                    Name = "曹护士",
                    IdCard = "110100200001010011",
                    Phone = "13912340011",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "RESP",
                    NurseRank = NurseRank.RegularNurse.ToString()
                },
                new Nurse
                {
                    Id = "N010",
                    EmployeeNumber = "nurse010",
                    PasswordHash = defaultHashedPassword,
                    Name = "彭护士",
                    IdCard = "110100200001010012",
                    Phone = "13912340012",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "RESP",
                    NurseRank = NurseRank.HeadNurse.ToString()
                },
                new Nurse
                {
                    Id = "N011",
                    EmployeeNumber = "nurse011",
                    PasswordHash = defaultHashedPassword,
                    Name = "曾护士",
                    IdCard = "110100200001010013",
                    Phone = "13912340013",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "RESP",
                    NurseRank = NurseRank.TeamLeader.ToString()
                },
                new Nurse
                {
                    Id = "N012",
                    EmployeeNumber = "nurse012",
                    PasswordHash = defaultHashedPassword,
                    Name = "肖护士",
                    IdCard = "110100200001010014",
                    Phone = "13912340014",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "RESP",
                    NurseRank = NurseRank.RegularNurse.ToString()
                },
                // === 消化内科 GAST 病区护士 ===
                new Nurse
                {
                    Id = "N013",
                    EmployeeNumber = "nurse013",
                    PasswordHash = defaultHashedPassword,
                    Name = "田护士",
                    IdCard = "110100200001010015",
                    Phone = "13912340015",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "GAST",
                    NurseRank = NurseRank.HeadNurse.ToString()
                },
                new Nurse
                {
                    Id = "N014",
                    EmployeeNumber = "nurse014",
                    PasswordHash = defaultHashedPassword,
                    Name = "董护士",
                    IdCard = "110100200001010016",
                    Phone = "13912340016",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "GAST",
                    NurseRank = NurseRank.TeamLeader.ToString()
                },
                new Nurse
                {
                    Id = "N015",
                    EmployeeNumber = "nurse015",
                    PasswordHash = defaultHashedPassword,
                    Name = "袁护士",
                    IdCard = "110100200001010017",
                    Phone = "13912340017",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "GAST",
                    NurseRank = NurseRank.RegularNurse.ToString()
                },
                new Nurse
                {
                    Id = "N016",
                    EmployeeNumber = "nurse016",
                    PasswordHash = defaultHashedPassword,
                    Name = "潘护士",
                    IdCard = "110100200001010018",
                    Phone = "13912340018",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "GAST",
                    NurseRank = NurseRank.HeadNurse.ToString()
                },
                new Nurse
                {
                    Id = "N017",
                    EmployeeNumber = "nurse017",
                    PasswordHash = defaultHashedPassword,
                    Name = "于护士",
                    IdCard = "110100200001010019",
                    Phone = "13912340019",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "GAST",
                    NurseRank = NurseRank.TeamLeader.ToString()
                },
                new Nurse
                {
                    Id = "N018",
                    EmployeeNumber = "nurse018",
                    PasswordHash = defaultHashedPassword,
                    Name = "蒋护士",
                    IdCard = "110100200001010020",
                    Phone = "13912340020",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "GAST",
                    NurseRank = NurseRank.RegularNurse.ToString()
                },
                // === 神经内科 NEUR 病区护士 ===
                new Nurse
                {
                    Id = "N019",
                    EmployeeNumber = "nurse019",
                    PasswordHash = defaultHashedPassword,
                    Name = "蔡护士",
                    IdCard = "110100200001010021",
                    Phone = "13912340021",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "NEUR",
                    NurseRank = NurseRank.HeadNurse.ToString()
                },
                new Nurse
                {
                    Id = "N020",
                    EmployeeNumber = "nurse020",
                    PasswordHash = defaultHashedPassword,
                    Name = "余护士",
                    IdCard = "110100200001010022",
                    Phone = "13912340022",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "NEUR",
                    NurseRank = NurseRank.TeamLeader.ToString()
                },
                new Nurse
                {
                    Id = "N021",
                    EmployeeNumber = "nurse021",
                    PasswordHash = defaultHashedPassword,
                    Name = "杜护士",
                    IdCard = "110100200001010023",
                    Phone = "13912340023",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "NEUR",
                    NurseRank = NurseRank.RegularNurse.ToString()
                },
                new Nurse
                {
                    Id = "N022",
                    EmployeeNumber = "nurse022",
                    PasswordHash = defaultHashedPassword,
                    Name = "叶护士",
                    IdCard = "110100200001010024",
                    Phone = "13912340024",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "NEUR",
                    NurseRank = NurseRank.HeadNurse.ToString()
                },
                new Nurse
                {
                    Id = "N023",
                    EmployeeNumber = "nurse023",
                    PasswordHash = defaultHashedPassword,
                    Name = "程护士",
                    IdCard = "110100200001010025",
                    Phone = "13912340025",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "NEUR",
                    NurseRank = NurseRank.TeamLeader.ToString()
                },
                new Nurse
                {
                    Id = "N024",
                    EmployeeNumber = "nurse024",
                    PasswordHash = defaultHashedPassword,
                    Name = "苏护士",
                    IdCard = "110100200001010026",
                    Phone = "13912340026",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "NEUR",
                    NurseRank = NurseRank.RegularNurse.ToString()
                },
                // === 肾内科 NEPH 病区护士 ===
                new Nurse
                {
                    Id = "N025",
                    EmployeeNumber = "nurse025",
                    PasswordHash = defaultHashedPassword,
                    Name = "魏护士",
                    IdCard = "110100200001010027",
                    Phone = "13912340027",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "NEPH",
                    NurseRank = NurseRank.HeadNurse.ToString()
                },
                new Nurse
                {
                    Id = "N026",
                    EmployeeNumber = "nurse026",
                    PasswordHash = defaultHashedPassword,
                    Name = "吕护士",
                    IdCard = "110100200001010028",
                    Phone = "13912340028",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "NEPH",
                    NurseRank = NurseRank.TeamLeader.ToString()
                },
                new Nurse
                {
                    Id = "N027",
                    EmployeeNumber = "nurse027",
                    PasswordHash = defaultHashedPassword,
                    Name = "丁护士",
                    IdCard = "110100200001010029",
                    Phone = "13912340029",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "NEPH",
                    NurseRank = NurseRank.RegularNurse.ToString()
                },
                new Nurse
                {
                    Id = "N028",
                    EmployeeNumber = "nurse028",
                    PasswordHash = defaultHashedPassword,
                    Name = "任护士",
                    IdCard = "110100200001010030",
                    Phone = "13912340030",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "NEPH",
                    NurseRank = NurseRank.HeadNurse.ToString()
                },
                new Nurse
                {
                    Id = "N029",
                    EmployeeNumber = "nurse029",
                    PasswordHash = defaultHashedPassword,
                    Name = "沈护士",
                    IdCard = "110100200001010031",
                    Phone = "13912340031",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "NEPH",
                    NurseRank = NurseRank.TeamLeader.ToString()
                },
                new Nurse
                {
                    Id = "N030",
                    EmployeeNumber = "nurse030",
                    PasswordHash = defaultHashedPassword,
                    Name = "姜护士",
                    IdCard = "110100200001010032",
                    Phone = "13912340032",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "NEPH",
                    NurseRank = NurseRank.RegularNurse.ToString()
                },
                // === 内分泌科 ENDO 病区护士 ===
                new Nurse
                {
                    Id = "N031",
                    EmployeeNumber = "nurse031",
                    PasswordHash = defaultHashedPassword,
                    Name = "范护士",
                    IdCard = "110100200001010033",
                    Phone = "13912340033",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "ENDO",
                    NurseRank = NurseRank.HeadNurse.ToString()
                },
                new Nurse
                {
                    Id = "N032",
                    EmployeeNumber = "nurse032",
                    PasswordHash = defaultHashedPassword,
                    Name = "方护士",
                    IdCard = "110100200001010034",
                    Phone = "13912340034",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "ENDO",
                    NurseRank = NurseRank.TeamLeader.ToString()
                },
                new Nurse
                {
                    Id = "N033",
                    EmployeeNumber = "nurse033",
                    PasswordHash = defaultHashedPassword,
                    Name = "石护士",
                    IdCard = "110100200001010035",
                    Phone = "13912340035",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "ENDO",
                    NurseRank = NurseRank.RegularNurse.ToString()
                },
                new Nurse
                {
                    Id = "N034",
                    EmployeeNumber = "nurse034",
                    PasswordHash = defaultHashedPassword,
                    Name = "姚护士",
                    IdCard = "110100200001010036",
                    Phone = "13912340036",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "ENDO",
                    NurseRank = NurseRank.HeadNurse.ToString()
                },
                new Nurse
                {
                    Id = "N035",
                    EmployeeNumber = "nurse035",
                    PasswordHash = defaultHashedPassword,
                    Name = "谭护士",
                    IdCard = "110100200001010037",
                    Phone = "13912340037",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "ENDO",
                    NurseRank = NurseRank.TeamLeader.ToString()
                },
                new Nurse
                {
                    Id = "N036",
                    EmployeeNumber = "nurse036",
                    PasswordHash = defaultHashedPassword,
                    Name = "廖护士",
                    IdCard = "110100200001010038",
                    Phone = "13912340038",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "ENDO",
                    NurseRank = NurseRank.RegularNurse.ToString()
                },
                // === 血液内科 HEMA 病区护士 ===
                new Nurse
                {
                    Id = "N037",
                    EmployeeNumber = "nurse037",
                    PasswordHash = defaultHashedPassword,
                    Name = "邹护士",
                    IdCard = "110100200001010039",
                    Phone = "13912340039",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "HEMA",
                    NurseRank = NurseRank.HeadNurse.ToString()
                },
                new Nurse
                {
                    Id = "N038",
                    EmployeeNumber = "nurse038",
                    PasswordHash = defaultHashedPassword,
                    Name = "熊护士",
                    IdCard = "110100200001010040",
                    Phone = "13912340040",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "HEMA",
                    NurseRank = NurseRank.TeamLeader.ToString()
                },
                new Nurse
                {
                    Id = "N039",
                    EmployeeNumber = "nurse039",
                    PasswordHash = defaultHashedPassword,
                    Name = "金护士",
                    IdCard = "110100200001010041",
                    Phone = "13912340041",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "HEMA",
                    NurseRank = NurseRank.RegularNurse.ToString()
                },
                new Nurse
                {
                    Id = "N040",
                    EmployeeNumber = "nurse040",
                    PasswordHash = defaultHashedPassword,
                    Name = "陆护士",
                    IdCard = "110100200001010042",
                    Phone = "13912340042",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "HEMA",
                    NurseRank = NurseRank.HeadNurse.ToString()
                },
                new Nurse
                {
                    Id = "N041",
                    EmployeeNumber = "nurse041",
                    PasswordHash = defaultHashedPassword,
                    Name = "郝护士",
                    IdCard = "110100200001010043",
                    Phone = "13912340043",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "HEMA",
                    NurseRank = NurseRank.TeamLeader.ToString()
                },
                new Nurse
                {
                    Id = "N042",
                    EmployeeNumber = "nurse042",
                    PasswordHash = defaultHashedPassword,
                    Name = "孔护士",
                    IdCard = "110100200001010044",
                    Phone = "13912340044",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "HEMA",
                    NurseRank = NurseRank.RegularNurse.ToString()
                },
                // === 普通外科 GEN_SUR 病区护士 ===
                new Nurse
                {
                    Id = "N043",
                    EmployeeNumber = "nurse043",
                    PasswordHash = defaultHashedPassword,
                    Name = "白护士",
                    IdCard = "110100200001010045",
                    Phone = "13912340045",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "GEN_SUR",
                    NurseRank = NurseRank.HeadNurse.ToString()
                },
                new Nurse
                {
                    Id = "N044",
                    EmployeeNumber = "nurse044",
                    PasswordHash = defaultHashedPassword,
                    Name = "崔护士",
                    IdCard = "110100200001010046",
                    Phone = "13912340046",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "GEN_SUR",
                    NurseRank = NurseRank.TeamLeader.ToString()
                },
                new Nurse
                {
                    Id = "N045",
                    EmployeeNumber = "nurse045",
                    PasswordHash = defaultHashedPassword,
                    Name = "康护士",
                    IdCard = "110100200001010047",
                    Phone = "13912340047",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "GEN_SUR",
                    NurseRank = NurseRank.RegularNurse.ToString()
                },
                new Nurse
                {
                    Id = "N046",
                    EmployeeNumber = "nurse046",
                    PasswordHash = defaultHashedPassword,
                    Name = "毛护士",
                    IdCard = "110100200001010048",
                    Phone = "13912340048",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "GEN_SUR",
                    NurseRank = NurseRank.HeadNurse.ToString()
                },
                new Nurse
                {
                    Id = "N047",
                    EmployeeNumber = "nurse047",
                    PasswordHash = defaultHashedPassword,
                    Name = "邱护士",
                    IdCard = "110100200001010049",
                    Phone = "13912340049",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "GEN_SUR",
                    NurseRank = NurseRank.TeamLeader.ToString()
                },
                new Nurse
                {
                    Id = "N048",
                    EmployeeNumber = "nurse048",
                    PasswordHash = defaultHashedPassword,
                    Name = "秦护士",
                    IdCard = "110100200001010050",
                    Phone = "13912340050",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "GEN_SUR",
                    NurseRank = NurseRank.RegularNurse.ToString()
                },
                // === 骨科 ORTH 病区护士 ===
                new Nurse
                {
                    Id = "N049",
                    EmployeeNumber = "nurse049",
                    PasswordHash = defaultHashedPassword,
                    Name = "江护士",
                    IdCard = "110100200001010049",
                    Phone = "13912340049",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "ORTH",
                    NurseRank = NurseRank.HeadNurse.ToString()
                },
                new Nurse
                {
                    Id = "N050",
                    EmployeeNumber = "nurse050",
                    PasswordHash = defaultHashedPassword,
                    Name = "史护士",
                    IdCard = "110100200001010050",
                    Phone = "13912340050",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "ORTH",
                    NurseRank = NurseRank.TeamLeader.ToString()
                },
                new Nurse
                {
                    Id = "N051",
                    EmployeeNumber = "nurse051",
                    PasswordHash = defaultHashedPassword,
                    Name = "顾护士",
                    IdCard = "110100200001010051",
                    Phone = "13912340051",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "ORTH",
                    NurseRank = NurseRank.RegularNurse.ToString()
                },
                new Nurse
                {
                    Id = "N052",
                    EmployeeNumber = "nurse052",
                    PasswordHash = defaultHashedPassword,
                    Name = "侯护士",
                    IdCard = "110100200001010052",
                    Phone = "13912340052",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "ORTH",
                    NurseRank = NurseRank.HeadNurse.ToString()
                },
                new Nurse
                {
                    Id = "N053",
                    EmployeeNumber = "nurse053",
                    PasswordHash = defaultHashedPassword,
                    Name = "邵护士",
                    IdCard = "110100200001010053",
                    Phone = "13912340053",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "ORTH",
                    NurseRank = NurseRank.TeamLeader.ToString()
                },
                new Nurse
                {
                    Id = "N054",
                    EmployeeNumber = "nurse054",
                    PasswordHash = defaultHashedPassword,
                    Name = "孟护士",
                    IdCard = "110100200001010054",
                    Phone = "13912340054",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "ORTH",
                    NurseRank = NurseRank.RegularNurse.ToString()
                },
                // === 神经外科 NSUR 病区护士 ===
                new Nurse
                {
                    Id = "N055",
                    EmployeeNumber = "nurse055",
                    PasswordHash = defaultHashedPassword,
                    Name = "龙护士",
                    IdCard = "110100200001010055",
                    Phone = "13912340055",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "NSUR",
                    NurseRank = NurseRank.HeadNurse.ToString()
                },
                new Nurse
                {
                    Id = "N056",
                    EmployeeNumber = "nurse056",
                    PasswordHash = defaultHashedPassword,
                    Name = "万护士",
                    IdCard = "110100200001010056",
                    Phone = "13912340056",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "NSUR",
                    NurseRank = NurseRank.TeamLeader.ToString()
                },
                new Nurse
                {
                    Id = "N057",
                    EmployeeNumber = "nurse057",
                    PasswordHash = defaultHashedPassword,
                    Name = "段护士",
                    IdCard = "110100200001010057",
                    Phone = "13912340057",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "NSUR",
                    NurseRank = NurseRank.RegularNurse.ToString()
                },
                new Nurse
                {
                    Id = "N058",
                    EmployeeNumber = "nurse058",
                    PasswordHash = defaultHashedPassword,
                    Name = "雷护士",
                    IdCard = "110100200001010058",
                    Phone = "13912340058",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "NSUR",
                    NurseRank = NurseRank.HeadNurse.ToString()
                },
                new Nurse
                {
                    Id = "N059",
                    EmployeeNumber = "nurse059",
                    PasswordHash = defaultHashedPassword,
                    Name = "钱护士",
                    IdCard = "110100200001010059",
                    Phone = "13912340059",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "NSUR",
                    NurseRank = NurseRank.TeamLeader.ToString()
                },
                new Nurse
                {
                    Id = "N060",
                    EmployeeNumber = "nurse060",
                    PasswordHash = defaultHashedPassword,
                    Name = "汤护士",
                    IdCard = "110100200001010060",
                    Phone = "13912340060",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "NSUR",
                    NurseRank = NurseRank.RegularNurse.ToString()
                },
                // === 心胸外科 CT_SUR 病区护士 ===
                new Nurse
                {
                    Id = "N061",
                    EmployeeNumber = "nurse061",
                    PasswordHash = defaultHashedPassword,
                    Name = "尹护士",
                    IdCard = "110100200001010061",
                    Phone = "13912340061",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "CT_SUR",
                    NurseRank = NurseRank.HeadNurse.ToString()
                },
                new Nurse
                {
                    Id = "N062",
                    EmployeeNumber = "nurse062",
                    PasswordHash = defaultHashedPassword,
                    Name = "黎护士",
                    IdCard = "110100200001010062",
                    Phone = "13912340062",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "CT_SUR",
                    NurseRank = NurseRank.TeamLeader.ToString()
                },
                new Nurse
                {
                    Id = "N063",
                    EmployeeNumber = "nurse063",
                    PasswordHash = defaultHashedPassword,
                    Name = "易护士",
                    IdCard = "110100200001010063",
                    Phone = "13912340063",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "CT_SUR",
                    NurseRank = NurseRank.RegularNurse.ToString()
                },
                new Nurse
                {
                    Id = "N064",
                    EmployeeNumber = "nurse064",
                    PasswordHash = defaultHashedPassword,
                    Name = "常护士",
                    IdCard = "110100200001010064",
                    Phone = "13912340064",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "CT_SUR",
                    NurseRank = NurseRank.HeadNurse.ToString()
                },
                new Nurse
                {
                    Id = "N065",
                    EmployeeNumber = "nurse065",
                    PasswordHash = defaultHashedPassword,
                    Name = "武护士",
                    IdCard = "110100200001010065",
                    Phone = "13912340065",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "CT_SUR",
                    NurseRank = NurseRank.TeamLeader.ToString()
                },
                new Nurse
                {
                    Id = "N066",
                    EmployeeNumber = "nurse066",
                    PasswordHash = defaultHashedPassword,
                    Name = "乔护士",
                    IdCard = "110100200001010066",
                    Phone = "13912340066",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "CT_SUR",
                    NurseRank = NurseRank.RegularNurse.ToString()
                },
                // === 泌尿外科 UROL 病区护士 ===
                new Nurse
                {
                    Id = "N067",
                    EmployeeNumber = "nurse067",
                    PasswordHash = defaultHashedPassword,
                    Name = "贺护士",
                    IdCard = "110100200001010067",
                    Phone = "13912340067",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "UROL",
                    NurseRank = NurseRank.HeadNurse.ToString()
                },
                new Nurse
                {
                    Id = "N068",
                    EmployeeNumber = "nurse068",
                    PasswordHash = defaultHashedPassword,
                    Name = "赖护士",
                    IdCard = "110100200001010068",
                    Phone = "13912340068",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "UROL",
                    NurseRank = NurseRank.TeamLeader.ToString()
                },
                new Nurse
                {
                    Id = "N069",
                    EmployeeNumber = "nurse069",
                    PasswordHash = defaultHashedPassword,
                    Name = "龚护士",
                    IdCard = "110100200001010069",
                    Phone = "13912340069",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "UROL",
                    NurseRank = NurseRank.RegularNurse.ToString()
                },
                new Nurse
                {
                    Id = "N070",
                    EmployeeNumber = "nurse070",
                    PasswordHash = defaultHashedPassword,
                    Name = "文护士",
                    IdCard = "110100200001010070",
                    Phone = "13912340070",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "UROL",
                    NurseRank = NurseRank.HeadNurse.ToString()
                },
                new Nurse
                {
                    Id = "N071",
                    EmployeeNumber = "nurse071",
                    PasswordHash = defaultHashedPassword,
                    Name = "庞护士",
                    IdCard = "110100200001010071",
                    Phone = "13912340071",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "UROL",
                    NurseRank = NurseRank.TeamLeader.ToString()
                },
                new Nurse
                {
                    Id = "N072",
                    EmployeeNumber = "nurse072",
                    PasswordHash = defaultHashedPassword,
                    Name = "樊护士",
                    IdCard = "110100200001010072",
                    Phone = "13912340072",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "UROL",
                    NurseRank = NurseRank.RegularNurse.ToString()
                },
                // === 烧伤科 BURN 病区护士 ===
                new Nurse
                {
                    Id = "N073",
                    EmployeeNumber = "nurse073",
                    PasswordHash = defaultHashedPassword,
                    Name = "兰护士",
                    IdCard = "110100200001010073",
                    Phone = "13912340073",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "BURN",
                    NurseRank = NurseRank.HeadNurse.ToString()
                },
                new Nurse
                {
                    Id = "N074",
                    EmployeeNumber = "nurse074",
                    PasswordHash = defaultHashedPassword,
                    Name = "殷护士",
                    IdCard = "110100200001010074",
                    Phone = "13912340074",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "BURN",
                    NurseRank = NurseRank.TeamLeader.ToString()
                },
                new Nurse
                {
                    Id = "N075",
                    EmployeeNumber = "nurse075",
                    PasswordHash = defaultHashedPassword,
                    Name = "施护士",
                    IdCard = "110100200001010075",
                    Phone = "13912340075",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "BURN",
                    NurseRank = NurseRank.RegularNurse.ToString()
                },
                new Nurse
                {
                    Id = "N076",
                    EmployeeNumber = "nurse076",
                    PasswordHash = defaultHashedPassword,
                    Name = "陶护士",
                    IdCard = "110100200001010076",
                    Phone = "13912340076",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "BURN",
                    NurseRank = NurseRank.HeadNurse.ToString()
                },
                new Nurse
                {
                    Id = "N077",
                    EmployeeNumber = "nurse077",
                    PasswordHash = defaultHashedPassword,
                    Name = "洪护士",
                    IdCard = "110100200001010077",
                    Phone = "13912340077",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "BURN",
                    NurseRank = NurseRank.TeamLeader.ToString()
                },
                new Nurse
                {
                    Id = "N078",
                    EmployeeNumber = "nurse078",
                    PasswordHash = defaultHashedPassword,
                    Name = "翟护士",
                    IdCard = "110100200001010078",
                    Phone = "13912340078",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "BURN",
                    NurseRank = NurseRank.RegularNurse.ToString()
                },
                // === 妇科 GYN 病区护士 ===
                new Nurse
                {
                    Id = "N079",
                    EmployeeNumber = "nurse079",
                    PasswordHash = defaultHashedPassword,
                    Name = "安护士",
                    IdCard = "110100200001010079",
                    Phone = "13912340079",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "GYN",
                    NurseRank = NurseRank.HeadNurse.ToString()
                },
                new Nurse
                {
                    Id = "N080",
                    EmployeeNumber = "nurse080",
                    PasswordHash = defaultHashedPassword,
                    Name = "颜护士",
                    IdCard = "110100200001010080",
                    Phone = "13912340080",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "GYN",
                    NurseRank = NurseRank.TeamLeader.ToString()
                },
                new Nurse
                {
                    Id = "N081",
                    EmployeeNumber = "nurse081",
                    PasswordHash = defaultHashedPassword,
                    Name = "倪护士",
                    IdCard = "110100200001010081",
                    Phone = "13912340081",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "GYN",
                    NurseRank = NurseRank.RegularNurse.ToString()
                },
                new Nurse
                {
                    Id = "N082",
                    EmployeeNumber = "nurse082",
                    PasswordHash = defaultHashedPassword,
                    Name = "严护士",
                    IdCard = "110100200001010082",
                    Phone = "13912340082",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "GYN",
                    NurseRank = NurseRank.HeadNurse.ToString()
                },
                new Nurse
                {
                    Id = "N083",
                    EmployeeNumber = "nurse083",
                    PasswordHash = defaultHashedPassword,
                    Name = "牛护士",
                    IdCard = "110100200001010083",
                    Phone = "13912340083",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "GYN",
                    NurseRank = NurseRank.TeamLeader.ToString()
                },
                new Nurse
                {
                    Id = "N084",
                    EmployeeNumber = "nurse084",
                    PasswordHash = defaultHashedPassword,
                    Name = "温护士",
                    IdCard = "110100200001010084",
                    Phone = "13912340084",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "GYN",
                    NurseRank = NurseRank.RegularNurse.ToString()
                },
                // === 产科 OBS 病区护士 ===
                new Nurse
                {
                    Id = "N085",
                    EmployeeNumber = "nurse085",
                    PasswordHash = defaultHashedPassword,
                    Name = "芦护士",
                    IdCard = "110100200001010085",
                    Phone = "13912340085",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "OBS",
                    NurseRank = NurseRank.HeadNurse.ToString()
                },
                new Nurse
                {
                    Id = "N086",
                    EmployeeNumber = "nurse086",
                    PasswordHash = defaultHashedPassword,
                    Name = "季护士",
                    IdCard = "110100200001010086",
                    Phone = "13912340086",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "OBS",
                    NurseRank = NurseRank.TeamLeader.ToString()
                },
                new Nurse
                {
                    Id = "N087",
                    EmployeeNumber = "nurse087",
                    PasswordHash = defaultHashedPassword,
                    Name = "俞护士",
                    IdCard = "110100200001010087",
                    Phone = "13912340087",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "OBS",
                    NurseRank = NurseRank.RegularNurse.ToString()
                },
                new Nurse
                {
                    Id = "N088",
                    EmployeeNumber = "nurse088",
                    PasswordHash = defaultHashedPassword,
                    Name = "章护士",
                    IdCard = "110100200001010088",
                    Phone = "13912340088",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "OBS",
                    NurseRank = NurseRank.HeadNurse.ToString()
                },
                new Nurse
                {
                    Id = "N089",
                    EmployeeNumber = "nurse089",
                    PasswordHash = defaultHashedPassword,
                    Name = "鲁护士",
                    IdCard = "110100200001010089",
                    Phone = "13912340089",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "OBS",
                    NurseRank = NurseRank.TeamLeader.ToString()
                },
                new Nurse
                {
                    Id = "N090",
                    EmployeeNumber = "nurse090",
                    PasswordHash = defaultHashedPassword,
                    Name = "葛护士",
                    IdCard = "110100200001010090",
                    Phone = "13912340090",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "OBS",
                    NurseRank = NurseRank.RegularNurse.ToString()
                },
                // === 儿科 PED 病区护士 ===
                new Nurse
                {
                    Id = "N091",
                    EmployeeNumber = "nurse091",
                    PasswordHash = defaultHashedPassword,
                    Name = "伍护士",
                    IdCard = "110100200001010091",
                    Phone = "13912340091",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "PED",
                    NurseRank = NurseRank.HeadNurse.ToString()
                },
                new Nurse
                {
                    Id = "N092",
                    EmployeeNumber = "nurse092",
                    PasswordHash = defaultHashedPassword,
                    Name = "韦护士",
                    IdCard = "110100200001010092",
                    Phone = "13912340092",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "PED",
                    NurseRank = NurseRank.TeamLeader.ToString()
                },
                new Nurse
                {
                    Id = "N093",
                    EmployeeNumber = "nurse093",
                    PasswordHash = defaultHashedPassword,
                    Name = "申护士",
                    IdCard = "110100200001010093",
                    Phone = "13912340093",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "PED",
                    NurseRank = NurseRank.RegularNurse.ToString()
                },
                new Nurse
                {
                    Id = "N094",
                    EmployeeNumber = "nurse094",
                    PasswordHash = defaultHashedPassword,
                    Name = "管护士",
                    IdCard = "110100200001010094",
                    Phone = "13912340094",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "PED",
                    NurseRank = NurseRank.HeadNurse.ToString()
                },
                new Nurse
                {
                    Id = "N095",
                    EmployeeNumber = "nurse095",
                    PasswordHash = defaultHashedPassword,
                    Name = "卢护士",
                    IdCard = "110100200001010095",
                    Phone = "13912340095",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "PED",
                    NurseRank = NurseRank.TeamLeader.ToString()
                },
                new Nurse
                {
                    Id = "N096",
                    EmployeeNumber = "nurse096",
                    PasswordHash = defaultHashedPassword,
                    Name = "莫护士",
                    IdCard = "110100200001010096",
                    Phone = "13912340096",
                    RoleType = "Nurse",
                    IsActive = true,
                    DeptCode = "PED",
                    NurseRank = NurseRank.RegularNurse.ToString()
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
                // === 内科系统病区 ===
                new Ward { Id = "CARD-W01", DepartmentId = "CARD" },
                new Ward { Id = "CARD-W02", DepartmentId = "CARD" },
                new Ward { Id = "RESP-W01", DepartmentId = "RESP" },
                new Ward { Id = "RESP-W02", DepartmentId = "RESP" },
                new Ward { Id = "GAST-W01", DepartmentId = "GAST" },
                new Ward { Id = "GAST-W02", DepartmentId = "GAST" },
                new Ward { Id = "NEUR-W01", DepartmentId = "NEUR" },
                new Ward { Id = "NEUR-W02", DepartmentId = "NEUR" },
                new Ward { Id = "NEPH-W01", DepartmentId = "NEPH" },
                new Ward { Id = "NEPH-W02", DepartmentId = "NEPH" },
                new Ward { Id = "ENDO-W01", DepartmentId = "ENDO" },
                new Ward { Id = "ENDO-W02", DepartmentId = "ENDO" },
                new Ward { Id = "HEMA-W01", DepartmentId = "HEMA" },
                new Ward { Id = "HEMA-W02", DepartmentId = "HEMA" },
                // === 外科系统病区 ===
                new Ward { Id = "GEN_SUR-W01", DepartmentId = "GEN_SUR" },
                new Ward { Id = "GEN_SUR-W02", DepartmentId = "GEN_SUR" },
                new Ward { Id = "ORTH-W01", DepartmentId = "ORTH" },
                new Ward { Id = "ORTH-W02", DepartmentId = "ORTH" },
                new Ward { Id = "NSUR-W01", DepartmentId = "NSUR" },
                new Ward { Id = "NSUR-W02", DepartmentId = "NSUR" },
                new Ward { Id = "CT_SUR-W01", DepartmentId = "CT_SUR" },
                new Ward { Id = "CT_SUR-W02", DepartmentId = "CT_SUR" },
                new Ward { Id = "UROL-W01", DepartmentId = "UROL" },
                new Ward { Id = "UROL-W02", DepartmentId = "UROL" },
                new Ward { Id = "BURN-W01", DepartmentId = "BURN" },
                new Ward { Id = "BURN-W02", DepartmentId = "BURN" },
                // === 妇产科系统病区 ===
                new Ward { Id = "GYN-W01", DepartmentId = "GYN" },
                new Ward { Id = "GYN-W02", DepartmentId = "GYN" },
                new Ward { Id = "OBS-W01", DepartmentId = "OBS" },
                new Ward { Id = "OBS-W02", DepartmentId = "OBS" },
                // === 儿科病区 ===
                new Ward { Id = "PED-W01", DepartmentId = "PED" },
                new Ward { Id = "PED-W02", DepartmentId = "PED" }
            };
            context.Wards.AddRange(wards);
            context.SaveChanges();

            var beds = new Bed[]
            {

                // === 心血管内科 CARD 病区床位 ===
                new Bed { Id = "CARD-W01-001", WardId = "CARD-W01", Status = "空闲" },
                new Bed { Id = "CARD-W01-002", WardId = "CARD-W01", Status = "空闲" },
                new Bed { Id = "CARD-W01-003", WardId = "CARD-W01", Status = "空闲" },
                new Bed { Id = "CARD-W02-001", WardId = "CARD-W02", Status = "空闲" },
                new Bed { Id = "CARD-W02-002", WardId = "CARD-W02", Status = "空闲" },
                new Bed { Id = "CARD-W02-003", WardId = "CARD-W02", Status = "空闲" },
                // === 呼吸内科 RESP 病区床位 ===
                new Bed { Id = "RESP-W01-001", WardId = "RESP-W01", Status = "空闲" },
                new Bed { Id = "RESP-W01-002", WardId = "RESP-W01", Status = "空闲" },
                new Bed { Id = "RESP-W01-003", WardId = "RESP-W01", Status = "空闲" },
                new Bed { Id = "RESP-W02-001", WardId = "RESP-W02", Status = "空闲" },
                new Bed { Id = "RESP-W02-002", WardId = "RESP-W02", Status = "空闲" },
                new Bed { Id = "RESP-W02-003", WardId = "RESP-W02", Status = "空闲" },
                // === 消化内科 GAST 病区床位 ===
                new Bed { Id = "GAST-W01-001", WardId = "GAST-W01", Status = "空闲" },
                new Bed { Id = "GAST-W01-002", WardId = "GAST-W01", Status = "空闲" },
                new Bed { Id = "GAST-W01-003", WardId = "GAST-W01", Status = "空闲" },
                new Bed { Id = "GAST-W02-001", WardId = "GAST-W02", Status = "空闲" },
                new Bed { Id = "GAST-W02-002", WardId = "GAST-W02", Status = "空闲" },
                new Bed { Id = "GAST-W02-003", WardId = "GAST-W02", Status = "空闲" },
                // === 神经内科 NEUR 病区床位 ===
                new Bed { Id = "NEUR-W01-001", WardId = "NEUR-W01", Status = "空闲" },
                new Bed { Id = "NEUR-W01-002", WardId = "NEUR-W01", Status = "空闲" },
                new Bed { Id = "NEUR-W01-003", WardId = "NEUR-W01", Status = "空闲" },
                new Bed { Id = "NEUR-W02-001", WardId = "NEUR-W02", Status = "空闲" },
                new Bed { Id = "NEUR-W02-002", WardId = "NEUR-W02", Status = "空闲" },
                new Bed { Id = "NEUR-W02-003", WardId = "NEUR-W02", Status = "空闲" },
                // === 肾内科 NEPH 病区床位 ===
                new Bed { Id = "NEPH-W01-001", WardId = "NEPH-W01", Status = "空闲" },
                new Bed { Id = "NEPH-W01-002", WardId = "NEPH-W01", Status = "空闲" },
                new Bed { Id = "NEPH-W01-003", WardId = "NEPH-W01", Status = "空闲" },
                new Bed { Id = "NEPH-W02-001", WardId = "NEPH-W02", Status = "空闲" },
                new Bed { Id = "NEPH-W02-002", WardId = "NEPH-W02", Status = "空闲" },
                new Bed { Id = "NEPH-W02-003", WardId = "NEPH-W02", Status = "空闲" },
                // === 内分泌科 ENDO 病区床位 ===
                new Bed { Id = "ENDO-W01-001", WardId = "ENDO-W01", Status = "空闲" },
                new Bed { Id = "ENDO-W01-002", WardId = "ENDO-W01", Status = "空闲" },
                new Bed { Id = "ENDO-W01-003", WardId = "ENDO-W01", Status = "空闲" },
                new Bed { Id = "ENDO-W02-001", WardId = "ENDO-W02", Status = "空闲" },
                new Bed { Id = "ENDO-W02-002", WardId = "ENDO-W02", Status = "空闲" },
                new Bed { Id = "ENDO-W02-003", WardId = "ENDO-W02", Status = "空闲" },
                // === 血液内科 HEMA 病区床位 ===
                new Bed { Id = "HEMA-W01-001", WardId = "HEMA-W01", Status = "空闲" },
                new Bed { Id = "HEMA-W01-002", WardId = "HEMA-W01", Status = "空闲" },
                new Bed { Id = "HEMA-W01-003", WardId = "HEMA-W01", Status = "空闲" },
                new Bed { Id = "HEMA-W02-001", WardId = "HEMA-W02", Status = "空闲" },
                new Bed { Id = "HEMA-W02-002", WardId = "HEMA-W02", Status = "空闲" },
                new Bed { Id = "HEMA-W02-003", WardId = "HEMA-W02", Status = "空闲" },
                // === 普通外科 GEN_SUR 病区床位 ===
                new Bed { Id = "GEN_SUR-W01-001", WardId = "GEN_SUR-W01", Status = "空闲" },
                new Bed { Id = "GEN_SUR-W01-002", WardId = "GEN_SUR-W01", Status = "空闲" },
                new Bed { Id = "GEN_SUR-W01-003", WardId = "GEN_SUR-W01", Status = "空闲" },
                new Bed { Id = "GEN_SUR-W02-001", WardId = "GEN_SUR-W02", Status = "空闲" },
                new Bed { Id = "GEN_SUR-W02-002", WardId = "GEN_SUR-W02", Status = "空闲" },
                new Bed { Id = "GEN_SUR-W02-003", WardId = "GEN_SUR-W02", Status = "空闲" },
                // === 骨科 ORTH 病区床位 ===
                new Bed { Id = "ORTH-W01-001", WardId = "ORTH-W01", Status = "空闲" },
                new Bed { Id = "ORTH-W01-002", WardId = "ORTH-W01", Status = "空闲" },
                new Bed { Id = "ORTH-W01-003", WardId = "ORTH-W01", Status = "空闲" },
                new Bed { Id = "ORTH-W02-001", WardId = "ORTH-W02", Status = "空闲" },
                new Bed { Id = "ORTH-W02-002", WardId = "ORTH-W02", Status = "空闲" },
                new Bed { Id = "ORTH-W02-003", WardId = "ORTH-W02", Status = "空闲" },
                // === 神经外科 NSUR 病区床位 ===
                new Bed { Id = "NSUR-W01-001", WardId = "NSUR-W01", Status = "空闲" },
                new Bed { Id = "NSUR-W01-002", WardId = "NSUR-W01", Status = "空闲" },
                new Bed { Id = "NSUR-W01-003", WardId = "NSUR-W01", Status = "空闲" },
                new Bed { Id = "NSUR-W02-001", WardId = "NSUR-W02", Status = "空闲" },
                new Bed { Id = "NSUR-W02-002", WardId = "NSUR-W02", Status = "空闲" },
                new Bed { Id = "NSUR-W02-003", WardId = "NSUR-W02", Status = "空闲" },
                // === 心胸外科 CT_SUR 病区床位 ===
                new Bed { Id = "CT_SUR-W01-001", WardId = "CT_SUR-W01", Status = "空闲" },
                new Bed { Id = "CT_SUR-W01-002", WardId = "CT_SUR-W01", Status = "空闲" },
                new Bed { Id = "CT_SUR-W01-003", WardId = "CT_SUR-W01", Status = "空闲" },
                new Bed { Id = "CT_SUR-W02-001", WardId = "CT_SUR-W02", Status = "空闲" },
                new Bed { Id = "CT_SUR-W02-002", WardId = "CT_SUR-W02", Status = "空闲" },
                new Bed { Id = "CT_SUR-W02-003", WardId = "CT_SUR-W02", Status = "空闲" },
                // === 泌尿外科 UROL 病区床位 ===
                new Bed { Id = "UROL-W01-001", WardId = "UROL-W01", Status = "空闲" },
                new Bed { Id = "UROL-W01-002", WardId = "UROL-W01", Status = "空闲" },
                new Bed { Id = "UROL-W01-003", WardId = "UROL-W01", Status = "空闲" },
                new Bed { Id = "UROL-W02-001", WardId = "UROL-W02", Status = "空闲" },
                new Bed { Id = "UROL-W02-002", WardId = "UROL-W02", Status = "空闲" },
                new Bed { Id = "UROL-W02-003", WardId = "UROL-W02", Status = "空闲" },
                // === 烧伤科 BURN 病区床位 ===
                new Bed { Id = "BURN-W01-001", WardId = "BURN-W01", Status = "空闲" },
                new Bed { Id = "BURN-W01-002", WardId = "BURN-W01", Status = "空闲" },
                new Bed { Id = "BURN-W01-003", WardId = "BURN-W01", Status = "空闲" },
                new Bed { Id = "BURN-W02-001", WardId = "BURN-W02", Status = "空闲" },
                new Bed { Id = "BURN-W02-002", WardId = "BURN-W02", Status = "空闲" },
                new Bed { Id = "BURN-W02-003", WardId = "BURN-W02", Status = "空闲" },
                // === 妇科 GYN 病区床位 ===
                new Bed { Id = "GYN-W01-001", WardId = "GYN-W01", Status = "空闲" },
                new Bed { Id = "GYN-W01-002", WardId = "GYN-W01", Status = "空闲" },
                new Bed { Id = "GYN-W01-003", WardId = "GYN-W01", Status = "空闲" },
                new Bed { Id = "GYN-W02-001", WardId = "GYN-W02", Status = "空闲" },
                new Bed { Id = "GYN-W02-002", WardId = "GYN-W02", Status = "空闲" },
                new Bed { Id = "GYN-W02-003", WardId = "GYN-W02", Status = "空闲" },
                // === 产科 OBS 病区床位 ===
                new Bed { Id = "OBS-W01-001", WardId = "OBS-W01", Status = "空闲" },
                new Bed { Id = "OBS-W01-002", WardId = "OBS-W01", Status = "空闲" },
                new Bed { Id = "OBS-W01-003", WardId = "OBS-W01", Status = "空闲" },
                new Bed { Id = "OBS-W02-001", WardId = "OBS-W02", Status = "空闲" },
                new Bed { Id = "OBS-W02-002", WardId = "OBS-W02", Status = "空闲" },
                new Bed { Id = "OBS-W02-003", WardId = "OBS-W02", Status = "空闲" },
                // === 儿科 PED 病区床位 ===
                new Bed { Id = "PED-W01-001", WardId = "PED-W01", Status = "占用" },
                new Bed { Id = "PED-W01-002", WardId = "PED-W01", Status = "空闲" },
                new Bed { Id = "PED-W01-003", WardId = "PED-W01", Status = "空闲" },
                new Bed { Id = "PED-W02-001", WardId = "PED-W02", Status = "空闲" },
                new Bed { Id = "PED-W02-002", WardId = "PED-W02", Status = "空闲" },
                new Bed { Id = "PED-W02-003", WardId = "PED-W02", Status = "空闲" }
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
                    NursingGrade = NursingGrade.Grade2, BedId = "RESP-W01-001", AttendingDoctorId = "D002",
                    OutpatientDiagnosis = "哮喘",
                    ScheduledAdmissionTime = UtcDate(2024, 12, 1, 8, 0),
                    ActualAdmissionTime = UtcDate(2024, 12, 1, 9, 30)
                },
                
                // ==================== 已入院患者 002~010 ====================
                // 普通外科患者 002~006
                new Patient
                {
                    Id = "P002", Name = "李四", Gender = "男", IdCard = "110100198802150002",
                    DateOfBirth = new DateTime(1988, 2, 15, 0, 0, 0, DateTimeKind.Utc),
                    Age = 36, Height = 172.0f, Weight = 68.0f, Status = PatientStatus.Hospitalized, PhoneNumber = "13800138002",
                    NursingGrade = NursingGrade.Grade2, BedId = "GEN_SUR-W01-002", AttendingDoctorId = "D008",
                    OutpatientDiagnosis = "急性阑尾炎",
                    ScheduledAdmissionTime = UtcDate(2024, 12, 2, 9, 0),
                    ActualAdmissionTime = UtcDate(2024, 12, 2, 10, 15)
                },
                new Patient
                {
                    Id = "P003", Name = "王五", Gender = "女", IdCard = "110100199503200003",
                    DateOfBirth = new DateTime(1995, 3, 20, 0, 0, 0, DateTimeKind.Utc),
                    Age = 29, Height = 165.0f, Weight = 58.0f, Status = PatientStatus.Hospitalized, PhoneNumber = "13800138003",
                    NursingGrade = NursingGrade.Grade1, BedId = "GEN_SUR-W01-003", AttendingDoctorId = "D008",
                    OutpatientDiagnosis = "胆囊结石",
                    ScheduledAdmissionTime = UtcDate(2024, 12, 3, 8, 30),
                    ActualAdmissionTime = UtcDate(2024, 12, 3, 9, 45)
                },
                new Patient
                {
                    Id = "P004", Name = "赵六", Gender = "男", IdCard = "110100198706100004",
                    DateOfBirth = new DateTime(1987, 6, 10, 0, 0, 0, DateTimeKind.Utc),
                    Age = 37, Height = 178.0f, Weight = 75.0f, Status = PatientStatus.Hospitalized, PhoneNumber = "13800138004",
                    NursingGrade = NursingGrade.Grade2, BedId = "GEN_SUR-W01-004", AttendingDoctorId = "D008",
                    OutpatientDiagnosis = "腹股沟疝",
                    ScheduledAdmissionTime = UtcDate(2024, 12, 4, 10, 0),
                    ActualAdmissionTime = UtcDate(2024, 12, 4, 11, 20)
                },
                new Patient
                {
                    Id = "P005", Name = "孙七", Gender = "女", IdCard = "110100199108250005",
                    DateOfBirth = new DateTime(1991, 8, 25, 0, 0, 0, DateTimeKind.Utc),
                    Age = 33, Height = 160.0f, Weight = 55.0f, Status = PatientStatus.Hospitalized, PhoneNumber = "13800138005",
                    NursingGrade = NursingGrade.Grade2, BedId = "GEN_SUR-W01-005", AttendingDoctorId = "D008",
                    OutpatientDiagnosis = "甲状腺结节",
                    ScheduledAdmissionTime = UtcDate(2024, 12, 5, 9, 0),
                    ActualAdmissionTime = UtcDate(2024, 12, 5, 10, 30)
                },
                new Patient
                {
                    Id = "P006", Name = "周八", Gender = "女", IdCard = "110100198912050006",
                    DateOfBirth = new DateTime(1989, 12, 5, 0, 0, 0, DateTimeKind.Utc),
                    Age = 35, Height = 165.0f, Weight = 60.0f, Status = PatientStatus.Hospitalized, PhoneNumber = "13800138006",
                    NursingGrade = NursingGrade.Grade3, BedId = "GEN_SUR-W01-006", AttendingDoctorId = "D008",
                    OutpatientDiagnosis = "乳腺纤维瘤",
                    ScheduledAdmissionTime = UtcDate(2024, 12, 6, 8, 0),
                    ActualAdmissionTime = UtcDate(2024, 12, 6, 9, 15)
                },
                
                // 其他科室患者 007~010
                new Patient
                {
                    Id = "P007", Name = "吴九", Gender = "男", IdCard = "110100199204120007",
                    DateOfBirth = new DateTime(1992, 4, 12, 0, 0, 0, DateTimeKind.Utc),
                    Age = 32, Height = 180.0f, Weight = 78.0f, Status = PatientStatus.Hospitalized, PhoneNumber = "13800138007",
                    NursingGrade = NursingGrade.Grade2, BedId = "ORTH-W01-001", AttendingDoctorId = "D009",
                    OutpatientDiagnosis = "股骨骨折",
                    ScheduledAdmissionTime = UtcDate(2024, 12, 7, 8, 30),
                    ActualAdmissionTime = UtcDate(2024, 12, 7, 10, 0)
                },
                new Patient
                {
                    Id = "P008", Name = "徐十", Gender = "女", IdCard = "110100198511180008",
                    DateOfBirth = new DateTime(1985, 11, 18, 0, 0, 0, DateTimeKind.Utc),
                    Age = 39, Height = 168.0f, Weight = 62.0f, Status = PatientStatus.Hospitalized, PhoneNumber = "13800138008",
                    NursingGrade = NursingGrade.Grade1, BedId = "NSUR-W01-001", AttendingDoctorId = "D010",
                    OutpatientDiagnosis = "脑膜瘤",
                    ScheduledAdmissionTime = UtcDate(2024, 12, 8, 9, 0),
                    ActualAdmissionTime = UtcDate(2024, 12, 8, 10, 45)
                },
                new Patient
                {
                    Id = "P009", Name = "马十一", Gender = "男", IdCard = "110100199709030009",
                    DateOfBirth = new DateTime(1997, 9, 3, 0, 0, 0, DateTimeKind.Utc),
                    Age = 27, Height = 176.0f, Weight = 70.0f, Status = PatientStatus.Hospitalized, PhoneNumber = "13800138009",
                    NursingGrade = NursingGrade.Special, BedId = "CT_SUR-W01-001", AttendingDoctorId = "D011",
                    OutpatientDiagnosis = "房间隔缺损",
                    ScheduledAdmissionTime = UtcDate(2024, 12, 9, 8, 0),
                    ActualAdmissionTime = UtcDate(2024, 12, 9, 9, 30)
                },
                new Patient
                {
                    Id = "P010", Name = "朱十二", Gender = "男", IdCard = "110100198403280010",
                    DateOfBirth = new DateTime(1984, 3, 28, 0, 0, 0, DateTimeKind.Utc),
                    Age = 40, Height = 174.0f, Weight = 73.0f, Status = PatientStatus.Hospitalized, PhoneNumber = "13800138010",
                    NursingGrade = NursingGrade.Grade2, BedId = "UROL-W01-001", AttendingDoctorId = "D012",
                    OutpatientDiagnosis = "肾结石",
                    ScheduledAdmissionTime = UtcDate(2024, 12, 10, 10, 0),
                    ActualAdmissionTime = UtcDate(2024, 12, 10, 11, 15)
                },
                
                // ==================== 待入院患者（用于测试入院功能） ====================

                new Patient
                {
                    Id = "P011", Name = "郑十一", Gender = "男", IdCard = "110100199203150011",
                    DateOfBirth = new DateTime(1992, 3, 15, 0, 0, 0, DateTimeKind.Utc),
                    Age = 32, Height = 178.0f, Weight = 75.0f, Status = PatientStatus.PendingAdmission, PhoneNumber = "13800138011",
                    NursingGrade = NursingGrade.Grade2, BedId = null, AttendingDoctorId = "D003",  // BedId 改为 null
                    OutpatientDiagnosis = "慢性胃炎，胃溃疡",
                    ScheduledAdmissionTime = UtcDate(2024, 12, 20, 9, 0),
                    ActualAdmissionTime = null
                },
                
                // 普通外科待入院患者 012~013
                new Patient
                {
                    Id = "P012", Name = "钱十二", Gender = "男", IdCard = "110100199006220012",
                    DateOfBirth = new DateTime(1990, 6, 22, 0, 0, 0, DateTimeKind.Utc),
                    Age = 34, Height = 173.0f, Weight = 69.0f, Status = PatientStatus.PendingAdmission, PhoneNumber = "13800138012",
                    NursingGrade = NursingGrade.Grade2, BedId = null, AttendingDoctorId = "D008",
                    OutpatientDiagnosis = "急性胆囊炎",
                    ScheduledAdmissionTime = UtcDate(2024, 12, 21, 9, 0),
                    ActualAdmissionTime = null
                },
                new Patient
                {
                    Id = "P013", Name = "孙十三", Gender = "女", IdCard = "110100199411080013",
                    DateOfBirth = new DateTime(1994, 11, 8, 0, 0, 0, DateTimeKind.Utc),
                    Age = 30, Height = 162.0f, Weight = 56.0f, Status = PatientStatus.PendingAdmission, PhoneNumber = "13800138013",
                    NursingGrade = NursingGrade.Grade2, BedId = null, AttendingDoctorId = "D008",
                    OutpatientDiagnosis = "腹股沟疝",
                    ScheduledAdmissionTime = UtcDate(2024, 12, 22, 8, 30),
                    ActualAdmissionTime = null
                },
                
                // 其他科室待入院患者 014~020
                new Patient
                {
                    Id = "P014", Name = "李十四", Gender = "男", IdCard = "110100198709150014",
                    DateOfBirth = new DateTime(1987, 9, 15, 0, 0, 0, DateTimeKind.Utc),
                    Age = 37, Height = 177.0f, Weight = 76.0f, Status = PatientStatus.PendingAdmission, PhoneNumber = "13800138014",
                    NursingGrade = NursingGrade.Grade1, BedId = null, AttendingDoctorId = "D001",
                    OutpatientDiagnosis = "冠心病，心绞痛",
                    ScheduledAdmissionTime = UtcDate(2024, 12, 23, 9, 0),
                    ActualAdmissionTime = null
                },
                new Patient
                {
                    Id = "P015", Name = "王十五", Gender = "女", IdCard = "110100199512200015",
                    DateOfBirth = new DateTime(1995, 12, 20, 0, 0, 0, DateTimeKind.Utc),
                    Age = 29, Height = 164.0f, Weight = 59.0f, Status = PatientStatus.PendingAdmission, PhoneNumber = "13800138015",
                    NursingGrade = NursingGrade.Grade2, BedId = null, AttendingDoctorId = "D003",
                    OutpatientDiagnosis = "胃息肉",
                    ScheduledAdmissionTime = UtcDate(2024, 12, 24, 8, 0),
                    ActualAdmissionTime = null
                },
                new Patient
                {
                    Id = "P016", Name = "赵十六", Gender = "男", IdCard = "110100198802280016",
                    DateOfBirth = new DateTime(1988, 2, 28, 0, 0, 0, DateTimeKind.Utc),
                    Age = 36, Height = 175.0f, Weight = 71.0f, Status = PatientStatus.PendingAdmission, PhoneNumber = "13800138016",
                    NursingGrade = NursingGrade.Grade2, BedId = null, AttendingDoctorId = "D004",
                    OutpatientDiagnosis = "偏头痛",
                    ScheduledAdmissionTime = UtcDate(2024, 12, 25, 9, 30),
                    ActualAdmissionTime = null
                },
                new Patient
                {
                    Id = "P017", Name = "周十七", Gender = "男", IdCard = "110100199308100017",
                    DateOfBirth = new DateTime(1993, 8, 10, 0, 0, 0, DateTimeKind.Utc),
                    Age = 31, Height = 179.0f, Weight = 77.0f, Status = PatientStatus.PendingAdmission, PhoneNumber = "13800138017",
                    NursingGrade = NursingGrade.Grade2, BedId = null, AttendingDoctorId = "D009",
                    OutpatientDiagnosis = "腰椎间盘突出",
                    ScheduledAdmissionTime = UtcDate(2024, 12, 26, 8, 0),
                    ActualAdmissionTime = null
                },
                new Patient
                {
                    Id = "P018", Name = "吴十八", Gender = "女", IdCard = "110100199007250018",
                    DateOfBirth = new DateTime(1990, 7, 25, 0, 0, 0, DateTimeKind.Utc),
                    Age = 34, Height = 166.0f, Weight = 60.0f, Status = PatientStatus.PendingAdmission, PhoneNumber = "13800138018",
                    NursingGrade = NursingGrade.Grade2, BedId = null, AttendingDoctorId = "D014",
                    OutpatientDiagnosis = "子宫肌瘤",
                    ScheduledAdmissionTime = UtcDate(2024, 12, 27, 9, 0),
                    ActualAdmissionTime = null
                },
                new Patient
                {
                    Id = "P019", Name = "徐十九", Gender = "女", IdCard = "110100199205180019",
                    DateOfBirth = new DateTime(1992, 5, 18, 0, 0, 0, DateTimeKind.Utc),
                    Age = 32, Height = 163.0f, Weight = 58.0f, Status = PatientStatus.PendingAdmission, PhoneNumber = "13800138019",
                    NursingGrade = NursingGrade.Grade2, BedId = null, AttendingDoctorId = "D015",
                    OutpatientDiagnosis = "妊娠期高血压",
                    ScheduledAdmissionTime = UtcDate(2024, 12, 28, 8, 30),
                    ActualAdmissionTime = null
                },
                new Patient
                {
                    Id = "P020", Name = "马二十", Gender = "男", IdCard = "110100201006050020",
                    DateOfBirth = new DateTime(2010, 6, 5, 0, 0, 0, DateTimeKind.Utc),
                    Age = 14, Height = 155.0f, Weight = 45.0f, Status = PatientStatus.PendingAdmission, PhoneNumber = "13800138020",
                    NursingGrade = NursingGrade.Grade2, BedId = null, AttendingDoctorId = "D016",
                    OutpatientDiagnosis = "小儿肺炎",
                    ScheduledAdmissionTime = UtcDate(2024, 12, 29, 9, 0),
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
                // === 心血管内科 CARD 病区排班 ===
                new NurseRoster { StaffId = "N001", WardId = "CARD-W01", ShiftId = "DAY", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N002", WardId = "CARD-W01", ShiftId = "EVENING", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N003", WardId = "CARD-W01", ShiftId = "NIGHT", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N004", WardId = "CARD-W02", ShiftId = "DAY", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N005", WardId = "CARD-W02", ShiftId = "EVENING", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N006", WardId = "CARD-W02", ShiftId = "NIGHT", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },

                // === 呼吸内科 RESP 病区排班 ===
                new NurseRoster { StaffId = "N007", WardId = "RESP-W01", ShiftId = "DAY", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N008", WardId = "RESP-W01", ShiftId = "EVENING", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N009", WardId = "RESP-W01", ShiftId = "NIGHT", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N010", WardId = "RESP-W02", ShiftId = "DAY", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N011", WardId = "RESP-W02", ShiftId = "EVENING", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N012", WardId = "RESP-W02", ShiftId = "NIGHT", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },

                // === 消化内科 GAST 病区排班 ===
                new NurseRoster { StaffId = "N013", WardId = "GAST-W01", ShiftId = "DAY", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N014", WardId = "GAST-W01", ShiftId = "EVENING", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N015", WardId = "GAST-W01", ShiftId = "NIGHT", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N016", WardId = "GAST-W02", ShiftId = "DAY", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N017", WardId = "GAST-W02", ShiftId = "EVENING", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N018", WardId = "GAST-W02", ShiftId = "NIGHT", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },

                // === 神经内科 NEUR 病区排班 ===
                new NurseRoster { StaffId = "N019", WardId = "NEUR-W01", ShiftId = "DAY", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N020", WardId = "NEUR-W01", ShiftId = "EVENING", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N021", WardId = "NEUR-W01", ShiftId = "NIGHT", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N022", WardId = "NEUR-W02", ShiftId = "DAY", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N023", WardId = "NEUR-W02", ShiftId = "EVENING", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N024", WardId = "NEUR-W02", ShiftId = "NIGHT", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },

                // === 肾内科 NEPH 病区排班 ===
                new NurseRoster { StaffId = "N025", WardId = "NEPH-W01", ShiftId = "DAY", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N026", WardId = "NEPH-W01", ShiftId = "EVENING", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N027", WardId = "NEPH-W01", ShiftId = "NIGHT", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N028", WardId = "NEPH-W02", ShiftId = "DAY", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N029", WardId = "NEPH-W02", ShiftId = "EVENING", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N030", WardId = "NEPH-W02", ShiftId = "NIGHT", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },

                // === 内分泌科 ENDO 病区排班 ===
                new NurseRoster { StaffId = "N031", WardId = "ENDO-W01", ShiftId = "DAY", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N032", WardId = "ENDO-W01", ShiftId = "EVENING", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N033", WardId = "ENDO-W01", ShiftId = "NIGHT", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N034", WardId = "ENDO-W02", ShiftId = "DAY", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N035", WardId = "ENDO-W02", ShiftId = "EVENING", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N036", WardId = "ENDO-W02", ShiftId = "NIGHT", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },

                // === 血液内科 HEMA 病区排班 ===
                new NurseRoster { StaffId = "N037", WardId = "HEMA-W01", ShiftId = "DAY", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N038", WardId = "HEMA-W01", ShiftId = "EVENING", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N039", WardId = "HEMA-W01", ShiftId = "NIGHT", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N040", WardId = "HEMA-W02", ShiftId = "DAY", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N041", WardId = "HEMA-W02", ShiftId = "EVENING", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N042", WardId = "HEMA-W02", ShiftId = "NIGHT", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },

                // === 普通外科 GEN_SUR 病区排班 ===
                new NurseRoster { StaffId = "N043", WardId = "GEN_SUR-W01", ShiftId = "DAY", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N044", WardId = "GEN_SUR-W01", ShiftId = "EVENING", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N045", WardId = "GEN_SUR-W01", ShiftId = "NIGHT", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N046", WardId = "GEN_SUR-W02", ShiftId = "DAY", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N047", WardId = "GEN_SUR-W02", ShiftId = "EVENING", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N048", WardId = "GEN_SUR-W02", ShiftId = "NIGHT", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },

                // === 骨科 ORTH 病区排班 ===
                new NurseRoster { StaffId = "N049", WardId = "ORTH-W01", ShiftId = "DAY", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N050", WardId = "ORTH-W01", ShiftId = "EVENING", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N051", WardId = "ORTH-W01", ShiftId = "NIGHT", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N052", WardId = "ORTH-W02", ShiftId = "DAY", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N053", WardId = "ORTH-W02", ShiftId = "EVENING", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N054", WardId = "ORTH-W02", ShiftId = "NIGHT", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },

                // === 神经外科 NSUR 病区排班 ===
                new NurseRoster { StaffId = "N055", WardId = "NSUR-W01", ShiftId = "DAY", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N056", WardId = "NSUR-W01", ShiftId = "EVENING", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N057", WardId = "NSUR-W01", ShiftId = "NIGHT", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N058", WardId = "NSUR-W02", ShiftId = "DAY", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N059", WardId = "NSUR-W02", ShiftId = "EVENING", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N060", WardId = "NSUR-W02", ShiftId = "NIGHT", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },

                // === 心胸外科 CT_SUR 病区排班 ===
                new NurseRoster { StaffId = "N061", WardId = "CT_SUR-W01", ShiftId = "DAY", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N062", WardId = "CT_SUR-W01", ShiftId = "EVENING", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N063", WardId = "CT_SUR-W01", ShiftId = "NIGHT", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N064", WardId = "CT_SUR-W02", ShiftId = "DAY", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N065", WardId = "CT_SUR-W02", ShiftId = "EVENING", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N066", WardId = "CT_SUR-W02", ShiftId = "NIGHT", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },

                // === 泌尿外科 UROL 病区排班 ===
                new NurseRoster { StaffId = "N067", WardId = "UROL-W01", ShiftId = "DAY", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N068", WardId = "UROL-W01", ShiftId = "EVENING", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N069", WardId = "UROL-W01", ShiftId = "NIGHT", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N070", WardId = "UROL-W02", ShiftId = "DAY", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N071", WardId = "UROL-W02", ShiftId = "EVENING", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N072", WardId = "UROL-W02", ShiftId = "NIGHT", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },

                // === 烧伤科 BURN 病区排班 ===
                new NurseRoster { StaffId = "N073", WardId = "BURN-W01", ShiftId = "DAY", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N074", WardId = "BURN-W01", ShiftId = "EVENING", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N075", WardId = "BURN-W01", ShiftId = "NIGHT", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N076", WardId = "BURN-W02", ShiftId = "DAY", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N077", WardId = "BURN-W02", ShiftId = "EVENING", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N078", WardId = "BURN-W02", ShiftId = "NIGHT", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },

                // === 妇科 GYN 病区排班 ===
                new NurseRoster { StaffId = "N079", WardId = "GYN-W01", ShiftId = "DAY", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N080", WardId = "GYN-W01", ShiftId = "EVENING", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N081", WardId = "GYN-W01", ShiftId = "NIGHT", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N082", WardId = "GYN-W02", ShiftId = "DAY", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N083", WardId = "GYN-W02", ShiftId = "EVENING", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N084", WardId = "GYN-W02", ShiftId = "NIGHT", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },

                // === 产科 OBS 病区排班 ===
                new NurseRoster { StaffId = "N085", WardId = "OBS-W01", ShiftId = "DAY", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N086", WardId = "OBS-W01", ShiftId = "EVENING", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N087", WardId = "OBS-W01", ShiftId = "NIGHT", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N088", WardId = "OBS-W02", ShiftId = "DAY", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N089", WardId = "OBS-W02", ShiftId = "EVENING", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N090", WardId = "OBS-W02", ShiftId = "NIGHT", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },

                // === 儿科 PED 病区排班 ===
                new NurseRoster { StaffId = "N091", WardId = "PED-W01", ShiftId = "DAY", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N092", WardId = "PED-W01", ShiftId = "EVENING", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N093", WardId = "PED-W01", ShiftId = "NIGHT", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N094", WardId = "PED-W02", ShiftId = "DAY", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N095", WardId = "PED-W02", ShiftId = "EVENING", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" },
                new NurseRoster { StaffId = "N096", WardId = "PED-W02", ShiftId = "NIGHT", WorkDate = DateOnly.FromDateTime(todayUtc), Status = "Scheduled" }
            };
            context.NurseRosters.AddRange(nurseRosters);
            context.SaveChanges(); // 保存排班数据
            
        }
    }
}
