using CareFlow.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CareFlow.Infrastructure.Services;

public class RecordValidationService : IRecordValidationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RecordValidationService> _logger;
    
    // 支持的表名映射到实际的DbSet
    private readonly Dictionary<string, Func<string, Task<bool>>> _tableValidators;

    public RecordValidationService(ApplicationDbContext context, ILogger<RecordValidationService> logger)
    {
        _context = context;
        _logger = logger;
        
        // 初始化表验证器映射
        _tableValidators = new Dictionary<string, Func<string, Task<bool>>>(StringComparer.OrdinalIgnoreCase)
        {
            // 组织和人员表
            { "Departments", ValidateDepartmentAsync },
            { "Staffs", ValidateStaffAsync },
            { "Doctors", ValidateDoctorAsync },
            { "Nurses", ValidateNurseAsync },
            
            // 空间管理表
            { "Wards", ValidateWardAsync },
            { "Beds", ValidateBedAsync },
            
            // 患者表
            { "Patients", ValidatePatientAsync },
            
            // 医嘱表
            { "MedicalOrders", ValidateMedicalOrderAsync },
            { "MedicationOrders", ValidateMedicationOrderAsync },
            { "InspectionOrders", ValidateInspectionOrderAsync },
            { "SurgicalOrders", ValidateSurgicalOrderAsync },
            { "OperationOrders", ValidateOperationOrderAsync },
            
            // 药品和报告表
            { "Drugs", ValidateDrugAsync },
            { "InspectionReports", ValidateInspectionReportAsync },
            { "HospitalTimeSlots", ValidateHospitalTimeSlotAsync },
            
            // 护理相关表
            { "VitalSignsRecords", ValidateVitalSignsRecordAsync },
            { "NursingCareNotes", ValidateNursingCareNoteAsync },
            { "ExecutionTasks", ValidateExecutionTaskAsync },
            { "ShiftTypes", ValidateShiftTypeAsync },
            { "NurseRosters", ValidateNurseRosterAsync }
        };
    }

    public async Task<bool> RecordExistsAsync(string tableName, string recordId)
    {
        if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(recordId))
        {
            _logger.LogWarning("TableName or RecordId is null or empty. TableName: {TableName}, RecordId: {RecordId}", tableName, recordId);
            return false;
        }

        if (!_tableValidators.TryGetValue(tableName, out var validator))
        {
            _logger.LogWarning("Unsupported table name: {TableName}", tableName);
            return false;
        }

        try
        {
            bool exists = await validator(recordId);
            _logger.LogInformation("Record validation for {TableName}.{RecordId}: {Exists}", tableName, recordId, exists);
            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating record {RecordId} in table {TableName}", recordId, tableName);
            return false;
        }
    }

    public IEnumerable<string> GetSupportedTables()
    {
        return _tableValidators.Keys.OrderBy(x => x);
    }

    #region 私有验证方法

    private async Task<bool> ValidateDepartmentAsync(string id)
        => await _context.Departments.AnyAsync(x => x.Id == id);

    private async Task<bool> ValidateStaffAsync(string id)
        => await _context.Staffs.AnyAsync(x => x.Id == id);

    private async Task<bool> ValidateDoctorAsync(string id)
        => await _context.Doctors.AnyAsync(x => x.Id == id);

    private async Task<bool> ValidateNurseAsync(string id)
        => await _context.Nurses.AnyAsync(x => x.Id == id);

    private async Task<bool> ValidateWardAsync(string id)
        => await _context.Wards.AnyAsync(x => x.Id == id);

    private async Task<bool> ValidateBedAsync(string id)
        => await _context.Beds.AnyAsync(x => x.Id == id);

    private async Task<bool> ValidatePatientAsync(string id)
        => await _context.Patients.AnyAsync(x => x.Id == id);

    private async Task<bool> ValidateMedicalOrderAsync(string id)
    {
        // 对于long类型的ID，需要尝试转换
        if (long.TryParse(id, out var longId))
        {
            return await _context.MedicalOrders.AnyAsync(x => x.Id == longId);
        }
        return false;
    }

    private async Task<bool> ValidateMedicationOrderAsync(string id)
    {
        if (long.TryParse(id, out var longId))
        {
            return await _context.MedicationOrders.AnyAsync(x => x.Id == longId);
        }
        return false;
    }

    private async Task<bool> ValidateInspectionOrderAsync(string id)
    {
        if (long.TryParse(id, out var longId))
        {
            return await _context.InspectionOrders.AnyAsync(x => x.Id == longId);
        }
        return false;
    }

    private async Task<bool> ValidateSurgicalOrderAsync(string id)
    {
        if (long.TryParse(id, out var longId))
        {
            return await _context.SurgicalOrders.AnyAsync(x => x.Id == longId);
        }
        return false;
    }

    private async Task<bool> ValidateOperationOrderAsync(string id)
    {
        if (long.TryParse(id, out var longId))
        {
            return await _context.OperationOrders.AnyAsync(x => x.Id == longId);
        }
        return false;
    }

    private async Task<bool> ValidateDrugAsync(string id)
        => await _context.Drugs.AnyAsync(x => x.Id == id);

    private async Task<bool> ValidateInspectionReportAsync(string id)
    {
        if (long.TryParse(id, out var longId))
        {
            return await _context.InspectionReports.AnyAsync(x => x.Id == longId);
        }
        return false;
    }

    private async Task<bool> ValidateHospitalTimeSlotAsync(string id)
    {
        // HospitalTimeSlot 的 ID 是 int 类型
        if (int.TryParse(id, out var intId))
        {
            return await _context.HospitalTimeSlots.AnyAsync(x => x.Id == intId);
        }
        return false;
    }

    private async Task<bool> ValidateVitalSignsRecordAsync(string id)
    {
        if (long.TryParse(id, out var longId))
        {
            return await _context.VitalSignsRecords.AnyAsync(x => x.Id == longId);
        }
        return false;
    }

    private async Task<bool> ValidateNursingCareNoteAsync(string id)
    {
        if (long.TryParse(id, out var longId))
        {
            return await _context.NursingCareNotes.AnyAsync(x => x.Id == longId);
        }
        return false;
    }

    private async Task<bool> ValidateExecutionTaskAsync(string id)
    {
        if (long.TryParse(id, out var longId))
        {
            return await _context.ExecutionTasks.AnyAsync(x => x.Id == longId);
        }
        return false;
    }

    private async Task<bool> ValidateShiftTypeAsync(string id)
        => await _context.ShiftTypes.AnyAsync(x => x.Id == id);

    private async Task<bool> ValidateNurseRosterAsync(string id)
    {
        if (long.TryParse(id, out var longId))
        {
            return await _context.NurseRosters.AnyAsync(x => x.Id == longId);
        }
        return false;
    }

    #endregion
}