using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using CareFlow.Core.Models;
using CareFlow.Core.Models.Organization;
using CareFlow.Core.Models.Space;
using CareFlow.Core.Models.Medical;
using CareFlow.Core.Models.Nursing;

namespace CareFlow.Application.Interfaces
{
    public interface ICareFlowDbContext
    {
        // 这是一个万能方法，让你可以用 _context.Set<Patient>()
        DbSet<TEntity> Set<TEntity>() where TEntity : class; 
        
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        
        // 开始数据库事务
        Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

        // 组织与人员
        DbSet<CareFlow.Core.Models.Organization.Department> Departments { get; set; }
        DbSet<Staff> Staffs { get; set; }
        DbSet<Doctor> Doctors { get; set; }
        DbSet<Nurse> Nurses { get; set; }

        // 空间管理
        DbSet<Ward> Wards { get; set; }
        DbSet<Bed> Beds { get; set; }

        // 患者
        DbSet<Patient> Patients { get; set; }

        // 医嘱
        DbSet<MedicalOrder> MedicalOrders { get; set; }
        DbSet<MedicationOrder> MedicationOrders { get; set; }
        DbSet<MedicationOrderItem> MedicationOrderItems { get; set; }
        DbSet<InspectionOrder> InspectionOrders { get; set; }
        DbSet<SurgicalOrder> SurgicalOrders { get; set; }
        DbSet<OperationOrder> OperationOrders { get; set; }
        DbSet<DischargeOrder> DischargeOrders { get; set; }

        // 医嘱附属
        DbSet<InspectionReport> InspectionReports { get; set; }
        DbSet<HospitalTimeSlot> HospitalTimeSlots { get; set; }
        DbSet<Drug> Drugs { get; set; }
        DbSet<MedicalOrderStatusHistory> MedicalOrderStatusHistories { get; set; }

        // 护理执行
        DbSet<VitalSignsRecord> VitalSignsRecords { get; set; }
        DbSet<NursingCareNote> NursingCareNotes { get; set; }
        DbSet<ExecutionTask> ExecutionTasks { get; set; }
        DbSet<ShiftType> ShiftTypes { get; set; }
        DbSet<NurseRoster> NurseRosters { get; set; }
        DbSet<NursingTask> NursingTasks { get; set; }
        DbSet<MedicationReturnRequest> MedicationReturnRequests { get; set; }
        DbSet<NursingRecordSupplement> NursingRecordSupplements { get; set; }

        // 条形码
        DbSet<BarcodeIndex> BarcodeIndexes { get; set; }

        // 系统日志
        DbSet<SystemLog> SystemLogs { get; set; }
    }
}