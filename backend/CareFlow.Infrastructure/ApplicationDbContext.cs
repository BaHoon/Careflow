using Microsoft.EntityFrameworkCore;
using CareFlow.Core.Models;
using CareFlow.Core.Models.Organization;
using CareFlow.Core.Models.Space;
using CareFlow.Core.Models.Medical;
using CareFlow.Application.Interfaces;

using CareFlow.Core.Models.Nursing;
namespace CareFlow.Infrastructure
{
    public class ApplicationDbContext : DbContext, ICareFlowDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        #region 1. Organization (人员与组织)
        public DbSet<Department> Departments { get; set; }
        public DbSet<Staff> Staffs { get; set; }
        public DbSet<Doctor> Doctors { get; set; }
        public DbSet<Nurse> Nurses { get; set; }
        #endregion

        #region 2. Space (空间管理)
        public DbSet<Ward> Wards { get; set; }
        public DbSet<Bed> Beds { get; set; }
        #endregion

        #region 3. Patient (患者信息)
        public DbSet<Patient> Patients { get; set; }
        #endregion

        #region 4. Medical (医嘱体系)
        // 基类
        public DbSet<MedicalOrder> MedicalOrders { get; set; }
        
        // 四种具体的医嘱类型
        public DbSet<MedicationOrder> MedicationOrders { get; set; } // 药品
        public DbSet<MedicationOrderItem> MedicationOrderItems { get; set; } // 药品医嘱明细
        public DbSet<InspectionOrder> InspectionOrders { get; set; } // 检查
        public DbSet<SurgicalOrder> SurgicalOrders { get; set; }     // 手术
        public DbSet<OperationOrder> OperationOrders { get; set; }   // 操作 (护理/治疗)

        // 医嘱附属表
        public DbSet<InspectionReport> InspectionReports { get; set; } // 检查报告
        public DbSet<HospitalTimeSlot> HospitalTimeSlots { get; set; } // 时段字典
        public DbSet<Drug> Drugs { get; set; } // 药品字典
        public DbSet<MedicalOrderStatusHistory> MedicalOrderStatusHistories { get; set; } // 医嘱状态变更历史
        #endregion

        //护理执行
        public DbSet<VitalSignsRecord> VitalSignsRecords { get; set; } // 生命体征
        public DbSet<NursingCareNote> NursingCareNotes { get; set; }   // 护理记录单
        public DbSet<ExecutionTask> ExecutionTasks { get; set; }       // 执行单(扫码用)
        public DbSet<ShiftType> ShiftTypes { get; set; }               // 班次定义
        public DbSet<NurseRoster> NurseRosters { get; set; }           // 护士排班
        public DbSet<NursingTask> NursingTasks { get; set; }
        public DbSet<MedicationReturnRequest> MedicationReturnRequests { get; set; } // 退药申请记录
        public DbSet<NursingRecordSupplement> NursingRecordSupplements { get; set; } // 护理记录补充说明

        #region 5. Barcode (条形码系统)
        public DbSet<BarcodeIndex> BarcodeIndexes { get; set; }         // 条形码索引表
        #endregion

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // --- 配置继承关系 (Table-Per-Type, TPT) ---

            // Staff 继承体系：数据库里会有 Staffs, Doctors, Nurses 三张表
            // Doctors 和 Nurses 表只存自己特有的字段，公共字段在 Staffs 表
            modelBuilder.Entity<Staff>().UseTptMappingStrategy();
            modelBuilder.Entity<Doctor>().ToTable("Doctors");
            modelBuilder.Entity<Nurse>().ToTable("Nurses");

            // MedicalOrder 继承体系：数据库里会有 MedicalOrders 以及 4 个子表
            modelBuilder.Entity<MedicalOrder>().UseTptMappingStrategy();
            modelBuilder.Entity<MedicationOrder>().ToTable("MedicationOrders");
            modelBuilder.Entity<InspectionOrder>().ToTable("InspectionOrders");
            modelBuilder.Entity<SurgicalOrder>().ToTable("SurgicalOrders");
            modelBuilder.Entity<OperationOrder>().ToTable("OperationOrders");

            // --- 其他特殊字段配置 ---

            // RequiredMeds 已改为使用 MedicationOrderItem 关系表，不再需要 jsonb 配置

            // 确保 HospitalTimeSlot 的 ID 是手动输入的 (不自增)
            modelBuilder.Entity<HospitalTimeSlot>()
                .Property(h => h.Id)
                .ValueGeneratedNever();
        }
    }
}