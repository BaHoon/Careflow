using CareFlow.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace CareFlow.Infrastructure;

public class ApplicationDbContext : DbContext
{
    // 构造函数：接收外部配置
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    // =========================================================
    // 1. 注册所有的实体 (DbSet)
    // =========================================================
    
    // 组织架构与人员
    public DbSet<Department> Departments { get; set; }
    public DbSet<StaffBase> Staffs { get; set; }
    public DbSet<Doctor> Doctors { get; set; }
    public DbSet<Nurse> Nurses { get; set; }

    // 空间管理
    public DbSet<Room> Rooms { get; set; }
    public DbSet<Bed> Beds { get; set; }

    // 患者与住院
    public DbSet<PatientArchive> PatientArchives { get; set; }
    public DbSet<Admission> Admissions { get; set; }

    // 医嘱与药品
    public DbSet<Medication> Medications { get; set; }
    public DbSet<MedicalOrder> MedicalOrders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }

    // 工作流与任务
    public DbSet<NurseSchedule> NurseSchedules { get; set; }
    public DbSet<ExecutionTask> ExecutionTasks { get; set; }


    // =========================================================
    // 2. 配置数据库映射规则 (Fluent API)
    // =========================================================
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // -----------------------------------------------------
        // 模块 A: 人员继承配置 (TPT 模式)
        // -----------------------------------------------------
        modelBuilder.Entity<StaffBase>().ToTable("Staff_Base");
        modelBuilder.Entity<Doctor>().ToTable("Doctors");
        modelBuilder.Entity<Nurse>().ToTable("Nurses");

        // -----------------------------------------------------
        // 模块 B: 关系配置
        // -----------------------------------------------------

        // 1. 科室 <-> 员工 (防止级联删除)
        modelBuilder.Entity<StaffBase>()
            .HasOne(s => s.Department)
            .WithMany(d => d.Staffs)
            .HasForeignKey(s => s.DeptId)
            .OnDelete(DeleteBehavior.Restrict);

        // 2. 病房 <-> 床位
        modelBuilder.Entity<Bed>()
            .HasOne(b => b.Room)
            .WithMany(r => r.Beds)
            .HasForeignKey(b => b.RoomId);

        // 3. 住院记录 (Admission) 的复杂配置 [核心重点!]
        modelBuilder.Entity<Admission>(entity =>
        {
            // 配置 JSON 字段 (EF Core 8.0+ 新特性)
            // 这会让 AllergySnapshot 存为 JSON 格式，而不是分开建表
            entity.OwnsOne(x => x.AllergySnapshot, b => b.ToJson());

            // 配置主治医生 (AttendingDoctor)
            entity.HasOne(a => a.AttendingDoctor)
                  .WithMany()
                  .HasForeignKey(a => a.AttendingDoctorId)
                  .OnDelete(DeleteBehavior.Restrict); // 重要：防止删医生时删掉住院记录

            // 配置住院医生 (ResidentDoctor)
            entity.HasOne(a => a.ResidentDoctor)
                  .WithMany()
                  .HasForeignKey(a => a.ResidentDoctorId)
                  .OnDelete(DeleteBehavior.Restrict); // 重要：防止删医生时删掉住院记录

            // 配置患者关联
            entity.HasOne(a => a.Patient)
                  .WithMany(p => p.Admissions)
                  .HasForeignKey(a => a.PatientId);
        });

        // -----------------------------------------------------
        // 模块 C: 全局软删除过滤器 (Soft Delete)
        // -----------------------------------------------------
        // 只要是继承了 SoftDeleteEntity 的表，查询时自动过滤 IsDeleted=true
        
        modelBuilder.Entity<Department>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<StaffBase>().HasQueryFilter(x => !x.IsDeleted); // 子类Doctor/Nurse自动生效
        
        modelBuilder.Entity<Room>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<Bed>().HasQueryFilter(x => !x.IsDeleted);
        
        modelBuilder.Entity<PatientArchive>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<Admission>().HasQueryFilter(x => !x.IsDeleted);
        
        modelBuilder.Entity<Medication>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<MedicalOrder>().HasQueryFilter(x => !x.IsDeleted);
        
        // 注意：OrderItem, NurseSchedule, ExecutionTask 继承的是 EntityBase (非软删除)，所以不用加
    }
}