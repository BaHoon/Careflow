using System.ComponentModel.DataAnnotations.Schema;
using CareFlow.Core.Enums;

namespace CareFlow.Core.Models;

[Table("Doctors")] // 告诉数据库：虽然继承自Staff，但请单独给我建一张表
public class Doctor : StaffBase
{
    public DoctorTitle Title { get; set; } // 职称
    
    public int PrescriptionLevel { get; set; } // 处方权等级
    
    [Column(TypeName = "varchar(100)")] // 指定数据库字段类型
    public string Specialty { get; set; } = string.Empty; // 专长
}