using System;
using System.Collections.Generic;

namespace CareFlow.Infrastructure.TempModels;

public partial class Bed
{
    public string Id { get; set; } = null!;

    public string WardId { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime CreateTime { get; set; }

    public virtual ICollection<Patient> Patients { get; set; } = new List<Patient>();

    public virtual Ward Ward { get; set; } = null!;
}
