using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CareFlow.Core.Models.Space;

namespace CareFlow.Core.Models.Organization
{
    public class Patient
    {
        [Key]
        public string PatientId { get; set; } = string.Empty; // 住院号
        public string Name { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string IdCard { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public int Age { get; set; }
        public float Weight { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public int NursingGrade { get; set; }

        // 外键关系
        public string BedId { get; set; } = string.Empty;
        [ForeignKey("BedId")]
        public Bed Bed { get; set; } = null!;

        public string AttendingDoctorId { get; set; } = string.Empty;
        [ForeignKey("AttendingDoctorId")]
        public Doctor AttendingDoctor { get; set; } = null!;
    }
}