using CareFlow.Core.Enums;
namespace CareFlow.Application.DTOs.Patient;

/// <summary>
/// 患者列表项DTO
/// </summary>
public class PatientListDto
{
    public string Id { get; set; } = null!;
    public string BedId { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Gender { get; set; } = null!;
    public int Age { get; set; }
    public float Weight { get; set; }
    public NursingGrade NursingGrade { get; set; }
    public string Department { get; set; } = null!;
    public string? Diagnosis { get; set; }
}

/// <summary>
/// 患者详情DTO
/// </summary>
public class PatientDetailDto
{
    public string Id { get; set; } = null!;
    public string BedId { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Gender { get; set; } = null!;
    public int Age { get; set; }
    public float Weight { get; set; }
    public NursingGrade NursingGrade { get; set; }
    public string Department { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;
    public string IdCard { get; set; } = null!;
    public DateTime DateOfBirth { get; set; }
    public string Status { get; set; } = null!;
    public string? AttendingDoctorName { get; set; }
    public string? Diagnosis { get; set; }
    public string[]? Allergies { get; set; }
    public string? MedicalHistory { get; set; }
}
