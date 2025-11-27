namespace CareFlow.Core.Enums;

public enum StaffType
{
    Doctor = 1, // 建议显式赋值，防止数据库存错
    Nurse = 2,
    Admin = 3
}