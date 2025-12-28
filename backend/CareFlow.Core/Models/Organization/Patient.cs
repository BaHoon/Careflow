using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CareFlow.Core.Models.Space;
using CareFlow.Core.Enums;
using CareFlow.Core.Models;





namespace CareFlow.Core.Models.Organization
{
    public class Patient : EntityBase<string>
    {
        public string Name { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string IdCard { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public int Age { get; set; }
        public float Height { get; set; }
        public float Weight { get; set; }
        public PatientStatus Status { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public NursingGrade NursingGrade { get; set; }
        
        // 入院相关信息
        public string? OutpatientDiagnosis { get; set; }
        public DateTime? ScheduledAdmissionTime { get; set; }
        public DateTime? ActualAdmissionTime { get; set; }

        // 近期护理任务异常状态：0=正常, 1=异常
        public int NursingAnomalyStatus { get; set; } = 0;

        // 外键关系
        public string BedId { get; set; } = string.Empty;
        [ForeignKey("BedId")]
        public Bed Bed { get; set; } = null!;

        public string AttendingDoctorId { get; set; } = string.Empty;
        [ForeignKey("AttendingDoctorId")]
        public Doctor AttendingDoctor { get; set; } = null!;
    }
}