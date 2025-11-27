using CareFlow.Core.Enums;

namespace CareFlow.Core.Models;

public class PatientArchive : SoftDeleteEntity<Guid>
{
    public string IdCardNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Gender Gender { get; set; }
    public DateTime DOB { get; set; } // 出生日期
    public string BloodType { get; set; } = string.Empty;
    public string EmergencyContact { get; set; } = string.Empty;
    public string EmergencyPhone { get; set; } = string.Empty;
    public string AllergyHistoryText { get; set; } = string.Empty; // 简述

    public virtual ICollection<Admission> Admissions { get; set; } = new List<Admission>();
}