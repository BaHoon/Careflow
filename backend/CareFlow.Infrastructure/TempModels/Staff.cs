using System;
using System.Collections.Generic;

namespace CareFlow.Infrastructure.TempModels;

public partial class Staff
{
    public string Id { get; set; } = null!;

    public string EmployeeNumber { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string IdCard { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public string RoleType { get; set; } = null!;

    public bool IsActive { get; set; }

    public string DeptCode { get; set; } = null!;

    public DateTime CreateTime { get; set; }

    public virtual Department DeptCodeNavigation { get; set; } = null!;

    public virtual Doctor? Doctor { get; set; }

    public virtual Nurse? Nurse { get; set; }
}
