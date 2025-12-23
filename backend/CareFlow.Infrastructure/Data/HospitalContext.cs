using System;
using System.Collections.Generic;
using CareFlow.Infrastructure.TempModels;
using Microsoft.EntityFrameworkCore;

namespace CareFlow.Infrastructure.Data;

public partial class HospitalContext : DbContext
{
    public HospitalContext()
    {
    }

    public HospitalContext(DbContextOptions<HospitalContext> options)
        : base(options)
    {
    }

    public virtual DbSet<BarcodeIndex> BarcodeIndexes { get; set; }

    public virtual DbSet<Bed> Beds { get; set; }

    public virtual DbSet<Department> Departments { get; set; }

    public virtual DbSet<Doctor> Doctors { get; set; }

    public virtual DbSet<Drug> Drugs { get; set; }

    public virtual DbSet<ExecutionTask> ExecutionTasks { get; set; }

    public virtual DbSet<HospitalTimeSlot> HospitalTimeSlots { get; set; }

    public virtual DbSet<InspectionOrder> InspectionOrders { get; set; }

    public virtual DbSet<InspectionReport> InspectionReports { get; set; }

    public virtual DbSet<MedicalOrder> MedicalOrders { get; set; }

    public virtual DbSet<MedicalOrderStatusHistory> MedicalOrderStatusHistories { get; set; }

    public virtual DbSet<MedicationOrder> MedicationOrders { get; set; }

    public virtual DbSet<MedicationOrderItem> MedicationOrderItems { get; set; }

    public virtual DbSet<Nurse> Nurses { get; set; }

    public virtual DbSet<NurseRoster> NurseRosters { get; set; }

    public virtual DbSet<NursingCareNote> NursingCareNotes { get; set; }

    public virtual DbSet<NursingTask> NursingTasks { get; set; }

    public virtual DbSet<OperationOrder> OperationOrders { get; set; }

    public virtual DbSet<Patient> Patients { get; set; }

    public virtual DbSet<ShiftType> ShiftTypes { get; set; }

    public virtual DbSet<Staff> Staffs { get; set; }

    public virtual DbSet<SurgicalOrder> SurgicalOrders { get; set; }

    public virtual DbSet<VitalSignsRecord> VitalSignsRecords { get; set; }

    public virtual DbSet<Ward> Wards { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseNpgsql("Name=DefaultConnection");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Bed>(entity =>
        {
            entity.HasIndex(e => e.WardId, "IX_Beds_WardId");

            entity.HasOne(d => d.Ward).WithMany(p => p.Beds).HasForeignKey(d => d.WardId);
        });

        modelBuilder.Entity<Doctor>(entity =>
        {
            entity.HasOne(d => d.IdNavigation).WithOne(p => p.Doctor).HasForeignKey<Doctor>(d => d.Id);
        });

        modelBuilder.Entity<Drug>(entity =>
        {
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.AdministrationRoute).HasMaxLength(50);
            entity.Property(e => e.ApprovalNumber).HasMaxLength(50);
            entity.Property(e => e.AtcCode).HasMaxLength(20);
            entity.Property(e => e.Category).HasMaxLength(100);
            entity.Property(e => e.Contraindications).HasMaxLength(1000);
            entity.Property(e => e.DosageForm).HasMaxLength(50);
            entity.Property(e => e.DosageInstructions).HasMaxLength(500);
            entity.Property(e => e.GenericName).HasMaxLength(200);
            entity.Property(e => e.Indications).HasMaxLength(1000);
            entity.Property(e => e.Manufacturer).HasMaxLength(200);
            entity.Property(e => e.PackageSpec).HasMaxLength(100);
            entity.Property(e => e.PriceUnit).HasMaxLength(20);
            entity.Property(e => e.Remarks).HasMaxLength(500);
            entity.Property(e => e.SideEffects).HasMaxLength(1000);
            entity.Property(e => e.Specification).HasMaxLength(100);
            entity.Property(e => e.Status).HasMaxLength(20);
            entity.Property(e => e.StorageConditions).HasMaxLength(200);
            entity.Property(e => e.TradeName).HasMaxLength(200);
            entity.Property(e => e.UnitPrice).HasPrecision(10, 4);
        });

        modelBuilder.Entity<ExecutionTask>(entity =>
        {
            entity.HasIndex(e => e.AssignedNurseId, "IX_ExecutionTasks_AssignedNurseId");

            entity.HasIndex(e => e.CompleterNurseId, "IX_ExecutionTasks_CompleterNurseId");

            entity.HasIndex(e => e.ExecutorStaffId, "IX_ExecutionTasks_ExecutorStaffId");

            entity.HasIndex(e => e.MedicalOrderId, "IX_ExecutionTasks_MedicalOrderId");

            entity.HasIndex(e => e.PatientId, "IX_ExecutionTasks_PatientId");

            entity.HasOne(d => d.AssignedNurse).WithMany(p => p.ExecutionTaskAssignedNurses).HasForeignKey(d => d.AssignedNurseId);

            entity.HasOne(d => d.CompleterNurse).WithMany(p => p.ExecutionTaskCompleterNurses).HasForeignKey(d => d.CompleterNurseId);

            entity.HasOne(d => d.ExecutorStaff).WithMany(p => p.ExecutionTaskExecutorStaffs).HasForeignKey(d => d.ExecutorStaffId);

            entity.HasOne(d => d.MedicalOrder).WithMany(p => p.ExecutionTasks).HasForeignKey(d => d.MedicalOrderId);

            entity.HasOne(d => d.Patient).WithMany(p => p.ExecutionTasks).HasForeignKey(d => d.PatientId);
        });

        modelBuilder.Entity<HospitalTimeSlot>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
        });

        modelBuilder.Entity<InspectionOrder>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.IdNavigation).WithOne(p => p.InspectionOrder).HasForeignKey<InspectionOrder>(d => d.Id);
        });

        modelBuilder.Entity<InspectionReport>(entity =>
        {
            entity.HasIndex(e => e.OrderId, "IX_InspectionReports_OrderId");

            entity.HasIndex(e => e.PatientId, "IX_InspectionReports_PatientId");

            entity.HasIndex(e => e.ReviewerId, "IX_InspectionReports_ReviewerId");

            entity.HasOne(d => d.Order).WithMany(p => p.InspectionReports).HasForeignKey(d => d.OrderId);

            entity.HasOne(d => d.Patient).WithMany(p => p.InspectionReports).HasForeignKey(d => d.PatientId);

            entity.HasOne(d => d.Reviewer).WithMany(p => p.InspectionReports).HasForeignKey(d => d.ReviewerId);
        });

        modelBuilder.Entity<MedicalOrder>(entity =>
        {
            entity.HasIndex(e => e.CancelledByDoctorId, "IX_MedicalOrders_CancelledByDoctorId");

            entity.HasIndex(e => e.DoctorId, "IX_MedicalOrders_DoctorId");

            entity.HasIndex(e => e.NurseId, "IX_MedicalOrders_NurseId");

            entity.HasIndex(e => e.PatientId, "IX_MedicalOrders_PatientId");

            entity.HasIndex(e => e.RejectedByNurseId, "IX_MedicalOrders_RejectedByNurseId");

            entity.HasIndex(e => e.SignedByNurseId, "IX_MedicalOrders_SignedByNurseId");

            entity.HasIndex(e => e.StopConfirmedByNurseId, "IX_MedicalOrders_StopConfirmedByNurseId");

            entity.HasIndex(e => e.StopDoctorId, "IX_MedicalOrders_StopDoctorId");

            entity.HasIndex(e => e.StopRejectedByNurseId, "IX_MedicalOrders_StopRejectedByNurseId");

            entity.HasOne(d => d.CancelledByDoctor).WithMany(p => p.MedicalOrderCancelledByDoctors).HasForeignKey(d => d.CancelledByDoctorId);

            entity.HasOne(d => d.Doctor).WithMany(p => p.MedicalOrderDoctors).HasForeignKey(d => d.DoctorId);

            entity.HasOne(d => d.Nurse).WithMany(p => p.MedicalOrderNurses).HasForeignKey(d => d.NurseId);

            entity.HasOne(d => d.Patient).WithMany(p => p.MedicalOrders).HasForeignKey(d => d.PatientId);

            entity.HasOne(d => d.RejectedByNurse).WithMany(p => p.MedicalOrderRejectedByNurses).HasForeignKey(d => d.RejectedByNurseId);

            entity.HasOne(d => d.SignedByNurse).WithMany(p => p.MedicalOrderSignedByNurses).HasForeignKey(d => d.SignedByNurseId);

            entity.HasOne(d => d.StopConfirmedByNurse).WithMany(p => p.MedicalOrderStopConfirmedByNurses).HasForeignKey(d => d.StopConfirmedByNurseId);

            entity.HasOne(d => d.StopDoctor).WithMany(p => p.MedicalOrderStopDoctors).HasForeignKey(d => d.StopDoctorId);

            entity.HasOne(d => d.StopRejectedByNurse).WithMany(p => p.MedicalOrderStopRejectedByNurses).HasForeignKey(d => d.StopRejectedByNurseId);
        });

        modelBuilder.Entity<MedicalOrderStatusHistory>(entity =>
        {
            entity.HasIndex(e => e.MedicalOrderId, "IX_MedicalOrderStatusHistories_MedicalOrderId");

            entity.HasOne(d => d.MedicalOrder).WithMany(p => p.MedicalOrderStatusHistories).HasForeignKey(d => d.MedicalOrderId);
        });

        modelBuilder.Entity<MedicationOrder>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.IdNavigation).WithOne(p => p.MedicationOrder).HasForeignKey<MedicationOrder>(d => d.Id);
        });

        modelBuilder.Entity<MedicationOrderItem>(entity =>
        {
            entity.HasIndex(e => e.DrugId, "IX_MedicationOrderItems_DrugId");

            entity.HasIndex(e => e.MedicalOrderId, "IX_MedicationOrderItems_MedicalOrderId");

            entity.Property(e => e.DrugId).HasMaxLength(50);

            entity.HasOne(d => d.Drug).WithMany(p => p.MedicationOrderItems).HasForeignKey(d => d.DrugId);

            entity.HasOne(d => d.MedicalOrder).WithMany(p => p.MedicationOrderItems).HasForeignKey(d => d.MedicalOrderId);
        });

        modelBuilder.Entity<Nurse>(entity =>
        {
            entity.HasOne(d => d.IdNavigation).WithOne(p => p.Nurse).HasForeignKey<Nurse>(d => d.Id);
        });

        modelBuilder.Entity<NurseRoster>(entity =>
        {
            entity.HasIndex(e => e.ShiftId, "IX_NurseRosters_ShiftId");

            entity.HasIndex(e => e.StaffId, "IX_NurseRosters_StaffId");

            entity.HasIndex(e => e.WardId, "IX_NurseRosters_WardId");

            entity.HasOne(d => d.Shift).WithMany(p => p.NurseRosters).HasForeignKey(d => d.ShiftId);

            entity.HasOne(d => d.Staff).WithMany(p => p.NurseRosters).HasForeignKey(d => d.StaffId);

            entity.HasOne(d => d.Ward).WithMany(p => p.NurseRosters).HasForeignKey(d => d.WardId);
        });

        modelBuilder.Entity<NursingCareNote>(entity =>
        {
            entity.HasIndex(e => e.PatientId, "IX_NursingCareNotes_PatientId");

            entity.HasIndex(e => e.RecorderNurseId, "IX_NursingCareNotes_RecorderNurseId");

            entity.Property(e => e.PipeCareData).HasColumnType("jsonb");

            entity.HasOne(d => d.Patient).WithMany(p => p.NursingCareNotes).HasForeignKey(d => d.PatientId);

            entity.HasOne(d => d.RecorderNurse).WithMany(p => p.NursingCareNotes).HasForeignKey(d => d.RecorderNurseId);
        });

        modelBuilder.Entity<NursingTask>(entity =>
        {
            entity.HasIndex(e => e.AssignedNurseId, "IX_NursingTasks_AssignedNurseId");

            entity.HasIndex(e => e.PatientId, "IX_NursingTasks_PatientId");

            entity.HasOne(d => d.AssignedNurse).WithMany(p => p.NursingTasks).HasForeignKey(d => d.AssignedNurseId);

            entity.HasOne(d => d.Patient).WithMany(p => p.NursingTasks).HasForeignKey(d => d.PatientId);
        });

        modelBuilder.Entity<OperationOrder>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.IdNavigation).WithOne(p => p.OperationOrder).HasForeignKey<OperationOrder>(d => d.Id);
        });

        modelBuilder.Entity<Patient>(entity =>
        {
            entity.HasIndex(e => e.AttendingDoctorId, "IX_Patients_AttendingDoctorId");

            entity.HasIndex(e => e.BedId, "IX_Patients_BedId");

            entity.HasOne(d => d.AttendingDoctor).WithMany(p => p.Patients).HasForeignKey(d => d.AttendingDoctorId);

            entity.HasOne(d => d.Bed).WithMany(p => p.Patients).HasForeignKey(d => d.BedId);
        });

        modelBuilder.Entity<Staff>(entity =>
        {
            entity.HasIndex(e => e.DeptCode, "IX_Staffs_DeptCode");

            entity.HasOne(d => d.DeptCodeNavigation).WithMany(p => p.Staff).HasForeignKey(d => d.DeptCode);
        });

        modelBuilder.Entity<SurgicalOrder>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.IdNavigation).WithOne(p => p.SurgicalOrder).HasForeignKey<SurgicalOrder>(d => d.Id);
        });

        modelBuilder.Entity<VitalSignsRecord>(entity =>
        {
            entity.HasIndex(e => e.PatientId, "IX_VitalSignsRecords_PatientId");

            entity.HasIndex(e => e.RecorderNurseId, "IX_VitalSignsRecords_RecorderNurseId");

            entity.HasOne(d => d.Patient).WithMany(p => p.VitalSignsRecords).HasForeignKey(d => d.PatientId);

            entity.HasOne(d => d.RecorderNurse).WithMany(p => p.VitalSignsRecords).HasForeignKey(d => d.RecorderNurseId);
        });

        modelBuilder.Entity<Ward>(entity =>
        {
            entity.HasIndex(e => e.DepartmentId, "IX_Wards_DepartmentId");

            entity.HasOne(d => d.Department).WithMany(p => p.Wards).HasForeignKey(d => d.DepartmentId);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
